using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Services;
using Nudge.Core.Abstractions;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>The reminder list, history and settings shown in the main window.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ReminderService _reminders;
    private readonly IStartupManager _startup;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _noReminders;

    [ObservableProperty]
    private bool _noHistory;

    public MainViewModel(ReminderService reminders, IStartupManager startup, IDialogService dialogs)
    {
        _reminders = reminders;
        _startup = startup;
        _dialogs = dialogs;
        _startWithWindows = startup.IsEnabled;
        _reminders.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<ReminderItemViewModel> Reminders { get; } = [];

    public ObservableCollection<HistoryItemViewModel> History { get; } = [];

    /// <summary>Rebuilds the lists; relative times such as "today" depend on the current moment.</summary>
    public void Refresh()
    {
        var now = _reminders.Now;
        Reset(Reminders, _reminders.GetAll().Select(r => new ReminderItemViewModel(r, now)));
        Reset(History, _reminders.GetHistory().Select(h => new HistoryItemViewModel(h, now)));
        NoReminders = Reminders.Count == 0;
        NoHistory = History.Count == 0;
    }

    partial void OnStartWithWindowsChanged(bool value) => _startup.SetEnabled(value);

    [RelayCommand]
    private void NewReminder() => _dialogs.EditReminder(null);

    [RelayCommand]
    private void Edit(ReminderItemViewModel item) => _dialogs.EditReminder(item.Reminder);

    [RelayCommand]
    private void TogglePause(ReminderItemViewModel item) => _reminders.SetActive(item.Reminder.Id, !item.IsActive);

    [RelayCommand]
    private void Delete(ReminderItemViewModel item)
    {
        if (_dialogs.Confirm($"למחוק את התזכורת \"{item.Title}\"?"))
        {
            _reminders.Delete(item.Reminder.Id);
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        if (_dialogs.Confirm("לנקות את כל ההיסטוריה?"))
        {
            _reminders.ClearHistory();
        }
    }

    private static void Reset<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
