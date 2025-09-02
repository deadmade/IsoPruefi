import { Routes, Route } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute";
import Welcome from "./pages/Welcome";
import SignIn from "./auth/SignIn";
import SignUp from "./auth/SignUp";
import UserPage from "./pages/UserPage";
import AdminPage from "./pages/AdminPage";

/**
 * Main application routing component.
 * Defines all routes for the app, including public, protected (user/admin), and admin-only routes.
 * Uses ProtectedRoute to restrict access based on user roles.
 *
 * - "/"           : Public welcome page.
 * - "/signin"     : Public sign-in page.
 * - "/signup"     : Public sign-up page.
 * - "/user"       : Protected route for users and admins.
 * - "/admin"      : Protected route for admins only.
 * - "*"           : Fallback route (renders Welcome).
 *
 * @returns {JSX.Element} The rendered application routes.
 */
export default function App() {
  return (
    <Routes>
      {/* Public routes */}
      <Route path="/" element={<Welcome />} />
      <Route path="/signin" element={<SignIn />} />
      <Route path="/signup" element={<SignUp />} />

      {/* Protected routes for user & admin */}
      <Route element={<ProtectedRoute allowed={["user","admin"]} />}>
        <Route path="/user" element={<UserPage />} />
      </Route>

      {/* Admin-only routes */}
      <Route element={<ProtectedRoute allowed={["admin"]} />}>
        <Route path="/admin" element={<AdminPage />} />
      </Route>

      {/* Fallback public route */}
      <Route path="*" element={<Welcome />} />
    </Routes>
  );
}
