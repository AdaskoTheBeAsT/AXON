# AXON Parser - Go Implementation

Minimal AXON format parser for Go 1.23+.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping
- ✅ Null value handling (`_`)
- ✅ Idiomatic Go error handling

## Usage

```go
import "github.com/axon/axon"

axonData := `
@schema User
id:I
name:S
active:B
@end

@data User[2]
1|Alice|1
2|Bob|0
@end
`

result, err := axon.Parse(axonData)
if err != nil {
    log.Fatal(err)
}

for _, dataBlock := range result.DataBlocks {
    for _, row := range dataBlock.Rows {
        fmt.Printf("ID: %v, Name: %v\n", row["id"], row["name"])
    }
}
```

## Build & Run

```bash
# Build
go build ./...

# Run example
go run example/main.go

# Test
go test ./...
```

## Type Mappings

| AXON Type | Go Type |
|-----------|---------|
| `I` | `int64` |
| `S` | `string` |
| `F` | `float64` |
| `B` | `bool` |
| `T` | `time.Time` |
| `?` (nullable) | `interface{}` (can be nil) |

## API

### `Parse(input string) (*ParseResult, error)`

Parses AXON-formatted string into structured data.

**Returns:**
```go
type ParseResult struct {
    Schemas    []Schema
    DataBlocks []DataBlock
}
```
