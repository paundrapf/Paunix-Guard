export type ReleaseMetadata = {
  version: string;
  channel: "stable" | "beta";
  installerUrl: string;
  releaseNotesUrl: string;
  publishedAt: string;
  sha256: string;
};

export type DownloadStats = {
  totalDownloads: number;
  lastDownloadedAt: string | null;
  versions: Record<string, number>;
};

export type Env = {
  PAUNIX_RELEASES?: R2Bucket;
  PUBLIC_SITE_URL?: string;
  PUBLIC_DOWNLOAD_FALLBACK_URL?: string;
  ADMIN_STATS_TOKEN?: string;
};

export const releaseKeys = {
  latestMetadata: "metadata/latest.json",
  latestInstaller: "installers/windows/latest/PaunixGuard-win-Setup.exe",
  updatePrefix: "updates/windows/",
  downloadStats: "admin/download-stats.json"
} as const;

export function defaultMetadata(env: Env): ReleaseMetadata {
  const siteUrl = env.PUBLIC_SITE_URL ?? "https://paunix-guard.pages.dev";
  return {
    version: "0.1.2",
    channel: "stable",
    installerUrl: `${siteUrl}/download/windows`,
    releaseNotesUrl: `${siteUrl}/changelog/0.1.2`,
    publishedAt: "2026-05-17T00:00:00Z",
    sha256: ""
  };
}

export function fallbackInstallerUrl(env: Env) {
  return (
    env.PUBLIC_DOWNLOAD_FALLBACK_URL ??
    "https://github.com/paundrapf/Paunix-Guard/releases/download/v0.1.1/PaunixGuard-win-Setup.exe"
  );
}

export async function getLatestMetadata(env: Env): Promise<ReleaseMetadata> {
  const fallback = defaultMetadata(env);
  const object = await env.PAUNIX_RELEASES?.get(releaseKeys.latestMetadata);

  if (!object) {
    return fallback;
  }

  try {
    return {
      ...fallback,
      ...((await object.json()) as Partial<ReleaseMetadata>)
    };
  } catch {
    return fallback;
  }
}

export async function getDownloadStats(env: Env): Promise<DownloadStats> {
  const fallback: DownloadStats = {
    totalDownloads: 0,
    lastDownloadedAt: null,
    versions: {}
  };

  const object = await env.PAUNIX_RELEASES?.get(releaseKeys.downloadStats);
  if (!object) {
    return fallback;
  }

  try {
    return {
      ...fallback,
      ...((await object.json()) as Partial<DownloadStats>)
    };
  } catch {
    return fallback;
  }
}

export async function recordDownload(env: Env, metadata: ReleaseMetadata) {
  if (!env.PAUNIX_RELEASES) {
    return;
  }

  const current = await getDownloadStats(env);
  const next: DownloadStats = {
    totalDownloads: current.totalDownloads + 1,
    lastDownloadedAt: new Date().toISOString(),
    versions: {
      ...current.versions,
      [metadata.version]: (current.versions[metadata.version] ?? 0) + 1
    }
  };

  await env.PAUNIX_RELEASES.put(releaseKeys.downloadStats, JSON.stringify(next, null, 2), {
    httpMetadata: {
      contentType: "application/json; charset=utf-8"
    }
  });
}

export function verifyAdminToken(request: Request, env: Env) {
  if (!env.ADMIN_STATS_TOKEN) {
    return false;
  }

  const authorization = request.headers.get("authorization");
  const bearer = authorization?.startsWith("Bearer ") ? authorization.slice("Bearer ".length).trim() : "";
  const headerToken = request.headers.get("x-admin-token")?.trim() ?? "";
  return bearer === env.ADMIN_STATS_TOKEN || headerToken === env.ADMIN_STATS_TOKEN;
}

export function jsonResponse(data: unknown, init: ResponseInit = {}) {
  const headers = new Headers(init.headers);
  headers.set("content-type", "application/json; charset=utf-8");
  headers.set("cache-control", headers.get("cache-control") ?? "public, max-age=60, must-revalidate");

  return new Response(JSON.stringify(data, null, 2), {
    ...init,
    headers
  });
}

export function streamObject(object: R2ObjectBody, key: string, init: ResponseInit = {}) {
  const headers = new Headers(init.headers);
  object.writeHttpMetadata(headers);
  headers.set("content-type", headers.get("content-type") ?? contentType(key));
  headers.set("etag", object.httpEtag);
  headers.set("cache-control", headers.get("cache-control") ?? cacheControl(key));

  return new Response(object.body, {
    ...init,
    headers
  });
}

export function sanitizeObjectPath(param: string | string[] | undefined): string | null {
  const raw = Array.isArray(param) ? param.join("/") : param;
  if (!raw) {
    return null;
  }

  const normalized = raw.replaceAll("\\", "/").replace(/^\/+/, "");
  if (!normalized || normalized.includes("..") || normalized.includes("//")) {
    return null;
  }

  return normalized;
}

function contentType(key: string) {
  if (key.endsWith(".json")) return "application/json; charset=utf-8";
  if (key.endsWith(".txt") || key.endsWith("RELEASES")) return "text/plain; charset=utf-8";
  if (key.endsWith(".exe")) return "application/vnd.microsoft.portable-executable";
  if (key.endsWith(".nupkg")) return "application/octet-stream";
  return "application/octet-stream";
}

function cacheControl(key: string) {
  if (key.endsWith(".nupkg") || key.endsWith(".exe")) {
    return "public, max-age=31536000, immutable";
  }

  return "public, max-age=60, must-revalidate";
}
