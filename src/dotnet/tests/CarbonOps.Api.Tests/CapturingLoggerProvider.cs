using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CarbonOps.Api.Tests;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> entries = new();
    private readonly AsyncLocal<CapturedLogScope?> currentScope = new();

    public IReadOnlyCollection<CapturedLogEntry> Entries => entries.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturingLogger(categoryName, entries, currentScope);
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly string category;
        private readonly ConcurrentQueue<CapturedLogEntry> entries;
        private readonly AsyncLocal<CapturedLogScope?> currentScope;

        public CapturingLogger(
            string category,
            ConcurrentQueue<CapturedLogEntry> entries,
            AsyncLocal<CapturedLogScope?> currentScope)
        {
            this.category = category;
            this.entries = entries;
            this.currentScope = currentScope;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            var parentScope = currentScope.Value;
            currentScope.Value = new CapturedLogScope(CaptureProperties(state), parentScope);

            return new ScopeRestore(() => currentScope.Value = parentScope);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var scope in EnumerateScopes(currentScope.Value))
            {
                foreach (var property in scope.Properties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            foreach (var property in CaptureProperties(state))
            {
                properties[property.Key] = property.Value;
            }

            entries.Enqueue(
                new CapturedLogEntry(
                    category,
                    logLevel,
                    formatter(state, exception),
                    properties));
        }

        private static IEnumerable<CapturedLogScope> EnumerateScopes(CapturedLogScope? scope)
        {
            if (scope is null)
            {
                yield break;
            }

            foreach (var parentScope in EnumerateScopes(scope.Parent))
            {
                yield return parentScope;
            }

            yield return scope;
        }
    }

    private sealed class ScopeRestore : IDisposable
    {
        private readonly Action restore;

        public ScopeRestore(Action restore)
        {
            this.restore = restore;
        }

        public void Dispose()
        {
            restore();
        }
    }

    private sealed record CapturedLogScope(
        IReadOnlyDictionary<string, object?> Properties,
        CapturedLogScope? Parent);

    private static IReadOnlyDictionary<string, object?> CaptureProperties<TState>(TState state)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (state is IEnumerable<KeyValuePair<string, object?>> structuredState)
        {
            foreach (var property in structuredState)
            {
                if (property.Key != "{OriginalFormat}")
                {
                    properties[property.Key] = property.Value;
                }
            }
        }

        return properties;
    }
}

internal sealed record CapturedLogEntry(
    string Category,
    LogLevel LogLevel,
    string Message,
    IReadOnlyDictionary<string, object?> Properties)
{
    public T GetProperty<T>(string name)
    {
        var value = Assert.Contains(name, Properties);
        return Assert.IsType<T>(value);
    }

    public override string ToString()
    {
        var properties = string.Join(
            " ",
            Properties
                .OrderBy(property => property.Key, StringComparer.Ordinal)
                .Select(property => $"{property.Key}={property.Value}"));

        return $"{Category} {LogLevel} {Message} {properties}";
    }
}
