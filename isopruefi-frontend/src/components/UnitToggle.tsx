import React from "react";

export type UnitToggleProps = {
    /** true → Fahrenheit, false → Celsius */
    value: boolean;
    onChange: (isF: boolean) => void;
};

export const UnitToggle: React.FC<UnitToggleProps> = ({ value, onChange }) => {
    return (
        <label className="inline-flex items-center gap-2 cursor-pointer">
            <input
                type="checkbox"
                checked={value}
                onChange={(e) => onChange(e.target.checked)}
                className="w-4 h-4 text-pink-600 bg-gray-100 border-gray-300 rounded focus:ring-pink-500 focus:ring-2"
            />
            <span className="text-sm font-medium text-gray-700">
                {value ? "Fahrenheit" : "Celsius"}
            </span>
        </label>
    );
};
