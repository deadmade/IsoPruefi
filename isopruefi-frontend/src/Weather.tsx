import { useEffect, useState } from 'react';
import {
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer
} from 'recharts';import { TemperatureDataClient, ApiException } from './api/api-client';

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

// const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7240';
const temperatureClient = new TemperatureDataClient('http://localhost:5160');

export function TempChart() {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<'all'|'hour'|'day'|'week'>('all');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const start = new Date('2025-07-01T00:00:00Z');
            const end = new Date();
            const place = 'Heidenheim';
            const isFahrenheit = false;

            try {
                const data = await temperatureClient.getTemperature(
                    start,
                    end,
                    place,
                    isFahrenheit) as any;
                console.log('📦 Raw API response:', data);

                // const south = data.temperatureSouth || [];
                const north = data.temperatureNord || [];
                const outside = data.temperatureOutside || [];

                if (!data) {
                    setError('No data received from server');
                    return;
                }

                const minLength =
                    Math.min(north.length, outside.length);

                // old implementation

                // const formatted: WeatherEntry[] = [];
                // for (let i = 0; i < minLength; i++) {
                //     const southData = data.temperatureSouth?.[i];
                //     const nordData = data.temperatureNord?.[i];
                //     const outsideData = data.temperatureOutside?.[i];

                const formatted: WeatherEntry[] = Array.from(
                    { length: minLength },
                    (_, i) => ({
                        timestamp: north[i].timestamp
                            ? new Date(north[i].timestamp).toISOString()
                            : '',
                        tempSouth: 0,
                        tempNorth: north[i]?.temperature ?? 0,
                        tempOutside: outside[i]?.temperature ?? 0,
                    })
                );
                console.log("Formatted weatherData", formatted);

                //     if (southData?.timestamp) {
                //         formatted.push({
                //             timestamp: new Date(southData.timestamp).toISOString(),
                //             tempSouth: southData.temperature || 0,
                //             tempNorth: nordData?.temperature || 0,
                //             tempOutside: outsideData?.temperature || 0,
                //         });
                //     }
                // }

                setWeatherData(formatted);
            } catch (error) {
                console.error("Failed to fetch temperature data", error);

                if (ApiException.isApiException(error)) {
                    setError(`API Error (${error.status}): ${error.message}`);
                } else if (error instanceof Error) {
                    setError(`Error: ${error.message}`);
                } else {
                    setError('An unexpected error occurred');
                }
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    // filtering

    const now = new Date().getTime();
    let cutoff = 0;

    switch (filter) {
        case 'hour':
            cutoff = now - 60 * 60 * 1000;
            break;
        case 'day':
            cutoff = now - 24 * 60 * 60 * 1000;
            break;
        case 'week':
            cutoff = now - 7 * 24 * 60 * 60 * 1000;
            break;
        default:
            cutoff = 0;
    }

    const filteredData = filter === 'all'
        ? weatherData
        : weatherData.filter(e => {
            const time = new Date(e.timestamp).getTime();
            return !isNaN(time) && time >= cutoff;
        });

    console.log("Filtered", filteredData.length, "of", weatherData.length);

    if (loading) {
        return <div style={style}>Loading temperature data...</div>;
    }

    if (error) {
        return (
            <div style={style}>
                <p style={{ color: 'red' }}>Error loading temperature data: {error}</p>
                <button onClick={() => window.location.reload()}>Retry</button>
            </div>
        );
    }

    console.log("Filtered data", filteredData);
    console.log("Sample timestamps", weatherData.map(e => e.timestamp));

    return (
        <div style={style}>

            <label style={{ marginBottom: 8, display: 'block' }}>
                Show:{' '}
                <select value={filter} onChange={(e) => setFilter(e.target.value as any)}>
                    <option value="all">All</option>
                    <option value="hour">Last Hour</option>
                    <option value="day">Today</option>
                    <option value="week">This Week</option>
                </select>
            </label>

            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={filteredData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="timestamp"
                           tickFormatter={(value) => new Date(value).
                           toLocaleTimeString([],
                               { hour: '2-digit', minute: '2-digit' })}/>
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line
                        type="monotone"
                        dataKey="tempSouth"
                        name="South"
                        stroke="#8884d8"
                        activeDot={{ r: 8 }}
                    />
                    <Line
                        type="monotone"
                        dataKey="tempNorth"
                        name="North"
                        stroke="#84d8d2"
                        activeDot={{ r: 8 }}
                    />
                    <Line
                        type="monotone"
                        dataKey="tempOutside"
                        name="Outside"
                        stroke="#82ca9d"
                    />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
