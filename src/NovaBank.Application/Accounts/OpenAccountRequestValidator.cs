using FluentValidation;
using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Accounts;

public sealed class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    // Enum.TryParse kullanılmaz: "0" veya "TRY,USD" gibi değerleri de kabul eder.
    private static readonly string[] Currencies = Enum.GetNames<Currency>();

    public OpenAccountRequestValidator()
    {
        RuleFor(x => x.Currency)
            .Must(currency => Currencies.Contains(currency))
            .WithMessage("Para birimi TRY, USD veya EUR olmalıdır.");
    }
}
