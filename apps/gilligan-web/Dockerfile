# Build from the monorepo root, using Gilligan's existing app-local lockfile.
FROM node:22-bookworm-slim AS dependencies
WORKDIR /repo/apps/gilligan-web
COPY apps/gilligan-web/package.json apps/gilligan-web/package-lock.json ./
RUN npm ci

FROM node:22-bookworm-slim AS builder
WORKDIR /repo/apps/gilligan-web
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=dependencies /repo/apps/gilligan-web/node_modules ./node_modules
COPY apps/gilligan-web/ ./
RUN npm run build

FROM node:22-bookworm-slim AS runtime
LABEL org.opencontainers.image.title="CritterOps Gilligan" \
      org.opencontainers.image.description="Gilligan Next.js frontend"
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    HOSTNAME=0.0.0.0 \
    PORT=3000
WORKDIR /app
COPY --from=builder /repo/apps/gilligan-web/.next/standalone ./
COPY --from=builder /repo/apps/gilligan-web/.next/static ./apps/gilligan-web/.next/static
COPY --from=builder /repo/apps/gilligan-web/public ./apps/gilligan-web/public
# Only the disposable Next.js cache needs to be writable.
RUN mkdir -p apps/gilligan-web/.next/cache \
    && chown node:node apps/gilligan-web/.next/cache
USER node
EXPOSE 3000
CMD ["node", "apps/gilligan-web/server.js"]
