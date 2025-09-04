import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    base: process.env.NODE_ENV === "production" ? "/frontend/" : "/",
    server: {
        port: 5173,
        proxy: {
            "/backend": {
                target:
                    process.env.VITE_PROXY_TARGET ||
                    "https://aicon.dhbw-heidenheim.de:5001",
                changeOrigin: true,
                secure: false,
            },
        },
    },
});
