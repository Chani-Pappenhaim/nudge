using Nudge.Core.Models;

namespace Nudge.App.Services;

/// <summary>Dialogs that view models can request without depending on windows.</summary>
public interface IDialogService
{
    /// <summary>Opens the editor for a new reminder (null) or an existing one; true when it was saved.</summary>
    bool EditReminder(Reminder? reminder);

    bool Confirm(string message);
}
