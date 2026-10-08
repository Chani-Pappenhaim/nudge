using System.Globalization;

namespace Nudge.App.Presentation;

/// <summary>Parsing and formatting of the time-of-day text fields.</summary>
internal static class TimeInput
{
    private static readonly string[] Formats = ["H:mm", "HH:mm", "H"];

    public const string InvalidMessage = "שעה לא תקינה. יש לכתוב למשל 14:30.";

    public static string Format(DateTime at) => at.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static bool TryParse(string text, out TimeOnly time) =>
        TimeOnly.TryParseExact(text.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static DateTime TruncateToMinute(DateTime value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMinute));
}
