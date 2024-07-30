using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Collections.Specialized;

namespace PulsarCurrentFix
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string testData = "{\"chargeCurrent\":11,\"chargeDuration\":8640,\"chargePower\":4161,\"chargeRemainingDuration\":17725,\"chargeRemainingEnergy\":21245.176,\"chargedEnergy\":11064.282,\"chargerFeatureHeating\":false,\"chargerFeatureIntegratedDevice\":false,\"chargerIcon\":null,\"chargerPhases1p3p\":false,\"chargerPhysicalPhases\":null,\"charging\":true,\"connected\":true,\"connectedDuration\":0,\"disableThreshold\":500,\"effectiveLimitSoc\":80,\"effectiveMaxCurrent\":16,\"effectiveMinCurrent\":6,\"effectivePlanSoc\":0,\"effectivePlanTime\":null,\"effectivePriority\":0,\"enableThreshold\":-3899,\"enabled\":true,\"limitEnergy\":0,\"limitSoc\":0,\"maxCurrent\":16,\"minCurrent\":6,\"mode\":\"pv\",\"phaseAction\":\"inactive\",\"phaseRemaining\":0,\"phasesActive\":3,\"phasesConfigured\":3,\"phasesEnabled\":3,\"planEnergy\":0,\"planOverrun\":0,\"planProjectedStart\":null,\"planTime\":null,\"priority\":0,\"pvAction\":\"inactive\",\"pvRemaining\":0,\"sessionCo2PerKWh\":null,\"sessionEnergy\":11064.282,\"sessionPrice\":0.504,\"sessionPricePerKWh\":0.046,\"sessionSolarPercentage\":96.554,\"smartCostActive\":false,\"title\":\"Test\",\"vehicleClimaterActive\":null,\"vehicleDetectionActive\":false,\"vehicleLimitSoc\":0,\"vehicleName\":\"ev4\",\"vehicleOdometer\":3267,\"vehicleRange\":175,\"vehicleSoc\":47.78}\r\n";
        private static int chargeCurrent = 0;  

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, my EVCC friend!");
            while (true)
            {
                if (CheckEVCCChargingAndMode() && chargeCurrent >= 6 && WallboxAPI.CheckForWrontCurrent(chargeCurrent))
                {
                    WallboxAPI.SetMaxCurrent(chargeCurrent);
                }

                // Warte 1 min
                Thread.Sleep(60000);
            }
        }

        private static bool CheckEVCCChargingAndMode()
        {
            try
            {
                string url = String.Format("{0}/api/state?jq=.result.loadpoints[{1}]",
                    ConfigurationManager.AppSettings.Get("evcc_url"),
                    ConfigurationManager.AppSettings.Get("evcc_loadpoint"));
                HttpResponseMessage response = client.Send(new HttpRequestMessage(HttpMethod.Get, url));
                response.EnsureSuccessStatusCode();

                var task = Task.Run(() => response.Content.ReadAsStringAsync());
                task.Wait();
                var content = task.Result;
                
                JObject json = JObject.Parse(content);

                //JObject json = JObject.Parse(testData);

                JObject loadpoint = (JObject)json;
                bool charging = loadpoint.Value<bool>("charging");
                string mode = loadpoint.Value<string>("mode");
                chargeCurrent = ((int)Decimal.Round(loadpoint.Value<decimal>("chargeCurrent"), 0));

                if (charging == true && !mode.Equals("now")) return true;
                else return false;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                return false;
            }
        }
    }
}