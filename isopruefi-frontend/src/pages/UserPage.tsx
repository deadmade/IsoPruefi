import {useState} from "react";
import {TempChart} from "../components/Weather.tsx";
import {PlacePicker} from "../components/PlacePicker.tsx";
import {UnitToggle} from "../components/UnitToggle.tsx";
import {useNavigate} from "react-router-dom";
import {clearToken} from "../utils/tokenHelpers.ts";
import ManageLocations from "../components/ManageLocations.tsx";

export default function UserPage() {
    const navigate = useNavigate();
    const [place, setPlace] = useState("Heidenheim an der Brenz");
    const [isF, setIsF] = useState(false);
    const [locVersion, setLocVersion] = useState(0);

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div className="min-h-screen w-full bg-[#f5cacd] p-6">
            <h1 className="text-4xl font-extrabold text-[#d3546c] mb-8 text-center">
                User Page
            </h1>

            {/* Controls Section */}
            <div
                className="flex flex-col sm:flex-row gap-6 items-start sm:items-center mb-6 bg-white rounded-xl shadow p-4 max-w-6xl mx-auto">
                <div className="w-full sm:w-80">
                    <label className="block text-sm font-semibold text-gray-700 mb-2">Location</label>
                    <PlacePicker value={place} onChange={setPlace} refreshKey={locVersion}/>
                    <p className="text-xs text-gray-500 mt-1">Select the monitoring location</p>
                </div>

                <div className="w-full sm:w-auto">
                    <label className="block text-sm font-semibold text-gray-700 mb-2">Temperature Units</label>
                    <UnitToggle value={isF} onChange={setIsF}/>
                </div>
            </div>

            {/* Weather Chart Section */}
            <div className="bg-white rounded-xl shadow p-6 max-w-6xl mx-auto">
                <h2 className="text-2xl font-bold text-gray-800 mb-4 text-center">
                    Weather Chart
                </h2>
                <TempChart place={place} isFahrenheit={isF}/>

                <div className="flex justify-end mt-4">
                    <button
                        onClick={handleLogout}
                        className="px-6 py-2 rounded-lg bg-pink-600 text-white font-semibold hover:bg-pink-800"
                    >
                        Logout
                    </button>
                </div>
            </div>

            {/* Admin Management Section - visible for enhanced functionality */}
            <div className="max-w-6xl mx-auto mt-6">
                <div className="bg-white rounded-xl shadow p-6">
                    <ManageLocations onChanged={() => setLocVersion(v => v + 1)}/>
                </div>
            </div>
        </div>
    );
}