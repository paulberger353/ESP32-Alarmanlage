#include <WiFi.h>
#include <WebServer.h>
#include <DHT.h>

#define DHTPIN 22
#define DHTTYPE DHT11

#define TRIGPIN 16
#define ECHOPIN 17
#define LDRPIN 34
#define BUZZER 27

const char* ssid = "ESP32_BS";
const char* password = "12345678";

WebServer server(80);
DHT dht(DHTPIN, DHTTYPE);

// Sensorwerte
float temp = NAN;
float hum = NAN;
int motion = 0;
int light = 0;
int alarmState = 0;
int lightRaw = 0;

unsigned long lastRead = 0;

// --- Buzzer Sirene Steuerung ---
unsigned long lastBuzzerUpdate = 0;
int buzzerFreq = 1000;
int buzzerStep = 50;
bool buzzerUp = true;

// --- Ultraschall ---
long readUltrasonicCM() {
  digitalWrite(TRIGPIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIGPIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIGPIN, LOW);

  long duration = pulseIn(ECHOPIN, HIGH, 30000);
  long distance = duration / 58;
  if(duration == 0) distance = 999;
  return distance;
}

// --- Sensoren lesen ---
void readSensors() {
  temp = dht.readTemperature();
  hum = dht.readHumidity();

  long distance = readUltrasonicCM();
  motion = (distance > 0 && distance <= 20) ? 1 : 0;

  lightRaw = analogRead(LDRPIN);
  light = (lightRaw < 2500) ? 0 : 1;

  // Alarm-Trigger-Logik
  if (light == 0 && motion == 1) {
      alarmState = 1;  // Alarm aktivieren
  }

  // Alarm-Reset: nur wenn Licht an ist
  if (light == 1) {
      alarmState = 0;
  }
}

// --- Buzzer Sirene ---
void buzzerSirene() {
  if (alarmState == 1) {
    if (millis() - lastBuzzerUpdate >= 10) { // 10 ms Schritt für „wiuwiu“
      lastBuzzerUpdate = millis();
      tone(BUZZER, buzzerFreq);

      // Frequenz rauf/runter
      if (buzzerUp) {
        buzzerFreq += buzzerStep;
        if (buzzerFreq >= 2000) buzzerUp = false;
      } else {
        buzzerFreq -= buzzerStep;
        if (buzzerFreq <= 1000) buzzerUp = true;
      }
    }
  } else {
    noTone(BUZZER); // Alarm aus
  }
}

// --- HTTP Handler ---
void handleData() {
  readSensors();
  String data = String(temp) + "," + String(hum) + "," + String(motion) + "," + String(light) + "," + String(lightRaw) + "," + String(alarmState);
  server.send(200, "text/plain", data);
}

void handleDisableAlarm() {
  alarmState = 0;
  noTone(BUZZER);
  server.send(200, "text/plain", "Alarm deaktiviert");
}

void setup() {
  Serial.begin(115200);
  pinMode(TRIGPIN, OUTPUT);
  pinMode(ECHOPIN, INPUT);
  pinMode(LDRPIN, INPUT);
  pinMode(BUZZER, OUTPUT);

  dht.begin();

  WiFi.softAP(ssid, password);
  Serial.println("Access Point gestartet");
  Serial.print("IP Adresse: ");
  Serial.println(WiFi.softAPIP());

  server.on("/data", handleData);
  server.on("/disable-alarm", handleDisableAlarm);
  server.begin();
}

void loop() {
  server.handleClient();

  if (millis() - lastRead >= 2000) { // alle 2 Sekunden Sensoren lesen
    lastRead = millis();
    readSensors();
    Serial.print("Temp: "); Serial.print(temp);
    Serial.print(" °C,    Hum: "); Serial.print(hum);
    Serial.print(" %,     Motion: "); Serial.print(motion);
    Serial.print(",     Light: "); Serial.print(light);
    Serial.print("  "); Serial.print(lightRaw);
    Serial.print(",     Alarm: "); Serial.println(alarmState);
  }

  // Alarm prüfen und Sirene abspielen
  buzzerSirene();
}
