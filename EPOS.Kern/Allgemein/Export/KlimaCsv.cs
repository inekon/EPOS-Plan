using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Daten EINER Region für den Klimaexport</b> (Auftrag KD-5, Entscheid KD-Q6: eine Region je Datei):
    /// Herkunft aus <c>Tab_Klimaregion_STAMM</c>, die Rechenspalten aus <c>Tab_Solar_STAMM</c> und — nur bei
    /// <c>Rohdaten = 1</c> — die Rohwerte aus <see cref="KlimaRohwerteSchema.TAB_ROH"/>. Ohne Datenbank, damit
    /// Schreiber und Vorschau ohne sie laufen; gelesen wird sie in <c>KlimaExportCtrl</c>.
    /// </summary>
    public sealed class KlimaExportDaten
    {
        public int IdRegion;
        public string Name = "";
        public string Quelle = "";
        public string Ausfuehrung = "";
        public string Szenario = "";
        public int? Bezugsjahr;
        public double? Latitude;
        public double? Longitude;
        public double? StandortHoeheM;
        public string BezugspunktName = "";
        public double? BezugspunktLatitude;
        public double? BezugspunktLongitude;
        public double? BezugspunktHoeheM;

        /// <summary>Trägt die Region Rohwerte (<c>Rohdaten = 1</c> und gefüllte Rohtabelle)?</summary>
        public bool Rohdaten;

        /// <summary>Raster der Rohwerte: <see cref="KlimaRohwerteSchema.ZEITBEZUG_MEZ"/> (TRY, ungedreht) oder <see cref="KlimaRohwerteSchema.ZEITBEZUG_UTC"/> (PVGIS).</summary>
        public string RohZeitbezug = KlimaRohwerteSchema.ZEITBEZUG_MEZ;

        /// <summary>
        /// Die Rechenspalten je Datenbankspalte (<see cref="KlimaExportGroesse.Spalte"/>), 8760 Werte. Raster UTC: Das ist
        /// der Vertrag von <c>Tab_Solar_STAMM</c> (Konzept Klimadatenquellen § 5 — TRY wird beim Import gedreht,
        /// <c>utc[u] = mez[(u + 1) % 8760]</c>, PVGIS liefert UTC).
        /// </summary>
        public Dictionary<string, double[]> Rechenwerte = new Dictionary<string, double[]>(StringComparer.Ordinal);

        /// <summary>Die Rohspalten je Spaltenname, 8760 Werte im Raster <see cref="RohZeitbezug"/>; NULL der Datenbank = <see cref="double.NaN"/>.</summary>
        public Dictionary<string, double[]> Rohwerte = new Dictionary<string, double[]>(StringComparer.Ordinal);

        /// <summary>Gibt es überhaupt Stundenwerte?</summary>
        public bool HatStunden => Rechenwerte.Values.Any(w => w != null && w.Length > 0);

        /// <summary>
        /// Kann die Größe exportiert werden? Leerer Grund = ja. Rohwerte ohne Rohdaten nennen „Region ohne Rohdaten
        /// importiert“, eine Rohspalte nur aus NULL nennt „Quelle führt die Größe nicht“.
        /// </summary>
        public string Sperrgrund(KlimaExportGroesse g)
        {
            if (g == null) return R.KEXP_GRUND_NICHT_GEFUEHRT;
            switch (g.Art)
            {
                case KlimaWertart.Rohwert:
                    if (!Rohdaten) return R.KEXP_GRUND_OHNE_ROHDATEN;
                    return Rohwerte.TryGetValue(g.Spalte, out double[] r) && r != null && r.Any(double.IsFinite)
                        ? "" : R.KEXP_GRUND_NICHT_GEFUEHRT;
                case KlimaWertart.Abgeleitet:
                    return Rechenwerte.ContainsKey("Temperatur") && Rechenwerte.ContainsKey("Luftfeuchte")
                        ? "" : R.KEXP_GRUND_NICHT_GEFUEHRT;
                default:
                    return Rechenwerte.TryGetValue(g.Spalte, out double[] w) && w != null && w.Length > 0
                        ? "" : R.KEXP_GRUND_NICHT_GEFUEHRT;
            }
        }
    }

    /// <summary>
    /// <b>Der Schreiber des Klimaexports</b> (Auftrag KD-5, Konzept Klimadatenimport § 5) — neben
    /// <see cref="ZeitreihenCsv"/>, mit dessen Zahlformat <c>0.0##</c>, der Entschärfung der Köpfe und der Kopfform
    /// „Name [Einheit]“. Wählbar sind Trennzeichen, Dezimalzeichen, Zeitspalte, Zeitbezug, Kopfart und ein
    /// Herkunftsblock (<see cref="KlimaExportOptionen"/>). Geschrieben wird immer das Gemeinjahr mit 8760 Stunden.
    /// </summary>
    public static class KlimaCsv
    {
        /// <summary>Stunden im Gemeinjahr.</summary>
        public const int STUNDEN = 8760;

        /// <summary>Zeilen der Vorschau (Konzept § 5: die ersten fünf Datenzeilen).</summary>
        public const int VORSCHAU_ZEILEN = 5;

        /// <summary>Magnus-Koeffizienten über Wasser nach Sonntag (1990), wie sie der DWD verwendet: a = 17,62, b = 243,12 °C.</summary>
        public const double MAGNUS_A = 17.62;

        /// <summary>Siehe <see cref="MAGNUS_A"/>.</summary>
        public const double MAGNUS_B = 243.12;

        private static readonly int[] TAGE_MONAT = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Die ganze Datei als Text (Zeilenende CRLF). Wirft bei widersprüchlichem Format oder leerer Wahl.</summary>
        public static string Text(KlimaExportDaten daten, KlimaExportOptionen optionen)
            => Schreiben(daten, optionen, STUNDEN);

        /// <summary>
        /// Die Vorschau: Herkunftsblock und Kopf wie in der Datei, danach die ersten <paramref name="zeilen"/>
        /// Datenzeilen — derselbe Weg wie <see cref="Text"/>, nur früher beendet.
        /// </summary>
        public static string Vorschau(KlimaExportDaten daten, KlimaExportOptionen optionen, int zeilen = VORSCHAU_ZEILEN)
            => Schreiben(daten, optionen, Math.Max(0, Math.Min(zeilen, STUNDEN)));

        /// <summary>
        /// Die Größen, die tatsächlich geschrieben werden: die gewählten in Katalogreihenfolge, ohne Unbekannte und ohne
        /// gesperrte (bei <c>Rohdaten = 0</c> bleiben also nur die Rechenwerte und der Taupunkt).
        /// </summary>
        public static IReadOnlyList<KlimaExportGroesse> Spalten(KlimaExportDaten daten, KlimaExportOptionen optionen)
        {
            var gewaehlt = new HashSet<string>(optionen?.Parameter ?? new List<string>(), StringComparer.Ordinal);
            return KlimaExportOptionen.Groessen
                .Where(g => gewaehlt.Contains(g.Schluessel) && daten != null && daten.Sperrgrund(g).Length == 0)
                .ToList();
        }

        /// <summary>
        /// Die Werte einer Größe im Raster des gewählten Zeitbezugs: Rechenwerte stehen in UTC, Rohwerte im Raster ihrer
        /// Quelle; der Taupunkt entsteht aus Temperatur und Feuchte der Rechenwerte.
        /// </summary>
        public static double[] Werte(KlimaExportDaten daten, KlimaExportGroesse g, KlimaZeitbezug ziel)
        {
            switch (g.Art)
            {
                case KlimaWertart.Rohwert:
                    return Verschieben(Gemeinjahr(daten.Rohwerte[g.Spalte]), Bezug(daten.RohZeitbezug), ziel);
                case KlimaWertart.Abgeleitet:
                {
                    double[] t = Gemeinjahr(daten.Rechenwerte["Temperatur"]);
                    double[] rf = Gemeinjahr(daten.Rechenwerte["Luftfeuchte"]);
                    var tp = new double[STUNDEN];
                    for (int i = 0; i < STUNDEN; i++) tp[i] = Taupunkt(t[i], rf[i]);
                    return Verschieben(tp, KlimaZeitbezug.Utc, ziel);
                }
                default:
                    return Verschieben(Gemeinjahr(daten.Rechenwerte[g.Spalte]), KlimaZeitbezug.Utc, ziel);
            }
        }

        /// <summary>Der Zeitbezug eines Schlüssels der Datenbank (<c>UTC</c>/<c>MEZ</c>); unbekannt zählt als MEZ (TRY-Raster).</summary>
        public static KlimaZeitbezug Bezug(string schluessel)
            => string.Equals(schluessel, KlimaRohwerteSchema.ZEITBEZUG_UTC, StringComparison.OrdinalIgnoreCase)
                ? KlimaZeitbezug.Utc : KlimaZeitbezug.Mez;

        /// <summary>
        /// <b>Die Umrechnung des Zeitbezugs</b> um eine Stunde (MEZ = UTC + 1, ganzjährig — TRY und Export kennen keine
        /// Sommerzeit). Die Stunde <c>i</c> in UTC ist die Stunde <c>i + 1</c> in MEZ, also
        /// <c>mez[j] = utc[(j − 1) mod 8760]</c> und <c>utc[j] = mez[(j + 1) mod 8760]</c> — dieselbe Drehung wie beim
        /// TRY-Import in die Rechentabelle (Konzept Klimadatenquellen § 5).
        ///
        /// <para><b>Warum zyklisch:</b> Das Gemeinjahr hat genau 8760 Stunden; die Stunde, die beim Verschieben über den
        /// Jahreswechsel fällt (31.12. 23 Uhr UTC = 1.1. 0 Uhr MEZ des Folgejahres), gibt es im typischen Jahr nicht.
        /// Das Testreferenzjahr und das TMY sind als periodisches Jahr gedacht — der Rechenkern liest sie ebenso —,
        /// darum rückt sie an den Anfang statt die Reihe zu kürzen oder mit einer leeren Stunde aufzufüllen.</para>
        /// </summary>
        public static double[] Verschieben(double[] werte, KlimaZeitbezug von, KlimaZeitbezug nach)
        {
            if (werte == null) return null;
            if (von == nach) return (double[])werte.Clone();
            int n = werte.Length;
            var aus = new double[n];
            int versatz = von == KlimaZeitbezug.Utc ? -1 : +1; // UTC → MEZ: j − 1; MEZ → UTC: j + 1
            for (int j = 0; j < n; j++) aus[j] = werte[((j + versatz) % n + n) % n];
            return aus;
        }

        /// <summary>
        /// <b>Der Taupunkt</b> [°C] aus Lufttemperatur [°C] und relativer Feuchte [%] nach der Magnus-Formel über Wasser:
        /// <c>γ = ln(RF/100) + a·t/(b + t)</c>, <c>τ = b·γ/(a − γ)</c> mit a = 17,62 und b = 243,12 °C
        /// (D. Sonntag, Z. Meteorol. 40 (1990) 340–344; so im DWD-Leitfaden für Feuchtemessungen). Eine GERECHNETE Größe,
        /// nie gespeichert (Konzept § 2). Feuchte ≤ 0 oder ein fehlender Wert ergibt <see cref="double.NaN"/> (leere Zelle);
        /// über 100 % wird auf 100 % begrenzt.
        /// </summary>
        public static double Taupunkt(double temperatur, double feuchteProzent)
        {
            if (!double.IsFinite(temperatur) || !double.IsFinite(feuchteProzent) || feuchteProzent <= 0) return double.NaN;
            double rf = Math.Min(feuchteProzent, 100.0) / 100.0;
            double gamma = Math.Log(rf) + MAGNUS_A * temperatur / (MAGNUS_B + temperatur);
            return MAGNUS_B * gamma / (MAGNUS_A - gamma);
        }

        /// <summary>Ein Dateiname aus dem Regionsnamen (Muster <see cref="ZeitreihenCsv.Dateistamm"/>).</summary>
        public static string Dateiname(KlimaExportDaten daten)
            => string.Format(CultureInfo.InvariantCulture, R.KEXP_DATEI, ZeitreihenCsv.Dateistamm(daten?.Name));

        // =====================================================================================================

        private static string Schreiben(KlimaExportDaten daten, KlimaExportOptionen o, int zeilen)
        {
            if (daten == null) throw new ArgumentNullException(nameof(daten));
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (o.Widerspruechlich) throw new ArgumentException(R.KEXP_DZ_KONFLIKT, nameof(o));
            IReadOnlyList<KlimaExportGroesse> spalten = Spalten(daten, o);
            if (spalten.Count == 0) throw new ArgumentException(R.KEXP_KEINE_PARAMETER, nameof(o));

            string sep = o.TrennzeichenText;
            CultureInfo kultur = o.Zahlkultur;
            const string NL = "\r\n";
            var sb = new StringBuilder(zeilen * 16 * (spalten.Count + 2) + 1024);

            if (o.Herkunft) Herkunftsblock(sb, daten, o, spalten, sep);

            // Spaltenkopf: Zeitspalten, dann je Größe „Name [Einheit]“ (eine Zeile) bzw. Name und Einheitenzeile.
            List<string> zeitkoepfe = Zeitkoepfe(o.Zeitspalte);
            if (o.Kopfart != KlimaKopfart.Ohne)
            {
                var namen = new List<string>(zeitkoepfe);
                var einheiten = zeitkoepfe.Select(_ => "").ToList();
                foreach (KlimaExportGroesse g in spalten)
                {
                    string name = ZeitreihenCsv.Entschaerft(g.Kopfname, sep);
                    string einheit = ZeitreihenCsv.Entschaerft(g.Einheit, sep);
                    if (o.Kopfart == KlimaKopfart.EineZeile)
                        namen.Add(einheit.Length > 0 ? name + " [" + einheit + "]" : name);
                    else
                    {
                        namen.Add(name);
                        einheiten.Add(einheit);
                    }
                }
                sb.Append(string.Join(sep, namen)).Append(NL);
                if (o.Kopfart == KlimaKopfart.MitEinheitenzeile) sb.Append(string.Join(sep, einheiten)).Append(NL);
            }

            var reihen = spalten.Select(g => Werte(daten, g, o.Zeitbezug)).ToList();
            int jahr = IsoJahr(daten);
            string format = o.VolleGenauigkeit ? "R" : ZeitreihenCsv.ZAHLFORMAT;
            for (int i = 0; i < zeilen; i++)
            {
                bool erste = true;
                foreach (string z in Zeitzellen(o.Zeitspalte, o.Zeitbezug, i, jahr))
                {
                    if (!erste) sb.Append(sep);
                    sb.Append(z);
                    erste = false;
                }
                for (int k = 0; k < reihen.Count; k++)
                {
                    if (!erste) sb.Append(sep);
                    erste = false;
                    double w = reihen[k][i];
                    if (!double.IsFinite(w)) continue;
                    sb.Append(spalten[k].Ganzzahl ? Math.Round(w).ToString("0", kultur) : w.ToString(format, kultur));
                }
                sb.Append(NL);
            }
            return sb.ToString();
        }

        /// <summary>Der Herkunftsblock: <c>#</c>-Zeilen mit Region, Quelle, Ausführung, Koordinaten, Höhe, Bezugspunkt, Zeitbezug, Lizenz.</summary>
        private static void Herkunftsblock(StringBuilder sb, KlimaExportDaten d, KlimaExportOptionen o,
                                           IReadOnlyList<KlimaExportGroesse> spalten, string sep)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            void Zeile(string schluessel, string wert)
            {
                if (string.IsNullOrWhiteSpace(wert)) return;
                // Die #-Zeilen tragen das Trennzeichen nicht: Ein Leser, der Kommentare nicht kennt, sieht eine Spalte.
                sb.Append("# ").Append(ZeitreihenCsv.Entschaerft(schluessel, sep)).Append(": ")
                  .Append(ZeitreihenCsv.Entschaerft(wert, sep)).Append("\r\n");
            }
            sb.Append("# ").Append(R.KEXP_KOPF_EXPORT).Append("\r\n");
            Zeile(R.KEXP_KOPF_REGION, d.Name);
            Zeile(R.KEXP_KOPF_QUELLE, string.Join(" ", new[] { d.Quelle, d.Szenario,
                d.Bezugsjahr.HasValue ? d.Bezugsjahr.Value.ToString(inv) : "" }.Where(s => !string.IsNullOrWhiteSpace(s))));
            Zeile(R.KEXP_KOPF_AUSFUEHRUNG, d.Ausfuehrung);
            if (d.Latitude.HasValue && d.Longitude.HasValue)
                Zeile(R.KEXP_KOPF_KOORDINATEN, d.Latitude.Value.ToString("0.0####", inv) + " / " + d.Longitude.Value.ToString("0.0####", inv));
            if (d.StandortHoeheM.HasValue) Zeile(R.KEXP_KOPF_HOEHE, d.StandortHoeheM.Value.ToString("0.#", inv) + " m");
            if (!string.IsNullOrWhiteSpace(d.BezugspunktName) || d.BezugspunktLatitude.HasValue)
            {
                string bp = d.BezugspunktName ?? "";
                if (d.BezugspunktLatitude.HasValue && d.BezugspunktLongitude.HasValue)
                    bp += " (" + d.BezugspunktLatitude.Value.ToString("0.0####", inv) + " / " +
                          d.BezugspunktLongitude.Value.ToString("0.0####", inv) +
                          (d.BezugspunktHoeheM.HasValue ? ", " + d.BezugspunktHoeheM.Value.ToString("0.#", inv) + " m" : "") + ")";
                Zeile(R.KEXP_KOPF_BEZUGSPUNKT, bp.Trim());
            }
            Zeile(R.KEXP_KOPF_ZEITBEZUG, o.Zeitbezug == KlimaZeitbezug.Utc ? R.KEXP_ZB_UTC : R.KEXP_ZB_MEZ);
            Zeile(R.KEXP_KOPF_ZEITSTEMPEL, R.KEXP_ZEITSTEMPEL_REGEL);
            if (spalten.Any(g => g.Gerechnet))
                Zeile(R.KEXP_GERECHNET_KOPF, string.Join(", ", spalten.Where(g => g.Gerechnet).Select(g => g.Kopfname)));
            if (spalten.Any(g => g.Art == KlimaWertart.Abgeleitet)) Zeile(R.KEXP_GERECHNET_KOPF, R.KEXP_TAUPUNKT_REGEL);
            Zeile(R.KEXP_KOPF_LIZENZ, Lizenz(d.Quelle));
        }

        /// <summary>Die Rechtsangabe der Quelle (Konzept § 5).</summary>
        private static string Lizenz(string quelle)
        {
            string q = (quelle ?? "").ToUpperInvariant();
            if (q.Contains("PVGIS")) return R.KEXP_LIZENZ_PVGIS;
            if (q.Contains("TRY") || q.Contains("DWD")) return R.KEXP_LIZENZ_DWD;
            return "";
        }

        private static List<string> Zeitkoepfe(KlimaZeitspalte z) => z switch
        {
            KlimaZeitspalte.Stunde => new List<string> { R.KEXP_SP_STUNDE },
            KlimaZeitspalte.DatumUhrzeit => new List<string> { R.KEXP_SP_DATUM },
            KlimaZeitspalte.StundeUndDatum => new List<string> { R.KEXP_SP_STUNDE, R.KEXP_SP_DATUM },
            KlimaZeitspalte.Iso => new List<string> { R.KEXP_SP_ISO },
            _ => new List<string>()
        };

        private static IEnumerable<string> Zeitzellen(KlimaZeitspalte z, KlimaZeitbezug bezug, int i, int jahr)
        {
            if (z == KlimaZeitspalte.Keine) yield break;
            if (z == KlimaZeitspalte.Stunde || z == KlimaZeitspalte.StundeUndDatum)
                yield return (i + 1).ToString(CultureInfo.InvariantCulture);
            if (z == KlimaZeitspalte.Stunde) yield break;
            (int monat, int tag) = Datum(i / 24);
            int stunde = i % 24;
            if (z == KlimaZeitspalte.Iso)
                yield return string.Format(CultureInfo.InvariantCulture, "{0:0000}-{1:00}-{2:00}T{3:00}:00{4}",
                                           jahr, monat, tag, stunde, bezug == KlimaZeitbezug.Utc ? "Z" : "+01:00");
            else
                yield return string.Format(CultureInfo.InvariantCulture, "{0:00}.{1:00}. {2:00}:00", tag, monat, stunde);
        }

        /// <summary>Monat und Tag eines Tages 0 … 364 des Gemeinjahrs.</summary>
        internal static (int Monat, int Tag) Datum(int tagImJahr)
        {
            int t = tagImJahr;
            for (int m = 0; m < 12; m++)
            {
                if (t < TAGE_MONAT[m]) return (m + 1, t + 1);
                t -= TAGE_MONAT[m];
            }
            return (12, 31);
        }

        /// <summary>Das Jahr der ISO-Spalte: das Bezugsjahr der Region, sofern es ein Gemeinjahr ist, sonst <see cref="KlimaExportOptionen.ISO_JAHR_VORGABE"/>.</summary>
        internal static int IsoJahr(KlimaExportDaten d)
            => d?.Bezugsjahr is int j && j >= 1 && j <= 9999 && !DateTime.IsLeapYear(j) ? j : KlimaExportOptionen.ISO_JAHR_VORGABE;

        /// <summary>Eine Reihe auf genau 8760 Werte (fehlende Stunden = NaN, Überzählige fallen weg).</summary>
        private static double[] Gemeinjahr(double[] werte)
        {
            var aus = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++) aus[i] = werte != null && i < werte.Length ? werte[i] : double.NaN;
            return aus;
        }
    }
}
