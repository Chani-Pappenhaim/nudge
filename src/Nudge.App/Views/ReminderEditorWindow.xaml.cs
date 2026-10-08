using System.Windows;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class ReminderEditorWindow : Window
{
    public ReminderEditorWindow(ReminderEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
        Loaded += (_, _) => TitleBox.Focus();
    }
}
