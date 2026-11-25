using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Contracts.Responses;
using NovaBank.Application.Common;
using NovaBank.Application.Customers;

namespace NovaBank.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController(
    CustomerService customers,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<PageRequest> pageValidator) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CustomerDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var customer = await customers.CreateAsync(request, cancellationToken);

        return CreatedResponse(nameof(Get), new { id = customer.Id }, customer, "Müşteri oluşturuldu.");
    }

    // Login gelince yetkiye bağlanmalı: tüm müşterilerin kişisel verisini listeler.
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<CustomerDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        await pageValidator.ValidateAndThrowAsync(request, cancellationToken);

        return OkResponse(await customers.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<CustomerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await customers.GetAsync(id, cancellationToken));
}
