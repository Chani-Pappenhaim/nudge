using System.ComponentModel;
using System.Windows;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>Raised when closing only hid the window, leaving reminders running.</summary>
    public event EventHandler? HiddenToTray;

    public bool IsExiting { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!IsExiting)
        {
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke(this, EventArgs.Empty);
        }
        base.OnClosing(e);
    }
}
