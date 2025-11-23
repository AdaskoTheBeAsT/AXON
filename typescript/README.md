# AXON Parser - TypeScript Implementation

Minimal AXON format parser for Node 24+.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping (`\"`, `\\`, `\n`, `\t`, `\|`)
- ✅ Null value handling (`_`)
- ✅ Pipe-delimited row parsing with quote awareness
- ✅ Full TypeScript type safety

## Installation

```bash
npm install
```

## Usage

```typescript
import { AxonParser } from './axon-parser.js';

const axonData = `
@schema User
id:I
name:S
active:B
@end

@data User[2]
1|Alice|1
2|Bob|0
@end
`;

const { schemas, dataBlocks } = AxonParser.parse(axonData);

for (const dataBlock of dataBlocks) {
  for (const row of dataBlock.rows) {
    console.log(`ID: ${row.id}, Name: ${row.name}`);
  }
}
```

## Build & Run

```bash
# Build TypeScript
npm run build

# Run example
npm run example
```

## Type Mappings

| AXON Type | TypeScript Type |
|-----------|-----------------|
| `I` | `number` |
| `S` | `string` |
| `F` | `number` |
| `B` | `boolean` |
| `T` | `Date` |
| `?` (nullable) | `T \| null` |

## API

### `AxonParser.parse(input: string): ParseResult`

Parses AXON-formatted string into structured data.

**Returns:**
```typescript
{
  schemas: Schema[];      // Parsed schema definitions
  dataBlocks: DataBlock[]; // Parsed data rows
}
```

### Types

```typescript
enum AxonType { String, Integer, Float, Boolean, Timestamp }

interface FieldDefinition {
  name: string;
  type: AxonType;
  isNullable: boolean;
}

interface Schema {
  name: string;
  fields: FieldDefinition[];
}

interface DataBlock {
  schemaName: string;
  count: number;
  rows: Record<string, unknown>[];
}
```
