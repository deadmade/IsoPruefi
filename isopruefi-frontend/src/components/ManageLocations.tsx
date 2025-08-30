import { useState } from "react";
import { addPostalLocation, removePostalLocation } from "../api/clients";

type Props = { onChanged?: () => void };

export default function ManageLocations({ onChanged }: Props) {
    const [postal, setPostal] = useState<string>("");
    const [busy, setBusy] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);

    const parseCode = () => {
        const n = Number(postal.trim());
        if (!Number.isInteger(n) || n <= 0) throw new Error("Enter a valid postal code (integer).");
        return n;
    };

    const handle = async (kind: "add" | "remove") => {
        try {
            setBusy(true);
            setMsg(null);
            const code = parseCode();
            if (kind === "add") {
                await addPostalLocation(code);
                setMsg(`Location ${code} added (or already exists).`);
            } else {
                await removePostalLocation(code);
                setMsg(`Location ${code} removed (if it existed).`);
            }
            onChanged?.(); // ask parent to refresh the drop-down
        } catch (e) {
            setMsg(e instanceof Error ? e.message : "Unexpected error");
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="mt-4 p-3 border border-gray-300 rounded-lg bg-white shadow-sm">
            <h3 className="mt-0 mb-3 text-lg font-semibold text-gray-800">Manage locations</h3>
            <div className="flex items-center gap-3">
                <label className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-700">Postal code:</span>
                    <input
                        type="text"
                        inputMode="numeric"
                        value={postal}
                        onChange={(e) => setPostal(e.target.value)}
                        placeholder="e.g. 89522"
                        className="w-32 px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300"
                    />
                </label>
                <button 
                    disabled={busy} 
                    onClick={() => handle("add")}
                    className="px-4 py-2 bg-pink-600 text-white text-sm font-medium rounded-md hover:bg-pink-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                    Add
                </button>
                <button 
                    disabled={busy} 
                    onClick={() => handle("remove")}
                    className="px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                    Remove
                </button>
            </div>
            {msg && (
                <div className={`mt-2 text-sm ${/error|500|invalid/i.test(msg) ? "text-red-600" : "text-green-600"}`}>
                    {msg}
                </div>
            )}
        </div>
    );
}
