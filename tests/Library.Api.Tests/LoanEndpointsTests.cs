using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Library.Api.Tests;

public class LoanEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_ReturnsCatalog()
    {
        var books = await _client.GetFromJsonAsync<JsonElement>("/books", TestContext.Current.CancellationToken);

        Assert.Equal(5, books.GetArrayLength());
    }

    [Fact]
    public async Task PostLoan_Valid_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync(
            "/loans", new { memberId = "M-100", isbn = "9780321125217" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.StartsWith("/loans/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PostLoan_UnknownMember_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            "/loans", new { memberId = "M-999", isbn = "9780321125217" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMember_Known_ReturnsOutstandingFines()
    {
        var member = await _client.GetFromJsonAsync<JsonElement>("/members/M-300", TestContext.Current.CancellationToken);

        Assert.Equal(12.50m, member.GetProperty("outstandingFines").GetDecimal());
    }
}
