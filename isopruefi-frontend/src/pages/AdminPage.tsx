import { useState } from "react";
import { TempChart } from "../components/Weather";
import { PlacePicker } from "../components/PlacePicker";
import { UnitToggle } from "../components/UnitToggle";
import { useNavigate } from "react-router-dom";
import { clearToken } from "../utils/tokenHelpers";
import ManageLocations from "../components/ManageLocations";
import ManageTopics from "../components/ManageTopics";

export default function AdminPage() {
    const navigate = useNavigate();

    const [place, setPlace] = useState("Heidenheim an der Brenz");
    const [isF, setIsF] = useState(false);
    const [locVersion, setLocVersion] = useState(0);

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div /*className="h-full w-full bg-[#f5cacd] p-6"*/>
            <h1 /*className="text-4xl font-extrabold text-[#d3546c] mb-8 text-center"*/>
                Admin Page
            </h1>

            <div style={{display: "flex", gap: 24, alignItems: "center"}}>
                <div>
                    <div style={{fontWeight: 600}}>Place</div>
                    <PlacePicker value={place} onChange={setPlace} refreshKey={locVersion}/>
                    <small style={{marginLeft: 8, opacity: 0.7}}>Pick the location</small>
                </div>

                <div>
                    <div style={{fontWeight: 600}}>Units</div>
                    <UnitToggle value={isF} onChange={setIsF}/>
                </div>
            </div>

            <section /* className="bg-white rounded-xl shadow p-6 max-w-[1200px] mx-auto mt-4"*/>
                <h2/* className="text-2xl font-bold text-gray-800 mb-4 text-center"*/>
                    Weather Chart
                </h2>
                <TempChart place={place} isFahrenheit={isF}/>
            </section>

            <br/><br/>
            <ManageLocations onChanged={() => setLocVersion(v => v + 1)}/>
            <br/><br/>
            <ManageTopics/>

            <div className="flex justify-end">
                <button
                    onClick={handleLogout}
                    /*className="mt-6 px-6 py-2 rounded-lg bg-pink-600 text-white font-semibold hover:bg-pink-800"*/
                >
                    Logout
                </button>
            </div>
        </div>
    );
}
