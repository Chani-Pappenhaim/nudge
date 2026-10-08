using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Nudge.App.ViewModels;
using Nudge.App.Views;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.Services;

/// <summary>Owns the application's windows: the main window, the editor and the stacked alerts.</summary>
public sealed class WindowService(
    IServiceProvider services,
    ReminderService reminders,
    ISoundPlayer sounds,
    TimeProvider time) : IDialogService
{
    private const double AlertSpacing = 12;

    private readonly List<AlertWindow> _alerts = [];
    private MainWindow? _main;
    private ReminderEditorWindow? _editor;

    /// <summary>Raised when the main window is closed to the notification area.</summary>
    public event EventHandler? MainHidden;

    public void ShowMain()
    {
        if (_main is null)
        {
            _main = new MainWindow(services.GetRequiredService<MainViewModel>());
            _main.HiddenToTray += (_, _) => MainHidden?.Invoke(this, EventArgs.Empty);
        }
        services.GetRequiredService<MainViewModel>().Refresh();
        _main.Show();
        if (_main.WindowState == WindowState.Minimized)
        {
            _main.WindowState = WindowState.Normal;
        }
        _main.Activate();
    }

    public bool EditReminder(Reminder? reminder)
    {
        if (_editor is not null)
        {
            _editor.Activate();
            return false;
        }
        var viewModel = services.GetRequiredService<ReminderEditorViewModel>();
        viewModel.Load(reminder);
        _editor = new ReminderEditorWindow(viewModel);
        if (_main is { IsVisible: true })
        {
            _editor.Owner = _main;
        }
        try
        {
            return _editor.ShowDialog() == true;
        }
        finally
        {
            _editor = null;
        }
    }

    public bool Confirm(string message)
    {
        const MessageBoxOptions RightToLeft = MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign;
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? _main;
        var result = owner is null
            ? MessageBox.Show(message, "Nudge", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, RightToLeft)
            : MessageBox.Show(owner, message, "Nudge", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, RightToLeft);
        return result == MessageBoxResult.Yes;
    }

    /// <summary>Shows an alert above the previous ones in the bottom corner of the screen.</summary>
    public void ShowAlert(Reminder reminder, Action onClosed)
    {
        AlertWindow? window = null;
        var viewModel = new AlertViewModel(reminder, reminders, sounds, time,
            suggestion => PickSnoozeTime(window!, reminder.Title, suggestion));
        window = new AlertWindow(viewModel);
        window.SizeChanged += (_, _) => ArrangeAlerts();
        window.Closed += (_, _) =>
        {
            _alerts.Remove(window);
            ArrangeAlerts();
            onClosed();
        };
        _alerts.Add(window);
        window.Show();
        ArrangeAlerts();
        viewModel.Start();
    }

    /// <summary>Asks for a moment to postpone an alert to. Closing the alert closes this dialog as well.</summary>
    private DateTime? PickSnoozeTime(AlertWindow alert, string title, DateTime suggestion)
    {
        var viewModel = services.GetRequiredService<SnoozeTimeViewModel>();
        viewModel.Load(suggestion);
        var dialog = new SnoozeTimeWindow(viewModel, title) { Owner = alert };
        return dialog.ShowDialog() == true ? viewModel.Result : null;
    }

    public void ExitApplication()
    {
        if (_main is not null)
        {
            _main.IsExiting = true;
        }
        foreach (var alert in _alerts.ToList())
        {
            alert.ViewModel.Abandon();
        }
        Application.Current.Shutdown();
    }

    private void ArrangeAlerts()
    {
        var area = SystemParameters.WorkArea;
        var bottom = area.Bottom - AlertSpacing;
        foreach (var alert in _alerts)
        {
            alert.Left = area.Right - alert.ActualWidth - AlertSpacing;
            alert.Top = bottom - alert.ActualHeight;
            bottom = alert.Top - AlertSpacing;
        }
    }
}
