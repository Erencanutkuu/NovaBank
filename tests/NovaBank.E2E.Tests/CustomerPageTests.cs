using System.Text.RegularExpressions;

namespace NovaBank.E2E.Tests;

public sealed class CustomerPageTests(AppFixture app) : PageTest(app)
{
    private const string AmountFormatMessage = "Tutarı 1.250,50 biçiminde yaz.";
    private const string AmountRangeMessage = "Tutar 0'dan büyük, en çok 1.000.000 olmalıdır.";

    private ILocator Balance => Page.GetByTestId("balance");
    private ILocator Rows => Page.GetByTestId("transaction");
    private ILocator AmountInput => Dialog.GetByLabel("Tutar");
    private ILocator Cards => Page.GetByTestId("account-card");

    private async Task<Guid> OpenPageAsync(string currency = "TRY", decimal balance = 0)
    {
        var customerId = await App.CreateCustomerAsync();
        var accountId = await App.OpenAccountAsync(customerId, currency, balance);
        await Page.GotoAsync($"/customers/{customerId}");
        await Expect(Balance).ToBeVisibleAsync();
        return accountId;
    }

    private async Task MoveAsync(string action, string amount)
    {
        await Button(action).ClickAsync();
        await AmountInput.FillAsync(amount);
        await DialogButton(action).ClickAsync();
    }

    [Fact]
    public async Task DepositAndWithdraw_UpdateBalanceCardAndHistory()
    {
        await OpenPageAsync();

        await MoveAsync("Para yatır", "1.000");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Para yatırıldı.");
        await Expect(Balance).ToHaveTextAsync("1.000,00 TL");

        await MoveAsync("Para çek", "250,5");
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Para çekildi.");
        await Expect(Balance).ToHaveTextAsync("749,50 TL");

        await Expect(Dialog).ToHaveCountAsync(0);
        await Expect(Cards).ToContainTextAsync("749,50 TL");
        await Expect(Rows).ToHaveCountAsync(2);
        await Expect(Rows.Nth(0)).ToContainTextAsync("Para çekme");
        await Expect(Rows.Nth(0)).ToContainTextAsync("−250,50 TL");
        await Expect(Rows.Nth(0)).ToContainTextAsync("749,50 TL");
        await Expect(Rows.Nth(1)).ToContainTextAsync("Para yatırma");
        await Expect(Rows.Nth(1)).ToContainTextAsync("+1.000,00 TL");
    }

    [Theory]
    [InlineData("1", "1,00 TL")]
    [InlineData(" 1.250,50 ", "1.250,50 TL")]
    [InlineData("1250,5", "1.250,50 TL")]
    [InlineData("0,01", "0,01 TL")]
    [InlineData("0,02", "0,02 TL")]
    [InlineData("999.999,99", "999.999,99 TL")]
    [InlineData("1.000.000", "1.000.000,00 TL")]
    public async Task Amount_AcceptsTurkishFormatAndShowsPreview(string text, string expected)
    {
        await OpenPageAsync();
        await Button("Para yatır").ClickAsync();
        await AmountInput.FillAsync(text);

        await Expect(Dialog.GetByTestId("amount-preview")).ToContainTextAsync(expected);
        await DialogButton("Para yatır").ClickAsync();

        await Expect(Balance).ToHaveTextAsync(expected);
    }

    [Theory]
    [InlineData("", AmountFormatMessage)]
    [InlineData("abc", AmountFormatMessage)]
    [InlineData("10.5", AmountFormatMessage)] // Nokta binlik ayırıcıdır; tahmin edilmez.
    [InlineData("1.00", AmountFormatMessage)]
    [InlineData("0.500", AmountFormatMessage)]
    [InlineData("10,555", AmountFormatMessage)]
    [InlineData("0,009", AmountFormatMessage)]
    [InlineData("1,000.50", AmountFormatMessage)]
    [InlineData("-5", AmountFormatMessage)]
    [InlineData("1e3", AmountFormatMessage)]
    [InlineData("0", AmountRangeMessage)]
    [InlineData("0,00", AmountRangeMessage)]
    [InlineData("1.000.000,01", AmountRangeMessage)]
    public async Task Amount_RejectsInvalidTextWithoutSendingRequest(string text, string message)
    {
        var accountId = await OpenPageAsync();

        await MoveAsync("Para yatır", text);

        await Expect(AmountInput).ToHaveAccessibleDescriptionAsync(new Regex(Regex.Escape(message)));
        await Expect(AmountInput).ToBeFocusedAsync();
        Assert.Empty(await App.GetAmountsAsync(accountId));
    }

    // İkili tabanda 0.1 + 0.2 = 0.30000000000000004 olur; ekranda ve kayıtta kuruş kaymamalı.
    [Fact]
    public async Task Cents_AddUpExactly()
    {
        await OpenPageAsync();

        await MoveAsync("Para yatır", "0,10");
        await Expect(Balance).ToHaveTextAsync("0,10 TL");
        await MoveAsync("Para yatır", "0,20");
        await Expect(Balance).ToHaveTextAsync("0,30 TL");
        await MoveAsync("Para çek", "0,30");

        await Expect(Balance).ToHaveTextAsync("0,00 TL");
    }

    [Theory]
    [InlineData("99,99", "0,01 TL")]
    [InlineData("100", "0,00 TL")]
    public async Task Withdraw_UpToBalance_Succeeds(string amount, string remaining)
    {
        await OpenPageAsync(balance: 100);

        await MoveAsync("Para çek", amount);

        await Expect(Balance).ToHaveTextAsync(remaining);
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_ShowsServerMessageAndKeepsDialog()
    {
        await OpenPageAsync(balance: 100);

        await MoveAsync("Para çek", "100,01");

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Bakiye yetersiz.");
        await Expect(AmountInput).ToHaveValueAsync("100,01");
        await Expect(Balance).ToHaveTextAsync("100,00 TL");
    }

    [Fact]
    public async Task Withdraw_AllFillsWholeBalance()
    {
        await OpenPageAsync("USD", 1234.56m);
        await Button("Para çek").ClickAsync();

        await DialogButton("Tümü").ClickAsync();
        await Expect(AmountInput).ToHaveValueAsync("1.234,56");
        await DialogButton("Para çek").ClickAsync();

        await Expect(Balance).ToHaveTextAsync("0,00 $");
    }

    [Fact]
    public async Task Deposit_QuickAmountFillsInput()
    {
        await OpenPageAsync();
        await Button("Para yatır").ClickAsync();

        await DialogButton("500").ClickAsync();

        await Expect(AmountInput).ToHaveValueAsync("500");
    }

    [Fact]
    public async Task Deposit_DoubleClickCreatesOneTransaction()
    {
        var accountId = await OpenPageAsync();
        await Page.RouteAsync("**/deposit", async route =>
        {
            await Task.Delay(400);
            await route.ContinueAsync();
        });
        await Button("Para yatır").ClickAsync();
        await AmountInput.FillAsync("10");

        await Dialog.Locator("form").EvaluateAsync("form => { form.requestSubmit(); form.requestSubmit(); }");
        await Page.Keyboard.PressAsync("Escape"); // İşlem sürerken pencere kapanmaz.
        await Expect(DialogButton("Para yatır")).ToBeDisabledAsync();

        await Expect(Balance).ToHaveTextAsync("10,00 TL");
        Assert.Equal([10m], await App.GetAmountsAsync(accountId));
    }

    // Sunucu işledi ama cevap gelmedi: ekran tekrar denemez, gerçek durumu gösterir.
    [Fact]
    public async Task Deposit_WhenResponseIsLost_SaysUncertainAndRefreshes()
    {
        var accountId = await OpenPageAsync();
        await Page.RouteAsync("**/deposit", async route =>
        {
            await route.FetchAsync();
            await route.AbortAsync();
        });

        await MoveAsync("Para yatır", "10");

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Sonuç belirsiz");
        await Expect(DialogButton("Para yatır")).ToHaveCountAsync(0); // Aynı pencereden tekrar gönderilemez.
        await DialogButton("Hareketleri kontrol et").ClickAsync();
        await Expect(Dialog).ToHaveCountAsync(0);
        await Expect(Balance).ToHaveTextAsync("10,00 TL");
        await Expect(Rows).ToHaveCountAsync(1);
        Assert.Equal([10m], await App.GetAmountsAsync(accountId));
    }

    [Fact]
    public async Task Deposit_WhenServerFails_SaysUncertain()
    {
        await OpenPageAsync();
        await Page.RouteAsync("**/deposit", route => route.FulfillAsync(new()
        {
            Status = 500,
            ContentType = "application/problem+json",
            Body = """{"title":"Beklenmeyen bir hata oluştu.","status":500,"errorCode":"9000","traceId":"x"}"""
        }));

        await MoveAsync("Para yatır", "10");

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Sonuç belirsiz");
    }

    [Theory]
    [InlineData("TRY", "TL")]
    [InlineData("USD", "$")]
    [InlineData("EUR", "€")]
    public async Task OpenAccount_AddsAndSelectsIt(string currency, string symbol)
    {
        var customerId = await App.CreateCustomerAsync();
        await Page.GotoAsync($"/customers/{customerId}");
        await Button("Hesap aç").First.ClickAsync();
        await Dialog.GetByRole(AriaRole.Radio, new() { Name = currency }).CheckAsync();

        await DialogButton("Hesap aç").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Hesap açıldı.");
        await Expect(Cards).ToHaveCountAsync(1);
        await Expect(Cards).ToContainTextAsync(currency);
        await Expect(Cards).ToContainTextAsync(new Regex(@"TR\d{2}( \d{4}){5} \d{2}"));
        await Expect(Balance).ToHaveTextAsync($"0,00 {symbol}");
        await Expect(Page.GetByText("Henüz hareket yok")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OpenAccount_SecondAccountBecomesSelected()
    {
        await OpenPageAsync(balance: 5);
        await Button("Hesap aç").ClickAsync();
        await Dialog.GetByRole(AriaRole.Radio, new() { Name = "EUR" }).CheckAsync();

        await DialogButton("Hesap aç").ClickAsync();

        await Expect(Cards).ToHaveCountAsync(2);
        await Expect(Balance).ToHaveTextAsync("0,00 €");
    }

    [Fact]
    public async Task Close_AsksFirstThenDisablesActions()
    {
        await OpenPageAsync();
        await Button("Hesabı kapat").ClickAsync();
        await Expect(Dialog).ToContainTextAsync("geri alınamaz");
        await DialogButton("Vazgeç").ClickAsync();
        await Expect(Button("Para yatır")).ToBeEnabledAsync();

        await Button("Hesabı kapat").ClickAsync();
        await DialogButton("Hesabı kapat").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Hesap kapatıldı.");
        await Expect(Cards).ToContainTextAsync("Kapalı");
        await Expect(Button("Para yatır")).ToBeDisabledAsync();
        await Expect(Button("Para çek")).ToBeDisabledAsync();
        await Expect(Button("Hesabı kapat")).ToBeDisabledAsync();
    }

    // Kapatılamayacak hesap için "kapatılacak" denmez; yapılacak iş gösterilir.
    [Fact]
    public async Task Close_WithBalance_LeadsToWithdraw()
    {
        await OpenPageAsync(balance: 0.01m);
        await Button("Hesabı kapat").ClickAsync();

        await Expect(Dialog).ToContainTextAsync("Hesapta 0,01 TL var. Kapatmak için önce bakiyeyi sıfırla.");
        await Expect(Dialog).Not.ToContainTextAsync("kapatılacak");
        await Expect(DialogButton("Hesabı kapat")).ToHaveCountAsync(0);
        await DialogButton("Para çek").ClickAsync();

        await Expect(Dialog.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Para çek");
        await Expect(AmountInput).ToBeFocusedAsync();
        await DialogButton("Tümü").ClickAsync();
        await DialogButton("Para çek").ClickAsync();
        await Expect(Balance).ToHaveTextAsync("0,00 TL");
    }

    // Pencere açıkken başka yerden para yatmış: sunucu reddeder, pencere güncel bakiyeyi gösterir.
    [Fact]
    public async Task Close_WhenMoneyArrivesMeanwhile_ShowsNewBalance()
    {
        var accountId = await OpenPageAsync();
        await Button("Hesabı kapat").ClickAsync();
        await App.DepositAsync(accountId, 5);

        await DialogButton("Hesabı kapat").ClickAsync();

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Hesapta 5,00 TL var.");
        await Expect(DialogButton("Hesabı kapat")).ToHaveCountAsync(0);
        await Expect(Cards).ToContainTextAsync("Açık");
    }

    [Fact]
    public async Task Close_WhenAlreadyClosed_ShowsOnlyServerMessage()
    {
        var accountId = await OpenPageAsync();
        await Button("Hesabı kapat").ClickAsync();
        (await App.Api.PostAsync($"/api/accounts/{accountId}/close", null)).EnsureSuccessStatusCode();

        await DialogButton("Hesabı kapat").ClickAsync();

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Hesap kapalı.");
        await Expect(Dialog).Not.ToContainTextAsync("kapatılacak");
        await DialogButton("Tamam").ClickAsync();
        await Expect(Cards).ToContainTextAsync("Kapalı");
    }

    // Hesap başka bir pencerede değişmiş: hata gelince ekran da güncellenir.
    [Fact]
    public async Task StaleScreen_RefreshesAfterServerRejects()
    {
        var accountId = await OpenPageAsync();
        (await App.Api.PostAsync($"/api/accounts/{accountId}/close", null)).EnsureSuccessStatusCode();

        await MoveAsync("Para yatır", "10");

        await Expect(Dialog.GetByRole(AriaRole.Alert)).ToContainTextAsync("Hesap kapalı.");
        await Expect(Cards).ToContainTextAsync("Kapalı");
    }

    [Fact]
    public async Task History_PagesAndReturnsToFirstPageAfterMove()
    {
        var accountId = await OpenPageAsync();
        for (var i = 1; i <= 11; i++)
        {
            await App.DepositAsync(accountId, i);
        }

        await Page.ReloadAsync();
        await Expect(Rows).ToHaveCountAsync(10);
        await Expect(Rows.First).ToContainTextAsync("+11,00 TL");
        await Button("Sonraki sayfa").ClickAsync();
        await Expect(Rows).ToHaveCountAsync(1);
        await Expect(Rows.First).ToContainTextAsync("+1,00 TL");

        await MoveAsync("Para yatır", "99");

        await Expect(Rows.First).ToContainTextAsync("+99,00 TL");
        await Expect(Button("Önceki sayfa")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task SwitchingAccounts_NeverShowsOldAccountsLateResponse()
    {
        var customerId = await App.CreateCustomerAsync();
        var first = await App.OpenAccountAsync(customerId, "TRY", 111);
        await App.OpenAccountAsync(customerId, "USD", 222);
        var release = new TaskCompletionSource();
        await Page.RouteAsync($"**/api/accounts/{first}/transactions*", async route =>
        {
            await release.Task;
            try { await route.ContinueAsync(); } catch (PlaywrightException) { /* İstek iptal edilmiş olabilir. */ }
        });
        await Page.GotoAsync($"/customers/{customerId}");

        await Cards.Nth(1).ClickAsync();
        await Expect(Rows.First).ToContainTextAsync("+222,00 $");
        release.SetResult();
        await Page.WaitForTimeoutAsync(500);

        await Expect(Rows).ToHaveCountAsync(1);
        await Expect(Rows.First).ToContainTextAsync("+222,00 $");
        await Expect(Balance).ToHaveTextAsync("222,00 $");
    }

    [Fact]
    public async Task SelectedAccount_SurvivesReload()
    {
        var customerId = await App.CreateCustomerAsync();
        await App.OpenAccountAsync(customerId, "TRY", 1);
        await App.OpenAccountAsync(customerId, "EUR", 2);
        await Page.GotoAsync($"/customers/{customerId}");
        await Expect(Balance).ToHaveTextAsync("1,00 TL");
        await Cards.Nth(1).ClickAsync();

        await Page.ReloadAsync();

        await Expect(Balance).ToHaveTextAsync("2,00 €");
        await Expect(Cards.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Fact]
    public async Task Keyboard_CanDepositWithoutMouse()
    {
        await OpenPageAsync();
        await Button("Para yatır").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(AmountInput).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync("75");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Balance).ToHaveTextAsync("75,00 TL");
        await Expect(Button("Para yatır")).ToBeFocusedAsync(); // Odak başladığı yere döner.
    }

    [Fact]
    public async Task CopyIban_PutsPlainIbanOnClipboard()
    {
        // Pano izinleri her tarayıcıda farklıdır; panoya yazılan değer burada yakalanır.
        await Page.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'clipboard', {
              value: { writeText: async text => { window.copied = text } }
            })
            """);
        await OpenPageAsync();

        await Button("IBAN'ı kopyala").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("IBAN kopyalandı.");
        Assert.Matches(@"^TR\d{24}$", await Page.EvaluateAsync<string>("window.copied"));
    }

    [Fact]
    public async Task Phone_HasNoHorizontalScroll()
    {
        var customerId = await App.CreateCustomerAsync(new string('A', 100), new string('B', 100));
        for (var i = 0; i < 12; i++)
        {
            await App.OpenAccountAsync(customerId, "TRY");
        }

        var accountId = await App.OpenAccountAsync(customerId, "TRY", 1_000_000);
        await Page.SetViewportSizeAsync(360, 780);

        await Page.GotoAsync($"/customers/{customerId}?account={accountId}");
        await Expect(Rows).ToHaveCountAsync(1);
        await AssertNoHorizontalScrollAsync();

        await Button("Para yatır").ClickAsync();
        await Expect(DialogButton("Para yatır")).ToBeInViewportAsync();
        await AssertNoHorizontalScrollAsync();
    }
}
