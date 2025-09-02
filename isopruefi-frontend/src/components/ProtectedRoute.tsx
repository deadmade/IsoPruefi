import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

/**
 * ProtectedRoute component for role-based route protection.
 * Checks authentication and user role before rendering child routes.
 * - If authentication is not ready, shows a loading indicator.
 * - If user is not authenticated, redirects to the public welcome page.
 * - If user role is not allowed, redirects to their default page.
 *
 * @param {object} props - Component props.
 * @param {Array<"admin"|"user">} props.allowed - Array of allowed roles for the route.
 * @returns {JSX.Element} The rendered protected route or a redirect.
 */
export default function ProtectedRoute({ allowed }: { allowed: Array<"admin"|"user"> }) {
  const { user, ready } = useAuth();

  if (!ready) return <div style={{padding:16}}>Lade…</div>; // short loader
  if (!user) return <Navigate to="/" replace />;
  if (!allowed.includes(user.role)) return <Navigate to={user.role === "admin" ? "/admin" : "/user"} replace />;

  return <Outlet />;
}
