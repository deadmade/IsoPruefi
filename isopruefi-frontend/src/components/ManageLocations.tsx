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
        <div style={{ marginTop: 16, padding: 12, border: "1px solid #ddd", borderRadius: 8 }}>
            <h3 style={{ marginTop: 0 }}>Manage locations</h3>
            <label>
                Postal code:&nbsp;
                <input
                    type="text"
                    inputMode="numeric"
                    value={postal}
                    onChange={(e) => setPostal(e.target.value)}
                    placeholder="e.g. 89522"
                    style={{ width: 120 }}
                />
            </label>
            &nbsp;&nbsp;
            <button disabled={busy} onClick={() => handle("add")}>Add</button>
            &nbsp;
            <button disabled={busy} onClick={() => handle("remove")}>Remove</button>
            {msg && (
                <div style={{ marginTop: 8, color: /error|500|invalid/i.test(msg) ? "crimson" : "green" }}>
                    {msg}
                </div>
            )}
            <div style={{ marginTop: 6, fontSize: 12, opacity: 0.7 }}>
            </div>
        </div>
    );
}
