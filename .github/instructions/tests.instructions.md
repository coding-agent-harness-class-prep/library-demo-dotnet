---
applyTo: "tests/**"
---

# Writing tests in this repository

- Use the real in-memory stores (`InMemoryBookCatalog`, `InMemoryMemberDirectory`,
  `InMemoryLoanRepository`) instead of mocks. There's no mocking library here.
- Control "today" with the `FixedClock` test helper; never depend on the real date.
- One behavior per test. If a test needs the word "and" in its name, split it.
- Endpoint tests go in `tests/Library.Api.Tests` and use `WebApplicationFactory<Program>`.
