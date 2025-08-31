import {useEffect, useMemo, useState} from "react";
import {createTopic, deleteTopic, getAllTopics, updateTopic, fetchPostalLocations, type PostalLocation} from "../api/clients";
import {TopicSetting, SensorType} from "../api/api-client";

// Keep table state as a plain shape
type Row = {
    topicSettingId?: number;
    sensorName?: string;
    sensorLocation?: string;
    sensorTypeEnum?: SensorType;
    defaultTopicPath?: string;
    groupId?: number;
    hasRecovery?: boolean;
    coordinateMappingId?: number;
    _editing?: boolean;
};

export default function ManageTopics() {
    const [rows, setRows] = useState<Row[]>([]);
    const [busy, setBusy] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);
    const [locations, setLocations] = useState<PostalLocation[]>([]);

    // create form state
    const [sensorName, setSensorName] = useState("");
    const [sensorLocation, setSensorLocation] = useState("");
    const [sensorTypeEnum, setSensorTypeEnum] = useState<SensorType>(SensorType.Temp);
    const [defaultTopicPath, setDefaultTopicPath] = useState("");
    const [groupId, setGroupId] = useState<string>("");
    const [hasRecovery, setHasRecovery] = useState(false);
    const [coordinateMappingId, setCoordinateMappingId] = useState<string>("");

    const canCreate = useMemo(
        () => sensorName.trim() !== "" && sensorLocation.trim() !== "",
        [sensorName, sensorLocation]
    );

    async function load() {
        setBusy(true);
        setMsg(null);
        try {
            const list = await getAllTopics();
            // Map class instances → plain rows for editing
            const mapped: Row[] = (list ?? []).map((t: any) => ({
                topicSettingId: t.topicSettingId ?? t.id,
                sensorName: t.sensorName,
                sensorLocation: t.sensorLocation,
                sensorTypeEnum: t.sensorTypeEnum,
                defaultTopicPath: t.defaultTopicPath,
                groupId: t.groupId,
                hasRecovery: t.hasRecovery,
                coordinateMappingId: t.coordinateMappingId,
            }));
            setRows(mapped);
        } catch (e: any) {
            setMsg(e?.message || "Failed to load topics");
        } finally {
            setBusy(false);
        }
    }

    useEffect(() => {
        void load();
        void loadLocations();
    }, []);

    async function loadLocations() {
        try {
            const locs = await fetchPostalLocations();
            setLocations(locs);
        } catch (error) {
            console.error('Error loading locations:', error);
        }
    }

    async function onCreate() {
        if (!canCreate) return;
        setBusy(true);
        setMsg(null);
        try {
            const payload = new TopicSetting();
            payload.sensorName = sensorName.trim();
            payload.sensorLocation = sensorLocation.trim();
            payload.sensorTypeEnum = sensorTypeEnum;
            if (defaultTopicPath.trim()) payload.defaultTopicPath = defaultTopicPath.trim();
            if (groupId) payload.groupId = Number(groupId);
            if (coordinateMappingId) payload.coordinateMappingId = Number(coordinateMappingId);
            payload.hasRecovery = hasRecovery;

            await createTopic(payload);
            setSensorName("");
            setSensorLocation("");
            setSensorTypeEnum(SensorType.Temp);
            setDefaultTopicPath("");
            setGroupId("");
            setCoordinateMappingId("");
            setHasRecovery(false);
            await load();
            setMsg("Topic created.");
        } catch (e: any) {
            setMsg(e?.message || "Failed to create topic");
        } finally {
            setBusy(false);
        }
    }

    function beginEdit(i: number) {
        setRows(prev => prev.map((r, idx) => (idx === i ? {...r, _editing: true} : r)));
    }

    function cancelEdit(i: number) {
        setRows(prev => prev.map((r, idx) => (idx === i ? {...r, _editing: false} : r)));
    }

    function patch(i: number, p: Partial<Row>) {
        setRows(prev => prev.map((r, idx) => (idx === i ? {...r, ...p} : r)));
    }

    async function onSave(i: number) {
        const r = rows[i];
        if (!r.topicSettingId) {
            setMsg("Missing topicSettingId");
            return;
        }
        setBusy(true);
        setMsg(null);
        try {
            const payload = new TopicSetting();
            payload.topicSettingId = r.topicSettingId;
            if (r.sensorName !== undefined) payload.sensorName = r.sensorName.trim();
            if (r.sensorLocation !== undefined) payload.sensorLocation = r.sensorLocation.trim();
            if (r.sensorTypeEnum !== undefined) payload.sensorTypeEnum = r.sensorTypeEnum;
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
        if (!r.topicSettingId) {
            setMsg("Missing topicSettingId");
            return;
        }
        if (!confirm(`Delete topic "${r.sensorName ?? ""}"?`)) return;
        setBusy(true);
        setMsg(null);
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
        <section className="mt-6">
            <h3 className="mb-2 text-lg font-semibold text-gray-800">Manage MQTT Topics (admin)</h3>

            {/* Create Form */}
            <div className="grid grid-cols-1 lg:grid-cols-6 gap-2 items-end max-w-full mb-3 p-4 bg-gray-50 rounded-lg">
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Sensor Name</label>
                    <input
                        placeholder="Sensor name"
                        value={sensorName}
                        onChange={e => setSensorName(e.target.value)}
                        disabled={busy}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Location</label>
                    <input
                        placeholder="Location (North/South/...)"
                        value={sensorLocation}
                        onChange={e => setSensorLocation(e.target.value)}
                        disabled={busy}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Sensor Type</label>
                        <select
                            value={sensorTypeEnum}
                            onChange={e => setSensorTypeEnum(Number(e.target.value) as SensorType)}
                            disabled={busy}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                        >
                            <option value={SensorType.Temp}>Temperature</option>
                            <option value={SensorType.Spl}>SPL</option>
                            <option value={SensorType.Hum}>Humidity</option>
                            <option value={SensorType.Ikea}>IKEA</option>
                            <option value={SensorType.Co2}>CO2</option>
                            <option value={SensorType.Mic}>Microphone</option>
                        </select>
                </div>
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Topic Path</label>
                    <input
                        placeholder="Default topic path (optional)"
                        value={defaultTopicPath}
                        onChange={e => setDefaultTopicPath(e.target.value)}
                        disabled={busy}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Group ID</label>
                    <input
                        placeholder="Group ID (optional)"
                        inputMode="numeric"
                        value={groupId}
                        onChange={e => setGroupId(e.target.value)}
                        disabled={busy}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Location</label>
                    <select
                        value={coordinateMappingId}
                        onChange={e => setCoordinateMappingId(e.target.value)}
                        disabled={busy}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                    >
                        <option value="">Select Location (optional)</option>
                        {locations.map((location) => (
                            <option key={location.postalCode} value={location.postalCode.toString()}>
                                {location.locationName}
                            </option>
                        ))}
                    </select>
                </div>
                <div className="flex items-center gap-2">
                    <label className="flex items-center gap-2 cursor-pointer">
                        <input
                            type="checkbox"
                            checked={hasRecovery}
                            onChange={e => setHasRecovery(e.target.checked)}
                            disabled={busy}
                            className="w-4 h-4 text-pink-600 bg-gray-100 border-gray-300 rounded focus:ring-pink-500 focus:ring-2"
                        />
                        <span className="text-xs font-medium text-gray-700">Recovery</span>
                    </label>
                    <button
                        onClick={onCreate}
                        disabled={busy || !canCreate}
                        className="px-4 py-2 bg-pink-600 text-white text-sm font-medium rounded-md hover:bg-pink-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                    >
                        Add
                    </button>
                </div>
            </div>

            {/* Table */}
            <div className="overflow-x-auto bg-white rounded-lg shadow border border-gray-200">
                <table className="min-w-full table-auto">
                    <thead className="bg-gray-50">
                    <tr>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">ID</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Sensor</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Location</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Type</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Default
                            Topic Path
                        </th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Group</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Coord Mapping</th>
                        <th className="px-3 py-2 text-left text-xs font-medium text-gray-700 uppercase tracking-wider border-b border-gray-200">Recovery</th>
                        <th className="px-3 py-2 border-b border-gray-200">Actions</th>
                    </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                    {rows.map((r, i) => (
                        <tr key={r.topicSettingId ?? i} className="hover:bg-gray-50">
                            <td className="px-3 py-2 text-sm text-gray-900">{r.topicSettingId ?? "—"}</td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        value={r.sensorName ?? ""}
                                        onChange={e => patch(i, {sensorName: e.target.value})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.sensorName ?? "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        value={r.sensorLocation ?? ""}
                                        onChange={e => patch(i, {sensorLocation: e.target.value})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.sensorLocation ?? "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <select
                                        value={r.sensorTypeEnum ?? SensorType.Temp}
                                        onChange={e => patch(i, {sensorTypeEnum: Number(e.target.value) as SensorType})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    >
                                        <option value={SensorType.Temp}>Temperature</option>
                                        <option value={SensorType.Spl}>SPL</option>
                                        <option value={SensorType.Hum}>Humidity</option>
                                        <option value={SensorType.Ikea}>IKEA</option>
                                        <option value={SensorType.Co2}>CO2</option>
                                        <option value={SensorType.Mic}>Microphone</option>
                                    </select>
                                ) : (
                                    <span className="text-sm text-gray-900">{r.sensorTypeEnum !== undefined ? SensorType[r.sensorTypeEnum] : "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        value={r.defaultTopicPath ?? ""}
                                        onChange={e => patch(i, {defaultTopicPath: e.target.value})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.defaultTopicPath ?? "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        inputMode="numeric"
                                        value={r.groupId?.toString() ?? ""}
                                        onChange={e => patch(i, {groupId: e.target.value ? Number(e.target.value) : undefined})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.groupId ?? "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        inputMode="numeric"
                                        value={r.coordinateMappingId?.toString() ?? ""}
                                        onChange={e => patch(i, {coordinateMappingId: e.target.value ? Number(e.target.value) : undefined})}
                                        className="w-full px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-pink-300"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.coordinateMappingId ?? "—"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2">
                                {r._editing ? (
                                    <input
                                        type="checkbox"
                                        checked={!!r.hasRecovery}
                                        onChange={e => patch(i, {hasRecovery: e.target.checked})}
                                        className="w-4 h-4 text-pink-600 bg-gray-100 border-gray-300 rounded focus:ring-pink-500 focus:ring-2"
                                    />
                                ) : (
                                    <span className="text-sm text-gray-900">{r.hasRecovery ? "Yes" : "No"}</span>
                                )}
                            </td>

                            <td className="px-3 py-2 whitespace-nowrap">
                                {!r._editing ? (
                                    <div className="flex gap-2">
                                        <button
                                            onClick={() => beginEdit(i)}
                                            disabled={busy}
                                            className="px-3 py-1 bg-blue-600 text-white text-xs font-medium rounded hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                                        >
                                            Edit
                                        </button>
                                        <button
                                            onClick={() => onDelete(i)}
                                            disabled={busy}
                                            className="px-3 py-1 bg-red-600 text-white text-xs font-medium rounded hover:bg-red-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                                        >
                                            Delete
                                        </button>
                                    </div>
                                ) : (
                                    <div className="flex gap-2">
                                        <button
                                            onClick={() => onSave(i)}
                                            disabled={busy}
                                            className="px-3 py-1 bg-green-600 text-white text-xs font-medium rounded hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                                        >
                                            Save
                                        </button>
                                        <button
                                            onClick={() => cancelEdit(i)}
                                            disabled={busy}
                                            className="px-3 py-1 bg-gray-600 text-white text-xs font-medium rounded hover:bg-gray-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                )}
                            </td>
                        </tr>
                    ))}
                    {rows.length === 0 && (
                        <tr>
                            <td colSpan={9} className="px-3 py-4 text-center text-sm text-gray-500">
                                No topics configured.
                            </td>
                        </tr>
                    )}
                    </tbody>
                </table>
            </div>

            {msg && (
                <p className={`mt-3 text-sm font-medium ${/fail|error/i.test(msg) ? "text-red-600" : "text-green-600"}`}>
                    {msg}
                </p>
            )}
        </section>
    );
}