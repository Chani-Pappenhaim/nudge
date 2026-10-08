using Nudge.Core.Models;

namespace Nudge.Core.Tests;

public class DebtTests
{
    private static readonly DateTime Start = new(2026, 10, 8, 12, 0, 0);

    private static Debt Money(decimal amount = 200m) =>
        Debt.Create(DebtDirection.OwedToMe, " Dana ", " lunch ", amount, Start);

    private static Debt Item() => Debt.Create(DebtDirection.IOwe, "Avi", "drill", null, Start);

    [Fact]
    public void Create_TrimsTextAndRoundsAmount()
    {
        var debt = Debt.Create(DebtDirection.OwedToMe, " Dana ", " lunch ", 10.555m, Start);

        Assert.Equal("Dana", debt.Person);
        Assert.Equal("lunch", debt.Description);
        Assert.Equal(10.56m, debt.Amount);
        Assert.False(debt.IsSettled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_RejectsNonPositiveAmount(int amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Money(amount));

    [Fact]
    public void Create_RejectsBlankPersonAndItemWithoutDescription()
    {
        Assert.Throws<ArgumentException>(() => Debt.Create(DebtDirection.IOwe, " ", "x", 5m, Start));
        Assert.Throws<ArgumentException>(() => Debt.Create(DebtDirection.IOwe, "Avi", " ", null, Start));
    }

    [Fact]
    public void RecordPayment_ReducesRemainingAndSettlesWhenPaidInFull()
    {
        var debt = Money().RecordPayment(50m, Start.AddDays(1));

        Assert.Equal(150m, debt.Remaining);
        Assert.False(debt.IsSettled);

        debt = debt.RecordPayment(150m, Start.AddDays(2));

        Assert.Equal(0m, debt.Remaining);
        Assert.Equal(Start.AddDays(2), debt.SettledAt);
    }

    [Fact]
    public void RecordPayment_RejectsOverpaymentZeroAndItems()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money().RecordPayment(201m, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money().RecordPayment(0m, Start));
        Assert.Throws<InvalidOperationException>(() => Item().RecordPayment(1m, Start));
    }

    [Fact]
    public void Settle_RecordsRestOfMoneyAsFinalPayment()
    {
        var debt = Money().RecordPayment(50m, Start).Settle(Start.AddDays(1));

        Assert.True(debt.IsSettled);
        Assert.Equal([50m, 150m], debt.Payments.Select(p => p.Amount));
    }

    [Fact]
    public void Settle_MarksItemReturned()
    {
        var debt = Item().Settle(Start.AddDays(3));

        Assert.Equal(Start.AddDays(3), debt.SettledAt);
        Assert.Empty(debt.Payments);
    }

    [Fact]
    public void SettledDebt_CannotBeChangedUntilReopened()
    {
        var settled = Money().Settle(Start);

        Assert.Throws<InvalidOperationException>(() => settled.RecordPayment(1m, Start));
        Assert.Throws<InvalidOperationException>(() => settled.Edit(DebtDirection.IOwe, "x", null, 5m));
        Assert.Throws<InvalidOperationException>(() => settled.Settle(Start));
    }

    [Fact]
    public void Reopen_UndoesFinalPaymentOfMoneyDebt()
    {
        var debt = Money().RecordPayment(50m, Start).Settle(Start.AddDays(1)).Reopen();

        Assert.False(debt.IsSettled);
        Assert.Equal(150m, debt.Remaining);
    }

    [Fact]
    public void Reopen_ReturnsItemToOpen()
    {
        var debt = Item().Settle(Start).Reopen();

        Assert.False(debt.IsSettled);
        Assert.Throws<InvalidOperationException>(() => debt.Reopen());
    }

    [Fact]
    public void UndoLastPayment_RemovesLatestPayment()
    {
        var debt = Money().RecordPayment(20m, Start).RecordPayment(30m, Start).UndoLastPayment();

        Assert.Equal(180m, debt.Remaining);
        Assert.Throws<InvalidOperationException>(() => Money().UndoLastPayment());
    }

    [Fact]
    public void Edit_KeepsAmountAbovePaidAndPaidDebtAsMoney()
    {
        var paid = Money().RecordPayment(50m, Start);

        Assert.Throws<ArgumentOutOfRangeException>(() => paid.Edit(DebtDirection.OwedToMe, "Dana", null, 50m));
        Assert.Throws<InvalidOperationException>(() => paid.Edit(DebtDirection.OwedToMe, "Dana", "book", null));
        Assert.Equal(100m, paid.Edit(DebtDirection.IOwe, "Dana", null, 100m).Amount);
    }

    [Fact]
    public void Edit_TurnsUnpaidMoneyDebtIntoItem()
    {
        var debt = Money().Edit(DebtDirection.OwedToMe, "Dana", "book", null);

        Assert.False(debt.IsMoney);
        Assert.Equal(0m, debt.Remaining);
    }
}
