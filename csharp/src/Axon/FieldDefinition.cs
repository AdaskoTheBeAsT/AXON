namespace Axon;

/// <summary>
/// Represents a field definition within an AXON schema.
/// </summary>
/// <remarks>
/// Readonly struct for zero-copy, stack allocation, and no defensive copies.
/// </remarks>
public readonly record struct FieldDefinition(string Name, AxonType Type, bool IsNullable = false);
