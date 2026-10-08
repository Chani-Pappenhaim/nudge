using System.Windows;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class DebtEditorWindow : Window
{
    public DebtEditorWindow(DebtEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
        Loaded += (_, _) => PersonBox.Focus();
    }
}
