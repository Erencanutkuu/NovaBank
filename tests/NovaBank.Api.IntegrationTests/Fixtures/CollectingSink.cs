using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace NovaBank.Api.IntegrationTests.Fixtures;

// Testte yazılan logları bellekte toplar.
public sealed class CollectingSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => _events.ToArray();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}
