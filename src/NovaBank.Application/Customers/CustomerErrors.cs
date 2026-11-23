using NovaBank.Application.Constants;
using NovaBank.Application.Exceptions;

namespace NovaBank.Application.Customers;

public static class CustomerErrors
{
    public static BusinessException NotFound() => new(ErrorCodes.CustomerNotFound, "Müşteri bulunamadı.");

    public static BusinessException EmailAlreadyExists() =>
        new(ErrorCodes.EmailAlreadyExists, "Bu e-posta adresi kayıtlı.");
}
