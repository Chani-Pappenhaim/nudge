using Nudge.Core.Models;

namespace Nudge.Core.Tests;

public class ReminderTests
{
    private static readonly DateTime Scheduled = new(2026, 10, 1, 9, 0, 0);

    private static Reminder Daily() => Reminder.Create("Medicine", Scheduled, Recurrence.Daily);

    [Fact]
    public void Create_TrimsTextAndRejectsBlankTitle()
    {
        var reminder = Reminder.Create("  Water  ", Scheduled, Recurrence.Once, note: " glass ");

        Assert.Equal("Water", reminder.Title);
        Assert.Equal("glass", reminder.Note);
        Assert.Throws<ArgumentException>(() => Reminder.Create("   ", Scheduled, Recurrence.Once));
    }

    [Fact]
    public void IsDue_RequiresActiveAndReachedDueTime()
    {
        var reminder = Daily();

        Assert.False(reminder.IsDue(Scheduled.AddMinutes(-1)));
        Assert.True(reminder.IsDue(Scheduled));
        Assert.False(reminder.Pause().IsDue(Scheduled));
    }

    [Fact]
    public void Complete_OneTimeReminder_Deactivates()
    {
        var done = Reminder.Create("Call", Scheduled, Recurrence.Once).Complete(Scheduled);

        Assert.False(done.IsActive);
    }

    [Fact]
    public void Complete_AfterSnooze_KeepsTheOriginalCadence()
    {
        var snoozed = Daily().Snooze(Scheduled, TimeSpan.FromMinutes(30));

        var done = snoozed.Complete(Scheduled.AddMinutes(30));

        Assert.Equal(Scheduled.AddDays(1), done.ScheduledAt);
        Assert.Null(done.SnoozedUntil);
        Assert.True(done.IsActive);
    }

    [Fact]
    public void Snooze_PostponesDueTimeOnly()
    {
        var now = Scheduled.AddMinutes(2);

        var snoozed = Daily().Snooze(now, TimeSpan.FromMinutes(10));

        Assert.Equal(now.AddMinutes(10), snoozed.DueAt);
        Assert.Equal(Scheduled, snoozed.ScheduledAt);
        Assert.Throws<ArgumentOutOfRangeException>(() => Daily().Snooze(now, TimeSpan.Zero));
    }

    [Fact]
    public void SnoozeUntil_PostponesToTheChosenMomentAndKeepsTheCadence()
    {
        var now = Scheduled.AddMinutes(2);
        var nextWeek = Scheduled.AddDays(7);

        var snoozed = Daily().SnoozeUntil(nextWeek, now);
        var done = snoozed.Complete(nextWeek.AddMinutes(1));

        Assert.Equal(nextWeek, snoozed.DueAt);
        Assert.Equal(Scheduled, snoozed.ScheduledAt);
        Assert.Equal(Scheduled.AddDays(8), done.ScheduledAt);
        Assert.Null(done.SnoozedUntil);
    }

    [Fact]
    public void SnoozeUntil_RejectsAMomentThatIsNotInTheFuture()
    {
        var now = Scheduled.AddMinutes(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => Daily().SnoozeUntil(now, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Daily().SnoozeUntil(now.AddMinutes(-1), now));
    }

    [Fact]
    public void Resume_RepeatingReminder_SkipsOccurrencesMissedWhilePaused()
    {
        var resumed = Daily().Pause().Resume(Scheduled.AddDays(3).AddHours(1));

        Assert.True(resumed.IsActive);
        Assert.Equal(Scheduled.AddDays(4), resumed.ScheduledAt);
    }

    [Fact]
    public void Resume_PastOneTimeReminder_BecomesDueImmediately()
    {
        var now = Scheduled.AddDays(1);

        var resumed = Reminder.Create("Call", Scheduled, Recurrence.Once).Pause().Resume(now);

        Assert.True(resumed.IsDue(now));
    }

    [Fact]
    public void Pause_ClearsSnooze()
    {
        var paused = Daily().Snooze(Scheduled, TimeSpan.FromMinutes(5)).Pause();

        Assert.False(paused.IsSnoozed);
    }
}
