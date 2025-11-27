using Axon;
var input = "@schema Test\nvalue:S\n@end\n\n@data Test[1]\nC:\\Users\\Test\n@end";
Console.WriteLine($"Input data row: [{input.Split('\n')[5]}]");
Console.WriteLine($"Input row chars: {string.Join(",", input.Split('\n')[5].Select(c => (int)c))}");
var (_, dataBlocks) = AxonParser.Parse(input);
Console.WriteLine($"Result: [{dataBlocks[0].Rows[0]["value"]}]");
