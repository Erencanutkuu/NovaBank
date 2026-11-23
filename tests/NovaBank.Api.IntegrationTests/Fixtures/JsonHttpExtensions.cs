using System.Text;
using System.Text.Json;

namespace NovaBank.Api.IntegrationTests.Fixtures;

public static class JsonHttpExtensions
{
    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string path, string json) =>
        client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("data");

    public static async Task AssertProblemAsync(this HttpResponseMessage response, int status, string errorCode)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(errorCode, (await response.ReadJsonAsync()).GetProperty("errorCode").GetString());
    }
}
