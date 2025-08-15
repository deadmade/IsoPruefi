import {TempChart} from "../components/Weather.tsx";
import {useNavigate} from "react-router-dom";
import {clearToken} from "../utils/tokenHelpers.ts";

export default function AdminPage() {

    const style = {padding: 20};
    const navigate = useNavigate();

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return (
        <div style={style}>
            <h1>Admin Page</h1>
            <div>
                <h2>Weather Chart on Admin page</h2>
                <TempChart />
                <br/><br/><br/>
                <button style={style} onClick={handleLogout}>Logout</button>
            </div>
        </div>
    )
}