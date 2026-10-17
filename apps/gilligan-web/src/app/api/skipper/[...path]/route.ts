// Server-only runtime configuration: no API hostname is bundled into the browser.
export const runtime = "nodejs";
export const dynamic = "force-dynamic";

type Context = { params: Promise<{ path: string[] }> };

const hopHeaders = [
  "connection", "keep-alive", "proxy-authenticate", "proxy-authorization",
  "te", "trailer", "transfer-encoding", "upgrade",
];

function cleanHeaders(source: Headers) {
  const headers = new Headers(source);
  source.get("connection")?.split(",").forEach((name) => headers.delete(name.trim()));
  hopHeaders.forEach((name) => headers.delete(name));
  headers.delete("content-length");
  return headers;
}

async function proxy(request: Request, context: Context) {
  const configured = process.env.SKIPPER_API_URL ??
    (process.env.NODE_ENV === "development" ? "http://localhost:5198" : undefined);
  let base: URL;
  try {
    if (!configured) throw new Error("Missing configuration");
    base = new URL(configured);
    if (!["http:", "https:"].includes(base.protocol) || base.username || base.password || base.search || base.hash) {
      throw new Error("Invalid configuration");
    }
  } catch {
    return Response.json({ message: "Set SKIPPER_API_URL to a valid HTTP(S) API base URL." }, { status: 503 });
  }

  const { path } = await context.params;
  // Reject traversal and encoded separators rather than allowing a path to escape the base URL.
  if (path.some((part) => !part || part === "." || part === ".." || /[\\/%]/.test(part))) {
    return Response.json({ message: "Invalid API path." }, { status: 400 });
  }
  base.pathname = `${base.pathname.replace(/\/$/, "")}/`;
  const target = new URL(path.map(encodeURIComponent).join("/"), base);
  target.search = new URL(request.url).search;
  const headers = cleanHeaders(request.headers);
  headers.delete("host");
  // Do not trust client-supplied forwarding metadata or pass Gilligan's origin as Skipper's.
  ["origin", "forwarded", "x-forwarded-host", "x-forwarded-proto", "x-forwarded-for"].forEach((name) => headers.delete(name));
  headers.set("accept-encoding", "identity");

  try {
    const upstream = await fetch(target, {
      method: request.method,
      headers,
      body: ["GET", "HEAD"].includes(request.method) ? undefined : await request.arrayBuffer(),
      cache: "no-store",
      redirect: "manual",
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(30_000)]),
    });
    const responseHeaders = cleanHeaders(upstream.headers);
    // Fetch decodes compressed responses, so their original encoding is no longer valid.
    responseHeaders.delete("content-encoding");
    responseHeaders.set("cache-control", "no-store");
    const location = upstream.headers.get("location");
    if (location) {
      const redirect = new URL(location, target);
      if (redirect.origin !== base.origin || !redirect.pathname.startsWith(base.pathname)) {
        return Response.json({ message: "Skipper returned an unsupported redirect." }, { status: 502 });
      }
      responseHeaders.set("location", `/api/skipper/${redirect.pathname.slice(base.pathname.length)}${redirect.search}${redirect.hash}`);
    }
    return new Response(request.method === "HEAD" ? null : upstream.body, {
      status: upstream.status,
      statusText: upstream.statusText,
      headers: responseHeaders,
    });
  } catch {
    return Response.json({ message: "Skipper is unavailable. Try again later." }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as PATCH, proxy as DELETE, proxy as HEAD, proxy as OPTIONS };
