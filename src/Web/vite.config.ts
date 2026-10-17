/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development the site runs on the Vite dev server and forwards /api to the YARP gateway.
// Aspire passes the gateway's address in services__gateway__http__0; without Aspire
// (e.g. next to "docker compose up") the gateway is on localhost:8080.
// In production there is no proxy: the gateway serves the built site itself (same origin).
const gateway =
  process.env.services__gateway__https__0 ??
  process.env.services__gateway__http__0 ??
  "http://localhost:8080";

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: {
      "/api": { target: gateway, changeOrigin: true, secure: false },
    },
  },
  build: {
    outDir: "dist",
    sourcemap: true,
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test-setup.ts"],
  },
});
