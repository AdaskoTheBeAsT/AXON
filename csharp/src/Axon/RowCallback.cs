namespace Axon;

/// <summary>
/// Delegate for streaming row parsing callback.
/// </summary>
/// <param name="schema">The schema for the current data block.</param>
/// <param name="rowIndex">The 0-based index of the current row.</param>
/// <param name="row">The raw row span (excluding newline).</param>
public delegate void RowCallback(Schema schema, int rowIndex, ReadOnlySpan<char> row);
