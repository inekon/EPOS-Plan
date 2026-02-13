using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class TemperatureService
{
    private static readonly HttpClient client = new HttpClient();

    public async Task GetAnnualTemperatureCurve(string location)
    {
        // 1. Geocoding: Koordinaten ermitteln
        var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={location}&count=1&language=de&format=json";
        var geoResponse = await client.GetStringAsync(geoUrl);
        using (var geoDoc = JsonDocument.Parse(geoResponse))
        {
            var locationData = geoDoc.RootElement.GetProperty("results")[0];

            double lat = locationData.GetProperty("latitude").GetDouble();
            double lon = locationData.GetProperty("longitude").GetDouble();

            // 2. Wetterdaten: Historische Werte für ein Jahr (z.B. 2024)
            // Wir nutzen die tägliche Durchschnittstemperatur (temperature_2m_mean)
            var weatherUrl = $"https://archive-api.open-meteo.com/v1/archive?latitude={lat.ToString().Replace(",", ".")}&longitude={lon.ToString().Replace(",", ".")}&start_date=1986-01-01&end_date=1986-12-31&hourly=temperature_2m&timezone=Europe%2FBerlin";

            var weatherResponse = await client.GetStringAsync(weatherUrl);
            using (var weatherDoc = JsonDocument.Parse(weatherResponse))
            {
                var dates = weatherDoc.RootElement.GetProperty("hourly").GetProperty("time").EnumerateArray().Select(x => x.GetString()).ToList();
                var temps = weatherDoc.RootElement.GetProperty("hourly").GetProperty("temperature_2m").EnumerateArray().Select(x => x.GetDouble()).ToList();

                // 3. Ausgabe (Beispiel: Durchschnitt pro Monat für eine glattere Kurve)
                Console.WriteLine($"Jahrestemperaturkurve für {location} ({lat}, {lon}):");
                for (int i = 0; i < dates.Count; i += 1) // Grober 30-Tage-Rhythmus
                {
                    Console.WriteLine($"{dates[i]}: {temps[i]}°C");
                }
          
                // inside GetAnnualTemperatureCurve, replace the write line with:
                string dateiPfad = @"C:\temp\meineDatei.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(dateiPfad) ?? @"C:\temp"); // ensure folder exists

                var lines = temps.Select(t => t.ToString("G", CultureInfo.CurrentCulture));
                File.WriteAllLines(dateiPfad, lines);
            }
        }
    }
}