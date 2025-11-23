#include "axon_parser.hpp"
#include <iostream>

int main() {
    std::string axon_data = R"(
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
)";

    try {
        auto result = axon::AxonParser::parse(axon_data);
        
        std::cout << "Parsed " << result.schemas.size() << " schema(s) and "
                  << result.data_blocks.size() << " data block(s)\n\n";
        
        for (const auto& schema : result.schemas) {
            std::cout << "Schema: " << schema.name << "\n";
            for (const auto& field : schema.fields) {
                std::string nullable = field.is_nullable ? "?" : "";
                std::cout << "  - " << field.name << ": ";
                
                switch (field.type) {
                    case axon::AxonType::String: std::cout << "String"; break;
                    case axon::AxonType::Integer: std::cout << "Integer"; break;
                    case axon::AxonType::Float: std::cout << "Float"; break;
                    case axon::AxonType::Boolean: std::cout << "Boolean"; break;
                    case axon::AxonType::Timestamp: std::cout << "Timestamp"; break;
                }
                
                std::cout << nullable << "\n";
            }
            std::cout << "\n";
        }
        
        for (const auto& data_block : result.data_blocks) {
            std::cout << "Data: " << data_block.schema_name << " ("
                      << data_block.rows.size() << " rows)\n";
            
            for (const auto& row : data_block.rows) {
                std::cout << "  ";
                for (const auto& [key, value] : row) {
                    std::cout << key << "=";
                    
                    std::visit([](const auto& v) {
                        using T = std::decay_t<decltype(v)>;
                        if constexpr (std::is_same_v<T, std::monostate>) {
                            std::cout << "null";
                        } else if constexpr (std::is_same_v<T, std::string>) {
                            std::cout << v;
                        } else if constexpr (std::is_same_v<T, bool>) {
                            std::cout << (v ? "true" : "false");
                        } else {
                            std::cout << v;
                        }
                    }, value);
                    
                    std::cout << " ";
                }
                std::cout << "\n";
            }
            std::cout << "\n";
        }
        
    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << "\n";
        return 1;
    }
    
    return 0;
}
