using Library.Domain.Model;

namespace Library.Domain.Lending;

public sealed record PolicyViolation(string Code, string Message);

public sealed record PolicyResult(IReadOnlyList<PolicyViolation> Violations)
{
    public static readonly PolicyResult Allowed = new([]);

    public bool IsAllowed => Violations.Count == 0;
}

/// <summary>Stable violation codes returned to API clients. Never renumber an existing code.</summary>
public static class PolicyCodes
{
    public const string LoanLimitReached = "LIB001";
    public const string NoCopiesAvailable = "LIB002";
}

/// <summary>Decides whether a member may borrow a book right now.</summary>
public sealed class LoanPolicy
{
    private static readonly IReadOnlyDictionary<MemberType, int> LoanLimits = new Dictionary<MemberType, int>
    {
        [MemberType.Adult] = 5,
        [MemberType.Student] = 3,
        [MemberType.Senior] = 5,
    };

    public PolicyResult CanBorrow(Member member, Book book, int activeLoansForMember, int activeLoansForBook)
    {
        var violations = new List<PolicyViolation>();

        if (activeLoansForMember >= LoanLimits[member.Type])
        {
            violations.Add(new(PolicyCodes.LoanLimitReached,
                $"{member.Name} already has {activeLoansForMember} books out, the limit for this membership."));
        }

        if (activeLoansForBook >= book.Copies)
        {
            violations.Add(new(PolicyCodes.NoCopiesAvailable, $"Every copy of \"{book.Title}\" is on loan."));
        }

        return violations.Count == 0 ? PolicyResult.Allowed : new PolicyResult(violations);
    }
}
