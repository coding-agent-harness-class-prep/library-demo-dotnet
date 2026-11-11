# Library lending service — instructions for Copilot

This is an ASP.NET Core minimal API on .NET 10. Business rules live in `src/Library.Domain` and have no
web dependencies; `src/Library.Api/Program.cs` only maps HTTP to the domain. Money is always `decimal`,
and dates are `DateOnly` read from the injected `TimeProvider` — never `DateTime.Now`.

## Loan rules

- Every borrowing rule lives in `LoanPolicy.CanBorrow` and reports a `PolicyViolation`. Never throw.
- Every new rule gets a new constant in `PolicyCodes`, numbered after the last one (`LIB003`, `LIB004`,
  …). Never reuse or renumber an existing code.
- Put a rule's limit in a named `public const` on `LoanPolicy` (for example `MaxOutstandingFines`).
- A violation message is one sentence, addressed to library staff, that names the member or the book.

## Tests

- Tests use xUnit v3 (`[Fact]`, `[Theory]` + `[InlineData]`). Name them `Method_Scenario_Expected`.
- Test both sides of every limit in one `[Theory]`: the last allowed value and the first rejected one.
- Assert on violation **codes**, not on message text.
- The build treats warnings as errors, xUnit's analyzers included: use `Assert.Empty` or `Assert.Single`
  rather than comparing a count, and pass `TestContext.Current.CancellationToken` to async calls.

## Every change

- Add a line to `CHANGELOG.md` under `## [Unreleased]`.
- Run `dotnet test` and make sure it passes before you say you're done.
