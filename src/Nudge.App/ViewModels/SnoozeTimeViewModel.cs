using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>Picks the date and time a due reminder is postponed to.</summary>
public sealed partial class SnoozeTimeViewModel(ReminderService reminders) : ObservableObject
{
    [ObservableProperty]
    private DateTime? _date;

    [ObservableProperty]
    private string _time = string.Empty;

    [ObservableProperty]
    private string? _error;

    /// <summary>Raised once a valid future moment was chosen; it is then available in <see cref="Result"/>.</summary>
    public event EventHandler? Picked;

    public DateTime? Result { get; private set; }

    public void Load(DateTime initial)
    {
        Date = initial.Date;
        Time = TimeInput.Format(initial);
        Error = null;
        Result = null;
    }

    [RelayCommand]
    private void Confirm()
    {
        Error = Validate(out var until);
        if (until is null)
        {
            return;
        }
        Result = until;
        Picked?.Invoke(this, EventArgs.Empty);
    }

    private string? Validate(out DateTime? until)
    {
        until = null;
        if (Date is not { } date)
        {
            return "יש לבחור תאריך.";
        }
        if (!TimeInput.TryParse(Time, out var time))
        {
            return TimeInput.InvalidMessage;
        }
        var chosen = date.Date + time.ToTimeSpan();
        if (chosen <= reminders.Now)
        {
            return "המועד שנבחר כבר עבר.";
        }
        until = chosen;
        return null;
    }
}
