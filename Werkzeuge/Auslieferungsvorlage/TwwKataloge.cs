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
    /// Kaskaden nehmen Tagesgänge, Ereignisse und die Zapfkategorien einer fallenden
    /// Nutzungsart mit; die Reihenfolge Nutzungsart vor Tagesgangsatz folgt dem Verweis
    /// <c>ID_Tagesgangsatz</c> (ohne Kaskade). Die Zapfkategorien (Schemaschritt T2) tragen
    /// eigenen Status und eigene Herkunft und folgen der Regel wie ein Kopf. Was bleibt,
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
    ///
    /// <para><b>Der freie Paketteil</b> (<c>Referenzlaeufe/Katalogpaket_frei/</c>, Stufe Z3) bringt
    /// die Daten des Repositoriums, die ausgeliefert werden dürfen, in JEDE Vorlage
    /// (<see cref="PaketteilEinspielen"/>), nach dem Katalogpaket, das ihn nur ersetzt, wo es
    /// dieselbe Zeile führt: Parameter der Stochastik, Ecodesign-Zapfprofil, Zapfkategorien nach
    /// Jordan/Vajen und — mit ZU20 — die fünf aus VDI 6002 <b>abgeleiteten</b> Nutzungsarten samt
    /// ihren Tagesgangsätzen und Tagesgängen (Herkunftsart <c>VERFAHREN</c>, Quelle „abgeleitet aus
    /// VDI 6002 Blatt n"; ihre Zahl steht in keiner Richtlinie).</para>
    /// </summary>
    internal sealed class TwwKataloge
    {
        /// <summary>Die Kopftabellen in LÖSCHreihenfolge (Verweisende vor Verwiesenen).</summary>
        internal static readonly string[] KOEPFE =
        {
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
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

        /// <summary>
        /// Köpfe mit eigenem Status, die an einem anderen Kopf hängen (Kaskade): Tabelle,
        /// Kopftabelle, Verweisspalte — für die Waisenprüfung wie <see cref="KINDER"/>.
        /// </summary>
        internal static readonly (string Kind, string Kopf, string Spalte)[] ABHAENGIGE =
        {
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "ID_Nutzungsart")
        };

        /// <summary>Die Tabellen in EINSPIELreihenfolge des Katalogpakets (Verwiesene zuerst).</summary>
        internal static readonly string[] PAKETREIHENFOLGE =
        {
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        /// <summary>Die Typtag-Ablage des lizenzierten Anwenders (Schritt T3) — nie in der Vorlage.</summary>
        internal const string TAB_TYPTAG_IMPORT = TwwSchema.TAB_TWW_TYPTAG_IMPORT;

        /// <summary>
        /// Die Messreihen-Ablage des Anwenders (Schritt T4) — nie in der Vorlage. Sie hängt an
        /// <c>ID_Projekt</c> und überlebte damit als Zeile eines Beispielprojekts; gemessene Daten
        /// gehören aber dem Objekt (Konzept Kapitel 9 K5), nie der Auslieferung.
        /// </summary>
        internal const string TAB_MESSREIHE = TwwSchema.TAB_TWW_MESSREIHE;

        /// <summary>Die Zeilen des Bedarfstag-Konstruktors (Schemaschritt T5) — nie in der Vorlage.</summary>
        internal const string TAB_KONSTRUKTORZEILE = TwwSchema.TAB_TWW_KONSTRUKTORZEILE;

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
            foreach (var k in KINDER.Concat(ABHAENGIGE))
                if (DataRepository.TabelleVorhanden(k.Kind) && DataRepository.TabelleVorhanden(k.Kopf))
                    anweisungen.Add(("DELETE FROM \"" + k.Kind + "\" WHERE \"" + k.Spalte + "\" NOT IN (SELECT \"ID\" FROM \"" +
                                     k.Kopf + "\")", new DbParam[0]));

            bool typtage = DataRepository.TabelleVorhanden(TAB_TYPTAG_IMPORT);
            if (typtage) anweisungen.Add(("DELETE FROM \"" + TAB_TYPTAG_IMPORT + "\"", new DbParam[0]));

            // Die Messreihen des Anwenders (Schritt T4) - auch die eines Beispielprojekts: Sie
            // haengen an ID_Projekt, wuerden also mit dem Beispielprojekt ueberleben. Gemessene
            // Daten gehoeren dem Objekt (Konzept Kapitel 9 K5), nie der Auslieferung.
            bool messreihen = DataRepository.TabelleVorhanden(TAB_MESSREIHE);
            if (messreihen) anweisungen.Add(("DELETE FROM \"" + TAB_MESSREIHE + "\"", new DbParam[0]));

            // Die Zeilen des Bedarfstag-Konstruktors (Schritt T5, ZU25) - auch die eines
            // Beispielprojekts: Der konstruierte Tag selbst traegt Status EIGEN und faellt eine
            // Regel weiter oben; seine Zeilen haetten danach niemanden mehr, den sie beschreiben.
            bool konstruktorzeilen = DataRepository.TabelleVorhanden(TAB_KONSTRUKTORZEILE);
            if (konstruktorzeilen) anweisungen.Add(("DELETE FROM \"" + TAB_KONSTRUKTORZEILE + "\"", new DbParam[0]));

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
            if (messreihen)
                _bericht.Zeile(TAB_MESSREIHE + " geleert (Messreihen des Anwenders, nie in der Vorlage - K5)");
            if (konstruktorzeilen)
                _bericht.Zeile(TAB_KONSTRUKTORZEILE + " geleert (Zeilen des Bedarfstag-Konstruktors, " +
                               "nie in der Vorlage - der konstruierte Tag selbst faellt als EIGEN)");
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
            foreach (string t in PAKETREIHENFOLGE) typen[t] = TwwPaketteilCtrl.Spaltentypen(t);
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

                    var staende = new List<string>();
                    foreach (var (tabelle, datei) in dateien)
                    {
                        int n = DateiEinspielen(v, tabelle, datei, typen[tabelle], staende);
                        _bericht.Zeile("eingespielt: " + Path.GetFileName(datei) + "  ->  " +
                                       n.ToString(CultureInfo.InvariantCulture) + " Zeile(n)");
                    }
                    foreach (string s in staende) _bericht.Zeile("frueherer Stand: " + s);
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

        // =================================================================================
        //  Der freie Paketteil (Referenzlaeufe/Katalogpaket_frei)
        // =================================================================================

        /// <summary>Der freie Paketteil, relativ zur Repowurzel.</summary>
        internal const string PAKETTEIL_FREI = "Referenzlaeufe/Katalogpaket_frei";

        /// <summary>Die Tabellen des Paketteils in Einspielreihenfolge — die des Kerns (<see cref="TwwPaketteilCtrl.TABELLEN"/>).</summary>
        internal static readonly string[] PAKETTEIL_TABELLEN = TwwPaketteilCtrl.TABELLEN;

        /// <summary>
        /// Die Herkunftsarten, die eine Zeile des freien Paketteils tragen darf (<c>FREI</c>,
        /// <c>VERFAHREN</c>, <c>EIGENKONSTRUKTION</c>) — die des Kerns (<see cref="TwwPaketteilCtrl.HERKUNFT"/>).
        /// </summary>
        internal static readonly string[] PAKETTEIL_HERKUNFT = TwwPaketteilCtrl.HERKUNFT;

        /// <summary>Die Platzhalter für <see cref="PAKETTEIL_HERKUNFT"/> in einem <c>IN (…)</c>.</summary>
        private static string PaketteilHerkunftIn() => "IN (" + string.Join(", ", PAKETTEIL_HERKUNFT.Select(_ => "?")) + ")";

        /// <summary>
        /// Der Ordner des freien Paketteils: unter der Repowurzel (erkennbar an <c>WP-Plan.sln</c>)
        /// oberhalb des Werkzeugs, sonst oberhalb des Laufordners; <c>null</c>, wenn keine Wurzel
        /// zu finden ist.
        /// </summary>
        internal static string PaketteilOrdner()
        {
            string wurzel = Schreibort.Wurzel(AppContext.BaseDirectory) ?? Schreibort.Wurzel(Directory.GetCurrentDirectory());
            return wurzel == null ? null : Path.Combine(wurzel, PAKETTEIL_FREI.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// <b>Spielt den freien Paketteil ein</b> (Stufe Z3): die Daten des Zapfprofilgenerators, die
        /// im Repositorium stehen dürfen — Parameter der Stochastik, Ecodesign-Zapfprofil samt
        /// Ereignissen, Zapfkategorien nach Jordan/Vajen und die fünf aus VDI 6002 abgeleiteten
        /// Nutzungsarten samt Tagesgangsätzen und Tagesgängen (ZU20) —, ohne die die Auslieferung weder
        /// stochastisch rechnet noch die Bedarfstag-Quelle (5) anbietet noch einen Katalog der
        /// Nutzungsarten führt. Immer, nach dem externen
        /// Katalogpaket; das Ergebnis ist dasselbe wie „Paketteil zuerst, das Katalogpaket ersetzt
        /// ihn nur, wo es dieselbe Zeile führt".
        ///
        /// <para><b>Der Einspielweg des Kerns.</b> Gelesen wird der Ordner mit dem Leser des
        /// Katalogimports (<see cref="TwwNutzungsartCtrl.PaketLesen"/>), eingespielt über
        /// <see cref="TwwPaketteilCtrl.Einspielen"/> — denselben Weg, auf dem der Kern einer älteren
        /// Datenbank den Paketteil selbst nachlädt. Format, Regeln des Paketteils und Bindung der
        /// Vorgabesätze beschreibt der Kern; die Katalogversion der Zeilen kommt aus der EINEN Regel
        /// des Kerns (<see cref="ZapfprofilCtrl.Zielkatalogversion"/>, Rückfall
        /// <see cref="ZapfprofilCtrl.KATALOGVERSION_RUECKFALL"/>), derselben, die der Katalogimport
        /// für ein Paket ohne Katalogversion nimmt (N38). <b>Schlüsselgleichheit:</b> Führt das
        /// Katalogpaket dieselbe Zeile, gilt seine (<see cref="TwwPaketteilCtrl.VORRANG_KATALOGPAKET"/>),
        /// und der Bericht meldet es; ohne Katalogpaket ersetzt der Paketteil eine gleiche Zeile der
        /// Quelle. Ein Fehler nennt Datei, Zeile und Grund und rollt den ganzen Paketteil zurück.</para>
        /// </summary>
        internal bool PaketteilEinspielen(string ordner, bool mitKatalogpaket, out string fehler)
        {
            fehler = null;
            _bericht.Leer();
            _bericht.Zeile("Freier Paketteil: " + (ordner ?? "(Repowurzel nicht gefunden)"));
            if (ordner == null || !Directory.Exists(ordner))
            {
                fehler = "Der freie Paketteil fehlt (" + (ordner ?? PAKETTEIL_FREI) + ") — ohne ihn rechnet die Auslieferung " +
                         "nicht stochastisch. Das Werkzeug laeuft aus dem Repository.";
                _bericht.Zeile("FEHLER  " + fehler);
                return false;
            }
            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(ordner, out ZapfSatz lesefehler);
            if (lesefehler != null)
            {
                fehler = "Freier Paketteil " + ordner + ": " + lesefehler.Klartext;
                _bericht.Zeile("FEHLER  " + fehler);
                return false;
            }
            return TwwPaketteilCtrl.Einspielen(dateien, ordner, mitKatalogpaket ? TwwPaketteilCtrl.VORRANG_KATALOGPAKET : null,
                                               false, _bericht.Zeile, out fehler, out _);
        }

        /// <summary>
        /// Spielt eine Datei des Katalogpakets ein (Status <c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1). Eine
        /// Nutzungsart des ausgelieferten Paketteils in einem früheren Stand — ein Katalogpaket, das vor
        /// der Bezugsart Zimmer gebaut wurde — kommt unter ihrem heutigen Bezeichner und ihrer heutigen
        /// Bezugsart hinein (<see cref="PaketteilNachfuehrung"/>, dieselbe Regel wie Schemaschritt und
        /// Katalogimport); <paramref name="staende"/> nimmt je solcher Zeile eine Berichtszeile auf. So
        /// trifft der Paketteil danach den natürlichen Schlüssel, statt eine zweite Zeile anzulegen.
        /// </summary>
        private static int DateiEinspielen(DbVorgang v, string tabelle, string datei, Dictionary<string, string> typen,
                                           List<string> staende)
        {
            List<List<string>> zeilen = TwwPaketteilCtrl.CsvLesen(File.ReadAllText(datei, Encoding.UTF8));
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

                var felder = new object[kopf.Count];
                for (int c = 0; c < kopf.Count; c++)
                {
                    object w = TwwPaketteilCtrl.Feldwert(z[c], typen[kopf[c]], ort + ", Spalte " + kopf[c]);
                    if (kopf[c] == "Status" && !string.Equals(Convert.ToString(w), TwwSchema.STATUS_AUSLIEFERUNG, StringComparison.Ordinal))
                        throw new InvalidDataException(ort + ": Status \"" + Convert.ToString(w) + "\" — ein Katalogpaket " +
                                                       "fuehrt nur Status AUSLIEFERUNG.");
                    if (kopf[c] == "ReadOnly" && !(w is long ro && ro == 1))
                        throw new InvalidDataException(ort + ": ReadOnly muss 1 sein (Auslieferung).");
                    felder[c] = w;
                }
                if (tabelle == TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)
                {
                    object Feld(string spalte) { int i = kopf.IndexOf(spalte); return i < 0 ? null : felder[i]; }
                    PaketteilNachfuehrung.Eintrag stand = PaketteilNachfuehrung.Finden(
                        Convert.ToString(Feld("Bezeichner"), CultureInfo.InvariantCulture),
                        Feld("Bezugsart") is long bz ? bz : (long?)null,
                        Convert.ToString(Feld("Status"), CultureInfo.InvariantCulture),
                        Convert.ToString(Feld("Bedarf_Version"), CultureInfo.InvariantCulture),
                        Convert.ToString(Feld("Bedarf_Herkunftsart"), CultureInfo.InvariantCulture));
                    if (stand != null)
                    {
                        felder[kopf.IndexOf("Bezeichner")] = stand.Bezeichner;
                        felder[kopf.IndexOf("Bezugsart")] = (long)stand.Bezugsart;
                        staende.Add(ort + ": \"" + stand.FruehererBezeichner + "\" (Bezugsart " + stand.FruehereBezugsart +
                                    ") gelesen als \"" + stand.Bezeichner + "\" (Bezugsart " + stand.Bezugsart + ")");
                    }
                }
                var werte = new List<DbParam>();
                foreach (object w in felder) werte.Add(new DbParam("?", w));
                if (mitReadOnly && !kopf.Contains("ReadOnly")) werte.Add(new DbParam("?", 1L));

                v.Ausfuehren(sql, werte.ToArray());
                n++;
            }
            return n;
        }

        // =================================================================================
        //  Die Pruefposten
        // =================================================================================

        /// <summary>
        /// Die Tww-Posten der Abnahme (Konzept 3.2, 6 (b), (c)): keine Zeile mit Status
        /// <c>IMPORT</c>, nur <c>AUSLIEFERUNG</c>, keine verwaiste Kindzeile, keine Herkunftsart
        /// <c>FIKTIV</c>, keine Normdaten (Herkunftsart <c>IMPORT</c>, <c>Tab_TwwTyptag_IMPORT</c>),
        /// jede Auslieferungszeile <c>ReadOnly = 1</c>, keine Eingabe aus den lokalen Normdaten
        /// (ZU11), kein Beispielprojekt auf dem Typtagweg bei leerer <c>Tab_TwwTyptag_IMPORT</c>
        /// (N16, Restlücke). <paramref name="mitnahmen"/> nennt zu einer IMPORT-Zeile das
        /// Beispielpaket, das sie mitgebracht hat.
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
            foreach (var k in KINDER.Concat(ABHAENGIGE)
                                    .Where(k => DataRepository.TabelleVorhanden(k.Kind) && DataRepository.TabelleVorhanden(k.Kopf)))
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
            ok &= Posten(waisen, "keine verwaiste Zeile in " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + ", " +
                                 TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + " und " +
                                 TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + " (Kopf fehlt oder traegt IMPORT)");

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

            // (5b) Messdaten (K5): keine Messreihe in der Vorlage - auch nicht die eines
            // Beispielprojekts. Eigener Posten, weil eine Messreihe kein Normimport ist,
            // sondern Objektdaten des Anwenders.
            var messdaten = new List<string>();
            if (DataRepository.TabelleVorhanden(TAB_MESSREIHE))
            {
                long n = Zahl("SELECT COUNT(*) FROM \"" + TAB_MESSREIHE + "\"", null);
                if (n > 0) messdaten.Add(TAB_MESSREIHE + ": " + n);
            }
            ok &= Posten(messdaten, "keine gemessene Reihe (" + TAB_MESSREIHE + ", K5: Messdaten " +
                                    "gehoeren dem Objekt)");

            // (5c) Die Zeilen des Bedarfstag-Konstruktors (Schritt T5, ZU25): Ein konstruierter
            // Bedarfstag ist EIGEN und faellt mit dem Katalog; seine Eingabezeilen gehoeren
            // deshalb ebenso nicht in die Vorlage.
            var konstruktorzeilen = new List<string>();
            if (DataRepository.TabelleVorhanden(TAB_KONSTRUKTORZEILE))
            {
                long n = Zahl("SELECT COUNT(*) FROM \"" + TAB_KONSTRUKTORZEILE + "\"", null);
                if (n > 0) konstruktorzeilen.Add(TAB_KONSTRUKTORZEILE + ": " + n);
            }
            ok &= Posten(konstruktorzeilen, "keine Zeile des Bedarfstag-Konstruktors (" +
                                            TAB_KONSTRUKTORZEILE + ", ZU25: der konstruierte Tag ist EIGEN)");

            // (7) Der Typtagweg eines Beispielprojekts ohne Typtage (N16, Restluecke): Die Vorlage
            // leert TAB_TYPTAG_IMPORT, ein mitgenommenes Beispielprojekt behaelt aber seine
            // Projektspalten. Traegt es Typtage_Aktiv = 1, liefe es im ausgelieferten Stand benannt
            // auf Ablehnung (ZapfEingabefehler.TyptageUngueltig, Zapfprofileingang) - kein stiller
            // Rueckfall auf den Formvektor, aber ein Projekt, das nicht rechnet.
            ok &= Posten(Typtagweg(), "kein Beispielprojekt mit " + TwwSchema.SPALTE_TYPTAGE_AKTIV +
                                      " = 1 bei leerer " + TAB_TYPTAG_IMPORT + " (das Projekt liefe " +
                                      "beim Anwender auf eine Ablehnung)");

            _bericht.Zeile("        Tww-Auslieferungszeilen (Status AUSLIEFERUNG): " +
                           auslieferung.ToString(CultureInfo.InvariantCulture));

            // Die Zeilen des freien Paketteils (Herkunftsart FREI, VERFAHREN oder EIGENKONSTRUKTION) — nachrichtlich je
            // Tabelle; die Ereignisse zaehlen an ihrem freien Bedarfstag, der Tagesgangsatz (ohne
            // eigene Herkunftsspalte) an seinen Tagesgaengen.
            var frei = new List<string>();
            foreach (string t in Vorhandene(PAKETTEIL_TABELLEN))
            {
                long n;
                if (t == TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM)
                    n = Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"ID_Bedarfstag\" IN (SELECT \"ID\" FROM \"" +
                             TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"Herkunftsart\" " + PaketteilHerkunftIn() + ")",
                             PAKETTEIL_HERKUNFT);
                else if (t == TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM)
                    n = !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM) ? 0
                        : Zahl("SELECT COUNT(DISTINCT \"ID_Tagesgangsatz\") FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                               "\" WHERE \"Herkunftsart\" " + PaketteilHerkunftIn(), PAKETTEIL_HERKUNFT);
                else
                {
                    List<string> h = Herkunftsspalten(t);
                    n = h.Count == 0 ? 0
                        : Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + h[0] + "\" " + PaketteilHerkunftIn(),
                               PAKETTEIL_HERKUNFT);
                }
                frei.Add(t + " " + n.ToString(CultureInfo.InvariantCulture));
            }
            _bericht.Zeile("        Tww-Zeilen mit Herkunftsart " + string.Join("/", PAKETTEIL_HERKUNFT) +
                           " (freier Paketteil): " + string.Join(", ", frei));
            return ok;
        }

        /// <summary>
        /// Die Beispielprojekte, die den Typtagweg tragen, waehrend die Tabelle der eingespielten
        /// Typtage leer ist (N16, Restluecke) - je Zeile die Projektkennung samt Klimazone und
        /// Gebaeudeart, damit der Bericht sagt, welche Wahl liegen geblieben ist. Fehlt die Tabelle
        /// oder die Spalte, ist nichts zu melden; steht eine Typtagzeile, ist der Weg gedeckt.
        /// </summary>
        private List<string> Typtagweg()
        {
            var befunde = new List<string>();
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PROJEKT)
                || !DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_PROJEKT, TwwSchema.SPALTE_TYPTAGE_AKTIV))
                return befunde;
            if (DataRepository.TabelleVorhanden(TAB_TYPTAG_IMPORT)
                && Zahl("SELECT COUNT(*) FROM \"" + TAB_TYPTAG_IMPORT + "\"", null) > 0)
                return befunde;

            bool zone = DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_PROJEKT, TwwSchema.SPALTE_TYPTAGE_KLIMAZONE);
            bool art = DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_PROJEKT, TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART);
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID_Projekt\"" +
                (zone ? ", \"" + TwwSchema.SPALTE_TYPTAGE_KLIMAZONE + "\"" : "") +
                (art ? ", \"" + TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART + "\"" : "") +
                " FROM \"" + TwwSchema.TAB_TWW_PROJEKT + "\" WHERE \"" + TwwSchema.SPALTE_TYPTAGE_AKTIV + "\" = 1" +
                " ORDER BY \"ID_Projekt\"");
            foreach (DataRow r in t.Rows)
            {
                string zeile = "ID_Projekt " + Convert.ToString(r["ID_Projekt"], CultureInfo.InvariantCulture);
                if (zone && r[TwwSchema.SPALTE_TYPTAGE_KLIMAZONE] != DBNull.Value)
                    zeile += ", Klimazone " + Convert.ToString(r[TwwSchema.SPALTE_TYPTAGE_KLIMAZONE], CultureInfo.InvariantCulture);
                if (art && r[TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART] != DBNull.Value
                    && Convert.ToString(r[TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART]).Length > 0)
                    zeile += ", Gebaeudeart " + Convert.ToString(r[TwwSchema.SPALTE_TYPTAGE_GEBAEUDEART]);
                befunde.Add(zeile);
            }
            return befunde;
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

        private static long Zahl(string sql, params string[] werte)
        {
            object o = werte == null || werte.Length == 0 || werte[0] == null
                ? DataRepository.ExecuteScalar(sql)
                : DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());
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
