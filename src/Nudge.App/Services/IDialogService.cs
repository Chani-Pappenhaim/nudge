using Nudge.Core.Models;

namespace Nudge.App.Services;

/// <summary>Dialogs that view models can request without depending on windows.</summary>
public interface IDialogService
{
    /// <summary>Opens the editor for a new reminder (null) or an existing one; returns it when it was saved.</summary>
    Reminder? EditReminder(Reminder? reminder);

    /// <summary>Opens the editor for a new reminder filled with suggested text; returns it when it was saved.</summary>
    Reminder? NewReminder(string title, string note);

    /// <summary>Opens the editor for an open debt, or for a new one (null) in the given direction;
    /// returns the direction it was saved under, or null when it was cancelled.</summary>
    DebtDirection? EditDebt(Debt? debt, DebtDirection newDebtDirection = DebtDirection.IOwe);

    /// <summary>Asks how much of a money debt was paid back and records it; true when it was recorded.</summary>
    bool RecordPayment(Debt debt);

    bool Confirm(string message);
}
