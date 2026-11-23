namespace NovaBank.Domain.Customers;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Customer() { } // EF Core ve Create metodu kullanır.

    public static Customer Create(string firstName, string lastName, string email, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName.Trim(),
        LastName = lastName.Trim(),
        Email = NormalizeEmail(email),
        CreatedAt = now
    };

    // Invariant: Türkçe kültürde "I" harfi "ı" olmasın.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
