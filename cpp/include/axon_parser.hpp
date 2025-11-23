#pragma once

#include <string>
#include <vector>
#include <map>
#include <variant>
#include <memory>
#include <optional>
#include <stdexcept>

namespace axon {

enum class AxonType {
    String,
    Integer,
    Float,
    Boolean,
    Timestamp
};

using AxonValue = std::variant<
    std::monostate,  // null
    std::string,
    int64_t,
    double,
    bool
>;

struct FieldDefinition {
    std::string name;
    AxonType type;
    bool is_nullable;
};

struct Schema {
    std::string name;
    std::vector<FieldDefinition> fields;
};

struct DataBlock {
    std::string schema_name;
    size_t count;
    std::vector<std::map<std::string, AxonValue>> rows;
};

struct ParseResult {
    std::vector<Schema> schemas;
    std::vector<DataBlock> data_blocks;
};

class AxonParser {
public:
    static ParseResult parse(const std::string& input);

private:
    static Schema parse_schema(const std::vector<std::string>& lines, size_t& index);
    static DataBlock parse_data_block(
        const std::vector<std::string>& lines,
        size_t& index,
        const std::vector<Schema>& schemas
    );
    static std::map<std::string, AxonValue> parse_row(
        const std::string& line,
        const Schema& schema
    );
    static std::vector<std::string> split_row(const std::string& line);
    static AxonValue parse_value(const std::string& value, AxonType type);
    static AxonType parse_type(char type_code);
    static char unescape_char(char c);
    static std::string unescape_string(const std::string& value);
    static std::vector<std::string> split_lines(const std::string& input);
    static std::string trim(const std::string& str);
};

} // namespace axon
