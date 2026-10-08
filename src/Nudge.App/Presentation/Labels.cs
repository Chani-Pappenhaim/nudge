using System.Globalization;
using Nudge.Core.Models;

namespace Nudge.App.Presentation;

/// <summary>Hebrew display text for domain values.</summary>
internal static class Labels
{
    public static string Of(RecurrenceKind kind) => kind switch
    {
        RecurrenceKind.Once => "פעם אחת",
        RecurrenceKind.Daily => "כל יום",
        RecurrenceKind.Weekly => "כל שבוע",
        RecurrenceKind.Interval => "כל X דקות",
        _ => kind.ToString(),
    };

    public static string Of(AlertSound sound) => sound switch
    {
        AlertSound.Default => "ברירת מחדל",
        AlertSound.Notification => "צליל קצר",
        AlertSound.Exclamation => "קריאה",
        AlertSound.Critical => "התראה חמורה",
        AlertSound.Silent => "שקט",
        _ => sound.ToString(),
    };

    public static string Describe(Recurrence recurrence) =>
        recurrence.Kind == RecurrenceKind.Interval ? $"כל {Duration(recurrence.Interval)}" : Of(recurrence.Kind);

    public static string Describe(HistoryEntry entry) => entry.Action switch
    {
        HistoryAction.Completed => "בוצע",
        HistoryAction.Snoozed when entry.SnoozeDuration is { } duration => $"נדחה ב-{Duration(duration)}",
        HistoryAction.Snoozed => "נדחה",
        HistoryAction.Deleted => "נמחק",
        _ => entry.Action.ToString(),
    };

    public static string Duration(TimeSpan duration)
    {
        var minutes = (int)duration.TotalMinutes;
        if (minutes % 60 != 0)
        {
            return minutes == 1 ? "דקה" : $"{minutes} דקות";
        }
        return (minutes / 60) switch
        {
            1 => "שעה",
            2 => "שעתיים",
            var hours => $"{hours} שעות",
        };
    }

    /// <summary>Formats a moment relative to today, e.g. "מחר בשעה 09:00".</summary>
    public static string When(DateTime at, DateTime now)
    {
        var time = at.ToString("HH:mm", CultureInfo.InvariantCulture);
        var days = (at.Date - now.Date).Days;
        return days switch
        {
            0 => $"היום בשעה {time}",
            1 => $"מחר בשעה {time}",
            -1 => $"אתמול בשעה {time}",
            _ => $"{at.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} בשעה {time}",
        };
    }
}
