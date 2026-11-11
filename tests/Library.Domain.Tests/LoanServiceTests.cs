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
