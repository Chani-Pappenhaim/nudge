using System.Globalization;

namespace Nudge.App.Presentation;

/// <summary>Parsing and formatting of shekel amounts.</summary>
internal static class MoneyInput
{
    public const string InvalidMessage = "סכום לא תקין. יש לכתוב מספר גדול מאפס, למשל 150 או 49.90.";

    /// <summary>Formats an amount such as "₪1,250" or "₪1,250.50"; agorot are shown only when there are any.</summary>
    public static string Format(decimal amount) =>
        "₪" + amount.ToString(amount % 1m == 0m ? "#,0" : "#,0.00", CultureInfo.InvariantCulture);

    /// <summary>Plain text for an input field, e.g. "1250.5".</summary>
    public static string ToText(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Reads a positive amount rounded to agorot; a shekel sign and thousands separators are allowed.</summary>
    public static bool TryParse(string text, out decimal amount)
    {
        if (!decimal.TryParse(text.Replace("₪", string.Empty).Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }
        amount = decimal.Round(amount, 2);
        return amount > 0m;
    }
}
