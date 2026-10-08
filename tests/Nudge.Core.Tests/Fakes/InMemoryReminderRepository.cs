using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Core.Tests.Fakes;

internal sealed class InMemoryReminderRepository : IReminderRepository
{
    private readonly Dictionary<Guid, Reminder> _items = [];

    public IReadOnlyList<Reminder> GetAll() => [.. _items.Values];

    public void Save(Reminder reminder) => _items[reminder.Id] = reminder;

    public void Delete(Guid id) => _items.Remove(id);
}
