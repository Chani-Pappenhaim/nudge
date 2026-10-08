using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Core.Services;

/// <summary>Application use cases for managing reminders and their history.</summary>
public sealed class ReminderService(
    IReminderRepository reminders,
    IHistoryRepository history,
    TimeProvider timeProvider)
{
    /// <summary>Raised after any reminder or history change.</summary>
    public event EventHandler? Changed;

    public DateTime Now => timeProvider.GetLocalNow().DateTime;

    /// <summary>Returns all reminders, active ones first, ordered by when they are due.</summary>
    public IReadOnlyList<Reminder> GetAll() =>
        [.. reminders.GetAll().OrderByDescending(r => r.IsActive).ThenBy(r => r.DueAt)];

    public IReadOnlyList<Reminder> GetDue()
    {
        var now = Now;
        return [.. reminders.GetAll().Where(r => r.IsDue(now)).OrderBy(r => r.DueAt)];
    }

    public Reminder? Find(Guid id) => reminders.GetAll().FirstOrDefault(r => r.Id == id);

    /// <summary>Returns the history log, newest first.</summary>
    public IReadOnlyList<HistoryEntry> GetHistory() =>
        [.. history.GetAll().OrderByDescending(h => h.At)];

    public void Save(Reminder reminder)
    {
        reminders.Save(reminder);
        OnChanged();
    }

    public void Delete(Guid id)
    {
        if (Find(id) is not { } reminder)
        {
            return;
        }
        reminders.Delete(id);
        history.Add(new HistoryEntry(Now, reminder.Title, HistoryAction.Deleted));
        OnChanged();
    }

    public void Complete(Guid id) =>
        Update(id, r => r.Complete(Now), r => new HistoryEntry(Now, r.Title, HistoryAction.Completed));

    public void Snooze(Guid id, TimeSpan duration) =>
        Update(id, r => r.Snooze(Now, duration),
            r => new HistoryEntry(Now, r.Title, HistoryAction.Snoozed, duration));

    public void SetActive(Guid id, bool isActive) =>
        Update(id, r => isActive ? r.Resume(Now) : r.Pause());

    public void ClearHistory()
    {
        history.Clear();
        OnChanged();
    }

    private void Update(Guid id, Func<Reminder, Reminder> change, Func<Reminder, HistoryEntry>? log = null)
    {
        if (Find(id) is not { } reminder)
        {
            return;
        }
        reminders.Save(change(reminder));
        if (log is not null)
        {
            history.Add(log(reminder));
        }
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
