#pragma once
#include <string>
#include <vector>
#include <map>

struct MockJsonArray {
    std::vector<std::string> values;

    void add(const std::string& value) {
        values.push_back(value);
    }

    void add(float value) {
        values.push_back(std::to_string(value));
    }

    std::string operator[](size_t i) const {
        return values[i];
    }

    size_t size() const {
        return values.size();
    }

    void clear() {
        values.clear();
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
        return primitives.at(key);
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
};

// // Redefine JsonDocument to use the mock
#define JsonDocument MockJsonDocument
#define JsonArray MockJsonArray
