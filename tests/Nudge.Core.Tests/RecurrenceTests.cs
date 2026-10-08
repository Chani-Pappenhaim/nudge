using Nudge.Core.Models;

namespace Nudge.Core.Tests;

public class RecurrenceTests
{
    private static readonly DateTime Scheduled = new(2026, 10, 1, 9, 0, 0);

    [Fact]
    public void Once_HasNoNextOccurrence() =>
        Assert.Null(Recurrence.Once.NextOccurrence(Scheduled, Scheduled.AddHours(1)));

    [Fact]
    public void Daily_SkipsMissedDaysAndKeepsTimeOfDay()
    {
        var next = Recurrence.Daily.NextOccurrence(Scheduled, new DateTime(2026, 10, 8, 12, 0, 0));

        Assert.Equal(new DateTime(2026, 10, 9, 9, 0, 0), next);
    }

    [Fact]
    public void Daily_OnTime_ReturnsTheFollowingDay()
    {
        var next = Recurrence.Daily.NextOccurrence(Scheduled, Scheduled);

        Assert.Equal(Scheduled.AddDays(1), next);
    }

    [Fact]
    public void Weekly_AdvancesBySevenDays()
    {
        var next = Recurrence.Weekly.NextOccurrence(Scheduled, Scheduled.AddMinutes(5));

        Assert.Equal(Scheduled.AddDays(7), next);
    }

    [Fact]
    public void Interval_CatchesUpToTheNextSlotAfterNow()
    {
        var every45 = Recurrence.Every(TimeSpan.FromMinutes(45));

        var next = every45.NextOccurrence(Scheduled, Scheduled.AddMinutes(100));

        Assert.Equal(Scheduled.AddMinutes(135), next);
    }

    [Fact]
    public void NextOccurrence_BeforeScheduledTime_AdvancesOneStep()
    {
        var next = Recurrence.Daily.NextOccurrence(Scheduled, Scheduled.AddHours(-3));

        Assert.Equal(Scheduled.AddDays(1), next);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(60 * 24 * 8)]
    public void Every_RejectsIntervalsOutOfRange(double minutes) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Recurrence.Every(TimeSpan.FromMinutes(minutes)));

    [Fact]
    public void From_RestoresEveryKind()
    {
        Assert.Same(Recurrence.Daily, Recurrence.From(RecurrenceKind.Daily, TimeSpan.Zero));
        Assert.Equal(Recurrence.Every(TimeSpan.FromMinutes(20)),
            Recurrence.From(RecurrenceKind.Interval, TimeSpan.FromMinutes(20)));
    }
}
