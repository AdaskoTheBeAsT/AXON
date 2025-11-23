using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axon;

public enum AxonType
{
    String,  // S
    Integer, // I
    Float,   // F
    Boolean, // B
    Timestamp // T
}

public record FieldDefinition(string Name, AxonType Type, bool IsNullable);

public record Schema(string Name, List<FieldDefinition> Fields);

public record DataBlock(string SchemaName, int Count, List<Dictionary<string, object?>> Rows);

public class AxonParser
{
    public static (List<Schema> Schemas, List<DataBlock> DataBlocks) Parse(string input)
    {
        var schemas = new List<Schema>();
        var dataBlocks = new List<DataBlock>();
        var lines = input.Split('\n').Select(l => l.Trim('\r', ' ')).ToArray();
        
        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            
            if (line.StartsWith("@schema"))
            {
                var schema = ParseSchema(lines, ref i);
                schemas.Add(schema);
            }
            else if (line.StartsWith("@data"))
            {
                var dataBlock = ParseDataBlock(lines, ref i, schemas);
                dataBlocks.Add(dataBlock);
            }
            else
            {
                i++;
            }
        }
        
        return (schemas, dataBlocks);
    }
    
    private static Schema ParseSchema(string[] lines, ref int index)
    {
        var headerLine = lines[index++];
        var schemaName = headerLine.Replace("@schema", "").Trim();
        var fields = new List<FieldDefinition>();
        
        while (index < lines.Length)
        {
            var line = lines[index].Trim();
            
            if (line == "@end")
            {
                index++;
                break;
            }
            
            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }
            
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var fieldName = parts[0].Trim();
                var typeStr = parts[1].Trim();
                bool isNullable = typeStr.EndsWith('?');
                
                if (isNullable)
                    typeStr = typeStr.TrimEnd('?');
                
                var type = typeStr switch
                {
                    "S" => AxonType.String,
                    "I" => AxonType.Integer,
                    "F" => AxonType.Float,
                    "B" => AxonType.Boolean,
                    "T" => AxonType.Timestamp,
                    _ => throw new Exception($"Unknown type: {typeStr}")
                };
                
                fields.Add(new FieldDefinition(fieldName, type, isNullable));
            }
            
            index++;
        }
        
        return new Schema(schemaName, fields);
    }
    
    private static DataBlock ParseDataBlock(string[] lines, ref int index, List<Schema> schemas)
    {
        var headerLine = lines[index++];
        var match = System.Text.RegularExpressions.Regex.Match(headerLine, @"@data\s+(\w+)\[(\d+)\]");
        
        if (!match.Success)
            throw new Exception($"Invalid @data header: {headerLine}");
        
        var schemaName = match.Groups[1].Value;
        var count = int.Parse(match.Groups[2].Value);
        
        var schema = schemas.FirstOrDefault(s => s.Name == schemaName)
            ?? throw new Exception($"Schema not found: {schemaName}");
        
        var rows = new List<Dictionary<string, object?>>();
        
        while (index < lines.Length)
        {
            var line = lines[index].Trim();
            
            if (line == "@end")
            {
                index++;
                break;
            }
            
            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }
            
            var row = ParseRow(line, schema);
            rows.Add(row);
            index++;
        }
        
        return new DataBlock(schemaName, count, rows);
    }
    
    private static Dictionary<string, object?> ParseRow(string line, Schema schema)
    {
        var values = SplitRow(line);
        var row = new Dictionary<string, object?>();
        
        for (int i = 0; i < schema.Fields.Count && i < values.Count; i++)
        {
            var field = schema.Fields[i];
            var value = values[i];
            
            if (value == "_")
            {
                row[field.Name] = null;
                continue;
            }
            
            object? parsed = field.Type switch
            {
                AxonType.String => UnescapeString(value),
                AxonType.Integer => long.Parse(value),
                AxonType.Float => double.Parse(value),
                AxonType.Boolean => value == "1",
                AxonType.Timestamp => DateTime.Parse(value),
                _ => value
            };
            
            row[field.Name] = parsed;
        }
        
        return row;
    }
    
    private static List<string> SplitRow(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inString = false;
        bool escaped = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (escaped)
            {
                current.Append(c switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => c
                });
                escaped = false;
                continue;
            }
            
            if (c == '\\')
            {
                escaped = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            
            if (c == '|' && !inString)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }
            
            current.Append(c);
        }
        
        if (current.Length > 0 || line.EndsWith('|'))
        {
            values.Add(current.ToString());
        }
        
        return values;
    }
    
    private static string UnescapeString(string value)
    {
        if (!value.Contains('\\'))
            return value;
        
        var sb = new StringBuilder();
        bool escaped = false;
        
        foreach (char c in value)
        {
            if (escaped)
            {
                sb.Append(c switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => c
                });
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString();
    }
}
