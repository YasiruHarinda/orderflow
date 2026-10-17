# 0013. A React web store, served by the YARP gateway

- **Status:** Accepted
- **Date:** 2026-10-17

## Context
The system could only be used with `curl`, which makes the saga hard to see and the project hard to demo. A small web front end should let a person place orders, watch them reach *Confirmed* or *Rejected*, and change stock levels.

Two questions: which technology, and where the built site runs.

## Decision
- **React + TypeScript + Vite** in `src/Web`, with **TanStack Query** for data fetching and polling, and **React Router** for pages. It is the most widely used front-end stack and pairs well with a .NET API. Blazor WebAssembly was considered: it would keep everything in C#, but it shows less front-end range and ships a much larger download.
- **The YARP gateway serves the built site** from `wwwroot`. The gateway's Dockerfile builds `src/Web` in a Node stage and copies the output in; Node is not in the runtime image.
  - The site and the API share **one origin**, so the browser never needs CORS and no extra public endpoint exists.
  - No new container, Deployment, Service or NetworkPolicy: the gateway's existing hardening, probes and scaling apply.
  - Hashed assets are cached for a year; `index.html` is `no-cache`, so a release is picked up immediately. A Content-Security-Policy allows only same-origin scripts and styles.
  - Unknown paths fall back to `index.html` (client-side routing), except `/api/*`, which stays a real 404.
- **Local development:** Aspire runs the Vite dev server (`AddViteApp`) with hot reload and passes it the gateway's address; Vite forwards `/api` there.
- **The Envoy HTTPRoute** now sends `/` (not only `/api`) to the gateway.
- **Quality gates:** a `web` CI job runs `npm ci`, `npm audit`, the type check, Vitest unit and component tests, and the production build. The kind smoke test checks that the store and its deep links are served. Dependabot updates npm packages weekly.
- **No login yet** (see 0012): the browser plays one customer with a random id kept in `localStorage`, and the stock page is open.

## Consequences
- ✅ One command (`docker compose up`, `k3d-up.ps1`, or Aspire) gives a usable store; the saga is visible to anyone.
- ✅ No CORS, no new attack surface, no new deployment unit.
- ❌ The store and the gateway are released together: a store-only change rebuilds and redeploys the gateway. If the front end grows or gets its own team, move it to its own container (e.g. an unprivileged nginx image) behind the same HTTPRoute.
- ❌ Order status uses polling (once a second until final). Server-sent events from Orders would be more efficient and are the next step for a live "saga view".
