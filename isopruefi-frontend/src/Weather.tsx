import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

/**
 * Represents one row of weather data.
 */
export type WeatherEntry = {
    id: number;
    timestamp: string;
    tempSouth: number;
    tempNorth: number;
    tempOutside: number;
};

/**
 * Dummy weather data used for charting temperature trends.
 */

export const WeatherData : WeatherEntry [] = [
    { id: 1, timestamp: "2025-07-17", tempSouth: 22, tempNorth: 21.5, tempOutside: 28 },
    { id: 2, timestamp: "2025-07-18", tempSouth: 24, tempNorth: 22.32, tempOutside: 32 },
    { id: 3, timestamp: "2025-07-19", tempSouth: 25, tempNorth: 20.141, tempOutside: 15 },
    { id: 4, timestamp: "2025-07-20", tempSouth: 21, tempNorth: 21.18, tempOutside: 12 },
    { id: 5, timestamp: "2025-07-21", tempSouth: 23, tempNorth: 21.97, tempOutside: 34 },
    { id: 6, timestamp: "2025-07-22", tempSouth: 22.35, tempNorth: 23.01, tempOutside: 22.55 },
];

/**
 * Displays the title of the weather chart section.
 */

export function WeatherChartTitle() {
    return (
        <h1>Weather Chart</h1>
    )
}

const style = {width: '100%', height: 400};

/**
 * Renders a multi-line chart showing South, North, and Outside temperatures.
 */

export function TempChart() {
    return (
        <div style={style}>
            <ResponsiveContainer width="100%" height="100%">
                <LineChart
                    width={500}
                    height={300}
                    data={WeatherData}
                    margin={{
                        top: 5,
                        right: 30,
                        left: 20,
                        bottom: 5,
                    }}
                >
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="timestamp" />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="tempSouth" name="South" stroke="#8884d8" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempNorth" name="North" stroke="#84d8d2" activeDot={{ r: 8}}/>
                    <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}

