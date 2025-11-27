namespace Axon.Performance;

/// <summary>
/// Large payload model (~500 bytes per record).
/// </summary>
public sealed class GitHubRepo
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string HtmlUrl { get; set; } = string.Empty;

    public string CloneUrl { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string DefaultBranch { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public int Stars { get; set; }

    public int Forks { get; set; }

    public int Watchers { get; set; }

    public int OpenIssues { get; set; }

    public long Size { get; set; }

    public bool Private { get; set; }

    public bool Fork { get; set; }

    public bool Archived { get; set; }

    public bool Disabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime PushedAt { get; set; }
}
