using NovaBank.Application.Accounts;
using NovaBank.Application.Common;
using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Transactions;

public sealed class TransactionService(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<TransactionDto> DepositAsync(
        Guid accountId, MoveMoneyRequest request, CancellationToken cancellationToken) =>
        MoveAsync(accountId, (account, now) => account.Deposit(request.Amount, now), cancellationToken);

    public Task<TransactionDto> WithdrawAsync(
        Guid accountId, MoveMoneyRequest request, CancellationToken cancellationToken) =>
        MoveAsync(accountId, (account, now) => account.Withdraw(request.Amount, now), cancellationToken);

    public async Task<PagedResult<TransactionDto>> ListAsync(
        Guid accountId, ListTransactionsRequest request, CancellationToken cancellationToken)
    {
        if (await accounts.FindAsync(accountId, cancellationToken) is null)
        {
            throw AccountErrors.NotFound();
        }

        var items = await transactions.ListAsync(
            accountId, (request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);
        var totalCount = await transactions.CountAsync(accountId, cancellationToken);

        return new PagedResult<TransactionDto>(
            items.Select(TransactionDto.From).ToList(), request.Page, request.PageSize, totalCount);
    }

    private async Task<TransactionDto> MoveAsync(
        Guid accountId,
        Func<Account, DateTimeOffset, AccountTransaction?> move,
        CancellationToken cancellationToken)
    {
        await using var work = await unitOfWork.BeginAsync(cancellationToken);

        // Satır kilitlenir: aynı hesaptaki diğer işlemler bu iş bitene kadar bekler ve güncel bakiyeyi görür.
        var account = await accounts.FindForUpdateAsync(accountId, cancellationToken)
            ?? throw AccountErrors.NotFound();

        if (account.Status == AccountStatus.Closed)
        {
            throw AccountErrors.Closed();
        }

        var transaction = move(account, timeProvider.GetUtcNowInMicroseconds())
            ?? throw AccountErrors.InsufficientBalance();

        // Bakiye ve hareket birlikte yazılır.
        transactions.Add(transaction);
        await work.CommitAsync(cancellationToken);

        return TransactionDto.From(transaction);
    }
}
