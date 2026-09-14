import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true
  },
  test: {
    environment: "node",
    include: ["src/**/*.test.ts"],
    env: {
      VITE_API_BASE_URL: "http://localhost:5037",
      VITE_USE_DEV_AUTH: "true",
      VITE_ENTRA_CLIENT_ID: "",
      VITE_ENTRA_TENANT_ID: "",
      VITE_API_SCOPE: ""
    }
  }
});
