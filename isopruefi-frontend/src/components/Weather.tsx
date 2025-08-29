import {useEffect, useMemo, useState} from 'react';
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

type Props = {
    /** City/place to request data for (defaults to Heidenheim) */
    place?: string;
    /** If true, request Fahrenheit from the API (defaults to false = °C) */
    isFahrenheit?: boolean;
};

const style = { width: '100%', height: 400 };

export function TempChart({ place = "Heidenheim", isFahrenheit = false }: Props) {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<"all" | "hour" | "day" | "week">("all");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        const run = async () => {
            setLoading(true);
            setError(null);

            const end = new Date();
            const start = new Date(end.getTime() - 24 * 60 * 60 * 1000); // last 24h

            try {
                const data = await tempClient.getTemperature(start, end, place, isFahrenheit);
                if (cancelled) return;

                if (!data) {
                    setError("No data received from server");
                    setWeatherData([]);
                    return;
                }

                const south = data.temperatureSouth ?? [];
                const north = data.temperatureNord ?? [];
                const outside = data.temperatureOutside ?? [];

                const maxLen = Math.max(south.length, north.length, outside.length);

                // Safe conversion to ISO string regardless of incoming type
                const toIso = (v: unknown): string => {
                    if (!v) return "";
                    if (v instanceof Date) return v.toISOString();
                    const d = new Date(v as any);
                    return isNaN(d.getTime()) ? "" : d.toISOString();
                };

                const merged: WeatherEntry[] = Array.from({ length: maxLen }, (_, i) => {
                    const ts = north[i]?.timestamp ?? south[i]?.timestamp ?? outside[i]?.timestamp ?? null;
                    return {
                        timestamp: toIso(ts),
                        tempSouth: south[i]?.temperature ?? 0,
                        tempNorth: north[i]?.temperature ?? 0,
                        tempOutside: outside[i]?.temperature ?? Number.NaN
                    };
                });

                setWeatherData(merged);
            } catch (err) {
                if (cancelled) return;
                console.error("Failed to fetch temperature data", err);
                if (ApiException.isApiException(err)) setError(`API Error (${err.status}): ${err.message}`);
                else if (err instanceof Error) setError(err.message);
                else setError("An unexpected error occurred");
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        run();
        return () => { cancelled = true; };
    }, [place, isFahrenheit]);

    // Client-side display filter
    const filteredData = useMemo(() => {
        if (filter === "all") return weatherData;
        const now = Date.now();
        const cutoff =
            filter === "hour" ? now - 60 * 60 * 1000 :
                filter === "day"  ? now - 24 * 60 * 60 * 1000 :
                    now - 7  * 24 * 60 * 60 * 1000;

        return weatherData.filter(e => {
            const t = new Date(e.timestamp).getTime();
            return !Number.isNaN(t) && t >= cutoff;
        });
    }, [weatherData, filter]);

    if (loading) return <div style={style}>Loading temperature data…</div>;

    if (error) {
        return (
            <div style={style}>
                <p style={{ color: "red" }}>Error loading temperature data: {error}</p>
                <button onClick={() => location.reload()}>Retry</button>
            </div>
        );
    }

    return (
        <div style={style}>
            <label style={{ marginBottom: 8, display: "block" }}>
                Show:{" "}
                <select value={filter} onChange={(e) => setFilter(e.target.value as any)}>
                    <option value="all">All</option>
                    <option value="hour">Last Hour</option>
                    <option value="day">Today</option>
                    <option value="week">This Week</option>
                </select>
                {" "}<small>({isFahrenheit ? "°F" : "°C"}, {place})</small>
            </label>

            <ResponsiveContainer width="100%" height="100%">
                <LineChart data={filteredData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis
                        dataKey="timestamp"
                        tickFormatter={(v) =>
                            new Date(v).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
                        }
                    />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="tempSouth"   name="South"   stroke="#8884d8" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempNorth"   name="North"   stroke="#84d8d2" activeDot={{ r: 8 }} />
                    <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" activeDot={{ r: 4 }} />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}