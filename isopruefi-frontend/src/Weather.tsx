import { useEffect, useState } from 'react';
import { LineChart, ResponsiveContainer } from 'recharts';

export type WeatherEntry = {
    timestamp: string;
    tempSouth: number;
    tempNorth: number;
    tempOutside: number;
};

export function WeatherChartTitle() {
    return <h1>Weather Chart</h1>
}

const style = { width: '100%', height: 400 };

export function TempChart() {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<'all'|'hour'|'day'|'week'>('all');

    useEffect(() => {
        const fetchData = async () => {
            const start = '2025-07-01T00:00:00Z';
            const end = '2025-07-31T23:59:59Z';
            const place = 'YourLocation'; // Replace with your actual location (e.g., "Heidenheim")
            const isFahrenheit = false;

            try {
                const response = await fetch(`http://localhost:5000/api/v1/TemperatureData/GetTemperature?start=${start}&end=${end}&place=${place}&isFahrenheit=${isFahrenheit}`);
                const data = await response.json();

                const formatted: WeatherEntry[] = data.temperatureSouth.map((_: any, index: number) => ({
                    timestamp: data.temperatureSouth[index].timestamp,
                    tempSouth: data.temperatureSouth[index].temperature,
                    tempNorth: data.temperatureNorth[index]?.temperature ?? 0,
                    tempOutside: data.temperatureOutside[index]?.temperature ?? 0
                }));

                setWeatherData(formatted);
            } catch (error) {
                console.error("Failed to fetch temperature data", error);
            }
        };

        fetchData();
    }, []);

    const now = Date.now();
    let cutoff = 0;
    switch (filter) {
        case 'hour': cutoff = now - 1000 * 60 * 60; break;
        case 'day':  cutoff = now - 1000 * 60 * 60 * 24; break;
        case 'week': cutoff = now - 1000 * 60 * 60 * 24 * 7; break;
    }
    const filteredData = filter === 'all'
        ? weatherData
        : weatherData.filter(e => new Date(e.timestamp).getTime() >= cutoff);

    return (
        <div style={style}>
            <label>
                Show:{' '}
                <select value={filter} onChange={e => setFilter(e.target.value as any)}>
                    <option value="all">All</option>
                    <option value="hour">Last Hour</option>
                    <option value="day">Today</option>
                    <option value="week">This Week</option>
                </select>
            </label>

            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={filteredData}>
                    {/* … */}
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
