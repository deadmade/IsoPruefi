/**
 * @fileoverview Temperature data visualization component with interactive charts and real-time displays.
 * Displays temperature data from multiple sensors and external weather sources in a responsive chart format.
 */

import {useEffect, useState} from "react";
import {CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis} from "recharts";
import {ApiException, getStoredLocationName, tempClient} from "../api/clients";

/**
 * Represents a single temperature data entry with timestamp and sensor readings.
 * Dynamic keys allow for flexible sensor data storage.
 */
export type WeatherEntry = {
    /** ISO timestamp string for the data point */
    timestamp: string;
    /** Dynamic keys for different sensors (e.g., "temp_Sensor1_Location") */
    [key: string]: number | string;
    /** External weather temperature for the location */
    tempOutside: number;
};

/**
 * Props for the TempChart component.
 */
type TempChartProps = {
    /** Location name for weather data retrieval */
    place?: string;
    /** Whether to display temperatures in Fahrenheit (default: Celsius) */
    isFahrenheit?: boolean;
};

/**
 * Temperature chart component that displays real-time temperature data from multiple sensors.
 *
 * Features:
 * - Interactive line chart with time filtering
 * - Multiple sensor data visualization
 * - Real-time temperature tiles
 * - Celsius/Fahrenheit unit conversion
 * - Responsive design with error handling
 *
 * @param props - Component configuration
 * @returns JSX element containing the temperature dashboard
 *
 * @example
 * ```tsx
 * // Basic usage with default location
 * <TempChart />
 *
 * // With specific location and Fahrenheit units
 * <TempChart place="Berlin" isFahrenheit={true} />
 * ```
 */
export function TempChart({place = "Heidenheim an der Brenz", isFahrenheit = false}: TempChartProps = {}) {
    const [weatherData, setWeatherData] = useState<WeatherEntry[]>([]);
    const [filter, setFilter] = useState<"all" | "hour" | "day" | "week">("all");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [sensorKeys, setSensorKeys] = useState<string[]>([]);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            setError(null);

            const end = new Date();
            const start = new Date(end.getTime() - 24 * 60 * 60 * 1000);

            try {
                // Convert the display location name to the stored location name for the API call
                const storedLocationName = getStoredLocationName(place);
                console.log('Display location:', place, 'Stored location:', storedLocationName);

                const data = await tempClient.getTemperature(start, end, storedLocationName, isFahrenheit);
                console.log('Temperature API Response:', data);
                console.log('Place parameter (stored):', storedLocationName);
                console.log('Start:', start, 'End:', end);

                if (!data) {
                    setError("No data received from server");
                    return;
                }

                const sensorData = data.sensorData || [];
                const outside = data.temperatureOutside || [];

                console.log('Sensor data:', sensorData.length, 'sensors');
                console.log('Outside data length:', outside.length);
                console.log('Outside data sample:', outside.slice(0, 3));

                // Group sensor data by timestamp to create unified data points
                const timestampMap = new Map<string, any>();
                const discoveredSensorKeys = new Set<string>();

                // Process sensor data
                sensorData.forEach((sensor: any) => {
                    const sensorName = sensor.sensorName || 'Unknown';
                    const location = sensor.location || '';
                    const tempData = sensor.temperatureDatas || [];
                    const sensorKey = `temp_${sensorName}_${location}`.replace(/\s+/g, '_');
                    discoveredSensorKeys.add(sensorKey);

                    tempData.forEach((temp: any) => {
                        const timestamp = temp.timestamp;
                        const isoTimestamp = timestamp ?
                            (timestamp instanceof Date ? timestamp.toISOString() : new Date(timestamp).toISOString()) : '';

                        if (!timestampMap.has(isoTimestamp)) {
                            timestampMap.set(isoTimestamp, {timestamp: isoTimestamp, tempOutside: 0});
                        }

                        const entry = timestampMap.get(isoTimestamp);
                        entry[sensorKey] = temp.temperature || 0;
                    });
                });

                // Process outside data
                outside.forEach((temp: any) => {
                    const timestamp = temp.timestamp;
                    const isoTimestamp = timestamp ?
                        (timestamp instanceof Date ? timestamp.toISOString() : new Date(timestamp).toISOString()) : '';

                    if (!timestampMap.has(isoTimestamp)) {
                        timestampMap.set(isoTimestamp, {timestamp: isoTimestamp, tempOutside: 0});
                    }

                    const entry = timestampMap.get(isoTimestamp);
                    entry.tempOutside = temp.temperature || 0;
                });

                const formatted: WeatherEntry[] = Array.from(timestampMap.values())
                    .filter(entry => entry.timestamp)
                    .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());

                setWeatherData(formatted);
                setSensorKeys(Array.from(discoveredSensorKeys));
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
    }, [place, isFahrenheit]);

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

    const lastVal = (arr: WeatherEntry[], key: string) => {
        for (let i = arr.length - 1; i >= 0; i--) {
            const v = arr[i][key];
            if (typeof v === "number" && !Number.isNaN(v)) return v;
        }
        return null;
    };

    const convertTemp = (temp: number) => {
        if (isFahrenheit) {
            return (temp * 9 / 5) + 32;
        }
        return temp;
    };

    const vOut = lastVal(weatherData, "tempOutside");
    const fmt = (v: number | null) => (v == null ? "—" : `${convertTemp(v || 0).toFixed(1)}°${isFahrenheit ? 'F' : 'C'}`);

    // Generate colors for sensor lines
    const colors = ["#d3546c", "#84d8d2", "#82ca9d", "#ffc658", "#ff7c7c", "#8884d8"];

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

                        {/* Dynamic sensor lines */}
                        {sensorKeys.map((sensorKey, index) => (
                            <Line
                                key={sensorKey}
                                type="monotone"
                                dataKey={sensorKey}
                                name={sensorKey.replace(/temp_|_/g, ' ').replace(/^\s+/, '')}
                                stroke={colors[index % colors.length]}
                                activeDot={{r: 1}}
                            />
                        ))}

                        {/* Outside temperature line */}
                        <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#3fbf86" activeDot={{r: 1}}/>
                    </LineChart>
                </ResponsiveContainer>
            </div>

            {/* RIGHT: tiles */}
            <div className="w-64 flex flex-col gap-4">
                {/* Dynamic sensor tiles */}
                {sensorKeys.map((sensorKey, index) => {
                    const value = lastVal(weatherData, sensorKey);
                    const displayName = sensorKey.replace(/temp_|_/g, ' ').replace(/^\s+/, '');
                    return (
                        <div key={sensorKey} className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                            <div className="text-sm text-gray-500">{displayName}</div>
                            <div className="mt-1 text-3xl font-bold"
                                 style={{color: colors[index % colors.length]}}>{fmt(value)}</div>
                        </div>
                    );
                })}

                {/* Outside tile */}
                <div className="rounded-xl bg-white shadow px-5 py-4 border border-gray-300">
                    <div className="text-sm text-gray-500">Outside</div>
                    <div className="mt-1 text-3xl font-bold text-[#3fbf86]">{fmt(vOut)}</div>
                </div>
            </div>
        </div>
    );
}