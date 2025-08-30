import { apiBase } from "../utils/config";
import { getToken } from "../utils/tokenHelpers";
import {
    AuthenticationClient,
    TemperatureDataClient,
    TempClient,
    TopicClient,
    ApiException,
    TopicSetting,
} from "./api-client.ts";

// Read the base URL from config
const BASE = apiBase();

// fetch wrapper that injects the bearer token at CALL time.
function authFetch(input: RequestInfo, init: RequestInit = {}) {
    const token = getToken();
    init.headers = {
        ...(init.headers || {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        Accept: "application/json",
    };
    return window.fetch(input as any, init);
}

// Export ready-to-use, typed clients
export const authClient = new AuthenticationClient(BASE, { fetch: authFetch });
export const tempClient = new TemperatureDataClient(BASE, { fetch: authFetch });
export const locationClient = new TempClient(BASE, { fetch: authFetch });
export const topicClient = new TopicClient(BASE, { fetch: authFetch });

// Export helper functions for managing locations
// Store the mapping between display names and stored location names
let locationMapping: { [displayName: string]: string } = {};

export const fetchPostalLocations = async () => {
    try {
        const response = await locationClient.getAllPostalcodes();
        // Parse the blob response - the API returns JSON as a blob
        const text = await response.data.text();
        console.log('Raw API response:', text);
        
        const data = JSON.parse(text);
        console.log('Parsed data:', data);
        
        // Handle different possible response formats
        let rawLocations: any[] = [];
        
        if (Array.isArray(data)) {
            rawLocations = data;
        } else if (data && Array.isArray(data.locations)) {
            rawLocations = data.locations;
        } else if (data && Array.isArray(data.data)) {
            rawLocations = data.data;
        } else {
            console.warn('Unexpected API response format:', data);
            return [];
        }
        
        // Clear the existing mapping
        locationMapping = {};
        
        // Map the raw data to PostalLocation format
        return rawLocations.map((item: any) => {
            let postalCode: number;
            let displayLocationName: string;
            let storedLocationName: string;
            
            // Handle case where API returns objects with item1/item2 properties
            if (item.item1 !== undefined && item.item2 !== undefined) {
                postalCode = typeof item.item1 === 'number' ? item.item1 : parseInt(item.item1);
                storedLocationName = String(item.item2);
                displayLocationName = storedLocationName; // For now, use the same name for display
            }
            // Handle standard format
            else if (item.postalCode !== undefined && item.locationName !== undefined) {
                postalCode = typeof item.postalCode === 'number' ? item.postalCode : parseInt(item.postalCode);
                storedLocationName = String(item.locationName);
                displayLocationName = storedLocationName;
            }
            // Handle alternative property names
            else {
                postalCode = item.PostalCode || item.code || item.postal_code || item.zip || 0;
                storedLocationName = item.LocationName || item.name || item.location || item.city || 'Unknown Location';
                displayLocationName = storedLocationName;
            }
            
            // Store the mapping for later use in temperature API calls
            locationMapping[displayLocationName] = storedLocationName;
            
            return {
                postalCode,
                locationName: displayLocationName
            };
        }).filter(location => location.postalCode && location.locationName);
    } catch (error) {
        console.error('Error fetching postal locations:', error);
        return [];
    }
};

// Helper function to get the stored location name for temperature API calls
export const getStoredLocationName = (displayLocationName: string): string => {
    return locationMapping[displayLocationName] || displayLocationName;
};

export const addPostalLocation = async (postalCode: number) => {
    return await locationClient.insertLocation(postalCode);
};

export const removePostalLocation = async (postalCode: number) => {
    return await locationClient.removePostalcode(postalCode);
};

// Export topic management functions
export const getAllTopics = async () => {
    return await topicClient.getAllTopics();
};

export const createTopic = async (topicSetting: TopicSetting) => {
    return await topicClient.createTopic(topicSetting);
};

export const updateTopic = async (topicSetting: TopicSetting) => {
    return await topicClient.updateTopic(topicSetting);
};

export const deleteTopic = async (topicSetting: TopicSetting) => {
    return await topicClient.deleteTopic(topicSetting);
};

// Export types
export type PostalLocation = {
    postalCode: number;
    locationName: string;
};

// Re-export ApiException and types so callers can do instanceof checks if needed
export { ApiException, TopicSetting };
