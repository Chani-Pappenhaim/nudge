using Microsoft.Extensions.Time.Testing;
using Nudge.Core.Models;
using Nudge.Core.Services;
using Nudge.Core.Tests.Fakes;

namespace Nudge.Core.Tests;

public class DebtServiceTests
{
    private static readonly DateTime Start = new(2026, 10, 8, 12, 0, 0);

    private readonly FakeTimeProvider _time = new();
    private readonly ReminderService _reminders;
    private readonly DebtService _service;

    public DebtServiceTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _time.SetUtcNow(new DateTimeOffset(Start, TimeSpan.Zero));
        _reminders = new ReminderService(new InMemoryReminderRepository(), new InMemoryHistoryRepository(), _time);
        _service = new DebtService(new InMemoryDebtRepository(), _reminders, _time);
    }

    private (Debt Debt, Reminder Reminder) AddWithReminder(decimal? amount = 100m)
    {
        var debt = _service.Add(DebtDirection.OwedToMe, "Dana", "book", amount);
        var reminder = Reminder.Create("collect from Dana", Start.AddDays(1), Recurrence.Every(TimeSpan.FromDays(1)));
        _reminders.Save(reminder);
        _service.LinkReminder(debt.Id, reminder.Id);
        return (debt, reminder);
    }

    [Fact]
    public void GetOpenAndSettled_SplitDebtsAndOrderThem()
    {
        var first = _service.Add(DebtDirection.OwedToMe, "A", null, 10m);
        _time.Advance(TimeSpan.FromMinutes(1));
        var second = _service.Add(DebtDirection.IOwe, "B", "pen", null);
        _time.Advance(TimeSpan.FromMinutes(1));
        var third = _service.Add(DebtDirection.IOwe, "C", null, 5m);
        _service.Settle(first.Id);
        _time.Advance(TimeSpan.FromMinutes(1));
        _service.Settle(third.Id);

        Assert.Equal([second.Id], _service.GetOpen().Select(d => d.Id));
        Assert.Equal([third.Id, first.Id], _service.GetSettled().Select(d => d.Id));
    }

    [Fact]
    public void RecordPayment_StampsCurrentTimeAndRaisesChanged()
    {
        var debt = _service.Add(DebtDirection.OwedToMe, "Dana", null, 100m);
        var raised = 0;
        _service.Changed += (_, _) => raised++;
        _time.Advance(TimeSpan.FromHours(2));

        _service.RecordPayment(debt.Id, 40m);

        var payment = Assert.Single(_service.Find(debt.Id)!.Payments);
        Assert.Equal(Start.AddHours(2), payment.At);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Settle_RemovesLinkedReminderAsCompleted()
    {
        var (debt, reminder) = AddWithReminder();

        _service.Settle(debt.Id);

        Assert.Null(_reminders.Find(reminder.Id));
        Assert.Null(_service.Find(debt.Id)!.ReminderId);
        Assert.Equal(HistoryAction.Completed, Assert.Single(_reminders.GetHistory()).Action);
    }

    [Fact]
    public void FinalPayment_RemovesLinkedReminder()
    {
        var (debt, reminder) = AddWithReminder();

        _service.RecordPayment(debt.Id, 30m);
        Assert.NotNull(_reminders.Find(reminder.Id));

        _service.RecordPayment(debt.Id, 70m);
        Assert.Null(_reminders.Find(reminder.Id));
    }

    [Fact]
    public void Delete_RemovesDebtAndLinkedReminder()
    {
        var (debt, reminder) = AddWithReminder(null);

        _service.Delete(debt.Id);

        Assert.Null(_service.Find(debt.Id));
        Assert.Null(_reminders.Find(reminder.Id));
        Assert.Equal(HistoryAction.Deleted, Assert.Single(_reminders.GetHistory()).Action);
    }

    [Fact]
    public void FindReminder_IgnoresReminderDeletedSeparately()
    {
        var (debt, reminder) = AddWithReminder();

        _reminders.Delete(reminder.Id);

        Assert.Null(_service.FindReminder(_service.Find(debt.Id)!));
        _service.Settle(debt.Id);
        Assert.True(_service.Find(debt.Id)!.IsSettled);
    }

    [Fact]
    public void UnknownId_IsIgnored()
    {
        _service.Settle(Guid.NewGuid());
        _service.Delete(Guid.NewGuid());

        Assert.Empty(_service.GetOpen());
    }
}
