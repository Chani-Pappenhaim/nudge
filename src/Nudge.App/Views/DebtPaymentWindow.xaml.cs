using System.Windows;
using Nudge.App.ViewModels;

namespace Nudge.App.Views;

public partial class DebtPaymentWindow : Window
{
    public DebtPaymentWindow(DebtPaymentViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
        Loaded += (_, _) =>
        {
            AmountBox.Focus();
            AmountBox.SelectAll();
        };
    }
}
