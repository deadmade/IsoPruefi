// Build-time fallback (optional)
const ENV_BASE = (import.meta as any).env?.VITE_API_BASE_URL ?? "";

// Runtime (wins if present)
const RUNTIME_BASE = (window as any).__APP_CONFIG__?.API_BASE_URL ?? "";

// Export one value for the whole app to use
export const API_BASE: string = (RUNTIME_BASE || ENV_BASE || "").replace(/\/+$/, "");
