/**
 * @fileoverview Configuration utilities for API base URL resolution.
 * Handles both runtime configuration and build-time environment variables.
 */

/**
 * Resolves the API base URL from runtime configuration or environment variables.
 *
 * Priority order:
 * 1. Runtime configuration from `window.__APP_CONFIG__.API_BASE_URL`
 * 2. Build-time environment variable `VITE_API_BASE_URL`
 * 3. Empty string as fallback
 *
 * @returns The API base URL with trailing slashes removed
 *
 * @example
 * ```typescript
 * // Returns "https://api.example.com" (trailing slash removed)
 * const baseUrl = apiBase();
 * ```
 */
export function apiBase(): string {
    const runtime = (window as any).__APP_CONFIG__?.API_BASE_URL || "";
    const env = (import.meta as any).env?.VITE_API_BASE_URL || "";
    return (runtime || env || "").replace(/\/+$/, ""); // trim trailing slash
}