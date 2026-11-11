using System.Collections.Concurrent;
using Library.Domain.Model;

namespace Library.Domain.Lending;

public interface IBookCatalog
{
    IReadOnlyList<Book> All();

    Book? Find(string isbn);
}

public interface IMemberDirectory
{
    Member? Find(string id);
}

public interface ILoanRepository
{
    void Save(Loan loan);

    Loan? Find(Guid id);

    IReadOnlyList<Loan> Active();
}

public sealed class InMemoryBookCatalog : IBookCatalog
{
    private readonly Dictionary<string, Book> _books = new[]
    {
        new Book("9780134685991", "Effective Java", "Joshua Bloch", 2),
        new Book("9781617294532", "C# in Depth", "Jon Skeet", 1),
        new Book("9780321125217", "Domain-Driven Design", "Eric Evans", 3),
        new Book("9780135957059", "The Pragmatic Programmer", "David Thomas and Andrew Hunt", 2),
        new Book("9781492040347", "Designing Data-Intensive Applications", "Martin Kleppmann", 1),
    }.ToDictionary(b => b.Isbn);

    public IReadOnlyList<Book> All() => [.. _books.Values.OrderBy(b => b.Title)];

    public Book? Find(string isbn) => _books.GetValueOrDefault(isbn);
}

public sealed class InMemoryMemberDirectory : IMemberDirectory
{
    private readonly Dictionary<string, Member> _members = new[]
    {
        new Member { Id = "M-100", Name = "Avery Chen", Type = MemberType.Adult },
        new Member { Id = "M-200", Name = "Jordan Diaz", Type = MemberType.Student },
        new Member { Id = "M-300", Name = "Morgan Patel", Type = MemberType.Senior, OutstandingFines = 12.50m },
    }.ToDictionary(m => m.Id);

    public Member? Find(string id) => _members.GetValueOrDefault(id);
}

public sealed class InMemoryLoanRepository : ILoanRepository
{
    private readonly ConcurrentDictionary<Guid, Loan> _loans = new();

    public void Save(Loan loan) => _loans[loan.Id] = loan;

    public Loan? Find(Guid id) => _loans.GetValueOrDefault(id);

    public IReadOnlyList<Loan> Active() => [.. _loans.Values.Where(l => l.IsActive).OrderBy(l => l.DueOn)];
}
