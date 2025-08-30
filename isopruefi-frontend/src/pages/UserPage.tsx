import {TempChart} from "../components/Weather.tsx";
import {useNavigate} from "react-router-dom";
import {clearToken} from "../utils/tokenHelpers.ts";

export default function UserPage() {
    const navigate = useNavigate();

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div className="h-full w-full bg-[#f5cacd] p-6">
            <h1 className="text-4xl font-extrabold text-[#d3546c] mb-8 text-center">
                User Page
            </h1>

            <div className="bg-white rounded-xl shadow p-6 max-w-300 mx-auto">
                <h2 className="text-2xl font-bold text-gray-800 mb-4 text-center">
                    Weather Chart
                </h2>
                <TempChart/>

                <div className="flex justify-end">
                    <button
                        onClick={handleLogout}
                        className="px-6 py-2 rounded-lg bg-pink-600 text-white font-semibold hover:bg-pink-800"
                    >
                        Logout
                    </button>
                </div>
            </div>
        </div>
    );
}