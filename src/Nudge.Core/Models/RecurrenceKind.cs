namespace Nudge.Core.Models;

/// <summary>How often a reminder repeats after it fires.</summary>
public enum RecurrenceKind
{
    Once,
    Daily,
    Weekly,
    Interval,
}
