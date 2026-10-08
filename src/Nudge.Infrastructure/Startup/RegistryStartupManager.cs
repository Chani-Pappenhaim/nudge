using Microsoft.Win32;
using Nudge.Core.Abstractions;

namespace Nudge.Infrastructure.Startup;

/// <summary>Registers the application under the current user's Windows "Run" key.</summary>
internal sealed class RegistryStartupManager(string executablePath) : IStartupManager
{
    public const string StartMinimizedArgument = "--minimized";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Nudge";

    private string Command => $"\"{executablePath}\" {StartMinimizedArgument}";

    /// <summary>True only when registered for this executable, so a moved app is not reported as enabled.</summary>
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(ValueName, Command);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
