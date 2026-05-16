# Security and Privacy

Paunix Guard must look and behave like a user-owned safety tool, not monitoring software.

## Allowed

- Detect that keyboard or pointer input occurred while armed.
- Detect charger state changes.
- Detect sleep, shutdown, and lock/session changes.
- Play local alarm sounds.
- Store local event history.

## Not Allowed

- Recording key contents.
- Remote shell or command execution.
- File browsing or exfiltration.
- Clipboard reads.
- Hidden screenshots or camera access.
- WiFi password extraction.
- Stealth persistence.

Future webcam features must be explicit, user-configurable, and logged.

