/**
 * @fileoverview JWT token management utilities for authentication.
 * Provides functions for storing, retrieving, and decoding JWT tokens.
 */

/**
 * Standard JWT payload structure with common claims.
 * Extends to allow additional custom claims from the authentication system.
 */
export interface JwtPayload {
    /** Subject (usually user ID or username) */
    sub?: string;
    /** Expiration time (Unix timestamp) */
    exp?: number;
    /** Issued at time (Unix timestamp) */
    iat?: number;

    /** Allow additional custom claims */
    [key: string]: unknown;
}

/**
 * Saves JWT tokens to browser localStorage for persistent authentication.
 *
 * @param token - The JWT access token
 * @param refreshToken - The refresh token for obtaining new access tokens
 *
 * @example
 * ```typescript
 * // After successful login
 * saveToken(response.accessToken, response.refreshToken);
 * ```
 */
export function saveToken(token: string, refreshToken: string): void {
    localStorage.setItem("token", token);
    localStorage.setItem("refreshToken", refreshToken);
}

/**
 * Retrieves the stored JWT access token from localStorage.
 *
 * @returns The access token string, or null if not found
 *
 * @example
 * ```typescript
 * const token = getToken();
 * if (token) {
 *   // Use token for authenticated requests
 * }
 * ```
 */
export function getToken(): string | null {
    return localStorage.getItem("token");
}

/**
 * Retrieves the stored refresh token from localStorage.
 *
 * @returns The refresh token string, or null if not found
 *
 * @example
 * ```typescript
 * const refreshToken = getRefreshToken();
 * if (refreshToken) {
 *   // Use to obtain new access token
 * }
 * ```
 */
export function getRefreshToken(): string | null {
    return localStorage.getItem("refreshToken");
}

/**
 * Removes both access and refresh tokens from localStorage.
 * Call this function when logging out or when tokens become invalid.
 *
 * @example
 * ```typescript
 * // On logout
 * clearToken();
 * // Redirect to login page
 * ```
 */
export function clearToken(): void {
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
}

/**
 * Decodes a JWT token and extracts the payload containing user information.
 * Does not verify the token signature - use only for reading claims.
 *
 * @param token - The JWT token to decode
 * @returns The decoded payload object, or null if decoding fails
 *
 * @example
 * ```typescript
 * const payload = decodeToken(accessToken);
 * if (payload) {
 *   console.log('Token expires at:', new Date(payload.exp * 1000));
 * }
 * ```
 */
export function decodeToken(token: string): JwtPayload | null {
    try {
        const payload = token.split('.')[1];
        return JSON.parse(atob(payload));
    } catch (e) {
        return null;
    }
}

/**
 * Extracts the user identifier from a JWT token.
 * The subject field typically contains the username or user ID.
 *
 * @param token - The JWT token to extract user information from
 * @returns The user identifier string, or null if extraction fails
 *
 * @example
 * ```typescript
 * const currentUser = getUserFromToken(getToken());
 * if (currentUser) {
 *   console.log('Current user:', currentUser);
 * }
 * ```
 */
export function getUserFromToken(token: string): string | null {
    const decoded = decodeToken(token);
    return decoded?.sub || null;
}