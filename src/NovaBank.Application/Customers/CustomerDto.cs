using NovaBank.Domain.Customers;

namespace NovaBank.Application.Customers;

public sealed record CustomerDto(Guid Id, string FirstName, string LastName, string Email, DateTimeOffset CreatedAt)
{
    public static CustomerDto From(Customer customer) =>
        new(customer.Id, customer.FirstName, customer.LastName, customer.Email, customer.CreatedAt);
}
