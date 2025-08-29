export function apiBase(): string {
    const runtime = (window as any).__APP_CONFIG__?.API_BASE_URL || "";
    const env     = (import.meta as any).env?.VITE_API_BASE_URL || "";
    return (runtime || env || "").replace(/\/+$/, ""); // trim trailing slash
}