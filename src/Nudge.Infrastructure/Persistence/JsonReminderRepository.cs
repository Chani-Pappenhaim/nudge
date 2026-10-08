using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Infrastructure.Persistence;

/// <summary>Keeps reminders in memory and persists every change to a JSON file.</summary>
internal sealed class JsonReminderRepository(JsonFileStore<ReminderRecord> store) : IReminderRepository
{
    private Dictionary<Guid, Reminder>? _cache;

    private Dictionary<Guid, Reminder> Items =>
        _cache ??= store.Load().Select(r => r.ToDomain()).ToDictionary(r => r.Id);

    public IReadOnlyList<Reminder> GetAll() => [.. Items.Values];

    public void Save(Reminder reminder)
    {
        Items[reminder.Id] = reminder;
        Persist();
    }

    public void Delete(Guid id)
    {
        if (Items.Remove(id))
        {
            Persist();
        }
    }

    private void Persist() => store.Save(Items.Values.Select(ReminderRecord.FromDomain));
}
