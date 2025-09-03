import { createContext, useContext, useEffect, useState } from "react";
import { decodeToken, type JwtPayload } from "../utils/tokenHelpers";

/**
 * Represents a user in the authentication context.
 * @property {string} username - The user's username.
 * @property {"admin" | "user"} role - The user's role.
 */
type User = {
  username: string;
  role: "admin" | "user";
};

/**
 * The shape of the authentication context.
 * @property {User | null} user - The current authenticated user, or null if not logged in.
 * @property {(user: User | null) => void} setUser - Function to update the user.
 * @property {boolean} ready - Indicates if the authentication state is initialized.
 */
type AuthContextType = {
  user: User | null;
  setUser: (user: User | null) => void;
  ready: boolean;
};

/**
 * React context for authentication state.
 */
const AuthContext = createContext<AuthContextType>({
  user: null,
  setUser: () => {},
  ready: false,
});

/**
 * Provides authentication context to child components.
 * Decodes JWT token from localStorage and sets user state.
 * @param {object} props - Component props.
 * @param {React.ReactNode} props.children - Child components.
 * @returns {JSX.Element} The context provider.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      const decoded: JwtPayload = decodeToken(token) ?? {};
      const claim =
        decoded.role ??
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

      const roles: string[] =
        Array.isArray(claim) ? claim : typeof claim === "string" && claim ? [claim] : [];

      if (roles.some((r) => /admin/i.test(r))) {
        setUser({ username: (decoded.sub as string) ?? "unknown", role: "admin" });
      } else if (roles.some((r) => /user/i.test(r))) {
        setUser({ username: (decoded.sub as string) ?? "unknown", role: "user" });
      }
    }
    setReady(true);
  }, []);

  return (
    <AuthContext.Provider value={{ user, setUser, ready }}>
      {children}
    </AuthContext.Provider>
  );
}

/**
 * Custom hook to access the authentication context.
 * @returns {AuthContextType} The authentication context value.
 */
export function useAuth() {
  return useContext(AuthContext);
}
