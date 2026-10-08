namespace Nudge.Core.Models;

/// <summary>A part of a money debt that was paid back.</summary>
public sealed record DebtPayment(DateTime At, decimal Amount);
