using Nudge.App.Presentation;
using Nudge.Core.Models;

namespace Nudge.App.ViewModels;

/// <summary>A debt as shown in the debts tab.</summary>
public sealed class DebtItemViewModel(Debt debt, Reminder? reminder, DateTime now)
{
    public Debt Debt { get; } = debt;

    public bool IsOwedToMe => Debt.Direction == DebtDirection.OwedToMe;

    public string DirectionText => IsOwedToMe ? "חייבים לי" : "אני חייבת";

    public string Person => Debt.Person;

    public string Description => Debt.Description;

    public bool HasDescription => Debt.Description.Length > 0;

    public bool IsMoney => Debt.IsMoney;

    public bool IsOpen => !Debt.IsSettled;

    public bool IsSettled => Debt.IsSettled;

    /// <summary>What is owed: the money left of the full amount, or that the debt is an item.</summary>
    public string AmountText => (Debt.IsMoney, Debt.IsSettled, Debt.Paid > 0m) switch
    {
        (false, _, _) => "חפץ",
        (true, true, _) => MoneyInput.Format(Debt.Amount!.Value),
        (true, false, true) => $"יתרה {MoneyInput.Format(Debt.Remaining)} מתוך {MoneyInput.Format(Debt.Amount!.Value)}",
        (true, false, false) => MoneyInput.Format(Debt.Amount!.Value),
    };

    public string AmountGlyph => Debt.IsMoney ? Glyphs.Money : Glyphs.Item;

    public string? ReminderText => reminder is null
        ? null
        : reminder.IsActive ? $"תזכורת {Labels.When(reminder.DueAt, now)}" : "תזכורת מושהית";

    public bool HasReminder => reminder is not null;

    public string ReminderToolTip => reminder is null ? "תזכורת" : "עריכת התזכורת";

    public string? SettledText => Debt.SettledAt is { } at ? $"הוחזר {Labels.When(at, now)}" : null;

    public bool CanUndoPayment => IsOpen && Debt.Payments.Count > 0;

    public string UndoPaymentToolTip => Debt.Payments.Count > 0
        ? $"ביטול ההחזר האחרון ({MoneyInput.Format(Debt.Payments[^1].Amount)})"
        : string.Empty;
}
