namespace NovaBank.E2E.Tests;

// Her test temiz bir tarayıcı oturumuyla başlar. Yakalanmamış JavaScript hatası testi bozar.
[Collection(AppCollection.Name)]
public abstract class PageTest(AppFixture app) : IAsyncLifetime
{
    private readonly List<string> _pageErrors = [];
    private IBrowserContext _context = null!;

    protected AppFixture App { get; } = app;
    protected IPage Page { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _context = await App.Browser.NewContextAsync(new()
        {
            BaseURL = App.BaseUrl,
            Locale = "tr-TR",
            TimezoneId = "Europe/Istanbul",
            ViewportSize = new() { Width = 1280, Height = 900 }
        });
        _context.SetDefaultTimeout(8_000);
        Page = await _context.NewPageAsync();
        Page.PageError += (_, error) => _pageErrors.Add(error);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        Assert.Empty(_pageErrors);
    }

    protected ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator Dialog => Page.GetByRole(AriaRole.Dialog);

    protected ILocator DialogButton(string name) =>
        Dialog.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    // Ekran yatay kaymamalı.
    protected async Task AssertNoHorizontalScrollAsync() =>
        Assert.True(await Page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
}
