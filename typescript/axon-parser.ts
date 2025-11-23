export enum AxonType {
  String = 'S',
  Integer = 'I',
  Float = 'F',
  Boolean = 'B',
  Timestamp = 'T'
}

export interface FieldDefinition {
  name: string;
  type: AxonType;
  isNullable: boolean;
}

export interface Schema {
  name: string;
  fields: FieldDefinition[];
}

export interface DataBlock {
  schemaName: string;
  count: number;
  rows: Record<string, unknown>[];
}

export interface ParseResult {
  schemas: Schema[];
  dataBlocks: DataBlock[];
}

export class AxonParser {
  static parse(input: string): ParseResult {
    const schemas: Schema[] = [];
    const dataBlocks: DataBlock[] = [];
    const lines = input.split('\n').map(l => l.trim());
    
    let i = 0;
    while (i < lines.length) {
      const line = lines[i].trim();
      
      if (line.startsWith('@schema')) {
        const schema = this.parseSchema(lines, i);
        schemas.push(schema.schema);
        i = schema.nextIndex;
      } else if (line.startsWith('@data')) {
        const dataBlock = this.parseDataBlock(lines, i, schemas);
        dataBlocks.push(dataBlock.dataBlock);
        i = dataBlock.nextIndex;
      } else {
        i++;
      }
    }
    
    return { schemas, dataBlocks };
  }
  
  private static parseSchema(lines: string[], startIndex: number): { schema: Schema; nextIndex: number } {
    const headerLine = lines[startIndex];
    const schemaName = headerLine.replace('@schema', '').trim();
    const fields: FieldDefinition[] = [];
    
    let i = startIndex + 1;
    while (i < lines.length) {
      const line = lines[i].trim();
      
      if (line === '@end') {
        i++;
        break;
      }
      
      if (!line) {
        i++;
        continue;
      }
      
      const parts = line.split(':');
      if (parts.length === 2) {
        const fieldName = parts[0].trim();
        let typeStr = parts[1].trim();
        const isNullable = typeStr.endsWith('?');
        
        if (isNullable) {
          typeStr = typeStr.slice(0, -1);
        }
        
        const type = this.parseType(typeStr);
        fields.push({ name: fieldName, type, isNullable });
      }
      
      i++;
    }
    
    return { schema: { name: schemaName, fields }, nextIndex: i };
  }
  
  private static parseDataBlock(
    lines: string[],
    startIndex: number,
    schemas: Schema[]
  ): { dataBlock: DataBlock; nextIndex: number } {
    const headerLine = lines[startIndex];
    const match = headerLine.match(/@data\s+(\w+)\[(\d+)\]/);
    
    if (!match) {
      throw new Error(`Invalid @data header: ${headerLine}`);
    }
    
    const schemaName = match[1];
    const count = parseInt(match[2], 10);
    
    const schema = schemas.find(s => s.name === schemaName);
    if (!schema) {
      throw new Error(`Schema not found: ${schemaName}`);
    }
    
    const rows: Record<string, unknown>[] = [];
    let i = startIndex + 1;
    
    while (i < lines.length) {
      const line = lines[i].trim();
      
      if (line === '@end') {
        i++;
        break;
      }
      
      if (!line) {
        i++;
        continue;
      }
      
      const row = this.parseRow(line, schema);
      rows.push(row);
      i++;
    }
    
    return { dataBlock: { schemaName, count, rows }, nextIndex: i };
  }
  
  private static parseRow(line: string, schema: Schema): Record<string, unknown> {
    const values = this.splitRow(line);
    const row: Record<string, unknown> = {};
    
    for (let i = 0; i < schema.fields.length && i < values.length; i++) {
      const field = schema.fields[i];
      const value = values[i];
      
      if (value === '_') {
        row[field.name] = null;
        continue;
      }
      
      row[field.name] = this.parseValue(value, field.type);
    }
    
    return row;
  }
  
  private static splitRow(line: string): string[] {
    const values: string[] = [];
    let current = '';
    let inString = false;
    let escaped = false;
    
    for (let i = 0; i < line.length; i++) {
      const c = line[i];
      
      if (escaped) {
        current += this.unescapeChar(c);
        escaped = false;
        continue;
      }
      
      if (c === '\\') {
        escaped = true;
        continue;
      }
      
      if (c === '"') {
        inString = !inString;
        continue;
      }
      
      if (c === '|' && !inString) {
        values.push(current);
        current = '';
        continue;
      }
      
      current += c;
    }
    
    if (current.length > 0 || line.endsWith('|')) {
      values.push(current);
    }
    
    return values;
  }
  
  private static parseValue(value: string, type: AxonType): unknown {
    switch (type) {
      case AxonType.String:
        return this.unescapeString(value);
      case AxonType.Integer:
        return parseInt(value, 10);
      case AxonType.Float:
        return parseFloat(value);
      case AxonType.Boolean:
        return value === '1';
      case AxonType.Timestamp:
        return new Date(value);
      default:
        return value;
    }
  }
  
  private static parseType(typeStr: string): AxonType {
    switch (typeStr) {
      case 'S': return AxonType.String;
      case 'I': return AxonType.Integer;
      case 'F': return AxonType.Float;
      case 'B': return AxonType.Boolean;
      case 'T': return AxonType.Timestamp;
      default: throw new Error(`Unknown type: ${typeStr}`);
    }
  }
  
  private static unescapeChar(c: string): string {
    switch (c) {
      case 'n': return '\n';
      case 't': return '\t';
      case 'r': return '\r';
      default: return c;
    }
  }
  
  private static unescapeString(value: string): string {
    if (!value.includes('\\')) {
      return value;
    }
    
    let result = '';
    let escaped = false;
    
    for (const c of value) {
      if (escaped) {
        result += this.unescapeChar(c);
        escaped = false;
      } else if (c === '\\') {
        escaped = true;
      } else {
        result += c;
      }
    }
    
    return result;
  }
}
