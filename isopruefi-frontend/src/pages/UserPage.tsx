import {useEffect, useState} from "react";
import { TempChart } from "../components/Weather";
import { PlacePicker } from "../components/PlacePicker";
import { UnitToggle } from "../components/UnitToggle";
import { useNavigate } from "react-router-dom";
import { clearToken } from "../utils/tokenHelpers";
import {
    fetchSensorsNormalized,
    fetchRecentStatus,
    type SensorMeta,
} from "../api/clients";

function SensorsTable({
                          place,
                          isFahrenheit,
                      }: {
    place: string;
    isFahrenheit: boolean;
}) {
    const [rows, setRows] = useState<SensorMeta[]>([]);
    const [status, setStatus] = useState<{north: {online: boolean; lastSeenMs?: number}; south: {online: boolean; lastSeenMs?: number}}>({ north: { online: false }, south: { online: false } });
    const [loading, setLoading] = useState(true);
    const [err, setErr] = useState<string | null>(null);

    // load sensors (topics)
    useEffect(() => {
        let alive = true;
        (async () => {
            try {
                const sensors = await fetchSensorsNormalized();
                if (!alive) return;
                setRows(sensors);
            } catch (e: any) {
                if (!alive) return;
                setErr(e?.message ?? "Failed to load sensors");
            } finally {
                if (!alive) return;
                setLoading(false);
            }
        })();
        return () => { alive = false; };
    }, []);

    // refresh status on mount, place change, and every 60s
    useEffect(() => {
        let alive = true;
        let timer: any;

        const load = async () => {
            try {
                const st = await fetchRecentStatus(place, isFahrenheit, 15);
                if (!alive) return;
                setStatus(st);
            } catch (e) {
                // keep previous status; optionally surface an inline warning
            }
        };

        load();
        timer = setInterval(load, 60_000);

        return () => { alive = false; clearInterval(timer); };
    }, [place, isFahrenheit]);

    // react to admin edits (broadcast/localStorage)
    useEffect(() => {
        const onStorage = (ev: StorageEvent) => {
            if (ev.key === "topicsVersion") {
                fetchSensorsNormalized().then(setRows).catch(() => {});
            }
        };
        window.addEventListener("storage", onStorage);

        let bc: BroadcastChannel | undefined;
        if ("BroadcastChannel" in window) {
            bc = new BroadcastChannel("topics");
            bc.onmessage = (msg) => {
                if (msg?.data?.type === "topics-changed") {
                    fetchSensorsNormalized().then(setRows).catch(() => {});
                }
            };
        }
        return () => {
            window.removeEventListener("storage", onStorage);
            bc?.close?.();
        };
    }, []);

    const fmtTime = (ms?: number) =>
        ms ? new Date(ms).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "—";

    const statusFor = (t: "north" | "south" | "other") =>
        t === "north" ? status.north : t === "south" ? status.south : { online: false };

    return (
        <section style={{ marginTop: 24 }}>
            <h2>Sensors</h2>
            {loading ? (
                <div>Loading sensors…</div>
            ) : err ? (
                <div style={{ color: "red" }}>{err}</div>
            ) : rows.length === 0 ? (
                <div>No sensors configured.</div>
            ) : (
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                    <thead>
                    <tr>
                        <th style={{ textAlign: "left", padding: "6px 8px" }}>Name</th>
                        <th style={{ textAlign: "left", padding: "6px 8px" }}>Location</th>
                        <th style={{ textAlign: "left", padding: "6px 8px" }}>Status</th>
                        <th style={{ textAlign: "left", padding: "6px 8px" }}>Last reading</th>
                    </tr>
                    </thead>
                    <tbody>
                    {rows.map((r) => {
                        const st = statusFor(r.type);
                        return (
                            <tr key={r.id}>
                                <td style={{ padding: "6px 8px", borderTop: "1px solid #eee" }}>{r.name}</td>
                                <td style={{ padding: "6px 8px", borderTop: "1px solid #eee" }}>{r.location}</td>
                                <td style={{ padding: "6px 8px", borderTop: "1px solid #eee" }}>
                    <span
                        style={{
                            display: "inline-block",
                            padding: "2px 8px",
                            borderRadius: 12,
                            background: r.type === "other" ? "#f0f0f0" : st.online ? "#e6f6ec" : "#f3f3f3",
                            border: "1px solid " + (r.type === "other" ? "#ddd" : st.online ? "#8fd19e" : "#ddd"),
                        }}
                        title={r.type === "other" ? "No stream" : st.online ? "Online" : "Offline"}
                    >
                      {r.type === "other" ? "N/A" : st.online ? "Online" : "Offline"}
                    </span>
                                </td>
                                <td style={{ padding: "6px 8px", borderTop: "1px solid #eee" }}>
                                    {r.type === "other" ? "—" : fmtTime(statusFor(r.type).lastSeenMs)}
                                </td>
                            </tr>
                        );
                    })}
                    </tbody>
                </table>
            )}
        </section>
    );
}

export default function UserPage() {
  const navigate = useNavigate();

  const [place, setPlace] = useState("Heidenheim an der Brenz");
  const [isF, setIsF] = useState(false);

  const handleLogout = () => {
    clearToken();
    navigate("/signin");
  };

  return (
    <div /*className="h-full w-full bg-[#f5cacd] p-6"*/>
      <h1 /*className="text-4xl font-extrabold text-[#d3546c] mb-8 text-center"*/>
        User Page
      </h1>

      <div style={{ display: "flex", gap: 24, alignItems: "center" }}>
        <div>
          <div style={{ fontWeight: 600 }}>Place</div>
          <PlacePicker value={place} onChange={setPlace} />
          <small style={{ marginLeft: 8, opacity: 0.7 }}>Pick the location</small>
        </div>

        <div>
          <div style={{ fontWeight: 600 }}>Units</div>
          <UnitToggle value={isF} onChange={setIsF} />
        </div>
      </div>

      <section /*className="bg-white rounded-xl shadow p-6 max-w-[1200px] mx-auto mt-4"*/>
        <h2 /*className="text-2xl font-bold text-gray-800 mb-4 text-center"*/>
          Weather Chart
        </h2>
        <TempChart place={place} isFahrenheit={isF} />
      </section>
        
        <br/><br/>
        
        <SensorsTable place={place} isFahrenheit={isF} />
    
        <br/><br/>
      <div className="flex justify-end">
        <button
          onClick={handleLogout}
          /*className="mt-6 px-6 py-2 rounded-lg bg-pink-600 text-white font-semibold hover:bg-pink-800"*/
        >
          Logout
        </button>
      </div>
    </div>
  );
}
