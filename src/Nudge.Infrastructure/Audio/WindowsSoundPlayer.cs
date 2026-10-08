using System.Runtime.InteropServices;
using Nudge.Core.Abstractions;
using Nudge.Core.Models;

namespace Nudge.Infrastructure.Audio;

/// <summary>Plays alert sounds from the user's Windows sound scheme.</summary>
internal sealed partial class WindowsSoundPlayer : ISoundPlayer
{
    private const uint SimpleBeep = 0x00000000;
    private const uint IconHand = 0x00000010;
    private const uint IconExclamation = 0x00000030;
    private const uint IconAsterisk = 0x00000040;

    public void Play(AlertSound sound)
    {
        uint? type = sound switch
        {
            AlertSound.Default => IconAsterisk,
            AlertSound.Notification => SimpleBeep,
            AlertSound.Exclamation => IconExclamation,
            AlertSound.Critical => IconHand,
            _ => null,
        };
        if (type is { } value)
        {
            MessageBeep(value);
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MessageBeep(uint type);
}
