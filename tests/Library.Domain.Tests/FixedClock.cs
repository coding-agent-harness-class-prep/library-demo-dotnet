namespace Library.Domain.Tests;

/// <summary>A clock the test can set and move forward.</summary>
public sealed class FixedClock(DateOnly today) : TimeProvider
{
    public DateOnly Today { get; set; } = today;

    public override DateTimeOffset GetUtcNow() => new(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
}
