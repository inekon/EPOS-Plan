using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Der gelesene Katalog</b> — genau das, was <see cref="ZapfprofilRechner.Rechnen"/> braucht:
    /// Nutzungsarten samt Tagesgangsätzen, der Parametersatz und die Zapfkategorien der Stochastik.
    /// Nichts davon wird geschrieben; der Katalog ist eine Momentaufnahme im Arbeitsspeicher.
    /// </summary>
    internal sealed class Katalog
    {
        internal IReadOnlyList<Nutzungsart> Arten { get; init; } = new Nutzungsart[0];
        internal IReadOnlyList<Tagesgangsatz> Saetze { get; init; } = new Tagesgangsatz[0];
        internal Parametersatz Parameter { get; init; }
        internal IReadOnlyList<Zapfkategorie> Kategorien { get; init; } = new Zapfkategorie[0];

        /// <summary>Woher der Katalog kommt — der Dateiname oder der Ordnername, für den Bericht.</summary>
        internal string Herkunft { get; init; } = "";

        /// <summary>Was beim Lesen zu nennen war (fehlende Tabelle, nicht gelesene Spaltenform).</summary>
        internal IReadOnlyList<string> Hinweise { get; init; } = new string[0];

        /// <summary>Die Nutzungsart mit diesem Bezeichner; <c>null</c> = keine.</summary>
        internal Nutzungsart Suchen(string bezeichner)
            => Arten.FirstOrDefault(a => string.Equals(a.Name, bezeichner, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <b>Die Katalogquelle</b>: eine <c>.sqlite</c>-Datei (Vorgabe
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>) <b>oder</b> ein Paketordner im Format N2
    /// (Kapitel 6 (b): je Tabelle eine <c>&lt;Tabelle&gt;.csv</c>, UTF-8, Trenner <c>;</c>,
    /// Kopfzeile mit den Spaltennamen der Tabelle, Zahlen mit Punkt, leeres Feld = NULL).
    ///
    /// <para><b>Beide Wege enden im gleichen Bau.</b> Gelesen wird in beiden Fällen zu Zeilen von
    /// Feldname → Text; <see cref="Katalogbau"/> macht daraus die Modelle des Kerns. Ein zweiter
    /// Bauweg entsteht nicht.</para>
    ///
    /// <para><b>Der freie Paketteil allein genügt nicht.</b>
    /// <c>Referenzlaeufe/Katalogpaket_frei/</c> führt nur die Parameter, die im Repositorium stehen
    /// dürfen — die Kaltwasser-, Wohnen- und Zirkulationsparameter des Mengengerüsts fehlen dort.
    /// Ein Paketordner ist deshalb nur dann eine vollständige Katalogquelle, wenn seine
    /// <c>Tab_TwwParameter_STAMM.csv</c> alle Schlüssel führt, die die Rechnung braucht; sonst lehnt
    /// der Rechner benannt ab (<c>PARAMETER_FEHLT</c>). Das Beispiel des Werkzeugs bringt einen
    /// solchen vollständigen, <b>erfundenen</b> Paketteil mit.</para>
    /// </summary>
    internal static class Katalogquelle
    {
        internal const string DATEI_NUTZUNGSART = "Tab_TwwNutzungsart_STAMM.csv";
        internal const string DATEI_TAGESGANGSATZ = "Tab_TwwTagesgangsatz_STAMM.csv";
        internal const string DATEI_TAGESGANG = "Tab_TwwTagesgang_STAMM.csv";
        internal const string DATEI_PARAMETER = "Tab_TwwParameter_STAMM.csv";
        internal const string DATEI_ZAPFKATEGORIE = "Tab_TwwZapfkategorie_STAMM.csv";

        /// <summary>Die Katalogversion, die ein Paketordner trägt (die CSV-Dateien führen sie nicht).</summary>
        internal const string VERSION_PAKET = "PAKET";

        /// <summary>
        /// Liest den Katalog aus <paramref name="pfad"/>; <paramref name="fehler"/> trägt den Grund
        /// bei <c>null</c>.
        /// </summary>
        internal static Katalog Lesen(string pfad, out string fehler)
        {
            fehler = null;
            if (string.IsNullOrWhiteSpace(pfad)) { fehler = "Die Katalogquelle ist leer."; return null; }
            if (Directory.Exists(pfad)) return AusPaket(pfad, out fehler);
            if (File.Exists(pfad)) return AusDatenbank(pfad, out fehler);
            fehler = "Die Katalogquelle \"" + pfad + "\" gibt es weder als Ordner noch als Datei.";
            return null;
        }

        // =============================================================================
        //  Der Paketordner
        // =============================================================================

        private static Katalog AusPaket(string ordner, out string fehler)
        {
            fehler = null;
            var hinweise = new List<string>();
            List<Dictionary<string, string>> arten = Csv(ordner, DATEI_NUTZUNGSART, true, ref fehler);
            if (fehler != null) return null;
            List<Dictionary<string, string>> saetze = Csv(ordner, DATEI_TAGESGANGSATZ, true, ref fehler);
            if (fehler != null) return null;
            List<Dictionary<string, string>> gaenge = Csv(ordner, DATEI_TAGESGANG, true, ref fehler);
            if (fehler != null) return null;
            List<Dictionary<string, string>> parameter = Csv(ordner, DATEI_PARAMETER, true, ref fehler);
            if (fehler != null) return null;
            List<Dictionary<string, string>> kategorien = Csv(ordner, DATEI_ZAPFKATEGORIE, false, ref fehler);
            if (kategorien == null)
                hinweise.Add("Der Paketordner fuehrt keine " + DATEI_ZAPFKATEGORIE
                             + " - ohne Zapfkategorien rechnet nur der deterministische Weg.");

            return Katalogbau.Bauen(arten, saetze, gaenge, parameter, kategorien, VERSION_PAKET,
                                    Path.GetFileName(Path.GetFullPath(ordner).TrimEnd(Path.DirectorySeparatorChar)),
                                    hinweise, out fehler);
        }

        /// <summary>Liest eine CSV-Datei des Pakets; <c>pflicht</c> = eine fehlende Datei ist ein Fehler.</summary>
        private static List<Dictionary<string, string>> Csv(string ordner, string datei, bool pflicht, ref string fehler)
        {
            string pfad = Path.Combine(ordner, datei);
            if (!File.Exists(pfad))
            {
                if (pflicht) fehler = "Im Paketordner fehlt " + datei + ".";
                return null;
            }
            string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);
            if (zeilen.Length == 0) { fehler = datei + " ist leer."; return null; }
            string[] kopf = Felder(zeilen[0]);
            var liste = new List<Dictionary<string, string>>();
            for (int i = 1; i < zeilen.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(zeilen[i])) continue;
                string[] f = Felder(zeilen[i]);
                if (f.Length != kopf.Length)
                {
                    fehler = datei + ", Zeile " + (i + 1).ToString(CultureInfo.InvariantCulture) + ": "
                             + f.Length.ToString(CultureInfo.InvariantCulture) + " Felder, aber "
                             + kopf.Length.ToString(CultureInfo.InvariantCulture) + " Spalten im Kopf.";
                    return null;
                }
                var zeile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int s = 0; s < kopf.Length; s++)
                    zeile[kopf[s]] = f[s].Length == 0 ? null : f[s];
                liste.Add(zeile);
            }
            return liste;
        }

        /// <summary>
        /// Zerlegt eine Zeile am <c>;</c>; ein Feld in doppelten Anführungszeichen darf den Trenner
        /// führen (<c>""</c> ist ein Anführungszeichen). Zeilenumbrüche in Feldern kommen im
        /// Paketformat nicht vor und werden nicht gelesen.
        /// </summary>
        internal static string[] Felder(string zeile)
        {
            var felder = new List<string>();
            var sb = new StringBuilder();
            bool inAnfuehrung = false;
            for (int i = 0; i < zeile.Length; i++)
            {
                char c = zeile[i];
                if (inAnfuehrung)
                {
                    if (c == '"' && i + 1 < zeile.Length && zeile[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') inAnfuehrung = false;
                    else sb.Append(c);
                }
                else if (c == '"' && sb.Length == 0) inAnfuehrung = true;
                else if (c == ';') { felder.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            felder.Add(sb.ToString());
            for (int i = 0; i < felder.Count; i++) felder[i] = felder[i].Trim();
            return felder.ToArray();
        }

        // =============================================================================
        //  Die SQLite-Datei
        // =============================================================================

        private static Katalog AusDatenbank(string datei, out string fehler)
        {
            fehler = null;
            var hinweise = new List<string>();
            try
            {
                using var v = Sqlitehilfe.Unveraenderlich(datei);
                foreach (string t in new[] { "Tab_TwwNutzungsart_STAMM", "Tab_TwwTagesgangsatz_STAMM",
                                             "Tab_TwwTagesgang_STAMM", "Tab_TwwParameter_STAMM" })
                    if (!Sqlitehilfe.TabelleVorhanden(v, t))
                    {
                        fehler = "In \"" + Path.GetFileName(datei) + "\" fehlt die Tabelle " + t
                                 + " - die Datei fuehrt keinen Brauchwasserkatalog.";
                        return null;
                    }

                List<Dictionary<string, string>> arten = Sqlitehilfe.Tabelle(v, Sqlitehilfe.SQL_NUTZUNGSART);
                List<Dictionary<string, string>> saetze = Sqlitehilfe.Tabelle(v, Sqlitehilfe.SQL_TAGESGANGSATZ);
                List<Dictionary<string, string>> gaenge = Sqlitehilfe.Tabelle(v, Sqlitehilfe.SQL_TAGESGANG);
                List<Dictionary<string, string>> parameter = Sqlitehilfe.Tabelle(v, Sqlitehilfe.SQL_PARAMETER);
                List<Dictionary<string, string>> kategorien = null;
                if (Sqlitehilfe.TabelleVorhanden(v, "Tab_TwwZapfkategorie_STAMM"))
                    kategorien = Sqlitehilfe.Tabelle(v, Sqlitehilfe.SQL_ZAPFKATEGORIE);
                else
                    hinweise.Add("Die Datei fuehrt keine Tab_TwwZapfkategorie_STAMM - ohne Zapfkategorien "
                                 + "rechnet nur der deterministische Weg.");

                return Katalogbau.Bauen(arten, saetze, gaenge, parameter, kategorien, null,
                                        Path.GetFileName(datei), hinweise, out fehler);
            }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException || ex is IOException
                                      || ex is InvalidOperationException)
            {
                fehler = "Die Katalogdatei \"" + Path.GetFileName(datei) + "\" ist nicht lesbar: " + ex.Message;
                return null;
            }
        }
    }
}
