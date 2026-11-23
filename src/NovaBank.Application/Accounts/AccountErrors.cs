using NovaBank.Application.Constants;
using NovaBank.Application.Exceptions;

namespace NovaBank.Application.Accounts;

public static class AccountErrors
{
    public static BusinessException NotFound() => new(ErrorCodes.AccountNotFound, "Hesap bulunamadı.");

    public static BusinessException Closed() => new(ErrorCodes.AccountClosed, "Hesap kapalı.");
}
