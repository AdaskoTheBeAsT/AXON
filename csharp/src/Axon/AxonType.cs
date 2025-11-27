namespace Axon;

/// <summary>
/// Represents the data types supported by the AXON format.
/// </summary>
public enum AxonType
{
    /// <summary>
    /// String type (S).
    /// </summary>
    String,

    /// <summary>
    /// Integer type (I).
    /// </summary>
    Integer,

    /// <summary>
    /// Floating-point type (F).
    /// </summary>
    Float,

    /// <summary>
    /// Boolean type (B).
    /// </summary>
    Boolean,

    /// <summary>
    /// Timestamp type (T).
    /// </summary>
    Timestamp,

    /// <summary>
    /// Decimal type (D) - precise decimal numbers without floating-point artifacts.
    /// </summary>
    Decimal,
}
