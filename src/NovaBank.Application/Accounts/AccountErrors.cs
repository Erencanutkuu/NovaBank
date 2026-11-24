using NovaBank.Application.Constants;
using NovaBank.Application.Exceptions;

namespace NovaBank.Application.Accounts;

public static class AccountErrors
{
    public static BusinessException NotFound() => new(ErrorCodes.AccountNotFound, "Hesap bulunamadı.");

    public static BusinessException Closed() => new(ErrorCodes.AccountClosed, "Hesap kapalı.");

    public static BusinessException HasBalance() =>
        new(ErrorCodes.AccountHasBalance, "Bakiyesi olan hesap kapatılamaz.");

    public static BusinessException InsufficientBalance() =>
        new(ErrorCodes.InsufficientBalance, "Bakiye yetersiz.");
}
