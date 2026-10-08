using System.Windows;
using Forms = System.Windows.Forms;

namespace Nudge.App.Services;

/// <summary>The notification-area icon that keeps the app reachable while its window is hidden.</summary>
public sealed class TrayIconService(WindowService windows) : IDisposable
{
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/nudge.ico");

    private Forms.NotifyIcon? _icon;
    private bool _backgroundHintShown;

    public void Show()
    {
        var menu = new Forms.ContextMenuStrip { RightToLeft = Forms.RightToLeft.Yes };
        menu.Items.Add("פתיחת Nudge", null, (_, _) => windows.ShowMain());
        menu.Items.Add("תזכורת חדשה", null, (_, _) => windows.EditReminder(null));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("יציאה", null, (_, _) => windows.ExitApplication());

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Nudge",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                windows.ShowMain();
            }
        };
        windows.MainHidden += OnMainHidden;
    }

    public void Dispose()
    {
        windows.MainHidden -= OnMainHidden;
        if (_icon is not null)
        {
            _icon.Visible = false;
            _icon.ContextMenuStrip?.Dispose();
            _icon.Dispose();
        }
    }

    /// <summary>Explains once that closing the window keeps reminders running.</summary>
    private void OnMainHidden(object? sender, EventArgs e)
    {
        if (_backgroundHintShown || _icon is null)
        {
            return;
        }
        _backgroundHintShown = true;
        _icon.ShowBalloonTip(4000, "Nudge ממשיך לפעול ברקע",
            "התזכורות יקפצו בזמן. ליציאה: קליק ימני על הסמל.", Forms.ToolTipIcon.Info);
    }

    private static System.Drawing.Icon LoadIcon()
    {
        using var stream = Application.GetResourceStream(IconUri).Stream;
        return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
    }
}
