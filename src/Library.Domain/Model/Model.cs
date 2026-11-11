namespace Library.Domain.Model;

/// <summary>Membership type. It sets the loan period, the loan limit, and the late-fee rules.</summary>
public enum MemberType
{
    Adult,
    Student,
    Senior,
}

public sealed record Book(string Isbn, string Title, string Author, int Copies);

public sealed class Member
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required MemberType Type { get; init; }

    /// <summary>Late fees the member owes and has not paid yet.</summary>
    public decimal OutstandingFines { get; set; }
}

public sealed class Loan
{
    public required Guid Id { get; init; }

    public required string Isbn { get; init; }

    public required string MemberId { get; init; }

    public required DateOnly BorrowedOn { get; init; }

    public required DateOnly DueOn { get; init; }

    public DateOnly? ReturnedOn { get; set; }

    public decimal LateFee { get; set; }

    public bool IsActive => ReturnedOn is null;
}
