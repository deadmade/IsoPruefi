import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/v1":  { target: "http://localhost:5160", changeOrigin: true },
      "/api": { target: "http://localhost:5160", changeOrigin: true },
    },
  },
});
