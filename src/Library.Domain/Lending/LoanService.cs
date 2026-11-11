using Library.Domain.Fees;
using Library.Domain.Model;

namespace Library.Domain.Lending;

public enum BorrowStatus
{
    Borrowed,
    MemberNotFound,
    BookNotFound,
    NotAllowed,
}

public sealed record BorrowOutcome(BorrowStatus Status, Loan? Loan = null, PolicyResult? Policy = null);

public sealed class LoanService(
    IBookCatalog books,
    IMemberDirectory members,
    ILoanRepository loans,
    LoanPolicy policy,
    DueDateCalculator dueDates,
    LateFeeCalculator lateFees,
    TimeProvider clock)
{
    public BorrowOutcome Borrow(string memberId, string isbn)
    {
        if (members.Find(memberId) is not { } member)
        {
            return new BorrowOutcome(BorrowStatus.MemberNotFound);
        }

        if (books.Find(isbn) is not { } book)
        {
            return new BorrowOutcome(BorrowStatus.BookNotFound);
        }

        var active = loans.Active();
        var verdict = policy.CanBorrow(
            member,
            book,
            activeLoansForMember: active.Count(l => l.MemberId == member.Id),
            activeLoansForBook: active.Count(l => l.Isbn == book.Isbn));

        if (!verdict.IsAllowed)
        {
            return new BorrowOutcome(BorrowStatus.NotAllowed, Policy: verdict);
        }

        var today = Today();
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            Isbn = book.Isbn,
            MemberId = member.Id,
            BorrowedOn = today,
            DueOn = dueDates.DueDate(today, member.Type),
        };

        loans.Save(loan);
        return new BorrowOutcome(BorrowStatus.Borrowed, loan, verdict);
    }

    /// <summary>Records a return and adds any late fee to the member's outstanding fines.</summary>
    public Loan? Return(Guid loanId)
    {
        if (loans.Find(loanId) is not { IsActive: true } loan)
        {
            return null;
        }

        var member = members.Find(loan.MemberId)!;
        loan.ReturnedOn = Today();
        loan.LateFee = lateFees.Calculate(loan.DueOn, loan.ReturnedOn.Value, member.Type);
        member.OutstandingFines += loan.LateFee;

        loans.Save(loan);
        return loan;
    }

    public Loan? Find(Guid loanId) => loans.Find(loanId);

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
