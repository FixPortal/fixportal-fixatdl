using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FixPortal.FixAtdl.Tests.TestDoubles;

/// <summary>
/// An in-memory <see cref="ILoggerFactory"/> that records every log entry so tests can assert
/// the library wired logging through. Captures the logger category, level, and rendered message.
/// </summary>
public sealed class RecordingLoggerFactory : ILoggerFactory
{
    public sealed record Entry(string Category, LogLevel Level, string Message);

    public ConcurrentQueue<Entry> Records { get; } = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Records);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class RecordingLogger(string category, ConcurrentQueue<Entry> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => sink.Enqueue(new Entry(category, logLevel, formatter(state, exception)));
    }
}
