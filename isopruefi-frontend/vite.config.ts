import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// const target = process.env.VITE_DEV_PROXY_TARGET || "http://localhost:5160";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/v1':  { target: process.env.VITE_DEV_PROXY_TARGET, changeOrigin: true, secure: false },
      '/api': { target: process.env.VITE_DEV_PROXY_TARGET, changeOrigin: true, secure: false },
    },
  }
});
