import {TempChart} from "../components/Weather.tsx";

export default function UserPage() {

    const style = {padding: 20};

    return(
        <div style={style}>
            <h1>User Page</h1>
            <div>
                <h2>Weather chart on user page</h2>
                <TempChart />
            </div>
        </div>
    )
}