using System;

namespace Axon;

/// <summary>
/// Exception thrown when parsing AXON format fails.
/// </summary>
public sealed class AxonParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AxonParseException"/> class.
    /// </summary>
    public AxonParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AxonParseException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message describing the parsing failure.</param>
    public AxonParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AxonParseException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message describing the parsing failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AxonParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
