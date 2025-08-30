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

export type SensorMeta = {
    id: number;
    name: string;
    location: string;
    type: "north" | "south" | "other";
};

export type LocationStatus = { online: boolean; lastSeenMs?: number };

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
    notifyTopicsChanged();
}

export async function updateTopic(setting: TopicSetting): Promise<void> {
    await topicClient.updateTopic(setting);
    notifyTopicsChanged();
}

export async function deleteTopic(setting: TopicSetting): Promise<void> {
    await topicClient.deleteTopic(setting);
    notifyTopicsChanged();
}

export async function fetchSensorsNormalized(): Promise<SensorMeta[]> {
    const list = await topicClient.getAllTopics(); // TopicSetting[]
    const rows: SensorMeta[] = [];

    for (const t of list ?? []) {
        const any = t as any; // tolerate generator field name changes
        const id = Number(
            any.topicSettingId ?? any.id ?? any.topicId ?? Date.now() + Math.random()
        );

        const rawType = String(any.sensorType ?? any.type ?? "").toLowerCase();
        let type: "north" | "south" | "other" = "other";
        if (rawType.includes("north") || rawType.includes("nord")) type = "north";
        else if (rawType.includes("south") || rawType.includes("sued") || rawType.includes("süd")) type = "south";

        const name =
            (String(any.sensorName ?? any.name ?? "").trim() ||
                (type === "north" ? "North" : type === "south" ? "South" : "Unnamed"));

        const location =
            (String(any.sensorLocation ?? any.location ?? "").trim() ||
                (type === "north" ? "north" : type === "south" ? "south" : "Unspecified"));

        rows.push({ id, name, location, type });
    }
    rows.sort((a, b) => {
        const rank = (x: SensorMeta) => (x.type === "north" ? 0 : x.type === "south" ? 1 : 2);
        const ra = rank(a), rb = rank(b);
        return ra !== rb ? ra - rb : a.name.localeCompare(b.name);
    });

    return rows;
}

export async function fetchRecentStatus(
    place: string,
    isFahrenheit: boolean,
    windowMinutes = 15
): Promise<{ north: LocationStatus; south: LocationStatus }> {
    const end = new Date();
    const start = new Date(end.getTime() - windowMinutes * 60 * 1000);

    const res = await tempClient.getTemperature(
        start,
        end,
        place || "Heidenheim an der Brenz",
        isFahrenheit
    );

    const toLastSeen = (arr?: Array<{ timestamp?: any }>): number | undefined => {
        let max = -1;
        for (const it of arr ?? []) {
            const ms = new Date((it as any).timestamp).getTime();
            if (!Number.isNaN(ms) && ms > max) max = ms;
        }
        return max >= 0 ? max : undefined;
    };

    // note: API field is "temperatureNord" (per your code/screens)
    const northSeen = toLastSeen((res as any)?.temperatureNord);
    const southSeen = toLastSeen((res as any)?.temperatureSouth);

    return {
        north: { online: northSeen !== undefined, lastSeenMs: northSeen },
        south: { online: southSeen !== undefined, lastSeenMs: southSeen },
    };
}

function notifyTopicsChanged() {
    try {
        // storage event (cross-tab)
        localStorage.setItem("topicsVersion", String(Date.now()));
        // BroadcastChannel if available
        if ("BroadcastChannel" in window) {
            const bc = new BroadcastChannel("topics");
            bc.postMessage({ type: "topics-changed", ts: Date.now() });
            bc.close();
        }
    } catch { /* ignore */ }
}

// Re-export ApiException so callers can do instanceof checks if needed
export { ApiException };
