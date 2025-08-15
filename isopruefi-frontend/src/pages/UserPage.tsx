import {TempChart} from "../components/Weather.tsx";
import {useNavigate} from "react-router-dom";
import {clearToken} from "../utils/tokenHelpers.ts";

export default function UserPage() {

    const style = {padding: 20};
    const navigate = useNavigate();

    const handleLogout = () => {
        clearToken();
        navigate("/signin");
    };

    return(
        <div style={style}>
            <h1>User Page</h1>
            <div>
                <h2>Weather chart on user page</h2>
                <TempChart />
            </div>
            <br/><br/><br/>
            <button style={style} onClick={handleLogout}>Logout</button>
        </div>
    )
}