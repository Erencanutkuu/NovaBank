using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Api.IntegrationTests.Fixtures;
using Serilog.Core;
using Serilog.Events;

namespace NovaBank.Api.IntegrationTests;

public sealed class LoggingTests : IDisposable
{
    private readonly CollectingSink _sink = new();
    private readonly NovaBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient(string? minimumLevel = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(_sink));
            if (minimumLevel is not null)
            {
                builder.UseSetting("Serilog:MinimumLevel:Default", minimumLevel);
            }
        }).CreateClient();

    private static object? Value(LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value) ? (value as ScalarValue)?.Value : null;

    private List<LogEvent> RequestLogs() =>
        _sink.Events.Where(e => e.Properties.ContainsKey("Elapsed")).ToList();

    private List<LogEvent> ErrorCodeLogs() =>
        _sink.Events.Where(e => e.Properties.ContainsKey("ErrorCode")).ToList();

    [Fact]
    public async Task Request_IsLoggedWithMethodPathStatusAndRequestId()
    {
        var response = await CreateClient().GetAsync("/test/single");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var log = Assert.Single(RequestLogs());
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.Equal("GET", Value(log, "RequestMethod"));
        Assert.Equal("/test/single", Value(log, "RequestPath"));
        Assert.Equal(200, Value(log, "StatusCode"));
        Assert.Equal(json.RootElement.GetProperty("traceId").GetString(), Value(log, "RequestId"));
    }

    [Fact]
    public async Task UnexpectedException_IsLoggedOnceWithRequestId()
    {
        var response = await CreateClient().GetAsync("/test/unexpected-error");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var log = Assert.Single(_sink.Events, e => e.Exception is not null);
        Assert.Equal(LogEventLevel.Error, log.Level);
        Assert.Equal("gizli-ic-ayrinti", log.Exception!.Message);
        Assert.Equal(json.RootElement.GetProperty("traceId").GetString(), Value(log, "RequestId"));
        Assert.Equal(500, Value(Assert.Single(RequestLogs()), "StatusCode"));
    }

    [Fact]
    public async Task BusinessException_IsLoggedAsInformationWithoutStackTrace()
    {
        await CreateClient().GetAsync("/test/business-error");

        Assert.DoesNotContain(_sink.Events, e => e.Exception is not null || e.Level >= LogEventLevel.Error);
        var log = Assert.Single(ErrorCodeLogs());
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.Equal("7555", Value(log, "ErrorCode"));
        Assert.Equal(409, Value(log, "StatusCode"));
    }

    [Fact]
    public async Task ValidationException_IsLoggedAsInformationWithoutStackTrace()
    {
        await CreateClient().PostAsync("/test/validation",
            new StringContent("""{"fullName":"","id":1}""", Encoding.UTF8, "application/json"));

        Assert.DoesNotContain(_sink.Events, e => e.Exception is not null || e.Level >= LogEventLevel.Error);
        Assert.Equal("7000", Value(Assert.Single(ErrorCodeLogs()), "ErrorCode"));
    }

    [Fact]
    public async Task QueryStringBodyAndHeaders_AreNotLogged()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/test/validation?token=gizli-sorgu")
        {
            Content = new StringContent("""{"fullName":"gizli-govde","id":0}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", "Bearer gizli-baslik");

        await CreateClient().SendAsync(request);

        Assert.Single(RequestLogs());
        foreach (var log in _sink.Events)
        {
            var text = log.RenderMessage() + string.Join(" ", log.Properties.Values) + log.Exception;
            Assert.DoesNotContain("gizli-", text);
        }
    }

    [Fact]
    public async Task FrameworkInformationLogs_AreFilteredByConfiguration()
    {
        await CreateClient().GetAsync("/test/single");

        Assert.NotEmpty(_sink.Events);
        Assert.DoesNotContain(_sink.Events, e =>
            e.Level < LogEventLevel.Warning &&
            Value(e, "SourceContext") is string source && source.StartsWith("Microsoft.AspNetCore."));
    }

    [Theory]
    [InlineData("Information", 1)]
    [InlineData("Error", 0)]
    public async Task MinimumLevel_ComesFromConfiguration(string level, int expectedRequestLogs)
    {
        await CreateClient(level).GetAsync("/test/single");

        Assert.Equal(expectedRequestLogs, RequestLogs().Count);
    }
}
