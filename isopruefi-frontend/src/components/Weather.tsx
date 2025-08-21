import { useEffect, useState } from 'react';
import {
    LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import { tempClient, ApiException } from '../api/clients';

export type Unit = "C" | "F";
export type WeatherEntry = {
    timestamp: string;
    tempSouth: number;
    tempNorth: number;
    tempOutside: number;
};

const style = { width: '100%', height: 400 };

export function TempChart() {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<'all'|'hour'|'day'|'week'>('all');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const end = new Date();
            const start = new Date(end.getTime() - 24 * 60 * 60 * 1000);
            const place = 'Heidenheim';
            const isFahrenheit = false;

            try {
                const data = await tempClient.getTemperature(start, end, place, isFahrenheit);
                if (!data) { setError('No data received from server'); return; }

                const south = data.temperatureSouth || [];
                const north = data.temperatureNord || [];
                const outside = data.temperatureOutside || [];

                const maxLen = Math.max(south.length, north.length, outside.length);

                const toIso = (t: unknown) =>
                    t
                        ? (t instanceof Date ? t.toISOString() : new Date(t as any).toISOString())
                        : '';

                const formatted: WeatherEntry[] = Array.from({ length: maxLen }, (_, i) => {
                    const anyTs =
                        north[i]?.timestamp ??
                        south[i]?.timestamp ??
                        outside[i]?.timestamp ??
                        null;

                    return {
                        timestamp: toIso(anyTs),
                        tempSouth: south[i]?.temperature ?? 0,
                        tempNorth: north[i]?.temperature ?? 0,
                        tempOutside: outside[i]?.temperature ?? 0,
                    };
                });

                setWeatherData(formatted);
            } catch (err) {
                console.error("Failed to fetch temperature data", err);
                if (ApiException.isApiException(err)) setError(`API Error (${err.status}): ${err.message}`);
                else if (err instanceof Error) setError(err.message);
                else setError('An unexpected error occurred');
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    // Client-side display filter
    const now = new Date().getTime();
    let cutoff = 0;
    switch (filter) {
        case 'hour': cutoff = now - 60 * 60 * 1000; break;
        case 'day':  cutoff = now - 24 * 60 * 60 * 1000; break;
        case 'week': cutoff = now - 7 * 24 * 60 * 60 * 1000; break;
        default:     cutoff = 0;
    }

    const filteredData = filter === 'all'
        ? weatherData
        : weatherData.filter(e => {
            const t = new Date(e.timestamp).getTime();
            return !Number.isNaN(t) && t >= cutoff;
        });

    if (loading) return <div style={style}>Loading temperature data...</div>;

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
                    <XAxis
                        dataKey="timestamp"
                        tickFormatter={(v) => new Date(v).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" 
                          dataKey="tempSouth"   
                          name="South"
                          stroke="#8884d8"
                          activeDot={{ r: 8 }} />
                    <Line type="monotone" 
                          dataKey="tempNorth"   
                          name="North" 
                          stroke="#84d8d2"  
                          activeDot={{ r: 8 }} />
                    <Line type="monotone" 
                          dataKey="tempOutside" 
                          name="Outside"
                          stroke="#82ca9d" 
                          activeDot={{r: 4}}/>
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
