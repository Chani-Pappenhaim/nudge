using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>Creates a new reminder or edits an existing one.</summary>
public sealed partial class ReminderEditorViewModel(ReminderService reminders, ISoundPlayer sounds) : ObservableObject
{
    private static readonly TimeSpan DefaultLeadTime = TimeSpan.FromMinutes(5);

    private Reminder? _existing;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private DateTime? _date;

    [ObservableProperty]
    private string _time = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInterval))]
    private RecurrenceKind _recurrence = RecurrenceKind.Once;

    [ObservableProperty]
    private string _intervalMinutes = "30";

    [ObservableProperty]
    private AlertSound _sound = AlertSound.Default;

    [ObservableProperty]
    private string? _error;

    /// <summary>Raised after the reminder was validated and stored.</summary>
    public event EventHandler? Saved;

    public static IReadOnlyList<Choice<RecurrenceKind>> RecurrenceOptions { get; } =
        [.. Enum.GetValues<RecurrenceKind>().Select(k => new Choice<RecurrenceKind>(k, Labels.Of(k)))];

    public static IReadOnlyList<Choice<AlertSound>> SoundOptions { get; } =
        [.. Enum.GetValues<AlertSound>().Select(s => new Choice<AlertSound>(s, Labels.Of(s)))];

    public string WindowTitle => _existing is null ? "תזכורת חדשה" : "עריכת תזכורת";

    public bool IsInterval => Recurrence == RecurrenceKind.Interval;

    public void Load(Reminder? reminder)
    {
        _existing = reminder;
        var at = reminder?.ScheduledAt ?? TimeInput.TruncateToMinute(reminders.Now + DefaultLeadTime);
        Title = reminder?.Title ?? string.Empty;
        Note = reminder?.Note ?? string.Empty;
        Date = at.Date;
        Time = TimeInput.Format(at);
        Recurrence = reminder?.Recurrence.Kind ?? RecurrenceKind.Once;
        if (reminder?.Recurrence.Kind == RecurrenceKind.Interval)
        {
            IntervalMinutes = ((int)reminder.Recurrence.Interval.TotalMinutes).ToString(CultureInfo.InvariantCulture);
        }
        Sound = reminder?.Sound ?? AlertSound.Default;
        Error = null;
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    private void PreviewSound() => sounds.Play(Sound);

    [RelayCommand]
    private void Save()
    {
        Error = Validate(out var reminder);
        if (reminder is null)
        {
            return;
        }
        reminders.Save(reminder);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Builds the reminder from the form, or returns a message describing what is wrong.</summary>
    private string? Validate(out Reminder? reminder)
    {
        reminder = null;
        if (string.IsNullOrWhiteSpace(Title))
        {
            return "יש להזין כותרת.";
        }
        if (Date is not { } date)
        {
            return "יש לבחור תאריך.";
        }
        if (!TimeInput.TryParse(Time, out var time))
        {
            return TimeInput.InvalidMessage;
        }
        if (BuildRecurrence() is not { } recurrence)
        {
            var max = (int)Core.Models.Recurrence.MaximumInterval.TotalMinutes;
            return $"המרווח חייב להיות מספר דקות בין 1 ל-{max}.";
        }

        var now = reminders.Now;
        var scheduledAt = date.Date + time.ToTimeSpan();
        if (scheduledAt <= now)
        {
            if (!recurrence.IsRepeating)
            {
                return "המועד שנבחר כבר עבר.";
            }
            // A repeating reminder that started in the past continues from its next occurrence.
            scheduledAt = recurrence.NextOccurrence(scheduledAt, now)!.Value;
        }

        var created = Reminder.Create(Title, scheduledAt, recurrence, Sound, Note);
        reminder = _existing is null ? created : created with { Id = _existing.Id };
        return null;
    }

    private Recurrence? BuildRecurrence()
    {
        if (Recurrence != RecurrenceKind.Interval)
        {
            return Core.Models.Recurrence.From(Recurrence, TimeSpan.Zero);
        }
        if (!int.TryParse(IntervalMinutes.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
        {
            return null;
        }
        var interval = TimeSpan.FromMinutes(minutes);
        return interval >= Core.Models.Recurrence.MinimumInterval && interval <= Core.Models.Recurrence.MaximumInterval
            ? Core.Models.Recurrence.Every(interval)
            : null;
    }
}
