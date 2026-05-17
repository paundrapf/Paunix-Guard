import type { APIContext } from "astro";

export const prerender = true;

export function GET(context: APIContext) {
  const origin = context.url.origin;
  return new Response(`User-agent: *\nAllow: /\nSitemap: ${origin}/sitemap.xml\n`, {
    headers: {
      "content-type": "text/plain; charset=utf-8",
      "cache-control": "public, max-age=3600"
    }
  });
}
