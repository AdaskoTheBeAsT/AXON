using System.Diagnostics;
using System.Text.Json;
using Axon;
using BenchmarkDotNet.Running;
using Toon;

namespace Axon.Performance;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--tokens")
        {
            // Show token efficiency comparison
            PrintTokenEfficiencyReport();
            return;
        }

        if (args.Length > 0 && args[0] == "--quick")
        {
            // Quick non-benchmark test
            RunQuickTest();
            return;
        }

        if (args.Length > 0 && args[0] == "--compare")
        {
            // Full format comparison
            PrintFullComparison();
            return;
        }

        if (args.Length > 0 && args[0] == "--sample")
        {
            // Show sample outputs for each format
            PrintFormatSamples();
            return;
        }

        if (args.Length > 0 && args[0] == "--benchmark")
        {
            // Full manual benchmark
            RunFullBenchmark();
            return;
        }

        // Run BenchmarkDotNet (may not work on .NET 10 preview)
        try
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BenchmarkDotNet failed: {ex.Message}");
            Console.WriteLine("Running manual benchmark instead...\n");
            RunFullBenchmark();
        }
    }

    private static void PrintFormatSamples()
    {
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("                       FORMAT SAMPLES: AXON vs TOON vs JSON");
        Console.WriteLine(new string('=', 100));

        // XL Payload Sample - 5 metrics
        Console.WriteLine("\n=== XL PAYLOAD SAMPLE (5 metrics - time series data) ===\n");
        var metrics5 = TestDataGenerator.GenerateMetrics(5);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, };
        var jsonPretty = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, };

        // Standard AXON
        var axonStandard = AxonSerializer.Serialize(metrics5, "Metric");
        Console.WriteLine("--- AXON Standard Format ---");
        Console.WriteLine(axonStandard);

        // Time-Series AXON
        var axonTs = AxonSerializer.SerializeTimeSeries(metrics5, "Metric");
        Console.WriteLine("--- AXON Time-Series Optimized Format ---");
        Console.WriteLine(axonTs);

        // TOON
        var toon = ToonEncoder.Encode(new { metrics = metrics5.Select(m => new { m.Timestamp, m.Views, m.Clicks, m.Conversions, m.Revenue, m.BounceRate }).ToList() });
        Console.WriteLine("--- TOON Format ---");
        Console.WriteLine(toon);

        // JSON
        var json = JsonSerializer.Serialize(metrics5, jsonPretty);
        Console.WriteLine("--- JSON Pretty ---");
        Console.WriteLine(json);

        // Size comparison
        Console.WriteLine("\n--- SIZE COMPARISON (5 rows) ---");
        var axonStdMetrics = TokenCounter.GetMetrics(axonStandard);
        var axonTsMetrics = TokenCounter.GetMetrics(axonTs);
        var toonMetrics = TokenCounter.GetMetrics(toon);
        var jsonCompact = JsonSerializer.Serialize(metrics5, jsonOptions);
        var jsonMetrics = TokenCounter.GetMetrics(jsonCompact);

        Console.WriteLine($"{"Format",-25} {"Bytes",10} {"Est.Tokens",12}");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"{"AXON Standard",-25} {axonStdMetrics.Bytes,10:N0} {axonStdMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"AXON Time-Series",-25} {axonTsMetrics.Bytes,10:N0} {axonTsMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"TOON",-25} {toonMetrics.Bytes,10:N0} {toonMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"JSON (compact)",-25} {jsonMetrics.Bytes,10:N0} {jsonMetrics.EstimatedTokens,12:N0}");

        // Full XL comparison
        Console.WriteLine("\n\n=== FULL XL PAYLOAD (1000 metrics) ===\n");
        var metrics1000 = TestDataGenerator.GenerateMetrics(1000);

        var axon1000Std = AxonSerializer.Serialize(metrics1000, "Metric");
        var axon1000Ts = AxonSerializer.SerializeTimeSeries(metrics1000, "Metric");
        var toon1000 = ToonEncoder.Encode(new { metrics = metrics1000.Select(m => new { m.Timestamp, m.Views, m.Clicks, m.Conversions, m.Revenue, m.BounceRate }).ToList() });
        var json1000 = JsonSerializer.Serialize(metrics1000, jsonOptions);

        var axon1000StdM = TokenCounter.GetMetrics(axon1000Std);
        var axon1000TsM = TokenCounter.GetMetrics(axon1000Ts);
        var toon1000M = TokenCounter.GetMetrics(toon1000);
        var json1000M = TokenCounter.GetMetrics(json1000);

        Console.WriteLine($"{"Format",-25} {"Bytes",10} {"Est.Tokens",12} {"vs JSON",12} {"vs TOON",12}");
        Console.WriteLine(new string('-', 75));

        var toonSavingsVsJson = (double)(json1000M.EstimatedTokens - toon1000M.EstimatedTokens) / json1000M.EstimatedTokens * 100;
        var axonStdSavingsVsJson = (double)(json1000M.EstimatedTokens - axon1000StdM.EstimatedTokens) / json1000M.EstimatedTokens * 100;
        var axonTsSavingsVsJson = (double)(json1000M.EstimatedTokens - axon1000TsM.EstimatedTokens) / json1000M.EstimatedTokens * 100;
        var axonStdVsToon = (double)(toon1000M.EstimatedTokens - axon1000StdM.EstimatedTokens) / toon1000M.EstimatedTokens * 100;
        var axonTsVsToon = (double)(toon1000M.EstimatedTokens - axon1000TsM.EstimatedTokens) / toon1000M.EstimatedTokens * 100;

        Console.WriteLine($"{"AXON Standard",-25} {axon1000StdM.Bytes,10:N0} {axon1000StdM.EstimatedTokens,12:N0} {axonStdSavingsVsJson,11:F1}% {axonStdVsToon,11:F1}%");
        Console.WriteLine($"{"AXON Time-Series",-25} {axon1000TsM.Bytes,10:N0} {axon1000TsM.EstimatedTokens,12:N0} {axonTsSavingsVsJson,11:F1}% {axonTsVsToon,11:F1}%");
        Console.WriteLine($"{"TOON",-25} {toon1000M.Bytes,10:N0} {toon1000M.EstimatedTokens,12:N0} {toonSavingsVsJson,11:F1}% {"baseline",12}");
        Console.WriteLine($"{"JSON",-25} {json1000M.Bytes,10:N0} {json1000M.EstimatedTokens,12:N0} {"baseline",12} {"-",12}");

        // Winner
        var tsWins = axon1000TsM.EstimatedTokens < toon1000M.EstimatedTokens;
        Console.WriteLine($"\n🏆 XL Payload Winner: {(tsWins ? "AXON Time-Series" : "TOON")}");
        if (tsWins)
        {
            Console.WriteLine($"   AXON beats TOON by {axonTsVsToon:F1}% ({toon1000M.EstimatedTokens - axon1000TsM.EstimatedTokens:N0} fewer tokens)");
        }
    }

    private static void RunFullBenchmark()
    {
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("                    PERFORMANCE BENCHMARK: AXON vs TOON vs JSON");
        Console.WriteLine(new string('=', 100));

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, };

        // 3 payload sizes
        var small = TestDataGenerator.GenerateEmployees(100);
        var medium = TestDataGenerator.GenerateOrders(100);
        var large = TestDataGenerator.GenerateRepos(500);

        Console.WriteLine("\nPayload sizes:");
        Console.WriteLine($"  Small:  100 employees");
        Console.WriteLine($"  Medium: 100 orders");
        Console.WriteLine($"  Large:  500 repos");

        // Warmup
        Console.WriteLine("\nWarming up...");
        for (var i = 0; i < 50; i++)
        {
            _ = AxonSerializer.Serialize(small, nameof(Employee));
            _ = JsonSerializer.Serialize(small, jsonOptions);
            _ = ToonEncoder.Encode(new { data = small });
        }

        const int iterations = 500;
        Console.WriteLine($"\nRunning {iterations} iterations each...\n");

        // SMALL PAYLOAD BENCHMARK
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("SMALL PAYLOAD (100 employees)");
        Console.WriteLine(new string('=', 100));
        RunPayloadBenchmark(small, nameof(Employee), jsonOptions, iterations);

        // MEDIUM PAYLOAD BENCHMARK
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("MEDIUM PAYLOAD (100 orders)");
        Console.WriteLine(new string('=', 100));
        RunPayloadBenchmark(medium, nameof(Order), jsonOptions, iterations);

        // LARGE PAYLOAD BENCHMARK
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("LARGE PAYLOAD (500 repos)");
        Console.WriteLine(new string('=', 100));
        RunPayloadBenchmark(large, "Repo", jsonOptions, iterations);

        // Token summary
        Console.WriteLine("\n");
        PrintTokenEfficiencyReport();
    }

    private static void RunPayloadBenchmark<T>(List<T> data, string schemaName, JsonSerializerOptions jsonOptions, int iterations)
    {
        var sw = Stopwatch.StartNew();

        // Pre-serialize for parsing tests
        var axonData = AxonSerializer.Serialize(data, schemaName);
        var jsonData = JsonSerializer.Serialize(data, jsonOptions);
        var toonData = ToonEncoder.Encode(new { data = data.Select(x => x).ToList() });

        Console.WriteLine($"\nSerialized sizes: AXON={axonData.Length:N0} chars, TOON={toonData.Length:N0} chars, JSON={jsonData.Length:N0} chars");

        // SERIALIZATION BENCHMARKS
        Console.WriteLine("\n--- Serialization Speed ---");
        Console.WriteLine($"{"Format",-15} {"Total (ms)",12} {"Per-Op (µs)",12} {"Ops/sec",12} {"vs JSON",10}");
        Console.WriteLine(new string('-', 65));

        // AXON Serialize
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = AxonSerializer.Serialize(data, schemaName);
        }

        sw.Stop();
        var axonSerMs = sw.ElapsedMilliseconds;
        var axonSerUs = axonSerMs * 1000.0 / iterations;
        var axonSerOps = iterations * 1000.0 / Math.Max(1, axonSerMs);

        // JSON Serialize
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Serialize(data, jsonOptions);
        }

        sw.Stop();
        var jsonSerMs = sw.ElapsedMilliseconds;
        var jsonSerUs = jsonSerMs * 1000.0 / iterations;
        var jsonSerOps = iterations * 1000.0 / Math.Max(1, jsonSerMs);

        // TOON Serialize
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = ToonEncoder.Encode(new { data = data.Select(x => x).ToList() });
        }

        sw.Stop();
        var toonSerMs = sw.ElapsedMilliseconds;
        var toonSerUs = toonSerMs * 1000.0 / iterations;
        var toonSerOps = iterations * 1000.0 / Math.Max(1, toonSerMs);

        var axonVsJson = jsonSerMs > 0 ? (double)axonSerMs / jsonSerMs : 0;
        var toonVsJson = jsonSerMs > 0 ? (double)toonSerMs / jsonSerMs : 0;

        Console.WriteLine($"{"AXON",-15} {axonSerMs,12:N0} {axonSerUs,12:F1} {axonSerOps,12:N0} {axonVsJson,9:F2}x");
        Console.WriteLine($"{"TOON",-15} {toonSerMs,12:N0} {toonSerUs,12:F1} {toonSerOps,12:N0} {toonVsJson,9:F2}x");
        Console.WriteLine($"{"JSON",-15} {jsonSerMs,12:N0} {jsonSerUs,12:F1} {jsonSerOps,12:N0} {"baseline",10}");

        // PARSING/DESERIALIZATION BENCHMARKS
        Console.WriteLine("\n--- Parsing/Deserialization Speed ---");
        Console.WriteLine($"{"Format",-15} {"Total (ms)",12} {"Per-Op (µs)",12} {"Ops/sec",12} {"vs JSON",10}");
        Console.WriteLine(new string('-', 65));

        // AXON Parse
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = AxonParser.Parse(axonData);
        }

        sw.Stop();
        var axonParseMs = sw.ElapsedMilliseconds;
        var axonParseUs = axonParseMs * 1000.0 / iterations;
        var axonParseOps = iterations * 1000.0 / Math.Max(1, axonParseMs);

        // JSON Deserialize
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Deserialize<List<T>>(jsonData, jsonOptions);
        }

        sw.Stop();
        var jsonParseMs = sw.ElapsedMilliseconds;
        var jsonParseUs = jsonParseMs * 1000.0 / iterations;
        var jsonParseOps = iterations * 1000.0 / Math.Max(1, jsonParseMs);

        var axonParseVsJson = jsonParseMs > 0 ? (double)axonParseMs / jsonParseMs : 0;

        Console.WriteLine($"{"AXON",-15} {axonParseMs,12:N0} {axonParseUs,12:F1} {axonParseOps,12:N0} {axonParseVsJson,9:F2}x");
        Console.WriteLine($"{"TOON",-15} {"N/A (no decoder)",35}");
        Console.WriteLine($"{"JSON",-15} {jsonParseMs,12:N0} {jsonParseUs,12:F1} {jsonParseOps,12:N0} {"baseline",10}");

        // Token efficiency
        Console.WriteLine("\n--- Token Efficiency ---");
        var axonTokens = TokenCounter.EstimateTokens(axonData);
        var toonTokens = TokenCounter.EstimateTokens(toonData);
        var jsonTokens = TokenCounter.EstimateTokens(jsonData);

        var axonSavings = (double)(jsonTokens - axonTokens) / jsonTokens * 100;
        var toonSavings = (double)(jsonTokens - toonTokens) / jsonTokens * 100;

        Console.WriteLine($"{"Format",-15} {"Tokens",12} {"vs JSON",12}");
        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"{"AXON",-15} {axonTokens,12:N0} {axonSavings,11:F1}%");
        Console.WriteLine($"{"TOON",-15} {toonTokens,12:N0} {toonSavings,11:F1}%");
        Console.WriteLine($"{"JSON",-15} {jsonTokens,12:N0} {"baseline",12}");

        Console.WriteLine();
    }

    private static void PrintTokenEfficiencyReport()
    {
        Console.WriteLine("\n" + new string('=', 90));
        Console.WriteLine("                    TOKEN EFFICIENCY REPORT - AXON vs TOON vs JSON");
        Console.WriteLine(new string('=', 90));

        // Generate test data
        var employees100 = TestDataGenerator.GenerateEmployees(100);
        var orders100 = TestDataGenerator.GenerateOrders(100);
        var repos500 = TestDataGenerator.GenerateRepos(500);
        var metrics1000 = TestDataGenerator.GenerateMetrics(1000);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

        // Small payload
        PrintPayloadComparison(
            "Small Payload (100 employees)",
            AxonSerializer.Serialize(employees100, nameof(Employee)),
            ToonEncoder.Encode(new { employees = employees100.Select(e => new { e.Id, e.Name, e.Email, e.Department, e.Salary, e.YearsExperience, e.Active }).ToList() }),
            JsonSerializer.Serialize(employees100, jsonOptions));

        // Medium payload (all fields for fair comparison)
        PrintPayloadComparison(
            "Medium Payload (100 orders)",
            AxonSerializer.Serialize(orders100, nameof(Order)),
            ToonEncoder.Encode(new
            {
                orders = orders100.Select(o => new
                {
                    o.OrderId,
                    o.CustomerId,
                    o.CustomerName,
                    o.CustomerEmail,
                    o.ShippingAddress,
                    o.ShippingCity,
                    o.ShippingCountry,
                    o.ShippingZip,
                    o.Subtotal,
                    o.Tax,
                    o.Total,
                    o.ItemCount,
                    OrderDate = o.OrderDate.ToString("O"),
                    ShippedDate = o.ShippedDate?.ToString("O"),
                    o.Status,
                    o.PaymentMethod,
                }).ToList(),
            }),
            JsonSerializer.Serialize(orders100, jsonOptions));

        // Large payload (all fields for fair comparison)
        PrintPayloadComparison(
            "Large Payload (500 repos)",
            AxonSerializer.Serialize(repos500, "Repo"),
            ToonEncoder.Encode(new
            {
                repos = repos500.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.FullName,
                    r.Description,
                    r.Url,
                    r.HtmlUrl,
                    r.CloneUrl,
                    r.Language,
                    r.DefaultBranch,
                    r.License,
                    r.Stars,
                    r.Forks,
                    r.Watchers,
                    r.OpenIssues,
                    r.Size,
                    IsPrivate = r.Private,
                    IsFork = r.Fork,
                    r.Archived,
                    r.Disabled,
                    CreatedAt = r.CreatedAt.ToString("O"),
                    UpdatedAt = r.UpdatedAt.ToString("O"),
                    PushedAt = r.PushedAt.ToString("O"),
                }).ToList(),
            }),
            JsonSerializer.Serialize(repos500, jsonOptions));

        // XL payload - use time-series optimized format for metrics
        PrintPayloadComparison(
            "XL Payload (1000 metrics) - Time Series Optimized",
            AxonSerializer.SerializeTimeSeries(metrics1000, "Metric"),
            ToonEncoder.Encode(new { metrics = metrics1000.Select(m => new { m.Timestamp, m.Views, m.Clicks, m.Conversions, m.Revenue, m.BounceRate }).ToList() }),
            JsonSerializer.Serialize(metrics1000, jsonOptions));

        Console.WriteLine("\n" + new string('=', 90));
        Console.WriteLine("LEGEND:");
        Console.WriteLine("  - Est.Tokens: Estimated token count using BPE-style tokenization");
        Console.WriteLine("  - vs JSON: Token savings compared to compact JSON baseline");
        Console.WriteLine("  - AXON benefits: Schema + tabular layout = fewer tokens, LLM-friendly");
        Console.WriteLine(new string('=', 90));
    }

    private static void PrintPayloadComparison(string label, string axon, string toon, string json)
    {
        var axonMetrics = TokenCounter.GetMetrics(axon);
        var toonMetrics = TokenCounter.GetMetrics(toon);
        var jsonMetrics = TokenCounter.GetMetrics(json);

        Console.WriteLine($"\n{label}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Format",-12} {"Bytes",12} {"Characters",12} {"Est.Tokens",12} {"vs JSON",12} {"Bytes/Token",12}");
        Console.WriteLine(new string('-', 80));

        var jsonTokens = jsonMetrics.EstimatedTokens;

        var axonSavings = (double)(jsonTokens - axonMetrics.EstimatedTokens) / jsonTokens * 100;
        var toonSavings = (double)(jsonTokens - toonMetrics.EstimatedTokens) / jsonTokens * 100;

        Console.WriteLine($"{"AXON",-12} {axonMetrics.Bytes,12:N0} {axonMetrics.Characters,12:N0} {axonMetrics.EstimatedTokens,12:N0} {axonSavings,11:F1}% {axonMetrics.BytesPerToken,12:F1}");
        Console.WriteLine($"{"TOON",-12} {toonMetrics.Bytes,12:N0} {toonMetrics.Characters,12:N0} {toonMetrics.EstimatedTokens,12:N0} {toonSavings,11:F1}% {toonMetrics.BytesPerToken,12:F1}");
        Console.WriteLine($"{"JSON",-12} {jsonMetrics.Bytes,12:N0} {jsonMetrics.Characters,12:N0} {jsonMetrics.EstimatedTokens,12:N0} {"baseline",12} {jsonMetrics.BytesPerToken,12:F1}");

        // Winner
        var winner = axonMetrics.EstimatedTokens < toonMetrics.EstimatedTokens ? "AXON" : "TOON";
        var bestSavings = Math.Max(axonSavings, toonSavings);
        Console.WriteLine($"  → Winner: {winner} ({bestSavings:F1}% fewer tokens than JSON)");
    }

    private static void RunQuickTest()
    {
        Console.WriteLine("Running quick performance test...\n");

        // Generate test data
        var employees = TestDataGenerator.GenerateEmployees(1000);
        var axon = AxonSerializer.Serialize(employees, nameof(Employee));

        Console.WriteLine($"Test data: 1000 employees");
        Console.WriteLine($"AXON size: {axon.Length:N0} characters\n");

        // Warmup
        Console.WriteLine("Warming up...");
        for (var i = 0; i < 100; i++)
        {
            _ = AxonParser.Parse(axon);
        }

        // Test parsing
        const int iterations = 500;
        Console.WriteLine($"\nParsing {iterations} times each:\n");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = AxonParser.Parse(axon);
        }

        sw.Stop();
        Console.WriteLine($"AXON Parser:          {sw.ElapsedMilliseconds,6} ms ({sw.ElapsedMilliseconds * 1000.0 / iterations:F2} µs/parse)");

        // Test serialization
        Console.WriteLine($"\nSerializing {iterations} times each:\n");

        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = AxonSerializer.Serialize(employees, nameof(Employee));
        }

        sw.Stop();
        var axonSerMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"AXON Serializer:      {axonSerMs,6} ms ({axonSerMs * 1000.0 / iterations:F2} µs/serialize)");

        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = ToonEncoder.Encode(new { employees = employees.Select(e => new { e.Id, e.Name, e.Email, e.Department, e.Salary, e.YearsExperience, e.Active }).ToList() });
        }

        sw.Stop();
        var toonSerMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"TOON Serializer:      {toonSerMs,6} ms ({toonSerMs * 1000.0 / iterations:F2} µs/serialize)");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, };
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Serialize(employees, jsonOptions);
        }

        sw.Stop();
        var jsonSerMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"JSON Serializer:      {jsonSerMs,6} ms ({jsonSerMs * 1000.0 / iterations:F2} µs/serialize)");

        // Summary
        Console.WriteLine($"\n--- Serialization Speed Summary ---");
        Console.WriteLine($"AXON vs JSON: {(double)jsonSerMs / Math.Max(1, axonSerMs):F2}x faster");
        Console.WriteLine($"AXON vs TOON: {(double)toonSerMs / Math.Max(1, axonSerMs):F2}x faster");
        Console.WriteLine($"TOON vs JSON: {(double)jsonSerMs / Math.Max(1, toonSerMs):F2}x faster");

        // Deserialization benchmark
        Console.WriteLine($"\n\nDeserializing {iterations} times each:\n");

        var jsonData = JsonSerializer.Serialize(employees, jsonOptions);

        // Warmup deserialization
        for (var i = 0; i < 100; i++)
        {
            _ = AxonSerializer.Deserialize<Employee>(axon);
            _ = JsonSerializer.Deserialize<List<Employee>>(jsonData, jsonOptions);
        }

        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = AxonSerializer.Deserialize<Employee>(axon);
        }

        sw.Stop();
        var axonDeserMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"AXON Deserialize:     {axonDeserMs,6} ms ({axonDeserMs * 1000.0 / iterations:F2} µs/deserialize)");

        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            _ = JsonSerializer.Deserialize<List<Employee>>(jsonData, jsonOptions);
        }

        sw.Stop();
        var jsonDeserMs = sw.ElapsedMilliseconds;
        Console.WriteLine($"JSON Deserialize:     {jsonDeserMs,6} ms ({jsonDeserMs * 1000.0 / iterations:F2} µs/deserialize)");

        Console.WriteLine($"TOON Deserialize:     {"N/A (no decoder)",20}");

        Console.WriteLine($"\n--- Deserialization Speed Summary ---");
        Console.WriteLine($"AXON vs JSON: {(double)jsonDeserMs / Math.Max(1, axonDeserMs):F2}x faster");

        Console.WriteLine("\n" + new string('-', 50));
        PrintTokenEfficiencyReport();
    }

    private static void PrintFullComparison()
    {
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine("                         FULL FORMAT COMPARISON: AXON vs TOON vs JSON");
        Console.WriteLine(new string('=', 100));

        var employees = TestDataGenerator.GenerateEmployees(10);

        // Show sample outputs
        Console.WriteLine("\n=== SAMPLE DATA (10 employees) ===\n");

        var axon = AxonSerializer.Serialize(employees, nameof(Employee));
        var toon = ToonEncoder.Encode(new { employees = employees.Select(e => new { e.Id, e.Name, e.Email, e.Department, e.Salary, e.YearsExperience, e.Active }).ToList() });
        var json = JsonSerializer.Serialize(employees, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, });
        var jsonCompact = JsonSerializer.Serialize(employees, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, });

        Console.WriteLine("--- AXON Format ---");
        Console.WriteLine(axon);

        Console.WriteLine("\n--- TOON Format ---");
        Console.WriteLine(toon);

        Console.WriteLine("\n--- JSON Compact ---");
        Console.WriteLine(jsonCompact.Length > 500 ? jsonCompact[..500] + "..." : jsonCompact);

        // Metrics
        Console.WriteLine("\n\n=== METRICS COMPARISON ===\n");

        var axonMetrics = TokenCounter.GetMetrics(axon);
        var toonMetrics = TokenCounter.GetMetrics(toon);
        var jsonCompactMetrics = TokenCounter.GetMetrics(jsonCompact);
        var jsonMetrics = TokenCounter.GetMetrics(json);

        Console.WriteLine($"{"Format",-20} {"Bytes",10} {"Lines",8} {"Est.Tokens",12}");
        Console.WriteLine(new string('-', 54));
        Console.WriteLine($"{"AXON",-20} {axonMetrics.Bytes,10:N0} {axonMetrics.Lines,8} {axonMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"TOON",-20} {toonMetrics.Bytes,10:N0} {toonMetrics.Lines,8} {toonMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"JSON (compact)",-20} {jsonCompactMetrics.Bytes,10:N0} {jsonCompactMetrics.Lines,8} {jsonCompactMetrics.EstimatedTokens,12:N0}");
        Console.WriteLine($"{"JSON (formatted)",-20} {jsonMetrics.Bytes,10:N0} {jsonMetrics.Lines,8} {jsonMetrics.EstimatedTokens,12:N0}");

        // Summary
        Console.WriteLine("\n=== KEY ADVANTAGES ===\n");
        Console.WriteLine("AXON Format:");
        Console.WriteLine("  ✓ Explicit schema with typed fields (S=String, I=Integer, B=Boolean, etc.)");
        Console.WriteLine("  ✓ Row count hint [@data Schema[N]] helps LLMs understand data size");
        Console.WriteLine("  ✓ Pipe-delimited rows are compact and scannable");
        Console.WriteLine("  ✓ Supports null values with underscore (_)");
        Console.WriteLine("  ✓ Full round-trip serialization/deserialization");
        Console.WriteLine("  ✓ Multiple data blocks per file");

        Console.WriteLine("\nTOON Format:");
        Console.WriteLine("  ✓ YAML-like indentation is human-readable");
        Console.WriteLine("  ✓ CSV-style tabular arrays with field headers");
        Console.WriteLine("  ✓ Good for nested structures");
        Console.WriteLine("  ✗ Currently no .NET decoder available");

        Console.WriteLine("\nJSON:");
        Console.WriteLine("  ✓ Universal support across all languages");
        Console.WriteLine("  ✓ Rich tooling ecosystem");
        Console.WriteLine("  ✗ Most verbose (highest token count)");
        Console.WriteLine("  ✗ No schema information embedded");
    }
}
