using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nudge.App.Services;
using Nudge.App.ViewModels;
using Nudge.Core;
using Nudge.Infrastructure;

namespace Nudge.App;

[SuppressMessage("Design", "CA1001", Justification = "Owned resources are released in OnExit, the application's lifetime end.")]
public partial class App : Application
{
#if DEBUG
    // Development builds are a separate instance with their own data, so they run beside the installed app.
    private const string InstanceName = "Nudge.Dev";
#else
    private const string InstanceName = "Nudge";
#endif
    private const string ActivationEventName = @"Local\" + InstanceName + ".Activate.6F1C2B7E";
    private const string StartMinimizedArgument = "--minimized";

    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), InstanceName);

    private EventWaitHandle? _activationSignal;
    private RegisteredWaitHandle? _activationWait;
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        _activationSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            // Another copy is already running: bring its window forward instead of starting twice.
            _activationSignal.Set();
            Shutdown();
            return;
        }

        // Dates and numbers in bindings and date pickers follow Hebrew conventions.
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("he-IL")));

        _host = BuildHost();
        _host.Start();

        var windows = _host.Services.GetRequiredService<WindowService>();
        _activationWait = ThreadPool.RegisterWaitForSingleObject(_activationSignal,
            (_, _) => Dispatcher.BeginInvoke(windows.ShowMain), null, Timeout.Infinite, executeOnlyOnce: false);

        _host.Services.GetRequiredService<TrayIconService>().Show();
        _host.Services.GetRequiredService<ReminderScheduler>().Start();

        if (!e.Args.Contains(StartMinimizedArgument, StringComparer.OrdinalIgnoreCase))
        {
            windows.ShowMain();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activationWait?.Unregister(null);
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        _activationSignal?.Dispose();
        base.OnExit(e);
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services
            .AddNudgeCore()
            .AddNudgeInfrastructure(DataDirectory, Environment.ProcessPath!)
            .AddSingleton<WindowService>()
            .AddSingleton<IDialogService>(sp => sp.GetRequiredService<WindowService>())
            .AddSingleton<ReminderScheduler>()
            .AddSingleton<TrayIconService>()
            .AddSingleton<MainViewModel>()
            .AddTransient<ReminderEditorViewModel>()
            .AddTransient<SnoozeTimeViewModel>()
            .AddTransient<DebtEditorViewModel>()
            .AddTransient<DebtPaymentViewModel>();
        return builder.Build();
    }

    /// <summary>Logs unexpected errors and keeps the app running so reminders are not lost.</summary>
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.AppendAllText(Path.Combine(DataDirectory, "error.log"), $"[{DateTime.Now:O}] {e.Exception}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging is best effort.
        }
        MessageBox.Show(e.Exception.Message, "Nudge", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
