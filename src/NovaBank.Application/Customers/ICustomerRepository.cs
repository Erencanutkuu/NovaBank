using NovaBank.Domain.Customers;

namespace NovaBank.Application.Customers;

public interface ICustomerRepository
{
    // E-posta başka bir istekle aynı anda kaydedildiyse EmailAlreadyExists kodlu BusinessException fırlatır.
    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task<Customer?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
}
