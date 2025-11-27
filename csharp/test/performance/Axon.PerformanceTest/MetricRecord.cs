namespace Axon.Performance;

/// <summary>
/// Time series data model.
/// </summary>
public sealed class MetricRecord
{
    public DateTime Timestamp { get; set; }

    public int Views { get; set; }

    public int Clicks { get; set; }

    public int Conversions { get; set; }

    public double Revenue { get; set; }

    public double BounceRate { get; set; }
}
