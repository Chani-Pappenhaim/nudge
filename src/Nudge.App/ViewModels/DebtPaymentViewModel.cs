using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>Records part of a money debt as paid back.</summary>
public sealed partial class DebtPaymentViewModel(DebtService debts) : ObservableObject
{
    private Debt? _debt;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private string? _error;

    /// <summary>Raised after the payment was validated and stored.</summary>
    public event EventHandler? Saved;

    public string Summary => _debt is null
        ? string.Empty
        : $"{_debt.Person} · נותרו {MoneyInput.Format(_debt.Remaining)} מתוך {MoneyInput.Format(_debt.Amount ?? 0m)}";

    public void Load(Debt debt)
    {
        _debt = debt;
        Amount = MoneyInput.ToText(debt.Remaining);
        Error = null;
        OnPropertyChanged(nameof(Summary));
    }

    [RelayCommand]
    private void Save()
    {
        if (_debt is null)
        {
            return;
        }
        if (!MoneyInput.TryParse(Amount, out var amount))
        {
            Error = MoneyInput.InvalidMessage;
            return;
        }
        if (amount > _debt.Remaining)
        {
            Error = $"הסכום גדול מהיתרה ({MoneyInput.Format(_debt.Remaining)}).";
            return;
        }
        debts.RecordPayment(_debt.Id, amount);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
