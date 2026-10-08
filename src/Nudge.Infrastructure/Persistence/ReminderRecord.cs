using Nudge.Core.Models;

namespace Nudge.Infrastructure.Persistence;

/// <summary>The stored shape of a reminder, decoupled from the domain model.</summary>
internal sealed record ReminderRecord(
    Guid Id,
    string Title,
    string Note,
    DateTime ScheduledAt,
    DateTime? SnoozedUntil,
    RecurrenceKind Repeats,
    int RepeatEveryMinutes,
    AlertSound Sound,
    bool IsActive)
{
    public static ReminderRecord FromDomain(Reminder reminder) => new(
        reminder.Id,
        reminder.Title,
        reminder.Note,
        reminder.ScheduledAt,
        reminder.SnoozedUntil,
        reminder.Recurrence.Kind,
        (int)reminder.Recurrence.Interval.TotalMinutes,
        reminder.Sound,
        reminder.IsActive);

    public Reminder ToDomain() => new()
    {
        Id = Id,
        Title = Title,
        Note = Note,
        ScheduledAt = ScheduledAt,
        SnoozedUntil = SnoozedUntil,
        Recurrence = Recurrence.From(Repeats, TimeSpan.FromMinutes(RepeatEveryMinutes)),
        Sound = Sound,
        IsActive = IsActive,
    };
}
