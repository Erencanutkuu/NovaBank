using System.Security.Cryptography;

namespace NovaBank.Domain.Accounts;

// TR IBAN: "TR" + 2 kontrol rakamı + 5 haneli banka kodu + "0" + 16 haneli hesap numarası = 26 karakter.
public sealed record Iban
{
    private const string CountryCode = "TR";
    private const string BankCode = "99999"; // Kurgusal; gerçek bir bankaya ait değildir.
    private const string ReservedDigit = "0";
    private const int AccountNumberLength = 16;

    public const int Length = 26;

    public string Value { get; }

    private Iban(string value) => Value = value;

    public static Iban Generate()
    {
        var accountNumber = RandomNumberGenerator.GetString("0123456789", AccountNumberLength);
        var bban = BankCode + ReservedDigit + accountNumber;

        // Kontrol rakamları "00" kabul edilip kalan bulunur; 98 - kalan, kontrol rakamlarını verir.
        var checkDigits = 98 - Mod97(bban + CountryCode + "00");

        return new Iban($"{CountryCode}{checkDigits:00}{bban}");
    }

    // Veritabanından veya kullanıcıdan gelen metni IBAN'a çevirir; geçersizse hata verir.
    public static Iban Parse(string value) =>
        IsValid(value) ? new Iban(value) : throw new FormatException("IBAN geçersiz.");

    public static bool IsValid(string? value) =>
        value is { Length: Length } &&
        value.StartsWith(CountryCode, StringComparison.Ordinal) &&
        value.Skip(CountryCode.Length).All(char.IsAsciiDigit) &&
        Mod97(value[4..] + value[..4]) == 1; // İlk dört karakter sona taşınır.

    // Sayı 64 bite sığmadığı için kalan, rakam rakam hesaplanır. Harfler: A=10, B=11, ... Z=35.
    private static int Mod97(string value)
    {
        var remainder = 0;

        foreach (var character in value)
        {
            remainder = char.IsAsciiDigit(character)
                ? (remainder * 10 + (character - '0')) % 97
                : (remainder * 100 + (character - 'A' + 10)) % 97;
        }

        return remainder;
    }
}
