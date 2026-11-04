using System.Net;
using System.Text.Json;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

// Başarılı cevap sözleşmesi: { data, message?, traceId }
public sealed class ApiResponseTests : IClassFixture<NovaBankApiFactory>
{
    private readonly HttpClient _client;

    public ApiResponseTests(NovaBankApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    [Fact]
    public async Task Ok_ReturnsDataAndTraceId()
    {
        var response = await _client.GetAsync("/test/single");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ReadJsonAsync(response);
        Assert.Equal(1, root.GetProperty("data").GetProperty("id").GetInt32());
        Assert.Equal("Test", root.GetProperty("data").GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Ok_WithoutMessage_OmitsMessageKey()
    {
        var root = await ReadJsonAsync(await _client.GetAsync("/test/single"));

        Assert.False(root.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Ok_WithMessage_ReturnsMessage()
    {
        var root = await ReadJsonAsync(await _client.GetAsync("/test/with-message"));

        Assert.Equal("İşlem tamam.", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Ok_EmptyList_ReturnsEmptyArray()
    {
        var root = await ReadJsonAsync(await _client.GetAsync("/test/empty-list"));

        var data = root.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task Ok_KeepsNullPropertiesInsideData()
    {
        var root = await ReadJsonAsync(await _client.GetAsync("/test/null-inside-data"));

        Assert.True(root.GetProperty("data").TryGetProperty("nickname", out var nickname));
        Assert.Equal(JsonValueKind.Null, nickname.ValueKind);
    }

    [Fact]
    public async Task Ok_ReturnsApplicationJson()
    {
        var response = await _client.GetAsync("/test/single");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Created_Returns201WithLocationAndBody()
    {
        var response = await _client.PostAsync("/test/items", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var root = await ReadJsonAsync(response);
        Assert.Equal(42, root.GetProperty("data").GetProperty("id").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Created_ReturnsMessageWithTurkishCharacters()
    {
        var root = await ReadJsonAsync(await _client.PostAsync("/test/items", content: null));

        Assert.Equal("Hesap açıldı.", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Created_LocationPointsToExistingResource()
    {
        var created = await _client.PostAsync("/test/items", content: null);

        var response = await _client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await ReadJsonAsync(response);
        Assert.Equal(42, root.GetProperty("data").GetProperty("id").GetInt32());
    }
}
