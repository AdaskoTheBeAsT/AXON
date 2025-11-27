namespace Axon.Performance;

/// <summary>
/// Test data generator for benchmarks.
/// </summary>
public static class TestDataGenerator
{
    private static readonly string[] FirstNames =
    [
        "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack",
        "Kate", "Leo", "Mia", "Noah", "Olivia", "Peter", "Quinn", "Rose", "Sam", "Tina",
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
    ];

    private static readonly string[] Departments =
    [
        "Engineering", "Sales", "Marketing", "Finance", "HR", "Operations", "Legal", "Support",
    ];

    private static readonly string[] Countries =
    [
        "US", "UK", "CA", "DE", "FR", "AU", "JP", "BR", "IN", "CN",
    ];

    private static readonly string[] Cities =
    [
        "New York", "London", "Toronto", "Berlin", "Paris", "Sydney", "Tokyo", "São Paulo", "Mumbai", "Shanghai",
    ];

    private static readonly string[] Statuses = ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"];

    private static readonly string[] PaymentMethods = ["Credit Card", "PayPal", "Bank Transfer", "Crypto", "Apple Pay"];

    private static readonly string[] Languages =
    [
        "TypeScript", "Python", "JavaScript", "Java", "C#", "Go", "Rust", "C++", "Ruby", "PHP",
    ];

    private static readonly string[] Licenses = ["MIT", "Apache-2.0", "GPL-3.0", "BSD-3-Clause", "ISC", "MPL-2.0"];

    /// <summary>
    /// Generates employee records for small payload tests.
    /// </summary>
    public static List<Employee> GenerateEmployees(int count, int seed = 42)
    {
        var random = new Random(seed);
        var employees = new List<Employee>(count);

        for (var i = 0; i < count; i++)
        {
            var firstName = FirstNames[random.Next(FirstNames.Length)];
            var lastName = LastNames[random.Next(LastNames.Length)];

            employees.Add(new Employee
            {
                Id = i + 1,
                Name = $"{firstName} {lastName}",
                Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
                Department = Departments[random.Next(Departments.Length)],
                Salary = Math.Round((decimal)(random.NextDouble() * 150000 + 50000), 2),
                YearsExperience = random.Next(1, 30),
                Active = random.NextDouble() > 0.1, // 90% active
            });
        }

        return employees;
    }

    /// <summary>
    /// Generates order records for medium payload tests.
    /// </summary>
    public static List<Order> GenerateOrders(int count, int seed = 42)
    {
        var random = new Random(seed);
        var orders = new List<Order>(count);
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < count; i++)
        {
            var firstName = FirstNames[random.Next(FirstNames.Length)];
            var lastName = LastNames[random.Next(LastNames.Length)];
            var countryIdx = random.Next(Countries.Length);
            var subtotal = Math.Round((decimal)(random.NextDouble() * 1000 + 10), 2);
            var tax = Math.Round(subtotal * 0.08m, 2);
            var orderDate = baseDate.AddDays(random.Next(365)).AddHours(random.Next(24));
            var status = Statuses[random.Next(Statuses.Length)];

            orders.Add(new Order
            {
                OrderId = 1000 + i,
                CustomerId = $"CUST-{random.Next(10000):D5}",
                CustomerName = $"{firstName} {lastName}",
                CustomerEmail = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
                ShippingAddress = $"{random.Next(1, 9999)} {LastNames[random.Next(LastNames.Length)]} Street",
                ShippingCity = Cities[countryIdx],
                ShippingCountry = Countries[countryIdx],
                ShippingZip = $"{random.Next(10000, 99999)}",
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                ItemCount = random.Next(1, 10),
                OrderDate = orderDate,
                ShippedDate = status is "Shipped" or "Delivered" ? orderDate.AddDays(random.Next(1, 5)) : null,
                Status = status,
                PaymentMethod = PaymentMethods[random.Next(PaymentMethods.Length)],
            });
        }

        return orders;
    }

    /// <summary>
    /// Generates GitHub repository records for large payload tests.
    /// </summary>
    public static List<GitHubRepo> GenerateRepos(int count, int seed = 42)
    {
        var random = new Random(seed);
        var repos = new List<GitHubRepo>(count);
        var baseDate = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var projectNames = new[]
        {
            "awesome-project", "data-toolkit", "web-framework", "ml-pipeline", "api-gateway",
            "cache-service", "message-queue", "analytics-engine", "config-manager", "auth-service",
            "log-aggregator", "task-scheduler", "file-processor", "notification-hub", "search-index",
        };

        var descriptions = new[]
        {
            "A comprehensive toolkit for modern development workflows",
            "High-performance library for data processing and analysis",
            "Lightweight framework for building scalable web applications",
            "Machine learning pipeline with automatic feature engineering",
            "Flexible API gateway with rate limiting and authentication",
            "Distributed cache service with automatic failover",
            "Message queue implementation with exactly-once delivery",
            "Real-time analytics engine for large-scale data",
            "Configuration management with hot reload support",
            "Authentication service with OAuth2 and OIDC support",
        };

        for (var i = 0; i < count; i++)
        {
            var owner = LastNames[random.Next(LastNames.Length)].ToLowerInvariant();
            var project = projectNames[random.Next(projectNames.Length)];
            var created = baseDate.AddDays(random.Next(3650));
            var updated = created.AddDays(random.Next((int)(DateTime.UtcNow - created).TotalDays));
            var pushed = updated.AddDays(-random.Next(30));

            repos.Add(new GitHubRepo
            {
                Id = 10000000 + i,
                Name = project,
                FullName = $"{owner}/{project}",
                Description = descriptions[random.Next(descriptions.Length)],
                Url = $"https://api.github.com/repos/{owner}/{project}",
                HtmlUrl = $"https://github.com/{owner}/{project}",
                CloneUrl = $"https://github.com/{owner}/{project}.git",
                Language = Languages[random.Next(Languages.Length)],
                DefaultBranch = random.NextDouble() > 0.3 ? "main" : "master",
                License = Licenses[random.Next(Licenses.Length)],
                Stars = random.Next(0, 100000),
                Forks = random.Next(0, 10000),
                Watchers = random.Next(0, 5000),
                OpenIssues = random.Next(0, 500),
                Size = random.Next(100, 1000000),
                Private = false,
                Fork = random.NextDouble() > 0.9,
                Archived = random.NextDouble() > 0.95,
                Disabled = false,
                CreatedAt = created,
                UpdatedAt = updated,
                PushedAt = pushed,
            });
        }

        return repos;
    }

    /// <summary>
    /// Generates time series metric records.
    /// </summary>
    public static List<MetricRecord> GenerateMetrics(int days, int seed = 42)
    {
        var random = new Random(seed);
        var metrics = new List<MetricRecord>(days);
        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < days; i++)
        {
            metrics.Add(new MetricRecord
            {
                Timestamp = baseDate.AddDays(i),
                Views = random.Next(1000, 10000),
                Clicks = random.Next(50, 500),
                Conversions = random.Next(5, 50),
                Revenue = Math.Round(random.NextDouble() * 10000, 2),
                BounceRate = Math.Round(random.NextDouble() * 0.5 + 0.3, 2),
            });
        }

        return metrics;
    }

    /// <summary>
    /// Gets approximate target sizes for different payload categories.
    /// </summary>
    public static (int Small, int Medium, int Large) GetRecordCountsForTargetSize(int targetKb)
    {
        // Approximate bytes per record (JSON format)
        const int employeeBytes = 150;
        const int orderBytes = 450;
        const int repoBytes = 700;

        var targetBytes = targetKb * 1024;

        return (
            Small: targetBytes / employeeBytes,
            Medium: targetBytes / orderBytes,
            Large: targetBytes / repoBytes);
    }
}
