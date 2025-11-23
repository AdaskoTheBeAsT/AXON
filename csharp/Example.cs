using System;
using Axon;

namespace Axon.Examples;

public class Example
{
    public static void Main()
    {
        var axonData = @"
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
";

        var (schemas, dataBlocks) = AxonParser.Parse(axonData);
        
        Console.WriteLine($"Parsed {schemas.Count} schema(s) and {dataBlocks.Count} data block(s)\n");
        
        foreach (var schema in schemas)
        {
            Console.WriteLine($"Schema: {schema.Name}");
            foreach (var field in schema.Fields)
            {
                var nullable = field.IsNullable ? "?" : "";
                Console.WriteLine($"  - {field.Name}: {field.Type}{nullable}");
            }
            Console.WriteLine();
        }
        
        foreach (var dataBlock in dataBlocks)
        {
            Console.WriteLine($"Data: {dataBlock.SchemaName} ({dataBlock.Rows.Count} rows)");
            foreach (var row in dataBlock.Rows)
            {
                Console.Write("  ");
                foreach (var kvp in row)
                {
                    var value = kvp.Value?.ToString() ?? "null";
                    Console.Write($"{kvp.Key}={value} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
