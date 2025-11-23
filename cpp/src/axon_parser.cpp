#include "axon_parser.hpp"
#include <sstream>
#include <regex>
#include <algorithm>

namespace axon {

ParseResult AxonParser::parse(const std::string& input) {
    auto lines = split_lines(input);
    ParseResult result;
    
    size_t i = 0;
    while (i < lines.size()) {
        std::string line = trim(lines[i]);
        
        if (line.find("@schema") == 0) {
            result.schemas.push_back(parse_schema(lines, i));
        } else if (line.find("@data") == 0) {
            result.data_blocks.push_back(parse_data_block(lines, i, result.schemas));
        } else {
            i++;
        }
    }
    
    return result;
}

Schema AxonParser::parse_schema(const std::vector<std::string>& lines, size_t& index) {
    std::string header_line = lines[index++];
    std::string schema_name = trim(header_line.substr(7)); // Remove "@schema"
    
    Schema schema;
    schema.name = schema_name;
    
    while (index < lines.size()) {
        std::string line = trim(lines[index]);
        
        if (line == "@end") {
            index++;
            break;
        }
        
        if (line.empty()) {
            index++;
            continue;
        }
        
        size_t colon_pos = line.find(':');
        if (colon_pos != std::string::npos) {
            std::string field_name = trim(line.substr(0, colon_pos));
            std::string type_str = trim(line.substr(colon_pos + 1));
            bool is_nullable = !type_str.empty() && type_str.back() == '?';
            
            if (is_nullable) {
                type_str.pop_back();
            }
            
            AxonType type = parse_type(type_str[0]);
            schema.fields.push_back({field_name, type, is_nullable});
        }
        
        index++;
    }
    
    return schema;
}

DataBlock AxonParser::parse_data_block(
    const std::vector<std::string>& lines,
    size_t& index,
    const std::vector<Schema>& schemas
) {
    std::string header_line = lines[index++];
    std::regex pattern(R"(@data\s+(\w+)\[(\d+)\])");
    std::smatch matches;
    
    if (!std::regex_search(header_line, matches, pattern)) {
        throw std::runtime_error("Invalid @data header: " + header_line);
    }
    
    std::string schema_name = matches[1].str();
    size_t count = std::stoull(matches[2].str());
    
    const Schema* schema = nullptr;
    for (const auto& s : schemas) {
        if (s.name == schema_name) {
            schema = &s;
            break;
        }
    }
    
    if (!schema) {
        throw std::runtime_error("Schema not found: " + schema_name);
    }
    
    DataBlock data_block;
    data_block.schema_name = schema_name;
    data_block.count = count;
    
    while (index < lines.size()) {
        std::string line = trim(lines[index]);
        
        if (line == "@end") {
            index++;
            break;
        }
        
        if (line.empty()) {
            index++;
            continue;
        }
        
        data_block.rows.push_back(parse_row(line, *schema));
        index++;
    }
    
    return data_block;
}

std::map<std::string, AxonValue> AxonParser::parse_row(
    const std::string& line,
    const Schema& schema
) {
    auto values = split_row(line);
    std::map<std::string, AxonValue> row;
    
    for (size_t i = 0; i < schema.fields.size() && i < values.size(); i++) {
        const auto& field = schema.fields[i];
        const auto& value = values[i];
        
        if (value == "_") {
            row[field.name] = std::monostate{};
            continue;
        }
        
        row[field.name] = parse_value(value, field.type);
    }
    
    return row;
}

std::vector<std::string> AxonParser::split_row(const std::string& line) {
    std::vector<std::string> values;
    std::string current;
    bool in_string = false;
    bool escaped = false;
    
    for (size_t i = 0; i < line.length(); i++) {
        char c = line[i];
        
        if (escaped) {
            current += unescape_char(c);
            escaped = false;
            continue;
        }
        
        if (c == '\\') {
            escaped = true;
            continue;
        }
        
        if (c == '"') {
            in_string = !in_string;
            continue;
        }
        
        if (c == '|' && !in_string) {
            values.push_back(current);
            current.clear();
            continue;
        }
        
        current += c;
    }
    
    if (!current.empty() || line.back() == '|') {
        values.push_back(current);
    }
    
    return values;
}

AxonValue AxonParser::parse_value(const std::string& value, AxonType type) {
    switch (type) {
        case AxonType::String:
            return unescape_string(value);
        case AxonType::Integer:
            return static_cast<int64_t>(std::stoll(value));
        case AxonType::Float:
            return std::stod(value);
        case AxonType::Boolean:
            return value == "1";
        case AxonType::Timestamp:
            return value;
        default:
            return value;
    }
}

AxonType AxonParser::parse_type(char type_code) {
    switch (type_code) {
        case 'S': return AxonType::String;
        case 'I': return AxonType::Integer;
        case 'F': return AxonType::Float;
        case 'B': return AxonType::Boolean;
        case 'T': return AxonType::Timestamp;
        default: throw std::runtime_error(std::string("Unknown type: ") + type_code);
    }
}

char AxonParser::unescape_char(char c) {
    switch (c) {
        case 'n': return '\n';
        case 't': return '\t';
        case 'r': return '\r';
        default: return c;
    }
}

std::string AxonParser::unescape_string(const std::string& value) {
    if (value.find('\\') == std::string::npos) {
        return value;
    }
    
    std::string result;
    bool escaped = false;
    
    for (char c : value) {
        if (escaped) {
            result += unescape_char(c);
            escaped = false;
        } else if (c == '\\') {
            escaped = true;
        } else {
            result += c;
        }
    }
    
    return result;
}

std::vector<std::string> AxonParser::split_lines(const std::string& input) {
    std::vector<std::string> lines;
    std::stringstream ss(input);
    std::string line;
    
    while (std::getline(ss, line)) {
        lines.push_back(line);
    }
    
    return lines;
}

std::string AxonParser::trim(const std::string& str) {
    auto start = str.begin();
    while (start != str.end() && std::isspace(*start)) {
        start++;
    }
    
    auto end = str.end();
    do {
        end--;
    } while (std::distance(start, end) > 0 && std::isspace(*end));
    
    return std::string(start, end + 1);
}

} // namespace axon
