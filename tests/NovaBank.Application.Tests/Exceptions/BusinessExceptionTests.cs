using NovaBank.Application.Constants;
using NovaBank.Application.Exceptions;

namespace NovaBank.Application.Tests.Exceptions;

public sealed class BusinessExceptionTests
{
    [Fact]
    public void Constructor_KeepsCodeAndMessage()
    {
        var exception = new BusinessException(ErrorCodes.InsufficientBalance, "Bakiye yetersiz.");

        Assert.Equal("7555", exception.ErrorCode);
        Assert.Equal("Bakiye yetersiz.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingCode(string? errorCode)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new BusinessException(errorCode!, "Mesaj"));

        Assert.Equal("errorCode", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingMessage(string? message)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new BusinessException("7555", message!));

        Assert.Equal("message", exception.ParamName);
    }
}
