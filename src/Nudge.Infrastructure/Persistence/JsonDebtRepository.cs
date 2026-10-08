using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Infrastructure.Persistence;

/// <summary>Keeps debts in memory and persists every change to a JSON file.</summary>
internal sealed class JsonDebtRepository(JsonFileStore<DebtRecord> store) : IDebtRepository
{
    private Dictionary<Guid, Debt>? _cache;

    private Dictionary<Guid, Debt> Items =>
        _cache ??= store.Load().Select(r => r.ToDomain()).ToDictionary(d => d.Id);

    public IReadOnlyList<Debt> GetAll() => [.. Items.Values];

    public void Save(Debt debt)
    {
        Items[debt.Id] = debt;
        Persist();
    }

    public void Delete(Guid id)
    {
        if (Items.Remove(id))
        {
            Persist();
        }
    }

    private void Persist() => store.Save(Items.Values.Select(DebtRecord.FromDomain));
}
