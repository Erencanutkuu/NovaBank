using System.Net;
using System.Text.Json;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

public sealed class CustomerTests(DatabaseApiFixture api) : IClassFixture<DatabaseApiFixture>
{
    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> CreateAsync(string firstName, string lastName, string email) =>
        api.Client.PostJsonAsync("/api/customers", JsonSerializer.Serialize(new { firstName, lastName, email }));

    [Fact]
    public async Task Create_Returns201AndNormalizedCustomer()
    {
        var email = UniqueEmail();

        var response = await CreateAsync(" Işıl ", "Çınar", $" {email.ToUpperInvariant()} ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.ReadDataAsync();
        Assert.NotEqual(Guid.Empty, data.GetProperty("id").GetGuid());
        Assert.Equal("Işıl", data.GetProperty("firstName").GetString());
        Assert.Equal("Çınar", data.GetProperty("lastName").GetString());
        Assert.Equal(email, data.GetProperty("email").GetString());
        Assert.Equal(TimeSpan.Zero, data.GetProperty("createdAt").GetDateTimeOffset().Offset);
    }

    // Location takip edilince veritabanından okunan gövde, oluşturma cevabıyla birebir aynı olmalı.
    [Fact]
    public async Task Create_LocationReturnsSameCustomer()
    {
        var created = await CreateAsync("Eren", "Utku", UniqueEmail());

        var response = await api.Client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((await created.ReadDataAsync()).GetRawText(), (await response.ReadDataAsync()).GetRawText());
    }

    [Fact]
    public async Task Create_WithExistingEmailInDifferentCase_Returns409()
    {
        var email = UniqueEmail();
        await CreateAsync("Eren", "Utku", email);

        var response = await CreateAsync("Başka", "Kişi", $" {email.ToUpperInvariant()}");

        await response.AssertProblemAsync(409, "7233");
    }

    [Fact]
    public async Task Create_WithSameEmailConcurrently_CreatesExactlyOne()
    {
        for (var round = 0; round < 5; round++)
        {
            var email = UniqueEmail();

            var responses = await Task.WhenAll(
                Enumerable.Range(0, 10).Select(_ => CreateAsync("Eren", "Utku", email)));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
            foreach (var response in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
            {
                await response.AssertProblemAsync(409, "7233");
            }
        }
    }

    public static TheoryData<string, string, string> InvalidFields() => new()
    {
        { """{"lastName":"Utku","email":"a@example.com"}""", "firstName", "Ad zorunludur." },
        { """{"firstName":"  ","lastName":"Utku","email":"a@example.com"}""", "firstName", "Ad zorunludur." },
        { """{"firstName":"Eren","lastName":null,"email":"a@example.com"}""", "lastName", "Soyad zorunludur." },
        { """{"firstName":"Eren","lastName":"Utku"}""", "email", "E-posta zorunludur." },
        { """{"firstName":"Eren","lastName":"Utku","email":"gecersiz"}""", "email", "E-posta biçimi geçersiz." },
        { """{"firstName":"Eren","lastName":"Utku","email":"a b@example.com"}""", "email", "E-posta biçimi geçersiz." },
        // PostgreSQL metinde NUL karakteri kabul etmez; 500 yerine 400 dönmeli.
        { """{"firstName":"Er\u0000en","lastName":"Utku","email":"a@example.com"}""", "firstName", "Ad geçersiz karakter içeriyor." },
        { """{"firstName":"Eren","lastName":"Ut\u0000ku","email":"a@example.com"}""", "lastName", "Soyad geçersiz karakter içeriyor." },
        { """{"firstName":"Eren","lastName":"Utku","email":"a\u0000@example.com"}""", "email", "E-posta biçimi geçersiz." },
        { $$"""{"firstName":"{{new string('a', 101)}}","lastName":"Utku","email":"a@example.com"}""", "firstName", "Ad en çok 100 karakter olabilir." },
        { $$"""{"firstName":"Eren","lastName":"{{new string('a', 101)}}","email":"a@example.com"}""", "lastName", "Soyad en çok 100 karakter olabilir." },
        { $$"""{"firstName":"Eren","lastName":"Utku","email":"{{new string('a', 250)}}@example.com"}""", "email", "E-posta en çok 254 karakter olabilir." },
    };

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task Create_WithInvalidField_Returns400(string json, string field, string message)
    {
        var response = await api.Client.PostJsonAsync("/api/customers", json);

        await response.AssertProblemAsync(400, "7000");
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.Equal([message], errors.GetProperty(field).EnumerateArray().Select(x => x.GetString()!));
    }

    [Fact]
    public async Task Create_WithBrokenUnicode_Returns400()
    {
        var response = await api.Client.PostJsonAsync(
            "/api/customers", """{"firstName":"\ud800","lastName":"Utku","email":"a@example.com"}""");

        await response.AssertProblemAsync(400, "7000");
    }

    // Diğer testler de müşteri ekler; burada yalnızca bu testin en yeni kayıtlarına bakılır.
    [Fact]
    public async Task List_ReturnsNewestFirstWithPaging()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Insert(0, (await (await CreateAsync("Eren", "Utku", UniqueEmail())).ReadDataAsync())
                .GetProperty("id").GetGuid());
        }

        var first = await (await api.Client.GetAsync("/api/customers?page=1&pageSize=2")).ReadDataAsync();
        var second = await (await api.Client.GetAsync("/api/customers?page=2&pageSize=2")).ReadDataAsync();

        Assert.Equal(ids.Take(2), Ids(first));
        Assert.Equal(ids[2], Ids(second)[0]);
        Assert.Equal(2, second.GetProperty("page").GetInt32());
        Assert.Equal(2, second.GetProperty("pageSize").GetInt32());
        Assert.True(first.GetProperty("totalCount").GetInt32() >= 3);

        static Guid[] Ids(JsonElement page) =>
            page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
    }

    [Fact]
    public async Task List_ItemsMatchCreatedCustomerAndDefaultTo20()
    {
        var created = await (await CreateAsync("Işıl", "Çınar", UniqueEmail())).ReadDataAsync();

        var response = await api.Client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.ReadDataAsync();
        Assert.Equal(20, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(created.GetRawText(), data.GetProperty("items")[0].GetRawText());
    }

    [Theory]
    [InlineData("?page=0", "page", "Sayfa 1 ile 1.000.000 arasında olmalıdır.")]
    [InlineData("?pageSize=101", "pageSize", "Sayfa boyutu 1 ile 100 arasında olmalıdır.")]
    [InlineData("?page=abc", "page", "Geçersiz değer.")]
    public async Task List_WithInvalidPaging_Returns400(string query, string field, string message)
    {
        var response = await api.Client.GetAsync($"/api/customers{query}");

        await response.AssertProblemAsync(400, "7000");
        Assert.Equal([message], (await response.ReadJsonAsync()).GetProperty("errors")
            .GetProperty(field).EnumerateArray().Select(x => x.GetString()!));
    }

    [Fact]
    public async Task List_BeyondLastPage_ReturnsEmpty()
    {
        var data = await (await api.Client.GetAsync("/api/customers?page=1000000&pageSize=100")).ReadDataAsync();

        Assert.Equal(0, data.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Get_UnknownCustomer_Returns404()
    {
        await (await api.Client.GetAsync($"/api/customers/{Guid.NewGuid()}")).AssertProblemAsync(404, "7232");
    }

    [Fact]
    public async Task Get_WithNonGuidId_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync("/api/customers/abc")).StatusCode);
    }

    [Fact]
    public async Task Email_IsNeverLogged()
    {
        var email = UniqueEmail();
        var created = await CreateAsync("Eren", "Utku", email);
        await CreateAsync("Eren", "Utku", email);
        await api.Client.GetAsync(created.Headers.Location);
        await api.Client.GetAsync("/api/customers");
        // Yarış: benzersiz index hatası da e-postayı loga taşımamalı.
        var racingEmail = UniqueEmail();
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => CreateAsync("Eren", "Utku", racingEmail)));

        Assert.NotEmpty(api.Logs.Events);
        foreach (var log in api.Logs.Events)
        {
            var text = log.RenderMessage() + string.Join(" ", log.Properties.Values) + log.Exception;
            Assert.DoesNotContain(email, text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(racingEmail, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
