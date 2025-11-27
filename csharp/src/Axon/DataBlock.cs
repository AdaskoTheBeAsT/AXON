using System.Runtime.CompilerServices;

namespace Axon;

/// <summary>
/// High-performance data block using array-backed rows for cache-friendly access.
/// </summary>
public sealed class DataBlock
{
    private readonly Schema _schema;
    private readonly List<object?[]> _rows;

    public DataBlock(Schema schema)
    {
        _schema = schema;
        SchemaName = schema.Name;
        _rows = [];
    }

    public string SchemaName { get; }

    public Schema Schema => _schema;

    public int Count => _rows.Count;

    public IReadOnlyList<Row> Rows => new RowList(_schema, _rows);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddRow(object?[] row) => _rows.Add(row);

    /// <summary>
    /// High-performance row accessor implementing dictionary interface.
    /// </summary>
    public readonly struct Row : IReadOnlyDictionary<string, object?>
    {
        private readonly Schema _schema;
        private readonly object?[] _values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Row(Schema schema, object?[] values)
        {
            _schema = schema;
            _values = values;
        }

        public int Count => _values.Length;

        public IEnumerable<string> Keys => _schema.Fields.Select(f => f.Name);

        public IEnumerable<object?> Values => _values;

        public object? this[string key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var idx = _schema.GetFieldIndex(key);
                return idx >= 0 && idx < _values.Length ? _values[idx] : null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => _schema.GetFieldIndex(key) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out object? value)
        {
            var idx = _schema.GetFieldIndex(key);
            if (idx >= 0 && idx < _values.Length)
            {
                value = _values[idx];
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var i = 0; i < _values.Length; i++)
            {
                yield return new KeyValuePair<string, object?>(_schema.Fields[i].Name, _values[i]);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class RowList(Schema schema, List<object?[]> rows) : IReadOnlyList<Row>
    {
        public int Count => rows.Count;

        public Row this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(schema, rows[index]);
        }

        public IEnumerator<Row> GetEnumerator()
        {
            foreach (var row in rows)
            {
                yield return new Row(schema, row);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
