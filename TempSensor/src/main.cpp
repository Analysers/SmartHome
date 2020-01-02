#include <Arduino.h>
#include <WiFi.h>
#include <AsyncTCP.h>
#include <ESPAsyncWebServer.h>
#include <ESPmDNS.h>
#include <ArduinoJson.h>
#include <DHT.h>

#define DHTPIN 27
#define DHTTYPE DHT11

DHT dht(DHTPIN, DHTTYPE);

#define SSID "yourssid"
#define PASS "yourpass"
#define DEVICENAME "sh-temp-1"
AsyncWebServer server(80);

void setup()
{
    // put your setup code here, to run once:
    Serial.begin(9600);
    Serial.println("Starting SmartHome TempSensor");
    WiFi.begin(SSID, PASS);
    delay(1);
    WiFi.setHostname(DEVICENAME);
    dht.begin();
    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        Serial.println("Connecting to WiFi..");
    }

    Serial.println("Connected to the WiFi network");
    Serial.print("Server IP: ");
    Serial.println(WiFi.localIP());
    Serial.println("\n\n");

    if (!MDNS.begin(DEVICENAME))
    {
        Serial.println("Error setting up MDNS responder!");
        while (1)
        {
            delay(1000);
        }
    }
    Serial.println("mDNS responder started");
    MDNS.addService("http", "tcp", 80);
    MDNS.addService("tempsensor", "tcp", 80);

    server.on("/sensor", HTTP_GET, [](AsyncWebServerRequest *request) {
        auto *response = request->beginResponseStream("application/json");
        const int capacity = JSON_OBJECT_SIZE(2);
        StaticJsonDocument<capacity> json;
        json["humidity"] = dht.readHumidity();
        json["temperature"] = dht.readTemperature();
        serializeJson(json, *response);
        request->send(response);

        Serial.println("Received sensor request from client");
    });

    server.begin();
}

void loop()
{
    // put your main code here, to run repeatedly:
}