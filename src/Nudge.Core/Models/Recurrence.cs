namespace Nudge.Core.Models;

/// <summary>Defines the cadence at which a reminder repeats.</summary>
public sealed record Recurrence
{
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaximumInterval = TimeSpan.FromDays(7);

    public static Recurrence Once { get; } = new(RecurrenceKind.Once, TimeSpan.Zero);
    public static Recurrence Daily { get; } = new(RecurrenceKind.Daily, TimeSpan.FromDays(1));
    public static Recurrence Weekly { get; } = new(RecurrenceKind.Weekly, TimeSpan.FromDays(7));

    private Recurrence(RecurrenceKind kind, TimeSpan interval)
    {
        Kind = kind;
        Interval = interval;
    }

    public RecurrenceKind Kind { get; }

    /// <summary>Time between occurrences; zero for one-time reminders.</summary>
    public TimeSpan Interval { get; }

    public bool IsRepeating => Kind != RecurrenceKind.Once;

    /// <summary>Creates a recurrence that repeats every <paramref name="interval"/>.</summary>
    public static Recurrence Every(TimeSpan interval)
    {
        if (interval < MinimumInterval || interval > MaximumInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval,
                $"Interval must be between {MinimumInterval} and {MaximumInterval}.");
        }
        return new Recurrence(RecurrenceKind.Interval, interval);
    }

    /// <summary>Restores a recurrence from its stored kind and interval.</summary>
    public static Recurrence From(RecurrenceKind kind, TimeSpan interval) => kind switch
    {
        RecurrenceKind.Once => Once,
        RecurrenceKind.Daily => Daily,
        RecurrenceKind.Weekly => Weekly,
        RecurrenceKind.Interval => Every(interval),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown recurrence kind."),
    };

    /// <summary>
    /// Returns the first occurrence on the original cadence that is later than <paramref name="now"/>,
    /// skipping any occurrences that were missed, or null for one-time reminders.
    /// </summary>
    public DateTime? NextOccurrence(DateTime scheduledAt, DateTime now)
    {
        if (!IsRepeating)
        {
            return null;
        }
        var elapsedSteps = (now - scheduledAt).Ticks / Interval.Ticks;
        var steps = Math.Max(1, elapsedSteps + 1);
        return scheduledAt + (Interval * steps);
    }
}
