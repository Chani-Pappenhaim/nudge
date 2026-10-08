namespace Nudge.Core.Models;

/// <summary>Which side of a debt the user is on.</summary>
public enum DebtDirection
{
    /// <summary>Someone owes the user.</summary>
    OwedToMe,

    /// <summary>The user owes someone.</summary>
    IOwe,
}
