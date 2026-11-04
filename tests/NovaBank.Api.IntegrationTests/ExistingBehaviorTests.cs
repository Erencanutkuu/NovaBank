using System.Net;
using System.Text.Json;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

// Bu iş sırasında DEĞİŞMEMESİ gereken mevcut davranışları sabitler.
public sealed class ExistingBehaviorTests : IClassFixture<NovaBankApiFactory>
{
    private readonly HttpClient _client;

    public ExistingBehaviorTests(NovaBankApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsPlainTextHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BusinessException_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/test/business-error");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Bakiye yetersiz.", root.GetProperty("title").GetString());
        Assert.Equal("7555", root.GetProperty("errorCode").GetString());
        Assert.Equal("/test/business-error", root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task UnknownRoute_Returns404WithoutApiResponseBody()
    {
        var response = await _client.GetAsync("/test/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"data\"", body);
    }
}
