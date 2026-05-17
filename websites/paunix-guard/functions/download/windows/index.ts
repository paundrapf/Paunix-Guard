import {
  fallbackInstallerUrl,
  getLatestMetadata,
  recordDownload,
  releaseKeys,
  streamObject,
  type Env
} from "../../_lib/release";

async function handleDownload(env: Env, shouldTrack: boolean) {
  const metadata = await getLatestMetadata(env);
  const installer = await env.PAUNIX_RELEASES?.get(releaseKeys.latestInstaller);

  if (shouldTrack) {
    await recordDownload(env, metadata);
  }

  if (!installer) {
    return Response.redirect(fallbackInstallerUrl(env), 302);
  }

  return streamObject(installer, releaseKeys.latestInstaller, {
    headers: {
      "content-disposition": `attachment; filename="PaunixGuard-${metadata.version}-win-Setup.exe"`
    }
  });
}

export const onRequestGet: PagesFunction<Env> = async ({ env }) => {
  return handleDownload(env, true);
};

export const onRequestHead: PagesFunction<Env> = async ({ env }) => {
  return handleDownload(env, false);
};
