using FluentValidation;
using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Transactions;

public sealed class MoveMoneyRequestValidator : AbstractValidator<MoveMoneyRequest>
{
    public MoveMoneyRequestValidator()
    {
        RuleFor(x => x.Amount)
            .Must(Account.IsValidTransactionAmount)
            .WithMessage("Tutar 0'dan büyük, en çok 1.000.000 ve en çok 2 ondalıklı olmalıdır.");
    }
}
