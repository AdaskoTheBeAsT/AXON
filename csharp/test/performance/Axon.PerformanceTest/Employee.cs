namespace Axon.Performance;

/// <summary>
/// Small payload model (~100 bytes per record).
/// </summary>
public sealed class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public decimal Salary { get; set; }

    public int YearsExperience { get; set; }

    public bool Active { get; set; }
}
