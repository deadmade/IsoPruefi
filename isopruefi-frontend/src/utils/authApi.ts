const API_VERSION = "v1";
const BASE_URL = "http://localhost:5160";

export async function login(username: string, password: string) {
    const response = await fetch(`${BASE_URL}/${API_VERSION}/Authentication/Login`, { 
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userName: username, password })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Login failed");
    }

    return response.json();
}

export async function register(userName: string, password: string) {
    const res = await fetch(
        `${BASE_URL}/${API_VERSION}/Authentication/Register`,
        {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userName, password }),
        }
    );
    
    if (!res.ok) {
        const text = await res.text();
        try {
            const problem = text ? JSON.parse(text) : null;
            const msg = problem?.detail || problem?.title || "Registration failed";
            throw new Error(msg);
        } catch {
            throw new Error(text || "Registration failed");
        }
    }

    const text = await res.text();
    if (!text) return;

    try {
        return JSON.parse(text);
    } catch {
        return;
    }
}

export async function refreshToken(token: string, refreshToken: string) {
    const response = await fetch(`${BASE_URL}/${API_VERSION}/Authentication/Refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, refreshToken })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Token refresh failed");
    }

    return response.json();
}