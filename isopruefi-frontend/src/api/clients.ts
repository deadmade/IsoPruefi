import { apiBase } from "../utils/config";
import { getToken } from "../utils/tokenHelpers";
import {
    AuthenticationClient,
    TemperatureDataClient,
    ApiException,
    TempClient,
    TopicClient,
    TopicSetting
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

// types the picker will use
export type PostalLocation = { postalCode: string; locationName: string };

export async function fetchPostalLocations(): Promise<PostalLocation[]> {
    const resp = await postClient.getAllPostalcodes();   // FileResponse
    const text = await resp.data.text();                 // Blob -> string
    const json = text ? JSON.parse(text) : [];

    // Accept both shapes just in case: ["Ulm","Heidenheim"] OR [{ postalCode, locationName }]
    if (Array.isArray(json)) {
        if (json.length === 0) return [];
        if (typeof json[0] === "string") {
            return (json as string[]).map(p => ({ postalCode: p, locationName: p }));
        }
        return (json as any[]).map(o => ({
            postalCode: String(o.postalCode ?? o.postalcode ?? o.code ?? o.name ?? ""),
            locationName:
                String(
                    o.locationName ?? o.location ?? o.city ?? o.town ?? o.name ?? o.postalCode ?? ""
                ),
        }));
    }
    return [];
}

export async function addPostalLocation(postalCode: number): Promise<void> {
    const resp = await postClient.insertLocation(postalCode);
    // Some endpoints return empty body; surface server errors if any
    if (resp.status >= 400) {
        const body = await resp.data.text();
        throw new Error(body || `Server error (${resp.status})`);
    }
}

// DELETE /Temp/RemovePostalcode?postalCode={int}
export async function removePostalLocation(postalCode: number): Promise<void> {
    try {
        await postClient.removePostalcode(postalCode);
        // success => nothing to return
    } catch (err: any) {
        // surface a nice message
        if (ApiException.isApiException?.(err)) {
            throw new Error(`API Error (${err.status}): ${err.message}`);
        }
        throw err;
    }
}
export const topicClient = new TopicClient(BASE, { fetch: authFetch });
export async function getAllTopics(): Promise<TopicSetting[]> {
    return topicClient.getAllTopics();
}

export async function createTopic(setting: TopicSetting): Promise<void> {
    await topicClient.createTopic(setting);
}

export async function updateTopic(setting: TopicSetting): Promise<void> {
    await topicClient.updateTopic(setting);
}

export async function deleteTopic(setting: TopicSetting): Promise<void> {
    await topicClient.deleteTopic(setting);
}

// Re-export ApiException so callers can do instanceof checks if needed
export { ApiException };
