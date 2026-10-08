using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Core.Tests.Fakes;

internal sealed class InMemoryDebtRepository : IDebtRepository
{
    private readonly Dictionary<Guid, Debt> _items = [];

    public IReadOnlyList<Debt> GetAll() => [.. _items.Values];

    public void Save(Debt debt) => _items[debt.Id] = debt;

    public void Delete(Guid id) => _items.Remove(id);
}
