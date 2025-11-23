package axon;

import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class AxonParser {
    
    private static final Pattern DATA_HEADER_PATTERN = Pattern.compile("@data\\s+(\\w+)\\[(\\d+)\\]");
    
    public static ParseResult parse(String input) {
        List<Schema> schemas = new ArrayList<>();
        List<DataBlock> dataBlocks = new ArrayList<>();
        String[] lines = input.split("\n");
        
        int i = 0;
        while (i < lines.length) {
            String line = lines[i].trim();
            
            if (line.startsWith("@schema")) {
                ParsedSchema parsed = parseSchema(lines, i);
                schemas.add(parsed.schema);
                i = parsed.nextIndex;
            } else if (line.startsWith("@data")) {
                ParsedDataBlock parsed = parseDataBlock(lines, i, schemas);
                dataBlocks.add(parsed.dataBlock);
                i = parsed.nextIndex;
            } else {
                i++;
            }
        }
        
        return new ParseResult(schemas, dataBlocks);
    }
    
    private static ParsedSchema parseSchema(String[] lines, int startIndex) {
        String headerLine = lines[startIndex];
        String schemaName = headerLine.replace("@schema", "").trim();
        List<FieldDefinition> fields = new ArrayList<>();
        
        int i = startIndex + 1;
        while (i < lines.length) {
            String line = lines[i].trim();
            
            if (line.equals("@end")) {
                i++;
                break;
            }
            
            if (line.isEmpty()) {
                i++;
                continue;
            }
            
            String[] parts = line.split(":");
            if (parts.length == 2) {
                String fieldName = parts[0].trim();
                String typeStr = parts[1].trim();
                boolean isNullable = typeStr.endsWith("?");
                
                if (isNullable) {
                    typeStr = typeStr.substring(0, typeStr.length() - 1);
                }
                
                AxonType type = AxonType.fromCode(typeStr.charAt(0));
                fields.add(new FieldDefinition(fieldName, type, isNullable));
            }
            
            i++;
        }
        
        return new ParsedSchema(new Schema(schemaName, fields), i);
    }
    
    private static ParsedDataBlock parseDataBlock(String[] lines, int startIndex, List<Schema> schemas) {
        String headerLine = lines[startIndex];
        Matcher matcher = DATA_HEADER_PATTERN.matcher(headerLine);
        
        if (!matcher.find()) {
            throw new IllegalArgumentException("Invalid @data header: " + headerLine);
        }
        
        String schemaName = matcher.group(1);
        int count = Integer.parseInt(matcher.group(2));
        
        Schema schema = schemas.stream()
            .filter(s -> s.name().equals(schemaName))
            .findFirst()
            .orElseThrow(() -> new IllegalArgumentException("Schema not found: " + schemaName));
        
        List<Map<String, Object>> rows = new ArrayList<>();
        int i = startIndex + 1;
        
        while (i < lines.length) {
            String line = lines[i].trim();
            
            if (line.equals("@end")) {
                i++;
                break;
            }
            
            if (line.isEmpty()) {
                i++;
                continue;
            }
            
            Map<String, Object> row = parseRow(line, schema);
            rows.add(row);
            i++;
        }
        
        return new ParsedDataBlock(new DataBlock(schemaName, count, rows), i);
    }
    
    private static Map<String, Object> parseRow(String line, Schema schema) {
        List<String> values = splitRow(line);
        Map<String, Object> row = new LinkedHashMap<>();
        
        for (int i = 0; i < schema.fields().size() && i < values.size(); i++) {
            FieldDefinition field = schema.fields().get(i);
            String value = values.get(i);
            
            if (value.equals("_")) {
                row.put(field.name(), null);
                continue;
            }
            
            Object parsed = parseValue(value, field.type());
            row.put(field.name(), parsed);
        }
        
        return row;
    }
    
    private static List<String> splitRow(String line) {
        List<String> values = new ArrayList<>();
        StringBuilder current = new StringBuilder();
        boolean inString = false;
        boolean escaped = false;
        
        for (int i = 0; i < line.length(); i++) {
            char c = line.charAt(i);
            
            if (escaped) {
                current.append(unescapeChar(c));
                escaped = false;
                continue;
            }
            
            if (c == '\\') {
                escaped = true;
                continue;
            }
            
            if (c == '"') {
                inString = !inString;
                continue;
            }
            
            if (c == '|' && !inString) {
                values.add(current.toString());
                current.setLength(0);
                continue;
            }
            
            current.append(c);
        }
        
        if (current.length() > 0 || line.endsWith("|")) {
            values.add(current.toString());
        }
        
        return values;
    }
    
    private static Object parseValue(String value, AxonType type) {
        return switch (type) {
            case STRING -> unescapeString(value);
            case INTEGER -> Long.parseLong(value);
            case FLOAT -> Double.parseDouble(value);
            case BOOLEAN -> value.equals("1");
            case TIMESTAMP -> LocalDateTime.parse(value, DateTimeFormatter.ISO_DATE_TIME);
        };
    }
    
    private static char unescapeChar(char c) {
        return switch (c) {
            case 'n' -> '\n';
            case 't' -> '\t';
            case 'r' -> '\r';
            default -> c;
        };
    }
    
    private static String unescapeString(String value) {
        if (!value.contains("\\")) {
            return value;
        }
        
        StringBuilder result = new StringBuilder();
        boolean escaped = false;
        
        for (char c : value.toCharArray()) {
            if (escaped) {
                result.append(unescapeChar(c));
                escaped = false;
            } else if (c == '\\') {
                escaped = true;
            } else {
                result.append(c);
            }
        }
        
        return result.toString();
    }
    
    private record ParsedSchema(Schema schema, int nextIndex) {}
    private record ParsedDataBlock(DataBlock dataBlock, int nextIndex) {}
}
