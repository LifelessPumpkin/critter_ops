// @vitest-environment node
import { afterEach, describe, expect, it, vi } from "vitest";
import { GET, POST } from "./route";

const context = (path = ["api", "animals"]) => ({ params: Promise.resolve({ path }) });
afterEach(() => { vi.unstubAllEnvs(); vi.unstubAllGlobals(); });

describe("runtime Skipper proxy", () => {
  it("reads configuration on each request and preserves repeated query parameters", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response("[]", { headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetch);
    for (const host of ["http://skipper:8080", "https://api.example.com/base"]) {
      vi.stubEnv("SKIPPER_API_URL", host);
      const result = await GET(new Request("http://gilligan/api/skipper/api/animals?statuses=Active&statuses=Retired"), context());
      expect(result.status).toBe(200);
      expect(String(fetch.mock.lastCall?.[0])).toBe(`${host}/api/animals?statuses=Active&statuses=Retired`);
    }
  });

  it("preserves mutation bodies, authorization and upstream validation errors", async () => {
    vi.stubEnv("SKIPPER_API_URL", "http://skipper:8080");
    const fetch = vi.fn().mockResolvedValue(new Response('{"title":"Invalid animal"}', { status: 400, headers: { "Content-Type": "application/problem+json" } }));
    vi.stubGlobal("fetch", fetch);
    const body = '{"name":"Test animal"}';
    const result = await POST(new Request("http://gilligan/api/skipper/api/animals", {
      method: "POST", headers: { "Content-Type": "application/json", Authorization: "Bearer test", Connection: "x-private", "x-private": "remove" }, body,
    }), context());
    const options = fetch.mock.lastCall?.[1];
    expect(options.method).toBe("POST");
    expect(new TextDecoder().decode(options.body)).toBe(body);
    expect(options.headers.get("authorization")).toBe("Bearer test");
    expect(options.headers.has("x-private")).toBe(false);
    expect(result.status).toBe(400);
    expect(await result.json()).toEqual({ title: "Invalid animal" });
  });

  it("reports missing or invalid production configuration without contacting an upstream", async () => {
    vi.stubEnv("NODE_ENV", "production");
    for (const value of [undefined, "", "not-a-url", "file:///etc/passwd", "http://user:secret@skipper"]) {
      vi.stubEnv("SKIPPER_API_URL", value);
      const result = await GET(new Request("http://gilligan/api/skipper/api/animals"), context());
      expect(result.status).toBe(503);
      expect(await result.text()).toContain("SKIPPER_API_URL");
    }
  });

  it("keeps the existing development default", async () => {
    vi.stubEnv("NODE_ENV", "development");
    vi.stubEnv("SKIPPER_API_URL", undefined);
    const fetch = vi.fn().mockResolvedValue(new Response("[]"));
    vi.stubGlobal("fetch", fetch);
    await GET(new Request("http://localhost:3000/api/skipper/api/animals"), context());
    expect(String(fetch.mock.lastCall?.[0])).toBe("http://localhost:5198/api/animals");
  });

  it("returns a useful error for DNS failure or unavailable Skipper", async () => {
    vi.stubEnv("SKIPPER_API_URL", "http://missing-skipper:8080");
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("fetch failed")));
    const result = await GET(new Request("http://gilligan/api/skipper/api/animals"), context());
    expect(result.status).toBe(502);
    expect(await result.json()).toEqual({ message: "Skipper is unavailable. Try again later." });
  });

  it("rejects traversal before fetching and rewrites internal redirects", async () => {
    vi.stubEnv("SKIPPER_API_URL", "http://skipper:8080");
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 302, headers: { Location: "http://skipper:8080/swagger/index.html" } }));
    vi.stubGlobal("fetch", fetch);
    expect((await GET(new Request("http://gilligan/api/skipper/test"), context(["..", "health"]))).status).toBe(400);
    expect(fetch).not.toHaveBeenCalled();
    const result = await GET(new Request("http://gilligan/api/skipper/swagger"), context(["swagger"]));
    expect(result.headers.get("location")).toBe("/api/skipper/swagger/index.html");
  });
});
