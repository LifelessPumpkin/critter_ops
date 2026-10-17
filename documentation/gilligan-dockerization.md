# Gilligan Dockerization implementation report

## Files changed

- `apps/gilligan-web/Dockerfile` — production multi-stage image.
- `apps/gilligan-web/next.config.ts` — standalone output and monorepo tracing root.
- `.dockerignore` — additional TypeScript, browser-test and npm-log exclusions.
- `apps/gilligan-web/src/app/api/skipper/[...path]/route.ts` — runtime HTTP API proxy.
- `apps/gilligan-web/src/app/api/skipper/[...path]/route.test.ts` — six proxy regression tests.
- `apps/gilligan-web/src/lib/api/animals.ts`
- `apps/gilligan-web/src/lib/api/enclosures.ts`
- `apps/gilligan-web/src/lib/api/activity.ts`
- `apps/gilligan-web/src/lib/api/tasks.ts` — browser API clients now use relative proxy URLs.
- `apps/gilligan-web/src/app/page.tsx` — existing Skipper dashboard links use the proxy.
- `apps/gilligan-web/README.md` — development, configuration, Docker and deployment instructions.
- `.github/workflows/mary-ann-testing.yml` — pre-merge Gilligan Docker build validation.
- `documentation/gilligan-dockerization.md` — this report.

No package/lockfile changes, full Compose stack, publishing, deployment, new Gilligan health endpoint, Docker HEALTHCHECK, probe script, push or PR description were introduced.

## Repository review and Docker architecture

Gilligan uses Next.js 16.4.0 and an app-local npm lockfile. CI already uses Node 22. There is no root npm workspace or external Ginger dependency; the reusable UI is currently in Gilligan's own `src/components/ui`. Production standalone output was previously disabled. Existing API clients ran from client components and used `NEXT_PUBLIC_API_URL` with a localhost:5198 fallback; that public setting could be embedded at build time. The dashboard also referenced it.

The Dockerfile uses official `node:22-bookworm-slim` in dependency, builder and runtime stages. `npm ci` uses the unchanged lockfile before source is copied. It builds from the monorepo root but copies only Gilligan source. Repository-wide file tracing produces a standalone tree with `apps/gilligan-web/server.js`; the runtime separately receives static/public assets. Future shared-package dependencies will require adding their manifests/source to the Dockerfile's build inputs. Current shared UI builds successfully inside Gilligan.

The runtime runs as the existing `node` user (UID/GID 1000), listening on 0.0.0.0:3000. Application artifacts are root-owned/readable; only disposable Next.js cache storage is writable. The tested image is approximately 295 MB. It requires no persistent volume or privileged execution. It uses normal Docker environment, network and port settings, compatible with future Compose, Swarm and GHCR use.

## API networking and future Compose

Browser calls use relative `/api/skipper/api/...` URLs. A small Node route forwards HTTP requests to the server-only `SKIPPER_API_URL` read at request time. It preserves methods, bodies, query parameters and upstream errors; removes transport headers; streams responses; prevents caching; limits upstream waits to 30 seconds; rewrites redirects within the configured API base. It returns 503 for missing/invalid configuration and 502 for an unavailable upstream without exposing internal destinations. No build-time API setting is required. Existing development retains the verified Skipper launch-profile default of localhost:5198; production has no localhost fallback.

Future service names are `gilligan`, `skipper`, `professor`, with Gilligan:3000 -> Skipper:8080 -> Professor:5432. Set `SKIPPER_API_URL=http://skipper:8080`, explicitly configure Skipper's `ASPNETCORE_HTTP_PORTS=8080` (its current Docker default is 5198), and supply Skipper's `ConnectionStrings__ProfessorDb`. Gilligan never connects directly to PostgreSQL and does not wait for dependencies at startup. The complete network/Compose configuration and database startup policy remain future work. Existing database-only Compose configuration was preserved.

Relative browser URLs allow external HTTPS termination and origin-root reverse proxy hosting; proxy configuration is not included. Subpath hosting requires a separate build-time `basePath` decision. The route does not implement WebSocket upgrades or cookie domain/path rewriting; neither is used by the current API clients. Future authentication changes need their own review.

## Local validation on 2026-10-11

| Check | Result | Evidence / limits |
| --- | --- | --- |
| Gilligan Docker build | PASS | Built from repository root with locked dependencies; Next.js standalone output compiled successfully. |
| Existing Skipper Docker build | PASS | Existing Dockerfile still builds with the extended root ignore rules. |
| Container startup / internal port | PASS | Next.js logs reported ready at 0.0.0.0:3000. |
| Host accessibility | PASS | Published host port 3187 served `/animals` with HTTP 200. |
| Non-root runtime | PASS | `docker exec ... id` reported UID/GID 1000; image user is `node`. |
| Runtime contents | PASS | No app source directory, local env file, Vitest installation or Git directory in checked runtime paths. |
| Skipper unavailable | PASS | Gilligan started and served `/animals`; proxy returned 502 JSON for an unresolvable Skipper hostname. |
| Skipper integration | PASS | Real Skipper and disposable PostgreSQL ran on an isolated Docker network. Existing migrations were applied only to that disposable database. Browser Animals page displayed the test animal and enclosure via Gilligan. |
| API mutation / upstream errors | PASS | Enclosure and animal POST requests succeeded through Gilligan; invalid animal POST preserved HTTP 400. |
| Runtime address changes | PASS | Same image ID `d22eb48ad4f0ec9dd6c3348455f8322883c2a23ccdd9e40f0a7371ff2fe70b85` served browser data with `skipper:8080`, then after recreation with `skipper-alt:8080`. No rebuild. |
| Restart / recreation | PASS | Browser test data remained available after both operations; state was stored outside Gilligan. |
| Browser/static rendering | PASS | Production Animals page hydrated, loaded data and displayed its stylesheet correctly; browser console snapshot had no warning/error entries. Dedicated requests for every static/public asset were NOT RUN. |
| Missing/invalid configuration | PASS (unit) | Regression tests verify 503 and useful configuration message. Actual missing-config container HTTP check NOT RUN. |
| Local development startup | PASS | Existing `npm run dev -- --port 3188` started and served `/animals` with HTTP 200. Development API/HMR verification NOT RUN. |
| Other route HTTP sweep | NOT RUN | Additional bundled HTTP validation approval was declined. |
| Temporary reverse proxy | NOT RUN | Remains manual validation before deployment. |
| Compose / Swarm / GHCR deployment | NOT RUN | Image is prepared; actual deployment/publishing is outside scope. |

The initial local Docker build was blocked by root-owned Docker buildx state. Building with `BUILDX_CONFIG=/private/tmp/critter-gilligan-buildx` succeeded without changing the user's Docker configuration. The production build also succeeded outside Docker. No claim is made that this was a separate clean Git checkout; Git inspection commands were not used. The Docker build ignored host node_modules, .next and environment files and installed dependencies afresh.

## Existing tests

| Check | Result |
| --- | --- |
| TypeScript (`tsc --noEmit`) | PASS |
| Mary Ann Gilligan wrapper / Vitest | PASS — 2 files, 7 tests, including six new proxy tests |
| Production build outside Docker | PASS |
| Production build inside Docker with Node 22 | PASS |
| Lint of changed TypeScript files | PASS |
| Full Gilligan lint | FAIL — six pre-existing React Hooks errors in untouched `AnimalOverview.tsx` and `Selection.tsx` |
| Full Mary Ann Skipper/security suites | NOT RUN — unrelated to this frontend change |
| Hosted Mary Ann CI run | NOT RUN — workflow updated locally; no push |

The host uses Node 25.4.0; container build validation used Node 22. Dependency installation reported five high-severity audit findings in the existing lockfile. Dependencies were intentionally not upgraded in this ticket; these findings were not individually assessed here.

## Security and limitations

Local environment/credential files are excluded by the existing root ignore rules. API configuration is injected into the container environment, never baked into the Dockerfile or browser JavaScript. The final stage copies only traced runtime dependencies/assets, not the installed build dependency tree. Non-root execution was verified. No extra Linux packages or health-check dependencies were added. Existing fonts require Google Fonts access at build time. The Node major tag is not digest-pinned and follows patch releases.

Remaining validation includes a complete route/static-asset sweep, development API/HMR behavior, and reverse proxy testing. Full lint is not green due to existing unrelated errors. No Gilligan health monitoring was implemented, per the revised requirements.

## Follow-up tickets

- Three-service local Compose environment, including Skipper port and database configuration.
- GHCR publishing and image tagging.
- Production deployment and reverse proxy/TLS configuration.
- Container monitoring and deployment checks; optionally a dedicated Gilligan health endpoint later.
- Existing frontend lint failures and dependency audit findings.
