using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;


// --- DATENMODELL FÜR NOMINATIM (GEOCODING) ---
// 1. Definition der Datenstruktur (Mapping der JSON-Antwort)
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

        // API URL für JSON Output
        // startyear/endyear: Zeitraum aus dem das TRY gebildet wird (z.B. 2005-2020)
        string url = $"https://re.jrc.ec.europa.eu/api/tmy?lat={lat.ToString().Replace(',', '.')}&lon={lon.ToString().Replace(',', '.')}&usepv=1&peakpower=1&loss=0&angle=0&aspect={Azimut}&outputformat=json&startyear=2005&endyear=2020";

        string jsonString = "";
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            jsonString = await response.Content.ReadAsStringAsync(); ;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // GEZIELT den 400er abgreifen
            var errorMsg = await response.Content.ReadAsStringAsync();
            MessageBox.Show($"Simulation konnte nicht gestartet werden: {errorMsg}", "Eingabefehler");
            return null;
        }
        else
        {
        // Alle anderen Fehler (500, 404, etc.)
            MessageBox.Show($"Serverfehler: {response.ReasonPhrase}");
            return null;
        }

        // Deserialisierung des JSON in C# Objekte
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<PvgisResponse>(jsonString, options);
        meteoDb = result?.Inputs?.Meteo_Data?.Meteo_Db + " - " + result?.Inputs?.Meteo_Data?.RadiationDb;

        return result?.Outputs?.TmyHourly ?? new List<TmyHourlyData>();
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

        // URL encoding für Leerzeichen etc. (Berlin Alexanderplatz -> Berlin%20Alexanderplatz)
        string encodedQuery = System.Net.WebUtility.UrlEncode(query);
        string url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&limit=1";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        // Die API gibt eine Liste zurück (auch wenn limit=1)
        var results = JsonSerializer.Deserialize<List<GeoResult>>(json);

        if (results == null || results.Count == 0)
        {
            return(false, 0, 0, null);    
        }
  
        var bestMatch = results[0];

        // Parsing der Strings in Doubles (Achtung: Punkt als Dezimaltrenner!)
        double lat = double.Parse(bestMatch.LatString, CultureInfo.InvariantCulture);
        double lon = double.Parse(bestMatch.LonString, CultureInfo.InvariantCulture);

        return (true, lat, lon, bestMatch.DisplayName);
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
    private readonly string _connectionString;

    public AccessRepository(string dbPath)
    {
        // Connection String für .accdb Dateien
        _connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
    }

    public void SaveTmyData(List<TmyHourlyData> dataList, string szOrt, string tabelle, int ID_Klimaregion)
    {
       
        using (OleDbConnection connection = new OleDbConnection(_connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {

                if (tabelle == "Tab_Klimadaten")
                {
                    // 1. Maximale ID abfragen
                    // NZ oder IIf(IsNull...) ist in Access das Äquivalent zu COALESCE
                    string maxIdQuery = "SELECT MAX(ID_Klimadaten) FROM Tab_Klimadaten";
                    int nextId = 1;

                    using (OleDbCommand cmdMax2 = new OleDbCommand(maxIdQuery, connection, transaction))
                    {
                        object result = cmdMax2.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            nextId = Convert.ToInt32(result) + 1;
                        }
                    }

                    // Wir nutzen eine Transaktion für deutlich bessere Performance bei 8760 Zeilen
                    string query = "INSERT INTO Tab_Klimadaten (ID_Klimadaten,ID_Klimaregion, Temperatur, Sol_Nord, Sol_Sued, Sol_Ost,Sol_West,Globalstrahlung, " +
                        "Direktstrahlung, Diffusstrahlung, WE, TagTyp_W, TagTyp_NW, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

                    using (OleDbCommand command = new OleDbCommand(query, connection, transaction))
                    {
                        // Parameter definieren
                        command.Parameters.Add("?", OleDbType.Integer);
                        command.Parameters.Add("?", OleDbType.Integer);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Boolean);
                        command.Parameters.Add("?", OleDbType.Integer);
                        command.Parameters.Add("?", OleDbType.Integer);
                        command.Parameters.Add("?", OleDbType.Double);

                        foreach (var data in dataList)
                        {
                            command.Parameters[0].Value = nextId++;
                            command.Parameters[1].Value = (int)ID_Klimaregion;
                            command.Parameters[2].Value = data.Temperature;
                            command.Parameters[3].Value = data.Sol_nord;
                            command.Parameters[4].Value = data.Sol_sued;
                            command.Parameters[5].Value = data.Sol_ost;
                            command.Parameters[6].Value = data.Sol_west;
                            command.Parameters[7].Value = data.GlobalIrradiance;
                            command.Parameters[8].Value = data.DirectIrradiance;
                            command.Parameters[9].Value = data.DiffuseIrradiance;
                            command.Parameters[10].Value = data.WE;
                            command.Parameters[11].Value = data.TagTyp_W;
                            command.Parameters[12].Value = data.TagTyp_NW;
                            command.Parameters[13].Value = Math.Round(data.Sonnenwinkel, 1);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    string query = "INSERT INTO Tab_Solar (ID_Klimaregion, Temperatur, Sol_Nord, Sol_Sued, Sol_Ost,Sol_West,Globalstrahlung, Direktstrahlung, " +
                        "Diffusstrahlung, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?)";

                    using (OleDbCommand command = new OleDbCommand(query, connection, transaction))
                    {
                        // Parameter definieren
                        command.Parameters.Add("?", OleDbType.Integer);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);
                        command.Parameters.Add("?", OleDbType.Double);

                        foreach (var data in dataList)
                        {
                            command.Parameters[0].Value = (int)ID_Klimaregion;
                            command.Parameters[1].Value = data.Temperature;
                            command.Parameters[2].Value = data.Sol_nord;
                            command.Parameters[3].Value = data.Sol_sued;
                            command.Parameters[4].Value = data.Sol_ost;
                            command.Parameters[5].Value = data.Sol_west;
                            command.Parameters[6].Value = data.GlobalIrradiance;
                            command.Parameters[7].Value = data.DirectIrradiance;
                            command.Parameters[8].Value = data.DiffuseIrradiance;
                            command.Parameters[9].Value = data.Sonnenwinkel > 0 ? Math.Round(data.Sonnenwinkel, 1) : 0;
                            command.ExecuteNonQuery();
                        }
                    }

                }

                transaction.Commit();
            }
        }
    }
}

public static class Nominatim
{
    private static readonly HttpClient client = new HttpClient();

    /// <summary>
    /// Wandelt einen Ortsnamen in Koordinaten um (via OpenStreetMap Nominatim API).
    /// </summary>
    public static async Task<(bool Success, double Lat, double Lon, string DisplayName, string Error)> TryGetCoordinatesAsync(string query)
    {
        try
        {
            // WICHTIG: Nominatim verlangt einen User-Agent! Sonst Fehler 403.
            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd("CSharp_EpwTool_Demo/1.0"))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; EpwTool/1.0)");
            }

            // URL encoding für Leerzeichen etc. (Berlin Alexanderplatz -> Berlin%20Alexanderplatz)
            string encodedQuery = System.Net.WebUtility.UrlEncode(query);
            string url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&limit=1";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            // Die API gibt eine Liste zurück (auch wenn limit=1)
            var results = JsonSerializer.Deserialize<List<GeoResult>>(json);
            if (results == null || results.Count == 0)
                return (false, 0, 0, null, $"Ort '{query}' konnte nicht gefunden werden.");
            var best = results[0];
            double lat = double.Parse(best.LatString, CultureInfo.InvariantCulture);
            double lon = double.Parse(best.LonString, CultureInfo.InvariantCulture);
            return (true, lat, lon, best.DisplayName, null);
        }
        catch (Exception ex)
        {
            return (false, 0, 0, null, ex.Message);
        }
    }
}
