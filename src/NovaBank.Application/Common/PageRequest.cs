namespace NovaBank.Application.Common;

// Sayfalı liste isteği.
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    // Skip adı verilmez: sorgu parametresi değildir, hesaplanır.
    public int GetSkip() => (Page - 1) * PageSize;
}
