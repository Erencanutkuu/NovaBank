using System.Text.Json.Serialization;

namespace NovaBank.Api.Contracts.Responses;

// Başarılı JSON cevaplarının ortak gövdesi. Hatalar bunu kullanmaz; ProblemDetails ile döner.
public sealed record ApiResponse<T>(
    T Data,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message, // null ise JSON'a hiç yazılmaz
    string TraceId);
