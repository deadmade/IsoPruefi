#pragma once

#ifdef UNIT_TEST
// Mock secrets for testing
#define SECRET_SSID "MockSSID"
#define SECRET_PASS "MockPassword"
#define SECRET_MQTT_USER "MockMQTTUser"
#define SECRET_MQTT_PASS "MockMQTTPassword"

// Mock compilation macros - only if not already defined
#ifndef F
#define F(string_literal) (string_literal)
#endif

#ifndef __DATE__
#define __DATE__ "Jul 26 2025"
#endif

#ifndef __TIME__
#define __TIME__ "14:55:00"
#endif

#else
// Production secrets
#include "secrets.h"
#endif