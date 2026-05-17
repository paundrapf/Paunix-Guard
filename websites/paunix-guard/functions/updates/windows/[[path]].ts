import { jsonResponse, releaseKeys, sanitizeObjectPath, streamObject, type Env } from "../../_lib/release";

export const onRequestGet: PagesFunction<Env> = async ({ env, params }) => {
  const path = sanitizeObjectPath(params.path);
  if (!path) {
    return jsonResponse({ error: "Invalid update asset path." }, { status: 400 });
  }

  if (!env.PAUNIX_RELEASES) {
    return jsonResponse({ error: "Release bucket is not configured." }, { status: 503 });
  }

  const key = `${releaseKeys.updatePrefix}${path}`;
  const object = await env.PAUNIX_RELEASES.get(key);
  if (!object) {
    return jsonResponse({ error: "Update asset not found." }, { status: 404 });
  }

  return streamObject(object, key);
};

export const onRequestHead = onRequestGet;
