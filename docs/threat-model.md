# Threat Model

Paunix Guard protects against short-window opportunistic theft in public workspaces.

## In Scope

- Someone touches the keyboard, mouse, or trackpad while the owner is away.
- Someone unplugs the charger.
- Someone tries to close the lid, sleep, log off, or shut down the laptop.
- The laptop is left unattended for a short period.

## Out of Scope

- Forced power-button shutdown.
- Battery removal or depletion.
- Admin-level process termination.
- Full disk access by an attacker who already has the machine.
- Physical destruction or signal jamming.

