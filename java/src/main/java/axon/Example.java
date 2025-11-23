package axon;

public class Example {
    public static void main(String[] args) {
        String axonData = """
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
            """;
        
        ParseResult result = AxonParser.parse(axonData);
        
        System.out.printf("Parsed %d schema(s) and %d data block(s)%n%n",
            result.schemas().size(), result.dataBlocks().size());
        
        for (Schema schema : result.schemas()) {
            System.out.println("Schema: " + schema.name());
            for (FieldDefinition field : schema.fields()) {
                String nullable = field.isNullable() ? "?" : "";
                System.out.printf("  - %s: %s%s%n", field.name(), field.type(), nullable);
            }
            System.out.println();
        }
        
        for (DataBlock dataBlock : result.dataBlocks()) {
            System.out.printf("Data: %s (%d rows)%n", dataBlock.schemaName(), dataBlock.rows().size());
            for (var row : dataBlock.rows()) {
                System.out.print("  ");
                row.forEach((key, value) -> 
                    System.out.printf("%s=%s ", key, value != null ? value : "null")
                );
                System.out.println();
            }
            System.out.println();
        }
    }
}
