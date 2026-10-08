using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Infrastructure.Persistence;

/// <summary>Persists the history log to a JSON file, keeping only the most recent entries.</summary>
internal sealed class JsonHistoryRepository(JsonFileStore<HistoryEntry> store) : IHistoryRepository
{
    public const int MaxEntries = 500;

    private List<HistoryEntry>? _cache;

    private List<HistoryEntry> Items => _cache ??= store.Load();

    public IReadOnlyList<HistoryEntry> GetAll() => [.. Items];

    public void Add(HistoryEntry entry)
    {
        Items.Add(entry);
        if (Items.Count > MaxEntries)
        {
            Items.RemoveRange(0, Items.Count - MaxEntries);
        }
        store.Save(Items);
    }

    public void Clear()
    {
        Items.Clear();
        store.Save(Items);
    }
}
