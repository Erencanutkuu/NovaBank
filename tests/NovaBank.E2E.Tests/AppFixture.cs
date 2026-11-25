using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaBank.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NovaBank.E2E.Tests;

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "app";
}

// Gerçek uygulamayı başlatır: geçici PostgreSQL + API süreci + derlenmiş frontend + Chromium.
// Geliştirme veritabanına ve 5080/5173 portlarına dokunmaz.
public sealed class AppFixture : IAsyncLifetime
{
    private static readonly string Root = FindRoot();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private readonly List<Process> _processes = [];
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public HttpClient Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<NovaBankDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        await using (var context = new NovaBankDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var apiUrl = $"http://127.0.0.1:{GetFreePort()}";
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        Start("dotnet", [Path.Combine(Root, $"src/NovaBank.Api/bin/{configuration}/net8.0/NovaBank.Api.dll")],
            Path.Combine(Root, "src/NovaBank.Api"),
            new()
            {
                // Production: ayar eksikse geliştirme veritabanına düşmek yerine başlamaz.
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["ASPNETCORE_URLS"] = apiUrl,
                ["ConnectionStrings__NovaBank"] = _postgres.GetConnectionString()
            });

        var frontend = Path.Combine(Root, "frontend");
        var vite = Path.Combine(frontend, "node_modules/vite/bin/vite.js");
        Assert.True(File.Exists(vite), "Önce frontend klasöründe 'npm ci' çalıştır.");
        await RunAsync("npm", ["run", "build"], frontend);

        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        Start("node", [vite, "preview", "--host", "127.0.0.1", "--port", $"{port}", "--strictPort"],
            frontend, new() { ["NOVABANK_API"] = apiUrl });

        Api = new HttpClient { BaseAddress = new Uri(apiUrl) };
        await WaitAsync($"{apiUrl}/health/ready");
        await WaitAsync(BaseUrl);

        // Tarayıcı kuruluysa hemen döner.
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync();
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        foreach (var process in _processes)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.Dispose();
        }

        await _postgres.DisposeAsync();
    }

    private void Start(string file, string[] arguments, string directory, Dictionary<string, string> environment)
    {
        var process = Process.Start(CreateInfo(file, arguments, directory, environment))!;
        _processes.Add(process);

        // Okunmayan çıktı tamponu dolarsa süreç durur.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, e) => Console.Error.WriteLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private static async Task RunAsync(string file, string[] arguments, string directory)
    {
        using var process = Process.Start(CreateInfo(file, arguments, directory, []))!;
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, $"{file} {string.Join(' ', arguments)} başarısız:\n{output}");
    }

    private static ProcessStartInfo CreateInfo(
        string file, string[] arguments, string directory, Dictionary<string, string> environment)
    {
        var info = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var (key, value) in environment)
        {
            info.Environment[key] = value;
        }

        return info;
    }

    private static async Task WaitAsync(string url)
    {
        using var client = new HttpClient();
        for (var attempt = 0; attempt < 150; attempt++)
        {
            try
            {
                if ((await client.GetAsync(url)).StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Henüz açılmadı.
            }

            await Task.Delay(200);
        }

        Assert.Fail($"{url} 30 saniyede açılmadı.");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "NovaBank.sln")))
        {
            directory = directory.Parent!;
        }

        return directory.FullName;
    }

    // Aşağıdakiler testin başlangıç verisini ekranı kullanmadan, doğrudan API ile hazırlar.

    public async Task<Guid> CreateCustomerAsync(string firstName = "Eren", string lastName = "Utku", string? email = null)
    {
        var data = await PostAsync("/api/customers",
            new { firstName, lastName, email = email ?? $"{Guid.NewGuid():N}@example.com" });
        return data.GetProperty("id").GetGuid();
    }

    public async Task<Guid> OpenAccountAsync(Guid customerId, string currency = "TRY", decimal balance = 0)
    {
        var data = await PostAsync($"/api/customers/{customerId}/accounts", new { currency });
        var id = data.GetProperty("id").GetGuid();
        if (balance > 0)
        {
            await DepositAsync(id, balance);
        }

        return id;
    }

    public Task DepositAsync(Guid accountId, decimal amount) =>
        PostAsync($"/api/accounts/{accountId}/deposit", new { amount });

    public async Task<decimal[]> GetAmountsAsync(Guid accountId)
    {
        var json = await Api.GetFromJsonAsync<JsonElement>($"/api/accounts/{accountId}/transactions?pageSize=100");
        return json.GetProperty("data").GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("amount").GetDecimal()).ToArray();
    }

    private async Task<JsonElement> PostAsync(string path, object body)
    {
        var response = await Api.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }
}
