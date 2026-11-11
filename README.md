# Library lending service (.NET)

A small lending API for a public library: books, members, loans, due dates, and late fees.
It's an ASP.NET Core minimal API on .NET 10 with an in-memory store, so there's no database to set up.

## Layout

```
Library.slnx
src/
  Library.Domain/        # the business rules — no web dependencies
    Model/               # Book, Member, Loan, MemberType
    Lending/             # LoanService, LoanPolicy, DueDateCalculator, LibraryCalendar, the stores
    Fees/                # LateFeeCalculator
  Library.Api/           # HTTP endpoints (Program.cs) and a sample .http file
tests/
  Library.Domain.Tests/  # xUnit tests for the domain
  Library.Api.Tests/     # endpoint tests through WebApplicationFactory
```

## Build, test, run

```powershell
dotnet build
dotnet test
dotnet run --project src/Library.Api     # http://localhost:5090
```

`global.json` opts `dotnet test` into Microsoft.Testing.Platform, which xUnit v3 uses. Package versions
are pinned in `Directory.Packages.props` and each project commits its `packages.lock.json`.

## Endpoints

| Method | Path | What it does |
|---|---|---|
| `GET` | `/books` | The catalog |
| `GET` | `/members/{id}` | A member, including outstanding fines |
| `POST` | `/loans` | Borrow a book (`{ "memberId", "isbn" }`): 201, 400 with policy codes, or 404 |
| `GET` | `/loans/{id}` | One loan, or 404 |
| `POST` | `/loans/{id}/return` | Return a book; any late fee is added to the member's fines |

## Lending rules in brief

Adults and seniors borrow for 14 days and students for 21. A loan is never due on a day the library is
closed (Sundays and public holidays). Late fees follow board policy LIB-12, in `LateFeeCalculator`.
