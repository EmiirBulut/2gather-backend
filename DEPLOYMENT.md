# Deployment — Render + Cloudflare

Target architecture: GitHub → Render (Static Site for the frontend, Docker Web Service for the backend, Managed PostgreSQL) → Cloudflare DNS on custom `emirbulutt.com` subdomains.

This document is the handoff for the manual steps in the Render and Cloudflare dashboards. Nothing described here has been created yet — no Render services, no Postgres instance, no DNS records, no domains.

---

## Render Frontend (Static Site)

| Setting | Value |
|---|---|
| Service type | Static Site |
| Repository | `2gather-frontend` |
| Branch | `main` |
| Root Directory | `/` (this repo is not a monorepo) |
| Build Command | `npm ci && npm run build` |
| Publish Directory | `dist` |

**Required environment variables** (set at build time — Vite inlines `VITE_*` vars into the bundle during `vite build`, so they must be present in the Render *build* environment, not just runtime):

| Variable | Initial value | Later value |
|---|---|---|
| `VITE_API_BASE_URL` | the Render backend service's `.onrender.com` URL | `https://api.<app>.emirbulutt.com` once the custom domain is live |

The repo's existing convention (`VITE_API_BASE_URL`, already used in `.env.development`/`src/services/api.ts`) was kept rather than introducing a new `VITE_API_URL` name — flagged in the handoff report in case the exact name `VITE_API_URL` is actually wanted; renaming is a one-line change in `src/services/api.ts` if so.

**Client-side routing (required manual step):** the app uses `createBrowserRouter` (history-mode routing), so a directly-opened nested URL (e.g. `/lists/abc/items`) needs a rewrite to `index.html` or it 404s. Verified against Render's current official docs: Render Static Sites do **not** support a Netlify-style `_redirects` file — the supported mechanism is a dashboard rule. After creating the Static Site, go to its **Redirects/Rewrites** tab and add:

| Source | Destination | Action |
|---|---|---|
| `/*` | `/index.html` | `Rewrite` |

---

## Render Backend (Docker Web Service)

| Setting | Value |
|---|---|
| Service type | Web Service (Docker runtime) |
| Repository | `2gather-backend` |
| Branch | `main` |
| Root Directory | `/` |
| Dockerfile Path | `Dockerfile` |
| Health Check Path | `/health` |

**Listening port:** the app does not have a fixed production port. `Program.cs` reads the `PORT` environment variable Render injects at runtime and binds Kestrel to `http://0.0.0.0:$PORT` when it's present; Render's Docker runtime detects the port itself. The Dockerfile's `EXPOSE 8080` is only a fallback default for running the image locally with plain `docker run` (no `PORT` set) — it is not the actual value Render will use.

**Required environment variables:**

| Variable | Purpose | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Also baked into the image as a default; setting it here is belt-and-suspenders |
| `ConnectionStrings__DefaultConnection` | Postgres connection string | From the Render Postgres instance — see below |
| `Jwt__Secret` | JWT signing key | **Must** be a real random value ≥32 chars — the app throws at startup in Production if this is missing or still the placeholder |
| `Jwt__Issuer` | `TwoGatherApi` | Non-secret; can also just come from the checked-in `appsettings.json` default |
| `Jwt__Audience` | `TwoGatherClient` | Same as above |
| `Resend__ApiKey` | Real Resend API key | Only needed once outbound invite emails must actually send |
| `Resend__FromEmail`, `Resend__FromName`, `Resend__AppBaseUrl` | Email sending config | `Resend__AppBaseUrl` should be the frontend's final URL |
| `Resend__WebhookSecret` | Signing secret (`whsec_...`) of the Resend webhook | Enables invite delivery tracking. In Resend → **Webhooks** add `https://<api-domain>/api/webhooks/resend` with the events `email.sent`, `email.delivered`, `email.bounced`, `email.complained`, `email.failed`. Without it the endpoint returns 404 and invites only show sent/failed |
| `R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey` | Cloudflare R2 credentials for item images | R2 API token with Object Read & Write on the bucket. If any R2 value is missing the API falls back to local disk, which Render wipes on every deploy (a warning is logged at startup) |
| `R2__BucketName` | R2 bucket name | e.g. `2gather-images` |
| `R2__PublicBaseUrl` | Public URL of the bucket | The bucket's custom domain (recommended) or its `r2.dev` URL — no trailing slash |
| `Cors__AllowedOrigins` | Comma-separated allowed origins | e.g. `https://<app>.emirbulutt.com` — see CORS section below |

Double-underscore (`__`) is ASP.NET Core's standard environment-variable path separator for nested config keys (`ConnectionStrings:DefaultConnection` → `ConnectionStrings__DefaultConnection`) — no custom config system involved.

---

## Render PostgreSQL

Create the Render Managed PostgreSQL instance first (or in parallel) and copy its **External Database URL** (needed for local migration runs) and **Internal Database URL** (what the backend service should actually use at runtime, since it's on Render's private network and doesn't incur the extra hop/cost of the external URL).

Render gives you a `postgres://user:pass@host/db` URL; ASP.NET Core / Npgsql expects the keyword=value form. Convert it to:
```
Host=<host>;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require
```
and set that as `ConnectionStrings__DefaultConnection` on the backend Web Service (using the **internal** URL's host once the backend is actually deployed on Render; use the **external** URL from your own machine for the one-off migration step below).

### Migration strategy

`Database.Migrate()` is **not** called at application startup — running schema migrations automatically on every deploy risks races across replicas and applies un-reviewed schema changes without a controlled step. Two options, in order of how you'll likely actually use them:

**Option A — simplest, do this first (no image changes needed):**
From your own machine (which has the .NET SDK), run:
```bash
dotnet tool install --global dotnet-ef   # once, if not already installed
cd 2gather-backend
ConnectionStrings__DefaultConnection="Host=<render-external-host>;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require" \
  dotnet ef database update \
  --project src/TwoGather.Infrastructure \
  --startup-project src/TwoGather.Api
```
Run this before the first deploy, and again after any deploy whose commits include new migrations.

**Option B — bundle-based, once you want it wired into deploys:**
The Dockerfile has a `bundle` build stage that produces a self-contained `efbundle` executable (it embeds its own runtime, so — unlike the `dotnet ef` CLI — it can run without the SDK or project source present, which the final runtime image intentionally excludes). It's copied into the final image alongside the app. To use it:
```bash
./efbundle --connection "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require"
```
This can be run from a Render **Shell** session on the backend service, or wired into Render's **Pre-Deploy Command** feature if your plan supports it. Render's own docs describe the pre-deploy command as running "in an isolated environment separate from your deployed service" after the build phase, without stating explicitly whether it shares the built Docker image's filesystem — **verify `efbundle` is actually present and runnable from the pre-deploy context before relying on it**; fall back to Option A if not.

No migration was run against any real database as part of preparing this repo.

---

## Custom Domains

Intended pattern (values below are placeholders — the actual `<app>` subdomain and every DNS target come from Render once the services exist; nothing here has been invented):

- Frontend: `https://<app>.emirbulutt.com`
- Backend API: `https://api.<app>.emirbulutt.com`

Sequence: add each custom domain in the Render service's **Settings → Custom Domains**. Render will then show the exact DNS record (typically a `CNAME`, sometimes an `A`/`ANAME` depending on whether it's a root or subdomain) it needs — copy that value into Cloudflare exactly as Render displays it.

---

## Cloudflare

Cloudflare is the DNS provider for `emirbulutt.com` (registered at Squarespace, which only needs to point its nameservers at Cloudflare once — not part of this repo prep). For each custom domain added in Render:

1. Render shows a specific record type + target (e.g. a `CNAME` pointing at something like `<service>.onrender.com`).
2. Add that exact record in Cloudflare's DNS dashboard for `emirbulutt.com`.
3. Set the proxy status per Render's instructions for that record (Render sometimes requires "DNS only" / grey-clouded rather than proxied, since Render terminates its own TLS — follow whatever Render's dashboard says at the time).
4. Wait for Render to verify the domain (shows as verified/active in the Render dashboard).

No DNS target is documented here in advance — Render only generates it once the service and custom domain actually exist.

---

## Cloudflare R2 (item images)

1. Cloudflare dashboard → **R2** → enable R2 (a payment method may be required even on the free tier) → **Create bucket** (e.g. `2gather-images`).
2. Bucket → **Settings** → **Public access**: connect a custom domain (e.g. `images.<your-domain>`, recommended for production) or enable the rate-limited `r2.dev` URL for testing. That URL is `R2__PublicBaseUrl`.
3. R2 → **Manage API tokens** → create a token with **Object Read & Write** scoped to the bucket. Copy the Access Key ID and Secret into `R2__AccessKeyId` / `R2__SecretAccessKey`; the Account ID shown on the R2 overview page goes into `R2__AccountId`.
4. Set the five `R2__*` variables on the Render backend service and redeploy. Uploads go to `items/<itemId>/<random>.<ext>`; replaced or deleted item images are removed from the bucket.

Free tier (at time of writing): 10 GB storage, 1M write / 10M read operations per month, no egress fees.

---

## CORS

`Cors:AllowedOrigins` (config key, environment variable `Cors__AllowedOrigins`) is a comma-separated list, read once at startup in `Program.cs`. Local dev's default (`http://localhost:5173`) lives in `appsettings.json`; nothing is hardcoded for production. Once the frontend's Render URL (and later its custom domain) is known, set:
```
Cors__AllowedOrigins=https://<app>.emirbulutt.com
```
(comma-separate multiple origins, e.g. during the transition period when both the `.onrender.com` URL and the custom domain need to work: `https://2gather-frontend.onrender.com,https://<app>.emirbulutt.com`). `AllowAnyOrigin` is never used.

**Cookies:** the app doesn't use any — access/refresh tokens travel via `Authorization: Bearer` header and JSON body respectively, stored client-side in `sessionStorage`; SignalR authenticates over its WebSocket handshake via an `access_token` query parameter. There is no `Secure`/`HttpOnly`/`SameSite` cookie configuration anywhere to adjust for the cross-subdomain production setup.

---

## Production Checklist

1. Push final production-ready code to GitHub.
2. Create Render PostgreSQL.
3. Create Render backend Web Service (Docker runtime, per the table above).
4. Configure backend environment variables (`ConnectionStrings__DefaultConnection` from the Postgres instance, `Jwt__Secret`, `Resend__*`, `Cors__AllowedOrigins` initially set to the frontend's `.onrender.com` URL).
5. Apply EF Core migrations using Option A (or B) above.
6. Verify the backend via its Render URL and `/health`.
7. Create Render frontend Static Site (per the table above), including the `/* → /index.html` Rewrite rule.
8. Configure `VITE_API_BASE_URL` initially using the Render backend's `.onrender.com` URL.
9. Verify frontend/backend operation using the Render URLs end-to-end.
10. Add the backend custom domain in Render.
11. Add the corresponding DNS record in Cloudflare.
12. Verify the backend custom domain in Render.
13. Add the frontend custom domain in Render.
14. Add the corresponding DNS record in Cloudflare.
15. Verify the frontend custom domain in Render.
16. Change frontend `VITE_API_BASE_URL` to the final API custom domain and redeploy the frontend (Vite bakes it in at build time — a redeploy is required, not just a runtime env change).
17. Update `Cors__AllowedOrigins` on the backend to the final frontend custom domain and redeploy the backend.
18. Redeploy as required.
19. Perform end-to-end production verification (register/login, create a list, real-time SignalR updates, image/email flows if configured).
20. Confirm HTTPS, authentication, database writes, migrations, and client-side routing (open a nested URL directly, e.g. `/lists/<id>/items`, and confirm it doesn't 404).
