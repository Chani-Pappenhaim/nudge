using System.Windows;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class SnoozeTimeWindow : Window
{
    public SnoozeTimeWindow(SnoozeTimeViewModel viewModel, string reminderTitle)
    {
        InitializeComponent();
        ReminderTitle = reminderTitle;
        DataContext = viewModel;
        viewModel.Picked += (_, _) => DialogResult = true;
        Loaded += (_, _) => DateBox.Focus();
    }

    public string ReminderTitle { get; }
}
