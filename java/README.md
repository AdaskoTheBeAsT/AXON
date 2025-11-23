# AXON Parser - Java Implementation

Minimal AXON format parser for Java 21+.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping
- ✅ Null value handling (`_`)
- ✅ Records for immutable data structures

## Usage

```java
import axon.*;

String axonData = """
    @schema User
    id:I
    name:S
    active:B
    @end
    
    @data User[2]
    1|Alice|1
    2|Bob|0
    @end
    """;

ParseResult result = AxonParser.parse(axonData);

for (DataBlock dataBlock : result.dataBlocks()) {
    for (var row : dataBlock.rows()) {
        System.out.println("ID: " + row.get("id") + ", Name: " + row.get("name"));
    }
}
```

## Build & Run

```bash
# Build with Maven
mvn clean package

# Run example
mvn exec:java -Dexec.mainClass="axon.Example"
```

## Type Mappings

| AXON Type | Java Type |
|-----------|-----------|
| `I` | `Long` |
| `S` | `String` |
| `F` | `Double` |
| `B` | `Boolean` |
| `T` | `LocalDateTime` |
| `?` (nullable) | `Object` (nullable) |
