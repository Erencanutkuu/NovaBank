using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Contracts.Responses;
using NovaBank.Application.Accounts;

namespace NovaBank.Api.Controllers;

[Route("api")]
public sealed class AccountsController(
    AccountService accounts,
    IValidator<OpenAccountRequest> openValidator) : ApiControllerBase
{
    [HttpPost("customers/{customerId:guid}/accounts")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Open(
        Guid customerId, OpenAccountRequest request, CancellationToken cancellationToken)
    {
        await openValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await accounts.OpenAsync(customerId, request, cancellationToken);

        return CreatedResponse(nameof(Get), new { id = account.Id }, account, "Hesap açıldı.");
    }

    [HttpGet("customers/{customerId:guid}/accounts")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AccountDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid customerId, CancellationToken cancellationToken) =>
        OkResponse(await accounts.ListAsync(customerId, cancellationToken));

    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await accounts.GetAsync(id, cancellationToken));

    [HttpPost("accounts/{id:guid}/close")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await accounts.CloseAsync(id, cancellationToken), "Hesap kapatıldı.");
}
