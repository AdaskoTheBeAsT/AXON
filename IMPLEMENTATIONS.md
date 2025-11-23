# AXON Parser Implementations

Reference implementations of AXON parsers across multiple languages.

## Available Languages

| Language | Version | Directory | Status |
|----------|---------|-----------|--------|
| **C#** | .NET 10 | [`/csharp`](./csharp) | ✅ Complete |
| **TypeScript** | Node 24 | [`/typescript`](./typescript) | ✅ Complete |
| **Java** | Java 21 | [`/java`](./java) | ✅ Complete |
| **Go** | Go 1.23 | [`/go`](./go) | ✅ Complete |
| **Rust** | Rust 1.70+ | [`/rust`](./rust) | ✅ Complete |
| **C++** | C++17 | [`/cpp`](./cpp) | ✅ Complete |

## Quick Start

### C# (.NET 10)
```bash
cd csharp
dotnet build
dotnet run --project Example.cs
```

### TypeScript (Node 24)
```bash
cd typescript
npm install
npm run example
```

### Java (Java 21)
```bash
cd java
mvn clean package
mvn exec:java -Dexec.mainClass="axon.Example"
```

### Go (Go 1.23)
```bash
cd go
go run example/main.go
```

### Rust (Rust 1.70+)
```bash
cd rust
cargo run --bin example
```

### C++ (C++17)
```bash
cd cpp
cmake -B build
cmake --build build
./build/example
```

## Features

All implementations include:

- ✅ **Schema parsing** - Type definitions with field names and types
- ✅ **Data parsing** - Pipe-delimited rows with type awareness
- ✅ **Nullable support** - Optional fields marked with `?`
- ✅ **String escaping** - Proper handling of `\"`, `\\`, `\n`, `\t`, `\|`
- ✅ **Null values** - Explicit `_` for null representation
- ✅ **Type safety** - Strong typing per language idioms
- ✅ **Streaming parsing** - O(n) single-pass parsing
- ✅ **Error handling** - Graceful failure with informative messages

## Example AXON Format

```
@schema User
id:I
name:S
email:S
active:B
age:I?
@end

@data User[3]
1|Alice|alice@example.com|1|28
2|Bob|bob@example.com|0|_
3|Carol|carol@example.com|1|35
@end
```

## Type System

| Code | Type | C# | TypeScript | Java | Go | Rust | C++ |
|------|------|-----|------------|------|-----|------|-----|
| `I` | Integer | `long` | `number` | `Long` | `int64` | `i64` | `int64_t` |
| `S` | String | `string` | `string` | `String` | `string` | `String` | `std::string` |
| `F` | Float | `double` | `number` | `Double` | `float64` | `f64` | `double` |
| `B` | Boolean | `bool` | `boolean` | `Boolean` | `bool` | `bool` | `bool` |
| `T` | Timestamp | `DateTime` | `Date` | `LocalDateTime` | `time.Time` | `String` | `std::string` |
| `?` | Nullable | `object?` | `T \| null` | `Object` | `interface{}` | `AxonValue::Null` | `std::monostate` |

## Performance Characteristics

All implementations use:
- **Single-pass parsing** - O(n) time complexity
- **Minimal allocations** - Efficient memory usage
- **Streaming support** - Can process large files
- **No regex in hot paths** - Only for header matching

## Architecture

Each parser follows the same structure:

1. **Lexing** - Split input into lines
2. **Schema parsing** - Extract type definitions
3. **Data parsing** - Parse rows with schema context
4. **Type coercion** - Convert string values to typed objects
5. **Result construction** - Return structured data

## Contributing

To add a new language implementation:

1. Create a new directory: `/[language]`
2. Implement the core parser following the spec
3. Add example usage
4. Include build/run instructions in README
5. Update this document

## Testing

Each implementation should handle:

- ✅ Basic types (I, S, F, B, T)
- ✅ Nullable fields
- ✅ String escaping (`\"`, `\\`, `\n`, `\|`)
- ✅ Empty strings
- ✅ Null values (`_`)
- ✅ Multiple schemas and data blocks
- ✅ Whitespace tolerance
- ✅ Error cases (malformed headers, missing schemas)

## License

All implementations follow the main project license (see [LICENSE](./LICENSE)).

---

**Need help?** Check individual language READMEs for detailed usage and API documentation.
