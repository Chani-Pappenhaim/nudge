namespace Nudge.Core.Models;

/// <summary>A record of something the user did with a reminder.</summary>
public sealed record HistoryEntry(DateTime At, string Title, HistoryAction Action, TimeSpan? SnoozeDuration = null);
