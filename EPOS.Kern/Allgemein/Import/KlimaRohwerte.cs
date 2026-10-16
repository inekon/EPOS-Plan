using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Stunde der ROHWERTE eines Klimadatensatzes (Auftrag KD-4): jede Größe, wie die Quelle sie liefert, in
    /// Hauseinheit. <c>null</c> heißt „die Quelle führt die Größe nicht“ — nie 0. Ziel ist
    /// <see cref="KlimaRohwerteSchema.TAB_ROH"/>; kein Rechenweg liest diese Werte.
    /// </summary>
    public sealed class KlimaRohstunde
    {
        /// <summary>Stunde 1 … 8760 im Raster der Quelle (TRY: MEZ ohne Sommerzeit, PVGIS: UTC).</summary>
        public int Stunde;

        /// <summary>Lufttemperatur [°C] — TRY <c>t</c>, PVGIS <c>T2m</c>.</summary>
        public double? Temperatur;

        /// <summary>Luftdruck [hPa] — TRY <c>p</c>, PVGIS <c>SP</c> (Pa, durch 100 geteilt).</summary>
        public double? Luftdruck_hPa;

        /// <summary>Windrichtung [Grad] — TRY <c>WR</c>, PVGIS <c>WD10m</c>.</summary>
        public double? Windrichtung_Grad;

        /// <summary>Windgeschwindigkeit [m/s] — TRY <c>WG</c>, PVGIS <c>WS10m</c>.</summary>
        public double? Windgeschwindigkeit_m_s;

        /// <summary>Bedeckungsgrad [Achtel] — TRY <c>N</c>.</summary>
        public double? Bedeckungsgrad_Achtel;

        /// <summary>Wasserdampfgehalt [g/kg] — TRY <c>x</c>.</summary>
        public double? Wasserdampfgehalt_g_kg;

        /// <summary>Relative Feuchte [%] — TRY <c>RF</c>, PVGIS <c>RH</c>.</summary>
        public double? Luftfeuchte_Prozent;

        /// <summary>Direktstrahlung horizontal [W/m²] — TRY <c>B</c>.</summary>
        public double? DirektHorizontal;

        /// <summary>Direktstrahlung normal [W/m²] — PVGIS <c>Gb(n)</c>.</summary>
        public double? DirektNormal;

        /// <summary>Diffusstrahlung horizontal [W/m²] — TRY <c>D</c>, PVGIS <c>Gd(h)</c> (ohne die Regel unter 5°).</summary>
        public double? DiffusHorizontal;

        /// <summary>Globalstrahlung horizontal [W/m²] — PVGIS <c>G(h)</c>; TRY führt sie nicht (B + D ist gerechnet).</summary>
        public double? GlobalHorizontal;

        /// <summary>Atmosphärische Gegenstrahlung [W/m²] — TRY <c>A</c>, PVGIS <c>IR(h)</c>.</summary>
        public double? Gegenstrahlung;

        /// <summary>Terrestrische Ausstrahlung [W/m²] — TRY <c>E</c> (nach Vorzeichen der Datei).</summary>
        public double? Ausstrahlung;

        /// <summary>Qualitätskennung der Strahlung — TRY <c>IL</c>.</summary>
        public int? QualitaetStrahlung;
    }

    /// <summary>Die Rohreihe eines Datensatzes samt Zeitbezug ihres Rasters.</summary>
    public sealed class KlimaRohreihe
    {
        /// <summary><see cref="KlimaRohwerteSchema.ZEITBEZUG_MEZ"/> oder <see cref="KlimaRohwerteSchema.ZEITBEZUG_UTC"/>.</summary>
        public string Zeitbezug = "";

        /// <summary>Die Stunden, aufsteigend nach <see cref="KlimaRohstunde.Stunde"/>.</summary>
        public List<KlimaRohstunde> Stunden = new List<KlimaRohstunde>();

        /// <summary>Pascal je Hektopascal.</summary>
        public const double PA_JE_HPA = 100.0;

        /// <summary>
        /// Die Rohreihe einer PVGIS-TMY-Antwort: die Stunden in der Reihenfolge der Antwort (UTC), Stunde = Index + 1,
        /// höchstens 8760. Die Werte kommen aus den Feldern, wie die Antwort sie führt — vor jeder Rechnung des Imports.
        /// </summary>
        public static KlimaRohreihe AusPvgis(IReadOnlyList<TmyHourlyData> stunden)
        {
            var r = new KlimaRohreihe { Zeitbezug = KlimaRohwerteSchema.ZEITBEZUG_UTC };
            if (stunden == null) return r;
            int n = System.Math.Min(stunden.Count, KlimaRohwerteSchema.STUNDE_MAX);
            for (int i = 0; i < n; i++)
            {
                TmyHourlyData s = stunden[i];
                if (s == null) continue;
                r.Stunden.Add(new KlimaRohstunde
                {
                    Stunde = i + 1,
                    Temperatur = s.Temperature,
                    Luftdruck_hPa = s.Luftdruck_Pa.HasValue ? s.Luftdruck_Pa.Value / PA_JE_HPA : (double?)null,
                    Windrichtung_Grad = s.Windrichtung,
                    Windgeschwindigkeit_m_s = s.WindSpeed,
                    Luftfeuchte_Prozent = s.Humidity,
                    DirektNormal = s.DirectIrradiance,
                    DiffusHorizontal = s.DiffuseIrradiance,
                    GlobalHorizontal = s.GlobalIrradiance,
                    Gegenstrahlung = s.Gegenstrahlung
                });
            }
            return r;
        }
    }

    /// <summary>
    /// Die HERKUNFT einer importierten Region (Auftrag KD-4): Standorthöhe, Bezugspunkt und gelesene Ausführung —
    /// Ziel sind die Regionsspalten von <see cref="KlimaRohwerteSchema"/>.
    /// </summary>
    public sealed class KlimaRegionsherkunft
    {
        public double? StandortHoeheM;
        public string BezugspunktArt = "";
        public string BezugspunktName = "";
        public double? BezugspunktLatitude;
        public double? BezugspunktLongitude;
        public double? BezugspunktHoeheM;
        public double? EntfernungKm;
        public string Ausfuehrung = "";
    }
}
