# Cloudflare Runtime

This website is built as static Astro pages and uses Cloudflare Pages Functions in `functions/` for runtime behavior.

Routes:

- `/download/windows` streams the latest installer from R2 or falls back to the current GitHub installer.
- `/api/latest` returns the latest release metadata.
- `/updates/windows/*` streams Velopack update feed assets from R2.

The R2 binding name is `PAUNIX_RELEASES`.
