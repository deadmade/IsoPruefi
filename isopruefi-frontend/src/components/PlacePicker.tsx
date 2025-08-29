import React from "react";

export type PlacePickerProps = {
    value: string;
    onChange: (place: string) => void;
};

const PLACES = ["Heidenheim", "Berlin", "Stuttgart", "Ulm"];

export const PlacePicker: React.FC<PlacePickerProps> = ({ value, onChange }) => {
    return (
        <select value={value} onChange={(e) => onChange(e.target.value)}>
            {PLACES.map((p) => (
                <option key={p} value={p}>{p}</option>
            ))}
        </select>
    );
};
