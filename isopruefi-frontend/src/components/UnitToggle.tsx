import React from "react";

export type UnitToggleProps = {
    /** true → Fahrenheit, false → Celsius */
    value: boolean;
    onChange: (isF: boolean) => void;
};

export function UnitToggle({ value, onChange }: UnitToggleProps) {
  return (
    <div className="flex items-center gap-6">
      <label className="flex items-center gap-2">
        <input
          type="radio"
          name="tempUnit"
          checked={!value}
          onChange={() => onChange(false)}
          className="text-pink-600 focus:ring-pink-600"
        />
        <span>°C</span>
      </label>

      <label className="flex items-center gap-2">
        <input
          type="radio"
          name="tempUnit"
          checked={value}
          onChange={() => onChange(true)}
          className="text-pink-600 focus:ring-pink-600"
        />
        <span>°F</span>
      </label>
    </div>
  );
}
