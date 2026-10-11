using System;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Probelesung des Optionendialogs „Format und Vorschau“</b>: die ganze Datei unter den
    /// gewählten Optionen gelesen und geprüft — wie viele Datenzeilen, welches Raster daraus folgt
    /// (oder warum keines), und die Reihe als Jahresbild. Sie schreibt nichts und ändert nichts an
    /// der Importkette; der Dialog zeigt sie unter der Vorschautabelle.
    /// </summary>
    public sealed class GanglinienProbe
    {
        /// <summary>Gelesene Datenzeilen (Werte der Wertspalte, ohne Kopfzeile).</summary>
        public int Datenzeilen;

        /// <summary>
        /// Die nicht leeren Zeilen ab der ersten Datenzeile bis zum Dateiende — die Vorgabe des Feldes
        /// „Zeilen zu importieren“ (bei einer Jahresreihe ihr Raster, 8 760 oder 35 040).
        /// </summary>
        public int ZeilenAbStart;

        /// <summary>Das Raster der geprüften Reihe; <see cref="GanglinienRaster.Unbekannt"/> ohne Erfolg.</summary>
        public GanglinienRaster Raster = GanglinienRaster.Unbekannt;

        /// <summary>Die geprüfte Reihe im Raster der Datei [kW]; leer ohne Erfolg.</summary>
        public double[] WerteKw = Array.Empty<double>();

        /// <summary>
        /// Die gelesene Reihe in der Einheit des <see cref="Format"/>s (bei der Ganglinie
        /// <see cref="WerteKw"/>); leer ohne Erfolg.
        /// </summary>
        public double[] Werte => WerteKw;

        /// <summary>Die Formatbeschreibung, nach der geprüft und gezeichnet wurde (IM-4).</summary>
        public GanglinienImportFormat Format = GanglinienImportFormat.Ganglinie;

        /// <summary>Warum die Reihe zu keinem Raster passt oder nicht lesbar ist; leer bei Erfolg.</summary>
        public string Grund = "";

        /// <summary>Die Reihe als Jahresverlauf (Stundenmittel); <c>null</c> ohne Erfolg.</summary>
        public Zeichenmodell Modell;

        /// <summary>Passt die Reihe zu einem Raster (bei einem Sonderformat: zum Format)?</summary>
        public bool Erfolgreich => Modell != null;

        /// <summary>
        /// Die Kennzahlen der Reihe, wie das Format sie verlangt: Jahressumme
        /// (<see cref="GanglinienImportFormat.Summe"/>) und Höchstleistung
        /// (<see cref="GanglinienImportFormat.Nennleistung"/>); leer ohne Erfolg oder ohne beide.
        /// </summary>
        public string Kennzahlen
        {
            get
            {
                if (!Erfolgreich || WerteKw.Length == 0 || Format == null) return "";
                var teile = new System.Collections.Generic.List<string>();
                if (Format.Summe)
                {
                    double stunden = Raster == GanglinienRaster.Viertelstunde ? 0.25 : 1.0;
                    double summe = 0;
                    foreach (double w in WerteKw) summe += w * stunden;
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_SUMME,
                                            summe.ToString("N0", CultureInfo.CurrentCulture), Format.Einheit + "h"));
                }
                if (Format.Nennleistung)
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_NENNLEISTUNG,
                                            WerteKw.Max().ToString("N1", CultureInfo.CurrentCulture), Format.Einheit));
                return string.Join(" \u00b7 ", teile);
            }
        }

        /// <summary>
        /// Die Zeile unter der Vorschau: „n Datenzeilen gelesen → Raster Stunde (8 760 Werte)“ oder
        /// „n Datenzeilen gelesen → kein Raster: Grund“.
        /// </summary>
        public string Zeile
        {
            get
            {
                string n = Datenzeilen.ToString("N0", CultureInfo.CurrentCulture);
                bool sonder = Format != null && !Format.IstGanglinie;
                if (!Erfolgreich)
                    return string.Format(CultureInfo.CurrentCulture,
                        sonder ? MyResource.Resource.IMPORT_PROBE_ABGELEHNT : MyResource.Resource.IMPORT_PROBE_KEIN_RASTER,
                        n, Grund);
                if (sonder)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_WERTE, n,
                                         WerteKw.Length.ToString("N0", CultureInfo.CurrentCulture), Format.Einheit);
                string raster = Raster == GanglinienRaster.Viertelstunde
                    ? MyResource.Resource.IMPORT_PROBE_VIERTEL
                    : MyResource.Resource.IMPORT_PROBE_STUNDE;
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_RASTER, n, raster,
                                     WerteKw.Length.ToString("N0", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        /// Liest und prüft die Datei unter den gewählten Optionen — derselbe Weg wie die Importkette
        /// (<see cref="GanglinienDatei.Lies"/>, <see cref="GanglinienPruefung.Pruefe"/>). Nie <c>null</c>,
        /// wirft nicht.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="optionen">Die gewählten Leseoptionen.</param>
        public static GanglinienProbe Lies(string pfad, GanglinienImportOptionen optionen)
            => Lies(pfad, optionen, null);

        /// <summary>
        /// Wie <see cref="Lies(string, GanglinienImportOptionen)"/> nach einer Formatbeschreibung
        /// (IM-4). Die Ganglinie (oder <c>null</c>) geht den Weg der Importkette; ein Sonderformat
        /// liest die Wertspalte ohne Einheits- und Rasterprüfung, prüft die feste
        /// <see cref="GanglinienImportFormat.Werteanzahl"/> und die eigene
        /// <see cref="GanglinienImportFormat.Pruefung"/> und zeichnet nach seiner Bildart. Eine
        /// Ausnahme steht im Ausnahmeprotokoll und als Grund in der Probe. Nie <c>null</c>.
        /// </summary>
        /// <param name="pfad">Die Quelldatei.</param>
        /// <param name="optionen">Die gewählten Leseoptionen.</param>
        /// <param name="format">Die Formatbeschreibung; <c>null</c> = Ganglinie.</param>
        public static GanglinienProbe Lies(string pfad, GanglinienImportOptionen optionen,
                                           GanglinienImportFormat format)
        {
            if (format != null && !format.IstGanglinie) return LiesFormat(pfad, optionen, format);
            var probe = new GanglinienProbe();
            if (format != null) probe.Format = format;
            try
            {
                GanglinienImportOptionen o = optionen ?? new GanglinienImportOptionen();
                GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, o);
                probe.Datenzeilen = roh.Werte?.Length ?? 0;
                probe.ZeilenAbStart = roh.ZeilenAbStart;
                if (!roh.Erfolgreich)
                {
                    probe.Grund = Grundtext(roh.Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler));
                    return probe;
                }

                GanglinienPruefErgebnis geprueft = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
                {
                    Rohwerte = roh.Werte,
                    Zeitstempel = roh.Zeitstempel,
                    Einheit = o.Einheit,
                    DeklariertesRaster = o.Raster,
                    Konvention = o.Konvention
                });
                if (!geprueft.Erfolgreich)
                {
                    probe.Grund = Grundtext(geprueft.Protokoll.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler));
                    return probe;
                }

                probe.Raster = geprueft.Zielraster;
                probe.WerteKw = geprueft.Werte;
                double[] stunden = probe.Raster == GanglinienRaster.Viertelstunde
                    ? new SimulationControl().Viertelstunden_zu_Stundenwerte_Mittelwert(probe.WerteKw)
                    : probe.WerteKw;
                probe.Modell = ChartRenderer.JahresverlaufModell("", stunden,
                    MyResource.Resource.CHART_ACHSE_LEISTUNG, Farbrolle.BEDARF);
                return probe;
            }
            catch (Exception ex)
            {
                probe.Modell = null;
                probe.Grund = ex.Message;
                return probe;
            }
        }

        /// <summary>Der Weg eines Sonderformats (Quellprofil, Spotpreis, TWW-Messreihe).</summary>
        private static GanglinienProbe LiesFormat(string pfad, GanglinienImportOptionen optionen,
                                                  GanglinienImportFormat format)
        {
            var probe = new GanglinienProbe { Format = format };
            try
            {
                GanglinienImportOptionen o = optionen ?? new GanglinienImportOptionen();
                GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, o);
                double[] werte = roh.Werte ?? Array.Empty<double>();
                probe.Datenzeilen = werte.Length;
                probe.ZeilenAbStart = roh.ZeilenAbStart;
                if (!roh.Erfolgreich)
                {
                    probe.Grund = Grundtext(roh.Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler));
                    return probe;
                }

                // Die feste Werteanzahl: eine andere Anzahl wird BENANNT abgelehnt - nie aufgefüllt,
                // nie still gekürzt (ein erfundenes Temperaturprofil wäre schlimmer als keines).
                if (format.Werteanzahl > 0 && werte.Length != format.Werteanzahl)
                {
                    probe.Grund = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_WERTEANZAHL,
                        werte.Length.ToString("N0", CultureInfo.CurrentCulture),
                        format.Werteanzahl.ToString("N0", CultureInfo.CurrentCulture));
                    return probe;
                }

                string eigen = format.Pruefung?.Invoke(werte) ?? "";
                if (!string.IsNullOrEmpty(eigen))
                {
                    probe.Grund = eigen;
                    return probe;
                }

                probe.WerteKw = werte;
                probe.Modell = format.Bildzeichner != null
                    ? format.Bildzeichner(werte)
                    : ChartRenderer.JahresverlaufModell("", format.Bildreihe?.Invoke(werte) ?? werte,
                                                        format.Achsentitel, format.Farbrolle);
                if (probe.Modell == null) probe.WerteKw = Array.Empty<double>();
                return probe;
            }
            catch (Exception ex)
            {
                Ausnahmeprotokoll.Vermerken("Probelesung \u201e" + System.IO.Path.GetFileName(pfad ?? "")
                    + "\u201c (" + format.Kennung + ") abgelehnt: " + ex.GetType().FullName + ": " + ex.Message);
                probe.Modell = null;
                probe.WerteKw = Array.Empty<double>();
                probe.Grund = ex.Message;
                return probe;
            }
        }

        private static string Grundtext(PruefMeldung fehler)
        {
            string text = fehler == null ? "" : GanglinienProtokollText.Text(fehler);
            return string.IsNullOrWhiteSpace(text)
                ? GanglinienProtokollText.Text(new PruefMeldung(PruefStufe.Fehler, GanglinienDatei.SchluesselDateiLeer))
                : text;
        }
    }
}
