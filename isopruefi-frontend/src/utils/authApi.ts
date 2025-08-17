import { authClient } from "../api/clients";

type LoginArg    = Parameters<typeof authClient.login>[0];
type RegisterArg = Parameters<typeof authClient.register>[0];
type RefreshArg  = Parameters<typeof authClient.refresh>[0];

/** Login and get tokens */
export async function login(userName: string, password: string) {
    const payload: LoginArg = { userName, password } as LoginArg;
    return authClient.login(payload);
}

/** Register a new user */
export async function register(userName: string, password: string) {
    const payload: RegisterArg = { userName, password } as RegisterArg;
    return authClient.register(payload);
}

/** Refresh tokens */
export async function refreshToken(token: string, refreshToken: string) {
    const payload: RefreshArg = { token, refreshToken } as RefreshArg;
    return authClient.refresh(payload);
}
