import {TempChart} from "../components/Weather.tsx";

export default function AdminPage() {

    const style = {padding: 20};

    return (
        <div style={style}>
            <h1>Admin Page</h1>
            <div>
                <h2>Weather Chart on Admin page</h2>
                <TempChart />
            </div>
        </div>
    )
}