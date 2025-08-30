import { useState, useEffect } from "react";
import { addPostalLocation, removePostalLocation, fetchPostalLocations, type PostalLocation } from "../api/clients";

type Props = { onChanged?: () => void };

export default function ManageLocations({ onChanged }: Props) {
    const [postal, setPostal] = useState<string>("");
    const [busy, setBusy] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);
    const [locations, setLocations] = useState<PostalLocation[]>([]);
    const [loadingLocations, setLoadingLocations] = useState(true);

    const loadLocations = async () => {
        try {
            setLoadingLocations(true);
            const data = await fetchPostalLocations();
            setLocations(data || []);
        } catch (error) {
            console.error("Failed to load locations:", error);
            setLocations([]);
        } finally {
            setLoadingLocations(false);
        }
    };

    useEffect(() => {
        loadLocations();
    }, []);

    const parseCode = () => {
        const n = Number(postal.trim());
        if (!Number.isInteger(n) || n <= 0) throw new Error("Enter a valid postal code (integer).");
        return n;
    };

    const handleAdd = async () => {
        try {
            setBusy(true);
            setMsg(null);
            const code = parseCode();
            await addPostalLocation(code);
            setMsg(`Location ${code} added successfully.`);
            setPostal(""); // clear input after successful add
            onChanged?.(); // ask parent to refresh the drop-down
            await loadLocations(); // refresh our local locations list
        } catch (e) {
            setMsg(e instanceof Error ? e.message : "Unexpected error");
        } finally {
            setBusy(false);
        }
    };

    const handleRemove = async (postalCode: number) => {
        if (!confirm(`Remove location with postal code ${postalCode}?`)) return;
        
        try {
            setBusy(true);
            setMsg(null);
            await removePostalLocation(postalCode);
            setMsg(`Location ${postalCode} removed successfully.`);
            onChanged?.(); // ask parent to refresh the drop-down
            await loadLocations(); // refresh our local locations list
        } catch (e) {
            setMsg(e instanceof Error ? e.message : "Unexpected error");
        } finally {
            setBusy(false);
        }
    };

    return (
        <div>
            <h3 className="text-lg font-semibold text-gray-800 mb-6">Manage Locations</h3>
            
            {/* Add Location Section */}
            <div className="mb-6 p-4 bg-gray-50 rounded-lg border">
                <h4 className="text-sm font-semibold text-gray-700 mb-3">Add New Location</h4>
                <div className="flex flex-col sm:flex-row gap-3">
                    <div className="flex-1">
                        <input
                            type="text"
                            inputMode="numeric"
                            value={postal}
                            onChange={(e) => setPostal(e.target.value)}
                            placeholder="Enter postal code (e.g. 89522)"
                            className="w-full px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-pink-300 focus:border-pink-300 disabled:bg-gray-100"
                            disabled={busy}
                        />
                    </div>
                    <button 
                        disabled={busy || !postal.trim()} 
                        onClick={handleAdd}
                        className="px-6 py-2 bg-pink-600 text-white text-sm font-medium rounded-lg hover:bg-pink-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
                    >
                        {busy ? "Adding..." : "Add Location"}
                    </button>
                </div>
                {msg && (
                    <div className={`mt-3 text-sm font-medium ${/error|500|invalid/i.test(msg) ? "text-red-600" : "text-green-600"}`}>
                        {msg}
                    </div>
                )}
            </div>
            
            {/* Locations Overview */}
            <div>
                <h4 className="text-sm font-semibold text-gray-700 mb-3">Available Locations ({locations.length})</h4>
                {loadingLocations ? (
                    <div className="flex items-center justify-center py-8 text-gray-500">
                        <svg className="animate-spin -ml-1 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        Loading locations...
                    </div>
                ) : locations.length > 0 ? (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        {locations.map((location) => (
                            <div 
                                key={`${location.postalCode}-${location.locationName}`}
                                className="flex items-center justify-between p-3 bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow"
                            >
                                <div className="flex-1">
                                    <span className="font-semibold text-gray-900 text-sm">{location.postalCode}</span>
                                    <span className="text-gray-600 text-sm ml-2">{location.locationName}</span>
                                </div>
                                <button
                                    onClick={() => handleRemove(location.postalCode)}
                                    disabled={busy}
                                    className="ml-3 p-1.5 text-red-600 hover:bg-red-50 rounded-md disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                                    title={`Remove ${location.locationName}`}
                                >
                                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                                    </svg>
                                </button>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-8 text-gray-500">
                        <svg className="mx-auto h-12 w-12 text-gray-400 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                        </svg>
                        <p className="text-sm font-medium">No locations available</p>
                        <p className="text-xs text-gray-400 mt-1">Add your first location using the form above</p>
                    </div>
                )}
            </div>
        </div>
    );
}
