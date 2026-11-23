namespace NovaBank.Application.Common;

public static class TimeProviderExtensions
{
    // PostgreSQL mikrosaniye saklar, .NET 100 nanosaniye. Kırpmazsak POST ve GET cevapları farklı zaman gösterir.
    public static DateTimeOffset GetUtcNowInMicroseconds(this TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
