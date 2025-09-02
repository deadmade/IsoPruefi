/**
 * @fileoverview API client configuration and authentication-wrapped client instances.
 * Provides pre-configured client instances with JWT token authentication for all API endpoints.
 */

import {apiBase} from "../utils/config";
import {getToken} from "../utils/tokenHelpers";
import {
    ApiException,
    AuthenticationClient,
    LocationClient,
    TemperatureDataClient,
    TopicClient,
    TopicSetting,
} from "./api-client.ts";

/**
 * Base URL for all API requests, retrieved from configuration.
 */
const BASE = apiBase();

/**
 * Fetch wrapper that automatically injects JWT bearer token for authenticated requests.
 * 
 * @param input - The request URL or Request object
 * @param init - Optional request configuration
 * @returns Promise resolving to the fetch response
 */
function authFetch(input: RequestInfo, init: RequestInit = {}) {
    const token = getToken();
    init.headers = {
        ...(init.headers || {}),
        ...(token ? {Authorization: `Bearer ${token}`} : {}),
        Accept: "application/json",
    };
    return window.fetch(input as any, init);
}

/**
 * Pre-configured authentication client with automatic JWT token handling.
 */
export const authClient = new AuthenticationClient(BASE, {fetch: authFetch});

/**
 * Pre-configured temperature data client with automatic JWT token handling.
 */
export const tempClient = new TemperatureDataClient(BASE, {fetch: authFetch});

/**
 * Pre-configured location client with automatic JWT token handling.
 */
export const locationClient = new LocationClient(BASE, {fetch: authFetch});

/**
 * Pre-configured MQTT topic client with automatic JWT token handling.
 */
export const topicClient = new TopicClient(BASE, {fetch: authFetch});

/**
 * Internal mapping between display location names and stored location names in the backend.
 */
let locationMapping: { [displayName: string]: string } = {};

/**
 * Fetches all postal code locations from the API and normalizes the response format.
 * Handles multiple possible response formats from the backend API.
 * 
 * @returns Promise resolving to an array of PostalLocation objects
 * @throws {ApiException} When API request fails
 */
export const fetchPostalLocations = async (): Promise<PostalLocation[]> => {
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

/**
 * Retrieves the backend-stored location name for a display location name.
 * Used internally to map user-friendly display names to backend identifiers.
 * 
 * @param displayLocationName - The display name shown to users
 * @returns The corresponding stored location name for API calls
 */
export const getStoredLocationName = (displayLocationName: string): string => {
    return locationMapping[displayLocationName] || displayLocationName;
};

/**
 * Adds a new postal code location to the system.
 * 
 * @param postalCode - The postal code to add
 * @returns Promise resolving to the API response
 * @throws {ApiException} When the request fails or postal code already exists
 */
export const addPostalLocation = async (postalCode: number) => {
    return await locationClient.insertLocation(postalCode);
};

/**
 * Removes a postal code location from the system.
 * 
 * @param postalCode - The postal code to remove
 * @returns Promise resolving to void on successful removal
 * @throws {ApiException} When the request fails or postal code doesn't exist
 */
export const removePostalLocation = async (postalCode: number) => {
    return await locationClient.removePostalcode(postalCode);
};

/**
 * Retrieves all MQTT topic settings from the system.
 * 
 * @returns Promise resolving to an array of TopicSetting objects
 * @throws {ApiException} When the request fails or access is denied
 */
export const getAllTopics = async (): Promise<TopicSetting[]> => {
    return await topicClient.getAllTopics();
};

/**
 * Creates a new MQTT topic configuration in the system.
 * 
 * @param topicSetting - The complete topic setting configuration to create
 * @returns Promise resolving to the newly created topic with assigned ID
 * @throws {ApiException} When validation fails, topic already exists, or access is denied
 */
export const createTopic = async (topicSetting: TopicSetting): Promise<any> => {
    return await topicClient.createTopic(topicSetting);
};

/**
 * Updates an existing MQTT topic configuration.
 * 
 * @param topicSetting - The topic setting with updated values (must include topicSettingId)
 * @returns Promise resolving to the updated topic setting
 * @throws {ApiException} When validation fails, topic doesn't exist, or access is denied
 */
export const updateTopic = async (topicSetting: TopicSetting): Promise<any> => {
    return await topicClient.updateTopic(topicSetting);
};

/**
 * Removes an MQTT topic configuration from the system.
 * 
 * @param topicSetting - The topic setting to delete (requires topicSettingId)
 * @returns Promise resolving to void on successful deletion
 * @throws {ApiException} When topic doesn't exist or access is denied
 */
export const deleteTopic = async (topicSetting: TopicSetting): Promise<any> => {
    return await topicClient.deleteTopic(topicSetting);
};

/**
 * Represents a postal code location with its associated name.
 * Used for location-based temperature data queries.
 */
export type PostalLocation = {
    /** The postal code number */
    postalCode: number;
    /** The human-readable location name */
    locationName: string;
};

/**
 * Re-exported API exception class for error handling.
 * Used for catching and handling API-specific errors.
 */
export {ApiException, TopicSetting};
