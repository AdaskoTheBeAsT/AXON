using System.Text.Json;
using Axon;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Toon;

namespace Axon.Performance;

/// <summary>
/// Comprehensive benchmarks comparing AXON vs TOON vs JSON formats.
/// Tests serialization speed, deserialization speed, and token efficiency.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[RankColumn]
public class FormatBenchmarks
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false, // Compact JSON for fair comparison
    };

    // Small payload: ~5KB (100 employees)
    private List<Employee> _employeesSmall = null!;
    private string _axonEmployeesSmall = null!;
    private string _toonEmployeesSmall = null!;
    private string _jsonEmployeesSmall = null!;
    private object _toonDataSmall = null!;

    // Medium payload: ~50KB (100 orders)
    private List<Order> _ordersMedium = null!;
    private string _axonOrdersMedium = null!;
    private string _toonOrdersMedium = null!;
    private string _jsonOrdersMedium = null!;
    private object _toonDataMedium = null!;

    // Large payload: ~500KB (1000 repos)
    private List<GitHubRepo> _reposLarge = null!;
    private string _axonReposLarge = null!;
    private string _toonReposLarge = null!;
    private string _jsonReposLarge = null!;
    private object _toonDataLarge = null!;

    // XL payload: ~1MB (time series)
    private List<MetricRecord> _metricsXl = null!;
    private string _axonMetricsXl = null!;
    private string _toonMetricsXl = null!;
    private string _jsonMetricsXl = null!;
    private object _toonDataXl = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data
        _employeesSmall = TestDataGenerator.GenerateEmployees(100);
        _ordersMedium = TestDataGenerator.GenerateOrders(100);
        _reposLarge = TestDataGenerator.GenerateRepos(500);
        _metricsXl = TestDataGenerator.GenerateMetrics(1000);

        // Prepare AXON formats
        _axonEmployeesSmall = AxonSerializer.Serialize(_employeesSmall, nameof(Employee));
        _axonOrdersMedium = AxonSerializer.Serialize(_ordersMedium, nameof(Order));
        _axonReposLarge = AxonSerializer.Serialize(_reposLarge, "Repo");
        _axonMetricsXl = AxonSerializer.Serialize(_metricsXl, "Metric");

        // Prepare TOON data (needs anonymous objects for ToonEncoder)
        _toonDataSmall = new
        {
            employees = _employeesSmall.Select(e => new
            {
                id = e.Id,
                name = e.Name,
                email = e.Email,
                department = e.Department,
                salary = e.Salary,
                yearsExperience = e.YearsExperience,
                active = e.Active,
            }).ToList(),
        };

        _toonDataMedium = new
        {
            orders = _ordersMedium.Select(o => new
            {
                orderId = o.OrderId,
                customerId = o.CustomerId,
                customerName = o.CustomerName,
                customerEmail = o.CustomerEmail,
                shippingAddress = o.ShippingAddress,
                shippingCity = o.ShippingCity,
                shippingCountry = o.ShippingCountry,
                shippingZip = o.ShippingZip,
                subtotal = o.Subtotal,
                tax = o.Tax,
                total = o.Total,
                itemCount = o.ItemCount,
                orderDate = o.OrderDate.ToString("O"),
                shippedDate = o.ShippedDate?.ToString("O"),
                status = o.Status,
                paymentMethod = o.PaymentMethod,
            }).ToList(),
        };

        _toonDataLarge = new
        {
            repos = _reposLarge.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                fullName = r.FullName,
                description = r.Description,
                url = r.Url,
                htmlUrl = r.HtmlUrl,
                cloneUrl = r.CloneUrl,
                language = r.Language,
                defaultBranch = r.DefaultBranch,
                license = r.License,
                stars = r.Stars,
                forks = r.Forks,
                watchers = r.Watchers,
                openIssues = r.OpenIssues,
                size = r.Size,
                isPrivate = r.Private,
                isFork = r.Fork,
                archived = r.Archived,
                disabled = r.Disabled,
                createdAt = r.CreatedAt.ToString("O"),
                updatedAt = r.UpdatedAt.ToString("O"),
                pushedAt = r.PushedAt.ToString("O"),
            }).ToList(),
        };

        _toonDataXl = new
        {
            metrics = _metricsXl.Select(m => new
            {
                timestamp = m.Timestamp.ToString("O"),
                views = m.Views,
                clicks = m.Clicks,
                conversions = m.Conversions,
                revenue = m.Revenue,
                bounceRate = m.BounceRate,
            }).ToList(),
        };

        // Pre-encode TOON
        _toonEmployeesSmall = ToonEncoder.Encode(_toonDataSmall);
        _toonOrdersMedium = ToonEncoder.Encode(_toonDataMedium);
        _toonReposLarge = ToonEncoder.Encode(_toonDataLarge);
        _toonMetricsXl = ToonEncoder.Encode(_toonDataXl);

        // Pre-encode JSON
        _jsonEmployeesSmall = JsonSerializer.Serialize(_employeesSmall, JsonOptions);
        _jsonOrdersMedium = JsonSerializer.Serialize(_ordersMedium, JsonOptions);
        _jsonReposLarge = JsonSerializer.Serialize(_reposLarge, JsonOptions);
        _jsonMetricsXl = JsonSerializer.Serialize(_metricsXl, JsonOptions);
    }

    // ==================== SERIALIZATION BENCHMARKS ====================

    [Benchmark(Description = "AXON Serialize - Small (100 emp, ~5KB)")]
    [BenchmarkCategory("Serialize", "Small")]
    public string AxonSerializeSmall() => AxonSerializer.Serialize(_employeesSmall, nameof(Employee));

    [Benchmark(Description = "AXON Serialize - Medium (100 orders, ~50KB)")]
    [BenchmarkCategory("Serialize", "Medium")]
    public string AxonSerializeMedium() => AxonSerializer.Serialize(_ordersMedium, nameof(Order));

    [Benchmark(Description = "AXON Serialize - Large (500 repos, ~500KB)")]
    [BenchmarkCategory("Serialize", "Large")]
    public string AxonSerializeLarge() => AxonSerializer.Serialize(_reposLarge, "Repo");

    [Benchmark(Description = "AXON Serialize - XL (1000 metrics, ~1MB)")]
    [BenchmarkCategory("Serialize", "XL")]
    public string AxonSerializeXl() => AxonSerializer.Serialize(_metricsXl, "Metric");

    [Benchmark(Description = "TOON Encode - Small (100 emp, ~5KB)")]
    [BenchmarkCategory("Serialize", "Small")]
    public string ToonSerializeSmall() => ToonEncoder.Encode(_toonDataSmall);

    [Benchmark(Description = "TOON Encode - Medium (100 orders, ~50KB)")]
    [BenchmarkCategory("Serialize", "Medium")]
    public string ToonSerializeMedium() => ToonEncoder.Encode(_toonDataMedium);

    [Benchmark(Description = "TOON Encode - Large (500 repos, ~500KB)")]
    [BenchmarkCategory("Serialize", "Large")]
    public string ToonSerializeLarge() => ToonEncoder.Encode(_toonDataLarge);

    [Benchmark(Description = "TOON Encode - XL (1000 metrics, ~1MB)")]
    [BenchmarkCategory("Serialize", "XL")]
    public string ToonSerializeXl() => ToonEncoder.Encode(_toonDataXl);

    [Benchmark(Description = "JSON Serialize - Small (100 emp, ~5KB)")]
    [BenchmarkCategory("Serialize", "Small")]
    public string JsonSerializeSmall() => JsonSerializer.Serialize(_employeesSmall, JsonOptions);

    [Benchmark(Description = "JSON Serialize - Medium (100 orders, ~50KB)")]
    [BenchmarkCategory("Serialize", "Medium")]
    public string JsonSerializeMedium() => JsonSerializer.Serialize(_ordersMedium, JsonOptions);

    [Benchmark(Description = "JSON Serialize - Large (500 repos, ~500KB)")]
    [BenchmarkCategory("Serialize", "Large")]
    public string JsonSerializeLarge() => JsonSerializer.Serialize(_reposLarge, JsonOptions);

    [Benchmark(Description = "JSON Serialize - XL (1000 metrics, ~1MB)")]
    [BenchmarkCategory("Serialize", "XL")]
    public string JsonSerializeXl() => JsonSerializer.Serialize(_metricsXl, JsonOptions);

    // ==================== DESERIALIZATION BENCHMARKS ====================

    [Benchmark(Description = "AXON Parse - Small")]
    [BenchmarkCategory("Deserialize", "Small")]
    public object AxonParseSmall() => AxonParser.Parse(_axonEmployeesSmall);

    [Benchmark(Description = "AXON Parse - Medium")]
    [BenchmarkCategory("Deserialize", "Medium")]
    public object AxonParseMedium() => AxonParser.Parse(_axonOrdersMedium);

    [Benchmark(Description = "AXON Parse - Large")]
    [BenchmarkCategory("Deserialize", "Large")]
    public object AxonParseLarge() => AxonParser.Parse(_axonReposLarge);

    [Benchmark(Description = "AXON Parse - XL")]
    [BenchmarkCategory("Deserialize", "XL")]
    public object AxonParseXl() => AxonParser.Parse(_axonMetricsXl);

    // Note: TOON library only has encoder, no decoder available
    // So we skip TOON deserialization benchmarks

    [Benchmark(Description = "JSON Deserialize - Small")]
    [BenchmarkCategory("Deserialize", "Small")]
    public List<Employee>? JsonDeserializeSmall() => JsonSerializer.Deserialize<List<Employee>>(_jsonEmployeesSmall, JsonOptions);

    [Benchmark(Description = "JSON Deserialize - Medium")]
    [BenchmarkCategory("Deserialize", "Medium")]
    public List<Order>? JsonDeserializeMedium() => JsonSerializer.Deserialize<List<Order>>(_jsonOrdersMedium, JsonOptions);

    [Benchmark(Description = "JSON Deserialize - Large")]
    [BenchmarkCategory("Deserialize", "Large")]
    public List<GitHubRepo>? JsonDeserializeLarge() => JsonSerializer.Deserialize<List<GitHubRepo>>(_jsonReposLarge, JsonOptions);

    [Benchmark(Description = "JSON Deserialize - XL")]
    [BenchmarkCategory("Deserialize", "XL")]
    public List<MetricRecord>? JsonDeserializeXl() => JsonSerializer.Deserialize<List<MetricRecord>>(_jsonMetricsXl, JsonOptions);
}

/// <summary>
/// Token efficiency comparison benchmarks.
/// </summary>
[MemoryDiagnoser]
public class TokenEfficiencyBenchmarks
{
    private string _axonSmall = null!;
    private string _axonMedium = null!;
    private string _axonLarge = null!;

    private string _toonSmall = null!;
    private string _toonMedium = null!;
    private string _toonLarge = null!;

    private string _jsonSmall = null!;
    private string _jsonMedium = null!;
    private string _jsonLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        var employees = TestDataGenerator.GenerateEmployees(100);
        var orders = TestDataGenerator.GenerateOrders(100);
        var repos = TestDataGenerator.GenerateRepos(500);

        _axonSmall = AxonSerializer.Serialize(employees, nameof(Employee));
        _axonMedium = AxonSerializer.Serialize(orders, nameof(Order));
        _axonLarge = AxonSerializer.Serialize(repos, "Repo");

        var toonSmall = new { employees = employees.Select(e => new { e.Id, e.Name, e.Email, e.Department, e.Salary, e.YearsExperience, e.Active }).ToList() };
        var toonMedium = new { orders = orders.Select(o => new { o.OrderId, o.CustomerId, o.CustomerName, o.Total, o.Status }).ToList() };
        var toonLarge = new { repos = repos.Select(r => new { r.Id, r.Name, r.FullName, r.Stars, r.Forks, r.Language }).ToList() };

        _toonSmall = ToonEncoder.Encode(toonSmall);
        _toonMedium = ToonEncoder.Encode(toonMedium);
        _toonLarge = ToonEncoder.Encode(toonLarge);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
        _jsonSmall = JsonSerializer.Serialize(employees, jsonOptions);
        _jsonMedium = JsonSerializer.Serialize(orders, jsonOptions);
        _jsonLarge = JsonSerializer.Serialize(repos, jsonOptions);
    }

    /// <summary>
    /// Prints token efficiency report (call from Program.cs manually).
    /// </summary>
    public void PrintTokenReport()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("TOKEN EFFICIENCY REPORT - AXON vs TOON vs JSON");
        Console.WriteLine(new string('=', 80));

        PrintComparison("Small Payload (100 employees)", _axonSmall, _toonSmall, _jsonSmall);
        PrintComparison("Medium Payload (100 orders)", _axonMedium, _toonMedium, _jsonMedium);
        PrintComparison("Large Payload (500 repos)", _axonLarge, _toonLarge, _jsonLarge);
    }

    private static void PrintComparison(string label, string axon, string toon, string json)
    {
        var axonMetrics = TokenCounter.GetMetrics(axon);
        var toonMetrics = TokenCounter.GetMetrics(toon);
        var jsonMetrics = TokenCounter.GetMetrics(json);

        Console.WriteLine($"\n{label}");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"{"Format",-15} {"Bytes",10} {"Chars",10} {"Est.Tokens",12} {"vs JSON",10}");
        Console.WriteLine(new string('-', 60));

        var jsonTokens = jsonMetrics.EstimatedTokens;

        Console.WriteLine($"{"AXON",-15} {axonMetrics.Bytes,10:N0} {axonMetrics.Characters,10:N0} {axonMetrics.EstimatedTokens,12:N0} {(double)(jsonTokens - axonMetrics.EstimatedTokens) / jsonTokens * 100,9:F1}%");
        Console.WriteLine($"{"TOON",-15} {toonMetrics.Bytes,10:N0} {toonMetrics.Characters,10:N0} {toonMetrics.EstimatedTokens,12:N0} {(double)(jsonTokens - toonMetrics.EstimatedTokens) / jsonTokens * 100,9:F1}%");
        Console.WriteLine($"{"JSON",-15} {jsonMetrics.Bytes,10:N0} {jsonMetrics.Characters,10:N0} {jsonMetrics.EstimatedTokens,12:N0} {"baseline",10}");
    }

    [Benchmark(Description = "Token count: AXON Small")]
    public int TokenCountAxonSmall() => TokenCounter.EstimateTokens(_axonSmall);

    [Benchmark(Description = "Token count: TOON Small")]
    public int TokenCountToonSmall() => TokenCounter.EstimateTokens(_toonSmall);

    [Benchmark(Description = "Token count: JSON Small")]
    public int TokenCountJsonSmall() => TokenCounter.EstimateTokens(_jsonSmall);
}

/// <summary>
/// Benchmark configuration.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithId("Default"));

        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
    }
}
