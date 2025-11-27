using System.Text;
using System.Text.RegularExpressions;

namespace Axon.Performance;

/// <summary>
/// Token counter utility for comparing format efficiency.
/// Uses approximations based on common tokenizer patterns (GPT-4/o200k_base style).
/// </summary>
public static partial class TokenCounter
{
    // Common tokenizer patterns based on GPT tokenizers
    // Average tokens per character varies by content:
    // - Plain English text: ~4 chars/token
    // - Code/structured data: ~3 chars/token
    // - JSON with many special chars: ~2.5 chars/token
    // - Numbers: ~2 chars/token

    /// <summary>
    /// Estimates token count using patterns similar to GPT tokenizers.
    /// This is an approximation - actual tokens vary by tokenizer.
    /// </summary>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var tokens = 0;
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            // Whitespace - often its own token or merged
            if (char.IsWhiteSpace(c))
            {
                // Multiple spaces often merge
                var spaceCount = 0;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    spaceCount++;
                    i++;
                }

                // Roughly 1-2 tokens for whitespace sequences
                tokens += spaceCount <= 4 ? 1 : (spaceCount + 3) / 4;
                continue;
            }

            // Numbers - typically 1-3 digits per token
            if (char.IsDigit(c))
            {
                var numLength = 0;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                {
                    numLength++;
                    i++;
                }

                // ~2-3 digits per token
                tokens += Math.Max(1, (numLength + 2) / 3);
                continue;
            }

            // Letters - words typically 3-6 chars per token
            if (char.IsLetter(c))
            {
                var wordLength = 0;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '-'))
                {
                    wordLength++;
                    i++;
                }

                // Common words: 1 token, longer words: multiple tokens
                tokens += wordLength <= 5 ? 1 : (wordLength + 3) / 4;
                continue;
            }

            // Special characters - often 1 token each for JSON/structured data
            // But some pairs merge (like ": " or ", ")
            if (i + 1 < text.Length)
            {
                var pair = text.Substring(i, 2);
                if (pair is ": " or ", " or "\\n" or "\\t" or "\\r" or "\": " or "\",")
                {
                    tokens++;
                    i += 2;
                    continue;
                }
            }

            // Single special char
            tokens++;
            i++;
        }

        return tokens;
    }

    /// <summary>
    /// More accurate estimation using regex patterns similar to BPE tokenizers.
    /// </summary>
    public static int EstimateTokensBpe(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Split by common BPE patterns
        var patterns = BpePattern();
        var matches = patterns.Matches(text);

        var tokens = 0;
        foreach (Match match in matches)
        {
            var segment = match.Value;

            // Estimate tokens for this segment
            if (segment.Length <= 4)
            {
                tokens++;
            }
            else if (segment.All(char.IsDigit))
            {
                // Numbers: ~2-3 digits per token
                tokens += (segment.Length + 2) / 3;
            }
            else if (segment.All(char.IsLetter))
            {
                // Words: common words 1 token, longer split
                tokens += segment.Length <= 6 ? 1 : (segment.Length + 3) / 4;
            }
            else
            {
                // Mixed: roughly 3-4 chars per token
                tokens += Math.Max(1, (segment.Length + 2) / 3);
            }
        }

        return Math.Max(1, tokens);
    }

    /// <summary>
    /// Counts bytes (for raw size comparison).
    /// </summary>
    public static int CountBytes(string text)
    {
        return Encoding.UTF8.GetByteCount(text);
    }

    /// <summary>
    /// Counts characters.
    /// </summary>
    public static int CountChars(string text)
    {
        return text.Length;
    }

    /// <summary>
    /// Counts lines.
    /// </summary>
    public static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var lines = 1;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                lines++;
            }
        }

        return lines;
    }

    /// <summary>
    /// Gets comprehensive metrics for a text.
    /// </summary>
    public static TokenMetrics GetMetrics(string text)
    {
        return new TokenMetrics
        {
            Bytes = CountBytes(text),
            Characters = CountChars(text),
            Lines = CountLines(text),
            EstimatedTokens = EstimateTokens(text),
            EstimatedTokensBpe = EstimateTokensBpe(text),
        };
    }

    /// <summary>
    /// Compares metrics between two formats.
    /// </summary>
    public static FormatComparison Compare(string format1, string format2, string format1Name, string format2Name)
    {
        var metrics1 = GetMetrics(format1);
        var metrics2 = GetMetrics(format2);

        return new FormatComparison
        {
            Format1Name = format1Name,
            Format2Name = format2Name,
            Format1Metrics = metrics1,
            Format2Metrics = metrics2,
            BytesSavings = (double)(metrics2.Bytes - metrics1.Bytes) / metrics2.Bytes * 100,
            TokenSavings = (double)(metrics2.EstimatedTokens - metrics1.EstimatedTokens) / metrics2.EstimatedTokens * 100,
        };
    }

    // BPE-style regex pattern for tokenization (simplified for .NET 10 compatibility)
    [GeneratedRegex(@"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex BpePattern();
}

/// <summary>
/// Token metrics for a text.
/// </summary>
public record TokenMetrics
{
    public int Bytes { get; init; }

    public int Characters { get; init; }

    public int Lines { get; init; }

    public int EstimatedTokens { get; init; }

    public int EstimatedTokensBpe { get; init; }

    public double BytesPerToken => EstimatedTokens > 0 ? (double)Bytes / EstimatedTokens : 0;

    public double CharsPerToken => EstimatedTokens > 0 ? (double)Characters / EstimatedTokens : 0;
}

/// <summary>
/// Comparison between two format outputs.
/// </summary>
public record FormatComparison
{
    public required string Format1Name { get; init; }

    public required string Format2Name { get; init; }

    public required TokenMetrics Format1Metrics { get; init; }

    public required TokenMetrics Format2Metrics { get; init; }

    public double BytesSavings { get; init; }

    public double TokenSavings { get; init; }
}
