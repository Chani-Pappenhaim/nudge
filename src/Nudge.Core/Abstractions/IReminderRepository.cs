using Nudge.Core.Models;

namespace Nudge.Core.Abstractions;

/// <summary>Durable storage for reminders.</summary>
public interface IReminderRepository
{
    IReadOnlyList<Reminder> GetAll();

    /// <summary>Adds the reminder, or replaces the stored reminder with the same id.</summary>
    void Save(Reminder reminder);

    void Delete(Guid id);
}
