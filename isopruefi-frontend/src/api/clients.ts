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
export const postClient = new TempClient(BASE, {fetch: authFetch});
export type PostalLocation = { postalCode: number; locationName: string };

// READ /Temp/GetAllPostalcodes
export async function fetchPostalLocations(): Promise<PostalLocation[]> {
    const resp = await postClient.getAllPostalcodes(); 
    const text = await resp.data.text();

    let json: unknown = [];
    try {
        json = text ? JSON.parse(text) : [];
    } catch {
        json = [];
    }

    const rows: PostalLocation[] = [];

    if (Array.isArray(json)) {
        for (const raw of json) {
            let code: number | undefined;
            let name = "";

            if (typeof raw === "object" && raw !== null) {
                const any = raw as any;
                const rawCode =
                    any.item1 ?? any.postalCode ?? any.postalcode ?? any.code ?? any.id;
                const rawName =
                    any.item2 ??
                    any.locationName ??
                    any.location ??
                    any.city ??
                    any.town ??
                    any.name ??
                    "";

                if (rawCode != null && !Number.isNaN(Number(rawCode))) {
                    code = Number(rawCode);
                }
                name = String(rawName ?? "").trim();
            } else if (typeof raw === "string" || typeof raw === "number") {
                const s = String(raw).trim();
                if (/^\d+$/.test(s)) code = Number(s);
                else name = s;
            }
            if (!name) continue;

            rows.push({ postalCode: code ?? 0, locationName: name });
        }
    }
    
    const seen = new Set<string>();
    const clean: PostalLocation[] = [];
    for (const r of rows) {
        const nm = r.locationName.trim();
        if (!nm) continue;
        
        const key = `${r.postalCode}-${nm.toLowerCase()}`;
        if (seen.has(key)) continue;
        seen.add(key);

        clean.push({ postalCode: r.postalCode, locationName: nm });
    }

    clean.sort((a, b) => a.locationName.localeCompare(b.locationName));
    return clean;
}
export async function addPostalLocation(postalCode: number | string): Promise<void> {
    const pcStr = String(postalCode).trim();
    if (!/^\d+$/.test(pcStr)) {
        throw new Error("Postal code must contain digits only.");
    }

    try {
        await postClient.insertLocation(Number(pcStr));
    } catch (err: any) {
        if (ApiException?.isApiException?.(err)) {
            throw new Error(`API Error (${err.status}): ${err.message}`);
        }
        throw err;
    }
}
export async function removePostalLocation(postalCode: number | string): Promise<void> {
    const pcStr = String(postalCode).trim();
    if (!/^\d+$/.test(pcStr)) {
        throw new Error("Postal code must contain digits only.");
    }
    try {
        await postClient.removePostalcode(Number(pcStr));
    } catch (err: any) {
        if (ApiException?.isApiException?.(err)) {
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
