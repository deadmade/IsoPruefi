import { useEffect, useState } from 'react';
import { LineChart, ResponsiveContainer, Line } from 'recharts';
import { TemperatureDataClient, ApiException } from './api/api-client';

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
// const temperatureClient = new TemperatureDataClient(API_BASE_URL);

export function TempChart() {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<'all'|'hour'|'day'|'week'>('all');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const temperatureClient = new TemperatureDataClient('');

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const start = new Date('2025-07-01T00:00:00Z');
            const end = new Date('2025-07-31T23:59:59Z');
            const place = 'Heidenheim';
            const isFahrenheit = false;

            try {
                const data = await temperatureClient.getTemperature(
                    start,
                    end,
                    place,
                    isFahrenheit) as any;
                console.log(data);

                const south = data.temperatureSouth || [];
                const north = data.temperatureNord || [];
                const outside = data.temperatureOutside || [];

                if (!data) {
                    setError('No data received from server');
                    return;
                }

                // Type assertion due to API client being generated with wrong types
                // The actual response is TemperatureDataOverview, not TemperatureData[]

                const minLength = Math.min(
                    south.length, north.length, outside.length
                );

                // old implementation

                // const formatted: WeatherEntry[] = [];
                // for (let i = 0; i < minLength; i++) {
                //     const southData = data.temperatureSouth?.[i];
                //     const nordData = data.temperatureNord?.[i];
                //     const outsideData = data.temperatureOutside?.[i];

                const formatted: WeatherEntry[] = Array.from(
                    { length: minLength },
                    (_, i) => ({
                        timestamp:   south[i].timestamp
                            ? new Date(south[i].timestamp).toISOString()
                            : '',
                        tempSouth:   south[i].temperature ?? 0,
                        tempNorth:   north[i].temperature ?? 0,
                        tempOutside: outside[i].temperature ?? 0,
                    })
                );

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
                    <Line type="monotone" dataKey="tempSouth"   name="South"   stroke="#8884d8" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempNorth"   name="North"   stroke="#84d8d2" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
