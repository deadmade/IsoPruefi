import {useEffect, useState} from "react";
import {CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis} from "recharts";
import {ApiException, tempClient} from "../api/clients";

export type WeatherEntry = {
    timestamp: string;
    tempSouth: number;
    tempNorth: number;
    tempOutside: number;
};

export function TempChart() {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<"all" | "hour" | "day" | "week">("all");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const end = new Date();
            const start = new Date(end.getTime() - 24 * 60 * 60 * 1000);
            const place = "Heidenheim";
            const isFahrenheit = false;

            try {
                const data = await tempClient.getTemperature(start, end, place, isFahrenheit);
                if (!data) {
                    setError("No data received from server");
                    return;
                }

                const south = data.temperatureSouth || [];
                const north = data.temperatureNord || [];
                const outside = data.temperatureOutside || [];
                const maxLen = Math.max(south.length, north.length, outside.length);

                const toIso = (t: unknown) =>
                    t ? (t instanceof Date ? t.toISOString() : new Date(t as any).toISOString()) : "";

                const formatted: WeatherEntry[] = Array.from({length: maxLen}, (_, i) => {
                    const anyTs = north[i]?.timestamp ?? south[i]?.timestamp ?? outside[i]?.timestamp ?? null;
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
                else setError("An unexpected error occurred");
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    const now = new Date().getTime();
    let cutoff = 0;
    switch (filter) {
        case "hour":
            cutoff = now - 60 * 60 * 1000;
            break;
        case "day":
            cutoff = now - 24 * 60 * 60 * 1000;
            break;
        case "week":
            cutoff = now - 7 * 24 * 60 * 60 * 1000;
            break;
        default:
            cutoff = 0;
    }

    const filteredData = filter === "all"
        ? weatherData
        : weatherData.filter(e => {
            const t = new Date(e.timestamp).getTime();
            return !Number.isNaN(t) && t >= cutoff;
        });

    if (loading) return (
        <div className="w-full h-[400px] rounded-xl bg-white boder-black-100 shadow flex items-center justify-center">
            <span className="text-gray-600">Loading temperature data…</span>
        </div>
    );

    if (error) return (
        <div
            className="w-full h-[400px] rounded-xl bg-white boder-black-100 shadow p-4 flex flex-col items-center justify-center gap-3">
            <p className="text-red-600 text-sm">Error loading temperature data: {error}</p>
            <button
                onClick={() => window.location.reload()}
                className="px-4 py-2 rounded-lg bg-pink-600 text-white hover:bg-pink-700"
            >
                Retry
            </button>
        </div>
    );

    const lastVal = (arr: WeatherEntry[], key: "tempSouth" | "tempNorth" | "tempOutside") => {
        for (let i = arr.length - 1; i >= 0; i--) {
            const v = arr[i][key];
            if (typeof v === "number" && !Number.isNaN(v)) return v;
        }
        return null;
    };

    const vSouth = lastVal(weatherData, "tempSouth");
    const vNorth = lastVal(weatherData, "tempNorth");
    const vOut = lastVal(weatherData, "tempOutside");
    const fmt = (v: number | null) => (v == null ? "—" : `${v.toFixed(1)}°C`);

    return (
        <div className="flex w-full gap-6">
            {/* LEFT: chart card */}
            <div className="flex-1 rounded-xl bg-white shadow p-4 h-[400px] border border-gray-300 text-center">
                <label className="mb-3 block text-sm text-gray-700">
                    Show:{" "}
                    <select
                        value={filter}
                        onChange={(e) => setFilter(e.target.value as any)}
                        className="ml-2 rounded-md border border-pink-200 bg-white 
                     px-3 py-1 text-sm shadow-sm text-center
                     focus:outline-none focus:ring-2 focus:ring-pink-100 focus:border-pink-200"
                    >
                        <option value="all">All</option>
                        <option value="hour">Last Hour</option>
                        <option value="day">Today</option>
                        <option value="week">This Week</option>
                    </select>
                </label>

                <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={filteredData} margin={{top: 8, right: 16, left: 8, bottom: 32}}>
                        <CartesianGrid strokeDasharray="3 3"/>
                        <XAxis
                            dataKey="timestamp"
                            tickFormatter={(v) =>
                                new Date(v).toLocaleTimeString([], {hour: "2-digit", minute: "2-digit"})
                            }
                        />
                        <YAxis/>
                        <Tooltip/>
                        <Legend verticalAlign="bottom" align="center" wrapperStyle={{paddingTop: 8}}/>
                        <Line type="monotone" dataKey="tempSouth" name="South" stroke="#d3546c" activeDot={{r: 1}}/>
                        <Line type="monotone" dataKey="tempNorth" name="North" stroke="#84d8d2" activeDot={{r: 1}}/>
                        <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" activeDot={{r: 1}}/>
                    </LineChart>
                </ResponsiveContainer>
            </div>

            {/* RIGHT: tiles */}
            <div className="w-64 flex flex-col gap-4">
                <div className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                    <div className="text-sm text-gray-500">South</div>
                    <div className="mt-1 text-3xl font-bold text-[#d3546c]">{fmt(vSouth)}</div>
                </div>
                <div className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                    <div className="text-sm text-gray-500">North</div>
                    <div className="mt-1 text-3xl font-bold text-[#3bbfba]">{fmt(vNorth)}</div>
                </div>
                <div className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                    <div className="text-sm text-gray-500">Outside</div>
                    <div className="mt-1 text-3xl font-bold text-[#3fbf86]">{fmt(vOut)}</div>
                </div>
            </div>
        </div>
    );

}
