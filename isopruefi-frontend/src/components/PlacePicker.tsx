export function PlacePicker({
                                value,
                                onChange,
                                options = ["Heidenheim", "Berlin", "Munich", "Stuttgart"],
                            }: {
    value: string;
    onChange: (next: string) => void;
    options?: string[];
}) {
    return (
        <label style={{ display: "inline-flex", gap: 8, alignItems: "center" }}>
            Place:
            <select value={value} onChange={e => onChange(e.target.value)}>
                {options.map(p => <option key={p} value={p}>{p}</option>)}
            </select>
        </label>
    );
}
