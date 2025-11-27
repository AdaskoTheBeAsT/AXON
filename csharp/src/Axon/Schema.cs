using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Axon;

/// <summary>
/// Represents an AXON schema definition with frozen field lookup for O(1) access.
/// </summary>
public sealed class Schema
{
    private readonly FrozenDictionary<string, int> _fieldIndex;

    public Schema(string name, IReadOnlyList<FieldDefinition> fields)
    {
        Name = name;
        Fields = [.. fields];
        _fieldIndex = Fields
            .Select((f, i) => (f.Name, i))
            .ToFrozenDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);
    }

    public string Name { get; }

    public FieldDefinition[] Fields { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetFieldIndex(string name) => _fieldIndex.TryGetValue(name, out var idx) ? idx : -1;
}
