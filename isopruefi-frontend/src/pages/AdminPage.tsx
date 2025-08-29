import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { TempChart, type Unit } from "../components/Weather";
import PlacePicker from "../components/PlacePicker";
import {UnitToggle} from "../components/UnitToggle"; 
import { clearToken } from "../utils/tokenHelpers";
export default function AdminPage() {

    const style = {padding: 20};
    const navigate = useNavigate();

    // UI state
    const [place, setPlace] = useState<string>("Heidenheim");
    const [unit, setUnit] = useState<Unit>("C"); // "C" | "F"
    const isF = unit === "F";

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div style={style}>
            <h1>Admin Page</h1>

            {/* Controls */}
            <div style={{ display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap", marginBottom: 12 }}>
                <div>
                    <label style={{ display: "block", fontWeight: 600, marginBottom: 4 }}>Place</label>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <PlacePicker value={place} onChange={setPlace} />
                        <small style={{ opacity: 0.7 }}>Pick the location</small>
                    </div>
                </div>

                <div>
                    <label style={{ display: "block", fontWeight: 600, marginBottom: 4 }}>Units</label>
                    <UnitToggle
                        value={isF}
                        onChange={(nextIsF: boolean) => setUnit(nextIsF ? "F" : "C")}
                    />
                </div>
            </div>

            <section>
                <TempChart place={place} isFahrenheit={isF} />
            </section>

            <br />
            <button style={{ marginTop: 16, padding: 12 }} onClick={handleLogout}>
                Logout
            </button>
        </div>
    );
}