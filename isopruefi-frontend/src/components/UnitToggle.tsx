/**
 * @fileoverview Temperature unit toggle component for switching between Celsius and Fahrenheit.
 * Provides a user-friendly checkbox interface for temperature unit selection.
 */

import React from "react";

/**
 * Props for the UnitToggle component.
 */
export type UnitToggleProps = {
    /** Current unit selection: true for Fahrenheit, false for Celsius */
    value: boolean;
    /** Callback function called when the unit selection changes */
    onChange: (isF: boolean) => void;
};

/**
 * A toggle component for switching between Celsius and Fahrenheit temperature units.
 *
 * Features:
 * - Checkbox-style toggle interface
 * - Clear visual indication of current selection
 * - Accessible design with proper labels
 * - Consistent styling with application theme
 *
 * @param props - Component configuration
 * @returns JSX element containing the unit toggle
 *
 * @example
 * ```tsx
 * // Basic usage
 * const [isFahrenheit, setIsFahrenheit] = useState(false);
 *
 * <UnitToggle 
 *   value={isFahrenheit} 
 *   onChange={setIsFahrenheit} 
 * />
 * ```
 */
export const UnitToggle: React.FC<UnitToggleProps> = ({value, onChange}) => {
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
