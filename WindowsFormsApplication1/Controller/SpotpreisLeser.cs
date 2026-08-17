using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NReco.Csv;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was das Einlesen einer Spotpreisdatei ergeben hat - noch ohne jede
    /// Kalenderaufbereitung.
    /// </summary>
    public sealed class SpotDateiErgebnis
    {
        /// <summary>Die gelesenen Stundenwerte in Dateireihenfolge.</summary>
        public List<SpotStundenwert> Zeilen = new List<SpotStundenwert>();

        /// <summary>
        /// Kalenderjahr der Datei - das Jahr der ersten brauchbaren Datenzeile.
        /// 0, wenn keine gelesen werden konnte.
        /// </summary>
        public int Jahr;

        /// <summary>Gelesene Zeilen insgesamt, ohne die Kopfzeile.</summary>
        public int ZeilenGesamt;

        /// <summary>Zeilen, die nicht zerlegt werden konnten.</summary>
        public int ZeilenUnlesbar;

        /// <summary>Zeilen, deren Jahr vom <see cref="Jahr"/> der Datei abweicht.</summary>
        public int ZeilenFremdesJahr;

        /// <summary>
        /// Zeilennummern der ersten unlesbaren Zeilen (hoechstens
        /// <see cref="SpotpreisLeser.MAX_GEMELDETE_ZEILEN"/>) - fuer das
        /// Validierungsprotokoll.
        /// </summary>
        public List<int> UnlesbareZeilen = new List<int>();

        /// <summary>
        /// true, wenn eine Kopfzeile erkannt und uebersprungen wurde.
        /// </summary>
        public bool MitKopfzeile;
    }

    /// <summary>
    /// Liest eine Spotpreisdatei im Format der Bundesnetzagentur/SMARD-Ausgabe
    /// (Fachkonzept Stromspeicher 4.1 a):
    /// <c>Datum;von;Zeitzone von;bis;Zeitzone bis;Spotmarktpreis in ct/kWh</c>,
    /// Semikolon als Trenner, Dezimalkomma, Zeitzonenkuerzel CET/CEST.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bewusst ohne Abhaengigkeiten.</b> Die Klasse kennt weder
    /// <c>DataRepository</c> noch <c>MyResource</c> noch ein Formular - sie nimmt einen
    /// <see cref="TextReader"/> und liefert Zahlen. Damit ist der Einlesevorgang
    /// pruefbar, ohne dass eine Oberflaeche laeuft oder eine Datenbank existiert; die
    /// Kalenderaufbereitung uebernimmt danach
    /// <see cref="SpotreihenAufbereitung.AusStundenwerten"/> in der Engine.
    /// </para>
    /// <para>
    /// <b>Kulturregel.</b> Die Datei traegt deutsche Zahlen - unabhaengig davon, welche
    /// Windows-Einstellung der Anwender fuehrt. Geparst wird deshalb mit einer FESTEN
    /// Kultur (<c>de-DE</c>), nicht mit <c>CurrentCulture</c>; sonst laese dieselbe
    /// Datei unter en-US "0,001" als 1. Als Rueckfall wird zusaetzlich invariant
    /// versucht, damit auch eine mit Punkt geschriebene Datei durchlaeuft (Abnahme
    /// Fachkonzept 8.6: Kulturtest).
    /// </para>
    /// <para>
    /// <b>Der Parser ist NReco.Csv</b> aus <c>Allgemein\Import\CsvReader.cs</c> - kein
    /// eigener Zeilenzerleger (Umsetzungskonzept 1.3).
    /// </para>
    /// </remarks>
    public static class SpotpreisLeser
    {
        /// <summary>Feldtrenner der Quelldatei.</summary>
        public const string TRENNZEICHEN = ";";

        /// <summary>Erwartete Spaltenzahl.</summary>
        public const int SPALTEN = 6;

        /// <summary>Spaltenindex des Datums.</summary>
        public const int SPALTE_DATUM = 0;

        /// <summary>Spaltenindex der Anfangsuhrzeit.</summary>
        public const int SPALTE_VON = 1;

        /// <summary>Spaltenindex der Zeitzone des Anfangs.</summary>
        public const int SPALTE_ZONE_VON = 2;

        /// <summary>Spaltenindex der Zeitzone des Endes.</summary>
        public const int SPALTE_ZONE_BIS = 4;

        /// <summary>Spaltenindex des Preises.</summary>
        public const int SPALTE_WERT = 5;

        /// <summary>Kuerzel der mitteleuropaeischen Sommerzeit in den Zeitzonenspalten.</summary>
        public const string ZONE_SOMMERZEIT = "CEST";

        /// <summary>Hoechstzahl namentlich gemeldeter Problemzeilen im Protokoll.</summary>
        public const int MAX_GEMELDETE_ZEILEN = 20;

        /// <summary>
        /// Ringpuffer des Parsers [Byte]. <b>Pflichtangabe:</b> Die eingebettete
        /// NReco-Fassung setzt <c>BufferSize</c> im Konstruktor NICHT vor (anders als
        /// die Dokumentation im Dateikopf behauptet); bleibt der Wert 0, teilt
        /// <c>FillBuffer</c> durch die Pufferlaenge 0. Alle Aufrufer im Bestand setzen
        /// ihn deshalb ausdruecklich (VDI-3805-Importe 32 kB). 64 kB, weil eine
        /// Spotpreiszeile zwar kurz ist, ein groesserer Puffer bei 8.784 Zeilen aber
        /// spuerbar weniger Nachladevorgaenge braucht.
        /// </summary>
        public const int PUFFERGROESSE = 65536;

        private static readonly string[] DATUMSFORMATE = { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" };

        private static readonly CultureInfo DATEIKULTUR = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>
        /// Liest eine Spotpreisdatei von der Platte. Die Datei wird mit
        /// Kodierungserkennung geoeffnet (die Referenzdatei traegt eine UTF-8-BOM).
        /// </summary>
        /// <exception cref="ArgumentException">Wenn der Pfad leer ist.</exception>
        /// <exception cref="IOException">Wenn die Datei nicht lesbar ist.</exception>
        public static SpotDateiErgebnis LiesDatei(string pfad)
        {
            if (string.IsNullOrEmpty(pfad))
                throw new ArgumentException("Kein Dateipfad angegeben.", nameof(pfad));

            using (StreamReader sr = new StreamReader(pfad, detectEncodingFromByteOrderMarks: true))
            {
                return Lies(sr);
            }
        }

        /// <summary>
        /// Liest Spotpreiszeilen aus einem beliebigen <see cref="TextReader"/> - der
        /// Einstieg fuer Tests und fuer Dateien, die schon geoeffnet sind.
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="leser"/> <c>null</c> ist.</exception>
        public static SpotDateiErgebnis Lies(TextReader leser)
        {
            if (leser == null) throw new ArgumentNullException(nameof(leser));

            SpotDateiErgebnis e = new SpotDateiErgebnis();
            CsvReader csv = new CsvReader(leser, TRENNZEICHEN);
            csv.BufferSize = PUFFERGROESSE;
            csv.TrimFields = true;

            int zeilennummer = 0;
            while (csv.Read())
            {
                zeilennummer++;

                if (csv.FieldsCount < SPALTEN)
                {
                    // Leerzeile am Dateiende ist kein Fehler.
                    if (csv.FieldsCount <= 1 && string.IsNullOrEmpty(csv[0])) continue;

                    Unlesbar(e, zeilennummer);
                    continue;
                }

                int monat, tag, jahr;
                if (!DatumZerlegen(csv[SPALTE_DATUM], out tag, out monat, out jahr))
                {
                    // Die Kopfzeile ist die einzige Zeile, die kein Datum tragen DARF.
                    if (zeilennummer == 1) { e.MitKopfzeile = true; continue; }
                    Unlesbar(e, zeilennummer);
                    continue;
                }

                int stunde;
                if (!StundeZerlegen(csv[SPALTE_VON], out stunde))
                {
                    Unlesbar(e, zeilennummer);
                    continue;
                }

                double wert;
                if (!ZahlZerlegen(csv[SPALTE_WERT], out wert))
                {
                    Unlesbar(e, zeilennummer);
                    continue;
                }

                if (e.Jahr == 0) e.Jahr = jahr;
                else if (jahr != e.Jahr) e.ZeilenFremdesJahr++;

                e.ZeilenGesamt++;
                e.Zeilen.Add(new SpotStundenwert(
                    monat, tag, stunde,
                    IstSommerzeit(csv[SPALTE_ZONE_VON]),
                    IstSommerzeit(csv[SPALTE_ZONE_BIS]),
                    wert));
            }

            return e;
        }

        // =================================================================
        // Zerlegen einzelner Felder
        // =================================================================

        /// <summary>
        /// Zerlegt das Datumsfeld. Akzeptiert die deutsche Schreibweise der Quelldatei
        /// und die ISO-Form, jeweils mit FESTEM Format - <c>Parse</c> mit
        /// <c>CurrentCulture</c> laese "03.04.2024" unter en-US als 4. Maerz.
        /// </summary>
        public static bool DatumZerlegen(string feld, out int tag, out int monat, out int jahr)
        {
            tag = 0; monat = 0; jahr = 0;
            if (string.IsNullOrEmpty(feld)) return false;

            DateTime d;
            if (!DateTime.TryParseExact(feld.Trim(), DATUMSFORMATE, CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out d))
                return false;

            tag = d.Day; monat = d.Month; jahr = d.Year;
            return true;
        }

        /// <summary>
        /// Zerlegt die Uhrzeit "HH:mm" zur Stunde 0..23. Die Minuten werden bewusst
        /// ignoriert: Die Datei fuehrt Stundenprodukte, "00:15" gaebe es nur in einer
        /// Viertelstundendatei - und die traegt dann auch andere Spalten.
        /// </summary>
        public static bool StundeZerlegen(string feld, out int stunde)
        {
            stunde = -1;
            if (string.IsNullOrEmpty(feld)) return false;

            string t = feld.Trim();
            int doppelpunkt = t.IndexOf(':');
            string kopf = doppelpunkt > 0 ? t.Substring(0, doppelpunkt) : t;

            int h;
            if (!int.TryParse(kopf, NumberStyles.Integer, CultureInfo.InvariantCulture, out h)) return false;
            if (h < 0 || h > 23) return false;

            stunde = h;
            return true;
        }

        /// <summary>
        /// Zerlegt einen Preis. Erst mit der Dateikultur de-DE (Dezimalkomma,
        /// Tausenderpunkt), dann invariant - so laeuft auch eine mit Punkt geschriebene
        /// Datei durch, ohne dass "1.234" jemals als 1234 UND als 1,234 lesbar waere:
        /// Der erste Versuch entscheidet.
        /// </summary>
        public static bool ZahlZerlegen(string feld, out double wert)
        {
            wert = 0.0;
            if (string.IsNullOrEmpty(feld)) return false;

            string t = feld.Trim();
            if (t.Length == 0) return false;

            if (double.TryParse(t, NumberStyles.Float | NumberStyles.AllowThousands, DATEIKULTUR, out wert))
                return true;

            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out wert);
        }

        /// <summary>
        /// true, wenn die Zeitzonenspalte Sommerzeit ausweist (CEST). Alles andere -
        /// CET, leer, unbekannt - gilt als Winterzeit; die Aufbereitung braucht daraus
        /// nur den WECHSEL, nicht die absolute Zone.
        /// </summary>
        public static bool IstSommerzeit(string feld)
        {
            return !string.IsNullOrEmpty(feld) &&
                   feld.Trim().Equals(ZONE_SOMMERZEIT, StringComparison.OrdinalIgnoreCase);
        }

        private static void Unlesbar(SpotDateiErgebnis e, int zeilennummer)
        {
            e.ZeilenUnlesbar++;
            if (e.UnlesbareZeilen.Count < MAX_GEMELDETE_ZEILEN) e.UnlesbareZeilen.Add(zeilennummer);
        }
    }
}
