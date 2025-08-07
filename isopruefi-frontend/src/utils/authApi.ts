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

export async function register(username: string, password: string, token: string) {
    const response = await fetch(`/v1/Authentication/Register`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({ userName: username, password })
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Registration failed");
    }

    return response.json();
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