using Nudge.App.Presentation;
using Nudge.Core.Models;

namespace Nudge.App.ViewModels;

/// <summary>A history log entry as shown in the history tab.</summary>
public sealed class HistoryItemViewModel(HistoryEntry entry, DateTime now)
{
    public string Title => entry.Title;

    public string ActionText => Labels.Describe(entry);

    public string WhenText => Labels.When(entry.At, now);

    public string Glyph => entry.Action switch
    {
        HistoryAction.Completed => Glyphs.Completed,
        HistoryAction.Snoozed => Glyphs.Snoozed,
        _ => Glyphs.Deleted,
    };
}
