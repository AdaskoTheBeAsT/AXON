# AXON Parser - C++ Implementation

Minimal AXON format parser for C++17+.

## Features

- ✅ Schema parsing with typed fields (I, S, F, B, T)
- ✅ Nullable field support (`?`)
- ✅ Data block parsing with row counts
- ✅ Proper string escaping
- ✅ Null value handling (`_`)
- ✅ Modern C++17 with `std::variant`
- ✅ Zero external dependencies (except CMake)

## Usage

```cpp
#include "axon_parser.hpp"
#include <iostream>

std::string axon_data = R"(
@schema User
id:I
name:S
active:B
@end

@data User[2]
1|Alice|1
2|Bob|0
@end
)";

auto result = axon::AxonParser::parse(axon_data);

for (const auto& data_block : result.data_blocks) {
    for (const auto& row : data_block.rows) {
        auto id = std::get<int64_t>(row.at("id"));
        auto name = std::get<std::string>(row.at("name"));
        std::cout << "ID: " << id << ", Name: " << name << "\n";
    }
}
```

## Build & Run

```bash
# Configure
cmake -B build -DCMAKE_BUILD_TYPE=Release

# Build
cmake --build build --config Release

# Run example
./build/example         # Linux/macOS
.\build\Release\example.exe  # Windows
```

## Type Mappings

| AXON Type | C++ Type |
|-----------|----------|
| `I` | `int64_t` |
| `S` | `std::string` |
| `F` | `double` |
| `B` | `bool` |
| `T` | `std::string` (ISO-8601) |
| `?` (nullable) | `std::monostate` |

## API

### `AxonParser::parse(const std::string& input) -> ParseResult`

Parses AXON-formatted string into structured data.

**Returns:**
```cpp
struct ParseResult {
    std::vector<Schema> schemas;
    std::vector<DataBlock> data_blocks;
};

using AxonValue = std::variant<
    std::monostate,  // null
    std::string,
    int64_t,
    double,
    bool
>;
```

## Accessing Values

Use `std::get` or `std::visit`:

```cpp
// Using std::get (throws if wrong type)
auto name = std::get<std::string>(row["name"]);

// Using std::visit (type-safe)
std::visit([](const auto& value) {
    using T = std::decay_t<decltype(value)>;
    if constexpr (std::is_same_v<T, std::string>) {
        std::cout << "String: " << value << "\n";
    }
}, row["name"]);
```

## Requirements

- C++17 or later
- CMake 3.20+
- Any modern C++ compiler (GCC 9+, Clang 10+, MSVC 2019+)
