# Cloudflare Release Process

Paunix Guard uses the website as the user-facing download path and Cloudflare R2 as the public Velopack feed host.

## Resources

- Cloudflare Pages project: `paunix-guard`
- R2 bucket: `paunix-guard-releases`
- Website source: `websites/paunix-guard`
- User installer key: `installers/windows/latest/PaunixGuard-win-Setup.exe`
- Velopack feed keys: `updates/windows/RELEASES`, `updates/windows/releases.win.json`, and `updates/windows/PaunixGuard-<version>-full.nupkg`
- Metadata keys: `metadata/latest.json` and `metadata/changelog.json`

## Recommended Release Command

```powershell
.\scripts\release-cloudflare.ps1 -Version 0.1.1 -Channel stable -Configuration Release
```

The script builds and tests the desktop app, packages Velopack assets, smoke-launches the published app, uploads installer/feed metadata to R2, builds the website, and deploys Cloudflare Pages.

## Manual Verification

After deployment, verify these URLs:

- `https://paunix-guard.pages.dev/download/windows`
- `https://paunix-guard.pages.dev/api/latest`
- `https://paunix-guard.pages.dev/updates/windows/RELEASES`

Then install from the website on a clean Windows profile. On first run, create a PIN in the setup wizard and confirm that the main app opens immediately after Finish.

## GitHub Releases

GitHub Releases remain useful for source tags and technical release history. Normal users should be sent to the website, not the GitHub asset list.
