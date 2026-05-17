import { getDownloadStats, jsonResponse, verifyAdminToken, type Env } from "../../_lib/release";

export const onRequestGet: PagesFunction<Env> = async ({ env, request }) => {
  if (!env.ADMIN_STATS_TOKEN) {
    return jsonResponse({ error: "Admin stats token is not configured." }, { status: 503 });
  }

  if (!verifyAdminToken(request, env)) {
    return jsonResponse({ error: "Unauthorized." }, { status: 401 });
  }

  const stats = await getDownloadStats(env);
  return jsonResponse(stats, {
    headers: {
      "cache-control": "private, no-store"
    }
  });
};
