namespace NovaBank.Application.Customers;

public sealed record CreateCustomerRequest(string FirstName, string LastName, string Email);
