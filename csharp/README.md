# AXON Parser - C# Implementation

Minimal AXON format parser for .NET 10.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping (`\"`, `\\`, `\n`, `\t`, `\|`)
- ✅ Null value handling (`_`)
- ✅ Pipe-delimited row parsing with quote awareness

## Usage

```csharp
using Axon;

var axonData = @"
@schema User
id:I
name:S
active:B
@end

@data User[2]
1|Alice|1
2|Bob|0
@end
";

var (schemas, dataBlocks) = AxonParser.Parse(axonData);

foreach (var dataBlock in dataBlocks)
{
    foreach (var row in dataBlock.Rows)
    {
        Console.WriteLine($"ID: {row["id"]}, Name: {row["name"]}");
    }
}
```

## Build & Run

```bash
dotnet build
dotnet run --project Example.cs
```

## Type Mappings

| AXON Type | C# Type |
|-----------|---------|
| `I` | `long` |
| `S` | `string` |
| `F` | `double` |
| `B` | `bool` |
| `T` | `DateTime` |
| `?` (nullable) | `object?` |
