import { useEffect, useMemo, useState } from "react";
import {
    getAllTopics,
    createTopic,
    updateTopic,
    deleteTopic,
} from "../api/clients";
import { TopicSetting } from "../api/api-client.ts";

// Keep table state as a plain shape
type Row = {
    topicSettingId?: number;
    sensorName?: string;
    sensorLocation?: string;
    sensorType?: string;
    defaultTopicPath?: string;
    groupId?: number;
    hasRecovery?: boolean;
    _editing?: boolean;
};

export default function ManageTopics() {
    const [rows, setRows] = useState<Row[]>([]);
    const [busy, setBusy] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);

    // create form state
    const [sensorName, setSensorName] = useState("");
    const [sensorLocation, setSensorLocation] = useState("");
    const [sensorType, setSensorType] = useState("");
    const [defaultTopicPath, setDefaultTopicPath] = useState("");
    const [groupId, setGroupId] = useState<string>("");
    const [hasRecovery, setHasRecovery] = useState(false);

    const canCreate = useMemo(
        () => sensorName.trim() !== "" && sensorLocation.trim() !== "",
        [sensorName, sensorLocation]
    );

    async function load() {
        setBusy(true); setMsg(null);
        try {
            const list = await getAllTopics();
            // Map class instances → plain rows for editing
            const mapped: Row[] = (list ?? []).map((t: any) => ({
                topicSettingId: t.topicSettingId ?? t.id,
                sensorName: t.sensorName,
                sensorLocation: t.sensorLocation,
                sensorType: t.sensorType,
                defaultTopicPath: t.defaultTopicPath,
                groupId: t.groupId,
                hasRecovery: t.hasRecovery,
            }));
            setRows(mapped);
        } catch (e: any) {
            setMsg(e?.message || "Failed to load topics");
        } finally {
            setBusy(false);
        }
    }

    useEffect(() => { void load(); }, []);

    async function onCreate() {
        if (!canCreate) return;
        setBusy(true); setMsg(null);
        try {
            const payload = new TopicSetting();
            payload.sensorName = sensorName.trim();
            payload.sensorLocation = sensorLocation.trim();
            if (sensorType.trim()) payload.sensorType = sensorType.trim();
            if (defaultTopicPath.trim()) payload.defaultTopicPath = defaultTopicPath.trim();
            if (groupId) payload.groupId = Number(groupId);
            payload.hasRecovery = hasRecovery;

            await createTopic(payload);
            setSensorName(""); setSensorLocation(""); setSensorType("");
            setDefaultTopicPath(""); setGroupId(""); setHasRecovery(false);
            await load();
            setMsg("Topic created.");
        } catch (e: any) {
            setMsg(e?.message || "Failed to create topic");
        } finally {
            setBusy(false);
        }
    }

    function beginEdit(i: number) {
        setRows(prev => prev.map((r, idx) => (idx === i ? { ...r, _editing: true } : r)));
    }
    function cancelEdit(i: number) {
        setRows(prev => prev.map((r, idx) => (idx === i ? { ...r, _editing: false } : r)));
    }
    function patch(i: number, p: Partial<Row>) {
        setRows(prev => prev.map((r, idx) => (idx === i ? { ...r, ...p } : r)));
    }

    async function onSave(i: number) {
        const r = rows[i];
        if (!r.topicSettingId) { setMsg("Missing topicSettingId"); return; }
        setBusy(true); setMsg(null);
        try {
            const payload = new TopicSetting();
            payload.topicSettingId = r.topicSettingId;
            if (r.sensorName !== undefined) payload.sensorName = r.sensorName.trim();
            if (r.sensorLocation !== undefined) payload.sensorLocation = r.sensorLocation.trim();
            if (r.sensorType !== undefined) payload.sensorType = r.sensorType?.trim();
            if (r.defaultTopicPath !== undefined) payload.defaultTopicPath = r.defaultTopicPath?.trim();
            if (r.groupId !== undefined) payload.groupId = r.groupId;
            payload.hasRecovery = !!r.hasRecovery;

            await updateTopic(payload);
            await load();
            setMsg("Topic updated.");
        } catch (e: any) {
            setMsg(e?.message || "Failed to update topic");
        } finally {
            setBusy(false);
        }
    }

    async function onDelete(i: number) {
        const r = rows[i];
        if (!r.topicSettingId) { setMsg("Missing topicSettingId"); return; }
        if (!confirm(`Delete topic "${r.sensorName ?? ""}"?`)) return;
        setBusy(true); setMsg(null);
        try {
            const payload = new TopicSetting();
            payload.topicSettingId = r.topicSettingId;
            await deleteTopic(payload);
            await load();
            setMsg("Topic deleted.");
        } catch (e: any) {
            setMsg(e?.message || "Failed to delete topic");
        } finally {
            setBusy(false);
        }
    }

    return (
        <section style={{ marginTop: 24 }}>
            <h3 style={{ marginBottom: 8 }}>Manage MQTT Topics (admin)</h3>

            {/* Create */}
            <div style={{
                display: "grid",
                gridTemplateColumns: "1fr 1fr 1fr 1.5fr 0.6fr auto",
                gap: 8,
                alignItems: "center",
                maxWidth: 1200,
                marginBottom: 12
            }}>
                <input placeholder="Sensor name"
                       value={sensorName} onChange={e => setSensorName(e.target.value)}
                       disabled={busy} style={{ padding: 6 }} />
                <input placeholder="Location (North/South/...)"
                       value={sensorLocation} onChange={e => setSensorLocation(e.target.value)}
                       disabled={busy} style={{ padding: 6 }} />
                <input placeholder="Sensor type (optional)"
                       value={sensorType} onChange={e => setSensorType(e.target.value)}
                       disabled={busy} style={{ padding: 6 }} />
                <input placeholder="Default topic path (optional)"
                       value={defaultTopicPath} onChange={e => setDefaultTopicPath(e.target.value)}
                       disabled={busy} style={{ padding: 6 }} />
                <input placeholder="Group ID (optional)" inputMode="numeric"
                       value={groupId} onChange={e => setGroupId(e.target.value)}
                       disabled={busy} style={{ padding: 6 }} />
                <label style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                    <input type="checkbox" checked={hasRecovery}
                           onChange={e => setHasRecovery(e.target.checked)} disabled={busy} />
                    Recovery
                </label>
                <button onClick={onCreate} disabled={busy || !canCreate}>Add</button>
            </div>

            {/* List */}
            <div style={{ overflowX: "auto" }}>
                <table style={{ borderCollapse: "collapse", width: "100%", maxWidth: 1200 }}>
                    <thead>
                    <tr>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>ID</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Sensor</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Location</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Type</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Default Topic Path</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Group</th>
                        <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 6 }}>Recovery</th>
                        <th style={{ borderBottom: "1px solid #ddd", padding: 6 }} />
                    </tr>
                    </thead>
                    <tbody>
                    {rows.map((r, i) => (
                        <tr key={r.topicSettingId ?? i}>
                            <td style={{ padding: 6 }}>{r.topicSettingId ?? "—"}</td>

                            <td style={{ padding: 6 }}>
                                {r._editing ? (
                                    <input value={r.sensorName ?? ""} onChange={e => patch(i, { sensorName: e.target.value })}
                                           style={{ padding: 4, width: "100%" }} />
                                ) : (r.sensorName ?? "—")}
                            </td>

                            <td style={{ padding: 6 }}>
                                {r._editing ? (
                                    <input value={r.sensorLocation ?? ""} onChange={e => patch(i, { sensorLocation: e.target.value })}
                                           style={{ padding: 4, width: "100%" }} />
                                ) : (r.sensorLocation ?? "—")}
                            </td>

                            <td style={{ padding: 6 }}>
                                {r._editing ? (
                                    <input value={r.sensorType ?? ""} onChange={e => patch(i, { sensorType: e.target.value })}
                                           style={{ padding: 4, width: "100%" }} />
                                ) : (r.sensorType ?? "—")}
                            </td>

                            <td style={{ padding: 6 }}>
                                {r._editing ? (
                                    <input value={r.defaultTopicPath ?? ""} onChange={e => patch(i, { defaultTopicPath: e.target.value })}
                                           style={{ padding: 4, width: "100%" }} />
                                ) : (r.defaultTopicPath ?? "—")}
                            </td>

                            <td style={{ padding: 6, minWidth: 70 }}>
                                {r._editing ? (
                                    <input inputMode="numeric" value={r.groupId?.toString() ?? ""}
                                           onChange={e => patch(i, { groupId: e.target.value ? Number(e.target.value) : undefined })}
                                           style={{ padding: 4, width: "100%" }} />
                                ) : (r.groupId ?? "—")}
                            </td>

                            <td style={{ padding: 6 }}>
                                {r._editing ? (
                                    <input type="checkbox" checked={!!r.hasRecovery}
                                           onChange={e => patch(i, { hasRecovery: e.target.checked })} />
                                ) : (r.hasRecovery ? "Yes" : "No")}
                            </td>

                            <td style={{ padding: 6, whiteSpace: "nowrap" }}>
                                {!r._editing ? (
                                    <>
                                        <button onClick={() => beginEdit(i)} disabled={busy}>Edit</button>{" "}
                                        <button onClick={() => onDelete(i)} disabled={busy}>Delete</button>
                                    </>
                                ) : (
                                    <>
                                        <button onClick={() => onSave(i)} disabled={busy}>Save</button>{" "}
                                        <button onClick={() => cancelEdit(i)} disabled={busy}>Cancel</button>
                                    </>
                                )}
                            </td>
                        </tr>
                    ))}
                    {rows.length === 0 && (
                        <tr><td colSpan={8} style={{ padding: 8, opacity: 0.7 }}>No topics configured.</td></tr>
                    )}
                    </tbody>
                </table>
            </div>

            {msg && <p style={{ marginTop: 8, color: /fail|error/i.test(msg) ? "crimson" : "green" }}>{msg}</p>}
        </section>
    );
}
