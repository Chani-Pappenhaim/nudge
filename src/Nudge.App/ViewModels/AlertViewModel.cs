using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>
/// A reminder that is due right now. The alert closes itself once the reminder is handled,
/// whether from this alert or from elsewhere (edited, paused or deleted).
/// </summary>
public sealed partial class AlertViewModel : IDisposable
{
    public const int DefaultSnoozeMinutes = 5;

    private const int MaxSoundRepeats = 8;
    private static readonly TimeSpan SoundRepeatInterval = TimeSpan.FromSeconds(4);

    private readonly Reminder _reminder;
    private readonly ReminderService _reminders;
    private readonly ISoundPlayer _sounds;
    private readonly TimeProvider _time;
    private ITimer? _soundTimer;
    private int _soundsPlayed;
    private bool _isResolved;

    public AlertViewModel(Reminder reminder, ReminderService reminders, ISoundPlayer sounds, TimeProvider time)
    {
        _reminder = reminder;
        _reminders = reminders;
        _sounds = sounds;
        _time = time;
        WhenText = Labels.When(reminder.DueAt, reminders.Now);
    }

    /// <summary>Raised when the alert has been handled and its window should close.</summary>
    public event EventHandler? CloseRequested;

    public static IReadOnlyList<Choice<int>> SnoozeOptions { get; } =
        [.. new[] { 5, 10, 30, 60 }.Select(minutes => new Choice<int>(minutes, $"{minutes} דק׳"))];

    public string Title => _reminder.Title;

    public string Note => _reminder.Note;

    public bool HasNote => _reminder.Note.Length > 0;

    public string WhenText { get; }

    public string RecurrenceText => Labels.Describe(_reminder.Recurrence);

    public void Start()
    {
        _reminders.Changed += OnRemindersChanged;
        if (_reminder.Sound != AlertSound.Silent)
        {
            _soundTimer = _time.CreateTimer(_ => PlaySound(), null, TimeSpan.Zero, SoundRepeatInterval);
        }
    }

    /// <summary>Closing the alert without choosing counts as a short snooze, so it is never silently lost.</summary>
    public void OnClosing()
    {
        if (!_isResolved)
        {
            Snooze(DefaultSnoozeMinutes);
        }
    }

    /// <summary>Closes the alert and leaves the reminder due, e.g. when the app exits.</summary>
    public void Abandon() => Resolve(() => { });

    public void Dispose()
    {
        _reminders.Changed -= OnRemindersChanged;
        _soundTimer?.Dispose();
        _soundTimer = null;
    }

    [RelayCommand]
    private void Complete() => Resolve(() => _reminders.Complete(_reminder.Id));

    [RelayCommand]
    private void Snooze(int minutes) => Resolve(() => _reminders.Snooze(_reminder.Id, TimeSpan.FromMinutes(minutes)));

    private void Resolve(Action action)
    {
        if (_isResolved)
        {
            return;
        }
        _isResolved = true;
        Dispose();
        action();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnRemindersChanged(object? sender, EventArgs e)
    {
        if (_reminders.Find(_reminder.Id) is not { } current || !current.IsDue(_reminders.Now))
        {
            Abandon();
        }
    }

    private void PlaySound()
    {
        if (Interlocked.Increment(ref _soundsPlayed) > MaxSoundRepeats)
        {
            _soundTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }
        _sounds.Play(_reminder.Sound);
    }
}
