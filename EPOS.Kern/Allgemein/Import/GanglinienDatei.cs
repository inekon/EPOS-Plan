using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using NReco.Csv;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Vom Anwender bestaetigte oder uebersteuerte Leseoptionen einer Ganglinien-
    /// Quelldatei (AP5, Fachkonzept 3.2).
    /// </summary>
    public sealed class GanglinienImportOptionen
    {
        /// <summary>Feldtrennzeichen; <c>'\0'</c> = einspaltige Datei (Altweg .txt).</summary>
        public char Trennzeichen = '\0';

        /// <summary>Dezimaltrennzeichen der Zahlenfelder: <c>','</c> oder <c>'.'</c>.</summary>
        public char Dezimaltrenner = '.';

        /// <summary>Erste Zeile ist eine Kopfzeile und wird uebersprungen.</summary>
        public bool Kopfzeile = false;

        /// <summary>Nullbasierter Index der Wertspalte.</summary>
        public int WertSpalte = 0;

        /// <summary>Nullbasierter Index der Zeitstempelspalte; <c>-1</c> = keine.</summary>
        public int ZeitSpalte = -1;

        /// <summary>Deklarierte Einheit der Werte.</summary>
        public GanglinienEinheit Einheit = GanglinienEinheit.Kilowatt;

        /// <summary>Deklariertes Raster; <c>Unbekannt</c> = automatisch erkennen.</summary>
        public GanglinienRaster Raster = GanglinienRaster.Unbekannt;

        /// <summary>Zeitstempelkonvention (Intervallanfang/-ende).</summary>
        public IntervallKonvention Konvention = IntervallKonvention.Automatisch;

        /// <summary>Excel: Name des zu lesenden Tabellenblatts. Leer = erstes Blatt.</summary>
        public string Blattname = "";

        /// <summary>Flache Kopie - der Dialog arbeitet auf einer Arbeitsfassung.</summary>
        public GanglinienImportOptionen Kopie()
        {
            return new GanglinienImportOptionen
            {
                Trennzeichen = Trennzeichen,
                Dezimaltrenner = Dezimaltrenner,
                Kopfzeile = Kopfzeile,
                WertSpalte = WertSpalte,
                ZeitSpalte = ZeitSpalte,
                Einheit = Einheit,
                Raster = Raster,
                Konvention = Konvention,
                Blattname = Blattname
            };
        }
    }

    /// <summary>
    /// Ergebnis der Formaterkennung: Vorbelegung der Optionen plus die ersten
    /// Zeilen als Vorschau fuer den Dialog.
    /// </summary>
    public sealed class GanglinienVorschau
    {
        /// <summary>Vorbelegung der Leseoptionen.</summary>
        public GanglinienImportOptionen Vorschlag = new GanglinienImportOptionen();

        /// <summary>Erste Zeilen der Datei, bereits in Felder zerlegt (inkl. Kopfzeile).</summary>
        public List<string[]> Zeilen = new List<string[]>();

        /// <summary>Vorhandene Tabellenblaetter (nur Excel).</summary>
        public List<string> Blaetter = new List<string>();

        /// <summary>Groesste Feldanzahl in der Vorschau.</summary>
        public int Spaltenzahl = 0;

        /// <summary>Quelle ist eine Excel-Mappe.</summary>
        public bool IstExcel = false;

        /// <summary>Meldungen der Erkennung (Info/Warnung/Fehler).</summary>
        public List<PruefMeldung> Meldungen = new List<PruefMeldung>();

        /// <summary>Die Datei konnte gelesen werden.</summary>
        public bool Lesbar = false;
    }

    /// <summary>Rohdaten einer Quelldatei, Eingang der <see cref="GanglinienPruefung"/>.</summary>
    public sealed class GanglinienRohdaten
    {
        /// <summary>Gelesene Werte in Dateireihenfolge.</summary>
        public double[] Werte = new double[0];

        /// <summary>Gelesene Zeitstempel oder <c>null</c>.</summary>
        public DateTime[] Zeitstempel = null;

        /// <summary>Meldungen des Lesevorgangs.</summary>
        public List<PruefMeldung> Meldungen = new List<PruefMeldung>();

        /// <summary>Kein Fehler beim Lesen.</summary>
        public bool Erfolgreich
        {
            get
            {
                foreach (PruefMeldung m in Meldungen)
                    if (m.Stufe == PruefStufe.Fehler) return false;
                return true;
            }
        }
    }

    /// <summary>
    /// Datei-Leseschicht des erweiterten Lastgangimports (AP5, Fachkonzept 3.2):
    /// erkennt Format und Spalten, liest CSV/TXT ueber die eingebettete
    /// <see cref="CsvReader"/>-Bibliothek (NReco, MIT) und Excel-Mappen ueber
    /// ClosedXML (MIT).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Arbeitsteilung.</b> Hier steht ausschliesslich Datei-I/O und
    /// Zeichenkettenauswertung. Jede fachliche Regel - Rastererkennung,
    /// Einheitenumrechnung, Schaltjahr, Sommerzeit, Plausibilitaet - liegt in
    /// <see cref="GanglinienPruefung"/> in der UI- und DB-freien Engine und ist
    /// dort testbar. Diese Klasse liefert nur <c>double[]</c> und optionale
    /// <c>DateTime[]</c>.
    /// </para>
    /// <para>
    /// <b>Kultur.</b> Zahlen werden nie ueber <c>CurrentCulture</c> geparst,
    /// sondern ueber den erkannten oder gesetzten Dezimaltrenner in eine
    /// invariante Form gebracht (<see cref="VersucheZahl"/>). Dieselbe Datei
    /// einmal mit Punkt und einmal mit Komma ergibt damit dieselbe Reihe, und der
    /// Lauf ist unabhaengig von der Windows-Regionseinstellung.
    /// </para>
    /// <para>
    /// <b>Excel.</b> Gelesen wird ueber <b>ClosedXML</b> (MIT) - ohne
    /// installiertes Office und ohne COM. Die Mappe wird einmal geoeffnet und
    /// ihre benutzte Flaeche in <b>einem</b> Durchlauf in ein <c>object[,]</c>
    /// uebernommen; die Datei bleibt dabei nur lesend belegt
    /// (<c>FileShare.ReadWrite</c>) und darf parallel in Excel offen sein.
    /// <see cref="ZellwertWieValue2"/> bildet die fruehere
    /// <c>Range.Value2</c>-Semantik nach: Datumszellen kommen als
    /// OLE-Automation-Serienzahl heraus, die Zeitspalte wird deshalb weiterhin
    /// ueber <see cref="DateTime.FromOADate"/> zurueckgewandelt. ClosedXML liest
    /// nur OOXML - <c>.xls</c> und <c>.xlsb</c> werden mit der gezielten Meldung
    /// <see cref="SchluesselExcelFehlt"/> abgewiesen.
    /// </para>
    /// <para>
    /// <b>Altweg.</b> Eine <c>.txt</c>-Datei mit einem Wert je Zeile ist der
    /// Sonderfall "kein Trennzeichen, eine Spalte, keine Kopfzeile" und laeuft
    /// durch dieselbe Kette. Die beiden anderen Ganglinienimporte (Waermebedarf
    /// seit W13.2, Solarthermie seit W14b.2) lesen ueber
    /// <see cref="GanglinienTextDatei"/>; die frueher dafuer zustaendige
    /// <c>ToolsClass.OpenText</c> ist mit Welle 14b geloescht.
    /// </para>
    /// </remarks>
    public static class GanglinienDatei
    {
        /// <summary>Anzahl der Zeilen in der Dialogvorschau.</summary>
        public const int VorschauZeilen = 10;

        /// <summary>Zeilen, die die Formaterkennung auswertet.</summary>
        private const int ErkennungsZeilen = 40;

        /// <summary>Hoechstzahl einzeln gemeldeter Lesefehler; danach nur noch die Summe.</summary>
        private const int MaxEinzelfehler = 10;

        // --- Protokollschluessel (Anzeige ueber MyResource.Resource.*) --------

        /// <summary>Datei nicht gefunden. {0} = Pfad.</summary>
        public const string SchluesselDateiFehlt = "IMPORT_PROT_DATEI_FEHLT";

        /// <summary>Datei enthaelt keine auswertbare Zeile.</summary>
        public const string SchluesselDateiLeer = "IMPORT_PROT_DATEI_LEER";

        /// <summary>Datei nicht lesbar. {0} = Meldung des Betriebssystems.</summary>
        public const string SchluesselLesefehler = "IMPORT_PROT_LESEFEHLER";

        /// <summary>Format erkannt. {0} = Trennzeichen, {1} = Dezimaltrenner, {2} = Spalten, {3} = Kopfzeile ja/nein.</summary>
        public const string SchluesselFormatErkannt = "IMPORT_PROT_FORMAT_ERKANNT";

        /// <summary>Spaltenwahl. {0} = Wertspalte, {1} = Zeitspalte (0 = keine).</summary>
        public const string SchluesselSpaltenwahl = "IMPORT_PROT_SPALTENWAHL";

        /// <summary>Zahl nicht lesbar. {0} = Zeilennummer, {1} = Feldinhalt.</summary>
        public const string SchluesselZahlUnlesbar = "IMPORT_PROT_ZAHL_UNLESBAR";

        /// <summary>Zeitstempel nicht lesbar. {0} = Zeilennummer, {1} = Feldinhalt.</summary>
        public const string SchluesselZeitUnlesbar = "IMPORT_PROT_ZEIT_UNLESBAR";

        /// <summary>Zeile hat zu wenige Felder. {0} = Zeilennummer, {1} = Feldanzahl.</summary>
        public const string SchluesselSpalteFehlt = "IMPORT_PROT_SPALTE_FEHLT";

        /// <summary>Weitere gleichartige Fehler unterdrueckt. {0} = Anzahl.</summary>
        public const string SchluesselWeitereFehler = "IMPORT_PROT_WEITERE_FEHLER";

        /// <summary>Format nicht lesbar (.xls/.xlsb) - Datei bitte als .xlsx oder CSV speichern.</summary>
        public const string SchluesselExcelFehlt = "IMPORT_PROT_EXCEL_FEHLT";

        /// <summary>Excel-Blatt nicht gefunden. {0} = gesuchter Name, {1} = verwendetes Blatt.</summary>
        public const string SchluesselExcelBlatt = "IMPORT_PROT_EXCEL_BLATT";

        /// <summary>Anzahl gelesener Werte. {0} = Werte, {1} = Zeitstempel.</summary>
        public const string SchluesselGelesen = "IMPORT_PROT_GELESEN";

        // --- Zeitformate -----------------------------------------------------

        /// <summary>Zeitformate ohne Zonenangabe; deutsche Schreibweise und ISO 8601.</summary>
        private static readonly string[] ZeitformateOhneZone =
        {
            "dd.MM.yyyy HH:mm",
            "dd.MM.yyyy HH:mm:ss",
            "dd.MM.yyyy HH:mm:ss.fff",
            "dd.MM.yyyy",
            "dd.MM.yy HH:mm",
            "dd.MM.yy HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyyMMddHHmm",
            "yyyyMMdd HH:mm"
        };

        /// <summary>Zeitformate mit Zonenangabe; ausgewertet wird die Ortszeit wie geschrieben.</summary>
        private static readonly string[] ZeitformateMitZone =
        {
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            "yyyy-MM-dd HH:mm:sszzz",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ"
        };

        /// <summary>Trennzeichenkandidaten in Vorzugsreihenfolge - Semikolon vor Komma.</summary>
        private static readonly char[] Trennzeichenkandidaten = { ';', '\t', '|', ',' };

        // =====================================================================
        // Oeffentliche Schnittstelle
        // =====================================================================

        /// <summary>Ist der Pfad eine Excel-Mappe (.xlsx/.xlsm/.xls)?</summary>
        /// <param name="pfad">Dateipfad.</param>
        public static bool IstExcelDatei(string pfad)
        {
            if (string.IsNullOrEmpty(pfad)) return false;
            string e = Path.GetExtension(pfad).ToLowerInvariant();
            return e == ".xlsx" || e == ".xlsm" || e == ".xls" || e == ".xlsb";
        }

        /// <summary>
        /// Erkennt Trennzeichen, Dezimaltrenner, Kopfzeile und Spaltenbelegung und
        /// liefert die ersten Zeilen als Vorschau.
        /// </summary>
        /// <param name="pfad">Quelldatei.</param>
        /// <returns>Vorschau samt Vorbelegung; nie <c>null</c>.</returns>
        public static GanglinienVorschau Erkenne(string pfad)
        {
            GanglinienVorschau v = new GanglinienVorschau();

            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
            {
                v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiFehlt, pfad ?? ""));
                return v;
            }

            try
            {
                v.IstExcel = IstExcelDatei(pfad);
                char trenn = v.IstExcel ? '\0' : ErkanntesTrennzeichen(pfad);
                List<string[]> zeilen = v.IstExcel
                    ? ExcelZeilen(pfad, "", ErkennungsZeilen, v)
                    : TextZeilen(pfad, trenn, ErkennungsZeilen);

                if (zeilen == null) return v;                       // Meldung steht bereits
                if (zeilen.Count == 0)
                {
                    v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiLeer));
                    return v;
                }

                foreach (string[] z in zeilen)
                    if (z.Length > v.Spaltenzahl) v.Spaltenzahl = z.Length;

                GanglinienImportOptionen o = v.Vorschlag;
                o.Trennzeichen = trenn;
                o.Dezimaltrenner = ErkannterDezimaltrenner(zeilen, o.Trennzeichen);
                o.Kopfzeile = IstKopfzeile(zeilen[0], o.Dezimaltrenner);
                SpaltenWaehlen(zeilen, o);

                for (int i = 0; i < zeilen.Count && i < VorschauZeilen; i++) v.Zeilen.Add(zeilen[i]);

                v.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SchluesselFormatErkannt,
                    TrennzeichenText(o.Trennzeichen),
                    o.Dezimaltrenner.ToString(),
                    v.Spaltenzahl.ToString(CultureInfo.InvariantCulture),
                    o.Kopfzeile ? "1" : "0"));
                v.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SchluesselSpaltenwahl,
                    (o.WertSpalte + 1).ToString(CultureInfo.InvariantCulture),
                    (o.ZeitSpalte + 1).ToString(CultureInfo.InvariantCulture)));

                v.Lesbar = true;
                return v;
            }
            catch (Exception ex)
            {
                v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLesefehler, ex.Message));
                return v;
            }
        }

        /// <summary>
        /// Zerlegt die ersten Zeilen mit <b>vorgegebenen</b> Optionen - fuer die
        /// Schaltflaeche "Vorschau aktualisieren", nachdem der Anwender
        /// Trennzeichen, Blatt oder Dezimaltrenner uebersteuert hat. Anders als
        /// <see cref="Erkenne"/> raet die Methode nichts.
        /// </summary>
        /// <param name="pfad">Quelldatei.</param>
        /// <param name="optionen">Vom Anwender gesetzte Optionen.</param>
        /// <returns>Vorschau mit unveraendert uebernommenen Optionen; nie <c>null</c>.</returns>
        public static GanglinienVorschau Vorschau(string pfad, GanglinienImportOptionen optionen)
        {
            GanglinienVorschau v = new GanglinienVorschau();
            v.Vorschlag = optionen ?? new GanglinienImportOptionen();

            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
            {
                v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiFehlt, pfad ?? ""));
                return v;
            }

            try
            {
                v.IstExcel = IstExcelDatei(pfad);
                List<string[]> zeilen = v.IstExcel
                    ? ExcelZeilen(pfad, v.Vorschlag.Blattname, ErkennungsZeilen, v)
                    : TextZeilen(pfad, v.Vorschlag.Trennzeichen, ErkennungsZeilen);

                if (zeilen == null) return v;
                foreach (string[] z in zeilen)
                    if (z.Length > v.Spaltenzahl) v.Spaltenzahl = z.Length;
                for (int i = 0; i < zeilen.Count && i < VorschauZeilen; i++) v.Zeilen.Add(zeilen[i]);
                v.Lesbar = zeilen.Count > 0;
                return v;
            }
            catch (Exception ex)
            {
                v.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLesefehler, ex.Message));
                return v;
            }
        }

        /// <summary>
        /// Liest die Datei vollstaendig nach den uebergebenen Optionen.
        /// </summary>
        /// <param name="pfad">Quelldatei.</param>
        /// <param name="optionen">Leseoptionen (aus <see cref="Erkenne"/>, ggf. vom Anwender geaendert).</param>
        /// <returns>Rohwerte samt Zeitstempeln und Protokoll; nie <c>null</c>.</returns>
        public static GanglinienRohdaten Lies(string pfad, GanglinienImportOptionen optionen)
        {
            GanglinienRohdaten r = new GanglinienRohdaten();
            if (optionen == null) optionen = new GanglinienImportOptionen();

            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
            {
                r.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiFehlt, pfad ?? ""));
                return r;
            }

            try
            {
                if (IstExcelDatei(pfad)) LiesExcel(pfad, optionen, r);
                else LiesText(pfad, optionen, r);
            }
            catch (Exception ex)
            {
                r.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLesefehler, ex.Message));
            }

            return r;
        }

        // =====================================================================
        // Zahlen und Zeitstempel
        // =====================================================================

        /// <summary>
        /// Parst eine Zahl mit dem angegebenen Dezimaltrenner - immer invariant,
        /// nie ueber <c>CurrentCulture</c>. Tausendertrenner (das jeweils andere
        /// Zeichen, Leerzeichen, geschuetztes Leerzeichen, Apostroph) werden
        /// entfernt.
        /// </summary>
        /// <param name="text">Feldinhalt.</param>
        /// <param name="dezimaltrenner"><c>','</c> oder <c>'.'</c>.</param>
        /// <param name="wert">Gelesener Wert.</param>
        /// <returns><c>true</c> bei Erfolg.</returns>
        public static bool VersucheZahl(string text, char dezimaltrenner, out double wert)
        {
            wert = 0.0;
            if (string.IsNullOrEmpty(text)) return false;

            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ' || c == '\t' || c == '\u00A0' || c == '\'' || c == '\"') continue;
                if (c == dezimaltrenner) { sb.Append('.'); continue; }
                if (c == ',' || c == '.') continue;              // Tausendertrenner
                sb.Append(c);
            }
            if (sb.Length == 0) return false;

            return double.TryParse(sb.ToString(), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out wert);
        }

        /// <summary>
        /// Parst einen Zeitstempel in einem der gaengigen Formate (deutsche
        /// Schreibweise, ISO 8601, mit und ohne Zonenangabe). Zonenbehaftete
        /// Angaben liefern die Ortszeit <i>wie geschrieben</i> - die Reihe wird
        /// durchgaengig als Ortszeitreihe verstanden.
        /// </summary>
        /// <param name="text">Feldinhalt.</param>
        /// <param name="wert">Gelesener Zeitpunkt.</param>
        /// <returns><c>true</c> bei Erfolg.</returns>
        public static bool VersucheZeit(string text, out DateTime wert)
        {
            wert = DateTime.MinValue;
            if (string.IsNullOrEmpty(text)) return false;
            string s = text.Trim();
            if (s.Length == 0) return false;

            if (DateTime.TryParseExact(s, ZeitformateOhneZone, CultureInfo.InvariantCulture,
                                       DateTimeStyles.AllowWhiteSpaces, out wert))
                return true;

            DateTimeOffset dto;
            if (DateTimeOffset.TryParseExact(s, ZeitformateMitZone, CultureInfo.InvariantCulture,
                                             DateTimeStyles.AllowWhiteSpaces, out dto))
            {
                wert = dto.DateTime;     // Ortszeitanteil, ohne Umrechnung auf die Maschinenzone
                return true;
            }
            return false;
        }

        // =====================================================================
        // Formaterkennung
        // =====================================================================

        /// <summary>
        /// Trennzeichen aus den ersten Zeilen: gewaehlt wird der Kandidat, der in
        /// (fast) allen Zeilen gleich oft vorkommt; Semikolon vor Tabulator vor
        /// Senkrechtstrich vor Komma, damit deutsche Exporte mit Dezimalkomma
        /// nicht am Komma zerlegt werden.
        /// </summary>
        private static char ErkanntesTrennzeichen(string pfad)
        {
            List<string> roh = RoheZeilen(pfad, ErkennungsZeilen);
            if (roh.Count == 0) return '\0';

            foreach (char kandidat in Trennzeichenkandidaten)
            {
                int ersteAnzahl = -1;
                int passend = 0;
                foreach (string z in roh)
                {
                    int n = 0;
                    for (int i = 0; i < z.Length; i++) if (z[i] == kandidat) n++;
                    if (ersteAnzahl < 0) ersteAnzahl = n;
                    if (n == ersteAnzahl) passend++;
                }
                if (ersteAnzahl >= 1 && passend * 10 >= roh.Count * 8) return kandidat;
            }
            return '\0';
        }

        /// <summary>
        /// Dezimaltrenner aus den Zahlenfeldern: gezaehlt wird, welches Zeichen in
        /// den Feldern zuletzt steht. Ist das Komma bereits Feldtrennzeichen,
        /// kann es kein Dezimaltrenner sein.
        /// </summary>
        private static char ErkannterDezimaltrenner(List<string[]> zeilen, char trennzeichen)
        {
            if (trennzeichen == ',') return '.';

            int komma = 0, punkt = 0;
            for (int z = 0; z < zeilen.Count; z++)
            {
                string[] felder = zeilen[z];
                for (int s = 0; s < felder.Length; s++)
                {
                    string f = felder[s];
                    if (string.IsNullOrEmpty(f)) continue;
                    if (SiehtNachZeitAus(f)) continue;          // 01.01.2024 ist kein Dezimalpunkt

                    int iK = f.LastIndexOf(',');
                    int iP = f.LastIndexOf('.');
                    if (iK < 0 && iP < 0) continue;

                    // Ein Trenner mit genau drei Folgeziffern ist ein Tausendertrenner.
                    if (iK > iP) { if (!DreiZiffernDanach(f, iK)) komma++; }
                    else if (iP > iK) { if (!DreiZiffernDanach(f, iP)) punkt++; }
                }
            }
            if (komma > punkt) return ',';
            if (punkt > komma) return '.';
            return '.';                                          // Gleichstand: invariant wie der Altweg
        }

        private static bool DreiZiffernDanach(string f, int pos)
        {
            int rest = f.Length - pos - 1;
            if (rest != 3) return false;
            for (int i = pos + 1; i < f.Length; i++) if (!char.IsDigit(f[i])) return false;
            return true;
        }

        /// <summary>Grobe Vorpruefung: enthaelt das Feld ein Datumsmuster?</summary>
        private static bool SiehtNachZeitAus(string f)
        {
            DateTime dummy;
            return VersucheZeit(f, out dummy);
        }

        /// <summary>
        /// Kopfzeile, wenn mindestens ein belegtes Feld weder Zahl noch Zeitstempel ist.
        /// </summary>
        private static bool IstKopfzeile(string[] erste, char dezimaltrenner)
        {
            if (erste == null || erste.Length == 0) return false;
            bool belegt = false;
            for (int i = 0; i < erste.Length; i++)
            {
                string f = erste[i];
                if (string.IsNullOrEmpty(f)) continue;
                belegt = true;
                double d;
                DateTime t;
                if (!VersucheZahl(f, dezimaltrenner, out d) && !VersucheZeit(f, out t)) return true;
            }
            return !belegt;
        }

        /// <summary>
        /// Waehlt Zeit- und Wertspalte: erste durchgaengig als Zeitstempel lesbare
        /// Spalte wird zur Zeitspalte, erste durchgaengig numerische Spalte
        /// daneben zur Wertspalte.
        /// </summary>
        private static void SpaltenWaehlen(List<string[]> zeilen, GanglinienImportOptionen o)
        {
            int von = o.Kopfzeile ? 1 : 0;
            int spalten = 0;
            for (int z = von; z < zeilen.Count; z++)
                if (zeilen[z].Length > spalten) spalten = zeilen[z].Length;

            o.ZeitSpalte = -1;
            o.WertSpalte = 0;
            if (spalten == 0) return;

            bool[] istZeit = new bool[spalten];
            bool[] istZahl = new bool[spalten];
            for (int s = 0; s < spalten; s++)
            {
                int zeit = 0, zahl = 0, gepruefte = 0;
                for (int z = von; z < zeilen.Count; z++)
                {
                    string[] f = zeilen[z];
                    if (s >= f.Length || string.IsNullOrEmpty(f[s])) continue;
                    gepruefte++;
                    DateTime t;
                    double d;
                    if (VersucheZeit(f[s], out t)) zeit++;
                    else if (VersucheZahl(f[s], o.Dezimaltrenner, out d)) zahl++;
                }
                istZeit[s] = gepruefte > 0 && zeit == gepruefte;
                istZahl[s] = gepruefte > 0 && zahl == gepruefte;
            }

            for (int s = 0; s < spalten; s++)
                if (istZeit[s]) { o.ZeitSpalte = s; break; }

            for (int s = 0; s < spalten; s++)
                if (istZahl[s] && s != o.ZeitSpalte) { o.WertSpalte = s; return; }

            // Keine durchgaengig numerische Spalte gefunden: letzte Spalte nehmen.
            o.WertSpalte = spalten - 1 == o.ZeitSpalte && spalten > 1 ? spalten - 2 : spalten - 1;
            if (o.WertSpalte < 0) o.WertSpalte = 0;
        }

        /// <summary>Anzeigetext eines Trennzeichens fuer das Protokoll.</summary>
        public static string TrennzeichenText(char c)
        {
            if (c == '\0') return "-";
            if (c == '\t') return "TAB";
            return c.ToString();
        }

        // =====================================================================
        // Textdateien (CSV / TXT) ueber NReco
        // =====================================================================

        /// <summary>Rohe Zeilen fuer die Trennzeichenerkennung (ohne Zerlegung).</summary>
        private static List<string> RoheZeilen(string pfad, int maximal)
        {
            List<string> liste = new List<string>();
            using (StreamReader sr = LeserOeffnen(pfad))
            {
                string z;
                while (liste.Count < maximal && (z = sr.ReadLine()) != null)
                {
                    if (z.Trim().Length == 0) continue;
                    liste.Add(z);
                }
            }
            return liste;
        }

        /// <summary>
        /// Erste Zeilen als Felder. Bei einspaltigen Dateien (Altweg .txt) ist jede
        /// Zeile genau ein Feld.
        /// </summary>
        private static List<string[]> TextZeilen(string pfad, char trenn, int maximal)
        {
            List<string[]> zeilen = new List<string[]>();

            if (trenn == '\0')
            {
                foreach (string z in RoheZeilen(pfad, maximal)) zeilen.Add(new string[] { z.Trim() });
                return zeilen;
            }

            using (StreamReader sr = LeserOeffnen(pfad))
            {
                CsvReader csv = new CsvReader(sr, trenn.ToString());
                csv.BufferSize = 65536;
                csv.TrimFields = true;
                while (zeilen.Count < maximal && csv.Read())
                {
                    string[] felder = new string[csv.FieldsCount];
                    for (int i = 0; i < csv.FieldsCount; i++) felder[i] = csv[i] ?? "";
                    zeilen.Add(felder);
                }
            }
            return zeilen;
        }

        /// <summary>Vollstaendiger Lesevorgang einer Text-/CSV-Datei.</summary>
        private static void LiesText(string pfad, GanglinienImportOptionen o, GanglinienRohdaten r)
        {
            List<double> werte = new List<double>(35040);
            List<DateTime> zeiten = o.ZeitSpalte >= 0 ? new List<DateTime>(35040) : null;
            int fehler = 0;
            int zeilennummer = 0;
            bool erste = true;

            using (StreamReader sr = LeserOeffnen(pfad))
            {
                if (o.Trennzeichen == '\0')
                {
                    string z;
                    while ((z = sr.ReadLine()) != null)
                    {
                        zeilennummer++;
                        string t = z.Trim();
                        if (t.Length == 0) continue;
                        if (erste && o.Kopfzeile) { erste = false; continue; }
                        erste = false;
                        ZeileUebernehmen(new string[] { t }, zeilennummer, o, werte, zeiten, r, ref fehler);
                    }
                }
                else
                {
                    CsvReader csv = new CsvReader(sr, o.Trennzeichen.ToString());
                    csv.BufferSize = 65536;
                    csv.TrimFields = true;
                    while (csv.Read())
                    {
                        zeilennummer++;
                        if (erste && o.Kopfzeile) { erste = false; continue; }
                        erste = false;
                        string[] felder = new string[csv.FieldsCount];
                        for (int i = 0; i < csv.FieldsCount; i++) felder[i] = csv[i] ?? "";
                        ZeileUebernehmen(felder, zeilennummer, o, werte, zeiten, r, ref fehler);
                    }
                }
            }

            Abschluss(werte, zeiten, r, fehler);
        }

        /// <summary>Ein Datensatz aus zerlegten Feldern.</summary>
        private static void ZeileUebernehmen(
            string[] felder, int zeilennummer, GanglinienImportOptionen o,
            List<double> werte, List<DateTime> zeiten, GanglinienRohdaten r, ref int fehler)
        {
            int noetig = Math.Max(o.WertSpalte, o.ZeitSpalte) + 1;
            if (felder.Length < noetig)
            {
                Fehler(r, ref fehler, SchluesselSpalteFehlt,
                       zeilennummer.ToString(CultureInfo.InvariantCulture),
                       felder.Length.ToString(CultureInfo.InvariantCulture));
                return;
            }

            double w;
            if (!VersucheZahl(felder[o.WertSpalte], o.Dezimaltrenner, out w))
            {
                Fehler(r, ref fehler, SchluesselZahlUnlesbar,
                       zeilennummer.ToString(CultureInfo.InvariantCulture), felder[o.WertSpalte]);
                return;
            }

            if (zeiten != null)
            {
                DateTime t;
                if (!VersucheZeit(felder[o.ZeitSpalte], out t))
                {
                    Fehler(r, ref fehler, SchluesselZeitUnlesbar,
                           zeilennummer.ToString(CultureInfo.InvariantCulture), felder[o.ZeitSpalte]);
                    return;
                }
                zeiten.Add(t);
            }
            werte.Add(w);
        }

        /// <summary>
        /// Oeffnet den Leser mit Kodierungserkennung (BOM schlaegt die Vorgabe).
        /// Vorgabe ist Windows-1252 - deutsche Zaehlerexporte sind fast nie UTF-8,
        /// und Umlaute stehen ohnehin nur in der Kopfzeile. Unter .NET 8 ist die
        /// Codepage 1252 nur nach Registrierung des
        /// <c>CodePagesEncodingProvider</c> verfuegbar; der Rueckfall ist deshalb
        /// <see cref="Encoding.Latin1"/>, das fuer alle deutschen Umlaute
        /// byteidentisch ist.
        /// </summary>
        private static StreamReader LeserOeffnen(string pfad)
        {
            Encoding vorgabe;
            try { vorgabe = Encoding.GetEncoding(1252); }
            catch (Exception) { vorgabe = Encoding.Latin1; }
            return new StreamReader(pfad, vorgabe, true);
        }

        // =====================================================================
        // Excel ueber ClosedXML - ein einziger Bulk-Read
        // =====================================================================

        /// <summary>
        /// Liest die Mappe in einem Zugriff und liefert die ersten
        /// <paramref name="maximal"/> Zeilen als Zeichenketten.
        /// </summary>
        private static List<string[]> ExcelZeilen(string pfad, string blatt, int maximal, GanglinienVorschau v)
        {
            object[,] daten;
            List<string> blaetter;
            string verwendet;
            if (!ExcelBulkRead(pfad, blatt, out daten, out blaetter, out verwendet, v.Meldungen)) return null;

            v.Blaetter.AddRange(blaetter);
            v.Vorschlag.Blattname = verwendet;

            // ExcelBulkRead legt das Feld EINS GROESSER an (new object[zeilen + 1,
            // spalten + 1]), damit es sich wie Excel 1-basiert ansprechen laesst;
            // Index 0 bleibt leer. Die Zahl der Zeilen und Spalten ist deshalb
            // GetLength() MINUS EINS - ohne das Minus lief jede Schleife eine
            // Stelle ueber das Feld hinaus (Befund W12-B27, iU9-W12.0i).
            int zeilen = daten.GetLength(0) - 1;
            int spalten = daten.GetLength(1) - 1;
            bool[] zeitspalte = ExcelZeitspalten(daten);

            List<string[]> liste = new List<string[]>();
            for (int z = 1; z <= zeilen && liste.Count < maximal; z++)
            {
                string[] felder = new string[spalten];
                bool leer = true;
                for (int s = 1; s <= spalten; s++)
                {
                    felder[s - 1] = zeitspalte[s]
                        ? ZelleAlsZeittext(daten[z, s])
                        : ZelleAlsText(daten[z, s]);
                    if (felder[s - 1].Length > 0) leer = false;
                }
                if (leer) continue;
                liste.Add(felder);
            }
            return liste;
        }

        /// <summary>
        /// Erkennt die Datumsspalten einer Excel-Mappe.
        /// <see cref="ZellwertWieValue2"/> liefert Datumszellen als
        /// OLE-Automation-Serienzahl und damit als
        /// <c>double</c> - an einer einzelnen Zelle ist ein Datum nicht von einem
        /// Messwert zu unterscheiden. Erkennbar ist es nur an der <i>Reihe</i>:
        /// lauter Zahlen im Datumsbereich (1954-2119), streng steigend, mit
        /// konstantem Schritt aus {1 Tag, 1 Stunde, 15 Minuten, 1 Minute}. Ohne
        /// diese Vorpruefung wuerde die Spaltenwahl die Datumsspalte fuer eine
        /// Wertspalte halten und Serienzahlen als Leistung importieren.
        /// </summary>
        /// <param name="daten">Bulk-Read der benutzten Flaeche (1-basiert).</param>
        /// <returns>Feld ueber die Spaltenindizes (1-basiert); <c>true</c> = Datumsspalte.</returns>
        private static bool[] ExcelZeitspalten(object[,] daten)
        {
            // 1-basiertes Feld aus ExcelBulkRead: GetLength() minus eins (W12-B27).
            int zeilen = daten.GetLength(0) - 1;
            int spalten = daten.GetLength(1) - 1;
            bool[] ergebnis = new bool[spalten + 1];
            double[] bekannteSchritte = { 1.0, 1.0 / 24.0, 1.0 / 96.0, 1.0 / 1440.0 };

            for (int s = 1; s <= spalten; s++)
            {
                bool hatVorher = false;
                double vorher = 0.0;
                bool hatSchritt = false;
                double schritt = 0.0;
                int geprueft = 0;
                bool gut = true;

                for (int z = 1; z <= zeilen && geprueft < 50 && gut; z++)
                {
                    object zelle = daten[z, s];
                    if (zelle == null) continue;
                    if (zelle is DateTime) { geprueft++; ergebnis[s] = true; continue; }
                    if (!(zelle is double))
                    {
                        if (geprueft > 0) gut = false;      // Text mitten in der Reihe
                        continue;                            // sonst: Kopfzeile
                    }

                    double d = (double)zelle;
                    if (d < 20000.0 || d > 80000.0) { gut = false; break; }
                    if (hatVorher)
                    {
                        double diff = d - vorher;
                        if (diff <= 0.0) { gut = false; break; }
                        if (!hatSchritt) { schritt = diff; hatSchritt = true; }
                        else if (Math.Abs(diff - schritt) > 1e-7) { gut = false; break; }
                    }
                    vorher = d;
                    hatVorher = true;
                    geprueft++;
                }

                if (ergebnis[s]) continue;
                if (!gut || !hatSchritt || geprueft < 4) continue;
                for (int i = 0; i < bekannteSchritte.Length; i++)
                    if (Math.Abs(schritt - bekannteSchritte[i]) < 1e-7) { ergebnis[s] = true; break; }
            }
            return ergebnis;
        }

        /// <summary>Zelleninhalt einer erkannten Datumsspalte als lesbarer Text.</summary>
        private static string ZelleAlsZeittext(object zelle)
        {
            DateTime t;
            if (ZelleAlsZeit(zelle, out t))
                return t.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            return ZelleAlsText(zelle);
        }

        /// <summary>Vollstaendiger Lesevorgang einer Excel-Mappe.</summary>
        private static void LiesExcel(string pfad, GanglinienImportOptionen o, GanglinienRohdaten r)
        {
            object[,] daten;
            List<string> blaetter;
            string verwendet;
            if (!ExcelBulkRead(pfad, o.Blattname, out daten, out blaetter, out verwendet, r.Meldungen)) return;

            // 1-basiertes Feld aus ExcelBulkRead: GetLength() minus eins (W12-B27).
            int zeilen = daten.GetLength(0) - 1;
            int spalten = daten.GetLength(1) - 1;
            List<double> werte = new List<double>(35040);
            List<DateTime> zeiten = o.ZeitSpalte >= 0 ? new List<DateTime>(35040) : null;
            int fehler = 0;
            bool erste = true;

            for (int z = 1; z <= zeilen; z++)
            {
                // Leerzeilen der benutzten Flaeche ueberspringen.
                bool leer = true;
                for (int s = 1; s <= spalten && leer; s++)
                    if (daten[z, s] != null && ZelleAlsText(daten[z, s]).Length > 0) leer = false;
                if (leer) continue;

                if (erste && o.Kopfzeile) { erste = false; continue; }
                erste = false;

                int wertSpalte = o.WertSpalte + 1;
                int zeitSpalte = o.ZeitSpalte + 1;
                if (wertSpalte > spalten || (zeiten != null && zeitSpalte > spalten))
                {
                    Fehler(r, ref fehler, SchluesselSpalteFehlt,
                           z.ToString(CultureInfo.InvariantCulture),
                           spalten.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                double w;
                if (!ZelleAlsZahl(daten[z, wertSpalte], o.Dezimaltrenner, out w))
                {
                    Fehler(r, ref fehler, SchluesselZahlUnlesbar,
                           z.ToString(CultureInfo.InvariantCulture), ZelleAlsText(daten[z, wertSpalte]));
                    continue;
                }

                if (zeiten != null)
                {
                    DateTime t;
                    if (!ZelleAlsZeit(daten[z, zeitSpalte], out t))
                    {
                        Fehler(r, ref fehler, SchluesselZeitUnlesbar,
                               z.ToString(CultureInfo.InvariantCulture), ZelleAlsText(daten[z, zeitSpalte]));
                        continue;
                    }
                    zeiten.Add(t);
                }
                werte.Add(w);
            }

            Abschluss(werte, zeiten, r, fehler);
        }

        /// <summary>
        /// Oeffnet die Mappe ueber ClosedXML, liest die benutzte Flaeche
        /// (<c>RangeUsed</c>) in <b>einem</b> Durchlauf als <c>object[,]</c> und
        /// gibt die Datei wieder frei.
        /// </summary>
        /// <returns><c>false</c>, wenn das Format nicht lesbar ist (.xls/.xlsb)
        /// oder die Mappe nicht geoeffnet werden kann.</returns>
        private static bool ExcelBulkRead(
            string pfad, string blatt, out object[,] daten, out List<string> blaetter,
            out string verwendet, List<PruefMeldung> meldungen)
        {
            daten = null;
            blaetter = new List<string>();
            verwendet = "";

            // ClosedXML liest ausschliesslich OOXML. Das alte Binaerformat .xls und
            // das binaere .xlsb bleiben aussen vor - dafuer gibt es die gezielte
            // Meldung statt einer Ausnahme aus der Bibliothek.
            string endung = Path.GetExtension(pfad).ToLowerInvariant();
            if (endung == ".xls" || endung == ".xlsb")
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselExcelFehlt));
                return false;
            }

            try
            {
                // FileShare.ReadWrite tritt an die Stelle des frueheren
                // schreibgeschuetzten Oeffnens: die Mappe darf waehrend des Imports
                // in Excel geoeffnet sein.
                using (FileStream strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (XLWorkbook mappe = new XLWorkbook(strom))
                {
                    IXLWorksheet gewaehlt = null;
                    foreach (IXLWorksheet b in mappe.Worksheets)
                    {
                        blaetter.Add(b.Name);
                        if (gewaehlt == null && (string.IsNullOrEmpty(blatt) || b.Name == blatt)) gewaehlt = b;
                    }
                    if (gewaehlt == null && mappe.Worksheets.Count > 0)
                        gewaehlt = mappe.Worksheet(1);
                    if (gewaehlt == null)
                    {
                        meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiLeer));
                        return false;
                    }

                    verwendet = gewaehlt.Name;
                    if (!string.IsNullOrEmpty(blatt) && verwendet != blatt)
                        meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SchluesselExcelBlatt, blatt, verwendet));

                    // *** Der einzige Datenzugriff: die gesamte benutzte Flaeche auf einmal. ***
                    IXLRange bereich = gewaehlt.RangeUsed();
                    if (bereich == null)
                    {
                        daten = new object[2, 2];      // leeres Blatt; 1-basiert wie Excel
                        return true;
                    }

                    int zeilen = bereich.RowCount();
                    int spalten = bereich.ColumnCount();
                    daten = new object[zeilen + 1, spalten + 1];   // 1-basiert wie Excel, Index 0 bleibt leer
                    for (int z = 1; z <= zeilen; z++)
                        for (int s = 1; s <= spalten; s++)
                            daten[z, s] = ZellwertWieValue2(bereich.Cell(z, s).Value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselLesefehler, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Bildet die Semantik von <c>Range.Value2</c> nach, auf der die gesamte
        /// nachgelagerte Auswertung beruht: Zahlen <i>und</i> Datumswerte kommen
        /// als <c>double</c> heraus, Datumswerte dabei als
        /// OLE-Automation-Serienzahl. <see cref="ExcelZeitspalten"/>,
        /// <see cref="ZelleAlsText"/>, <see cref="ZelleAlsZahl"/> und
        /// <see cref="ZelleAlsZeit"/> bleiben dadurch unveraendert.
        /// </summary>
        /// <param name="wert">Zellwert aus ClosedXML.</param>
        /// <returns>Wert in Value2-Form; <c>null</c> bei leerer Zelle.</returns>
        private static object ZellwertWieValue2(XLCellValue wert)
        {
            if (wert.IsBlank) return null;
            if (wert.IsNumber) return wert.GetNumber();
            if (wert.IsDateTime) return wert.GetDateTime().ToOADate();
            if (wert.IsTimeSpan) return wert.GetTimeSpan().TotalDays;
            if (wert.IsBoolean) return wert.GetBoolean();
            if (wert.IsText) return wert.GetText();
            if (wert.IsError) return wert.GetError().ToString();
            return wert.ToString();
        }

        /// <summary>
        /// Zelleninhalt als Text. <see cref="ZellwertWieValue2"/> liefert Zahlen
        /// als <c>double</c>; die Ausgabe ist bewusst invariant.
        /// </summary>
        private static string ZelleAlsText(object zelle)
        {
            if (zelle == null) return "";
            if (zelle is double) return ((double)zelle).ToString("R", CultureInfo.InvariantCulture);
            if (zelle is DateTime) return ((DateTime)zelle).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            return Convert.ToString(zelle, CultureInfo.InvariantCulture) ?? "";
        }

        /// <summary>Zelleninhalt als Zahl - numerische Zellen ohne jedes Parsen.</summary>
        private static bool ZelleAlsZahl(object zelle, char dezimaltrenner, out double wert)
        {
            wert = 0.0;
            if (zelle == null) return false;
            if (zelle is double) { wert = (double)zelle; return true; }
            if (zelle is int) { wert = (int)zelle; return true; }
            return VersucheZahl(Convert.ToString(zelle, CultureInfo.InvariantCulture), dezimaltrenner, out wert);
        }

        /// <summary>
        /// Zelleninhalt als Zeitpunkt. <see cref="ZellwertWieValue2"/> gibt
        /// Datumszellen als OLE-Automation-Serienzahl zurueck; sie wird ueber
        /// <see cref="DateTime.FromOADate"/> zurueckgewandelt. Der zulaessige
        /// Bereich ist auf 1900-2199 begrenzt, damit reine Messwerte nicht
        /// versehentlich als Datum gelesen werden.
        /// </summary>
        private static bool ZelleAlsZeit(object zelle, out DateTime wert)
        {
            wert = DateTime.MinValue;
            if (zelle == null) return false;
            if (zelle is DateTime) { wert = (DateTime)zelle; return true; }
            if (zelle is double)
            {
                double d = (double)zelle;
                if (d < 1.0 || d > 109575.0) return false;      // 01.01.1900 ... 31.12.2199
                try { wert = DateTime.FromOADate(d); return true; }
                catch (ArgumentException) { return false; }
            }
            return VersucheZeit(Convert.ToString(zelle, CultureInfo.InvariantCulture), out wert);
        }

        // =====================================================================
        // Gemeinsames
        // =====================================================================

        private static void Fehler(GanglinienRohdaten r, ref int fehler, string schluessel, params string[] werte)
        {
            fehler++;
            if (fehler <= MaxEinzelfehler)
                r.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, schluessel, werte));
        }

        private static void Abschluss(List<double> werte, List<DateTime> zeiten, GanglinienRohdaten r, int fehler)
        {
            if (fehler > MaxEinzelfehler)
                r.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselWeitereFehler,
                    (fehler - MaxEinzelfehler).ToString(CultureInfo.InvariantCulture)));

            r.Werte = werte.ToArray();
            r.Zeitstempel = zeiten != null ? zeiten.ToArray() : null;

            if (r.Werte.Length == 0 && fehler == 0)
                r.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, SchluesselDateiLeer));

            r.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SchluesselGelesen,
                r.Werte.Length.ToString(CultureInfo.InvariantCulture),
                (r.Zeitstempel != null ? r.Zeitstempel.Length : 0).ToString(CultureInfo.InvariantCulture)));
        }
    }
}
