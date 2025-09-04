import React, {useEffect, useState} from "react";
import {fetchPostalLocations, type PostalLocation} from "../api/clients";

type Props = {
    value?: string;
    onChange: (place: string) => void;
    placeholder?: string;
    refreshKey?: number;              // <-- add this
};

const DEFAULT_PLACE = "Heidenheim an der Brenz";

export const PlacePicker: React.FC<Props> = ({
                                                 value,
                                                 onChange,
                                                 placeholder = "Pick the location",
                                                 refreshKey,                       // <-- accept it
                                             }) => {
    const [opts, setOpts] = useState<PostalLocation[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let alive = true;
        (async () => {
            setLoading(true);
            try {
                const rows = await fetchPostalLocations();
                if (!alive) return;

                setOpts(rows);

                // Only set default value if no value is currently selected
                const current = (value ?? "").trim();
                if (!current && rows.length > 0) {
                    const preferred = rows.find((r: PostalLocation) => /heidenheim/i.test(r.locationName)) ?? rows[0];
                    if (preferred?.locationName) onChange(preferred.locationName);
                }
            } finally {
                if (alive) setLoading(false);
            }
        })();

        return () => {
            alive = false;
        };
    }, [refreshKey, onChange, value]);

    const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const next = e.target.value;
        console.log('PlacePicker: Location changed to:', next);
        if (next) onChange(next); // never propagate ""
    };

    const current = (value ?? "").trim();

    console.log('PlacePicker render - current value:', current, 'available options:', opts.map(o => o.locationName));

    // Ensure the selected value exists in options or add it if it's the default
    const hasCurrentOption = current === "" || opts.some(o => o.locationName === current) || current === DEFAULT_PLACE;
    const selectValue = hasCurrentOption ? current : "";

    console.log('PlacePicker - selectValue:', selectValue, 'hasCurrentOption:', hasCurrentOption);

    return (
        <select
            value={selectValue}
            onChange={handleChange}
            disabled={loading || opts.length === 0}
            className="w-full rounded-lg border border-gray-300 px-4 py-2 bg-white text-gray-900 focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100 disabled:text-gray-400"
        >
            {selectValue === "" && (
                <option value="" disabled>
                    {placeholder}
                </option>
            )}

            {opts.map(o => (
                <option
                    key={`${o.postalCode}-${o.locationName}`}
                    value={o.locationName}
                >
                    {o.locationName}
                </option>
            ))}

            {!opts.some(o => o.locationName === DEFAULT_PLACE) && (
                <option value={DEFAULT_PLACE}>{DEFAULT_PLACE}</option>
            )}
        </select>
    );
};
