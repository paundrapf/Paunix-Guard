# Paunix Guard

Paunix Guard is a Windows-first, privacy-first laptop guard for public workspaces.
It is designed to protect a laptop while the owner steps away briefly by reacting to physical theft signals such as power unplug, input activity, sleep attempts, and shutdown attempts.

The first implementation is desktop-only and offline-first. Phone pairing, webcam snapshots, and cloud relay are planned companion features, not requirements for core protection.

## Principles

- Offline protection must work without a phone or internet connection.
- The app must never record key contents, browse user files, read clipboard data, open a remote shell, or run hidden surveillance.
- Settings and event history live in `%LocalAppData%\PaunixGuard`, not beside the executable.
- `AGENTS.md` and `MEMORY.md` are local AI context files and are intentionally ignored by git.

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

## License

MIT

