import { apiBase } from "../utils/config";
import { getToken } from "../utils/tokenHelpers";
import {
    AuthenticationClient,
    TemperatureDataClient,
    ApiException,
} from "./api-client.ts";

// Read the base URL from config
const BASE = apiBase();

// fetch wrapper that injects the bearer token at CALL time.
function authFetch(input: RequestInfo, init: RequestInit = {}) {
    const token = getToken();
    init.headers = {
        ...(init.headers || {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        Accept: "application/json",
    };
    return window.fetch(input as any, init);
}

// Export ready-to-use, typed clients
export const authClient = new AuthenticationClient(BASE, { fetch: authFetch });
export const tempClient = new TemperatureDataClient(BASE, { fetch: authFetch });

// Re-export ApiException so callers can do instanceof checks if needed
export { ApiException };
