namespace NovaBank.E2E.Tests;

public sealed class CustomersPageTests(AppFixture app) : PageTest(app)
{
    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.com";

    private async Task FillAsync(string firstName, string lastName, string email)
    {
        await Dialog.GetByLabel("Ad", new() { Exact = true }).FillAsync(firstName);
        await Dialog.GetByLabel("Soyad").FillAsync(lastName);
        await Dialog.GetByLabel("E-posta").FillAsync(email);
        await DialogButton("Müşteri ekle").ClickAsync();
    }

    [Fact]
    public async Task List_ShowsNewestCustomerAndOpensIt()
    {
        var email = UniqueEmail();
        await App.CreateCustomerAsync("Işıl", "Çınar", email);

        await Page.GotoAsync("/");

        var row = Page.GetByRole(AriaRole.Link).Filter(new() { HasText = email });
        await Expect(row).ToContainTextAsync("Işıl Çınar");
        await Expect(row).ToContainTextAsync("IÇ"); // Türkçe baş harf: ı -> I
        await row.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Işıl Çınar");
    }

    [Fact]
    public async Task Add_CreatesCustomerAndOpensIt()
    {
        await Page.GotoAsync("/");
        await Button("Müşteri ekle").ClickAsync();

        await FillAsync("Deniz", "Yıldız", UniqueEmail());

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Deniz Yıldız");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Müşteri oluşturuldu.");
        await Expect(Page.GetByText("Henüz hesap yok")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Add_WithExistingEmail_ShowsErrorAndKeepsForm()
    {
        var email = UniqueEmail();
        await App.CreateCustomerAsync(email: email);
        await Page.GotoAsync("/");
        await Button("Müşteri ekle").ClickAsync();

        await FillAsync("Deniz", "Yıldız", email);

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("e-posta");
        await Expect(Dialog.GetByLabel("Soyad")).ToHaveValueAsync("Yıldız");
    }

    [Fact]
    public async Task Add_WithEmptyFields_ShowsErrorsNextToFields()
    {
        await Page.GotoAsync("/");
        await Button("Müşteri ekle").ClickAsync();

        await FillAsync("", " ", "gecersiz");

        await Expect(Dialog.GetByLabel("Ad", new() { Exact = true })).ToHaveAccessibleDescriptionAsync("Ad zorunludur.");
        await Expect(Dialog.GetByLabel("Soyad")).ToHaveAccessibleDescriptionAsync("Soyad zorunludur.");
        await Expect(Dialog.GetByLabel("E-posta")).ToHaveAccessibleDescriptionAsync("E-posta biçimi geçersiz.");
    }

    [Fact]
    public async Task Add_CanBeClosedWithEscapeAndStartsEmptyAgain()
    {
        await Page.GotoAsync("/");
        await Button("Müşteri ekle").ClickAsync();
        await Dialog.GetByLabel("Soyad").FillAsync("Yarım");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Dialog).ToHaveCountAsync(0);
        await Button("Müşteri ekle").ClickAsync();
        await Expect(Dialog.GetByLabel("Soyad")).ToHaveValueAsync("");
    }

    [Fact]
    public async Task List_PagesThroughCustomers()
    {
        var emails = new List<string>();
        for (var i = 0; i < 21; i++)
        {
            emails.Add(UniqueEmail());
            await App.CreateCustomerAsync(email: emails[^1]);
        }

        await Page.GotoAsync("/");
        await Expect(Page.GetByText(emails[^1])).ToBeVisibleAsync();
        await Expect(Page.GetByText(emails[0])).ToHaveCountAsync(0);
        await Expect(Button("Önceki sayfa")).ToBeDisabledAsync();

        await Button("Sonraki sayfa").ClickAsync();

        await Expect(Page.GetByText(emails[0])).ToBeVisibleAsync();
        await Expect(Page.GetByText(emails[^1])).ToHaveCountAsync(0);
        await Expect(Button("Önceki sayfa")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task List_WhenServerIsDown_ShowsMessageAndRecovers()
    {
        // Vite vekil sunucusu API'ye ulaşamayınca düz metin 500 döner.
        await Page.RouteAsync("**/api/customers?*", route => route.FulfillAsync(new() { Status = 500, Body = "" }));
        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Sunucuya ulaşılamıyor");

        await Page.UnrouteAsync("**/api/customers?*");
        await Button("Tekrar dene").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("/customers/00000000-0000-0000-0000-000000000001", "Müşteri bulunamadı.")]
    [InlineData("/customers/abc", "Müşteri bulunamadı.")]
    [InlineData("/olmayan-sayfa", "Sayfa bulunamadı")]
    public async Task UnknownAddresses_ShowMessageAndWayBack(string path, string message)
    {
        await Page.GotoAsync(path);

        await Expect(Page.GetByText(message)).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Müşterilere dön" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Müşteriler");
    }

    [Fact]
    public async Task Phone_HasNoHorizontalScroll()
    {
        await App.CreateCustomerAsync(new string('A', 100), new string('B', 100),
            new string('c', 60) + "@" + new string('d', 60) + ".com");
        await Page.SetViewportSizeAsync(360, 780);

        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Link).First).ToBeVisibleAsync();

        await AssertNoHorizontalScrollAsync();
    }
}
