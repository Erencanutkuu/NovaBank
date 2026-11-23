using System.Net;
using System.Text.Json;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

// Hata kodu -> HTTP durumu eşleme tablosu.
public sealed class ExceptionHandlingTests(NovaBankApiFactory factory) : IClassFixture<NovaBankApiFactory>
{
    [Theory]
    [InlineData("/test/business-error/7231", HttpStatusCode.NotFound, "7231", "Test mesajı.")]
    [InlineData("/test/business-error/7555", HttpStatusCode.Conflict, "7555", "Test mesajı.")]
    [InlineData("/test/business-error/7556", HttpStatusCode.Conflict, "7556", "Test mesajı.")]
    [InlineData("/test/business-error/1234", HttpStatusCode.BadRequest, "1234", "Test mesajı.")]
    [InlineData("/test/unexpected-error", HttpStatusCode.InternalServerError, "9000", "Beklenmeyen bir hata oluştu.")]
    public async Task Exception_IsMappedToProblemDetails(
        string path, HttpStatusCode status, string errorCode, string title)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal((int)status, root.GetProperty("status").GetInt32());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.Equal(errorCode, root.GetProperty("errorCode").GetString());
        Assert.Equal(path, root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }
}
