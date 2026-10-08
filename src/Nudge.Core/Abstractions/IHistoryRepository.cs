using Nudge.Core.Models;

namespace Nudge.Core.Abstractions;

/// <summary>Durable storage for the reminder history log.</summary>
public interface IHistoryRepository
{
    IReadOnlyList<HistoryEntry> GetAll();

    void Add(HistoryEntry entry);

    void Clear();
}
