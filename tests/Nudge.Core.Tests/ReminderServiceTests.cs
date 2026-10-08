using Microsoft.Extensions.Time.Testing;
using Nudge.Core.Models;
using Nudge.Core.Services;
using Nudge.Core.Tests.Fakes;

namespace Nudge.Core.Tests;

public class ReminderServiceTests
{
    private static readonly DateTime Start = new(2026, 10, 8, 12, 0, 0);

    private readonly FakeTimeProvider _time = new();
    private readonly ReminderService _service;

    public ReminderServiceTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
        _time.SetUtcNow(new DateTimeOffset(Start, TimeSpan.Zero));
        _service = new ReminderService(new InMemoryReminderRepository(), new InMemoryHistoryRepository(), _time);
    }

    private Reminder Add(string title, DateTime at, Recurrence? recurrence = null)
    {
        var reminder = Reminder.Create(title, at, recurrence ?? Recurrence.Once);
        _service.Save(reminder);
        return reminder;
    }

    [Fact]
    public void GetDue_ReturnsOnlyActiveRemindersWhoseTimeHasCome()
    {
        var due = Add("due", Start.AddMinutes(-1));
        Add("future", Start.AddMinutes(1));
        var paused = Add("paused", Start.AddMinutes(-5));
        _service.SetActive(paused.Id, false);

        Assert.Equal([due.Id], _service.GetDue().Select(r => r.Id));
    }

    [Fact]
    public void GetAll_ListsActiveRemindersFirstByDueTime()
    {
        var later = Add("later", Start.AddHours(2));
        var sooner = Add("sooner", Start.AddHours(1));
        var paused = Add("paused", Start);
        _service.SetActive(paused.Id, false);

        Assert.Equal([sooner.Id, later.Id, paused.Id], _service.GetAll().Select(r => r.Id));
    }

    [Fact]
    public void Complete_AdvancesRepeatingReminderAndLogsHistory()
    {
        var reminder = Add("water", Start, Recurrence.Every(TimeSpan.FromMinutes(30)));

        _service.Complete(reminder.Id);

        Assert.Equal(Start.AddMinutes(30), _service.Find(reminder.Id)!.DueAt);
        var entry = Assert.Single(_service.GetHistory());
        Assert.Equal(HistoryAction.Completed, entry.Action);
        Assert.Equal("water", entry.Title);
    }

    [Fact]
    public void Snooze_PostponesFromNowAndLogsDuration()
    {
        var reminder = Add("call", Start.AddMinutes(-10));

        _service.Snooze(reminder.Id, TimeSpan.FromMinutes(5));

        Assert.Equal(Start.AddMinutes(5), _service.Find(reminder.Id)!.DueAt);
        Assert.Equal(TimeSpan.FromMinutes(5), _service.GetHistory().Single().SnoozeDuration);
    }

    [Fact]
    public void Delete_RemovesReminderAndLogsHistory()
    {
        var reminder = Add("old", Start);

        _service.Delete(reminder.Id);

        Assert.Null(_service.Find(reminder.Id));
        Assert.Equal(HistoryAction.Deleted, _service.GetHistory().Single().Action);
    }

    [Fact]
    public void ActionsOnUnknownReminder_AreIgnored()
    {
        var unknown = Guid.NewGuid();

        _service.Complete(unknown);
        _service.Snooze(unknown, TimeSpan.FromMinutes(5));
        _service.Delete(unknown);

        Assert.Empty(_service.GetHistory());
    }

    [Fact]
    public void Changes_RaiseChangedEvent()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        var reminder = Add("x", Start);
        _service.Complete(reminder.Id);
        _service.ClearHistory();

        Assert.Equal(3, raised);
    }

    [Fact]
    public void GetHistory_ReturnsNewestFirst()
    {
        var reminder = Add("x", Start, Recurrence.Daily);
        _service.Snooze(reminder.Id, TimeSpan.FromMinutes(5));
        _time.Advance(TimeSpan.FromMinutes(5));
        _service.Complete(reminder.Id);

        Assert.Equal([HistoryAction.Completed, HistoryAction.Snoozed],
            _service.GetHistory().Select(h => h.Action));
    }
}
