using System.Globalization;
using NovaBank.Domain.Customers;

namespace NovaBank.Domain.Tests;

public sealed class CustomerTests
{
    [Fact]
    public void Create_TrimsFieldsAndLowercasesEmail()
    {
        var now = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

        var customer = Customer.Create(" Işıl ", " Çınar ", " Isil@Example.COM ", now);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Işıl", customer.FirstName);
        Assert.Equal("Çınar", customer.LastName);
        Assert.Equal("isil@example.com", customer.Email);
        Assert.Equal(now, customer.CreatedAt);
    }

    [Fact]
    public void NormalizeEmail_DoesNotDependOnTurkishCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            Assert.Equal("info@example.com", Customer.NormalizeEmail("INFO@EXAMPLE.COM"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
