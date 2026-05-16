# Architecture

Paunix Guard is split into small assemblies so the core security model stays testable and understandable.

- `PaunixGuard.Core` contains guard states, trigger policy, settings, PIN hashing, and orchestration interfaces.
- `PaunixGuard.Windows` contains Windows-only adapters for audio, power, input, session, and shutdown behavior.
- `PaunixGuard.Storage` persists settings and event history under `%LocalAppData%\PaunixGuard`.
- `PaunixGuard.Updater` wraps Velopack so update behavior is isolated from the UI.
- `PaunixGuard.App` is the WPF app shell.

The desktop app is the source of truth. Future phone or website features should send requests to the desktop app or relay, but they must not own OS-level security behavior.

