namespace Nudge.App.Presentation;

/// <summary>A selectable value paired with its display text.</summary>
public sealed record Choice<T>(T Value, string Label);
