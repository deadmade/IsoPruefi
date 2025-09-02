/**
 * @fileoverview Main application component with routing configuration.
 * Defines the primary routing structure and navigation for the IsoPrüfi frontend application.
 */

import {BrowserRouter as Router, Route, Routes} from "react-router-dom";
import Welcome from "./pages/Welcome.tsx";
import SignIn from "./auth/SignIn.tsx";
import SignUp from "./auth/SignUp.tsx";
import UserPage from "./pages/UserPage.tsx";
import AdminPage from "./pages/AdminPage.tsx";

/**
 * Main application component that sets up routing and navigation structure.
 *
 * Provides routing for:
 * - Welcome/landing page (/)
 * - User authentication (sign-in/sign-up)
 * - Role-based user and admin dashboards
 *
 * Uses React Router for client-side routing with browser history.
 *
 * @returns JSX element containing the entire application with routing
 *
 * @example
 * ```tsx
 * // Used as root component in main.tsx
 * <App />
 * ```
 */
export default function App() {

    return (
        <Router>
            <Routes>
                <Route path="/" element={<Welcome/>}/>
                <Route path="/signin" element={<SignIn/>}/>
                <Route path="/signup" element={<SignUp/>}/>
                <Route path="/user" element={<UserPage/>}/>
                <Route path="/admin" element={<AdminPage/>}/>
            </Routes>
        </Router>
    );
}
