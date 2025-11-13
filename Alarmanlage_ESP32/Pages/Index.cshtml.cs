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
        private static readonly HttpClient httpClient = new HttpClient();

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
        public string Alarm { get; set; } = "0";

        // Liste zur Speicherung der letzten 100 Messungen
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
                    Alarm = "0"; // Alarm erfolgreich deaktiviert
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
                if (parts.Length >= 5)
                {
                    Temp = parts[0];
                    Hum = parts[1];
                    Motion = parts[2];
                    Light = parts[3];
                    LightRaw = parts[4];
                    Alarm = parts[5];

                    // Log-Eintrag formatieren
                    string log = $"{DateTime.Now:HH:mm:ss} - Temp:{Temp}°C Hum:{Hum}% Motion:{Motion} Light:{Light} ({LightRaw}) Alarm:{Alarm}";

                    // Neueste Daten oben einfügen
                    History.Insert(0, log);

                    // Liste auf max. 100 Einträge beschränken
                    if (History.Count > 100)
                        History.RemoveAt(History.Count - 1);
                }
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung und Logeintrag
                _logger.LogError(ex, "Fehler beim Abrufen der ESP-Daten");
                Temp = Hum = Motion = Light = Alarm = "--";
            }
        }
    }
}
