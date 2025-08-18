/*
    Saves the tokens
 */

export interface JwtPayload {
    sub?: string;
    exp?: number;
    iat?: number;
    [key: string]: unknown; // Allow additional claims
}

export function saveToken(token: string, refreshToken: string) {
    localStorage.setItem("token", token);
    localStorage.setItem("refreshToken", refreshToken);
}

/*
    Retrieves the access token
 */

export function getToken(): string | null {
    return localStorage.getItem("token");
}

/*
    Retrieves the refresh token
 */

export function getRefreshToken(): string | null {
    return localStorage.getItem("refreshToken");
}

/*
    Removes both tokens 
 */

export function clearToken() {
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
}

/*
    Decodes the JWT and returns the payload — the part of the token 
    that contains the user’s role, ID, and expiry.
 */

export function decodeToken(token: string): JwtPayload | null {
    try {
        const payload = token.split('.')[1];
        return JSON.parse(atob(payload));
    } catch (e) {
        return null;
    }
}

/*
    Extracts the subject field from the token, 
    which usually contains the username or user ID.
 */

export function getUserFromToken(token: string): string | null {
    const decoded = decodeToken(token);
    return decoded?.sub || null;
}
