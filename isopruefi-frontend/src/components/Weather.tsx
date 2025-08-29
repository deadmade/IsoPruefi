import { useEffect, useState } from "react";
import {
    LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from "recharts";
import { tempClient, ApiException } from "../api/clients";

type Row = {
    ts: number;                // epoch ms (numeric X axis)
    tempSouth?: number;
    tempNorth?: number;
    tempOutside?: number;
};

type Props = {
    place?: string;
    isFahrenheit?: boolean;
};

const style = { width: "100%", height: 400 };

export function TempChart({
                              place = "Heidenheim an der Brenz",
                              isFahrenheit = false,
                          }: Props) {
    const [data, setData] = useState<Row[]>([]);
    const [filter, setFilter] = useState<"all" | "hour" | "day" | "week">("all");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        const end = new Date();
        const rangeMs =
            filter === "hour" ? 1 * 60 * 60 * 1000 :
                filter === "day"  ? 24 * 60 * 60 * 1000 :
                    filter === "week" ? 7  * 24 * 60 * 60 * 1000 :
                        30 * 24 * 60 * 60 * 1000; // all = last 30 days
        const start = new Date(end.getTime() - rangeMs);

        const toMs = (v: unknown): number | null => {
            if (!v) return null;
            const t = new Date(v as any).getTime();
            return Number.isNaN(t) ? null : t;
        };

        const indexByTs = (
            arr: Array<{ timestamp?: any; temperature?: number }> | undefined,
        ) => {
            const m = new Map<number, number>();
            for (const it of arr ?? []) {
                const ts = toMs(it?.timestamp);
                const temp = it?.temperature;
                if (ts != null && typeof temp === "number" && !Number.isNaN(temp)) {
                    m.set(ts, temp);
                }
            }
            return m;
        };

        const decimate = (rows: Row[], max: number) => {
            if (rows.length <= max) return rows;
            const step = Math.ceil(rows.length / max);
            const out: Row[] = [];
            for (let i = 0; i < rows.length; i += step) out.push(rows[i]);
            // include the very last point
            const last = rows[rows.length - 1];
            if (out[out.length - 1]?.ts !== last.ts) out.push(last);
            return out;
        };

        (async () => {
            setLoading(true);
            setError(null);
            try {
                const res = await tempClient.getTemperature(start, end, place, isFahrenheit);
                if (cancelled) return;

                const southM   = indexByTs(res?.temperatureSouth);
                const northM   = indexByTs(res?.temperatureNord);
                const outsideM = indexByTs(res?.temperatureOutside);

                const keys = new Set<number>();
                southM.forEach((_, k) => keys.add(k));
                northM.forEach((_, k) => keys.add(k));
                outsideM.forEach((_, k) => keys.add(k));

                let merged: Row[] = Array.from(keys)
                    .sort((a, b) => a - b)
                    .map(ts => ({
                        ts,
                        tempSouth:   southM.get(ts),
                        tempNorth:   northM.get(ts),
                        tempOutside: outsideM.get(ts),
                    }));

                // Downsample only for large ranges to keep rendering smooth
                const MAX_POINTS =
                    filter === "week" ? 2500 :
                        filter === "all"  ? 3000 : 4000;
                merged = decimate(merged, MAX_POINTS);

                setData(merged);
            } catch (err) {
                if (cancelled) return;
                if (ApiException.isApiException(err)) setError(`API Error (${err.status}): ${err.message}`);
                else setError(err instanceof Error ? err.message : "Unexpected error");
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => { cancelled = true; };
    }, [place, isFahrenheit, filter]);

    if (loading) return <div style={style}>Loading temperature data…</div>;
    if (error) {
        return (
            <div style={style}>
                <p style={{ color: "red" }}>Error loading temperature data: {error}</p>
                <button onClick={() => location.reload()}>Retry</button>
            </div>
        );
    }

    const tickFmt = (v: number) => {
        const d = new Date(v);
        return d.toLocaleString([], {
            month: "short", day: "2-digit",
            hour: "2-digit", minute: "2-digit"
        });
    };

    if (!data.length) {
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
                <div style={{opacity:.7}}>No data for this range.</div>
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
                <LineChart data={data}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis
                        type="number"
                        dataKey="ts"
                        domain={["dataMin", "dataMax"]}
                        scale="time"
                        tickFormatter={tickFmt}
                    />
                    <YAxis />
                    <Tooltip labelFormatter={(v) => new Date(v as number).toLocaleString()} />
                    <Legend />
                    <Line type="monotone" dataKey="tempSouth"   name="South"   stroke="#8884d8" activeDot={{ r: 5 }} connectNulls />
                    <Line type="monotone" dataKey="tempNorth"   name="North"   stroke="#84d8d2" activeDot={{ r: 5 }} connectNulls />
                    <Line type="monotone" dataKey="tempOutside" name="Outside" stroke="#82ca9d" activeDot={{ r: 4 }} connectNulls />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
}
