import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/v1":  { target: "https://aicon.dhbw-heidenheim.de:5001/backend", changeOrigin: true, secure: false },
      "/api": { target: "https://aicon.dhbw-heidenheim.de:5001/backend", changeOrigin: true, secure: false },
    },
  },
});
