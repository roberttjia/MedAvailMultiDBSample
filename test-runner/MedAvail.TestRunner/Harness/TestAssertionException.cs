using System;
using System.Diagnostics.CodeAnalysis;

namespace MedAvail.TestRunner.Harness;

/// <summary>
/// Thrown by a test body to signal a controlled failure (vs. an unexpected
/// exception). The harness records the message as the failure reason.
/// </summary>
public sealed class TestAssertionException : Exception
{
    public TestAssertionException(string message) : base(message) { }
}

/// <summary>Thrown to signal a test should be skipped (e.g. missing config).</summary>
public sealed class TestSkippedException : Exception
{
    public TestSkippedException(string message) : base(message) { }
}

public static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new TestAssertionException(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
            throw new TestAssertionException($"{message} (expected: {expected}, actual: {actual})");
    }

    [DoesNotReturn]
    public static void Skip(string message) => throw new TestSkippedException(message);
}
