using Microsoft.AspNetCore.Mvc;
using NovaBank.Api.Contracts.Responses;

namespace NovaBank.Api.Controllers;

// Controller'lar bu sınıftan türeyip başarılı cevapları aşağıdaki yardımcılarla üretir.
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // 200 OK + { data, message?, traceId }
    protected OkObjectResult OkResponse<T>(T data, string? message = null) =>
        Ok(CreateResponse(data, message));

    // 201 Created + Location header + { data, message?, traceId }
    protected CreatedAtActionResult CreatedResponse<T>(
        string actionName,
        object? routeValues,
        T data,
        string? message = null) =>
        CreatedAtAction(actionName, routeValues, CreateResponse(data, message));

    // traceId, GlobalExceptionHandler ile aynı kaynaktan gelir.
    private ApiResponse<T> CreateResponse<T>(T data, string? message) =>
        new(data, message, HttpContext.TraceIdentifier);
}
