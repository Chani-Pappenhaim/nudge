using Nudge.Core.Models;

namespace Nudge.Core.Abstractions;

/// <summary>Plays reminder alert sounds.</summary>
public interface ISoundPlayer
{
    void Play(AlertSound sound);
}
