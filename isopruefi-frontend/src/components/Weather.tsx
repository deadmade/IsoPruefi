import { useEffect, useState } from "react";
import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { ApiException, getStoredLocationName, tempClient } from "../api/clients";

/**
 * Represents a single weather data entry for a specific timestamp.
 * 
 * @property {string} timestamp - ISO formatted timestamp of the data point.
 * @property {number} t - Epoch time in milliseconds.
 * @property {number} [tempOutside] - Outside temperature value.
 * @property {Record<string, number | string | undefined>} [key] - Dynamic sensor values keyed by sensor name.
 */
export type WeatherEntry = {
    timestamp: string;     // ISO
    t: number;             // epoch ms
    [key: string]: number | string | undefined;
    tempOutside?: number;
};

/**
 * Props for the TempChart component.
 * 
 * @property {string} [place] - Location name for which to display weather data.
 * @property {boolean} [isFahrenheit] - Whether to display temperatures in Fahrenheit.
 */
type TempChartProps = { place?: string; isFahrenheit?: boolean };

/**
 * Displays a temperature chart and sensor tiles for a given location.
 * Fetches weather and sensor data from the backend, supports filtering by time range,
 * and allows switching between Celsius and Fahrenheit.
 * 
 * @param {TempChartProps} props - Component props.
 * @returns {JSX.Element} The rendered temperature chart and sensor tiles.
 */
export function TempChart({ place = "Heidenheim an der Brenz", isFahrenheit = false }: TempChartProps = {}) {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<"all" | "hour" | "day" | "week">("all");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [sensorKeys, setSensorKeys] = useState<string[]>([]);

    /**
     * Converts Celsius to Fahrenheit.
     * @param {number} v - Temperature in Celsius.
     * @returns {number} Temperature in Fahrenheit.
     */
    const toF = (v: number) => (v * 9) / 5 + 32;

    /**
     * Converts temperature to selected unit.
     * @param {number} v - Temperature value.
     * @returns {number} Converted temperature.
     */
    const maybeConv = (v: number) => (isFahrenheit ? toF(v) : v);

    /**
     * Fetches weather and sensor data from the backend API.
     * Applies selected time filter and temperature unit.
     */
    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const end = new Date();
            let start: Date;
            switch (filter) {
                case "hour":
                    start = new Date(end.getTime() - 60 * 60 * 1000);
                    break;
                case "day":
                    start = new Date(end);
                    start.setHours(0, 0, 0, 0);
                    break;
                case "week":
                    start = new Date(end.getTime() - 7 * 24 * 60 * 60 * 1000);
                    break;
                default:
                    start = new Date(end.getTime() - 30 * 24 * 60 * 60 * 1000);
            }

            try {
                const stored = getStoredLocationName(place);

                const data = await tempClient.getTemperature(start, end, stored, false);
                if (!data) { setError("No data received from server"); return; }

                const tsMap = new Map<string, any>();
                const discovered = new Set<string>();

                // Map sensor data to keys and timestamps
                (data.sensorData || []).forEach((s: any) => {
                    const key = `temp_${(s.sensorName || "Unknown")}_${(s.location || "")}`.replace(/\s+/g, "_");
                    discovered.add(key);
                    (s.temperatureDatas || []).forEach((p: any) => {
                        const iso = p?.timestamp ? new Date(p.timestamp).toISOString() : "";
                        if (!iso) return;
                        if (!tsMap.has(iso)) tsMap.set(iso, { timestamp: iso, t: new Date(iso).getTime() });
                        const v = typeof p.temperature === "number" ? p.temperature : undefined;
                        if (typeof v === "number") tsMap.get(iso)[key] = maybeConv(v);
                    });
                });

                // Map outside temperature data
                (data.temperatureOutside || []).forEach((p: any) => {
                    const iso = p?.timestamp ? new Date(p.timestamp).toISOString() : "";
                    if (!iso) return;
                    if (!tsMap.has(iso)) tsMap.set(iso, { timestamp: iso, t: new Date(iso).getTime() });
                    const v = typeof p.temperature === "number" ? p.temperature : undefined;
                    if (typeof v === "number") tsMap.get(iso).tempOutside = maybeConv(v);
                });

                // Format and sort data by timestamp
                const formatted: WeatherEntry[] = Array.from(tsMap.values())
                    .filter(e => e.timestamp)
                    .sort((a, b) => a.t - b.t);

                // Fill missing values for each key
                const keys = ["tempOutside", ...Array.from(discovered)];
                for (const k of keys) {
                    const firstIdx = formatted.findIndex(e => typeof e[k] === "number" && !Number.isNaN(e[k] as number));
                    if (firstIdx === -1) continue;

                    const firstVal = formatted[firstIdx][k] as number;
                    for (let i = 0; i < firstIdx; i++) {
                        formatted[i][k] = firstVal;
                    }

                    let last = firstVal;
                    for (let i = firstIdx + 1; i < formatted.length; i++) {
                        const v = formatted[i][k];
                        if (typeof v === "number" && !Number.isNaN(v)) last = v as number;
                        else formatted[i][k] = last;
                    }
                }

                setWeatherData(formatted);
                setSensorKeys(Array.from(discovered).sort());
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
    }, [place, isFahrenheit, filter]);

    /**
     * Renders a loading indicator while temperature data is being fetched.
     * @returns {JSX.Element} Loading spinner and message.
     */
    if (loading) return (
        <div className="w-full h-[400px] rounded-xl bg-white boder-black-100 shadow flex items-center justify-center">
            <span className="text-gray-600">Loading temperature data…</span>
        </div>
    );

    /**
     * Renders an error message and retry button if temperature data fails to load.
     * @returns {JSX.Element} Error message and retry button.
     */
    if (error) return (
        <div className="w-full h-[400px] rounded-xl bg-white boder-black-100 shadow p-4 flex flex-col items-center justify-center gap-3">
            <p className="text-red-600 text-sm">Error loading temperature data: {error}</p>
            <button
                onClick={() => window.location.reload()}
                className="px-4 py-2 rounded-lg bg-pink-600 text-white hover:bg-pink-700"
            >
                Retry
            </button>
        </div>
    );

    /**
     * Returns the last valid value for a given key in the weather data array.
     * @param {WeatherEntry[]} arr - Array of weather entries.
     * @param {string} key - Key to search for.
     * @returns {number | null} Last valid value or null.
     */
    const lastVal = (arr: WeatherEntry[], key: string) => {
        for (let i = arr.length - 1; i >= 0; i--) {
            const v = arr[i][key];
            if (typeof v === "number" && !Number.isNaN(v)) return v;
        }
        return null;
    };

    /**
     * Formats a temperature value for display.
     * @param {number | null} v - Temperature value.
     * @returns {string} Formatted temperature string.
     */
    const fmt = (v: number | null) => (v == null ? "—" : `${(v || 0).toFixed(1)}°${isFahrenheit ? "F" : "C"}`);

    const vOut = lastVal(weatherData, "tempOutside");

    /** Chart line colors for sensors and outside temperature. */
    const colors = ["#d3546c", "#84d8d2", "#82ca9d", "#ffc658", "#ff7c7c", "#8884d8"];

    /**
     * Formats the X axis tick value based on the selected filter.
     * @param {number} t - Epoch time in ms.
     * @returns {string} Formatted tick label.
     */
    const tickFmt = (t: number) => {
        const d = new Date(t);
        if (filter === "week") return d.toLocaleString([], { weekday: "short", hour: "2-digit" });
        if (filter === "all") return d.toLocaleDateString([], { month: "short", day: "2-digit" });
        return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    };

    return (
        <div className="flex w-full gap-6">
            {/* Chart Section */}
            <div className="flex-1 rounded-xl bg-white shadow p-4 h-[400px] border border-gray-300 text-center">
                <label className="mb-3 block text-sm text-gray-700">
                    Show:{" "}
                    <select
                        value={filter}
                        onChange={(e) => setFilter(e.target.value as any)}
                        className="ml-2 rounded-md border border-pink-200 bg-white px-3 py-1 text-sm shadow-sm text-center focus:outline-none focus:ring-2 focus:ring-pink-100 focus:border-pink-200"
                    >
                        <option value="all">All</option>
                        <option value="hour">Last Hour</option>
                        <option value="day">Today</option>
                        <option value="week">This Week</option>
                    </select>
                </label>

                <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={weatherData} margin={{ top: 8, right: 16, left: 8, bottom: 32 }}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis type="number" dataKey="t" domain={["dataMin", "dataMax"]} tickFormatter={tickFmt} />
                        <YAxis />
                        <Tooltip
                            labelFormatter={(t) => new Date(Number(t)).toLocaleString()}
                            content={({ active, payload, label }) => {
                                if (!active || !payload) return null;

                                // Custom order for tooltip display
                                const order = ["Sensor One North", "Sensor Two South", "Outside"];
                                const sorted = [...payload].sort((a, b) => {
                                    const ia = order.indexOf(a.name);
                                    const ib = order.indexOf(b.name);
                                    return ia - ib;
                                });

                                return (
                                    <div className="bg-white p-2 rounded shadow text-sm">
                                        <div className="font-medium mb-1">
                                            {new Date(Number(label)).toLocaleString()}
                                        </div>
                                        {sorted.map((p) => (
                                            <div key={p.dataKey} style={{ color: p.color }}>
                                                {p.name}: {typeof p.value === "number" ? p.value.toFixed(1) : p.value}
                                            </div>
                                        ))}
                                    </div>
                                );
                            }}
                        />
                        <Legend verticalAlign="bottom" align="center" wrapperStyle={{ paddingTop: 8, paddingBottom: 8 }} />

                        {/* Dynamic sensor lines */}
                        {sensorKeys.map((k, i) => (
                            <Line
                                key={k}
                                type="monotone"
                                dataKey={k}
                                name={k.replace(/temp_|_/g, " ").replace(/^\s+/, "")}
                                stroke={colors[i % colors.length]}
                                activeDot={{ r: 1 }}
                                connectNulls={true}
                            />
                        ))}

                        {/* Outside temperature line */}
                        <Line
                            type="monotone"
                            dataKey="tempOutside"
                            name="Outside"
                            stroke="#3fbf86"
                            activeDot={{ r: 1 }}
                            connectNulls={true}
                        />
                    </LineChart>
                </ResponsiveContainer>
            </div>

            {/* Sensor Tiles Section */}
            <div className="w-35 flex flex-col gap-4">
                {sensorKeys.map((k, i) => {
                    const v = lastVal(weatherData, k);
                    const name = k.replace(/temp_|_/g, " ").replace(/^\s+/, "");
                    return (
                        <div key={k} className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                            <div className="text-sm text-gray-500">{name}</div>
                            <div className="mt-1 text-3xl font-bold" style={{ color: colors[i % colors.length] }}>
                                {fmt(v)}
                            </div>
                        </div>
                    );
                })}

                <div className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                    <div className="text-sm text-gray-500">Outside</div>
                    <div className="mt-1 text-3xl font-bold text-[#3fbf86]">{fmt(vOut)}</div>
                </div>
            </div>
        </div>
    );
}