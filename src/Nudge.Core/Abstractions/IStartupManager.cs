namespace Nudge.Core.Abstractions;

/// <summary>Controls whether the application launches when the user signs in.</summary>
public interface IStartupManager
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
