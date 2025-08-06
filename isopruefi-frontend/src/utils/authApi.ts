const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5160";

/*
    login - sends POST requests with { userName, password }
 */

export async function login(username: string, password: string) {

    const response = await fetch(`${BASE_URL}/Authentication/Login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({userName: username, password})
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Logic failed");
    }

    return response.json();
}

/*
    sends POST with { userName, password } to create a new account
 */

export async function register(username: string, password: string) {
    const response = await fetch(`${BASE_URL}/Authentication/Register`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({userName: username, password})
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Registration failed");
    }
}

/*
    refreshToken - keeps the session alive without forcing a new login
 */

export async function refreshToken(token: string, refreshToken: string) {
    const response = await fetch(`${BASE_URL}/Authentication/Refresh`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({token, refreshToken})
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Token refresh failed");
    }

    return response.json();
}