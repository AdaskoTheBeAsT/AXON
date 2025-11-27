#pragma warning disable CC0001 // Use var - cannot use var with stackalloc
#pragma warning disable CC0004 // Empty catch block - intentional for conversion errors

using System.Buffers;
using System.Collections;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Axon;

/// <summary>
/// Ultra-fast AXON serializer optimized for .NET 10 with minimal allocations.
/// </summary>
public static class AxonSerializer
{
    private static readonly Lock TypeCacheLock = new();
    private static readonly Dictionary<Type, TypeMeta> TypeCache = new(64);
    private static readonly SearchValues<char> EscapeChars = SearchValues.Create("|\\'\n\r\t\"");

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string Serialize<T>(T value, string? name = null)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(4096);
        SerializeCore(value, name ?? typeof(T).Name, sb);
        sb.Append('~');
        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void SerializeTo<T>(T value, StringBuilder sb, string? name = null)
    {
        if (value is null)
        {
            return;
        }

        SerializeCore(value, name ?? typeof(T).Name, sb);
        sb.Append('~');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string SerializeTimeSeries<T>(IEnumerable<T> items, string? name = null, DateTime? baseDate = null)
    {
        var list = items as IList<T> ?? [.. items];
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(list.Count * 32);
        var meta = GetMeta(typeof(T));
        var tsField = Array.Find(meta.Fields, f => f.Code == 'T');

        DateTime effectiveBase;
        if (baseDate.HasValue)
        {
            effectiveBase = baseDate.Value;
        }
        else if (tsField is not null && list[0] is not null)
        {
            var v = tsField.Get(list[0]!);
            effectiveBase = v switch
            {
                DateTime dt => dt.Date,
                DateTimeOffset dto => dto.UtcDateTime.Date,
                _ => DateTime.UtcNow.Date,
            };
        }
        else
        {
            effectiveBase = DateTime.UtcNow.Date;
        }

        WriteHeader(sb, name ?? typeof(T).Name, list.Count, meta, effectiveBase);

        foreach (var item in list)
        {
            WriteTimeSeriesRow(sb, item, meta, tsField, effectiveBase);
        }

        sb.Append('~');
        return sb.ToString();
    }

    /// <summary>
    /// Deserializes AXON data to a list of strongly-typed objects.
    /// </summary>
    public static IList<T> Deserialize<T>(string axon)
        where T : new()
    {
        var (_, blocks) = AxonParser.Parse(axon);
        var totalCount = blocks.Sum(b => b.Count);
        var result = new List<T>(totalCount);

        foreach (var block in blocks)
        {
            foreach (var row in block.Rows)
            {
                result.Add(MapToObject<T>(row, block.Schema));
            }
        }

        return result;
    }

    /// <summary>
    /// Deserializes a specific named block from AXON data.
    /// </summary>
    public static IList<T> Deserialize<T>(string axon, string blockName)
        where T : new()
    {
        var (_, blocks) = AxonParser.Parse(axon);
        var block = blocks.FirstOrDefault(b => b.SchemaName.Equals(blockName, StringComparison.OrdinalIgnoreCase));

        if (block is null)
        {
            return [];
        }

        var result = new List<T>(block.Count);
        foreach (var row in block.Rows)
        {
            result.Add(MapToObject<T>(row, block.Schema));
        }

        return result;
    }

    /// <summary>
    /// Deserializes AXON data using a custom mapping function.
    /// </summary>
    public static IList<T> Deserialize<T>(string axon, Func<DataBlock.Row, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var (_, blocks) = AxonParser.Parse(axon);
        var totalCount = blocks.Sum(b => b.Count);
        var result = new List<T>(totalCount);

        foreach (var block in blocks)
        {
            foreach (var row in block.Rows)
            {
                result.Add(mapper(row));
            }
        }

        return result;
    }

    /// <summary>
    /// Deserializes AXON data to a list of dictionaries (dynamic deserialization).
    /// </summary>
    public static IList<Dictionary<string, object?>> DeserializeDynamic(string axon)
    {
        var (_, blocks) = AxonParser.Parse(axon);
        var totalCount = blocks.Sum(b => b.Count);
        var result = new List<Dictionary<string, object?>>(totalCount);

        foreach (var block in blocks)
        {
            foreach (var row in block.Rows)
            {
                var dict = new Dictionary<string, object?>(row.Count, StringComparer.Ordinal);
                foreach (var kvp in row)
                {
                    dict[kvp.Key] = kvp.Value;
                }

                result.Add(dict);
            }
        }

        return result;
    }

    /// <summary>
    /// Deserializes the first block from AXON data to a single object.
    /// </summary>
    public static T? DeserializeOne<T>(string axon)
        where T : new()
    {
        var (_, blocks) = AxonParser.Parse(axon);
        if (blocks.Count == 0 || blocks[0].Count == 0)
        {
            return default;
        }

        return MapToObject<T>(blocks[0].Rows[0], blocks[0].Schema);
    }

    /// <summary>
    /// Parses AXON and returns raw schemas and data blocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (IReadOnlyList<Schema> Schemas, IReadOnlyList<DataBlock> DataBlocks) DeserializeRaw(string axon)
        => AxonParser.Parse(axon);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeCore<T>(T value, string name, StringBuilder sb)
    {
        if (value is IEnumerable enumerable && typeof(T) != typeof(string))
        {
            var elemType = GetElementType(typeof(T));
            if (elemType is not null)
            {
                SerializeCollection(enumerable, elemType, name, sb);
                return;
            }
        }

        SerializeSingle(value, name, sb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeCollection(IEnumerable items, Type elemType, string name, StringBuilder sb)
    {
        var meta = GetMeta(elemType);
        var list = items.Cast<object?>().ToList();
        if (list.Count == 0)
        {
            return;
        }

        WriteHeader(sb, name, list.Count, meta, null);

        foreach (var item in list)
        {
            WriteRow(sb, item, meta);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeSingle<T>(T value, string name, StringBuilder sb)
    {
        var meta = GetMeta(typeof(T));
        WriteHeader(sb, name, 1, meta, null);
        WriteRow(sb, value, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHeader(StringBuilder sb, string name, int count, TypeMeta meta, DateTime? baseDate)
    {
        sb.Append('`');
        sb.Append(name);
        sb.Append('[');
        sb.Append(count);

        if (baseDate.HasValue)
        {
            sb.Append('@');
            Span<char> buf = stackalloc char[6];
            baseDate.Value.TryFormat(buf, out _, "yyMMdd", CultureInfo.InvariantCulture);
            sb.Append(buf);
        }

        sb.Append("](");

        for (var i = 0; i < meta.Fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            var f = meta.Fields[i];
            sb.Append(f.Name);
            sb.Append(':');
            sb.Append(f.Code);
            if (f.Nullable)
            {
                sb.Append('?');
            }
        }

        sb.AppendLine(")");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteRow(StringBuilder sb, object? item, TypeMeta meta)
    {
        if (item is null)
        {
            WriteNullRow(sb, meta.Fields.Length);
            return;
        }

        for (var i = 0; i < meta.Fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            WriteValue(sb, meta.Fields[i].Get(item), meta.Fields[i].Code);
        }

        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTimeSeriesRow(StringBuilder sb, object? item, TypeMeta meta, FieldMeta? tsField, DateTime baseDate)
    {
        if (item is null)
        {
            WriteNullRow(sb, meta.Fields.Length);
            return;
        }

        for (var i = 0; i < meta.Fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            var f = meta.Fields[i];
            var v = f.Get(item);

            if (f == tsField && v is not null)
            {
                var date = v switch
                {
                    DateTime dt => dt,
                    DateTimeOffset dto => dto.UtcDateTime,
                    _ => baseDate,
                };
                sb.Append((int)(date.Date - baseDate).TotalDays);
            }
            else
            {
                WriteValueCompact(sb, v, f.Code);
            }
        }

        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteNullRow(StringBuilder sb, int fieldCount)
    {
        for (var i = 0; i < fieldCount; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            sb.Append('_');
        }

        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteValue(StringBuilder sb, object? v, char code)
    {
        if (v is null)
        {
            sb.Append('_');
            return;
        }

        switch (code)
        {
            case 'S':
                WriteEscaped(sb, v.ToString()!);
                break;
            case 'I':
                WriteInt(sb, Convert.ToInt64(v, CultureInfo.InvariantCulture));
                break;
            case 'F':
                WriteDouble(sb, Convert.ToDouble(v, CultureInfo.InvariantCulture));
                break;
            case 'D':
                WriteDecimal(sb, Convert.ToDecimal(v, CultureInfo.InvariantCulture));
                break;
            case 'B':
                sb.Append(Convert.ToBoolean(v, CultureInfo.InvariantCulture) ? '1' : '0');
                break;
            case 'T':
                WriteTimestamp(sb, v);
                break;
            default:
                WriteEscaped(sb, v.ToString()!);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteValueCompact(StringBuilder sb, object? v, char code)
    {
        if (v is null)
        {
            sb.Append('_');
            return;
        }

        switch (code)
        {
            case 'S':
                WriteEscaped(sb, v.ToString()!);
                break;
            case 'I':
                WriteInt(sb, Convert.ToInt64(v, CultureInfo.InvariantCulture));
                break;
            case 'F':
                WriteCompactDouble(sb, Convert.ToDouble(v, CultureInfo.InvariantCulture));
                break;
            case 'D':
                WriteCompactDecimal(sb, Convert.ToDecimal(v, CultureInfo.InvariantCulture));
                break;
            case 'B':
                sb.Append(Convert.ToBoolean(v, CultureInfo.InvariantCulture) ? '+' : '-');
                break;
            case 'T':
                WriteCompactTimestamp(sb, v);
                break;
            default:
                WriteEscaped(sb, v.ToString()!);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt(StringBuilder sb, long v)
    {
        Span<char> buf = stackalloc char[20];
        v.TryFormat(buf, out var len, default, CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDouble(StringBuilder sb, double v)
    {
        Span<char> buf = stackalloc char[32];
        v.TryFormat(buf, out var len, "G17", CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDecimal(StringBuilder sb, decimal v)
    {
        Span<char> buf = stackalloc char[32];
        v.TryFormat(buf, out var len, "G", CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteCompactDouble(StringBuilder sb, double v)
    {
        if (Math.Abs(v) < 1e-12)
        {
            sb.Append('0');
            return;
        }

        Span<char> buf = stackalloc char[32];
        v.TryFormat(buf, out var len, "G15", CultureInfo.InvariantCulture);
        var span = buf[..len];

        if (span.Length >= 2 && span[0] == '0' && span[1] == '.')
        {
            sb.Append(span[1..]);
        }
        else if (span.Length >= 3 && span[0] == '-' && span[1] == '0' && span[2] == '.')
        {
            sb.Append('-');
            sb.Append(span[2..]);
        }
        else
        {
            sb.Append(span);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteCompactDecimal(StringBuilder sb, decimal v)
    {
        if (Math.Abs(v) < 1e-12M)
        {
            sb.Append('0');
            return;
        }

        Span<char> buf = stackalloc char[32];
        v.TryFormat(buf, out var len, "G", CultureInfo.InvariantCulture);
        var span = buf[..len];

        if (span.Length >= 2 && span[0] == '0' && span[1] == '.')
        {
            sb.Append(span[1..]);
        }
        else if (span.Length >= 3 && span[0] == '-' && span[1] == '0' && span[2] == '.')
        {
            sb.Append('-');
            sb.Append(span[2..]);
        }
        else
        {
            sb.Append(span);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTimestamp(StringBuilder sb, object v)
    {
        Span<char> buf = stackalloc char[33];
        var dt = v switch
        {
            DateTime d => d.ToUniversalTime(),
            DateTimeOffset dto => dto.UtcDateTime,
            _ => DateTime.UtcNow,
        };
        dt.TryFormat(buf, out var len, "O", CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteCompactTimestamp(StringBuilder sb, object v)
    {
        Span<char> buf = stackalloc char[12];
        var dt = v switch
        {
            DateTime d => d,
            DateTimeOffset dto => dto.UtcDateTime,
            _ => DateTime.UtcNow,
        };
        var fmt = dt.TimeOfDay == TimeSpan.Zero ? "yyMMdd" : "yyMMddHHmmss";
        dt.TryFormat(buf, out var len, fmt, CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteEscaped(StringBuilder sb, string s)
    {
        if (s.AsSpan().IndexOfAny(EscapeChars) < 0)
        {
            sb.Append(s);
            return;
        }

        foreach (var c in s)
        {
            _ = c switch
            {
                '\\' => sb.Append("\\\\"),
                '|' => sb.Append("\\|"),
                '\n' => sb.Append("\\n"),
                '\r' => sb.Append("\\r"),
                '\t' => sb.Append("\\t"),
                '"' => sb.Append("\\\""),
                _ => sb.Append(c),
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T MapToObject<T>(IReadOnlyDictionary<string, object?> row, Schema schema)
        where T : new()
    {
        var obj = new T();
        var meta = GetMeta(typeof(T));

        foreach (var f in meta.Fields)
        {
            if (row.TryGetValue(f.Name, out var v) && v is not null && f.Set is not null)
            {
                try
                {
                    var converted = ConvertValue(v, f.Type);
                    if (converted is not null)
                    {
                        f.Set(obj, converted);
                    }
                }
                catch
                {
                    // Ignore conversion errors
                }
            }
        }

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value.GetType() == underlying)
        {
            return value;
        }

        return ConvertToType(value, underlying);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ConvertToType(object value, Type target) => target switch
    {
        _ when target == typeof(int) && value is long l => (int)l,
        _ when target == typeof(short) && value is long ls => (short)ls,
        _ when target == typeof(byte) && value is long lb => (byte)lb,
        _ when target == typeof(long) && value is int i => (long)i,
        _ when target == typeof(float) && value is double d => (float)d,
        _ when target == typeof(double) && value is decimal dec => (double)dec,
        _ when target == typeof(DateTimeOffset) && value is DateTime dt => new DateTimeOffset(dt),
        _ when target == typeof(DateOnly) && value is DateTime dt2 => DateOnly.FromDateTime(dt2),
        _ when target == typeof(TimeOnly) && value is DateTime dt3 => TimeOnly.FromDateTime(dt3),
        _ when target == typeof(string) => value.ToString(),
        _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture),
    };

    private static TypeMeta GetMeta(Type type)
    {
        lock (TypeCacheLock)
        {
            if (TypeCache.TryGetValue(type, out var m))
            {
                return m;
            }

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            var fields = new FieldMeta[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var pt = p.PropertyType;
                var ut = Nullable.GetUnderlyingType(pt) ?? pt;

                fields[i] = new FieldMeta(
                    p.Name,
                    GetCode(ut),
                    !pt.IsValueType || Nullable.GetUnderlyingType(pt) is not null,
                    ut,
                    CreateGetter(p),
                    p.CanWrite ? CreateSetter(p) : null);
            }

            var meta = new TypeMeta(fields);
            TypeCache[type] = meta;
            return meta;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char GetCode(Type t) => t switch
    {
        _ when t == typeof(string) => 'S',
        _ when t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) => 'I',
        _ when t == typeof(decimal) => 'D',
        _ when t == typeof(float) || t == typeof(double) => 'F',
        _ when t == typeof(bool) => 'B',
        _ when t == typeof(DateTime) || t == typeof(DateTimeOffset) => 'T',
        _ => 'S',
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type? GetElementType(Type t)
    {
        if (t.IsArray)
        {
            return t.GetElementType();
        }

        if (t.IsGenericType && t.GetGenericArguments().Length == 1)
        {
            return t.GetGenericArguments()[0];
        }

        foreach (var i in t.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return i.GetGenericArguments()[0];
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<object, object?> CreateGetter(PropertyInfo p)
    {
        var m = p.GetGetMethod();
        return m is null ? _ => null : obj => m.Invoke(obj, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Action<object, object?> CreateSetter(PropertyInfo p)
    {
        var m = p.GetSetMethod();
        return m is null ? (_, _) => { } : (obj, v) => m.Invoke(obj, [v]);
    }

    private sealed record TypeMeta(FieldMeta[] Fields);

    private sealed record FieldMeta(
        string Name,
        char Code,
        bool Nullable,
        Type Type,
        Func<object, object?> Get,
        Action<object, object?>? Set);
}
