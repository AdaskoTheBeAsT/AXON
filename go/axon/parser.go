package axon

import (
	"fmt"
	"regexp"
	"strconv"
	"strings"
	"time"
)

type AxonType string

const (
	TypeString    AxonType = "S"
	TypeInteger   AxonType = "I"
	TypeFloat     AxonType = "F"
	TypeBoolean   AxonType = "B"
	TypeTimestamp AxonType = "T"
)

type FieldDefinition struct {
	Name       string
	Type       AxonType
	IsNullable bool
}

type Schema struct {
	Name   string
	Fields []FieldDefinition
}

type DataBlock struct {
	SchemaName string
	Count      int
	Rows       []map[string]interface{}
}

type ParseResult struct {
	Schemas    []Schema
	DataBlocks []DataBlock
}

var dataHeaderRegex = regexp.MustCompile(`@data\s+(\w+)\[(\d+)\]`)

func Parse(input string) (*ParseResult, error) {
	lines := strings.Split(input, "\n")
	for i := range lines {
		lines[i] = strings.TrimSpace(lines[i])
	}

	var schemas []Schema
	var dataBlocks []DataBlock

	i := 0
	for i < len(lines) {
		line := lines[i]

		if strings.HasPrefix(line, "@schema") {
			schema, nextIdx, err := parseSchema(lines, i)
			if err != nil {
				return nil, err
			}
			schemas = append(schemas, schema)
			i = nextIdx
		} else if strings.HasPrefix(line, "@data") {
			dataBlock, nextIdx, err := parseDataBlock(lines, i, schemas)
			if err != nil {
				return nil, err
			}
			dataBlocks = append(dataBlocks, dataBlock)
			i = nextIdx
		} else {
			i++
		}
	}

	return &ParseResult{
		Schemas:    schemas,
		DataBlocks: dataBlocks,
	}, nil
}

func parseSchema(lines []string, startIndex int) (Schema, int, error) {
	headerLine := lines[startIndex]
	schemaName := strings.TrimSpace(strings.Replace(headerLine, "@schema", "", 1))

	var fields []FieldDefinition
	i := startIndex + 1

	for i < len(lines) {
		line := strings.TrimSpace(lines[i])

		if line == "@end" {
			i++
			break
		}

		if line == "" {
			i++
			continue
		}

		parts := strings.Split(line, ":")
		if len(parts) == 2 {
			fieldName := strings.TrimSpace(parts[0])
			typeStr := strings.TrimSpace(parts[1])
			isNullable := strings.HasSuffix(typeStr, "?")

			if isNullable {
				typeStr = strings.TrimSuffix(typeStr, "?")
			}

			fields = append(fields, FieldDefinition{
				Name:       fieldName,
				Type:       AxonType(typeStr),
				IsNullable: isNullable,
			})
		}

		i++
	}

	return Schema{
		Name:   schemaName,
		Fields: fields,
	}, i, nil
}

func parseDataBlock(lines []string, startIndex int, schemas []Schema) (DataBlock, int, error) {
	headerLine := lines[startIndex]
	matches := dataHeaderRegex.FindStringSubmatch(headerLine)

	if len(matches) < 3 {
		return DataBlock{}, 0, fmt.Errorf("invalid @data header: %s", headerLine)
	}

	schemaName := matches[1]
	count, _ := strconv.Atoi(matches[2])

	var schema *Schema
	for idx := range schemas {
		if schemas[idx].Name == schemaName {
			schema = &schemas[idx]
			break
		}
	}

	if schema == nil {
		return DataBlock{}, 0, fmt.Errorf("schema not found: %s", schemaName)
	}

	var rows []map[string]interface{}
	i := startIndex + 1

	for i < len(lines) {
		line := strings.TrimSpace(lines[i])

		if line == "@end" {
			i++
			break
		}

		if line == "" {
			i++
			continue
		}

		row, err := parseRow(line, *schema)
		if err != nil {
			return DataBlock{}, 0, err
		}
		rows = append(rows, row)
		i++
	}

	return DataBlock{
		SchemaName: schemaName,
		Count:      count,
		Rows:       rows,
	}, i, nil
}

func parseRow(line string, schema Schema) (map[string]interface{}, error) {
	values := splitRow(line)
	row := make(map[string]interface{})

	for i := 0; i < len(schema.Fields) && i < len(values); i++ {
		field := schema.Fields[i]
		value := values[i]

		if value == "_" {
			row[field.Name] = nil
			continue
		}

		parsed, err := parseValue(value, field.Type)
		if err != nil {
			return nil, err
		}
		row[field.Name] = parsed
	}

	return row, nil
}

func splitRow(line string) []string {
	var values []string
	var current strings.Builder
	inString := false
	escaped := false

	for i := 0; i < len(line); i++ {
		c := line[i]

		if escaped {
			current.WriteByte(unescapeChar(c))
			escaped = false
			continue
		}

		if c == '\\' {
			escaped = true
			continue
		}

		if c == '"' {
			inString = !inString
			continue
		}

		if c == '|' && !inString {
			values = append(values, current.String())
			current.Reset()
			continue
		}

		current.WriteByte(c)
	}

	if current.Len() > 0 || strings.HasSuffix(line, "|") {
		values = append(values, current.String())
	}

	return values
}

func parseValue(value string, axonType AxonType) (interface{}, error) {
	switch axonType {
	case TypeString:
		return unescapeString(value), nil
	case TypeInteger:
		return strconv.ParseInt(value, 10, 64)
	case TypeFloat:
		return strconv.ParseFloat(value, 64)
	case TypeBoolean:
		return value == "1", nil
	case TypeTimestamp:
		return time.Parse(time.RFC3339, value)
	default:
		return value, nil
	}
}

func unescapeChar(c byte) byte {
	switch c {
	case 'n':
		return '\n'
	case 't':
		return '\t'
	case 'r':
		return '\r'
	default:
		return c
	}
}

func unescapeString(value string) string {
	if !strings.Contains(value, "\\") {
		return value
	}

	var result strings.Builder
	escaped := false

	for i := 0; i < len(value); i++ {
		c := value[i]
		if escaped {
			result.WriteByte(unescapeChar(c))
			escaped = false
		} else if c == '\\' {
			escaped = true
		} else {
			result.WriteByte(c)
		}
	}

	return result.String()
}
