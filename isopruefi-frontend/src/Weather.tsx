import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

export const WeatherData = [
    { id: 1, timestamp: "2025-07-17", tempInside: 22, tempOutside: 28 },
    { id: 2, timestamp: "2025-07-18", tempInside: 24, tempOutside: 32 },
    { id: 3, timestamp: "2025-07-19", tempInside: 25, tempOutside: 15 },
    { id: 4, timestamp: "2025-07-20", tempInside: 21, tempOutside: 12 },
    { id: 5, timestamp: "2025-07-21", tempInside: 23, tempOutside: 34 },
    { id: 6, timestamp: "2025-07-22", tempInside: 22.35, tempOutside: 22.55 },
];


export function WeatherChart() {
    return (
        <h1>Weather Chart</h1>
    )
}

export function TempChart() {
    return (
        <div style={{width: '100%', height: 400}}>
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
                    <Line type="monotone" dataKey="tempInside" name="Inside" stroke="#8884d8" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}

