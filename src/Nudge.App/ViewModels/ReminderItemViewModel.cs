using Nudge.App.Presentation;
using Nudge.Core.Models;

namespace Nudge.App.ViewModels;

/// <summary>A reminder as shown in the main list.</summary>
public sealed class ReminderItemViewModel(Reminder reminder, DateTime now)
{
    public Reminder Reminder { get; } = reminder;

    public string Title => Reminder.Title;

    public string Note => Reminder.Note;

    public bool HasNote => Reminder.Note.Length > 0;

    public bool IsActive => Reminder.IsActive;

    public string WhenText => (Reminder.IsActive, Reminder.IsSnoozed) switch
    {
        (false, _) => "מושהית",
        (true, true) => $"נדחתה עד {Labels.When(Reminder.DueAt, now)}",
        (true, false) => Labels.When(Reminder.DueAt, now),
    };

    public string RecurrenceText => Labels.Describe(Reminder.Recurrence);

    public string ToggleGlyph => IsActive ? Glyphs.Pause : Glyphs.Play;

    public string ToggleToolTip => IsActive ? "השהיה" : "הפעלה מחדש";
}
