namespace NovaBank.Application.Transactions;

public sealed record ListTransactionsRequest(int Page = 1, int PageSize = 20);
