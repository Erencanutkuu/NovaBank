using FluentValidation;

namespace NovaBank.Application.Customers;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // Bir alanda ilk hatada dur; alan başına tek mesaj döner.

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en çok 100 karakter olabilir.")
            .Must(HasNoControlCharacter).WithMessage("Ad geçersiz karakter içeriyor.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en çok 100 karakter olabilir.")
            .Must(HasNoControlCharacter).WithMessage("Soyad geçersiz karakter içeriyor.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(254).WithMessage("E-posta en çok 254 karakter olabilir.")
            .EmailAddress().WithMessage("E-posta biçimi geçersiz.")
            .Must(email => !email.Trim().Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            .WithMessage("E-posta biçimi geçersiz.");
    }

    // PostgreSQL metin alanı NUL karakterini kabul etmez.
    private static bool HasNoControlCharacter(string value) => !value.Any(char.IsControl);
}
