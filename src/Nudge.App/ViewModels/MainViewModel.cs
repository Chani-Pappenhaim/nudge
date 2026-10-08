using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.App.Services;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>The reminder list, debts, history and settings shown in the main window.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ReminderService _reminders;
    private readonly DebtService _debts;
    private readonly IStartupManager _startup;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _noReminders;

    [ObservableProperty]
    private bool _noHistory;

    [ObservableProperty]
    private bool _showIOwe = true;

    [ObservableProperty]
    private bool _showOwedToMe;

    [ObservableProperty]
    private bool _showSettled;

    [ObservableProperty]
    private bool _noDebtsInView;

    [ObservableProperty]
    private string _debtsEmptyTitle = string.Empty;

    [ObservableProperty]
    private string _debtsEmptyHint = string.Empty;

    [ObservableProperty]
    private string _owedToMeTotal = string.Empty;

    [ObservableProperty]
    private string _iOweTotal = string.Empty;

    [ObservableProperty]
    private string _settledHeader = string.Empty;

    public MainViewModel(ReminderService reminders, DebtService debts, IStartupManager startup, IDialogService dialogs)
    {
        _reminders = reminders;
        _debts = debts;
        _startup = startup;
        _dialogs = dialogs;
        _startWithWindows = startup.IsEnabled;
        // Debt cards show their reminder's time, so a reminder change refreshes them too.
        _reminders.Changed += (_, _) => Refresh();
        _debts.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<ReminderItemViewModel> Reminders { get; } = [];

    public ObservableCollection<HistoryItemViewModel> History { get; } = [];

    public ObservableCollection<DebtItemViewModel> OwedToMe { get; } = [];

    public ObservableCollection<DebtItemViewModel> IOwe { get; } = [];

    public ObservableCollection<DebtItemViewModel> SettledDebts { get; } = [];

    /// <summary>Rebuilds the lists; relative times such as "today" depend on the current moment.</summary>
    public void Refresh()
    {
        var now = _reminders.Now;
        Reset(Reminders, _reminders.GetAll().Select(r => new ReminderItemViewModel(r, now)));
        Reset(History, _reminders.GetHistory().Select(h => new HistoryItemViewModel(h, now)));
        NoReminders = Reminders.Count == 0;
        NoHistory = History.Count == 0;

        var open = _debts.GetOpen();
        Reset(OwedToMe, open.Where(d => d.Direction == DebtDirection.OwedToMe).Select(d => Item(d, now)));
        Reset(IOwe, open.Where(d => d.Direction == DebtDirection.IOwe).Select(d => Item(d, now)));
        Reset(SettledDebts, _debts.GetSettled().Select(d => Item(d, now)));
        OwedToMeTotal = Total("חייבים לי", OwedToMe);
        IOweTotal = Total("אני חייבת", IOwe);
        SettledHeader = SettledDebts.Count == 0 ? "הוחזרו" : $"הוחזרו ({SettledDebts.Count})";
        UpdateDebtsEmptyState();
    }

    partial void OnStartWithWindowsChanged(bool value) => _startup.SetEnabled(value);

    partial void OnShowIOweChanged(bool value) => UpdateDebtsEmptyState();

    partial void OnShowOwedToMeChanged(bool value) => UpdateDebtsEmptyState();

    partial void OnShowSettledChanged(bool value) => UpdateDebtsEmptyState();

    /// <summary>Shows the message matching the selected debt list when that list is empty.</summary>
    private void UpdateDebtsEmptyState()
    {
        (NoDebtsInView, DebtsEmptyTitle, DebtsEmptyHint) = (ShowOwedToMe, ShowSettled) switch
        {
            (true, _) => (OwedToMe.Count == 0, "אף אחד לא חייב לי כרגע",
                "כסף או חפץ שהשאלת — לחיצה על \"חוב חדש\" תרשום אותו."),
            (_, true) => (SettledDebts.Count == 0, "עדיין אין חובות שהוחזרו",
                "חוב שיסומן כהוחזר במלואו יעבור לכאן."),
            _ => (IOwe.Count == 0, "אין חובות פתוחים שלי",
                "כסף או חפץ ששאלת — לחיצה על \"חוב חדש\" תרשום אותו."),
        };
    }

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

    /// <summary>Starts a debt in the direction being viewed; settled debts are viewed from "I owe".</summary>
    [RelayCommand]
    private void NewDebt() =>
        ShowDebts(_dialogs.EditDebt(null, ShowOwedToMe ? DebtDirection.OwedToMe : DebtDirection.IOwe));

    [RelayCommand]
    private void EditDebt(DebtItemViewModel item) => ShowDebts(_dialogs.EditDebt(item.Debt));

    [RelayCommand]
    private void RecordPayment(DebtItemViewModel item) => _dialogs.RecordPayment(item.Debt);

    [RelayCommand]
    private void SettleDebt(DebtItemViewModel item) => _debts.Settle(item.Debt.Id);

    [RelayCommand]
    private void UndoPayment(DebtItemViewModel item) => _debts.UndoLastPayment(item.Debt.Id);

    [RelayCommand]
    private void ReopenDebt(DebtItemViewModel item) => _debts.Reopen(item.Debt.Id);

    /// <summary>Edits the debt's reminder, or creates one and links it when there is none.</summary>
    [RelayCommand]
    private void DebtReminder(DebtItemViewModel item)
    {
        if (_debts.FindReminder(item.Debt) is { } existing)
        {
            _dialogs.EditReminder(existing);
            return;
        }
        var debt = item.Debt;
        var what = debt.IsMoney ? MoneyInput.Format(debt.Remaining) : debt.Description;
        var title = debt.Direction == DebtDirection.IOwe
            ? $"להחזיר ל{debt.Person}: {what}"
            : $"לגבות מ{debt.Person}: {what}";
        var note = debt.IsMoney ? debt.Description : string.Empty;
        if (_dialogs.NewReminder(title, note) is { } created)
        {
            _debts.LinkReminder(debt.Id, created.Id);
        }
    }

    [RelayCommand]
    private void DeleteDebt(DebtItemViewModel item)
    {
        var message = item.HasReminder
            ? $"למחוק את החוב של \"{item.Person}\" ואת התזכורת שלו?"
            : $"למחוק את החוב של \"{item.Person}\"?";
        if (_dialogs.Confirm(message))
        {
            _debts.Delete(item.Debt.Id);
        }
    }

    /// <summary>Switches to the open debts of a direction, so a debt that was just saved stays in sight.</summary>
    private void ShowDebts(DebtDirection? direction)
    {
        if (direction is not { } saved)
        {
            return;
        }
        ShowIOwe = saved == DebtDirection.IOwe;
        ShowOwedToMe = saved == DebtDirection.OwedToMe;
        ShowSettled = false;
    }

    private DebtItemViewModel Item(Debt debt, DateTime now) => new(debt, _debts.FindReminder(debt), now);

    /// <summary>Sums the money still owed in one direction and counts the items, e.g. "חייבים לי ₪350 · 2 חפצים";
    /// just the label when there is nothing.</summary>
    private static string Total(string label, IEnumerable<DebtItemViewModel> items)
    {
        var debts = items.Select(i => i.Debt).ToList();
        var parts = new List<string> { label };
        if (debts.Any(d => d.IsMoney))
        {
            parts.Add(MoneyInput.Format(debts.Sum(d => d.Remaining)));
        }
        var count = debts.Count(d => !d.IsMoney);
        if (count > 0)
        {
            parts.Add(count == 1 ? "חפץ אחד" : $"{count} חפצים");
        }
        return parts.Count switch
        {
            1 => label,
            2 => $"{label} {parts[1]}",
            _ => $"{label} {parts[1]} · {parts[2]}",
        };
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
