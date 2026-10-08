namespace Nudge.Core.Models;

/// <summary>
/// Money or an item owed between the user and someone else. Instances are immutable;
/// every state change returns a new instance.
/// </summary>
public sealed record Debt
{
    public required Guid Id { get; init; }

    public required DebtDirection Direction { get; init; }

    /// <summary>Who owes the user, or whom the user owes.</summary>
    public required string Person { get; init; }

    /// <summary>What the debt is for; for an item, the item itself.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The amount of money owed, or null when the debt is an item.</summary>
    public decimal? Amount { get; init; }

    public IReadOnlyList<DebtPayment> Payments { get; init; } = [];

    public required DateTime CreatedAt { get; init; }

    /// <summary>When the debt was returned in full; null while it is open.</summary>
    public DateTime? SettledAt { get; init; }

    /// <summary>The reminder created for this debt, if any.</summary>
    public Guid? ReminderId { get; init; }

    public bool IsMoney => Amount.HasValue;

    public bool IsSettled => SettledAt.HasValue;

    public decimal Paid => Payments.Sum(p => p.Amount);

    /// <summary>The money still owed; zero for an item.</summary>
    public decimal Remaining => (Amount ?? 0m) - Paid;

    public static Debt Create(DebtDirection direction, string person, string? description, decimal? amount,
        DateTime createdAt) =>
        new Debt { Id = Guid.NewGuid(), Direction = direction, Person = string.Empty, CreatedAt = createdAt }
            .Edit(direction, person, description, amount);

    /// <summary>
    /// Changes the details of an open debt. A money amount must stay above what was already paid back,
    /// and an item needs a description saying what it is.
    /// </summary>
    public Debt Edit(DebtDirection direction, string person, string? description, decimal? amount)
    {
        EnsureOpen();
        ArgumentException.ThrowIfNullOrWhiteSpace(person);
        var text = description?.Trim() ?? string.Empty;
        if (amount is { } money)
        {
            amount = decimal.Round(money, 2);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount.Value, Paid, nameof(amount));
        }
        else
        {
            if (text.Length == 0)
            {
                throw new ArgumentException("An item debt needs a description of the item.", nameof(description));
            }
            if (Payments.Count > 0)
            {
                throw new InvalidOperationException("A debt that was partly paid back must stay a money debt.");
            }
        }
        return this with { Direction = direction, Person = person.Trim(), Description = text, Amount = amount };
    }

    /// <summary>Records part of a money debt as paid back; paying the rest settles the debt.</summary>
    public Debt RecordPayment(decimal amount, DateTime at)
    {
        EnsureOpen();
        if (!IsMoney)
        {
            throw new InvalidOperationException("Only a money debt can be paid back in parts.");
        }
        amount = decimal.Round(amount, 2);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, Remaining);
        var paid = this with { Payments = [.. Payments, new DebtPayment(at, amount)] };
        return paid.Remaining == 0m ? paid with { SettledAt = at } : paid;
    }

    /// <summary>Marks the debt as returned in full; the rest of a money debt is recorded as a final payment.</summary>
    public Debt Settle(DateTime at)
    {
        EnsureOpen();
        return IsMoney ? RecordPayment(Remaining, at) : this with { SettledAt = at };
    }

    /// <summary>Removes the latest payment of a money debt, reopening the debt if that payment settled it.</summary>
    public Debt UndoLastPayment()
    {
        if (Payments.Count == 0)
        {
            throw new InvalidOperationException("There is no payment to undo.");
        }
        return this with { Payments = [.. Payments.SkipLast(1)], SettledAt = null };
    }

    /// <summary>Undoes marking the debt as returned.</summary>
    public Debt Reopen()
    {
        if (!IsSettled)
        {
            throw new InvalidOperationException("The debt is still open.");
        }
        return IsMoney ? UndoLastPayment() : this with { SettledAt = null };
    }

    public Debt LinkReminder(Guid? reminderId) => this with { ReminderId = reminderId };

    private void EnsureOpen()
    {
        if (IsSettled)
        {
            throw new InvalidOperationException("The debt was already returned.");
        }
    }
}
