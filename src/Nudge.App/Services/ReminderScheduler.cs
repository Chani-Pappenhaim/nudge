using System.Windows.Threading;
using Nudge.Core.Services;

namespace Nudge.App.Services;

/// <summary>Watches for reminders that become due and shows one alert per reminder.</summary>
public sealed class ReminderScheduler(ReminderService reminders, WindowService windows) : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private readonly HashSet<Guid> _alerting = [];
    private readonly DispatcherTimer _timer = new() { Interval = CheckInterval };

    public void Start()
    {
        _timer.Tick += (_, _) => ShowDueAlerts();
        _timer.Start();
        ShowDueAlerts();
    }

    public void Dispose() => _timer.Stop();

    private void ShowDueAlerts()
    {
        foreach (var reminder in reminders.GetDue())
        {
            if (_alerting.Add(reminder.Id))
            {
                windows.ShowAlert(reminder, () => _alerting.Remove(reminder.Id));
            }
        }
    }
}
