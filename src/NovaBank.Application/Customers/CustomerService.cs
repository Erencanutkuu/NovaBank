using NovaBank.Application.Common;
using NovaBank.Domain.Customers;

namespace NovaBank.Application.Customers;

public sealed class CustomerService(ICustomerRepository customers, TimeProvider timeProvider)
{
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(
            request.FirstName, request.LastName, request.Email, timeProvider.GetUtcNowInMicroseconds());

        // Olağan durum burada yakalanır; aynı anda gelen istekleri veritabanındaki benzersiz index yakalar.
        if (await customers.EmailExistsAsync(customer.Email, cancellationToken))
        {
            throw CustomerErrors.EmailAlreadyExists();
        }

        await customers.AddAsync(customer, cancellationToken);

        return CustomerDto.From(customer);
    }

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(id, cancellationToken) ?? throw CustomerErrors.NotFound();

        return CustomerDto.From(customer);
    }
}
