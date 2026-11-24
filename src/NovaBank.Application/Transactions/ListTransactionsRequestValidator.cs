using FluentValidation;

namespace NovaBank.Application.Transactions;

public sealed class ListTransactionsRequestValidator : AbstractValidator<ListTransactionsRequest>
{
    public ListTransactionsRequestValidator()
    {
        // Üst sınır, atlanacak satır sayısının int'e sığmasını da sağlar.
        RuleFor(x => x.Page)
            .InclusiveBetween(1, 1_000_000)
            .WithMessage("Sayfa 1 ile 1.000.000 arasında olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
    }
}
