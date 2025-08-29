import { apiBase } from "../utils/config";
import { getToken } from "../utils/tokenHelpers";
import {
    AuthenticationClient,
    TemperatureDataClient,
    ApiException,
    TempClient
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
export const postClient = new TempClient(BASE, {fetch: authFetch})

export type PostalLocation = { postalCode: string; locationName: string };

export async function fetchPostalLocations(): Promise<PostalLocation[]> {
    // The generated client returns a Blob wrapped in FileResponse
    const resp = await postClient.getAllPostalcodes(); // FileResponse
    const text = await resp.data.text();               // Blob -> string
    const json = JSON.parse(text);

    // Accept both shapes just in case: string[] or array of objects
    if (Array.isArray(json)) {
        if (json.length === 0) return [];

        // 1) string[]: ["Heidenheim", "Ulm", ...]
        if (typeof json[0] === "string") {
            return (json as string[]).map(p => ({ postalCode: p, locationName: p }));
        }

        // 2) objects: { postalCode, locationName } (or similar casing)
        return (json as any[]).map(o => ({
            postalCode: o.postalCode ?? o.postalcode ?? o.code ?? o.name ?? "",
            locationName:
                o.locationName ?? o.location ?? o.city ?? o.town ?? o.name ?? o.postalCode ?? "",
        }));
    }

    // Fallback: unexpected shape
    return [];
}

export async function addPostalLocation(postalCode: number): Promise<void> {
    const resp = await postClient.insertLocation(postalCode); // name from NSwag
    const body = await resp.data.text();                      // may be empty
    if (resp.status >= 400) throw new Error(body || `Server error (${resp.status})`);
}

export async function removePostalLocation(postalCode: number): Promise<void> {
    const resp = await postClient.removePostalcode(postalCode); // name from NSwag
    const body = await resp.data.text();
    if (resp.status >= 400) throw new Error(body || `Server error (${resp.status})`);
}

// Re-export ApiException so callers can do instanceof checks if needed
export { ApiException };
