using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nudge.App.Presentation;
using Nudge.Core.Models;
using Nudge.Core.Services;

namespace Nudge.App.ViewModels;

/// <summary>Creates a new debt or edits an open one.</summary>
public sealed partial class DebtEditorViewModel(DebtService debts) : ObservableObject
{
    private Debt? _existing;

    [ObservableProperty]
    private DebtDirection _direction = DebtDirection.IOwe;

    [ObservableProperty]
    private string _person = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionLabel))]
    private bool _isMoney = true;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _error;

    /// <summary>Raised after the debt was validated and stored.</summary>
    public event EventHandler? Saved;

    public static IReadOnlyList<Choice<DebtDirection>> DirectionOptions { get; } =
    [
        new(DebtDirection.IOwe, "אני חייבת"),
        new(DebtDirection.OwedToMe, "חייבים לי"),
    ];

    public static IReadOnlyList<Choice<bool>> KindOptions { get; } = [new(true, "כסף"), new(false, "חפץ")];

    public string WindowTitle => _existing is null ? "חוב חדש" : "עריכת חוב";

    public string DescriptionLabel => IsMoney ? "על מה (לא חובה)" : "מה הושאל";

    /// <summary>A debt that was partly paid back stays a money debt.</summary>
    public bool CanChangeKind => _existing is not { Payments.Count: > 0 };

    /// <summary>The direction the debt was stored under by the last successful save.</summary>
    public DebtDirection? SavedDirection { get; private set; }

    /// <summary>Loads an open debt, or starts a new one in the given direction.</summary>
    public void Load(Debt? debt, DebtDirection newDebtDirection = DebtDirection.IOwe)
    {
        _existing = debt;
        SavedDirection = null;
        Direction = debt?.Direction ?? newDebtDirection;
        Person = debt?.Person ?? string.Empty;
        IsMoney = debt?.IsMoney ?? true;
        Amount = debt?.Amount is { } amount ? MoneyInput.ToText(amount) : string.Empty;
        Description = debt?.Description ?? string.Empty;
        Error = null;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(CanChangeKind));
    }

    [RelayCommand]
    private void Save()
    {
        Error = Validate(out var amount);
        if (Error is not null)
        {
            return;
        }
        if (_existing is null)
        {
            debts.Add(Direction, Person, Description, amount);
        }
        else
        {
            debts.Edit(_existing.Id, Direction, Person, Description, amount);
        }
        SavedDirection = Direction;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Checks the form and returns a message describing what is wrong, or null when it is valid.</summary>
    private string? Validate(out decimal? amount)
    {
        amount = null;
        if (string.IsNullOrWhiteSpace(Person))
        {
            return "יש להזין שם.";
        }
        if (!IsMoney)
        {
            return string.IsNullOrWhiteSpace(Description) ? "יש לכתוב מה הושאל." : null;
        }
        if (!MoneyInput.TryParse(Amount, out var money))
        {
            return MoneyInput.InvalidMessage;
        }
        amount = money;
        var paid = _existing?.Paid ?? 0m;
        if (amount <= paid)
        {
            return $"כבר הוחזרו {MoneyInput.Format(paid)}, לכן הסכום חייב להיות גדול מזה.";
        }
        return null;
    }
}
