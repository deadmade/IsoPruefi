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
export const fetchPostalLocations = async () => {
    const response = await locationClient.getAllPostalcodes();
    // Parse the blob response - assuming it returns JSON
    const text = await response.data.text();
    return JSON.parse(text);
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
