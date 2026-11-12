using System.Net;
using System.Text.Json;
using Library.Domain.Lending;
using Library.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Api.Tests;

public class LoanExportTests
{
    [Fact]
    public async Task GetLoanExport_UnknownMember_Returns404()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/members/M-999/loans/export", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLoanExport_NoLoans_ReturnsEmptyArray()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/members/M-100/loans/export", TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(json.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task GetLoanExport_LoanHistory_ReturnsOldestFirst()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var loans = factory.Services.GetRequiredService<ILoanRepository>();
        var older = new Loan
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MemberId = "M-100",
            Isbn = "9780134685991",
            BorrowedOn = new DateOnly(2024, 1, 2),
            DueOn = new DateOnly(2024, 1, 16),
            ReturnedOn = new DateOnly(2024, 1, 15),
        };
        var newer = new Loan
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            MemberId = "M-100",
            Isbn = "9781617294532",
            BorrowedOn = new DateOnly(2024, 2, 1),
            DueOn = new DateOnly(2024, 2, 15),
        };
        loans.Save(newer);
        loans.Save(older);
        loans.Save(new Loan
        {
            Id = Guid.NewGuid(),
            MemberId = "M-200",
            Isbn = older.Isbn,
            BorrowedOn = older.BorrowedOn,
            DueOn = older.DueOn,
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/members/M-100/loans/export", TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var history = json.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, history.Length);
        Assert.Equal(new[] { "loanId", "isbn", "borrowedOn", "dueOn", "returnedOn" },
            history[0].EnumerateObject().Select(property => property.Name));
        Assert.Equal(older.Id, history[0].GetProperty("loanId").GetGuid());
        Assert.Equal(older.Isbn, history[0].GetProperty("isbn").GetString());
        Assert.Equal("2024-01-02", history[0].GetProperty("borrowedOn").GetString());
        Assert.Equal("2024-01-16", history[0].GetProperty("dueOn").GetString());
        Assert.Equal("2024-01-15", history[0].GetProperty("returnedOn").GetString());
        Assert.Equal(newer.Id, history[1].GetProperty("loanId").GetGuid());
        Assert.Equal(newer.Isbn, history[1].GetProperty("isbn").GetString());
        Assert.Equal("2024-02-01", history[1].GetProperty("borrowedOn").GetString());
        Assert.Equal("2024-02-15", history[1].GetProperty("dueOn").GetString());
        Assert.Equal(JsonValueKind.Null, history[1].GetProperty("returnedOn").ValueKind);
    }
}
