export type Unit = "C" | "F";

export function UnitToggle({
                               value,
                               onChange,
                           }: { value: Unit; onChange: (next: Unit) => void }) {
    return (
        <label style={{ display: "inline-flex", gap: 8, alignItems: "center" }}>
            Show in:
            <select value={value} onChange={e => onChange(e.target.value as Unit)}>
                <option value="C">°C</option>
                <option value="F">°F</option>
            </select>
        </label>
    );
}
