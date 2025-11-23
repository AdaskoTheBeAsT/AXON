# AXON Parser - Rust Implementation

Minimal AXON format parser for Rust 1.70+.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping
- ✅ Null value handling (`_`)
- ✅ Zero-copy parsing where possible
- ✅ Strong type safety with enums

## Usage

```rust
use axon_parser::{parse, AxonValue};

let axon_data = r#"
@schema User
id:I
name:S
active:B
@end

@data User[2]
1|Alice|1
2|Bob|0
@end
"#;

let result = parse(axon_data).unwrap();

for data_block in &result.data_blocks {
    for row in &data_block.rows {
        println!("ID: {:?}, Name: {:?}", row.get("id"), row.get("name"));
    }
}
```

## Build & Run

```bash
# Build
cargo build --release

# Run example
cargo run --bin example

# Test
cargo test
```

## Type Mappings

| AXON Type | Rust Type |
|-----------|-----------|
| `I` | `i64` |
| `S` | `String` |
| `F` | `f64` |
| `B` | `bool` |
| `T` | `String` (ISO-8601) |
| `?` (nullable) | `AxonValue::Null` |

## API

### `parse(input: &str) -> Result<ParseResult, String>`

Parses AXON-formatted string into structured data.

**Returns:**
```rust
pub struct ParseResult {
    pub schemas: Vec<Schema>,
    pub data_blocks: Vec<DataBlock>,
}

pub enum AxonValue {
    Null,
    String(String),
    Integer(i64),
    Float(f64),
    Boolean(bool),
    Timestamp(String),
}
```
