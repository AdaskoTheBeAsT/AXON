# AXON Parser - C# Implementation

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![Performance](https://img.shields.io/badge/Parse-244μs-brightgreen?style=flat)
![Tokens](https://img.shields.io/badge/Tokens-−74%25%20vs%20JSON-blue?style=flat)
![Deserialize](https://img.shields.io/badge/Deserialize-1.46x%20faster-orange?style=flat)
![SIMD](https://img.shields.io/badge/SIMD-AVX2%2FSSE2-purple?style=flat)

Ultra-fast .NET 10 parser using SIMD, unsafe code, and zero-allocation patterns.

```text
┌─────────────────────────────────────────────────────────────────┐
│  AXON Format                                                    │
│  `User[3](id:I,name:S,active:B)                                 │
│  1|Alice|1                                                      │
│  2|Bob|0                                                        │
│  3|Carol|1                                                      │
│  ~                                                              │
└─────────────────────────────────────────────────────────────────┘
```

## Benchmarks

### Token Efficiency (vs JSON baseline)

| Payload | AXON | TOON | Winner |
|---------|------|------|--------|
| Small (100 employees) | -58.3% | -58.6% | TOON |
| Medium (100 orders) | -55.4% | -54.4% | **AXON** |
| Large (500 repos) | -40.2% | -37.8% | **AXON** |
| XL Time-Series (1000 metrics) | **-74.3%** | -50.2% | **AXON** |

### Parsing Speed (1000 employees, 500 iterations, Release build)

| Format | Time | Per-Op |
|--------|------|--------|
| **AXON** | **122ms** | **244µs** |

### Serialization Speed (1000 employees, 500 iterations, Release build)

| Format | Time | Per-Op | vs JSON |
|--------|------|--------|---------|
| **AXON** | 207ms | 414µs | **1.15x faster** |
| JSON | 238ms | 476µs | baseline |
| TOON | 2178ms | 4356µs | 9.2x slower |

### Deserialization Speed (1000 employees, 500 iterations, Release build)

| Format | Time | Per-Op | vs JSON |
|--------|------|--------|---------|
| **AXON** | 255ms | 510µs | **1.46x faster** |
| JSON | 373ms | 746µs | baseline |
| TOON | N/A | N/A | no decoder |

## Format

Compact backtick/tilde format for minimal size:

```text
`User[3](id:I,name:S,active:B)
1|Alice|1
2|Bob|0
3|Carol|1
~
```

## Usage

```csharp
var (schemas, dataBlocks) = AxonParser.Parse(axonData);

foreach (var row in dataBlocks[0].Rows)
{
    Console.WriteLine($"ID: {row["id"]}, Name: {row["name"]}");
}
```

## Zero-Allocation Streaming

```csharp
AxonParser.ParseWithCallback(axonData, (schema, rowIndex, rowSpan) =>
{
    var id = AxonParser.GetFieldAt(rowSpan, 0);
    var name = AxonParser.GetFieldAt(rowSpan, 1);
});
```

## Serialization

```csharp
var axon = AxonSerializer.Serialize(users, "User");

// Time-series optimized (74% smaller than JSON)
var axon = AxonSerializer.SerializeTimeSeries(metrics, "Metric");
```

## Type Mappings

| Type | C# | Example |
|------|-----|---------|
| `I` | `long` | `42` |
| `S` | `string` | `Alice` |
| `F` | `double` | `3.14` |
| `D` | `decimal` | `99.99` |
| `B` | `bool` | `1`/`0` |
| `T` | `DateTime` | `2024-11-23T10:30:00Z` |
| `?` | nullable | `_` = null |

## Escape Sequences

`\"` `\\` `\n` `\t` `\r` `\|`

## Performance Optimization Techniques

### 1. `SearchValues<char>` - SIMD Delimiter Scanning

```csharp
private static readonly SearchValues<char> Delimiters = SearchValues.Create("|\"\\");

// Usage: SIMD-accelerated search for any delimiter
var idx = remaining.IndexOfAny(Delimiters);
```

**Why it's fast:** Uses CPU vector instructions (SSE2/AVX2) to scan 16-32 characters simultaneously instead of checking one-by-one. Finding `|`, `"`, or `\` in a 1000-char string becomes ~32x faster.

### 2. `nint` - Native-Sized Integers

```csharp
nint pos = 0;
nint len = span.Length;
```

**Why it's fast:** `nint` matches the CPU's native word size (64-bit on x64). Eliminates sign-extension instructions when indexing into arrays/spans. The JIT generates cleaner assembly with fewer mov/movsxd instructions.

### 3. `MemoryMarshal.GetReference` - Direct Span Access

```csharp
ref char start = ref MemoryMarshal.GetReference(span);
```

**Why it's fast:** Gets a direct managed pointer to the first element, bypassing the span's bounds-checking infrastructure. Combined with `Unsafe.Add`, allows pointer-style access without safety overhead.

### 4. `Unsafe.Add` - Pointer Arithmetic

```csharp
var c = Unsafe.Add(ref lineRef, i++);
Unsafe.Add(ref bufRef, bp++) = c;
```

**Why it's fast:** Direct pointer arithmetic without bounds checks. The JIT emits a simple `lea` or `add` instruction instead of the bounds-check branch that `span[i]` would require.

### 5. `[SkipLocalsInit]` - Skip Stack Zeroing

```csharp
[SkipLocalsInit]
private static void ParseRowInner(...)
{
    nint fi = 0;  // Not zeroed by runtime
    nint bp = 0;
    // ...
}
```

**Why it's fast:** By default, .NET zeros all local variables on method entry for safety. This attribute skips that zeroing when you're immediately assigning values anyway. Saves ~1 cycle per local variable.

### 6. `MemoryMarshal.GetArrayDataReference` - Array Element Access

```csharp
ref char bufRef = ref MemoryMarshal.GetArrayDataReference(buf);
```

**Why it's fast:** Returns a reference to element 0 without bounds checking. Works even on empty arrays (returns ref to where element 0 *would* be). Enables unsafe-style array access.

### 7. `ArrayPool<char>.Shared` - Buffer Reuse

```csharp
var buf = ArrayPool<char>.Shared.Rent(line.Length + 64);
try {
    // use buffer
} finally {
    ArrayPool<char>.Shared.Return(buf);
}
```

**Why it's fast:** Reuses heap allocations across parse calls. Instead of allocating/GC'ing a new char[] for every row, we rent from a pool. Reduces GC pressure significantly in tight loops.

### 8. `FrozenDictionary` - Immutable O(1) Lookup

```csharp
// In Schema constructor
_fieldIndex = fields.Select((f, i) => (f.Name, i))
    .ToFrozenDictionary(x => x.Name, x => x.i);
```

**Why it's fast:** Optimized for read-heavy scenarios. Pre-computes perfect hash function at creation time. No locking, no resize checks. Field name → index lookup is true O(1).

### 9. `readonly record struct` - Stack Allocation

```csharp
public readonly record struct FieldDefinition(string Name, AxonType Type, bool IsNullable = false);
```

**Why it's fast:** Value type lives on the stack, not the heap. No GC allocation when creating field definitions. Copying is cheap (just a few bytes). Better cache locality.

### 10. `[MethodImpl]` Attributes - JIT Hints

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
private static object? ParseValue(...)
```

- **AggressiveInlining:** Forces the JIT to inline the method, eliminating call overhead
- **AggressiveOptimization:** Tells JIT to spend more time optimizing (better codegen, loop unrolling)
- **NoInlining:** (for throw helpers) Keeps exception paths out of hot code

### 11. Fast Integer Parsing - 4 Digits at a Time

```csharp
while (i + 4 <= len)
{
    result = (result * 10000)
        + ((Unsafe.Add(ref r, i) - '0') * 1000)
        + ((Unsafe.Add(ref r, i + 1) - '0') * 100)
        + ((Unsafe.Add(ref r, i + 2) - '0') * 10)
        + (Unsafe.Add(ref r, i + 3) - '0');
    i += 4;
}
```

**Why it's fast:** Processes 4 digits per iteration instead of 1. Reduces loop overhead by 4x. The multiplications by constants (1000, 100, 10) are optimized by the JIT into shifts and adds.

### 12. Branch-Free Boolean Parsing

```csharp
return v[0] is '1' or '+';
```

**Why it's fast:** Pattern matching compiles to a simple comparison without branching. No `if/else` chain, no `bool.Parse()` overhead.

### 13. `ReadOnlySpan<char>` Throughout

```csharp
private static void SkipToLine(ref char start, nint len, ref nint pos, out ReadOnlySpan<char> line)
```

**Why it's fast:** Spans are stack-only, zero-allocation views into existing memory. Slicing (`span[1..10]`) creates a new span without copying data. No string allocations until final value conversion.

## Run Benchmarks

```bash
cd test/performance/Axon.PerformanceTest
dotnet run -c Release -- --tokens    # Token efficiency report
dotnet run -c Release -- --quick     # Quick performance test
dotnet run -c Release -- --benchmark # Full BenchmarkDotNet run
```
