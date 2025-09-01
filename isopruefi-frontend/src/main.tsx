import React from 'react';
import App from './App.tsx'
import ReactDOM from 'react-dom/client';
import './index.css';
import { AuthProvider } from './auth/AuthContext.tsx';
import { BrowserRouter } from "react-router-dom";

/**
 * Loads application configuration from /config.json and sets it on the global window object.
 * Then renders the React application inside the root element.
 * The app is wrapped with React.StrictMode, BrowserRouter for routing, and AuthProvider for authentication context.
 */
async function start() {
    try {
        const res = await fetch("/config.json", {cache: "no-store"});
        (window as any).__APP_CONFIG__ = res.ok ? await res.json() : {};
    } catch {
        (window as any).__APP_CONFIG__ = {};
    }

    ReactDOM.createRoot(document.getElementById("root")!).render(
        <React.StrictMode>
            <BrowserRouter>
                <AuthProvider>
                    <App />
                </AuthProvider>
            </BrowserRouter>
        </React.StrictMode>
    );
}

// Start the application.
start();