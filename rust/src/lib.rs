use std::collections::HashMap;
use regex::Regex;

#[derive(Debug, Clone, PartialEq)]
pub enum AxonType {
    String,
    Integer,
    Float,
    Boolean,
    Timestamp,
}

impl AxonType {
    pub fn from_code(code: char) -> Result<Self, String> {
        match code {
            'S' => Ok(AxonType::String),
            'I' => Ok(AxonType::Integer),
            'F' => Ok(AxonType::Float),
            'B' => Ok(AxonType::Boolean),
            'T' => Ok(AxonType::Timestamp),
            _ => Err(format!("Unknown type code: {}", code)),
        }
    }
}

#[derive(Debug, Clone)]
pub struct FieldDefinition {
    pub name: String,
    pub field_type: AxonType,
    pub is_nullable: bool,
}

#[derive(Debug, Clone)]
pub struct Schema {
    pub name: String,
    pub fields: Vec<FieldDefinition>,
}

#[derive(Debug, Clone)]
pub enum AxonValue {
    Null,
    String(String),
    Integer(i64),
    Float(f64),
    Boolean(bool),
    Timestamp(String),
}

#[derive(Debug, Clone)]
pub struct DataBlock {
    pub schema_name: String,
    pub count: usize,
    pub rows: Vec<HashMap<String, AxonValue>>,
}

#[derive(Debug)]
pub struct ParseResult {
    pub schemas: Vec<Schema>,
    pub data_blocks: Vec<DataBlock>,
}

pub fn parse(input: &str) -> Result<ParseResult, String> {
    let lines: Vec<&str> = input.lines().map(|l| l.trim()).collect();
    let mut schemas = Vec::new();
    let mut data_blocks = Vec::new();

    let mut i = 0;
    while i < lines.len() {
        let line = lines[i];

        if line.starts_with("@schema") {
            let (schema, next_idx) = parse_schema(&lines, i)?;
            schemas.push(schema);
            i = next_idx;
        } else if line.starts_with("@data") {
            let (data_block, next_idx) = parse_data_block(&lines, i, &schemas)?;
            data_blocks.push(data_block);
            i = next_idx;
        } else {
            i += 1;
        }
    }

    Ok(ParseResult {
        schemas,
        data_blocks,
    })
}

fn parse_schema(lines: &[&str], start_index: usize) -> Result<(Schema, usize), String> {
    let header_line = lines[start_index];
    let schema_name = header_line.replace("@schema", "").trim().to_string();

    let mut fields = Vec::new();
    let mut i = start_index + 1;

    while i < lines.len() {
        let line = lines[i].trim();

        if line == "@end" {
            i += 1;
            break;
        }

        if line.is_empty() {
            i += 1;
            continue;
        }

        let parts: Vec<&str> = line.split(':').collect();
        if parts.len() == 2 {
            let field_name = parts[0].trim().to_string();
            let mut type_str = parts[1].trim();
            let is_nullable = type_str.ends_with('?');

            if is_nullable {
                type_str = &type_str[..type_str.len() - 1];
            }

            let field_type = AxonType::from_code(type_str.chars().next().unwrap())?;
            fields.push(FieldDefinition {
                name: field_name,
                field_type,
                is_nullable,
            });
        }

        i += 1;
    }

    Ok((Schema {
        name: schema_name,
        fields,
    }, i))
}

fn parse_data_block(
    lines: &[&str],
    start_index: usize,
    schemas: &[Schema],
) -> Result<(DataBlock, usize), String> {
    let header_line = lines[start_index];
    let re = Regex::new(r"@data\s+(\w+)\[(\d+)\]").unwrap();
    let captures = re.captures(header_line)
        .ok_or_else(|| format!("Invalid @data header: {}", header_line))?;

    let schema_name = captures.get(1).unwrap().as_str().to_string();
    let count: usize = captures.get(2).unwrap().as_str().parse().unwrap();

    let schema = schemas.iter()
        .find(|s| s.name == schema_name)
        .ok_or_else(|| format!("Schema not found: {}", schema_name))?;

    let mut rows = Vec::new();
    let mut i = start_index + 1;

    while i < lines.len() {
        let line = lines[i].trim();

        if line == "@end" {
            i += 1;
            break;
        }

        if line.is_empty() {
            i += 1;
            continue;
        }

        let row = parse_row(line, schema)?;
        rows.push(row);
        i += 1;
    }

    Ok((DataBlock {
        schema_name,
        count,
        rows,
    }, i))
}

fn parse_row(line: &str, schema: &Schema) -> Result<HashMap<String, AxonValue>, String> {
    let values = split_row(line);
    let mut row = HashMap::new();

    for (i, field) in schema.fields.iter().enumerate() {
        if i >= values.len() {
            break;
        }

        let value = &values[i];

        if value == "_" {
            row.insert(field.name.clone(), AxonValue::Null);
            continue;
        }

        let parsed = parse_value(value, &field.field_type)?;
        row.insert(field.name.clone(), parsed);
    }

    Ok(row)
}

fn split_row(line: &str) -> Vec<String> {
    let mut values = Vec::new();
    let mut current = String::new();
    let mut in_string = false;
    let mut escaped = false;
    let chars: Vec<char> = line.chars().collect();

    for i in 0..chars.len() {
        let c = chars[i];

        if escaped {
            current.push(unescape_char(c));
            escaped = false;
            continue;
        }

        if c == '\\' {
            escaped = true;
            continue;
        }

        if c == '"' {
            in_string = !in_string;
            continue;
        }

        if c == '|' && !in_string {
            values.push(current.clone());
            current.clear();
            continue;
        }

        current.push(c);
    }

    if !current.is_empty() || line.ends_with('|') {
        values.push(current);
    }

    values
}

fn parse_value(value: &str, axon_type: &AxonType) -> Result<AxonValue, String> {
    match axon_type {
        AxonType::String => Ok(AxonValue::String(unescape_string(value))),
        AxonType::Integer => value.parse::<i64>()
            .map(AxonValue::Integer)
            .map_err(|e| e.to_string()),
        AxonType::Float => value.parse::<f64>()
            .map(AxonValue::Float)
            .map_err(|e| e.to_string()),
        AxonType::Boolean => Ok(AxonValue::Boolean(value == "1")),
        AxonType::Timestamp => Ok(AxonValue::Timestamp(value.to_string())),
    }
}

fn unescape_char(c: char) -> char {
    match c {
        'n' => '\n',
        't' => '\t',
        'r' => '\r',
        _ => c,
    }
}

fn unescape_string(value: &str) -> String {
    if !value.contains('\\') {
        return value.to_string();
    }

    let mut result = String::new();
    let mut escaped = false;

    for c in value.chars() {
        if escaped {
            result.push(unescape_char(c));
            escaped = false;
        } else if c == '\\' {
            escaped = true;
        } else {
            result.push(c);
        }
    }

    result
}
