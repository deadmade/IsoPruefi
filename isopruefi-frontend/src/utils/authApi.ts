/**
 * @fileoverview Authentication API utilities for user login, registration, and token management.
 * Provides a simplified interface for authentication operations with response normalization.
 */

import {authClient} from "../api/clients";
import type {FileResponse} from "../api/api-client";

/**
 * Represents the result of a successful login operation.
 */
export type LoginResult = { 
    /** JWT access token for authenticated requests */
    token: string; 
    /** Refresh token for obtaining new access tokens */
    refreshToken: string 
};

/**
 * Normalizes various response formats from the authentication API into typed objects.
 * Handles FileResponse (blob), direct Response objects, and already-parsed objects.
 * 
 * @template T - The expected return type
 * @param res - The response from the authentication API
 * @returns Promise resolving to the parsed object of type T
 * 
 * @internal
 */
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

/**
 * Authenticates a user with username and password credentials.
 * 
 * @param userName - The user's login username
 * @param password - The user's password
 * @returns Promise resolving to login tokens
 * @throws {ApiException} When credentials are invalid or server error occurs
 * 
 * @example
 * ```typescript
 * try {
 *   const result = await login('user@example.com', 'password123');
 *   saveToken(result.token, result.refreshToken);
 * } catch (error) {
 *   console.error('Login failed:', error);
 * }
 * ```
 */
export async function login(userName: string, password: string): Promise<LoginResult> {
    const res = await authClient.login({userName, password} as any);
    return toJson<LoginResult>(res);
}

/**
 * Registers a new user in the system. Requires admin privileges.
 * 
 * @param userName - The desired username for the new user
 * @param password - The password for the new user
 * @returns Promise that resolves on successful registration
 * @throws {ApiException} When registration fails, username exists, or insufficient permissions
 * 
 * @example
 * ```typescript
 * try {
 *   await register('newuser@example.com', 'securePassword123');
 *   console.log('User registered successfully');
 * } catch (error) {
 *   console.error('Registration failed:', error);
 * }
 * ```
 */
export async function register(userName: string, password: string): Promise<void> {
    const res = await authClient.register({userName, password} as any);
    try {
        await toJson<any>(res);
    } catch { /* ignore empty/204 responses */ }
}

/**
 * Refreshes an expired access token using a valid refresh token.
 * 
 * @param token - The expired JWT access token
 * @param refreshToken - The valid refresh token
 * @returns Promise resolving to new authentication tokens
 * @throws {ApiException} When refresh token is invalid, expired, or revoked
 * 
 * @example
 * ```typescript
 * try {
 *   const tokens = await refreshToken(oldToken, refreshToken);
 *   saveToken(tokens.token, tokens.refreshToken);
 * } catch (error) {
 *   // Refresh failed, redirect to login
 *   clearToken();
 *   window.location.href = '/login';
 * }
 * ```
 */
export async function refreshToken(token: string, refreshToken: string): Promise<LoginResult> {
    const res = await authClient.refresh({token, refreshToken} as any);
    return toJson<LoginResult>(res);
}
