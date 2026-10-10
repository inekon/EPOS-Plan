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

        /// <summary>Das Raster der geprüften Reihe; <see cref="GanglinienRaster.Unbekannt"/> ohne Erfolg.</summary>
        public GanglinienRaster Raster = GanglinienRaster.Unbekannt;

        /// <summary>Die geprüfte Reihe im Raster der Datei [kW]; leer ohne Erfolg.</summary>
        public double[] WerteKw = Array.Empty<double>();

        /// <summary>Warum die Reihe zu keinem Raster passt oder nicht lesbar ist; leer bei Erfolg.</summary>
        public string Grund = "";

        /// <summary>Die Reihe als Jahresverlauf (Stundenmittel); <c>null</c> ohne Erfolg.</summary>
        public Zeichenmodell Modell;

        /// <summary>Passt die Reihe zu einem Raster?</summary>
        public bool Erfolgreich => Modell != null;

        /// <summary>
        /// Die Zeile unter der Vorschau: „n Datenzeilen gelesen → Raster Stunde (8 760 Werte)“ oder
        /// „n Datenzeilen gelesen → kein Raster: Grund“.
        /// </summary>
        public string Zeile
        {
            get
            {
                string n = Datenzeilen.ToString("N0", CultureInfo.CurrentCulture);
                if (!Erfolgreich)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMPORT_PROBE_KEIN_RASTER, n, Grund);
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
        {
            var probe = new GanglinienProbe();
            try
            {
                GanglinienImportOptionen o = optionen ?? new GanglinienImportOptionen();
                GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, o);
                probe.Datenzeilen = roh.Werte?.Length ?? 0;
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

        private static string Grundtext(PruefMeldung fehler)
        {
            string text = fehler == null ? "" : GanglinienProtokollText.Text(fehler);
            return string.IsNullOrWhiteSpace(text)
                ? GanglinienProtokollText.Text(new PruefMeldung(PruefStufe.Fehler, GanglinienDatei.SchluesselDateiLeer))
                : text;
        }
    }
}
