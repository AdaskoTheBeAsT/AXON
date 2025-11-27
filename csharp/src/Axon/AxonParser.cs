#pragma warning disable CC0001 // Use var - nint requires explicit type
#pragma warning disable CC0105 // Use var - nint requires explicit type
#pragma warning disable CC0120 // Switch default clause - not needed for known cases

using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Axon;

/// <summary>
/// Ultra-fast AXON parser using SIMD, unsafe code, and zero-allocation patterns.
/// </summary>
public static class AxonParser
{
    private static readonly SearchValues<char> LineBreak = SearchValues.Create("\r\n");
    private static readonly SearchValues<char> Delimiters = SearchValues.Create("|\"\\");

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    public static (List<Schema> Schemas, List<DataBlock> DataBlocks) Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var schemas = new List<Schema>(4);
        var dataBlocks = new List<DataBlock>(4);
        var span = input.AsSpan();
        nint pos = 0;
        nint len = span.Length;

        ref char start = ref MemoryMarshal.GetReference(span);

        while (pos < len)
        {
            SkipToLine(ref start, len, ref pos, out var line);
            if (line.IsEmpty || line[0] != '`')
            {
                continue;
            }

            ParseBlock(ref start, len, ref pos, line, out var schema, out var dataBlock);
            schemas.Add(schema);
            dataBlocks.Add(dataBlock);
        }

        return (schemas, dataBlocks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    public static void ParseWithCallback(string input, RowCallback callback)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(callback);

        var span = input.AsSpan();
        nint pos = 0;
        nint len = span.Length;

        ref char start = ref MemoryMarshal.GetReference(span);

        while (pos < len)
        {
            SkipToLine(ref start, len, ref pos, out var line);
            if (line.IsEmpty || line[0] != '`')
            {
                continue;
            }

            var schema = ParseHeader(line);
            nint rowIdx = 0;

            while (pos < len)
            {
                SkipToLine(ref start, len, ref pos, out var rowLine);
                if (rowLine.IsEmpty)
                {
                    continue;
                }

                if (rowLine[0] == '~')
                {
                    break;
                }

                callback(schema, (int)rowIdx++, rowLine);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    public static int CountFields(ReadOnlySpan<char> row)
    {
        if (row.IsEmpty)
        {
            return 0;
        }

        nint count = 1;
        var inStr = false;
        var esc = false;
        nint i = 0;
        nint len = row.Length;

        ref char r = ref MemoryMarshal.GetReference(row);

        while (i < len)
        {
            var c = Unsafe.Add(ref r, i);

            if (esc)
            {
                esc = false;
                i++;
                continue;
            }

            switch (c)
            {
                case '\\':
                    esc = true;
                    break;
                case '"':
                    inStr = !inStr;
                    break;
                case '|' when !inStr:
                    count++;
                    break;
            }

            i++;
        }

        return (int)count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetFieldAt(ReadOnlySpan<char> row, int fieldIndex)
    {
        nint cur = 0;
        nint start = 0;
        var inStr = false;
        var esc = false;
        nint len = row.Length;

        ref char r = ref MemoryMarshal.GetReference(row);

        for (nint i = 0; i < len; i++)
        {
            var c = Unsafe.Add(ref r, i);

            if (esc)
            {
                esc = false;
                continue;
            }

            switch (c)
            {
                case '\\':
                    esc = true;
                    break;
                case '"':
                    inStr = !inStr;
                    break;
                case '|' when !inStr:
                    if (cur == fieldIndex)
                    {
                        return row[(int)start..(int)i];
                    }

                    cur++;
                    start = i + 1;
                    break;
            }
        }

        return cur == fieldIndex ? row[(int)start..] : [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private static void SkipToLine(ref char start, nint len, ref nint pos, out ReadOnlySpan<char> line)
    {
        var remaining = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref start, pos), (int)(len - pos));
        var lineEnd = remaining.IndexOfAny(LineBreak);

        if (lineEnd < 0)
        {
            line = remaining.Trim();
            pos = len;
            return;
        }

        line = remaining[..lineEnd].Trim();
        pos += lineEnd + 1;

        if (pos < len && remaining[lineEnd] == '\r' && Unsafe.Add(ref start, pos) == '\n')
        {
            pos++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static void ParseBlock(ref char start, nint len, ref nint pos, ReadOnlySpan<char> header, out Schema schema, out DataBlock dataBlock)
    {
        schema = ParseHeader(header);
        dataBlock = new DataBlock(schema);

        while (pos < len)
        {
            SkipToLine(ref start, len, ref pos, out var line);
            if (line.IsEmpty)
            {
                continue;
            }

            if (line[0] == '~')
            {
                break;
            }

            dataBlock.AddRow(ParseRowSimd(line, schema));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static Schema ParseHeader(ReadOnlySpan<char> header)
    {
        var h = header[1..];
        var bStart = h.IndexOf('[');
        var name = h[..bStart].ToString();

        var pStart = h.IndexOf('(');
        var cStart = h.IndexOf('{');
        var ultra = cStart >= 0 && (pStart < 0 || cStart < pStart);
        var fStart = ultra ? cStart : pStart;
        var fEnd = ultra ? h.IndexOf('}') : h.IndexOf(')');

        if (fStart < 0 || fEnd < 0)
        {
            ThrowBadHeader(header);
        }

        var fieldsSpan = h[(fStart + 1)..fEnd];
        var fields = new List<FieldDefinition>(8);
        nint start = 0;
        nint fLen = fieldsSpan.Length;

        while (start < fLen)
        {
            var remaining = fieldsSpan[(int)start..];
            var comma = remaining.IndexOf(',');
            nint end = comma < 0 ? fLen : start + comma;
            var fd = fieldsSpan[(int)start..(int)end].Trim();

            if (!fd.IsEmpty)
            {
                ParseFieldDef(fd, ultra, fields);
            }

            start = end + 1;
        }

        return new Schema(name, fields);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBadHeader(ReadOnlySpan<char> header) =>
        throw new AxonParseException($"Bad header: {header.ToString()}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseFieldDef(ReadOnlySpan<char> fd, bool ultra, List<FieldDefinition> fields)
    {
        var colon = fd.IndexOf(':');
        if (colon > 0)
        {
            var fn = fd[..colon].ToString();
            var ts = fd[(colon + 1)..];
            var nullable = ts[^1] == '?';
            if (nullable)
            {
                ts = ts[..^1];
            }

            fields.Add(new FieldDefinition(fn, ParseType(ts), nullable));
        }
        else if (ultra)
        {
            fields.Add(new FieldDefinition(fd.ToString(), AxonType.String, true));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AxonType ParseType(ReadOnlySpan<char> t) => t.Length == 1
        ? t[0] switch
        {
            'S' or 's' => AxonType.String,
            'I' or 'i' => AxonType.Integer,
            'F' or 'f' => AxonType.Float,
            'D' or 'd' => AxonType.Decimal,
            'B' or 'b' => AxonType.Boolean,
            'T' or 't' => AxonType.Timestamp,
            _ => ThrowUnknownType(t),
        }
        : ThrowUnknownType(t);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static AxonType ThrowUnknownType(ReadOnlySpan<char> t) =>
        throw new AxonParseException($"Unknown type: {t.ToString()}");

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static object?[] ParseRowSimd(ReadOnlySpan<char> line, Schema schema)
    {
        var fields = schema.Fields;
        var row = new object?[fields.Length];
        var buf = ArrayPool<char>.Shared.Rent(line.Length + 64);

        try
        {
            ParseRowInner(line, fields, row, buf);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buf);
        }

        return row;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static void ParseRowInner(ReadOnlySpan<char> line, FieldDefinition[] fields, object?[] row, char[] buf)
    {
        nint fi = 0;
        nint bp = 0;
        nint i = 0;
        var inStr = false;
        var esc = false;
        nint lineLen = line.Length;
        nint fieldsLen = fields.Length;

        ref char lineRef = ref MemoryMarshal.GetReference(line);
        ref char bufRef = ref MemoryMarshal.GetArrayDataReference(buf);

        while (i < lineLen && fi < fieldsLen)
        {
            if (!inStr && !esc && TryFastCopy(line, buf, ref i, ref bp))
            {
                continue;
            }

            var c = Unsafe.Add(ref lineRef, i++);

            if (esc)
            {
                Unsafe.Add(ref bufRef, bp++) = c switch { 'n' => '\n', 't' => '\t', 'r' => '\r', _ => c };
                esc = false;
                continue;
            }

            ProcessChar(c, fields, row, ref bufRef, ref fi, ref bp, ref inStr, ref esc);
        }

        if (fi < fieldsLen)
        {
            row[fi] = ParseValue(buf.AsSpan(0, (int)bp), fields[fi]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryFastCopy(ReadOnlySpan<char> line, char[] buf, ref nint i, ref nint bp)
    {
        var remaining = line[(int)i..];
        var idx = remaining.IndexOfAny(Delimiters);

        if (idx > 0)
        {
            remaining[..idx].CopyTo(buf.AsSpan((int)bp));
            bp += idx;
            i += idx;
            return true;
        }

        if (idx < 0)
        {
            remaining.CopyTo(buf.AsSpan((int)bp));
            bp += remaining.Length;
            i = line.Length;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessChar(char c, FieldDefinition[] fields, object?[] row, ref char bufRef, ref nint fi, ref nint bp, ref bool inStr, ref bool esc)
    {
        switch (c)
        {
            case '\\':
                esc = true;
                break;
            case '"':
                inStr = !inStr;
                break;
            case '|' when !inStr:
                row[fi] = ParseValue(MemoryMarshal.CreateReadOnlySpan(ref bufRef, (int)bp), fields[fi]);
                fi++;
                bp = 0;
                break;
            default:
                Unsafe.Add(ref bufRef, bp++) = c;
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ParseValue(ReadOnlySpan<char> v, FieldDefinition f)
    {
        if (v.Length == 1 && v[0] == '_')
        {
            return null;
        }

        if (v.IsEmpty)
        {
            return f.Type == AxonType.String ? string.Empty : null;
        }

        return f.Type switch
        {
            AxonType.String => v.ToString(),
            AxonType.Integer => ParseLongFast(v),
            AxonType.Float => double.Parse(v, CultureInfo.InvariantCulture),
            AxonType.Decimal => decimal.Parse(v, CultureInfo.InvariantCulture),
            AxonType.Boolean => v[0] is '1' or '+',
            AxonType.Timestamp => DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => v.ToString(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    private static long ParseLongFast(ReadOnlySpan<char> s)
    {
        if (s.IsEmpty)
        {
            return 0;
        }

        ref char r = ref MemoryMarshal.GetReference(s);
        var neg = r == '-';
        nint i = neg ? 1 : 0;
        nint len = s.Length;
        var result = 0L;

        // Process 4 digits at a time
        while (i + 4 <= len)
        {
            result = (result * 10000)
                + ((Unsafe.Add(ref r, i) - '0') * 1000)
                + ((Unsafe.Add(ref r, i + 1) - '0') * 100)
                + ((Unsafe.Add(ref r, i + 2) - '0') * 10)
                + (Unsafe.Add(ref r, i + 3) - '0');
            i += 4;
        }

        while (i < len)
        {
            result = (result * 10) + (Unsafe.Add(ref r, i++) - '0');
        }

        return neg ? -result : result;
    }
}
