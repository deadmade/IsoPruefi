    import './App.css';
    import {WeatherChart} from "./Weather"
    import { WeatherData } from "./Weather";
    import {TempChart} from "./Weather";

    function App() {
        return (
            <div>
                <WeatherChart />
                <TempChart />
            </div>
        );
    }

// export function List() {
//     const inside = WeatherData.map(data =>
//     <li key={data.id}>
//         <p>
//             {data.timestamp}
//             <b>{' ' + data.tempInside + ' '}</b>
//             <b>{' ' + data.tempOutside + ' '}</b>
//         </p>
//     </li>
// );
//     return (
//         <article>
//             <h2>Inside/Outside temperature</h2>
//             <ul>
//                 <li>{inside}</li>
//             </ul>
//         </article>
//     )
// }

export default App;
