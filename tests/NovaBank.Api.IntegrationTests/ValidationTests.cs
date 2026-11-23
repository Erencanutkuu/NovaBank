using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Api.IntegrationTests.Fixtures;
using NovaBank.Api.IntegrationTests.TestControllers;

namespace NovaBank.Api.IntegrationTests;

// Doğrulama hatası sözleşmesi: 400 + ProblemDetails + errors { alan: [mesaj] }
public sealed class ValidationTests : IClassFixture<NovaBankApiFactory>
{
    private readonly HttpClient _client;
    private readonly TestWorkCounter _counter;

    public ValidationTests(NovaBankApiFactory factory)
    {
        _client = factory.CreateClient();
        _counter = factory.Services.GetRequiredService<TestWorkCounter>();
    }

    private async Task<(HttpResponseMessage Response, JsonElement Body)> PostAsync(string json)
    {
        var response = await _client.PostAsync(
            "/test/validation", new StringContent(json, Encoding.UTF8, "application/json"));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (response, document.RootElement.Clone());
    }

    private static string[] Messages(JsonElement body, string field) =>
        body.GetProperty("errors").GetProperty(field).EnumerateArray().Select(x => x.GetString()!).ToArray();

    private static void AssertValidationContract(HttpResponseMessage response, JsonElement body)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Gönderilen bilgiler geçersiz.", body.GetProperty("title").GetString());
        Assert.Equal("/test/validation", body.GetProperty("instance").GetString());
        Assert.Equal("7000", body.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task ValidRequest_Continues()
    {
        var before = _counter.Count;

        var (response, body) = await PostAsync("""{"fullName":"Eren","id":1}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Eren", body.GetProperty("data").GetProperty("fullName").GetString());
        Assert.Equal(before + 1, _counter.Count);
    }

    [Fact]
    public async Task InvalidRequest_ReturnsAllFieldErrors()
    {
        var (response, body) = await PostAsync("""{"fullName":"","id":0}""");

        AssertValidationContract(response, body);
        Assert.Equal(["Ad zorunludur."], Messages(body, "fullName"));
        Assert.Equal(["Id pozitif olmalı."], Messages(body, "id"));
    }

    [Fact]
    public async Task InvalidRequest_DoesNotRunWork()
    {
        var before = _counter.Count;

        await PostAsync("""{"fullName":"","id":0}""");

        Assert.Equal(before, _counter.Count);
    }

    [Fact]
    public async Task AsyncRule_IsSupported()
    {
        var (response, body) = await PostAsync("""{"fullName":"kayitli","id":1}""");

        AssertValidationContract(response, body);
        Assert.Equal(["Bu ad kayıtlı."], Messages(body, "fullName"));
    }

    [Fact]
    public async Task NestedFields_UseCamelCasePath()
    {
        var (response, body) = await PostAsync(
            """{"fullName":"Eren","id":1,"address":{"city":""},"items":[{"name":""}]}""");

        AssertValidationContract(response, body);
        Assert.Equal(["Şehir zorunludur."], Messages(body, "address.city"));
        Assert.Equal(["Kalem adı zorunludur."], Messages(body, "items[0].name"));
    }

    [Fact]
    public async Task FieldNames_DoNotDependOnTurkishCulture()
    {
        var previous = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var (_, body) = await PostAsync("""{"fullName":"Eren","id":0}""");
            Assert.Equal(["Id pozitif olmalı."], Messages(body, "id"));
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = previous;
        }
    }

    [Fact]
    public async Task MissingField_IsReportedByValidator()
    {
        var (response, body) = await PostAsync("""{"id":1}""");

        AssertValidationContract(response, body);
        Assert.Equal(["Ad zorunludur."], Messages(body, "fullName"));
    }

    [Fact]
    public async Task WrongJsonType_UsesSameContractWithoutInternalDetails()
    {
        var (response, body) = await PostAsync("""{"fullName":"Eren","id":"abc"}""");

        AssertValidationContract(response, body);
        Assert.Equal(["Geçersiz değer."], Messages(body, "id"));
        Assert.DoesNotContain("System.", body.GetRawText());
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public async Task EmptyBody_UsesSameContract(string json)
    {
        var (response, body) = await PostAsync(json);

        AssertValidationContract(response, body);
        Assert.Equal(["İstek gövdesi zorunludur."], Messages(body, "body"));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{bozuk}")]
    public async Task MalformedJson_UsesSameContract(string json)
    {
        var (response, body) = await PostAsync(json);

        AssertValidationContract(response, body);
        Assert.Equal(["Geçersiz değer."], Messages(body, "body"));
        Assert.DoesNotContain("LineNumber", body.GetRawText());
    }
}
