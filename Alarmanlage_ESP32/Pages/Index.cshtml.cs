using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;

namespace Alarmanlage_ESP32.Pages
{
    /// <summary>
    /// PageModel für die Hauptseite der Alarmanlage.
    /// Ruft Sensordaten vom ESP32 ab und zeigt sie an.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static readonly HttpClient httpClient = new HttpClient 
        { 
            Timeout = TimeSpan.FromSeconds(2) 
        };

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Aktuelle Sensordaten
        public string Temp { get; set; } = "--";
        public string Hum { get; set; } = "--";
        public string Motion { get; set; } = "0";
        public string Light { get; set; } = "0";
        public string LightRaw { get; set; } = "0";
        public string Alarm { get; set; } = string.Empty; // default: empty => kein Alarm

        // Liste zur Speicherung der letzten100 Messungen
        public static List<string> History = new List<string>();

        // URL des ESP32 Webservers (ändert sich je nach Setup)
        private string ESP_URL = "http://192.168.4.1/data";

        /// <summary>
        /// Wird bei jedem Seitenaufruf ausgeführt.
        /// Holt aktuelle Daten vom ESP32.
        /// </summary>
        public async Task OnGetAsync()
        {
            await FetchESPData();
        }

        /// <summary>
        /// Wird beim Klick auf "Alarm deaktivieren" ausgeführt.
        /// Sendet einen Befehl an den ESP32, um den Alarm auszuschalten.
        /// </summary>
        public async Task<IActionResult> OnPostDisableAlarmAsync()
        {
            try
            {
                var resp = await httpClient.GetAsync("http://192.168.4.1/disable-alarm");
                if (resp.IsSuccessStatusCode)
                {
                    Alarm = string.Empty; // Alarm erfolgreich deaktiviert
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Deaktivieren des Alarms");
            }
            return RedirectToPage(); // Seite neu laden
        }

        /// <summary>
        /// Holt aktuelle Sensordaten vom ESP32 und speichert sie in den Properties.
        /// Fügt die Daten außerdem der History hinzu (neueste oben).
        /// </summary>
        private async Task FetchESPData()
        {
            try
            {
                // Rohdaten vom ESP32 abrufen
                var data = await httpClient.GetStringAsync(ESP_URL);
                var parts = data.Split(',');

                // Wenn genug Daten vorhanden sind, Werte übernehmen
                // Zugriff auf parts[5] benötigt mindestens6 Teile
                if (parts.Length >=6)
                {
                    Temp = parts[0];
                    Hum = parts[1];
                    LightRaw = parts[4];


                    ShowBinaryDataOnWebsite(Convert.ToInt32(parts[2]), Convert.ToInt32(parts[3]), Convert.ToInt32(parts[5]));

                    // Log-Eintrag bilden und in Historie einfügen
                    History.Insert(0, BuildLogString(parts));

                    // Liste auf max.100 Einträge beschränken
                    if (History.Count >100)
                        History.RemoveAt(History.Count -1);
                }
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung und Logeintrag
                _logger.LogError(ex, "Fehler beim Abrufen der ESP-Daten");
                Temp = Hum = Motion = Light = "--";
                Alarm = string.Empty; // setze Alarm leer bei Fehler
            }
        }

        /// <summary>
        /// Methode bastelt sich aus den übergebenen Daten eine string zusammen für die HIstorie
        /// </summary>
        /// <param name="parts">Daten vom ESP</param>
        /// <returns>String für die Historie</returns>
        private string BuildLogString(string[] parts)
        {
            string log = DateTime.Now.ToLongTimeString() + $" TTTemp: {parts[0]}\tHum: {parts[1]}\tLicht: {parts[3]} ({parts[4]})\t";

            if (parts[2] == "1")
            {
                log += "Bewegung erkannt!\t";
            }
            else
            {
                log += "Keine Bewegung erkannt!\t\t";
            }

            if (parts[5] == "1")
            {
                log += "ALARM!!";
            }
            else
            {
                log += "Kein Alarm";
            }

            return log;        
        }

        /// <summary>
        /// Methode stellt die binären Werte für 
        /// Bewegung, Licht und Alarmstate dar
        /// </summary>
        /// <param name="motion">Bewegung</param>
        /// <param name="light">Licht</param>
        /// <param name="alarmstate">Alarmstate</param>
        private void ShowBinaryDataOnWebsite(int _motion, int _light, int _alarmstate)
        {
            //Stelle Werte für Bewegung dar
            if (_motion ==1)
            {
                Motion = "Erkannt!";
            }
            else
            {
                Motion = string.Empty;
            }

            //Stelle Werte für Licht dar
            if (_light ==1)
            {
                Light = "AN";
            }
            else 
            {
                Light = "AUS";
            }

            //Stelle Werte für Alarm dar
            if (_alarmstate ==1)
            {
                Alarm = "Einbrecher!"; // genau dieser Text löst Alarm im Frontend aus
            }
            else
            {
                Alarm = string.Empty;
            }
        }
    }
}
