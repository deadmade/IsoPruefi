import {defineConfig} from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    server: {
        port: 5173,
        proxy: {
            "/v1": {target: "https://localhost:7240", changeOrigin: true, secure: false},
            "/api": {target: "https://localhost:7240", changeOrigin: true, secure: false},
        },
    },
});