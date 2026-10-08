# Nudge

A lightweight Windows reminder app with a Hebrew, right-to-left interface.

- One-time, daily, weekly or every-N-minutes reminders
- Always-on-top alerts that stack in the corner of the screen, with a repeating sound
- Snooze for 5/10/30/60 minutes or mark as done; closing an alert snoozes it instead of losing it
- Pause and resume — repeating reminders skip occurrences missed while paused or while the PC was off
- History of completed, snoozed and deleted reminders
- Runs from the notification area, optional start with Windows, single instance

## Download and run

Two options, both built as described below:

- **Installer** — `NudgeSetup-<version>.exe` (Hebrew). Installs for the current user without administrator
  rights, adds Start menu and optional desktop shortcuts and an optional start-with-Windows entry, and
  appears in *Settings → Apps* for uninstalling.
- **Portable** — `Nudge.exe`. A single self-contained file; just run it. No .NET runtime needed.

Data is stored in `%APPDATA%\Nudge` (`reminders.json`, `history.json`). Uninstalling asks whether to delete it.

## Architecture

```
src/
  Nudge.Core            Domain model and use cases. No UI or OS dependencies.
  Nudge.Infrastructure  JSON storage, Windows sounds, startup registration.
  Nudge.App             WPF (MVVM) desktop app: windows, tray icon, scheduler.
tests/
  Nudge.Core.Tests
  Nudge.Infrastructure.Tests
```

Dependencies point inward: `App → Infrastructure → Core`. Core defines ports
(`IReminderRepository`, `ISoundPlayer`, ...) that Infrastructure implements, and the clock is
injected as `TimeProvider` so time-dependent rules are tested deterministically.

Reminders are immutable records. A reminder keeps its regular cadence (`ScheduledAt`) separate from
a snooze (`SnoozedUntil`), so snoozing never shifts later occurrences.

Stack: .NET 9, WPF with the Fluent theme, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, xUnit.

## Build

Requires the .NET 9 SDK on Windows.

```bash
dotnet test
dotnet publish src/Nudge.App -p:PublishProfile=win-x64
```

The executable is written to `artifacts/publish/Nudge.exe`.

To build the installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and compile the script
after publishing:

```bash
ISCC.exe installer/Nudge.iss
```

The installer is written to `artifacts/installer/`.
