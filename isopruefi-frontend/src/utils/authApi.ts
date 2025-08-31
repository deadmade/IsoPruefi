import {authClient} from "../api/clients";
import type {FileResponse} from "../api/api-client";

export type LoginResult = { token: string; refreshToken: string };

async function toJson<T>(res: unknown): Promise<T> {
    // Already a typed object (ideal case)
    if (res && typeof res === "object" && "token" in (res as any)) {
        return res as T;
    }

    // NSwag FileResponse (blob)
    const fr = res as FileResponse;
    if (fr?.data && typeof (fr.data as any).text === "function") {
        const txt = await fr.data.text();
        return txt ? (JSON.parse(txt) as T) : ({} as T);
    }

    // Raw Response fallback (rare)
    if (res instanceof Response) {
        const txt = await res.text();
        return txt ? (JSON.parse(txt) as T) : ({} as T);
    }

    return res as T;
}

/** Login and get tokens (normalized to { token, refreshToken }) */
export async function login(userName: string, password: string): Promise<LoginResult> {
    const res = await authClient.login({userName, password} as any);
    return toJson<LoginResult>(res);
}

/** Register a new user (server may return empty body; we swallow it) */
export async function register(userName: string, password: string): Promise<void> {
    const res = await authClient.register({userName, password} as any);
    try {
        await toJson<any>(res);
    } catch { /* ignore empty/204 */
    }
}

/** Refresh tokens (normalized to { token, refreshToken }) */
export async function refreshToken(token: string, refreshToken: string): Promise<LoginResult> {
    const res = await authClient.refresh({token, refreshToken} as any);
    return toJson<LoginResult>(res);
}
