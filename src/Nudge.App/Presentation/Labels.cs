using System.Globalization;
using Nudge.Core.Models;

namespace Nudge.App.Presentation;

/// <summary>Hebrew display text for domain values.</summary>
internal static class Labels
{
    private const int MinutesPerDay = 24 * 60;
    private const int MinutesPerWeek = 7 * MinutesPerDay;

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
        HistoryAction.Snoozed when entry.SnoozeDuration is { } duration && IsWholeUnit(duration) =>
            $"נדחה ב-{Duration(duration)}",
        HistoryAction.Snoozed when entry.SnoozeDuration is { } duration => $"נדחה {To(entry.At + duration, entry.At)}",
        HistoryAction.Snoozed => "נדחה",
        HistoryAction.Deleted => "נמחק",
        _ => entry.Action.ToString(),
    };

    /// <summary>Names a duration in its largest whole unit, e.g. "שבועיים", "3 ימים", "90 דקות".</summary>
    public static string Duration(TimeSpan duration)
    {
        var minutes = (int)duration.TotalMinutes;
        if (minutes >= MinutesPerWeek && minutes % MinutesPerWeek == 0)
        {
            return Count(minutes / MinutesPerWeek, "שבוע", "שבועיים", "שבועות");
        }
        if (minutes >= MinutesPerDay && minutes % MinutesPerDay == 0)
        {
            return Count(minutes / MinutesPerDay, "יום", "יומיים", "ימים");
        }
        if (minutes >= 60 && minutes % 60 == 0)
        {
            return Count(minutes / 60, "שעה", "שעתיים", "שעות");
        }
        return Count(minutes, "דקה", "2 דקות", "דקות");
    }

    /// <summary>Formats the target of a move in time, e.g. "למחר בשעה 09:00" or "ל-15/10/2026 בשעה 09:00".</summary>
    public static string To(DateTime at, DateTime now)
    {
        var when = When(at, now);
        return char.IsAsciiDigit(when[0]) ? $"ל-{when}" : $"ל{when}";
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

    /// <summary>
    /// True for durations that read naturally as one unit: minutes under an hour, hours under a day,
    /// or whole days. Anything else (such as a postpone to a chosen date) is better shown as a target time.
    /// </summary>
    private static bool IsWholeUnit(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero || duration.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            return false;
        }
        var minutes = (int)duration.TotalMinutes;
        return minutes < 60 || (minutes < MinutesPerDay && minutes % 60 == 0) || minutes % MinutesPerDay == 0;
    }

    private static string Count(int count, string one, string two, string many) => count switch
    {
        1 => one,
        2 => two,
        _ => $"{count} {many}",
    };
}
