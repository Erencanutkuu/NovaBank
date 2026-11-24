using Microsoft.EntityFrameworkCore.Storage;
using NovaBank.Application.Common;

namespace NovaBank.Infrastructure.Persistence;

public sealed class UnitOfWork(NovaBankDbContext context) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
        new UnitOfWorkTransaction(context, await context.Database.BeginTransactionAsync(cancellationToken));

    private sealed class UnitOfWorkTransaction(
        NovaBankDbContext context, IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Onaylanmamış işlem burada geri alınır.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
