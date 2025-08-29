import { useState } from "react";
import { TempChart } from "../components/Weather";
import { PlacePicker } from "../components/PlacePicker";
import { UnitToggle } from "../components/UnitToggle";
import { useNavigate } from "react-router-dom";
import { clearToken } from "../utils/tokenHelpers";

export default function UserPage() {
    const style = { padding: 20 };
    const navigate = useNavigate();

    const [place, setPlace] = useState("Heidenheim an der Brenz");
    const [isF, setIsF] = useState(false);

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div style={style}>
            <h1>User Page</h1>

            <div style={{ display: "flex", gap: 24, alignItems: "center" }}>
                <div>
                    <div style={{ fontWeight: 600 }}>Place</div>
                    <PlacePicker value={place} onChange={setPlace} />
                    <small style={{ marginLeft: 8, opacity: 0.7 }}>Pick the location</small>
                </div>

                <div>
                    <div style={{ fontWeight: 600 }}>Units</div>
                    <UnitToggle value={isF} onChange={setIsF} />
                </div>
            </div>

            <section style={{ marginTop: 16 }}>
                <TempChart place={place} isFahrenheit={isF} />
            </section>
            
            <br/><br/>
            <button style={{ marginTop: 24 }} onClick={handleLogout}>
                Logout
            </button>
        </div>
    );
}
