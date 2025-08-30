import React from 'react';
import App from './App.tsx'
import ReactDOM from 'react-dom/client';
import './index.css';

async function start() {
    try {
        const res = await fetch("/config.json", {cache: "no-store"});
        (window as any).__APP_CONFIG__ = res.ok ? await res.json() : {};
    } catch {
        (window as any).__APP_CONFIG__ = {};
    }


    ReactDOM.createRoot(document.getElementById("root")!).render(
        <React.StrictMode>
            <title>IsoPruefi</title>
            <App/>
        </React.StrictMode>
    );
}

start();