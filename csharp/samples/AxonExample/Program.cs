// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="AdaskoTheBeAsT">
// Copyright (c) AdaskoTheBeAsT. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Axon;

namespace Axon.Examples;

/// <summary>
/// Example demonstrating AXON parser usage.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point for the example.
    /// </summary>
    public static void Main()
    {
        // Compact backtick/tilde format: `Name[count](field:Type,...)
        var axonData = """
            `User[3](id:I,name:S,email:S,active:B,age:I?)
            1|Alice|alice@example.com|1|28
            2|Bob|bob@example.com|0|_
            3|Carol|carol@example.com|1|35
            ~
            """;

        var (schemas, dataBlocks) = AxonParser.Parse(axonData);

        Console.WriteLine($"Parsed {schemas.Count} schema(s) and {dataBlocks.Count} data block(s)");
        Console.WriteLine();

        foreach (var schema in schemas)
        {
            Console.WriteLine($"Schema: {schema.Name}");
            foreach (var field in schema.Fields)
            {
                var nullable = field.IsNullable ? "?" : string.Empty;
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
