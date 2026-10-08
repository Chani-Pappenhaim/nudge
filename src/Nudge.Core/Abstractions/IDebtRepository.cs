using Nudge.Core.Models;

namespace Nudge.Core.Abstractions;

/// <summary>Durable storage for debts.</summary>
public interface IDebtRepository
{
    IReadOnlyList<Debt> GetAll();

    /// <summary>Adds the debt, or replaces the stored debt with the same id.</summary>
    void Save(Debt debt);

    void Delete(Guid id);
}
