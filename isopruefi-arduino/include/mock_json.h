#pragma once
#include <string>
#include <vector>
#include <map>
#include <sstream>
#include <cstring>

struct MockJsonArray {
    std::vector<std::string> values;

    void add(const std::string& value) {
        values.push_back(value);
    }

    void add(float value) {
        values.push_back(std::to_string(value));
    }

    void add(int value) {
        values.push_back(std::to_string(value));
    }

    std::string operator[](size_t i) const {
        if (i < values.size()) {
            return values[i];
        }
        return "";
    }

    size_t size() const {
        return values.size();
    }

    void clear() {
        values.clear();
    }
    
    bool is_null() const {
        return values.empty();
    }
};

struct MockJsonDocument {
    std::map<std::string, std::string> primitives;
    std::map<std::string, MockJsonArray> arrays;

    void clear() {
        primitives.clear();
        arrays.clear();
    }

    std::string& operator[](const std::string& key) {
        return primitives[key];
    }

    const std::string& operator[](const std::string& key) const {
        static std::string empty = "";
        auto it = primitives.find(key);
        return (it != primitives.end()) ? it->second : empty;
    }

    MockJsonArray& createNestedArray(const std::string& key) {
        return arrays[key];
    }

    bool containsKey(const std::string& key) const {
        return primitives.count(key) > 0 || arrays.count(key) > 0;
    }

    size_t size(const std::string& key) const {
        auto it = arrays.find(key);
        return (it != arrays.end()) ? it->second.size() : 0;
    }
    
    // ArduinoJson compatibility methods
    template<typename T>
    bool is() const {
        return false; // Simplified implementation
    }
    
    // Specialized template for JsonObject check
    bool isJsonObject(const std::string& key) const {
        return arrays.count(key) > 0;
    }
};

// Template specialization for JsonObject check
template<>
inline bool MockJsonDocument::is<MockJsonArray>() const {
    return !arrays.empty();
}

// Serialization functions to match ArduinoJson API
inline size_t serializeJson(const MockJsonDocument& doc, char* output, size_t outputSize) {
    std::stringstream ss;
    ss << "{";
    
    bool first = true;
    
    // Add primitives
    for (const auto& pair : doc.primitives) {
        if (!first) ss << ",";
        ss << "\"" << pair.first << "\":\"" << pair.second << "\"";
        first = false;
    }
    
    // Add arrays
    for (const auto& pair : doc.arrays) {
        if (!first) ss << ",";
        ss << "\"" << pair.first << "\":[";
        for (size_t i = 0; i < pair.second.values.size(); ++i) {
            if (i > 0) ss << ",";
            ss << pair.second.values[i];
        }
        ss << "]";
        first = false;
    }
    
    ss << "}";
    
    std::string result = ss.str();
    size_t len = std::min(result.length(), outputSize - 1);
    strncpy(output, result.c_str(), len);
    output[len] = '\0';
    
    return len;
}

// StaticJsonDocument template to match ArduinoJson
template<size_t Size>
struct StaticJsonDocument : public MockJsonDocument {
    StaticJsonDocument() = default;
};

#ifdef UNIT_TEST
// Redefine JsonDocument to use the mock
#define JsonDocument MockJsonDocument
#define JsonArray MockJsonArray
#endif