namespace Nudge.Core.Models;

/// <summary>
/// A reminder and its lifecycle. Instances are immutable; every state change returns a new instance.
/// </summary>
public sealed record Reminder
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string Note { get; init; } = string.Empty;

    /// <summary>The occurrence on the reminder's regular cadence, unaffected by snoozing.</summary>
    public required DateTime ScheduledAt { get; init; }

    /// <summary>When set, the reminder was postponed and fires at this time instead.</summary>
    public DateTime? SnoozedUntil { get; init; }

    public Recurrence Recurrence { get; init; } = Recurrence.Once;

    public AlertSound Sound { get; init; } = AlertSound.Default;

    public bool IsActive { get; init; } = true;

    public DateTime DueAt => SnoozedUntil ?? ScheduledAt;

    public bool IsSnoozed => SnoozedUntil.HasValue;

    public static Reminder Create(string title, DateTime scheduledAt, Recurrence recurrence,
        AlertSound sound = AlertSound.Default, string? note = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new Reminder
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Note = note?.Trim() ?? string.Empty,
            ScheduledAt = scheduledAt,
            Recurrence = recurrence,
            Sound = sound,
        };
    }

    public bool IsDue(DateTime now) => IsActive && DueAt <= now;

    /// <summary>Marks the current occurrence as done: repeating reminders advance, one-time reminders deactivate.</summary>
    public Reminder Complete(DateTime now)
    {
        var next = Recurrence.NextOccurrence(ScheduledAt, now);
        return next is null
            ? this with { IsActive = false, SnoozedUntil = null }
            : this with { ScheduledAt = next.Value, SnoozedUntil = null };
    }

    public Reminder Snooze(DateTime now, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        return this with { SnoozedUntil = now + duration };
    }

    /// <summary>Postpones the current occurrence to a specific moment, which must be in the future.</summary>
    public Reminder SnoozeUntil(DateTime until, DateTime now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(until, now);
        return this with { SnoozedUntil = until };
    }

    public Reminder Pause() => this with { IsActive = false, SnoozedUntil = null };

    /// <summary>Reactivates the reminder; a repeating reminder skips occurrences missed while paused.</summary>
    public Reminder Resume(DateTime now)
    {
        if (IsActive)
        {
            return this;
        }
        var scheduledAt = ScheduledAt <= now && Recurrence.IsRepeating
            ? Recurrence.NextOccurrence(ScheduledAt, now)!.Value
            : ScheduledAt;
        return this with { IsActive = true, ScheduledAt = scheduledAt };
    }
}
