using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Core.Services;

/// <summary>
/// Application use cases for tracking debts. A debt's linked reminder lives as long as the debt is open:
/// it is removed once the debt is returned or deleted.
/// </summary>
public sealed class DebtService(IDebtRepository debts, ReminderService reminders, TimeProvider timeProvider)
{
    /// <summary>Raised after any debt change.</summary>
    public event EventHandler? Changed;

    private DateTime Now => timeProvider.GetLocalNow().DateTime;

    /// <summary>Returns the open debts, oldest first.</summary>
    public IReadOnlyList<Debt> GetOpen() =>
        [.. debts.GetAll().Where(d => !d.IsSettled).OrderBy(d => d.CreatedAt)];

    /// <summary>Returns the returned debts, most recently returned first.</summary>
    public IReadOnlyList<Debt> GetSettled() =>
        [.. debts.GetAll().Where(d => d.IsSettled).OrderByDescending(d => d.SettledAt)];

    public Debt? Find(Guid id) => debts.GetAll().FirstOrDefault(d => d.Id == id);

    /// <summary>The reminder linked to the debt, unless it was deleted in the meantime.</summary>
    public Reminder? FindReminder(Debt debt) => debt.ReminderId is { } id ? reminders.Find(id) : null;

    public Debt Add(DebtDirection direction, string person, string? description, decimal? amount)
    {
        var debt = Debt.Create(direction, person, description, amount, Now);
        debts.Save(debt);
        OnChanged();
        return debt;
    }

    public void Edit(Guid id, DebtDirection direction, string person, string? description, decimal? amount) =>
        Update(id, d => d.Edit(direction, person, description, amount));

    public void RecordPayment(Guid id, decimal amount) => Update(id, d => d.RecordPayment(amount, Now));

    public void Settle(Guid id) => Update(id, d => d.Settle(Now));

    public void UndoLastPayment(Guid id) => Update(id, d => d.UndoLastPayment());

    public void Reopen(Guid id) => Update(id, d => d.Reopen());

    public void LinkReminder(Guid id, Guid reminderId) => Update(id, d => d.LinkReminder(reminderId));

    public void Delete(Guid id)
    {
        if (Find(id) is not { } debt)
        {
            return;
        }
        debts.Delete(id);
        OnChanged();
        if (debt.ReminderId is { } reminderId)
        {
            reminders.Delete(reminderId);
        }
    }

    private void Update(Guid id, Func<Debt, Debt> change)
    {
        if (Find(id) is not { } debt)
        {
            return;
        }
        var updated = change(debt);
        var finishedReminder = updated.IsSettled && !debt.IsSettled ? updated.ReminderId : null;
        if (finishedReminder is not null)
        {
            updated = updated.LinkReminder(null);
        }
        debts.Save(updated);
        OnChanged();
        if (finishedReminder is { } reminderId)
        {
            // A returned debt needs no more reminding; the reminder counts as done.
            reminders.Delete(reminderId, HistoryAction.Completed);
        }
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
