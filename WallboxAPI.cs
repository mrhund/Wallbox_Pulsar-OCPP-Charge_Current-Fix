using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Net.Http.Headers;
using System.Text;

namespace PulsarCurrentFix
{
    internal class WallboxAPI
    {
        private static string tokenUrl = "https://api.wall-box.com/auth/token/user";
        private static string chargersUrl = "https://api.wall-box.com/chargers/config/" + ConfigurationManager.AppSettings.Get("wallbox_deviceid");
        private static string username = ConfigurationManager.AppSettings.Get("wallbox_username");
        private static string password = ConfigurationManager.AppSettings.Get("wallbox_password");

        private static string token = "";

        public static void SetMaxCurrent(int maxCurrent)
        {
            if (token == null)
            {
                Console.WriteLine("Fehler beim Abrufen des JWT-Tokens");
                return;
            }

            UpdateChargerConfig(chargersUrl, token, maxCurrent);
        }


        public static bool CheckForWrontCurrent(int evccChargingCurrent) {
            try
            {
                // Schritt 1: Authentifizierung und Abrufen des Tokens
                token =  GetJwtToken(tokenUrl, username, password);
                if (token == null)
                {
                    Console.WriteLine("Fehler beim Abrufen des JWT-Tokens");
                    return false;
                }

                // Schritt 2: Abrufen der Charger-Konfiguration
                return IsWrongCurrent(chargersUrl, token, evccChargingCurrent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}");
                return false;
            }
        }

        private static string GetJwtToken(string url, string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

                var response = client.Send(new HttpRequestMessage(HttpMethod.Post, url));
                if (response.IsSuccessStatusCode)
                {
                    var task = Task.Run(() => response.Content.ReadAsStringAsync());
                    task.Wait();
                    var content = task.Result;
                    JObject json = JObject.Parse(content);
                    return json["jwt"]?.ToString();
                }
                return null;
            }
        }

        private static bool IsWrongCurrent(string url, string token, int evccChargeCurrent)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = client.Send(new HttpRequestMessage(HttpMethod.Get, url));
                if (response.IsSuccessStatusCode)
                {
                    var task = Task.Run(() => response.Content.ReadAsStringAsync());
                    task.Wait();
                    var content = task.Result;
                    JObject json = JObject.Parse(content);
                    var maxChargingCurrent = json.Value<int>("max_charging_current");
                    if (maxChargingCurrent > evccChargeCurrent) return true;
                    else return false;
                }
                else
                {
                    Console.WriteLine("Fehler beim Abrufen der Charger-Konfiguration");
                    return false;
                }
            }
        }

        private static void UpdateChargerConfig(string url, string token, int maxChargingCurrent)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var data = new { max_charging_current = maxChargingCurrent };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var message = new HttpRequestMessage(HttpMethod.Post, url);
                message.Content = content;

                var response = client.Send(message);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(String.Format("Charger-Konfiguration erfolgreich aktualisiert. Neuer Wert: {0}A", maxChargingCurrent));
                }
                else
                {
                    Console.WriteLine("Fehler beim Aktualisieren der Charger-Konfiguration.");
                }
            }
        }
    }

}
