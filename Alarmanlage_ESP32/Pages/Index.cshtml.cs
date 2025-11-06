using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;

namespace Alarmanlage_ESP32.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static readonly HttpClient httpClient = new HttpClient();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string Temp { get; set; } = "--";
        public string Hum { get; set; } = "--";
        public string Motion { get; set; } = "0";
        public string Light { get; set; } = "0";
        public string Alarm { get; set; } = "0";

        public static List<string> History = new List<string>();

        private string ESP_URL = "http://192.168.4.1/data"; // /data vom ESP32 WebServer

        public async Task OnGetAsync()
        {
            await FetchESPData();
        }

        public async Task<IActionResult> OnPostDisableAlarmAsync()
        {
            try
            {
                var resp = await httpClient.GetAsync("http://192.168.4.1/disable-alarm");
                if (resp.IsSuccessStatusCode)
                {
                    Alarm = "0";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Deaktivieren des Alarms");
            }
            return RedirectToPage();
        }

        private async Task FetchESPData()
        {
            try
            {
                var data = await httpClient.GetStringAsync(ESP_URL);
                var parts = data.Split(',');
                if (parts.Length >= 5)
                {
                    Temp = parts[0];
                    Hum = parts[1];
                    Motion = parts[2];
                    Light = parts[3];
                    Alarm = parts[4];

                    string log = $"{DateTime.Now:HH:mm:ss} - Temp:{Temp}°C Hum:{Hum}% Motion:{Motion} Light:{Light} Alarm:{Alarm}";
                    History.Add(log);
                    if (History.Count > 100) History.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der ESP-Daten");
                Temp = Hum = Motion = Light = Alarm = "--";
            }
        }
    }
}
