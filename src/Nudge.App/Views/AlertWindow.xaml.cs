using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class AlertWindow : Window
{
    private bool _isClosing;

    public AlertWindow(AlertViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) =>
        {
            if (!_isClosing)
            {
                Close();
            }
        };
    }

    public AlertViewModel ViewModel { get; }

    protected override void OnClosing(CancelEventArgs e)
    {
        _isClosing = true;
        ViewModel.OnClosing();
        base.OnClosing(e);
    }

    private void OnDragAreaPressed(object sender, MouseButtonEventArgs e) => DragMove();
}
