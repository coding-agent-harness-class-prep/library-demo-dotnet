using System.Globalization;
using System.Text.Json.Serialization;
using Library.Domain.Fees;
using Library.Domain.Lending;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();
builder.Services.AddSingleton<IMemberDirectory, InMemoryMemberDirectory>();
builder.Services.AddSingleton<ILoanRepository, InMemoryLoanRepository>();
builder.Services.AddSingleton<LibraryCalendar>();
builder.Services.AddSingleton<DueDateCalculator>();
builder.Services.AddSingleton<LateFeeCalculator>();
builder.Services.AddSingleton<LoanPolicy>();
builder.Services.AddSingleton<LoanService>();

var app = builder.Build();

app.MapGet("/books", (IBookCatalog books) => books.All());

app.MapGet("/members/{id}", (string id, IMemberDirectory members) =>
    members.Find(id) is { } member ? Results.Ok(member) : Results.NotFound());

app.MapGet("/members/{id}/loans/export", (string id, IMemberDirectory members, LoanService service) =>
{
    if (members.Find(id) is null)
    {
        return Results.NotFound();
    }

    var history = service.ForMember(id).Select(loan => new
    {
        loanId = loan.Id,
        isbn = loan.Isbn,
        borrowedOn = loan.BorrowedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        dueOn = loan.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        returnedOn = loan.ReturnedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    });
    return Results.Text(JsonConvert.SerializeObject(history), "application/json");
});

app.MapGet("/loans/{id:guid}", (Guid id, LoanService service) =>
    service.Find(id) is { } loan ? Results.Ok(loan) : Results.NotFound());

app.MapGet("/loans/overdue", (LoanService service) => Results.Ok(service.GetOverdue()));

app.MapPost("/loans", (BorrowRequest request, LoanService service) =>
{
    var outcome = service.Borrow(request.MemberId, request.Isbn);
    return outcome.Status switch
    {
        BorrowStatus.Borrowed => Results.Created($"/loans/{outcome.Loan!.Id}", outcome.Loan),
        BorrowStatus.NotAllowed => Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "This loan is not allowed.",
            extensions: new Dictionary<string, object?> { ["violations"] = outcome.Policy!.Violations }),
        _ => Results.NotFound(),
    };
});

app.MapPost("/loans/{id:guid}/return", (Guid id, LoanService service) =>
    service.Return(id) is { } loan ? Results.Ok(loan) : Results.NotFound());

app.Run();

public sealed record BorrowRequest(string MemberId, string Isbn);

// Exposes the entry point to WebApplicationFactory in the API tests.
public partial class Program;
