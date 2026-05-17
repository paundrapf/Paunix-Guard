import type { APIContext } from "astro";

export const prerender = true;

const routes = ["/", "/download", "/security", "/docs", "/faq", "/support", "/changelog", "/changelog/0.1.2", "/changelog/0.1.1"];

export function GET(context: APIContext) {
  const urls = routes
    .map((route) => `<url><loc>${new URL(route, context.url.origin).href}</loc></url>`)
    .join("");

  return new Response(`<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${urls}</urlset>`, {
    headers: {
      "content-type": "application/xml; charset=utf-8",
      "cache-control": "public, max-age=3600"
    }
  });
}
