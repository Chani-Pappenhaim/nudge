using Nudge.Core.Models;
using Nudge.Infrastructure.Persistence;

namespace Nudge.Infrastructure.Tests;

public sealed class JsonPersistenceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("nudge-tests-").FullName;

    private string PathOf(string name) => Path.Combine(_directory, name);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Reminders_SurviveARestart()
    {
        var path = PathOf("reminders.json");
        var reminder = Reminder.Create("לשתות מים", new DateTime(2026, 10, 8, 9, 30, 0),
            Recurrence.Every(TimeSpan.FromMinutes(45)), AlertSound.Exclamation, "כוס מלאה")
            .Snooze(new DateTime(2026, 10, 8, 9, 30, 0), TimeSpan.FromMinutes(10));

        new JsonReminderRepository(new JsonFileStore<ReminderRecord>(path)).Save(reminder);
        var reloaded = new JsonReminderRepository(new JsonFileStore<ReminderRecord>(path)).GetAll();

        Assert.Equal(reminder, Assert.Single(reloaded));
    }

    [Fact]
    public void Reminders_FileKeepsHebrewReadable()
    {
        var path = PathOf("reminders.json");

        new JsonReminderRepository(new JsonFileStore<ReminderRecord>(path))
            .Save(Reminder.Create("תזכורת", DateTime.Today, Recurrence.Daily));

        Assert.Contains("תזכורת", File.ReadAllText(path));
    }

    [Fact]
    public void Delete_RemovesFromDisk()
    {
        var path = PathOf("reminders.json");
        var repository = new JsonReminderRepository(new JsonFileStore<ReminderRecord>(path));
        var reminder = Reminder.Create("x", DateTime.Today, Recurrence.Once);
        repository.Save(reminder);

        repository.Delete(reminder.Id);

        Assert.Empty(new JsonReminderRepository(new JsonFileStore<ReminderRecord>(path)).GetAll());
    }

    [Fact]
    public void Debts_SurviveARestart()
    {
        var path = PathOf("debts.json");
        var start = new DateTime(2026, 10, 8, 9, 30, 0);
        var debt = Debt.Create(DebtDirection.IOwe, "דנה", "ארוחת צהריים", 120.5m, start)
            .RecordPayment(20m, start.AddDays(1))
            .LinkReminder(Guid.NewGuid());
        var item = Debt.Create(DebtDirection.OwedToMe, "אבי", "מקדחה", null, start).Settle(start.AddDays(2));
        var repository = new JsonDebtRepository(new JsonFileStore<DebtRecord>(path));
        repository.Save(debt);
        repository.Save(item);

        var reloaded = new JsonDebtRepository(new JsonFileStore<DebtRecord>(path)).GetAll().ToDictionary(d => d.Id);

        // Records compare lists by reference, so payments are compared separately.
        Assert.Equal(debt with { Payments = reloaded[debt.Id].Payments }, reloaded[debt.Id]);
        Assert.Equal(debt.Payments, reloaded[debt.Id].Payments);
        Assert.Equal(item with { Payments = reloaded[item.Id].Payments }, reloaded[item.Id]);
        Assert.Empty(reloaded[item.Id].Payments);
    }

    [Fact]
    public void CorruptFile_IsBackedUpInsteadOfLost()
    {
        var path = PathOf("reminders.json");
        File.WriteAllText(path, "{ not valid json");

        var reminders = new JsonFileStore<ReminderRecord>(path).Load();

        Assert.Empty(reminders);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_directory, "reminders.json.corrupt-*"));
    }

    [Fact]
    public void History_KeepsOnlyTheMostRecentEntries()
    {
        var path = PathOf("history.json");
        var repository = new JsonHistoryRepository(new JsonFileStore<HistoryEntry>(path));

        for (var i = 0; i < JsonHistoryRepository.MaxEntries + 5; i++)
        {
            repository.Add(new HistoryEntry(DateTime.Today.AddMinutes(i), $"#{i}", HistoryAction.Completed));
        }

        var reloaded = new JsonHistoryRepository(new JsonFileStore<HistoryEntry>(path)).GetAll();
        Assert.Equal(JsonHistoryRepository.MaxEntries, reloaded.Count);
        Assert.Equal("#5", reloaded[0].Title);
    }
}
