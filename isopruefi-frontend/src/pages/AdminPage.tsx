import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { clearToken } from "../utils/tokenHelpers";
import { TempChart, type Unit } from "../components/Weather";
import PlacePicker from "../components/PlacePicker";
import {UnitToggle} from "../components/UnitToggle";
import ManageLocations from "../components/ManageLocations";

export default function AdminPage() {
    const style = { padding: 20 };
    const navigate = useNavigate();

    const [place, setPlace] = useState("Heidenheim");
    const [unit, setUnit] = useState<Unit>("C");
    const [pickerKey, setPickerKey] = useState(0); // to re-mount the picker after changes

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div style={style}>
            <div style={{ display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap" }}>
                <div>
                    <label style={{ display: "block", fontWeight: 600, marginBottom: 4 }}>Place</label>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <PlacePicker key={pickerKey} value={place} onChange={setPlace} />
                        <small style={{ opacity: 0.7 }}>Pick the location</small>
                    </div>
                </div>

                <div>
                    <label style={{ display: "block", fontWeight: 600, marginBottom: 4 }}>Units</label>
                    <UnitToggle value={unit === "F"} onChange={(f) => setUnit(f ? "F" : "C")} />
                </div>
            </div>

            <h2 style={{ marginTop: 20 }}>Weather Chart on Admin page</h2>
            <TempChart place={place} isFahrenheit={unit === "F"} />
            
            <ManageLocations onChanged={() => setPickerKey((k) => k + 1)} />

            <br /><br />
            <button style={style} onClick={handleLogout}>Logout</button>
        </div>
    );
}
