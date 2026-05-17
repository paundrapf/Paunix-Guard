# Paunix Guard

<img src="assets/logo-128x128.png" alt="Paunix Guard" width="128" align="right">

[![License](https://img.shields.io/badge/license-PolyForm%20Shield-blue)](https://polyformproject.org/licenses/shield/1.0.0/)
[![Download v0.1.2](https://img.shields.io/badge/download-v0.1.2-brightgreen)](https://paunix-guard.pages.dev/download)

Paunix Guard is a Windows-first, privacy-first laptop guard for public workspaces.
It is designed to protect a laptop while the owner steps away briefly by reacting to physical theft signals such as power unplug, input activity, sleep attempts, shutdown attempts, lid close, and desktop/session switches.

The first implementation is desktop-only and offline-first. Phone pairing, webcam snapshots, and cloud relay are planned companion features, not requirements for core protection.

## Principles

- Offline protection must work without a phone or internet connection.
- The app must never record key contents, browse user files, read clipboard data, open a remote shell, or run hidden surveillance.
- Settings and event history live in `%LocalAppData%\PaunixGuard`, not beside the executable.
- `AGENTS.md` and `MEMORY.md` are local AI context files and are intentionally ignored by git.

## Features

- **7 triggers:** Charger unplug, keyboard/mouse input, lid close, sleep attempt, shutdown/logoff, desktop/session switch, manual panic
- **Grace + Warning:** 3-second grace after arming. Then 4-second warning countdown with each keystroke resetting it.
- **Kiosk mode:** Blocks common escape shortcuts such as Win key, Alt+Tab, Alt+F4, Alt+Space, Ctrl+Esc, and right-click while armed
- **Power protection:** `PowerCreateRequest` keeps the system awake where Windows allows user-mode apps to do so
- **Session detection:** Lock, logoff, and remote disconnect trigger an immediate alarm when Windows reports the session change
- **Virtual desktop detection:** Moving away from the guard desktop triggers an immediate alarm
- **Multi-monitor:** One guard screen per display, all fullscreen
- **2x wrong PIN -> alarm:** Brute-force protection
- **Setup wizard:** First-run guided PIN setup
- **Event history:** SQLite log of all guard events

## License

This project is **source available** under the [PolyForm Shield License 1.0.0](https://polyformproject.org/licenses/shield/1.0.0/).

- You may view, audit, and learn from the source code.
- You may contribute via pull requests (see [CONTRIBUTING.md](CONTRIBUTING.md)).
- You may NOT use this software to provide a competing product.
- You may NOT redistribute modified versions for commercial purposes.

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

Paunix Guard uses Velopack for installer packaging and a public Cloudflare-hosted update feed for update distribution.
Automatic in-app updates work when Paunix Guard is installed with the setup installer. Standalone EXE/zip builds can still be updated manually. Core protection works entirely offline.

Unsigned early builds may trigger Windows SmartScreen warnings until code signing is configured.

## Website and Distribution

The user-facing website lives in `websites/paunix-guard` and deploys to Cloudflare Pages.
Release installers and Velopack feed files are served from Cloudflare R2 through website routes so users only need the Windows installer.

```powershell
cd websites\paunix-guard
npm install
npm run build
```

Release packaging and Cloudflare upload are documented in [docs/release-cloudflare.md](docs/release-cloudflare.md).

## FAQ

### Why does Windows warn me about this app?
Because Paunix Guard is not code-signed yet. This is normal for early source-available desktop projects. Click **More info** -> **Run anyway** to continue. We plan to add code signing in a future release.

### I forgot my PIN. What do I do?
Enter your current PIN in the main window, then click **Reset PIN**. If you truly forgot it, delete `%LocalAppData%\PaunixGuard\settings.json` and restart the app. The setup wizard will guide you through creating a new PIN.

### Does this work without internet?
Yes. All protection features (triggers, alarm, PIN, guard screen) work entirely offline. Update checks happen only when internet is available.

### Will the alarm go off if I close my laptop lid?
Yes - lid close is a trigger by default. You can disable it in **Settings -> Triggers**.
