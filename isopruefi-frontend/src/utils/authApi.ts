import { apiBase } from "./config";

const v1 = (p: string) => `${apiBase()}/v1${p}`;       // e.g. https://aicon.../backend/v1/Authentication/...
// const api = (p: string) => `${apiBase()}/api/v1${p}`;  // e.g. https://aicon.../backend/api/v1/...

export async function login(userName: string, password: string) {
    const r = await fetch(v1("/Authentication/Login"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName, password }),
    });
    if (!r.ok) throw new Error((await r.text()) || "Login failed");
    return r.json();
}

export async function register(userName: string, password: string) {
    const r = await fetch(v1("/Authentication/Register"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName, password }),
    });
    if (!r.ok) {
        const t = await r.text();
        try { const p = t ? JSON.parse(t) : null; throw new Error(p?.detail || p?.title || "Registration failed"); }
        catch { throw new Error(t || "Registration failed"); }
    }
    const t = await r.text();
    return t ? JSON.parse(t) : undefined; // empty 200/204 is OK
}

export async function refreshToken(token: string, refreshToken: string) {
    const r = await fetch(v1("/Authentication/Refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken }),
    });
    if (!r.ok) throw new Error((await r.text()) || "Token refresh failed");
    return r.json();
}
