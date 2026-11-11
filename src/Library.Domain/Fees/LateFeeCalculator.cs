using Library.Domain.Model;

namespace Library.Domain.Fees;

/// <summary>
/// Late fees as set by the library board (policy LIB-12).
/// </summary>
public sealed class LateFeeCalculator
{
    private const int GraceDays = 2;
    private const int SeniorFeeFreeDays = 7;
    private const decimal DailyFee = 0.25m;
    private const decimal StudentRate = 0.5m;
    private const decimal MaxFeePerLoan = 10.00m;

    public decimal Calculate(DateOnly dueOn, DateOnly returnedOn, MemberType memberType)
    {
        var daysLate = returnedOn.DayNumber - dueOn.DayNumber;
        if (daysLate <= GraceDays)
        {
            return 0m;
        }

        // Past the grace period every late day counts, including the grace days themselves.
        var chargeableDays = memberType == MemberType.Senior ? daysLate - SeniorFeeFreeDays : daysLate;
        if (chargeableDays <= 0)
        {
            return 0m;
        }

        var fee = chargeableDays * DailyFee * (memberType == MemberType.Student ? StudentRate : 1m);
        return Math.Min(Math.Round(fee, 2, MidpointRounding.AwayFromZero), MaxFeePerLoan);
    }
}
