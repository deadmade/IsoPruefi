#pragma once
#include <string>
#include <vector>
#include <map>
#include <sstream>
#include <cstring>

// Forward declarations
struct MockJsonArray;
struct MockJsonObject;
struct MockJsonValue;

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
    
    void add(uint32_t value) {
        values.push_back(std::to_string(value));
    }
    
    void add(std::nullptr_t) {
        values.push_back("null");
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

struct MockJsonObject {
    std::map<std::string, MockJsonValue*> values;
    
    MockJsonValue& operator[](const std::string& key);
    
    template<typename T>
    T to() {
        static MockJsonArray arr;
        return arr;
    }
    
    size_t size() const {
        return values.size();
    }
};

struct MockJsonValue {
    std::string stringValue;
    MockJsonArray* arrayPtr;
    MockJsonObject* objectPtr;
    
    MockJsonValue() : arrayPtr(nullptr), objectPtr(nullptr) {}
    MockJsonValue(const std::string& str) : stringValue(str), arrayPtr(nullptr), objectPtr(nullptr) {}
    
    // Assignment operators
    MockJsonValue& operator=(const std::string& str) {
        stringValue = str;
        return *this;
    }
    
    MockJsonValue& operator=(const char* str) {
        stringValue = str;
        return *this;
    }
    
    MockJsonValue& operator=(int val) {
        stringValue = std::to_string(val);
        return *this;
    }
    
    MockJsonValue& operator=(uint32_t val) {
        stringValue = std::to_string(val);
        return *this;
    }
    
    MockJsonValue& operator=(float val) {
        stringValue = std::to_string(val);
        return *this;
    }
    
    MockJsonValue& operator=(std::nullptr_t) {
        stringValue = "null";
        return *this;
    }
    
    // Conversion methods
    const char* c_str() const {
        return stringValue.c_str();
    }
    
    template<typename T>
    bool is() const {
        return false; // Simplified
    }
    
    size_t size() const {
        if (arrayPtr) return arrayPtr->size();
        if (objectPtr) return objectPtr->size();
        return 0;
    }
    
    // Template specializations for to() method
    template<typename T>
    T& to() {
        static T defaultValue;
        return defaultValue;
    }
};

// Template specializations
template<>
inline MockJsonArray& MockJsonValue::to<MockJsonArray>() {
    if (!arrayPtr) {
        arrayPtr = new MockJsonArray();
    }
    return *arrayPtr;
}

template<>
inline MockJsonObject& MockJsonValue::to<MockJsonObject>() {
    if (!objectPtr) {
        objectPtr = new MockJsonObject();
    }
    return *objectPtr;
}

// MockJsonObject method implementation
inline MockJsonValue& MockJsonObject::operator[](const std::string& key) {
    if (values.find(key) == values.end()) {
        values[key] = new MockJsonValue();
    }
    return *values[key];
}

struct MockJsonDocument {
    std::map<std::string, MockJsonValue*> values;

    void clear() {
        for (auto& pair : values) {
            delete pair.second;
        }
        values.clear();
    }
    
    ~MockJsonDocument() {
        clear();
    }

    MockJsonValue& operator[](const std::string& key) {
        if (values.find(key) == values.end()) {
            values[key] = new MockJsonValue();
        }
        return *values[key];
    }

    MockJsonArray& createNestedArray(const std::string& key) {
        if (values.find(key) == values.end()) {
            values[key] = new MockJsonValue();
        }
        return values[key]->to<MockJsonArray>();
    }
    
    MockJsonObject& createNestedObject(const std::string& key) {
        if (values.find(key) == values.end()) {
            values[key] = new MockJsonValue();
        }
        return values[key]->to<MockJsonObject>();
    }

    bool containsKey(const std::string& key) const {
        return values.count(key) > 0;
    }

    size_t size(const std::string& key) const {
        auto it = values.find(key);
        return (it != values.end()) ? it->second->size() : 0;
    }
    
    // ArduinoJson compatibility methods
    template<typename T>
    bool is() const {
        return false; // Simplified implementation
    }
};

// Serialization functions to match ArduinoJson API
inline size_t serializeJson(const MockJsonDocument& doc, char* output, size_t outputSize) {
    std::stringstream ss;
    ss << "{";
    
    bool first = true;
    
    // Add all values
    for (const auto& pair : doc.values) {
        if (!first) ss << ",";
        ss << "\"" << pair.first << "\":";
        
        // Check if this value has an array
        if (pair.second->arrayPtr && !pair.second->arrayPtr->values.empty()) {
            ss << "[";
            bool arrayFirst = true;
            for (const auto& arrayVal : pair.second->arrayPtr->values) {
                if (!arrayFirst) ss << ",";
                if (arrayVal == "null") {
                    ss << "null";
                } else {
                    // Try to parse as number, otherwise treat as string
                    char* endptr;
                    double numVal = strtod(arrayVal.c_str(), &endptr);
                    if (*endptr == '\0') {
                        // It's a valid number
                        ss << arrayVal;
                    } else {
                        // It's a string, add quotes
                        ss << "\"" << arrayVal << "\"";
                    }
                }
                arrayFirst = false;
            }
            ss << "]";
        }
        // Check if this value has an object
        else if (pair.second->objectPtr) {
            ss << "{";
            bool objFirst = true;
            for (const auto& objPair : pair.second->objectPtr->values) {
                if (!objFirst) ss << ",";
                ss << "\"" << objPair.first << "\":\"" << objPair.second->stringValue << "\"";
                objFirst = false;
            }
            ss << "}";
        }
        // Regular string/number value
        else {
            if (pair.second->stringValue == "null") {
                ss << "null";
            } else {
                // Try to parse as number, otherwise treat as string
                char* endptr;
                double numVal = strtod(pair.second->stringValue.c_str(), &endptr);
                if (*endptr == '\0') {
                    // It's a valid number
                    ss << pair.second->stringValue;
                } else {
                    // It's a string, add quotes
                    ss << "\"" << pair.second->stringValue << "\"";
                }
            }
        }
        
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
#define JsonObject MockJsonObject
#endif