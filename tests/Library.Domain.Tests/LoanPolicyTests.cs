using Library.Domain.Lending;
using Library.Domain.Model;

namespace Library.Domain.Tests;

public class LoanPolicyTests
{
    private readonly LoanPolicy _policy = new();

    private static readonly Book Book = new("9780134685991", "Effective Java", "Joshua Bloch", Copies: 2);

    private static Member MemberOf(MemberType type) => new() { Id = "M-1", Name = "Test Member", Type = type };

    [Fact]
    public void CanBorrow_UnderLimitsWithCopiesFree_IsAllowed()
    {
        var result = _policy.CanBorrow(MemberOf(MemberType.Adult), Book, activeLoansForMember: 0, activeLoansForBook: 0);

        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData(MemberType.Adult, 5)]
    [InlineData(MemberType.Student, 3)]
    [InlineData(MemberType.Senior, 5)]
    public void CanBorrow_AtLoanLimit_ReturnsLoanLimitReached(MemberType type, int limit)
    {
        var result = _policy.CanBorrow(MemberOf(type), Book, activeLoansForMember: limit, activeLoansForBook: 0);

        var violation = Assert.Single(result.Violations);
        Assert.Equal(PolicyCodes.LoanLimitReached, violation.Code);
    }

    [Fact]
    public void CanBorrow_StudentBelowLimit_IsAllowed()
    {
        var result = _policy.CanBorrow(MemberOf(MemberType.Student), Book, activeLoansForMember: 2, activeLoansForBook: 0);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void CanBorrow_AllCopiesOut_ReturnsNoCopiesAvailable()
    {
        var result = _policy.CanBorrow(MemberOf(MemberType.Adult), Book, activeLoansForMember: 0, activeLoansForBook: 2);

        var violation = Assert.Single(result.Violations);
        Assert.Equal(PolicyCodes.NoCopiesAvailable, violation.Code);
        Assert.Contains("Effective Java", violation.Message);
    }
}
