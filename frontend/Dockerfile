# syntax=docker/dockerfile:1
# Build context: frontend/

FROM node:26-alpine AS build
WORKDIR /app

# Install dependencies first (cached until the lock file changes).
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/telemetry-dashboard/browser /usr/share/nginx/html

# Unprivileged port; the official image runs worker processes as the 'nginx' user.
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=3 CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
