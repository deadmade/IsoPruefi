import React from "react";

export type UnitToggleProps = {
    /** true → Fahrenheit, false → Celsius */
    value: boolean;
    onChange: (isF: boolean) => void;
};

export const UnitToggle: React.FC<UnitToggleProps> = ({ value, onChange }) => {
    return (
        <label style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
            <input
                type="checkbox"
                checked={value}
                onChange={(e) => onChange(e.target.checked)}
            />
            <span>{value ? "Fahrenheit" : "Celsius"}</span>
        </label>
    );
};
