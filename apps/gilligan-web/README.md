# Gilligan Web App

Gilligan is the Next.js 16.4 frontend for CritterOps. Use Node.js 22 (matching CI and the Docker image) and npm. Dependencies are managed by `apps/gilligan-web/package-lock.json`; there are currently no npm workspaces or external Ginger package imports. UI components live in `src/components/ui`. The Dockerfile copies only Gilligan source. If shared packages are introduced later, add their manifests/source to its build stages; repository-wide output tracing is already enabled.

## Local development

```sh
cd apps/gilligan-web
npm ci
npm run dev
```

Open http://localhost:3000. In development only, Skipper defaults to its existing HTTP launch-profile address, `http://localhost:5198`. Override it with `SKIPPER_API_URL` in the process environment or `apps/gilligan-web/.env.local`. Restart the development server after changing environment configuration. Docker is not required for Gilligan development.

`NEXT_PUBLIC_API_URL` is no longer used. Migrate existing settings to server-only `SKIPPER_API_URL`. Browser requests use relative `/api/skipper/api/...` paths, including animals, enclosures, activity and tasks. The dashboard's existing Skipper Swagger and health links also pass through `/api/skipper`; these are Skipper resources, not Gilligan health endpoints. Skipper only enables Swagger in its Development environment.

## Production Docker image

Run from the **CritterOps monorepo root**:

```sh
docker build -f apps/gilligan-web/Dockerfile -t critterops/gilligan:local .
docker run -d --name critterops-gilligan \
  -p 3000:3000 \
  -e SKIPPER_API_URL=http://host.docker.internal:5198 \
  critterops/gilligan:local
```

The example reaches the existing Skipper development port on the host. Docker Desktop provides `host.docker.internal`; on Linux Docker Engine add `--add-host=host.docker.internal:host-gateway` and ensure Skipper listens on an address reachable from Docker. For a shared Docker network, supply its service address instead. Publish another host port with `-p 8081:3000`.

The three stages install dependencies with `npm ci`, build Next.js, and copy only traced standalone runtime files, static assets and public assets into `node:22-bookworm-slim`. Next.js traces from the monorepo root; the entry point is `apps/gilligan-web/server.js` inside the standalone tree. The existing Node `node` user runs the process, listening on `0.0.0.0:3000`. Application artifacts are root-owned and readable by that user; only the disposable Next.js cache is writable. No persistent frontend volume is needed. Application state belongs in Skipper/Professor.

The root `.dockerignore` excludes local environment files, credentials, Git metadata, installed dependencies, build output and test artifacts. No API URL or secrets are passed to the build. Existing Google fonts are downloaded by `next/font` at build time, so the build needs npm registry and Google Fonts network access. Package versions and the lockfile remain unchanged. Node's major-version base tag follows Node 22 patch updates; it is not digest-pinned.

## Runtime API configuration

`SKIPPER_API_URL` is the HTTP(S) base URL **reachable from the Gilligan server**, for example `http://skipper:8080` or `https://api.example.com`. An optional URL path prefix is supported; credentials, query strings and fragments are rejected. Supply credentials using an appropriate future authentication design, not URL userinfo.

The Next.js Node route handler reads the setting on each API request, so the same image can be recreated with a different address without rebuilding. Do not put it in `next.config.ts`'s `env` option or use a `NEXT_PUBLIC_` variable: public variables are compiled into browser bundles. Missing or invalid production configuration returns a useful 503 JSON message when an API request occurs. Gilligan still starts and serves pages. Unreachable Skipper, DNS failures and upstream timeouts return 502; existing frontend error states handle unsuccessful responses. Requests time out after 30 seconds; there is no startup wait for Skipper or PostgreSQL.

The proxy preserves query parameters, HTTP methods, request bodies, application headers, response status and body, including API validation errors. Hop-by-hop/transport headers are removed and requests/responses are not cached. Responses stream through. Internal redirects under the configured API base are rewritten to relative proxy URLs; external or out-of-base redirects return 502 to avoid exposing internal destinations. The proxy is for existing HTTP API and Swagger traffic; it does not implement WebSocket upgrades. Cookies are passed through without domain/path rewriting; authentication changes would need separate review.

## Deployment compatibility

The same standard image can be used by `docker run`, Docker Compose or `docker service create`, and tagged for future `ghcr.io/<owner>/<repository>/gilligan:<tag>` publishing. No registry credentials, publishing workflow, Swarm configuration or deployment automation is included.

A future Compose stack can use service names `gilligan`, `skipper`, `professor` on a shared network:

```text
Browser -> Gilligan :3000 -> Skipper :8080 -> Professor :5432
```

Set Gilligan's `SKIPPER_API_URL=http://skipper:8080`. **Skipper currently defaults to container port 5198**, so the future Compose ticket must explicitly set `ASPNETCORE_HTTP_PORTS=8080` (or use `http://skipper:5198` instead). Skipper needs `ConnectionStrings__ProfessorDb` pointing to Professor. Gilligan does not connect to PostgreSQL. The browser resolves only Gilligan's public origin, never Docker service names. Startup ordering, database configuration and the complete Compose stack remain future work; the existing database-only Compose file is unchanged.

A reverse proxy may terminate HTTPS and forward Gilligan's public Host and trusted forwarding headers to port 3000. Serve Gilligan at the origin root and route `/api/skipper` and `/_next` to it; no production hostname is built into the app. Subpath hosting would require an explicit Next.js `basePath` build configuration. No reverse proxy or TLS configuration is included.

There is no Gilligan health endpoint, Docker HEALTHCHECK or probe script. Validate startup with normal routes and container logs:

```sh
curl -f http://localhost:3000/animals
curl -f http://localhost:3000/api/skipper/api/animals
docker logs critterops-gilligan
docker exec critterops-gilligan id
docker restart critterops-gilligan
```

For runtime configuration validation, remove and recreate the container from the same image with a different `SKIPPER_API_URL`, then repeat an API request. A dedicated Gilligan health endpoint is optional future monitoring work.

## Validation

```sh
cd apps/gilligan-web
npm run lint
npx tsc --noEmit
npm test
npm run build
```

The existing Mary Ann wrapper is `./packages/mary-ann-tests/scripts/test-gilligan.sh` from the monorepo root. Its tests include runtime proxy regression coverage. Pre-merge Mary Ann CI also builds the Gilligan Docker image; it does not publish it or deploy anything.
