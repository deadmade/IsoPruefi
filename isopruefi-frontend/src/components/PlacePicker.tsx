import { useEffect, useState } from "react";
import { fetchPostalLocations, type PostalLocation } from "../api/clients";

export default function PlacePicker({
                                        value,
                                        onChange,
                                    }: {
    value: string;
    onChange: (next: string) => void;
}) {
    const [opts, setOpts] = useState<PostalLocation[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let alive = true;
        (async () => {
            try {
                const rows = await fetchPostalLocations();
                if (!alive) return;

                // Defensive: ensure at least one option
                const safe = rows.length
                    ? rows
                    : [{ postalCode: "Heidenheim", locationName: "Heidenheim" }];

                setOpts(safe);

                // If current value is empty or no longer present, select first
                if (!value || !safe.some(o => o.locationName === value || o.postalCode === value)) {
                    onChange(safe[0].locationName || safe[0].postalCode);
                }
            } catch {
                if (!alive) return;
                // Fallback so UI stays usable
                const fallback = [{ postalCode: "Heidenheim", locationName: "Heidenheim" }];
                setOpts(fallback);
                if (!value) onChange(fallback[0].locationName);
            } finally {
                if (alive) setLoading(false);
            }
        })();
        return () => {
            alive = false;
        };
    }, [onChange, value]);

    return (
        <select
            value={value}
            onChange={(e) => onChange(e.target.value)}
            disabled={loading || !opts.length}
            title="Pick the location"
        >
            {opts.map((o) => {
                const label = o.locationName || o.postalCode;
                return (
                    <option key={`${o.postalCode}|${o.locationName}`} value={label}>
                        {label}
                    </option>
                );
            })}
        </select>
    );
}
