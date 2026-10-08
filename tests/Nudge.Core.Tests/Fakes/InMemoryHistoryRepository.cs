using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Core.Tests.Fakes;

internal sealed class InMemoryHistoryRepository : IHistoryRepository
{
    private readonly List<HistoryEntry> _items = [];

    public IReadOnlyList<HistoryEntry> GetAll() => [.. _items];

    public void Add(HistoryEntry entry) => _items.Add(entry);

    public void Clear() => _items.Clear();
}
