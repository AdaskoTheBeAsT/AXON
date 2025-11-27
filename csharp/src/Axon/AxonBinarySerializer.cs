using System.Reflection;
using System.Text;

namespace Axon;

/// <summary>
/// AXON Binary serializer for maximum speed (not LLM-readable, but useful for storage/transmission).
/// </summary>
public static class AxonBinarySerializer
{
    private const byte Version = 1;

    /// <summary>
    /// Serializes to compact binary format.
    /// </summary>
    /// <typeparam name="T">The type of items to serialize.</typeparam>
    /// <param name="items">The items to serialize.</param>
    /// <returns>Binary representation of the data.</returns>
    public static byte[] SerializeBinary<T>(IEnumerable<T> items)
        where T : class
    {
        using var ms = new MemoryStream(4096);
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write(Version);

        var list = items.ToList();
        var type = typeof(T);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        writer.Write(type.Name);
        writer.Write((byte)props.Length);
        foreach (var prop in props)
        {
            writer.Write(prop.Name);
            writer.Write(GetBinaryTypeCode(prop.PropertyType));
        }

        writer.Write(list.Count);

        foreach (var item in list)
        {
            foreach (var prop in props)
            {
                var value = prop.GetValue(item);
                WriteBinaryValue(writer, value, prop.PropertyType);
            }
        }

        return ms.ToArray();
    }

    private static byte GetBinaryTypeCode(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            return 1;
        }

        if (underlying == typeof(int))
        {
            return 2;
        }

        if (underlying == typeof(long))
        {
            return 3;
        }

        if (underlying == typeof(double))
        {
            return 4;
        }

        if (underlying == typeof(float))
        {
            return 5;
        }

        if (underlying == typeof(bool))
        {
            return 6;
        }

        if (underlying == typeof(DateTime))
        {
            return 7;
        }

        if (underlying == typeof(decimal))
        {
            return 8;
        }

        return 1;
    }

    private static void WriteBinaryValue(BinaryWriter writer, object? value, Type type)
    {
        if (value is null)
        {
            writer.Write(true);
            return;
        }

        writer.Write(false);

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            writer.Write((string)value);
        }
        else if (underlying == typeof(int))
        {
            writer.Write((int)value);
        }
        else if (underlying == typeof(long))
        {
            writer.Write((long)value);
        }
        else if (underlying == typeof(double))
        {
            writer.Write((double)value);
        }
        else if (underlying == typeof(float))
        {
            writer.Write((float)value);
        }
        else if (underlying == typeof(bool))
        {
            writer.Write((bool)value);
        }
        else if (underlying == typeof(DateTime))
        {
            writer.Write(((DateTime)value).ToBinary());
        }
        else if (underlying == typeof(decimal))
        {
            writer.Write((decimal)value);
        }
        else
        {
            writer.Write(value.ToString() ?? string.Empty);
        }
    }
}
