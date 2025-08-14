import { API_BASE } from "./config";

const API_VERSION  = (p: string) => `${API_BASE}/v1${p}`;
// const BASE_URL = (p: string) => `${API_BASE}/api/v1${p}`;

export async function login(userName: string, password: string) {
    const r = await fetch(API_VERSION("/Authentication/Login"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName, password }),
    });
    if (!r.ok) throw new Error((await r.text()) || "Login failed");
    return r.json();
}

export async function register(userName: string, password: string) {
    const r = await fetch(API_VERSION("/Authentication/Register"), {
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
    const r = await fetch(API_VERSION("/Authentication/Refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken }),
    });
    if (!r.ok) throw new Error((await r.text()) || "Token refresh failed");
    return r.json();
}
