using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das ERGEBNIS von „Supportpaket speichern…".
    /// </summary>
    /// <param name="Pfad">Die geschriebene ZIP-Datei; leer, wenn nichts geschrieben wurde (Abbruch oder Fehler).</param>
    /// <param name="Enthalten">Die Eintraege im Paket, in Reihenfolge.</param>
    /// <param name="Fehlend">Die Quellen, die es nicht gab — kein Fehler, nur benannt.</param>
    /// <param name="Fehler">Leer, wenn das Paket steht; sonst die Meldung des Schreibfehlers am Ziel.</param>
    public sealed record Supportpaketergebnis(string Pfad, IReadOnlyList<string> Enthalten,
                                              IReadOnlyList<string> Fehlend, string Fehler)
    {
        /// <summary>Abbruch durch den Anwender: kein Pfad, kein Fehler.</summary>
        public static Supportpaketergebnis Abgebrochen { get; } =
            new Supportpaketergebnis("", Array.Empty<string>(), Array.Empty<string>(), "");

        /// <summary>Steht das Paket?</summary>
        public bool Geschrieben => !string.IsNullOrEmpty(Pfad) && string.IsNullOrEmpty(Fehler);
    }

    /// <summary>
    /// Das SUPPORTPAKET (Konzept Administration und Support, Abschnitt 5, Entscheid AS-Q6 (a);
    /// Entwicklerhandbuch Abschnitt 3): eine ZIP-Datei, die der Anwender an seine E-Mail haengt —
    /// der Supportweg ohne Fernwartung. Plattformfrei; das Ziel waehlt die Schale.
    ///
    /// <para><b>Inhalt:</b> <see cref="EINTRAG_DIAGNOSE"/> (der Diagnoseblock, den die Schale
    /// uebergibt), <see cref="Ausnahmeprotokoll.DATEI"/> und <see cref="Ausnahmeprotokoll.DATEI_ALT"/>
    /// aus dem Protokollordner, <see cref="MIGRATIONSPROTOKOLL"/> neben der Datenbank,
    /// <see cref="EINTRAG_KATALOGABGLEICH"/> (die juengsten Zeilen von <c>Tab_Katalogabgleich</c>
    /// ueber <see cref="Katalogabgleich.Protokoll"/>, tabulatorgetrennt mit Spaltenkoepfen) und nur
    /// auf ausdruecklichen Wunsch <see cref="KiProtokoll.Dateiname"/> — es kann Eingaben des
    /// Anwenders enthalten.</para>
    ///
    /// <para><b>Nie</b> Projektdaten, nie die Datenbank: Das Paket nimmt nur die benannten
    /// Protokolldateien auf, keine Datei mit Datenbankendung.</para>
    ///
    /// <para>Eine fehlende Quelle ist kein Fehler, sie steht in <see cref="Supportpaketergebnis.Fehlend"/>.
    /// Ein Schreibfehler am Ziel kommt benannt zurueck; eine halb geschriebene Datei wird entfernt.</para>
    /// </summary>
    public static class Supportpaket
    {
        /// <summary>Eintrag des Diagnoseblocks.</summary>
        public const string EINTRAG_DIAGNOSE = "Diagnosedaten.txt";

        /// <summary>Eintrag des Auszugs aus <c>Tab_Katalogabgleich</c>.</summary>
        public const string EINTRAG_KATALOGABGLEICH = "Katalogabgleich.txt";

        /// <summary>
        /// Das Protokoll der Schemamigration neben der Datenbank — der Name von
        /// <c>SchemaMigration.PROTOKOLL_DATEI</c>; die Klasse liegt in der Windows-Schale.
        /// </summary>
        public const string MIGRATIONSPROTOKOLL = "migration_protokoll.txt";

        /// <summary>Hoechstzahl der Zeilen aus <c>Tab_Katalogabgleich</c>, neueste zuerst.</summary>
        public const int KATALOGABGLEICH_ZEILEN = 2000;

        /// <summary>Der Vorgabename im Speicherdialog der Schale: <c>EPOS-Plan_Supportpaket_yyyy-MM-dd_HHmm.zip</c>.</summary>
        public static string Vorgabename(DateTime zeitpunkt) =>
            "EPOS-Plan_Supportpaket_" + zeitpunkt.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture) + ".zip";

        /// <summary>
        /// Schreibt das Paket nach <paramref name="zielpfad"/> — mit den Ordnern der laufenden
        /// Datenbank (<see cref="Diagnosedaten.ProtokollordnerPfad"/>, Ordner von <see cref="DataRepository.GetDBPath"/>).
        /// </summary>
        public static Supportpaketergebnis Erstellen(string zielpfad, string diagnosetext, bool mitKiProtokoll)
        {
            string datenbankordner = null;
            try
            {
                string db = DataRepository.GetDBPath();
                datenbankordner = string.IsNullOrWhiteSpace(db) ? null : Path.GetDirectoryName(db);
            }
            catch { /* ohne Datenbankpfad fehlen die Dateien daneben */ }
            string protokollordner = null;
            try { protokollordner = Diagnosedaten.ProtokollordnerPfad(); }
            catch { /* dito */ }
            return Erstellen(zielpfad, diagnosetext, mitKiProtokoll, protokollordner, datenbankordner);
        }

        /// <summary>Schreibt das Paket mit ausdruecklichen Ordnern (fuer Pruefungen und Werkzeuge).</summary>
        public static Supportpaketergebnis Erstellen(string zielpfad, string diagnosetext, bool mitKiProtokoll,
                                                     string protokollordner, string datenbankordner)
        {
            var enthalten = new List<string>();
            var fehlend = new List<string>();
            if (string.IsNullOrWhiteSpace(zielpfad))
                return new Supportpaketergebnis("", enthalten, fehlend, Text(() => MyResource.Resource.SUP_PAKET_KEIN_ZIEL, "Kein Ziel angegeben."));

            // Die Quellen: Eintrag und Inhalt; null = fehlt.
            var quellen = new List<(string Eintrag, byte[] Inhalt)>
            {
                (EINTRAG_DIAGNOSE, new UTF8Encoding(true).GetBytes(diagnosetext ?? "")),
                (Ausnahmeprotokoll.DATEI, Lesen(protokollordner, Ausnahmeprotokoll.DATEI)),
                (Ausnahmeprotokoll.DATEI_ALT, Lesen(protokollordner, Ausnahmeprotokoll.DATEI_ALT)),
                (MIGRATIONSPROTOKOLL, Lesen(datenbankordner, MIGRATIONSPROTOKOLL) ?? Lesen(protokollordner, MIGRATIONSPROTOKOLL)),
                (EINTRAG_KATALOGABGLEICH, KatalogabgleichAuszug()),
            };
            if (mitKiProtokoll)
                quellen.Add((KiProtokoll.Dateiname,
                             Lesen(protokollordner, KiProtokoll.Dateiname) ?? Lesen(datenbankordner, KiProtokoll.Dateiname)));

            string ziel;
            try { ziel = Path.GetFullPath(zielpfad); }
            catch (Exception ex) { return new Supportpaketergebnis("", enthalten, fehlend, ex.Message); }

            bool angelegt = false;
            try
            {
                using (var strom = new FileStream(ziel, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    angelegt = true;
                    using (var zip = new ZipArchive(strom, ZipArchiveMode.Create))
                    {
                        foreach (var (eintrag, inhalt) in quellen)
                        {
                            if (inhalt == null) { fehlend.Add(eintrag); continue; }
                            ZipArchiveEntry e = zip.CreateEntry(eintrag, CompressionLevel.Optimal);
                            using (Stream s = e.Open()) s.Write(inhalt, 0, inhalt.Length);
                            enthalten.Add(eintrag);
                        }
                    }
                }
                return new Supportpaketergebnis(ziel, enthalten, fehlend, "");
            }
            catch (Exception ex)
            {
                if (angelegt)
                {
                    try { File.Delete(ziel); } catch { /* bleibt liegen */ }
                }
                return new Supportpaketergebnis("", Array.Empty<string>(), fehlend, ex.Message);
            }
        }

        /// <summary>Liest eine Protokolldatei, auch wenn ein Schreiber sie offen haelt; <c>null</c> = fehlt.</summary>
        private static byte[] Lesen(string ordner, string datei)
        {
            if (string.IsNullOrWhiteSpace(ordner)) return null;
            try
            {
                string pfad = Path.Combine(ordner, datei);
                if (!File.Exists(pfad)) return null;
                using var strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var puffer = new MemoryStream();
                strom.CopyTo(puffer);
                return puffer.ToArray();
            }
            catch { return null; }
        }

        /// <summary>
        /// Der Auszug aus <c>Tab_Katalogabgleich</c>: Spaltenkoepfe und je Zeile die Werte,
        /// tabulatorgetrennt, neueste zuerst; <c>null</c>, wenn es Datenbank oder Tabelle nicht gibt.
        /// Liest ueber <see cref="Katalogabgleich.Protokoll"/> — kein eigenes SQL.
        /// </summary>
        private static byte[] KatalogabgleichAuszug()
        {
            try
            {
                if (!DataRepository.DatenbankVorhanden()) return null;
                if (!DataRepository.TabelleVorhanden(KatalogfassungSchema.TAB_ABGLEICH)) return null;
                var sb = new StringBuilder();
                sb.Append(string.Join("\t", "Zeitpunkt", "Fassung", "Tabelle", "Schluessel", "Aktion", "Hinweis")).Append("\r\n");
                foreach (KatalogabgleichProtokollzeile z in Katalogabgleich.Protokoll(KATALOGABGLEICH_ZEILEN))
                {
                    sb.Append(string.Join("\t",
                        Feld(z.Zeitpunkt), z.Fassung.ToString(CultureInfo.InvariantCulture), Feld(z.Tabelle),
                        Feld(z.Schluessel), Feld(z.Aktion), Feld(z.Hinweis))).Append("\r\n");
                }
                return new UTF8Encoding(true).GetBytes(sb.ToString());
            }
            catch { return null; }
        }

        /// <summary>Ein Feld ohne Tabulator und Zeilenwechsel.</summary>
        private static string Feld(string wert) =>
            (wert ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

        private static string Text(Func<string> quelle, string ersatz)
        {
            try { string t = quelle(); return string.IsNullOrEmpty(t) ? ersatz : t; }
            catch { return ersatz; }
        }
    }
}
