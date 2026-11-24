using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Contracts.Responses;
using NovaBank.Application.Common;
using NovaBank.Application.Transactions;

namespace NovaBank.Api.Controllers;

[Route("api/accounts/{accountId:guid}")]
public sealed class TransactionsController(
    TransactionService transactions,
    IValidator<MoveMoneyRequest> moveValidator,
    IValidator<ListTransactionsRequest> listValidator) : ApiControllerBase
{
    [HttpPost("deposit")]
    [ProducesResponseType<ApiResponse<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deposit(
        Guid accountId, MoveMoneyRequest request, CancellationToken cancellationToken)
    {
        await moveValidator.ValidateAndThrowAsync(request, cancellationToken);

        return OkResponse(await transactions.DepositAsync(accountId, request, cancellationToken), "Para yatırıldı.");
    }

    [HttpPost("withdraw")]
    [ProducesResponseType<ApiResponse<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(
        Guid accountId, MoveMoneyRequest request, CancellationToken cancellationToken)
    {
        await moveValidator.ValidateAndThrowAsync(request, cancellationToken);

        return OkResponse(await transactions.WithdrawAsync(accountId, request, cancellationToken), "Para çekildi.");
    }

    [HttpGet("transactions")]
    [ProducesResponseType<ApiResponse<PagedResult<TransactionDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid accountId, [FromQuery] ListTransactionsRequest request, CancellationToken cancellationToken)
    {
        await listValidator.ValidateAndThrowAsync(request, cancellationToken);

        return OkResponse(await transactions.ListAsync(accountId, request, cancellationToken));
    }
}
