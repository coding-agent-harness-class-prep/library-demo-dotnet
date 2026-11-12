using Library.Domain.Fees;
using Library.Domain.Lending;
using Library.Domain.Model;

namespace Library.Domain.Tests;

public class LoanServiceTests
{
    private const string EffectiveJava = "9780134685991";

    // Monday 1 June 2026: an ordinary open day, well clear of any holiday.
    private readonly FixedClock _clock = new(new DateOnly(2026, 6, 1));
    private readonly InMemoryMemberDirectory _members = new();
    private readonly LoanService _service;

    public LoanServiceTests()
    {
        _service = new LoanService(
            new InMemoryBookCatalog(),
            _members,
            new InMemoryLoanRepository(),
            new LoanPolicy(),
            new DueDateCalculator(new LibraryCalendar()),
            new LateFeeCalculator(),
            _clock);
    }

    [Fact]
    public void Borrow_AdultMember_IsDueInFourteenDays()
    {
        var outcome = _service.Borrow("M-100", EffectiveJava);

        Assert.Equal(BorrowStatus.Borrowed, outcome.Status);
        Assert.Equal(new DateOnly(2026, 6, 15), outcome.Loan!.DueOn);
    }

    [Fact]
    public void Borrow_StudentMember_IsDueInTwentyOneDays()
    {
        var outcome = _service.Borrow("M-200", EffectiveJava);

        Assert.Equal(new DateOnly(2026, 6, 22), outcome.Loan!.DueOn);
    }

    [Fact]
    public void Borrow_UnknownMember_ReturnsMemberNotFound()
    {
        Assert.Equal(BorrowStatus.MemberNotFound, _service.Borrow("M-999", EffectiveJava).Status);
    }

    [Fact]
    public void Borrow_LastCopyAlreadyOut_ReturnsNotAllowed()
    {
        const string csharpInDepth = "9781617294532"; // one copy only
        _service.Borrow("M-100", csharpInDepth);

        var outcome = _service.Borrow("M-200", csharpInDepth);

        Assert.Equal(BorrowStatus.NotAllowed, outcome.Status);
        Assert.Contains(outcome.Policy!.Violations, v => v.Code == PolicyCodes.NoCopiesAvailable);
    }

    [Fact]
    public void GetOverdue_NoLoans_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetOverdue());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void GetOverdue_DueDateBoundary_OnlyIncludesPastDueLoans(int daysAfterDue)
    {
        var loan = _service.Borrow("M-100", EffectiveJava).Loan!;
        _clock.Today = loan.DueOn.AddDays(daysAfterDue);

        var overdue = _service.GetOverdue();

        if (daysAfterDue > 0)
        {
            Assert.Equal(loan.Id, Assert.Single(overdue).LoanId);
        }
        else
        {
            Assert.Empty(overdue);
        }
    }

    [Theory]
    [InlineData("M-100", 2, 0)]
    [InlineData("M-100", 5, 1.25)]
    [InlineData("M-100", 60, 10)]
    [InlineData("M-200", 5, 0.63)]
    [InlineData("M-300", 5, 0)]
    [InlineData("M-300", 8, 0.25)]
    public void GetOverdue_OverdueLoan_ReturnsDetailsWithPolicyFee(string memberId, int daysOverdue, decimal expectedFee)
    {
        var loan = _service.Borrow(memberId, EffectiveJava).Loan!;
        _clock.Today = loan.DueOn.AddDays(daysOverdue);

        var overdue = Assert.Single(_service.GetOverdue());

        Assert.Equal(loan.Id, overdue.LoanId);
        Assert.Equal(loan.Isbn, overdue.Isbn);
        Assert.Equal(loan.MemberId, overdue.MemberId);
        Assert.Equal(loan.DueOn, overdue.DueOn);
        Assert.Equal(daysOverdue, overdue.DaysOverdue);
        Assert.Equal(expectedFee, overdue.FeeIfReturnedToday);
        Assert.True(loan.IsActive);
        Assert.Equal(0m, loan.LateFee);
        Assert.Null(loan.ReturnedOn);
    }

    [Fact]
    public void GetOverdue_MultipleLoans_ReturnsMostOverdueFirst()
    {
        var newer = _service.Borrow("M-200", EffectiveJava).Loan!;
        var older = _service.Borrow("M-100", EffectiveJava).Loan!;
        var returned = _service.Borrow("M-100", "9781617294532").Loan!;
        _service.Return(returned.Id);
        _clock.Today = newer.DueOn.AddDays(5);

        var overdue = _service.GetOverdue();

        Assert.Equal(new[] { older.Id, newer.Id }, overdue.Select(loan => loan.LoanId));
    }

    [Fact]
    public void GetOverdue_ReturnedLoan_ReturnsEmptyList()
    {
        var loan = _service.Borrow("M-100", EffectiveJava).Loan!;
        _clock.Today = loan.DueOn.AddDays(5);
        _service.Return(loan.Id);

        Assert.Empty(_service.GetOverdue());
    }

    [Fact]
    public void Return_OnTime_ChargesNothing()
    {
        var loan = _service.Borrow("M-100", EffectiveJava).Loan!;
        _clock.Today = loan.DueOn;

        var returned = _service.Return(loan.Id)!;

        Assert.Equal(0m, returned.LateFee);
        Assert.Equal(0m, _members.Find("M-100")!.OutstandingFines);
    }

    [Fact]
    public void Return_FiveDaysLate_AddsFeeToMemberFines()
    {
        var loan = _service.Borrow("M-100", EffectiveJava).Loan!;
        _clock.Today = loan.DueOn.AddDays(5);

        var returned = _service.Return(loan.Id)!;

        Assert.Equal(1.25m, returned.LateFee);
        Assert.Equal(1.25m, _members.Find("M-100")!.OutstandingFines);
        Assert.False(returned.IsActive);
    }
}
