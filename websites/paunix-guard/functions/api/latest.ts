import { getLatestMetadata, jsonResponse, type Env } from "../_lib/release";

export const onRequestGet: PagesFunction<Env> = async ({ env }) => {
  const metadata = await getLatestMetadata(env);
  return jsonResponse(metadata);
};
