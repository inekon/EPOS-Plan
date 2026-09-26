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

        // =================================================================================
        //  Der freie Paketteil (Referenzlaeufe/Katalogpaket_frei)
        // =================================================================================

        /// <summary>Der freie Paketteil, relativ zur Repowurzel.</summary>
        internal const string PAKETTEIL_FREI = "Referenzlaeufe/Katalogpaket_frei";

        /// <summary>
        /// Die Katalogversion der Paketteil-Zeilen, wenn der Katalog selbst keine führt (kein
        /// Parameter nach Katalogpaket und Tww-Regel).
        /// </summary>
        internal const string KATALOGVERSION_FREI = "FREI-1";

        /// <summary>Die Tabellen des Paketteils in Einspielreihenfolge (Verwiesene zuerst).</summary>
        internal static readonly string[] PAKETTEIL_TABELLEN =
        {
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM
        };

        /// <summary>
        /// Die Herkunftsarten, die eine Zeile des freien Paketteils tragen darf: <c>FREI</c> für eine
        /// frei verfügbare Quelle, <c>VERFAHREN</c> für einen aus einem Verfahren gerechneten Wert
        /// (die aus VDI 6002 abgeleiteten Nutzungsarten, ZU19/ZU20 — ihre Zahl steht in keiner
        /// Richtlinie, und ihre Quelle ist keine freie) und <c>EIGENKONSTRUKTION</c> für eine Setzung
        /// von INEKON aus einer eigenen Unterlage (die Setzungen der Speicherauslegung aus der Vorlage
        /// TWW-Auslegung V4, N28 — keine frei verfügbare Quelle, kein Verfahren).
        /// </summary>
        internal static readonly string[] PAKETTEIL_HERKUNFT =
        {
            TwwSchema.HERKUNFT_FREI, TwwSchema.HERKUNFT_VERFAHREN, TwwSchema.HERKUNFT_EIGENKONSTRUKTION
        };

        /// <summary>
        /// Jeder Parameter des Paketteils ist ein Schlüssel, den das Programm liest
        /// (<see cref="TwwParameterkatalog"/>, ZU31), in seiner Einheit und in seinem Bereich — sonst
        /// lehnte ihn der Katalogimport ab, und die Auslieferung trüge eine stille oder verrutschte
        /// Zeile. Ein Verstoß nennt Datei, Zeile und Grund; Rückgabe: die Zahl der geprüften Zeilen.
        /// </summary>
        internal static int ParameterDesPaketteilsPruefen(List<Dictionary<string, object>> zeilen, List<string> orte)
        {
            for (int i = 0; i < zeilen.Count; i++)
            {
                string s = Convert.ToString(zeilen[i]["Schluessel"], CultureInfo.InvariantCulture);
                TwwParameterschluessel k = TwwParameterkatalog.Finden(s);
                if (k == null)
                    throw new InvalidDataException(orte[i] + ": den Parameter \"" + s + "\" liest das Programm nicht.");
                string einheit = zeilen[i].TryGetValue("Einheit", out object e) ? Convert.ToString(e, CultureInfo.InvariantCulture) : null;
                if (!k.EinheitPasst(einheit))
                    throw new InvalidDataException(orte[i] + ": \"" + s + "\" in der Einheit \"" + einheit + "\" statt \"" + k.Einheit + "\".");
                double w = Convert.ToDouble(zeilen[i]["Wert"], CultureInfo.InvariantCulture);
                if (!k.ImBereich(w))
                    throw new InvalidDataException(orte[i] + ": \"" + s + "\" = " + w.ToString("R", CultureInfo.InvariantCulture) +
                                                   " liegt ausserhalb " + TwwParameterkatalog.Bereichstext(k) + ".");
            }
            return zeilen.Count;
        }

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
        /// <para><b>Format</b> wie das Katalogpaket (N2), mit drei Regeln des Paketteils: Jede Zeile
        /// trägt Status <c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1 (oder keine Spalte) und in jeder
        /// Provenienzgruppe eine Herkunftsart aus <see cref="PAKETTEIL_HERKUNFT"/> — <c>FREI</c> für
        /// eine frei verfügbare Quelle, <c>VERFAHREN</c> für einen gerechneten Wert,
        /// <c>EIGENKONSTRUKTION</c> für eine Setzung von INEKON. Der Paketteil führt <b>keine Katalogversion</b>: Seine Zeilen treten der
        /// Katalogversion des Katalogs bei (die des zuletzt angelegten Parameters, sonst
        /// <see cref="KATALOGVERSION_FREI"/>) — sonst sähe der Parametersatz der Auslieferung die
        /// Parameter der Stochastik nicht. Die <c>ID</c> eines Bedarfstags ist nur Schlüssel des
        /// Pakets (die Ereignisse verweisen über <c>ID_Bedarfstag</c> darauf); die Datenbank vergibt
        /// die echte; ebenso die <c>ID</c> eines Tagesgangsatzes, auf die Tagesgänge und Nutzungsarten
        /// über <c>ID_Tagesgangsatz</c> verweisen. Die <b>Zapfkategorien</b> führen keine <c>ID_Nutzungsart</c>: Sie sind
        /// Vorgabesätze, von denen jede Nutzungsart mit Status <c>AUSLIEFERUNG</c> ohne eigene
        /// Kategorien <b>den Satz ihrer Gruppe</b> bekommt (Stufe Z5). Die Gruppe steht in der
        /// Steuerspalte <see cref="TwwSchema.STEUERSPALTE_GRUPPE"/> — der einzigen Spalte des
        /// Paketteils, die keine Spalte der Tabelle ist; sie wird nicht geschrieben. Welche Gruppe
        /// eine Nutzungsart trägt, sagt <see cref="TwwSchema.Kategoriengruppe"/> (Kalenderart
        /// „Wohnen" oder Nichtwohnen). Ein Paketteil ohne die Spalte bindet seinen einen Satz wie
        /// bisher an jede Nutzungsart; fehlt der Satz einer Gruppe, bleiben ihre Nutzungsarten ohne
        /// Kategorien — der Bericht meldet es, und sie rechnen nicht stochastisch.</para>
        ///
        /// <para><b>Schlüsselgleichheit.</b> Führt das Katalogpaket dieselbe Zeile (Parameter:
        /// Schlüssel und Katalogversion; Bedarfstag, Tagesgangsatz und Nutzungsart: Bezeichner und
        /// Katalogversion), gilt seine, und
        /// der Bericht meldet es. Ohne Katalogpaket ersetzt der Paketteil eine gleiche Zeile der
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
            // Schemaauskunft VOR der Transaktion (eigene Verbindung, siehe Bereinigen).
            var vorhanden = new HashSet<string>(Vorhandene(PAKETTEIL_TABELLEN), StringComparer.Ordinal);
            if (vorhanden.Count == 0)
            {
                _bericht.Zeile("keine Tww-Tabelle im Schema — nichts einzuspielen");
                return true;
            }
            bool mitNutzungsarten = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM);

            var zeilen = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.Ordinal);
            var orte = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            try
            {
                var bekannt = new HashSet<string>(PAKETTEIL_TABELLEN.Select(t => t + ".csv"), StringComparer.OrdinalIgnoreCase);
                foreach (string d in Directory.GetFiles(ordner, "*.csv").OrderBy(x => x, StringComparer.Ordinal))
                    if (!bekannt.Contains(Path.GetFileName(d)))
                        throw new InvalidDataException(Path.GetFileName(d) + " gehoert nicht zum Paketteil (erlaubt: " +
                                                       string.Join(", ", PAKETTEIL_TABELLEN) + ").");
                foreach (string t in PAKETTEIL_TABELLEN)
                {
                    string d = Path.Combine(ordner, t + ".csv");
                    if (!File.Exists(d)) throw new InvalidDataException(t + ".csv fehlt im Paketteil.");
                    if (!vorhanden.Contains(t))
                    {
                        // Etwa die Zapfkategorien in einer Quelle vor Schritt 115: benannt uebergangen.
                        _bericht.Zeile("uebergangen: " + t + ".csv — die Tabelle fehlt im Schema der Quelle");
                        zeilen[t] = new List<Dictionary<string, object>>();
                        orte[t] = new List<string>();
                        continue;
                    }
                    (zeilen[t], orte[t]) = PaketteilLesen(t, d, Spaltentypen(t));
                }
                int bekannteParameter = ParameterDesPaketteilsPruefen(zeilen[TwwSchema.TAB_TWW_PARAMETER_STAMM],
                                                                      orte[TwwSchema.TAB_TWW_PARAMETER_STAMM]);
                _bericht.Zeile("ok      jeder Parameter des Paketteils ist ein Schluessel des Programms, in Einheit und Bereich (" +
                               bekannteParameter.ToString(CultureInfo.InvariantCulture) + ")");
            }
            catch (InvalidDataException ex)
            {
                fehler = "Freier Paketteil " + ordner + ": " + ex.Message;
                _bericht.Zeile("FEHLER  " + fehler);
                return false;
            }

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    object kv = v.Skalar("SELECT Katalogversion FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " ORDER BY ID DESC LIMIT 1");
                    string version = kv == null || kv == DBNull.Value ? KATALOGVERSION_FREI : Convert.ToString(kv, CultureInfo.InvariantCulture);
                    _bericht.Zeile("Katalogversion der Paketteil-Zeilen: " + version +
                                   (kv == null || kv == DBNull.Value ? " (der Katalog fuehrt keine eigene)" : " (die des Katalogs)"));
                    var meldungen = new List<string>();

                    // --- Tagesgangsätze, Tagesgänge und Nutzungsarten (ZU20) --------------------
                    // Die ID des Satzes ist Schlüssel des Pakets: Tagesgänge und Nutzungsarten
                    // verweisen darauf, die Datenbank vergibt die echte. Tritt ein Satz zurück
                    // (das Katalogpaket führt ihn), treten seine Tagesgänge und die Nutzungsarten,
                    // die auf ihn zeigen, mit ihm zurück — benannt, nie still.
                    var satzIds = new Dictionary<long, long?>();
                    int nSaetze = 0, nGaenge = 0, nArten = 0;
                    List<Dictionary<string, object>> saetzeZ = zeilen[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM];
                    for (int i = 0; i < saetzeZ.Count; i++)
                    {
                        if (!(saetzeZ[i].TryGetValue("ID", out object roh) && roh is long schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM][i] + ": die Spalte ID " +
                                                           "(Schluessel des Pakets fuer Tagesgaenge und Nutzungsarten) fehlt.");
                        if (satzIds.ContainsKey(schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM][i] + ": ID " + schluesselId + " doppelt.");
                        string satzname = Convert.ToString(saetzeZ[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "Bezeichner", satzname, version, mitKatalogpaket, meldungen))
                        {
                            satzIds[schluesselId] = null;
                            continue;
                        }
                        satzIds[schluesselId] = Einfuegen(v, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, saetzeZ[i], version);
                        nSaetze++;
                    }
                    List<Dictionary<string, object>> gaengeZ = zeilen[TwwSchema.TAB_TWW_TAGESGANG_STAMM];
                    for (int i = 0; i < gaengeZ.Count; i++)
                    {
                        if (!(gaengeZ[i].TryGetValue("ID_Tagesgangsatz", out object roh) && roh is long kopf) ||
                            !satzIds.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_TAGESGANG_STAMM][i] +
                                                           ": ID_Tagesgangsatz verweist auf keinen Tagesgangsatz des Paketteils (Waise).");
                        if (neu == null) continue;                       // der Satz tritt zurueck, seine Gaenge mit ihm
                        Einfuegen(v, TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                                  new Dictionary<string, object>(gaengeZ[i], StringComparer.Ordinal) { ["ID_Tagesgangsatz"] = neu.Value }, null);
                        nGaenge++;
                    }
                    List<Dictionary<string, object>> artenZ = zeilen[TwwSchema.TAB_TWW_NUTZUNGSART_STAMM];
                    for (int i = 0; i < artenZ.Count; i++)
                    {
                        if (!(artenZ[i].TryGetValue("ID_Tagesgangsatz", out object roh) && roh is long kopf) ||
                            !satzIds.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_NUTZUNGSART_STAMM][i] +
                                                           ": ID_Tagesgangsatz verweist auf keinen Tagesgangsatz des Paketteils (Waise).");
                        string artname = Convert.ToString(artenZ[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (neu == null)
                        {
                            meldungen.Add(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " \"" + artname + "\": ihr Tagesgangsatz tritt " +
                                          "zurueck — die Nutzungsart des Paketteils mit ihm");
                            continue;
                        }
                        if (Gleich(v, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bezeichner", artname, version, mitKatalogpaket, meldungen))
                            continue;
                        Einfuegen(v, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
                                  new Dictionary<string, object>(artenZ[i], StringComparer.Ordinal) { ["ID_Tagesgangsatz"] = neu.Value }, version);
                        nArten++;
                    }

                    // --- Parameter ------------------------------------------------------------
                    int parameter = 0;
                    List<Dictionary<string, object>> p = zeilen[TwwSchema.TAB_TWW_PARAMETER_STAMM];
                    for (int i = 0; i < p.Count; i++)
                    {
                        string schluessel = Convert.ToString(p[i]["Schluessel"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_PARAMETER_STAMM, "Schluessel", schluessel, version, mitKatalogpaket, meldungen))
                            continue;
                        Einfuegen(v, TwwSchema.TAB_TWW_PARAMETER_STAMM, p[i], version);
                        parameter++;
                    }

                    // --- Bedarfstage samt Ereignissen -------------------------------------------
                    var ids = new Dictionary<long, long?>();
                    int tage = 0, ereignisse = 0;
                    List<Dictionary<string, object>> b = zeilen[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM];
                    for (int i = 0; i < b.Count; i++)
                    {
                        if (!(b[i].TryGetValue("ID", out object roh) && roh is long schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM][i] + ": die Spalte ID " +
                                                           "(Schluessel des Pakets fuer die Ereignisse) fehlt.");
                        if (ids.ContainsKey(schluesselId))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_STAMM][i] + ": ID " + schluesselId + " doppelt.");
                        string bezeichner = Convert.ToString(b[i]["Bezeichner"], CultureInfo.InvariantCulture);
                        if (Gleich(v, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Bezeichner", bezeichner, version, mitKatalogpaket, meldungen))
                        {
                            ids[schluesselId] = null;
                            continue;
                        }
                        ids[schluesselId] = Einfuegen(v, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, b[i], version);
                        tage++;
                    }
                    List<Dictionary<string, object>> e = zeilen[TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM];
                    for (int i = 0; i < e.Count; i++)
                    {
                        if (!(e[i].TryGetValue("ID_Bedarfstag", out object roh) && roh is long kopf) || !ids.TryGetValue(kopf, out long? neu))
                            throw new InvalidDataException(orte[TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM][i] +
                                                           ": ID_Bedarfstag verweist auf keinen Bedarfstag des Paketteils (Waise).");
                        if (neu == null) continue;                        // der Kopf tritt zurueck, seine Ereignisse mit ihm
                        var z = new Dictionary<string, object>(e[i], StringComparer.Ordinal) { ["ID_Bedarfstag"] = neu.Value };
                        Einfuegen(v, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, z, null);
                        ereignisse++;
                    }

                    // --- Zapfkategorien: der Vorgabesatz SEINER GRUPPE an jeder Nutzungsart ohne eigene --
                    List<Dictionary<string, object>> k = zeilen[TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM];
                    var arten = new List<(long Id, string Gruppe)>();
                    long eigene = 0;
                    if (mitNutzungsarten && k.Count > 0)
                    {
                        DataTable dt = v.Lese("SELECT ID, Kalenderart FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                              " WHERE Status = ? AND ID NOT IN " +
                                              "(SELECT ID_Nutzungsart FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ") ORDER BY ID",
                                              new DbParam("?", TwwSchema.STATUS_AUSLIEFERUNG));
                        foreach (DataRow r in dt.Rows)
                            arten.Add((Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture),
                                       TwwSchema.Kategoriengruppe(Convert.ToInt64(r["Kalenderart"], CultureInfo.InvariantCulture))));
                        eigene = Convert.ToInt64(v.Skalar("SELECT COUNT(DISTINCT ID_Nutzungsart) FROM " + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM),
                                                 CultureInfo.InvariantCulture);
                    }
                    var ohneSatz = new List<string>();
                    var jeGruppe = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach ((long art, string gruppe) in arten)
                    {
                        List<Dictionary<string, object>> satz = Vorgabesatz(k, gruppe);
                        if (satz.Count == 0)
                        {
                            if (!ohneSatz.Contains(gruppe)) ohneSatz.Add(gruppe);
                            continue;
                        }
                        jeGruppe[gruppe] = jeGruppe.TryGetValue(gruppe, out int n) ? n + 1 : 1;
                        foreach (Dictionary<string, object> z in satz)
                            Einfuegen(v, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM,
                                      new Dictionary<string, object>(z, StringComparer.Ordinal) { ["ID_Nutzungsart"] = art }, null);
                    }

                    v.Commit();

                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + ".csv  ->  " + nSaetze + " von " + saetzeZ.Count + " Zeile(n)");
                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + ".csv  ->  " + nGaenge + " von " + gaengeZ.Count + " Zeile(n)");
                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv  ->  " + nArten + " von " + artenZ.Count + " Zeile(n)");
                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_PARAMETER_STAMM + ".csv  ->  " + parameter + " von " + p.Count + " Zeile(n)");
                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ".csv  ->  " + tage + " von " + b.Count + " Zeile(n)");
                    _bericht.Zeile("eingespielt: " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + ".csv  ->  " + ereignisse + " von " + e.Count + " Zeile(n)");
                    string gruppen = string.Join(", ", jeGruppe.OrderBy(g => g.Key, StringComparer.Ordinal)
                                                               .Select(g => g.Value + " x " + g.Key +
                                                                            " (" + Vorgabesatz(k, g.Key).Count + " Kategorien)"));
                    _bericht.Zeile("Zapfkategorien (Vorgabesaetze, " + k.Count + " Zeile(n) in " +
                                   Gruppen(k).Count + " Gruppe(n)): an " + jeGruppe.Sum(g => g.Value) +
                                   " Nutzungsart(en) mit Status AUSLIEFERUNG ohne eigene Kategorien gebunden" +
                                   (gruppen.Length > 0 ? " — " + gruppen : "") +
                                   (eigene > 0 ? "; " + eigene + " Nutzungsart(en) fuehren eigene Kategorien des Katalogs" : "") +
                                   (arten.Count == 0 ? " — der Katalog fuehrt keine solche Nutzungsart" : ""));
                    foreach (string g in ohneSatz)
                        _bericht.Zeile("MELDUNG Zapfkategorien: der Paketteil fuehrt keinen Vorgabesatz der Gruppe \"" + g +
                                       "\" — die Nutzungsarten dieser Gruppe bleiben ohne Kategorien und rechnen nicht stochastisch");
                    foreach (string m in meldungen) _bericht.Zeile("MELDUNG " + m);
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    fehler = "Freier Paketteil " + ordner + ": " + ex.Message;
                    _bericht.Zeile("FEHLER  " + fehler);
                    return false;
                }
            }
        }

        /// <summary>
        /// Liest eine Datei des Paketteils: Spalten der Tabelle, getypt; die Regeln des Paketteils
        /// (keine Katalogversion, Kategorien ohne Nutzungsart, FREI/AUSLIEFERUNG/ReadOnly 1) geprüft.
        /// Rückgabe: die Zeilen und je Zeile ihr Ort (Datei, Zeile) für Fehlermeldungen.
        /// </summary>
        private static (List<Dictionary<string, object>>, List<string>) PaketteilLesen(string tabelle, string datei,
                                                                                      Dictionary<string, string> typen)
        {
            string name = Path.GetFileName(datei);
            List<List<string>> roh = CsvLesen(File.ReadAllText(datei, Encoding.UTF8));
            if (roh.Count < 2) throw new InvalidDataException(name + ": keine Datenzeile.");
            List<string> kopf = roh[0].Select(s => s.Trim()).ToList();
            bool mitGruppe = tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM;
            foreach (string s in kopf)
                if (!typen.ContainsKey(s) && !(mitGruppe && s == TwwSchema.STEUERSPALTE_GRUPPE))
                    throw new InvalidDataException(name + ": die Spalte \"" + s + "\" gibt es in " + tabelle + " nicht.");
            if (kopf.Contains("Katalogversion"))
                throw new InvalidDataException(name + ": der Paketteil fuehrt keine Katalogversion — seine Zeilen treten der " +
                                               "Katalogversion des Katalogs bei.");
            if (tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM && kopf.Contains("ID_Nutzungsart"))
                throw new InvalidDataException(name + ": die Kategorien des Paketteils sind ein Vorgabesatz ohne ID_Nutzungsart.");
            bool kopfzeile = typen.ContainsKey("Status");
            // Die Herkunftsspalten der Tabelle: "Herkunftsart" oder — bei den Nutzungsarten — je
            // Provenienzgruppe eine "<Gruppe>_Herkunftsart". Der Tagesgangsatz führt keine (seine
            // Herkunft steht an seinen Tagesgängen), die Ereignisse weder sie noch Status.
            List<string> herkunft = typen.Keys.Where(s => s == "Herkunftsart" ||
                                                          s.EndsWith("_Herkunftsart", StringComparison.Ordinal))
                                              .OrderBy(s => s, StringComparer.Ordinal).ToList();
            if (kopfzeile && !kopf.Contains("Status"))
                throw new InvalidDataException(name + ": Status gehoert in jede Zeile des Paketteils.");
            foreach (string h in herkunft)
                if (!kopf.Contains(h))
                    throw new InvalidDataException(name + ": " + h + " gehoert in jede Zeile des Paketteils.");

            var zeilen = new List<Dictionary<string, object>>();
            var orte = new List<string>();
            for (int i = 1; i < roh.Count; i++)
            {
                List<string> z = roh[i];
                if (z.Count == 1 && string.IsNullOrWhiteSpace(z[0])) continue;
                string ort = name + " Zeile " + (i + 1).ToString(CultureInfo.InvariantCulture);
                if (z.Count != kopf.Count)
                    throw new InvalidDataException(ort + ": " + z.Count + " Felder, die Kopfzeile nennt " + kopf.Count + ".");
                var w = new Dictionary<string, object>(StringComparer.Ordinal);
                for (int c = 0; c < kopf.Count; c++)
                    w[kopf[c]] = kopf[c] == TwwSchema.STEUERSPALTE_GRUPPE && !typen.ContainsKey(kopf[c])
                        ? Gruppe(z[c], ort)
                        : Wert(z[c], typen[kopf[c]], ort + ", Spalte " + kopf[c]);
                foreach (string h in herkunft)
                    if (!PAKETTEIL_HERKUNFT.Contains(Convert.ToString(w[h]), StringComparer.Ordinal))
                        throw new InvalidDataException(ort + ": " + h + " \"" + Convert.ToString(w[h]) + "\" — der Paketteil " +
                                                       "fuehrt nur " + string.Join(", ", PAKETTEIL_HERKUNFT) + ".");
                if (kopfzeile)
                {
                    if (!string.Equals(Convert.ToString(w["Status"]), TwwSchema.STATUS_AUSLIEFERUNG, StringComparison.Ordinal))
                        throw new InvalidDataException(ort + ": Status \"" + Convert.ToString(w["Status"]) + "\" — der Paketteil fuehrt nur AUSLIEFERUNG.");
                    if (w.TryGetValue("ReadOnly", out object ro) && !(ro is long l && l == 1))
                        throw new InvalidDataException(ort + ": ReadOnly muss 1 sein (Auslieferung).");
                    if (typen.ContainsKey("ReadOnly")) w["ReadOnly"] = 1L;
                }
                zeilen.Add(w);
                orte.Add(ort);
            }
            return (zeilen, orte);
        }

        /// <summary>
        /// Führt der Katalog die Zeile mit diesem natürlichen Schlüssel schon (<paramref name="spalte"/>
        /// und Katalogversion)? Mit Katalogpaket gilt dessen Zeile (<c>true</c>, gemeldet); ohne ersetzt
        /// der Paketteil die Zeile der Quelle (sie fällt, <c>false</c>, gemeldet).
        /// </summary>
        private static bool Gleich(DbVorgang v, string tabelle, string spalte, string wert, string version, bool mitKatalogpaket,
                                   List<string> meldungen)
        {
            object id = v.Skalar("SELECT ID FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = ? AND Katalogversion = ?",
                                 new DbParam("?", wert), new DbParam("?", version));
            if (id == null || id == DBNull.Value) return false;
            if (mitKatalogpaket)
            {
                meldungen.Add(tabelle + " \"" + wert + "\" (" + version + "): das Katalogpaket fuehrt dieselbe Zeile — " +
                              "die Zeile des Paketteils tritt zurueck");
                return true;
            }
            v.Ausfuehren("DELETE FROM \"" + tabelle + "\" WHERE ID = ?", new DbParam("?", id));
            meldungen.Add(tabelle + " \"" + wert + "\" (" + version + "): die Zeile der Quelle ist durch die des Paketteils ersetzt");
            return false;
        }

        /// <summary>
        /// <b>Der Vorgabesatz einer Gruppe</b> aus den Kategoriezeilen des Paketteils: die Zeilen mit
        /// dieser Gruppe in ihrer Reihenfolge; führt der Paketteil keine (etwa ein älteres Paket ohne
        /// Steuerspalte), gelten die Zeilen ohne Gruppe für jede Nutzungsart.
        /// </summary>
        private static List<Dictionary<string, object>> Vorgabesatz(List<Dictionary<string, object>> zeilen, string gruppe)
        {
            var satz = zeilen.Where(z => Gruppe(z) == gruppe).ToList();
            return satz.Count > 0 ? satz : zeilen.Where(z => Gruppe(z) == null).ToList();
        }

        /// <summary>Die Gruppen, die die Kategoriezeilen des Paketteils führen (<c>null</c> = ohne Gruppe).</summary>
        private static List<string> Gruppen(List<Dictionary<string, object>> zeilen)
            => zeilen.Select(Gruppe).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>Die Gruppe einer gelesenen Kategoriezeile (<c>null</c>, wenn sie keine trägt).</summary>
        private static string Gruppe(Dictionary<string, object> zeile)
            => zeile.TryGetValue(TwwSchema.STEUERSPALTE_GRUPPE, out object g) && g != null
                ? Convert.ToString(g, CultureInfo.InvariantCulture)
                : null;

        /// <summary>
        /// Die Gruppe einer Paketzeile der Zapfkategorien (Steuerspalte <c>Gruppe</c>): „Wohnen",
        /// „Nichtwohnen" oder leer (dann bindet der Satz an jede Nutzungsart — Rückfall eines
        /// Paketteils ohne Gruppen). Jeder andere Text ist ein Fehler.
        /// </summary>
        private static object Gruppe(string roh, string ort)
        {
            string g = (roh ?? "").Trim();
            if (g.Length == 0) return null;
            if (g != TwwSchema.KATEGORIENGRUPPE_WOHNEN && g != TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN)
                throw new InvalidDataException(ort + ": Gruppe \"" + g + "\" — erlaubt sind \"" +
                                               TwwSchema.KATEGORIENGRUPPE_WOHNEN + "\", \"" +
                                               TwwSchema.KATEGORIENGRUPPE_NICHTWOHNEN + "\" und leer.");
            return g;
        }

        private static long Einfuegen(DbVorgang v, string tabelle, Dictionary<string, object> zeile, string version)
        {
            var spalten = zeile.Keys.Where(s => s != "ID" && s != TwwSchema.STEUERSPALTE_GRUPPE).ToList();
            var werte = spalten.Select(s => new DbParam("?", zeile[s])).ToList();
            if (version != null)
            {
                spalten.Add("Katalogversion");
                werte.Add(new DbParam("?", version));
            }
            return v.EinfuegenUndId("INSERT INTO \"" + tabelle + "\" (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                    ") VALUES (" + string.Join(", ", spalten.Select(_ => "?")) + ")", werte.ToArray());
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
