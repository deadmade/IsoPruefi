import { useEffect, useState } from "react";
import {
    LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from "recharts";
import { tempClient, ApiException } from "../api/clients";

type Row = {
    ts: number;
    tempSouth?: number;
    tempNorth?: number;
    tempOutside?: number;
};

type Props = {
    place?: string;
    isFahrenheit?: boolean;
};

const style = { width: "100%", height: 400 };

const ALL_DAYS = 30;

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

        const now = new Date();
        let start = new Date(now);
        let end = new Date(now);
        let bucketMs: number;
        
        if (filter === "hour") {
            bucketMs = 60000;
            start = new Date(end.getTime() - 60 * 60 * 1000);
        } else if (filter === "day") {
            bucketMs = 60 * 60 * 1000; // 1 hour
            start = new Date(end);
            start.setHours(0, 0, 0, 0); // local midnight
        } else if (filter === "week") {
            bucketMs = 12 * 60 * 60 * 1000; // 12 hours
            start = new Date(end.getTime() - 7 * 24 * 60 * 60 * 1000);
            const hours = start.getHours();
            start.setHours(hours < 12 ? 0: 12, 0, 0, 0); // start to 12h boundary for stable 14 buckets
        } else {
            bucketMs = 24 * 60 * 60 * 1000; // 1 day
            start = new Date (end.getTime() - ALL_DAYS * 24 * 60 * 60 * 1000);
            start.setHours(0, 0, 0, 0);
            end.setHours(23, 59, 59, 999);
        }
        
        const toMs = (v: unknown): number | null => {
            if (!v) return null;
            const t = new Date(v as any).getTime();
            return Number.isNaN(t) ? null : t;
        }
        
        type TD = {timestamp?: any; temperature?: number};

        // Average readings into buckets anchored at "start"
        const bucketSeries = (arr?: TD[]) => {
            const sums = new Map<number, {s: number; n: number }>();
            const startMs = start.getTime();
            for (const it of arr ?? []) {
                const ts = toMs(it?.timestamp);
                const v = it?.temperature;
                if(ts == null || typeof v!== "number" || Number.isNaN(v)) continue;
                const b = startMs + Math.floor((ts - startMs) / bucketMs) * bucketMs;
                const cur = sums.get(b) ?? {s: 0, n: 0};
                cur.s += v; cur.n += 1;
                sums.set(b, cur);
            }
            const out = new Map<number, number>();
            for (const [b, {s, n}] of sums) out.set(b, s/n);
            return out;
        };
        
        const buildTimeline = () => {
            const t: number[] = [];
            const startMs = start.getTime();
            const endMs = end.getTime();
            for (let ms = startMs; ms <= endMs; ms += bucketMs) t.push(ms);
            if (t.length === 0 || t[t.length - 1] < endMs) t.push(startMs + 
                Math.floor((endMs - startMs) / bucketMs) * bucketMs); 
            return t;
        }

        (async () => {
            setLoading(true);
            setError(null);
            try {
                const res = await tempClient.getTemperature(start, end, place, isFahrenheit);
                if (cancelled) return;

                const southM   = bucketSeries(res?.temperatureSouth);
                const northM   = bucketSeries(res?.temperatureNord);
                const outsideM = bucketSeries(res?.temperatureOutside);

                const timeline = buildTimeline();

                const merged: Row[] = timeline.map(ts => ({
                    ts,
                    tempSouth:   southM.get(ts),
                    tempNorth:   northM.get(ts),
                    tempOutside: outsideM.get(ts),
                }));

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

    const tickFmt = (v: number) => {
        const d = new Date(v);
        if (filter === "hour") return d.toLocaleTimeString([], { minute: "2-digit", second: "2-digit" });
        if (filter === "day")  return d.toLocaleTimeString([], { hour: "2-digit" });
        if (filter === "week") return d.toLocaleString([], { month: "short", day: "2-digit", hour: "2-digit" });
        return d.toLocaleDateString([], { month: "short", day: "2-digit" }); // all
    };
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
