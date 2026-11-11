using Library.Domain.Model;

namespace Library.Domain.Lending;

/// <summary>The days the library is closed: every Sunday, plus the public holidays below.</summary>
public sealed class LibraryCalendar
{
    private static readonly HashSet<DateOnly> Holidays =
    [
        new(2026, 1, 1),
        new(2026, 5, 25),
        new(2026, 7, 4),
        new(2026, 9, 7),
        new(2026, 11, 26),
        new(2026, 12, 25),
        new(2027, 1, 1),
    ];

    public bool IsClosed(DateOnly date) => date.DayOfWeek == DayOfWeek.Sunday || Holidays.Contains(date);
}

/// <summary>
/// Works out when a loan is due from the member type and the library calendar.
/// </summary>
public sealed class DueDateCalculator(LibraryCalendar calendar)
{
    public const int StandardLoanDays = 14;
    public const int StudentLoanDays = 21;

    public DateOnly DueDate(DateOnly borrowedOn, MemberType memberType)
    {
        var due = borrowedOn.AddDays(memberType == MemberType.Student ? StudentLoanDays : StandardLoanDays);

        if (calendar.IsClosed(due))
        {
            due = due.AddDays(1);
        }

        return due;
    }
}
