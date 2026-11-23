using NovaBank.Domain.Accounts;

namespace NovaBank.Domain.Tests;

public sealed class IbanTests
{
    [Fact]
    public void Generate_CreatesValidTurkishIbanWithFictionalBankCode()
    {
        var iban = Iban.Generate().Value;

        Assert.Matches("^TR[0-9]{2}999990[0-9]{16}$", iban);
        Assert.True(Iban.IsValid(iban));
    }

    [Fact]
    public void Generate_CreatesDifferentNumbers()
    {
        Assert.Equal(1000, Enumerable.Range(0, 1000).Select(_ => Iban.Generate().Value).Distinct().Count());
    }

    [Fact]
    public void Generate_AlwaysHasTwoValidCheckDigits()
    {
        Assert.All(Enumerable.Range(0, 1000), _ => Assert.True(Iban.IsValid(Iban.Generate().Value)));
    }

    // Standartlarda örnek olarak kullanılan TR IBAN.
    [Fact]
    public void IsValid_AcceptsKnownIban()
    {
        Assert.True(Iban.IsValid("TR330006100519786457841326"));
    }

    [Theory]
    [InlineData("TR330006100519786457841327")] // tek rakam yanlış
    [InlineData("TR330006100519786457841362")] // iki rakam yer değiştirmiş
    [InlineData("TR33000610051978645784132")]  // 25 karakter
    [InlineData("DE330006100519786457841326")]
    [InlineData("tr330006100519786457841326")]
    [InlineData("TR33000610051978645784132A")]
    [InlineData("TR٣٣0006100519786457841326")] // ASCII olmayan rakam
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsWrongIban(string? value)
    {
        Assert.False(Iban.IsValid(value));
    }
}
