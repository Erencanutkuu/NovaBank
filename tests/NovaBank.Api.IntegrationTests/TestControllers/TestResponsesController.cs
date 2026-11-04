using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Controllers;
using NovaBank.Application.Constants;
using NovaBank.Application.Exceptions;

namespace NovaBank.Api.IntegrationTests.TestControllers;

// Yalnızca testlerde var; production API'de bu adresler bulunmaz.
[Route("test")]
public sealed class TestResponsesController : ApiControllerBase
{
    [HttpGet("business-error")]
    public IActionResult BusinessError() =>
        throw new BusinessException(ErrorCodes.InsufficientBalance, "Bakiye yetersiz.");

    [HttpGet("single")]
    public IActionResult Single() => OkResponse(new { id = 1, name = "Test" });

    [HttpGet("with-message")]
    public IActionResult WithMessage() => OkResponse(new { id = 1 }, "İşlem tamam.");

    [HttpGet("empty-list")]
    public IActionResult EmptyList() => OkResponse(Array.Empty<int>());

    [HttpGet("null-inside-data")]
    public IActionResult NullInsideData() => OkResponse(new { id = 1, nickname = (string?)null });

    [HttpGet("items/{id:int}")]
    public IActionResult GetItem(int id) => OkResponse(new { id });

    [HttpPost("items")]
    public IActionResult CreateItem() =>
        CreatedResponse(nameof(GetItem), new { id = 42 }, new { id = 42 }, "Hesap açıldı.");
}
