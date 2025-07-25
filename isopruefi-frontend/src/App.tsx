import './App.css';
import {WeatherChartTitle} from "./Weather"
import {TempChart} from "./Weather";

// entry point of the website. shows WeatherChartTitle and TempChart

function App() {
    return (
        <div>
            <WeatherChartTitle />
            <TempChart />
        </div>
    );
}

export default App;
