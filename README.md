# Paunix Guard

<img src="assets/logo-128x128.png" alt="Paunix Guard" width="128" align="right">

[![License](https://img.shields.io/badge/license-PolyForm%20Shield-blue)](https://polyformproject.org/licenses/shield/1.0.0/)

Paunix Guard is a Windows-first, privacy-first laptop guard for public workspaces.
It is designed to protect a laptop while the owner steps away briefly by reacting to physical theft signals such as power unplug, input activity, sleep attempts, shutdown attempts, lid close, and desktop/session switches.

The first implementation is desktop-only and offline-first. Phone pairing, webcam snapshots, and cloud relay are planned companion features, not requirements for core protection.

## Principles

- Offline protection must work without a phone or internet connection.
- The app must never record key contents, browse user files, read clipboard data, open a remote shell, or run hidden surveillance.
- Settings and event history live in `%LocalAppData%\PaunixGuard`, not beside the executable.
- `AGENTS.md` and `MEMORY.md` are local AI context files and are intentionally ignored by git.

## Features

- **6 triggers:** Charger unplug, keyboard/mouse input, lid close, sleep attempt, shutdown/logoff, desktop/session switch
- **Grace + Warning:** 3-second grace after arming. Then 4-second warning countdown (each keystroke resets). No false alarms.
- **Kiosk mode:** Blocks Win key, Alt+Tab, Alt+F4, right-click, Task Manager while armed
- **Power protection:** `PowerCreateRequest` prevents sleep even from lock screen
- **Session detection:** Lock, logoff, remote disconnect → immediate alarm
- **Virtual desktop detection:** Swipe to another desktop → immediate alarm
- **Multi-monitor:** One guard screen per display, all fullscreen
- **2x wrong PIN → alarm:** Brute-force protection
- **Setup wizard:** First-run guided PIN setup
- **Event history:** SQLite log of all guard events

## License

This project is **source available** under the [PolyForm Shield License 1.0.0](https://polyformproject.org/licenses/shield/1.0.0/).

- ✅ You may view, audit, and learn from the source code
- ✅ You may contribute via pull requests (see [CONTRIBUTING.md](CONTRIBUTING.md))
- ❌ You may NOT use this software to provide a competing product
- ❌ You may NOT redistribute modified versions for commercial purposes

## Development

Requirements:

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell

Commands:

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
```

## Updates

Paunix Guard uses Velopack for installer packaging and GitHub Releases for update distribution.
Unsigned early builds may trigger Windows SmartScreen warnings until code signing is configured.


