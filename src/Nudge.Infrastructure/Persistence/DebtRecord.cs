using Nudge.Core.Models;

namespace Nudge.Infrastructure.Persistence;

/// <summary>The stored shape of a debt, decoupled from the domain model.</summary>
internal sealed record DebtRecord(
    Guid Id,
    DebtDirection Direction,
    string Person,
    string Description,
    decimal? Amount,
    IReadOnlyList<DebtPaymentRecord>? Payments,
    DateTime CreatedAt,
    DateTime? SettledAt,
    Guid? ReminderId)
{
    public static DebtRecord FromDomain(Debt debt) => new(
        debt.Id,
        debt.Direction,
        debt.Person,
        debt.Description,
        debt.Amount,
        [.. debt.Payments.Select(p => new DebtPaymentRecord(p.At, p.Amount))],
        debt.CreatedAt,
        debt.SettledAt,
        debt.ReminderId);

    public Debt ToDomain() => new()
    {
        Id = Id,
        Direction = Direction,
        Person = Person,
        Description = Description,
        Amount = Amount,
        Payments = [.. (Payments ?? []).Select(p => new DebtPayment(p.At, p.Amount))],
        CreatedAt = CreatedAt,
        SettledAt = SettledAt,
        ReminderId = ReminderId,
    };
}

/// <summary>The stored shape of a debt payment.</summary>
internal sealed record DebtPaymentRecord(DateTime At, decimal Amount);
