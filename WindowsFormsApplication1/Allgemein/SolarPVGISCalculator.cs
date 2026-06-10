using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{

    // --- DATENMODELL FÜR NOMINATIM (GEOCODING) ---
    // Definition der Datenstruktur (Mapping der JSON-Antwort)
    public class PvgisResponse
    {
        [JsonPropertyName("outputs")]
        public PvgisOutputs Outputs { get; set; }
        [JsonPropertyName("inputs")]
        public PvgisInputs Inputs { get; set; }
    }

    public class PvgisInputs
    {
        // Innerhalb von "inputs" liegt "meteo_data"
        public MeteoData Meteo_Data { get; set; }
    }

    public class MeteoData
    {
        // Hier liegt endlich der gesuchte Wert
        [JsonPropertyName("meteo_db")]
        public string Meteo_Db { get; set; }
        [JsonPropertyName("radiation_db")]
        public string RadiationDb { get; set; }
    }

    public class PvgisOutputs
    {
        [JsonPropertyName("tmy_hourly")]
        public List<TmyHourlyData> TmyHourly { get; set; }
    }

    public class GeoResult
    {
        [JsonPropertyName("lat")]
        public string LatString { get; set; } // Nominatim liefert Koordinaten als String!

        [JsonPropertyName("lon")]
        public string LonString { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }

    public class TmyHourlyData
    {
        [JsonPropertyName("time(UTC)")]
        public string TimeString { get; set; } // Format: "20200101:0000"

        [JsonPropertyName("T2m")]
        public double Temperature { get; set; } // Lufttemperatur [°C]

        [JsonPropertyName("RH")]
        public double Humidity { get; set; } // Relative Feuchte [%]

        [JsonPropertyName("G(h)")]
        public double GlobalIrradiance { get; set; } // Globalstrahlung [W/m2]

        [JsonPropertyName("Gb(n)")]
        public double DirectIrradiance { get; set; } // Globalstrahlung [W/m2]

        [JsonPropertyName("Gd(h)")]
        public double DiffuseIrradiance { get; set; } // Globalstrahlung [W/m2]

        [JsonPropertyName("WS10m")]
        public double WindSpeed { get; set; } // Windgeschwindigkeit [m/s]

        public double Sol_sued;
        public double Sol_ost;
        public double Sol_nord;
        public double Sol_west;

        public bool WE;
        public int TagTyp_W;
        public int TagTyp_NW;
        public double Sonnenwinkel;
    }

    public class PVGIS_EPW_Downloader
    {
        public static double longitude;
        public static double latitude;
        public static string displayName;
        public static string meteoDb;

        private static readonly HttpClient client = new HttpClient();

        public static async Task<List<TmyHourlyData>> GetTMY(double lon, double lat, int Azimut)
        {
            try
            {
                // 1. Basis-URL holen und absichern
                string baseUrl = Properties.Settings.Default.PVGISUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("Die PVGIS-API URL ist in den Einstellungen leer.");
                }

                // Automatische Korrektur, falls der Admin das 'tmy' oder das '/' vergessen/falsch eingegeben hat
                if (!baseUrl.EndsWith("/") && !baseUrl.EndsWith("tmy"))
                {
                    baseUrl += "/";
                }

                // Wenn der Admin nur die Haupt-URL eingetragen hat (z.B. .../api/v5_2/), hängen wir "tmy" an
                if (!baseUrl.EndsWith("tmy"))
                {
                    baseUrl += "tmy";
                }

                // Koordinaten-Konvertierung (Kommata durch Punkte ersetzen)
                string strLat = lat.ToString(CultureInfo.InvariantCulture);
                string strLon = lon.ToString(CultureInfo.InvariantCulture);

                string url = $"{baseUrl}?lat={strLat}&lon={strLon}&usepv=1&peakpower=1&loss=0&angle=0&aspect={Azimut}&outputformat=json&startyear=2005&endyear=2020";

                // 2. HTTP-Abfrage abschicken (mit try-catch geschützt gegen Timeouts/DNS-Fehler)
                var response = await client.GetAsync(url);

                // 3. Statuscodes detailliert prüfen
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        throw new Exception("Der PVGIS-Server hat eine leere Antwort zurückgegeben.");
                    }

                    // Deserialisierung des JSON absichern
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var result = JsonSerializer.Deserialize<PvgisResponse>(jsonString, options);

                        // Globale Variable oder Property setzen
                        meteoDb = result?.Inputs?.Meteo_Data?.Meteo_Db + " - " + result?.Inputs?.Meteo_Data?.RadiationDb;

                        return result?.Outputs?.TmyHourly ?? new List<TmyHourlyData>();
                    }
                    catch (JsonException)
                    {
                        throw new Exception("Die empfangenen Klimadaten sind beschädigt oder ungültig (JSON-Fehler).");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Gezielter 400er Fehler (PVGIS sendet oft den genauen Grund als Text, z.B. "Location outside database")
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    throw new ArgumentException($"PVGIS-Eingabefehler: {errorMsg}");
                }
                else
                {
                    // Alle anderen HTTP-Fehler (404, 500, 503)
                    throw new HttpRequestException($"Der Klimadaten-Server meldet einen Fehler: {response.StatusCode} ({response.ReasonPhrase})");
                }
            }
            // Fängt ungültige URLs ab
            catch (UriFormatException ex)
            {
                throw new Exception($"Die PVGIS-URL in den Administrationseinstellungen ist ungültig: {ex.Message}");
            }
            // Fängt Verbindungsabbrüche, falsche Serveradressen oder Timeouts ab
            catch (HttpRequestException ex) when (!ex.Message.Contains("Klimadaten-Server meldet"))
            {
                throw new Exception($"Es konnte keine Verbindung zum PVGIS-Server hergestellt werden. Internetverbindung prüfen oder URL korrigieren! ({ex.Message})");
            }
            // Reißt alle verbleibenden Exceptions mit, verpackt sie aber sauber
            catch (Exception ex)
            {
                throw new Exception($"Fehler bei der Klimadaten Ermittlung: {ex.Message}");
            }
        }

        /// <summary>
        /// Wandelt einen Ortsnamen in Koordinaten um (via OpenStreetMap Nominatim API).
        /// </summary>
        public static async Task<(bool Success, double Lat, double Lon, string DisplayName)> GetCoordinatesAsync(string query)
        {
            // WICHTIG: Nominatim verlangt einen User-Agent! Sonst Fehler 403.
            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd("CSharp_EpwTool_Demo/1.0"))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; EpwTool/1.0)");
            }

            try
            {
                // 1. Basis-URL aus den Settings holen
                string baseUrl = Properties.Settings.Default.GeoKodierung;

                // Sicherheitsprüfung: Falls gar nichts eingetragen ist
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return (false, 0, 0, "Fehler: Die Geo-API URL ist in den Einstellungen leer.");
                }

                // Falls der Admin das '/' am Ende vergessen hat, fügen wir es sauber an
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl += "/";
                }

                // URL encoding für Leerzeichen etc.
                string encodedQuery = System.Net.WebUtility.UrlEncode(query);
                string url = $"{baseUrl}search?q={encodedQuery}&format=json&limit=1";

                // 2. HTTP-Abfrage abschicken
                var response = await client.GetAsync(url);

                // Löst eine HttpRequestException aus, wenn der HTTP-Statuscode ein Fehler ist (z.B. 404, 500)
                response.EnsureSuccessStatusCode();

                // 3. JSON lesen
                string json = await response.Content.ReadAsStringAsync();

                // 4. Deserialisierung absichern
                var results = JsonSerializer.Deserialize<List<GeoResult>>(json);

                if (results == null || results.Count == 0)
                {
                    return (false, 0, 0, "Es wurden keine Koordinaten für diesen Ort gefunden.");
                }

                var bestMatch = results[0];

                // Parsing der Strings in Doubles (Achtung: Punkt als Dezimaltrenner!)
                double lat = double.Parse(bestMatch.LatString, CultureInfo.InvariantCulture);
                double lon = double.Parse(bestMatch.LonString, CultureInfo.InvariantCulture);

                return (true, lat, lon, bestMatch.DisplayName);
            }
            // Fängt fehlerhafte Formatierungen der URL ab (z.B. ungültige Zeichen)
            catch (UriFormatException ex)
            {
                return (false, 0, 0, $"Ungültiges URL-Format in den Einstellungen: {ex.Message}");
            }
            // Fängt Serverfehler, Timeouts, DNS-Fehler (Domain existiert nicht) oder falsche HTTP-Statuscodes ab
            catch (HttpRequestException ex)
            {
                return (false, 0, 0, $"Fehler bei der Serververbindung: Malformed URL oder Server offline? ({ex.Message})");
            }
            // Fängt unerwartete JSON-Formatierungsfehler ab (falls die API HTML statt JSON liefert)
            catch (JsonException)
            {
                return (false, 0, 0, "Fehler: Der Server hat keine gültigen JSON-Daten zurückgegeben.");
            }
            // Fängt alle anderen unvorhergesehenen Fehler ab
            catch (Exception ex)
            {
                return (false, 0, 0, $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
            }
        }

        // Add this helper method to the SolarCalculator class or as a private static method in the same file
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }

    public static class SolarCalculator
    {
        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;
        public static double sonnenwinkel;
        public static double sonnen_azimut;
        public static double lastCosTheta;

        /// <param name="dni">Gb(n) - Direct Normal Irradiance aus PVGIS</param>
        /// <param name="dhi">Gd(h) - Diffuse Horizontal Irradiance aus PVGIS</param>
        /// <param name="ghi">G(h) - Global Horizontal Irradiance aus PVGIS</param>
        public static double CalculateHourly(double Lon, double Lat, int Tilt, int Azimuth, double ghi, double dni, double dhi, double t2m, int dayOfYear, double hour)
        {
            // 1. Wahre Solarzeit (inkl. Längengrad & Zeitgleichung)
            double b = (360.0 / 365.0) * (dayOfYear - 81) * Deg2Rad;
            double eot = 9.87 * Math.Sin(2 * b) - 7.53 * Math.Cos(b) - 1.5 * Math.Sin(b);
            double solarHour = hour + (eot + 4.0 * Lon) / 60.0;
            double omega = (solarHour - 12.0) * 15.0;

            // 2. Sonnenposition
            double delta = 23.45 * Math.Sin(360.0 * (284 + dayOfYear) / 365.0 * Deg2Rad);
            double sinAlpha = Math.Sin(Lat * Deg2Rad) * Math.Sin(delta * Deg2Rad) +
                              Math.Cos(Lat * Deg2Rad) * Math.Cos(delta * Deg2Rad) * Math.Cos(omega * Deg2Rad);
            double alpha = Math.Asin(sinAlpha);

            sonnenwinkel = alpha * Rad2Deg;

            if (alpha <= 0) return 0; // Nacht

            double cosGammaS = (Math.Sin(alpha) * Math.Sin(Lat * Deg2Rad) - Math.Sin(delta * Deg2Rad)) /
                               (Math.Cos(alpha) * Math.Cos(Lat * Deg2Rad));
            double gammaS = Math.Acos(Clamp(cosGammaS, -1.0, 1.0));
            if (omega < 0) gammaS = -gammaS;
            sonnen_azimut = gammaS;

            // 3. Einfallswinkel auf Modul
            double cosTheta = Math.Sin(alpha) * Math.Cos(Tilt * Deg2Rad) +
                              Math.Cos(alpha) * Math.Sin(Tilt * Deg2Rad) * Math.Cos(gammaS - (Azimuth * Deg2Rad));
            lastCosTheta = Math.Max(0, cosTheta);
            cosTheta = Math.Max(0, cosTheta);

            // 4. Einstrahlung auf geneigte Fläche (G_GTI)
            double direct = dni * cosTheta;
            double skyView = (1.0 + Math.Cos(Tilt * Deg2Rad)) / 2.0;
            double groundView = (1.0 - Math.Cos(Tilt * Deg2Rad)) / 2.0;
            double gTotal = direct + (dhi * skyView) + (ghi * 0.2 * groundView); // 0.2 = Albedo Boden


            // 5. Temperaturkorrektur (Zelltemp)

            //double tCell = t2m + (gTotal * 0.04); // 0.04 = Koeffizient für Fassade (geringere Kühlung)
            //double tempFactor = 1.0 + (tCell - 25.0) * -0.0035;

            //return gTotal * Area * Efficiency * tempFactor * PR;
            return gTotal;
        }

        public static List<TmyHourlyData> GetDailyAverages(List<TmyHourlyData> hourlyData)
        {
            return hourlyData
                .GroupBy(h => (DateTime.ParseExact(h.TimeString, "yyyyMMdd:HHmm", CultureInfo.InvariantCulture).Month, DateTime.ParseExact(h.TimeString, "yyyyMMdd:HHmm", CultureInfo.InvariantCulture).Day)) // Gruppierung nach reinem Datum
                .Select(group => new TmyHourlyData
                {
                    // Wir nehmen den Datumsteil der Gruppe als String zurück
                    TimeString = $"{group.Key.Day:D2}.{group.Key.Month:D2}.{DateTime.Now.Year}",

                    // Berechnung der Durchschnitte
                    Sol_sued = group.Average(x => x.Sol_sued),
                    Sol_ost = group.Average(x => x.Sol_ost),
                    Sol_west = group.Average(x => x.Sol_west),
                    Sol_nord = group.Average(x => x.Sol_nord),
                    Temperature = group.Average(x => x.Temperature),
                    GlobalIrradiance = group.Average(x => x.GlobalIrradiance),
                    DirectIrradiance = group.Average(x => x.DirectIrradiance),
                    DiffuseIrradiance = group.Average(x => x.DiffuseIrradiance),
                    Sonnenwinkel = group.Max(x => x.Sonnenwinkel)
                })
                .ToList();
        }

        // Add this helper method to the SolarCalculator class or as a private static method in the same file
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Calculate(double ghi, double dhi, double lat, double lon, int doy, double hour, double slope, double azTarget)
        {
            if (ghi <= 0) return 0;
            double r = Math.PI / 180.0;

            // Sonnenstand
            double decl = 23.45 * Math.Sin((360.0 / 365.0 * (doy - 81)) * r) * r;
            double hourAngle = (hour - 12.0) * 15.0 * r + (lon * r);
            double latR = lat * r;

            double sinEl = Math.Sin(latR) * Math.Sin(decl) + Math.Cos(latR) * Math.Cos(decl) * Math.Cos(hourAngle);
            double el = Math.Asin(sinEl);
            if (el <= 0.02) return 0;

            // Einfallswinkel auf Fläche
            double cosAz = (Math.Sin(decl) * Math.Cos(latR) - Math.Cos(decl) * Math.Sin(latR) * Math.Cos(hourAngle)) / Math.Cos(el);
            double sunAz = Math.Acos(Clamp(cosAz, -1, 1)) * (hourAngle > 0 ? 1 : -1);

            double sR = slope * r;
            double aTR = azTarget * r;
            double cosTheta = Math.Sin(el) * Math.Cos(sR) + Math.Cos(el) * Math.Sin(sR) * Math.Cos(sunAz - aTR);

            // Transposition
            double zen = (Math.PI / 2.0) - el;
            double beam = Math.Max(0, ghi - dhi) * (Math.Max(0, cosTheta) / Math.Cos(zen));
            double diff = dhi * (1.0 + Math.Cos(sR)) / 2.0;
            double refl = ghi * 0.2 * (1.0 - Math.Cos(sR)) / 2.0;

            return Math.Max(0, beam + diff + refl);
        }


        public static double CalculateTimeOffset(double lat, double lon, DateTime date)
        {
            int dayOfYear = date.DayOfYear;

            // 1. Zeitgleichung (Equation of Time) in Minuten
            double b = 2.0 * Math.PI * (dayOfYear - 1) / 365.0;
            double eot = 229.18 * (0.000075 + 0.001868 * Math.Cos(b) - 0.032077 * Math.Sin(b)
                            - 0.014615 * Math.Cos(2 * b) - 0.040849 * Math.Sin(2 * b));

            // 2. Sonnendeklination (Neigung der Erdachse)
            double declination = 23.45 * Math.Sin(Deg2Rad * (360.0 / 365.0 * (dayOfYear - 81)));

            // 3. Stundenwinkel bei Sonnenaufgang (h = -0.83° für Lichtbrechung)
            double cosOmega = (Math.Sin(-0.83 * Deg2Rad) - Math.Sin(lat * Deg2Rad) * Math.Sin(declination * Deg2Rad))
                                / (Math.Cos(lat * Deg2Rad) * Math.Cos(declination * Deg2Rad));

            // Prüfung auf Polartag/nacht
            double omega = 0;
            bool sunNeverSets = cosOmega < -1;
            bool sunNeverRises = cosOmega > 1;
            if (!sunNeverSets && !sunNeverRises) omega = Math.Acos(cosOmega) * Rad2Deg;

            // 4. Mittagszeit in UTC (Solar Noon)
            // 720 Minuten = 12:00 Uhr
            double solarNoonUTC = 720 - (4 * lon) - eot;


            double EquationOfTime = eot;
            double SolarOffsetMinutes = (4 * lon) + eot; // Totaler Versatz UTC -> Sonnenzeit
            return SolarOffsetMinutes;
        }

    }

    public class AccessRepository
    {
        // Konstruktor leer und sauber, da Verbindung von außen kommt
        public AccessRepository() { }

        public void SaveTmyData(List<TmyHourlyData> dataList, string szOrt, string tabelle, int ID_Klimaregion,
                                        OleDbConnection connection, OleDbTransaction transaction)
        {
            if (dataList == null || dataList.Count == 0) return;

            try
            {
                bool istKlimadaten = (tabelle == "Tab_Klimadaten");

                // 1. SQL-Queries festlegen (ID_Klimadaten wurde aus der Tab_Klimadaten-Query entfernt!)
                string query = istKlimadaten
                    ? "INSERT INTO Tab_Klimadaten (ID_Klimaregion, Temperatur, Sol_Nord, Sol_Sued, Sol_Ost, Sol_West, Globalstrahlung, Direktstrahlung, Diffusstrahlung, WE, TagTyp_W, TagTyp_NW, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)"
                    : "INSERT INTO Tab_Solar (ID_Klimaregion, Temperatur, Sol_Nord, Sol_Sued, Sol_Ost, Sol_West, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?)";

                using (OleDbCommand command = new OleDbCommand(query, connection, transaction))
                {
                    // 2. Typisierte Parameter vorab definieren (Zwingend nötig für Speed bei 8760 Schleifendurchläufen!)
                    // HIER WURDE DER ERSTE ID-PARAMETER ENTFERNT!
                    command.Parameters.Add("?", OleDbType.Integer);      // ID_Klimaregion
                    command.Parameters.Add("?", OleDbType.Double);       // Temperatur
                    command.Parameters.Add("?", OleDbType.Double);       // Sol_Nord
                    command.Parameters.Add("?", OleDbType.Double);       // Sol_Sued
                    command.Parameters.Add("?", OleDbType.Double);       // Sol_Ost
                    command.Parameters.Add("?", OleDbType.Double);       // Sol_West
                    command.Parameters.Add("?", OleDbType.Double);       // Globalstrahlung
                    command.Parameters.Add("?", OleDbType.Double);       // Direktstrahlung
                    command.Parameters.Add("?", OleDbType.Double);       // Diffusstrahlung

                    if (istKlimadaten)
                    {
                        command.Parameters.Add("?", OleDbType.Boolean);      // WE
                        command.Parameters.Add("?", OleDbType.Integer);      // TagTyp_W
                        command.Parameters.Add("?", OleDbType.Integer);      // TagTyp_NW
                        command.Parameters.Add("?", OleDbType.Double);       // Sonnenwinkel
                    }
                    else
                    {
                        command.Parameters.Add("?", OleDbType.Double);       // Sonnenwinkel
                    }

                    // 3. Die Schleife befüllt jetzt blitzschnell nur noch die Werte (.Value)
                    foreach (var data in dataList)
                    {
                        int pIdx = 0;

                        // HIER WURDE DER SEITENEFFEKT 'nextId++' KOMPLETT ENTFERNT!
                        command.Parameters[pIdx++].Value = ID_Klimaregion;
                        command.Parameters[pIdx++].Value = data.Temperature;
                        command.Parameters[pIdx++].Value = data.Sol_nord;
                        command.Parameters[pIdx++].Value = data.Sol_sued;
                        command.Parameters[pIdx++].Value = data.Sol_ost;
                        command.Parameters[pIdx++].Value = data.Sol_west;
                        command.Parameters[pIdx++].Value = data.GlobalIrradiance;
                        command.Parameters[pIdx++].Value = data.DirectIrradiance;
                        command.Parameters[pIdx++].Value = data.DiffuseIrradiance;

                        if (istKlimadaten)
                        {
                            command.Parameters[pIdx++].Value = data.WE;
                            command.Parameters[pIdx++].Value = data.TagTyp_W;
                            command.Parameters[pIdx++].Value = data.TagTyp_NW;
                            command.Parameters[pIdx++].Value = Math.Round(data.Sonnenwinkel, 1);
                        }
                        else
                        {
                            command.Parameters[pIdx++].Value = data.Sonnenwinkel > 0 ? Math.Round(data.Sonnenwinkel, 1) : 0;
                        }

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Schreiben der Datensätze in die Tabelle '{tabelle}': {ex.Message}", ex);
            }
        }
    }

}
