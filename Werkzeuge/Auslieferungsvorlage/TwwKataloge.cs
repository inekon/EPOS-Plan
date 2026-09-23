using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// <b>Die Kataloge des Zapfprofilgenerators in der Vorlage</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Abschnitt 3.2 und Kapitel 6 (b), (c); Stufe Z0, Posten P10).
    ///
    /// <para><b>Eine eigene Regel, unabhängig von <c>--kataloge</c>.</b> Die
    /// <c>Tab_Tww*_STAMM</c>-Tabellen führen ihre Auslieferungsmarke in <c>Status</c>, nicht
    /// in <c>ReadOnly</c>: In die Vorlage gehört genau, was <c>Status = 'AUSLIEFERUNG'</c>
    /// trägt. Zeilen mit <c>Status = 'IMPORT'</c> (mit einem Projekt mitgenommen) und
    /// <c>'EIGEN'</c> (Anwenderkopien, der fiktive Testkatalog) fallen, ebenso jede Zeile mit
    /// Herkunftsart <c>FIKTIV</c> oder <c>IMPORT</c> (Normimport, nie in der Auslieferung).
    /// Die ReadOnly-Regel von <c>--kataloge readonly</c> rührt diese Tabellen deshalb nicht
    /// an (<see cref="IstTww"/>). Gelöscht wird bei eingeschalteten Fremdschlüsseln — die
    /// Kaskaden nehmen Tagesgänge und Ereignisse mit; die Reihenfolge Nutzungsart vor
    /// Tagesgangsatz folgt dem Verweis <c>ID_Tagesgangsatz</c> (ohne Kaskade). Was bleibt,
    /// bekommt <c>ReadOnly = 1</c>: Eine Auslieferungszeile ist unveränderlich (Konzept 3.1,
    /// K7), und die Prüfung verlangt es für jede Zeile mit Status <c>AUSLIEFERUNG</c>.</para>
    ///
    /// <para><b>Die Projekttabellen</b> <c>Tab_TwwZone</c>, <c>Tab_TwwWohnungstyp</c> und
    /// <c>Tab_TwwProjekt</c> sind Projektdaten und fallen in Schritt 2 wie alle anderen
    /// (<see cref="Projektsicht"/> erkennt sie über <c>ID_Projekt</c> bzw. den
    /// Fremdschlüssel auf die Zone).</para>
    ///
    /// <para><b>Das Katalogpaket</b> (<c>--katalogpaket &lt;ordner&gt;</c>, Frage ZU14) bringt
    /// die Auslieferungswerte von außerhalb des Repositoriums: je Tabelle eine Datei
    /// <c>&lt;Tabelle&gt;.csv</c> (UTF-8, Kopfzeile mit Spaltennamen, Trenner <c>;</c> oder
    /// <c>,</c>, Zahlen mit Punkt). Jede Kopfzeile trägt <c>Status = 'AUSLIEFERUNG'</c>;
    /// <c>ReadOnly</c> ist 1 oder fehlt (dann 1). Das Paket ERSETZT den Tww-Katalog der
    /// Quelle; ohne Paket bleibt, was die Quelle mit Status <c>AUSLIEFERUNG</c> führt — in
    /// der Testdatenbank nichts.</para>
    /// </summary>
    internal sealed class TwwKataloge
    {
        /// <summary>Die Kopftabellen in LÖSCHreihenfolge (Verweisende vor Verwiesenen).</summary>
        internal static readonly string[] KOEPFE =
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        /// <summary>Kindtabelle, Kopftabelle, Verweisspalte — Löschen über die Kaskade.</summary>
        internal static readonly (string Kind, string Kopf, string Spalte)[] KINDER =
        {
            (TwwSchema.TAB_TWW_TAGESGANG_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "ID_Tagesgangsatz"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "ID_Bedarfstag")
        };

        /// <summary>Die Tabellen in EINSPIELreihenfolge des Katalogpakets (Verwiesene zuerst).</summary>
        internal static readonly string[] PAKETREIHENFOLGE =
        {
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        /// <summary>Die Typtag-Ablage des lizenzierten Anwenders (Schritt T3) — nie in der Vorlage.</summary>
        internal const string TAB_TYPTAG_IMPORT = "Tab_TwwTyptag_IMPORT";

        /// <summary>Die lokalen Normdaten (ZU11) — keine Eingabe des Laufs darf dort liegen.</summary>
        internal const string NORMZAHLEN = "Referenzlaeufe/Normzahlen/";

        private readonly Bericht _bericht;

        internal TwwKataloge(Bericht bericht) { _bericht = bericht; }

        /// <summary>Gehört die Tabelle zum Zapfprofilgenerator (eigene Katalogregel)?</summary>
        internal static bool IstTww(string tabelle) =>
            tabelle != null && tabelle.StartsWith("Tab_Tww", StringComparison.Ordinal);

        private static IEnumerable<string> Vorhandene(IEnumerable<string> tabellen) =>
            tabellen.Where(DataRepository.TabelleVorhanden);

        // =================================================================================
        //  SCHRITT 3c - die Regel
        // =================================================================================

        /// <summary>
        /// Wendet die Tww-Regel an: nur <c>Status = 'AUSLIEFERUNG'</c> ohne Herkunftsart
        /// <c>FIKTIV</c>/<c>IMPORT</c> bleibt; <c>Tab_TwwTyptag_IMPORT</c> wird geleert.
        /// Rückgabe <c>false</c>, wenn die Fremdschlüssel nicht eingeschaltet sind.
        /// </summary>
        internal bool Bereinigen()
        {
            _bericht.Abschnitt("Schritt 3c — Zapfprofil-Kataloge (Tww), eigene Regel");
            _bericht.Zeile("Regel: es bleibt Status = 'AUSLIEFERUNG' ohne Herkunftsart FIKTIV oder IMPORT;");
            _bericht.Zeile("       IMPORT und EIGEN fallen — unabhaengig von --kataloge (Konzept 3.2, 6 (b)).");
            _bericht.Leer();

            List<string> tabellen = Vorhandene(KOEPFE.Concat(KINDER.Select(k => k.Kind))).ToList();
            if (tabellen.Count == 0)
            {
                _bericht.Zeile("keine Tww-Tabelle im Schema — nichts zu tun");
                return true;
            }

            Dictionary<string, long> vorher = Zaehlen(tabellen);

            // Die Anweisungen entstehen VOR der Transaktion: Die Schemaauskunft laeuft ueber
            // eine eigene Verbindung und soll nicht neben einer offenen Schreibtransaktion lesen.
            var anweisungen = new List<(string Sql, DbParam[] Werte)>();
            foreach (string t in Vorhandene(KOEPFE))
            {
                var bedingung = new List<string>();
                var werte = new List<DbParam>();
                if (DataRepository.SpalteVorhanden(t, "Status"))
                {
                    bedingung.Add("\"Status\" IS NULL OR \"Status\" <> ?");
                    werte.Add(new DbParam("?", TwwSchema.STATUS_AUSLIEFERUNG));
                }
                foreach (string h in Herkunftsspalten(t))
                {
                    bedingung.Add("\"" + h + "\" IN (?, ?)");
                    werte.Add(new DbParam("?", TwwSchema.HERKUNFT_FIKTIV));
                    werte.Add(new DbParam("?", TwwSchema.HERKUNFT_IMPORT));
                }
                if (t == TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM))
                {
                    // Der Satz traegt keine eigene Herkunft — die steht an seinen Tagesgaengen.
                    bedingung.Add("\"ID\" IN (SELECT \"ID_Tagesgangsatz\" FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                  "\" WHERE \"Herkunftsart\" IN (?, ?))");
                    werte.Add(new DbParam("?", TwwSchema.HERKUNFT_FIKTIV));
                    werte.Add(new DbParam("?", TwwSchema.HERKUNFT_IMPORT));
                }
                if (bedingung.Count == 0) continue;

                string sql = "DELETE FROM \"" + t + "\" WHERE ((" + string.Join(") OR (", bedingung) + "))";
                if (t == TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM))
                    // Ein Satz, auf den eine verbliebene Nutzungsart zeigt, bleibt stehen
                    // (Verweis ohne Kaskade) — die Pruefung meldet ihn dann mit Namen.
                    sql += " AND \"ID\" NOT IN (SELECT \"ID_Tagesgangsatz\" FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                           "\" WHERE \"ID_Tagesgangsatz\" IS NOT NULL)";
                anweisungen.Add((sql, werte.ToArray()));
            }

            // Waisen aus einem Altbestand (die Kaskade raeumt nur, was sie selbst loest).
            foreach (var k in KINDER)
                if (DataRepository.TabelleVorhanden(k.Kind) && DataRepository.TabelleVorhanden(k.Kopf))
                    anweisungen.Add(("DELETE FROM \"" + k.Kind + "\" WHERE \"" + k.Spalte + "\" NOT IN (SELECT \"ID\" FROM \"" +
                                     k.Kopf + "\")", new DbParam[0]));

            bool typtage = DataRepository.TabelleVorhanden(TAB_TYPTAG_IMPORT);
            if (typtage) anweisungen.Add(("DELETE FROM \"" + TAB_TYPTAG_IMPORT + "\"", new DbParam[0]));

            // Was bleibt, ist Auslieferung — und die ist unveraenderlich (Konzept 3.1, 3.2, K7):
            // ReadOnly = 1, sonst waere eine ausgelieferte, noch unbenutzte Zeile beim Anwender
            // aenderbar und loeschbar.
            List<string> mitReadOnly = Vorhandene(KOEPFE)
                .Where(t => DataRepository.SpalteVorhanden(t, "ReadOnly") && DataRepository.SpalteVorhanden(t, "Status"))
                .ToList();

            long gesperrt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                long fk = Convert.ToInt64(v.Skalar("PRAGMA foreign_keys"));
                _bericht.Zeile((fk == 1 ? "ok      " : "FEHLER  ") + "Fremdschluessel eingeschaltet (PRAGMA foreign_keys = " +
                               fk.ToString(CultureInfo.InvariantCulture) + ")");
                if (fk != 1) return false;

                foreach (var (sql, werte) in anweisungen) v.Ausfuehren(sql, werte);
                foreach (string t in mitReadOnly)
                    gesperrt += v.Ausfuehren("UPDATE \"" + t + "\" SET \"ReadOnly\" = 1 WHERE \"Status\" = ? AND " +
                                             "(\"ReadOnly\" IS NULL OR \"ReadOnly\" <> 1)",
                                             new DbParam("?", TwwSchema.STATUS_AUSLIEFERUNG));
                v.Commit();
            }

            Dictionary<string, long> nachher = Zaehlen(tabellen);
            _bericht.Leer();
            _bericht.Tabellenkopf("Tww-Katalogtabelle", "vorher", "nachher");
            foreach (string t in tabellen) _bericht.Tabellenzeile(t, vorher[t], nachher[t]);
            _bericht.Zeile("ReadOnly = 1 gesetzt: " + gesperrt.ToString(CultureInfo.InvariantCulture) +
                           " Zeile(n) mit Status AUSLIEFERUNG (Auslieferung ist unveraenderlich)");
            if (typtage)
                _bericht.Zeile(TAB_TYPTAG_IMPORT + " geleert (Typtage des lizenzierten Anwenders, nie in der Vorlage)");
            return true;
        }

        // =================================================================================
        //  Das Katalogpaket
        // =================================================================================

        /// <summary>
        /// Spielt das Katalogpaket ein. Vorher fallen die Tww-Katalogzeilen, die die Regel
        /// übrig ließ: Das Paket ist danach der ganze Tww-Katalog der Vorlage. Alles in
        /// EINER Transaktion; ein Fehler rollt zurück und nennt Datei, Zeile und Grund.
        /// </summary>
        internal bool PaketEinspielen(string ordner, out string fehler)
        {
            fehler = null;
            _bericht.Leer();
            _bericht.Zeile("Katalogpaket: " + (ordner ?? "keines (--katalogpaket fehlt) — die Vorlage fuehrt nur, " +
                                                          "was die Quelle mit Status AUSLIEFERUNG traegt"));
            if (ordner == null) return true;

            var dateien = new List<(string Tabelle, string Datei)>();
            foreach (string t in PAKETREIHENFOLGE)
            {
                string d = Path.Combine(ordner, t + ".csv");
                if (File.Exists(d)) dateien.Add((t, d));
            }

            // Spaltentypen und Kopfliste VOR der Transaktion (eigene Verbindung, siehe Bereinigen).
            var typen = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            foreach (string t in PAKETREIHENFOLGE) typen[t] = Spaltentypen(t);
            List<string> koepfe = Vorhandene(KOEPFE).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    long ersetzt = 0;
                    foreach (string t in koepfe)
                        ersetzt += v.Ausfuehren("DELETE FROM \"" + t + "\"");
                    _bericht.Zeile("ersetzt: " + ersetzt.ToString(CultureInfo.InvariantCulture) +
                                   " Tww-Kopfzeile(n) der Quelle (samt Tagesgaengen und Ereignissen)");

                    foreach (var (tabelle, datei) in dateien)
                    {
                        int n = DateiEinspielen(v, tabelle, datei, typen[tabelle]);
                        _bericht.Zeile("eingespielt: " + Path.GetFileName(datei) + "  ->  " +
                                       n.ToString(CultureInfo.InvariantCulture) + " Zeile(n)");
                    }
                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    fehler = "Katalogpaket " + ordner + ": " + ex.Message;
                    _bericht.Zeile("FEHLER  " + fehler);
                    return false;
                }
            }
        }

        private static int DateiEinspielen(DbVorgang v, string tabelle, string datei, Dictionary<string, string> typen)
        {
            List<List<string>> zeilen = CsvLesen(File.ReadAllText(datei, Encoding.UTF8));
            if (zeilen.Count == 0) return 0;

            List<string> kopf = zeilen[0].Select(s => s.Trim()).ToList();
            foreach (string s in kopf)
                if (!typen.ContainsKey(s))
                    throw new InvalidDataException(Path.GetFileName(datei) + ": die Spalte \"" + s + "\" gibt es in " +
                                                   tabelle + " nicht.");

            bool mitStatus = typen.ContainsKey("Status");
            bool mitReadOnly = typen.ContainsKey("ReadOnly");
            if (mitStatus && !kopf.Contains("Status"))
                throw new InvalidDataException(Path.GetFileName(datei) + ": die Spalte Status fehlt — ein Katalogpaket " +
                                               "fuehrt nur Status AUSLIEFERUNG, und das steht in jeder Zeile.");

            var spalten = new List<string>(kopf);
            if (mitReadOnly && !kopf.Contains("ReadOnly")) spalten.Add("ReadOnly");

            string sql = "INSERT INTO \"" + tabelle + "\" (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                         ") VALUES (" + string.Join(", ", spalten.Select(_ => "?")) + ")";

            int n = 0;
            for (int i = 1; i < zeilen.Count; i++)
            {
                List<string> z = zeilen[i];
                if (z.Count == 1 && string.IsNullOrWhiteSpace(z[0])) continue;   // Leerzeile
                string ort = Path.GetFileName(datei) + " Zeile " + (i + 1).ToString(CultureInfo.InvariantCulture);
                if (z.Count != kopf.Count)
                    throw new InvalidDataException(ort + ": " + z.Count + " Felder, die Kopfzeile nennt " + kopf.Count + ".");

                var werte = new List<DbParam>();
                for (int c = 0; c < kopf.Count; c++)
                {
                    object w = Wert(z[c], typen[kopf[c]], ort + ", Spalte " + kopf[c]);
                    if (kopf[c] == "Status" && !string.Equals(Convert.ToString(w), TwwSchema.STATUS_AUSLIEFERUNG, StringComparison.Ordinal))
                        throw new InvalidDataException(ort + ": Status \"" + Convert.ToString(w) + "\" — ein Katalogpaket " +
                                                       "fuehrt nur Status AUSLIEFERUNG.");
                    if (kopf[c] == "ReadOnly" && !(w is long ro && ro == 1))
                        throw new InvalidDataException(ort + ": ReadOnly muss 1 sein (Auslieferung).");
                    werte.Add(new DbParam("?", w));
                }
                if (mitReadOnly && !kopf.Contains("ReadOnly")) werte.Add(new DbParam("?", 1L));

                v.Ausfuehren(sql, werte.ToArray());
                n++;
            }
            return n;
        }

        private static object Wert(string roh, string typ, string ort)
        {
            if (roh == null || roh.Length == 0) return DBNull.Value;
            string t = (typ ?? "").ToUpperInvariant();
            if (t.StartsWith("INT", StringComparison.Ordinal))
            {
                if (long.TryParse(roh.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                throw new InvalidDataException(ort + ": \"" + roh + "\" ist keine ganze Zahl.");
            }
            if (t.StartsWith("REAL", StringComparison.Ordinal))
            {
                if (double.TryParse(roh.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw new InvalidDataException(ort + ": \"" + roh + "\" ist keine Zahl (Dezimalpunkt).");
            }
            return roh;
        }

        private static Dictionary<string, string> Spaltentypen(string tabelle)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name, type FROM pragma_table_info(?)", new DbParam("?", tabelle));
            foreach (DataRow r in dt.Rows) d[Convert.ToString(r["name"])] = Convert.ToString(r["type"]);
            return d;
        }

        /// <summary>
        /// CSV nach RFC 4180: Felder in doppelten Anführungszeichen dürfen Trenner,
        /// Zeilenumbrüche und verdoppelte Anführungszeichen tragen. Der Trenner ist
        /// <c>;</c>, wenn die Kopfzeile einen enthält, sonst <c>,</c>.
        /// </summary>
        internal static List<List<string>> CsvLesen(string text)
        {
            var zeilen = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return zeilen;
            if (text[0] == '﻿') text = text.Substring(1);

            int kopfEnde = text.IndexOf('\n');
            string kopf = kopfEnde < 0 ? text : text.Substring(0, kopfEnde);
            char trenner = kopf.IndexOf(';') >= 0 ? ';' : ',';

            var zeile = new List<string>();
            var feld = new StringBuilder();
            bool inAnf = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inAnf)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { feld.Append('"'); i++; }
                        else inAnf = false;
                    }
                    else feld.Append(c);
                    continue;
                }
                if (c == '"') { inAnf = true; continue; }
                if (c == trenner) { zeile.Add(feld.ToString()); feld.Clear(); continue; }
                if (c == '\r') continue;
                if (c == '\n')
                {
                    zeile.Add(feld.ToString()); feld.Clear();
                    zeilen.Add(zeile); zeile = new List<string>();
                    continue;
                }
                feld.Append(c);
            }
            if (feld.Length > 0 || zeile.Count > 0) { zeile.Add(feld.ToString()); zeilen.Add(zeile); }
            return zeilen;
        }

        // =================================================================================
        //  Die Pruefposten
        // =================================================================================

        /// <summary>
        /// Die Tww-Posten der Abnahme (Konzept 3.2, 6 (b), (c)): keine Zeile mit Status
        /// <c>IMPORT</c>, nur <c>AUSLIEFERUNG</c>, keine verwaiste Kindzeile, keine Herkunftsart
        /// <c>FIKTIV</c>, keine Normdaten (Herkunftsart <c>IMPORT</c>, <c>Tab_TwwTyptag_IMPORT</c>),
        /// jede Auslieferungszeile <c>ReadOnly = 1</c>, keine Eingabe aus den lokalen Normdaten
        /// (ZU11). <paramref name="mitnahmen"/> nennt zu einer IMPORT-Zeile das Beispielpaket,
        /// das sie mitgebracht hat.
        /// </summary>
        internal bool Pruefen(IReadOnlyList<string> eingaben = null, IReadOnlyList<string> mitnahmen = null)
        {
            _bericht.Leer();
            _bericht.Zeile("Zapfprofil-Kataloge (Tww)");
            List<string> koepfe = Vorhandene(KOEPFE).ToList();
            if (koepfe.Count == 0)
            {
                _bericht.Zeile("ok      keine Tww-Tabelle im Schema");
                return true;
            }
            bool ok = true;

            // (1) Status IMPORT und (2) alles ausser AUSLIEFERUNG
            var import = new List<string>();
            var fremd = new List<string>();
            var beschreibbar = new List<string>();
            long auslieferung = 0;
            foreach (string t in koepfe.Where(t => DataRepository.SpalteVorhanden(t, "Status")))
            {
                long i = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = ?", TwwSchema.STATUS_IMPORT);
                long f = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" IS NULL OR \"Status\" <> ?",
                              TwwSchema.STATUS_AUSLIEFERUNG);
                auslieferung += Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = ?", TwwSchema.STATUS_AUSLIEFERUNG);
                if (i > 0) import.Add(t + ": " + i);
                if (f > 0) fremd.Add(t + ": " + f);
                if (DataRepository.SpalteVorhanden(t, "ReadOnly"))
                {
                    long b = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = ? AND " +
                                  "(\"ReadOnly\" IS NULL OR \"ReadOnly\" <> 1)", TwwSchema.STATUS_AUSLIEFERUNG);
                    if (b > 0) beschreibbar.Add(t + ": " + b);
                }
            }
            // Das verursachende Beispielpaket gleich mit nennen — sonst ist die Zeile schwer zu deuten.
            if (import.Count > 0 && mitnahmen != null)
                foreach (string m in mitnahmen) import.Add("mitgebracht von Beispielpaket " + m);
            ok &= Posten(import, "keine Zeile mit Status IMPORT");
            ok &= Posten(fremd, "nur Status AUSLIEFERUNG (keine Zeile EIGEN)");
            ok &= Posten(beschreibbar, "jede Zeile mit Status AUSLIEFERUNG traegt ReadOnly = 1");

            // (3) verwaiste Zeilen
            var waisen = new List<string>();
            foreach (var k in KINDER.Where(k => DataRepository.TabelleVorhanden(k.Kind) && DataRepository.TabelleVorhanden(k.Kopf)))
            {
                long w = Zahl("SELECT COUNT(*) FROM \"" + k.Kind + "\" WHERE \"" + k.Spalte + "\" NOT IN (SELECT \"ID\" FROM \"" +
                              k.Kopf + "\" WHERE \"Status\" <> ?)", TwwSchema.STATUS_IMPORT);
                if (w > 0) waisen.Add(k.Kind + ": " + w);
            }
            if (koepfe.Contains(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM) && koepfe.Contains(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM))
            {
                long w = Zahl("SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" WHERE \"ID_Tagesgangsatz\" " +
                              "NOT IN (SELECT \"ID\" FROM \"" + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + "\" WHERE \"Status\" <> ?)",
                              TwwSchema.STATUS_IMPORT);
                if (w > 0) waisen.Add(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " ohne Tagesgangsatz: " + w);
            }
            ok &= Posten(waisen, "keine verwaiste Zeile in " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " und " +
                                 TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + " (Kopf fehlt oder traegt IMPORT)");

            // (4) Herkunftsart FIKTIV und (5) Herkunftsart IMPORT (Normimport)
            var fiktiv = new List<string>();
            var normimport = new List<string>();
            foreach (string t in Vorhandene(KOEPFE.Concat(KINDER.Select(k => k.Kind))))
                foreach (string h in Herkunftsspalten(t))
                {
                    long f = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + h + "\" = ?", TwwSchema.HERKUNFT_FIKTIV);
                    long n = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + h + "\" = ?", TwwSchema.HERKUNFT_IMPORT);
                    if (f > 0) fiktiv.Add(t + "." + h + ": " + f);
                    if (n > 0) normimport.Add(t + "." + h + ": " + n);
                }
            ok &= Posten(fiktiv, "keine Zeile mit Herkunftsart FIKTIV (der Testkatalog bleibt draussen)");

            // (6) Lokale Normdaten (ZU11): keine Eingabe des Laufs liegt dort.
            var normpfade = new List<string>();
            foreach (string p in eingaben ?? new List<string>())
                if (UnterNormzahlen(p)) normpfade.Add(p);
            ok &= Posten(normpfade, "keine Eingabe aus den lokalen Normdaten (ZU11, " + NORMZAHLEN +
                                    "; Quelle, Katalogpaket, Beispiele)");

            if (DataRepository.TabelleVorhanden(TAB_TYPTAG_IMPORT))
            {
                long n = Zahl("SELECT COUNT(*) FROM \"" + TAB_TYPTAG_IMPORT + "\"", null);
                if (n > 0) normimport.Add(TAB_TYPTAG_IMPORT + ": " + n);
            }
            ok &= Posten(normimport, "keine Zeile aus einem Normimport (Herkunftsart IMPORT, " + TAB_TYPTAG_IMPORT + ")");

            _bericht.Zeile("        Tww-Auslieferungszeilen (Status AUSLIEFERUNG): " +
                           auslieferung.ToString(CultureInfo.InvariantCulture));
            return ok;
        }

        /// <summary>
        /// Liegt der Pfad unter einem Ordner <c>Referenzlaeufe/Normzahlen</c>? Verglichen wird
        /// der volle Pfad (<see cref="Path.GetFullPath(string)"/>) Segment für Segment, ohne
        /// Groß- und Kleinschreibung — ein Repository-Ort ist dafür nicht nötig.
        /// </summary>
        internal static bool UnterNormzahlen(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) return false;
            string[] teile = Path.GetFullPath(pfad).Replace('\\', '/')
                                 .Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < teile.Length; i++)
                if (string.Equals(teile[i], "Referenzlaeufe", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(teile[i + 1], "Normzahlen", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private bool Posten(List<string> befunde, string text)
        {
            if (befunde.Count == 0) { _bericht.Zeile("ok      " + text); return true; }
            _bericht.Zeile("FEHLER  " + text + " — verletzt:");
            foreach (string b in befunde) _bericht.Zeile("            " + b);
            return false;
        }

        private static long Zahl(string sql, string wert)
        {
            object o = wert == null
                ? DataRepository.ExecuteScalar(sql)
                : DataRepository.ExecuteScalar(sql, new DbParam("?", wert));
            return o == null || o == DBNull.Value ? 0L : Convert.ToInt64(o);
        }

        /// <summary>Die Herkunftsspalten einer Tabelle: <c>Herkunftsart</c> und <c>*_Herkunftsart</c>.</summary>
        internal static List<string> Herkunftsspalten(string tabelle) =>
            DataRepository.SpaltenVonTabelle(tabelle)
                .Where(s => s == "Herkunftsart" || s.EndsWith("_Herkunftsart", StringComparison.Ordinal))
                .ToList();

        private static Dictionary<string, long> Zaehlen(IEnumerable<string> tabellen)
        {
            var d = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string t in tabellen) d[t] = Vorlagenbau.Zaehle(t);
            return d;
        }
    }
}
