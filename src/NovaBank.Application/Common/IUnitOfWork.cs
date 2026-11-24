namespace NovaBank.Application.Common;

// Birden çok değişikliği tek veritabanı işleminde toplar: ya hepsi yazılır ya hiçbiri.
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken cancellationToken);
}

// CommitAsync çağrılmadan bırakılırsa değişiklikler geri alınır.
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    // Değişiklikleri kaydeder ve işlemi onaylar.
    Task CommitAsync(CancellationToken cancellationToken);
}
