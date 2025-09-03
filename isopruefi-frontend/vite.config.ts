import {defineConfig} from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    base: process.env.NODE_ENV === 'production' ? '/frontend/' : '/',
    server: {
        port: 5173,
        proxy: {
            "/v1": {
                target: `${process.env.VITE_API_BASE_URL || "https://aicon.dhbw-heidenheim.de:5001/backend"}`,
                changeOrigin: true,
                secure: false
            },
            "/api": {
                target: `${process.env.VITE_API_BASE_URL || "https://aicon.dhbw-heidenheim.de:5001/backend"}`,
                changeOrigin: true,
                secure: false
            },
        },
    },
});
