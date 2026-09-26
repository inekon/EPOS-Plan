using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7 — <b>eine</b> Definition der Kennzahlentabelle für alle drei Ausgaben.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Bis E7 stand dieselbe Zeilenliste dreimal
    /// im Code: im Word-Baustein, im Excel-Generator und im Ergebnisreiter. Aus vierzehn
    /// Zeilen waren über die Etappen E2 bis E5 zweiundzwanzig geworden, jede davon
    /// dreimal geschrieben. Die Zahlen liefen dabei nicht auseinander — das Drumherum
    /// aber schon: unterschiedliche Stammspalten-Platzhalter, unterschiedliche
    /// Sichtbarkeitsprüfungen, gemischte Textschichten. Jede weitere Zeile hätte den
    /// Fehler dreimal wiederholt.</para>
    ///
    /// <para><b>Was hier steht und was nicht.</b> Hier steht, WELCHE Zeilen es gibt, wie
    /// sie heißen, woher ihr Wert kommt, wie er formatiert wird und wann die Zeile
    /// überhaupt erscheint. Nicht hier steht, WIE gerendert wird — Word baut Tabellen,
    /// Excel schreibt Zellen mit Zahlformat, der Reiter füllt ein Grid. Diese drei
    /// bleiben getrennt.</para>
    ///
    /// <para><b>Drei-Schichten-Regel.</b> <see cref="WirtZeile.Schluessel"/> ist
    /// sprachneutral und ASCII, <see cref="WirtZeile.Titel"/> kommt ausschließlich aus
    /// <c>MyResource.Resource.WIRT_ZEILE_*</c>. Die Titel dürfen deshalb <b>nicht</b>
    /// noch einmal durch <c>BerichtTexte.T()</c> laufen — sie sind bereits übersetzt.</para>
    /// </summary>
    public sealed class WirtZeile
    {
        /// <summary>Sprachneutraler Schlüssel der Zeile (ASCII, eingefroren).</summary>
        public string Schluessel = "";

        /// <summary>Anzeigetitel aus <c>MyResource</c> — bereits lokalisiert.</summary>
        public string Titel = "";

        /// <summary>.NET-Zahlformat für Word und Reiter („N0", „N1", „N3").</summary>
        public string Format = "N0";

        /// <summary>Zellformat für Excel („#,##0", „#,##0.0", „#,##0.000").</summary>
        public string ExcelFormat = "#,##0";

        /// <summary>Zahlenwert der Zeile; <c>null</c> = kein Wert (Anzeige „—", Excel leer).</summary>
        public Func<WirtschaftlichkeitErgebnis, double?> Wert;

        /// <summary>
        /// Textwert statt Zahl (z. B. die Herkunft der Steuersätze). Ist er gesetzt,
        /// bleibt <see cref="Wert"/> unbenutzt und die Zeile ist eine <b>Textzeile</b>.
        /// </summary>
        public Func<WirtschaftlichkeitErgebnis, string> Text;

        /// <summary>
        /// Was in der Stammspalte steht, wenn die Größe dort keine Bedeutung hat
        /// (Kapitalwert gegenüber Stamm, Annuität, Amortisation, interner Zinsfuß).
        /// <c>null</c> = die Zeile gilt auch für den Stamm.
        ///
        /// <para><b>Excel schreibt hier trotzdem nichts</b> — die Wertspalten müssen
        /// numerisch bleiben, sonst sind Filter und Diagramme hinüber. Das ist der eine
        /// bewusst verbliebene Unterschied zwischen den Ausgaben; er steht hier, statt
        /// dreimal zufällig zu entstehen.</para>
        /// </summary>
        public string StammAnzeige;

        /// <summary>
        /// KONZEPT § 2.9 — <b>welcher Stand die Referenz ist</b>: <c>Tab_Projekt.ID</c>
        /// der gewählten Referenz, <b>0 = der Stamm</b> (Vorgabe und Bestandsverhalten).
        ///
        /// <para>Sie steht an der ZEILE und nicht an der Ausgabe, weil sie darüber
        /// entscheidet, wo <see cref="StammAnzeige"/> greift — und das ist eine Aussage
        /// der Zeilendefinition, keine der Darstellung. Stünde sie dreimal in Word,
        /// Excel und Reiter, zeigte der eine „(Referenz)" in einer anderen Spalte als
        /// der andere.</para>
        ///
        /// <para>Die Sicht 2 (§ 2.15) setzt hier A; die Menge ist dann [A, B].</para>
        /// </summary>
        public int IdReferenz;

        /// <summary>
        /// Ist dieses Ergebnis die Referenz der Zeile? Ohne gewählte Referenz
        /// (<see cref="IdReferenz"/> = 0) ist es der Stamm — wortgleich zum Bestand.
        /// </summary>
        public bool IstReferenz(WirtschaftlichkeitErgebnis e)
        {
            if (e == null) return false;
            return IdReferenz > 0 ? e.IdProjekt == IdReferenz : e.IstStamm;
        }

        /// <summary>true, wenn die Zeile Text statt einer Zahl führt.</summary>
        public bool IstText { get { return Text != null; } }

        // =====================================================================
        // ETAPPE B7 — die Rubrik „Erlöse und Vorteile" (Konzept § 2.6)
        // =====================================================================

        /// <summary>
        /// Zugehörigkeit zur Erlösrubrik: <see cref="BLOCK_A"/> zahlungswirksam,
        /// <see cref="BLOCK_B"/> Ausweis, leer = gewöhnliche Kennzahl.
        ///
        /// <para><b>Warum das am Zeilenobjekt steht und nicht in der Ausgabe.</b> Die
        /// Teilung in zwei Blöcke ist keine Kosmetik, sondern eine Rechenaussage:
        /// Block B darf nicht addiert werden. Stünde sie in Word, Excel und Reiter je
        /// einmal, wäre sie dreimal geschrieben — und der erste Ausgabeweg, der sie
        /// vergisst, summiert eine Doppelzählung.</para>
        /// </summary>
        public string Block = "";

        /// <summary>Blocküberschrift ohne eigene Werte; sie entfällt mit ihrem Block.</summary>
        public bool IstUeberschrift;

        /// <summary>AUFTRAG U6 — Überschrift eines KOMPONENTENblocks (nicht des Blocks
        /// A oder B). Sie beendet den vorigen Komponentenblock, nicht den äußeren.</summary>
        public bool IstKomponentenkopf;

        /// <summary>Summenzeile des Blocks A — sie summiert nur, was in
        /// <see cref="Block"/> <c>A</c> steht.</summary>
        public bool IstSumme;

        /// <summary>
        /// AUFTRAG U6 — <b>der Anlagenbezug der Zeile</b>: eine sprachneutrale
        /// Komponentenkennung (<see cref="KOMPONENTE_BHKW"/>, <see cref="KOMPONENTE_PV"/>,
        /// <see cref="KOMPONENTE_KESSEL"/>) oder <see cref="KOMPONENTE_PROJEKTWEIT"/>
        /// (leer) für alles, was an keiner Anlage hängt.
        ///
        /// <para><b>Warum das an der Zeile steht.</b> A und B bleiben die ÄUSSERE
        /// Ordnung — sie entscheidet, was in die Summe geht. Die Komponente gliedert
        /// INNEN (Konzept § 2.6, Entscheid Q15): Der Planer fragt „was bringt das
        /// Blockheizkraftwerk?", und die Antwort steht in einem Block mit Zwischensumme.
        /// Die Zuordnung entsteht hier EINMAL; stünde sie in Seite, Word, Excel und
        /// Vorschau je einmal, lägen dieselben Beträge in vier verschiedenen Blöcken.</para>
        /// </summary>
        public string Komponente = KOMPONENTE_PROJEKTWEIT;

        /// <summary>
        /// AUFTRAG U6 — Zwischensumme EINES Komponentenblocks. Sie ist zugleich
        /// <see cref="IstSumme"/> (Word und Excel setzen daran ihre Auszeichnung), aber
        /// NICHT die Blocksumme: Über die entscheidet allein
        /// <c>Block == BLOCK_A &amp;&amp; !IstTeilsumme</c>.
        /// </summary>
        public bool IstTeilsumme;

        /// <summary>Alles ohne Anlagenbezug — die § 9b-Entlastung hängt am Restbezug,
        /// nicht an einer Anlage (Konzept § 2.6, Klarstellung 2).</summary>
        public const string KOMPONENTE_PROJEKTWEIT = "";

        /// <summary>Blockheizkraftwerk — KWK-Zuschlag, § 53/§ 53a, Eigenstrom.</summary>
        public const string KOMPONENTE_BHKW = "BHKW";

        /// <summary>Photovoltaik — Vergütung, anzulegender Wert, § 51a.</summary>
        public const string KOMPONENTE_PV = "PV";

        /// <summary>Kessel — § 54 EnergieStG entlastet den HEIZstoff, nicht den
        /// Brennstoff der Stromerzeugung.</summary>
        public const string KOMPONENTE_KESSEL = "KESSEL";

        /// <summary>Die Reihenfolge der Komponentenblöcke innerhalb eines Blocks —
        /// projektweit steht zuletzt (Mockup Kategorie 7).</summary>
        public static readonly string[] Komponentenfolge =
        {
            KOMPONENTE_BHKW, KOMPONENTE_PV, KOMPONENTE_KESSEL, KOMPONENTE_PROJEKTWEIT
        };

        /// <summary>Der Anzeigename einer Komponente — aus <c>MyResource</c>, nie aus
        /// dem Schlüssel abgeleitet (Drei-Schichten-Regel).</summary>
        public static string Komponentenname(string komponente)
        {
            if (string.Equals(komponente, KOMPONENTE_BHKW, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_ERL_K_BHKW;
            if (string.Equals(komponente, KOMPONENTE_PV, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_ERL_K_PV;
            if (string.Equals(komponente, KOMPONENTE_KESSEL, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_ERL_K_KESSEL;
            return MyResource.Resource.WIRT_ERL_PROJEKTWEIT;
        }

        /// <summary>Einzugstiefe: 0 = Hauptzeile, 1 = Unter-/Herleitungszeile.</summary>
        public int Einzug;

        /// <summary>
        /// ETAPPE B7 — die Zeile erscheint, sobald das Projekt eine Anlage führt, für
        /// die die Position gilt, <b>auch wenn der Betrag 0 ist</b>.
        ///
        /// <para><b>Der Anwenderbefund vom 17.09.2026, der das erzwang:</b> „Vergütungen
        /// und Reduktionen sind in den Ergebnissen nicht dargestellt." Sie waren
        /// gerechnet — nur blendete die Konvention „nie 0-Zeilen" jede Position aus,
        /// deren Satz ungepflegt war. Der Anwender sah damit nicht, dass es die Position
        /// gibt, und erst recht nicht, warum sie 0 ist. Seit B7 steht sie da, und
        /// <see cref="Grundtext"/> sagt, woran es liegt.</para>
        /// </summary>
        public bool ImmerZeigen;

        /// <summary>
        /// Klartext der fehlenden Grundlage einer Zelle OHNE Wert („kein
        /// KWK-Zuschlagssatz gepflegt"). <c>null</c> oder leer = ohne Zusatz.
        ///
        /// <para><b>ETAPPE E5 (Entscheid Q16):</b> Er greift, wenn die Zelle keinen Wert
        /// trägt — und ebenso, wenn sie 0 trägt und die Funktion einen Grund nennt: Dann
        /// ist die 0 keine gerechnete Null, sondern eine fehlende Grundlage (siehe
        /// <see cref="Grund"/>).</para>
        /// </summary>
        public Func<WirtschaftlichkeitErgebnis, string> Grundtext;

        // =====================================================================
        // ETAPPE E5 (V‑A) — die Einordnung nach DIN EN 17463 Anhang C
        // =====================================================================

        /// <summary>
        /// ETAPPE E5 (V‑A, Entscheid V‑3): Die Kennzahl ist <b>nachrichtlich</b> — der
        /// Kapitalwert ist das einzige Maß der Vorteilhaftigkeit (DIN EN 17463 Anhang C,
        /// Konzept § 2.11.1). Gesetzt an dynamischer Amortisation und internem Zinsfuß
        /// (<see cref="WirtschaftlichkeitZeilen.IstNachrichtlich"/>, Empfehlung Q3 vom
        /// 22.09.2026: die Annuität ist der Kapitalwert als gleichmäßiger Jahresbetrag und
        /// trägt kein Label); Seite, Kachel und Wortbericht zeigen das Label
        /// <c>WIRT_KZ_NACHRICHTLICH</c> daneben. Die Zahlen bleiben, wie sie sind.
        /// </summary>
        public bool Nachrichtlich;

        /// <summary>
        /// ETAPPE E5 (V‑A): eine WARNUNG zur Zelle, die ihren Wert nicht ändert — heute
        /// allein die Mehrdeutigkeit des internen Zinsfußes bei mehr als einem
        /// Vorzeichenwechsel (<see cref="ValeriAusweis.IzfWarnung"/>). <c>null</c> oder
        /// leer = keine Warnung. Sie steht NICHT in <see cref="Anzeige"/>: Die Zelle
        /// bleibt die Zahl; wie die Warnung daneben erscheint, entscheidet die Ausgabe.
        /// </summary>
        public Func<WirtschaftlichkeitErgebnis, string> Warntext;

        /// <summary>Die Warnung einer Zelle; <c>""</c> = keine (E5, V‑A).</summary>
        public string Warnung(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || Warntext == null) return "";
            if (IstReferenz(e) && StammAnzeige != null) return "";
            return Warntext(e) ?? "";
        }

        /// <summary>Kennung des zahlungswirksamen Blocks A (Konzept § 2.6).</summary>
        public const string BLOCK_A = "A";

        /// <summary>Kennung des Ausweisblocks B — <b>nicht addieren</b>.</summary>
        public const string BLOCK_B = "B";

        /// <summary>
        /// ETAPPE E5 (Entscheid Q16, Prüfpapier 19.09.2026): der <b>Grund</b> einer
        /// Ergebniszelle ohne Wert — <c>null</c>, wenn die Zelle eine Zahl trägt.
        ///
        /// <para>Ohne Wert ist eine Zelle, deren Größe fehlt, UND eine, die 0 trägt,
        /// während <see cref="Grundtext"/> einen Grund nennt: Diese 0 ist keine gerechnete
        /// Null, sondern die Folge einer fehlenden Grundlage (kein Satz gepflegt, keine
        /// Anlage, keine Bezugsspitze). Eine gerechnete Null ohne Grund bleibt eine 0.</para>
        ///
        /// <para>Die Referenzzelle einer Differenzkennzahl trägt ihren Platzhalter
        /// (<see cref="StammAnzeige"/>) und keinen Grund; eine Textzeile keinen.</para>
        /// </summary>
        /// <returns>Der Grund; <c>""</c> = ohne Wert, aber ohne benannten Grund
        /// (Anzeige „—"); <c>null</c> = die Zelle trägt einen Wert.</returns>
        public string Grund(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || IstText || Wert == null) return null;
            if (IstReferenz(e) && StammAnzeige != null) return null;
            double? v = Wert(e);
            string g = Grundtext == null ? null : Grundtext(e);
            if (!v.HasValue) return g ?? "";
            if (v.Value == 0 && !string.IsNullOrEmpty(g)) return g;
            return null;
        }

        /// <summary>
        /// Der formatierte Zellinhalt für Word und Reiter; <c>„—"</c>, wenn es keinen
        /// Wert gibt.
        ///
        /// <para><b>ETAPPE E5 (Q16):</b> Eine Zelle ohne Wert zeigt „— ‹Grund›" statt einer
        /// Null — dem Mockup folgend: „Eine Zelle ohne Betrag trägt einen Gedankenstrich
        /// und den Grund; eine 0 steht nur, wo null gerechnet wurde." Bis E5 stand hier
        /// „0 — ‹Grund›" (B7). Excel bleibt numerisch (<see cref="ExcelWert"/>).</para>
        /// </summary>
        public string Anzeige(WirtschaftlichkeitErgebnis e, System.Globalization.CultureInfo kultur)
        {
            if (e == null) return "—";
            if (IstText) { string t = Text(e); return string.IsNullOrEmpty(t) ? "—" : t; }
            if (IstReferenz(e) && StammAnzeige != null) return StammAnzeige;

            string grund = Grund(e);
            if (grund != null) return grund.Length == 0 ? "—" : "— " + grund;

            double? v = Wert == null ? null : Wert(e);
            return v.HasValue ? v.Value.ToString(Format, kultur) : "—";
        }

        /// <summary>
        /// Der Zahlenwert für Excel; <c>null</c> = Zelle bleibt leer.
        ///
        /// <para><b>ETAPPE E5 (Q16):</b> Eine Zelle ohne Wert bleibt LEER — keine 0 als
        /// Wert und kein Text in der Wertspalte; ihr Grund steht in Seite und Wortbericht
        /// (<see cref="Grund"/>). So bleiben Filter und Diagramme des Blattes numerisch
        /// und behaupten keine Null, die niemand gerechnet hat.</para>
        /// </summary>
        public double? ExcelWert(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || IstText || Wert == null) return null;
            if (IstReferenz(e) && StammAnzeige != null) return null;
            if (Grund(e) != null) return null;
            return Wert(e);
        }
    }

    /// <summary>
    /// Baut die Kennzahlenliste der Wirtschaftlichkeit — die eine Wahrheit für
    /// Word-Baustein, Excel-Blatt und Ergebnisreiter (Etappe E7).
    /// </summary>
    public static class WirtschaftlichkeitZeilen
    {
        /// <summary>
        /// Die sichtbaren Kennzahlzeilen einer Vergleichsgruppe.
        /// </summary>
        /// <param name="menge">
        /// Alle Ergebnisse der Gruppe (alle Szenarien). Über diese Menge entscheidet
        /// sich, ob eine Zeile überhaupt erscheint — nie über ein einzelnes Szenario,
        /// sonst hätten Word, Excel und Reiter wieder verschiedene Tabellen.
        /// </param>
        /// <param name="tarif">
        /// Tarifparameter der Gruppe; er entscheidet über die Beschriftung der
        /// Stromkostenzeile. <c>null</c> = kein wirksamer Rollentarif.
        /// </param>
        public static List<WirtZeile> Kennzahlen(IList<WirtschaftlichkeitErgebnis> menge,
                                                 TarifParameter tarif)
        {
            return Kennzahlen(menge, tarif, 0);
        }

        /// <summary>
        /// KONZEPT § 2.9 und § 2.15 — dieselbe Zeilendefinition mit AUSDRÜCKLICH
        /// gewählter Referenz. Sie entscheidet, welche Spalte „(Referenz)" trägt und
        /// welche keine Differenzkennzahlen zeigt.
        ///
        /// <para>Die Sicht 2 der Ergebnisansicht gibt hier die MENGE [A, B] und A als
        /// Referenz — so wie Sicht 1 die Menge aller Stände und den Stamm gibt. Keine
        /// dritte Wahrheit, kein zweiter Zeilenkatalog.</para>
        /// </summary>
        /// <param name="idReferenz">
        /// <c>Tab_Projekt.ID</c> der Referenz; <b>0 = Stamm</b> (Vorgabe und
        /// Bestandsverhalten).
        /// </param>
        public static List<WirtZeile> Kennzahlen(IList<WirtschaftlichkeitErgebnis> menge,
                                                 TarifParameter tarif, int idReferenz)
        {
            List<WirtZeile> zeilen = Baue(menge, tarif);
            if (idReferenz > 0)
                foreach (WirtZeile z in zeilen) z.IdReferenz = idReferenz;
            return zeilen;
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, Entscheid V‑3) — welche Kennzahlen <b>nachrichtlich</b> sind:
        /// dynamische Amortisation und interner Zinsfuß (DIN EN 17463 Anhang C; Mockup
        /// Kategorie 8, Kennzahltafel „Lohnt es sich?").
        ///
        /// <para><b>Die Annuität trägt kein Label</b> (Empfehlung Q3, gilt bis zum
        /// Anwenderentscheid): Sie ist der Kapitalwert, mit dem Annuitätenfaktor auf
        /// gleichmäßige Jahresbeträge umgelegt — dieselbe Aussage in anderer Einheit, keine
        /// zweite Entscheidungsgröße (VDI 2067). Das Mockup führt sie als „gleichmäßiger
        /// Jahresvorteil" ohne Einordnung.</para>
        ///
        /// <para>Die Regel steht EINMAL hier: Die Zeilendefinition setzt damit
        /// <see cref="WirtZeile.Nachrichtlich"/>, die Hülle ihre Kacheln.</para>
        /// </summary>
        /// <param name="schluessel">Der sprachneutrale Zeilenschlüssel (<c>AMORTISATION</c>,
        /// <c>IRR</c>).</param>
        public static bool IstNachrichtlich(string schluessel)
        {
            return string.Equals(schluessel, "AMORTISATION", StringComparison.Ordinal) ||
                   string.Equals(schluessel, "IRR", StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE E5 (U2, Mockup Kategorie 8 „Lohnt es sich?") — die Zeilen der
        /// <b>Kennzahltafel</b> in ihrer Reihenfolge: Kapitalwertdifferenz, Annuität,
        /// dynamische Amortisation, interner Zinsfuß, Wärmegestehungskosten, Nettobarwert
        /// absolut. Alle übrigen Zeilen der Definition gliedern den Kapitalwert
        /// („Woraus entsteht die Zahl?").
        /// </summary>
        public static readonly string[] KENNZAHLTAFEL =
        {
            "KAPITALWERT_DIFF", "ANNUITAET", "AMORTISATION", "IRR", "GESTEHUNGSKOSTEN", "NETTOBARWERT"
        };

        /// <summary>
        /// ETAPPE E5 (U2): Der Nettobarwert steht in BEIDEN Tafeln — als letzte Kennzahl
        /// und als Summe der Gliederung (Mockup: „Gliederung des Kapitalwerts" endet mit
        /// ihm).
        /// </summary>
        public const string GLIEDERUNGSSUMME = "NETTOBARWERT";

        /// <summary>ETAPPE E5 (U2): Steht die Zeile in der Kennzahltafel
        /// (<see cref="KENNZAHLTAFEL"/>)?</summary>
        public static bool IstKennzahl(string schluessel)
        {
            return Array.IndexOf(KENNZAHLTAFEL, schluessel) >= 0;
        }

        private static List<WirtZeile> Baue(IList<WirtschaftlichkeitErgebnis> menge,
                                            TarifParameter tarif)
        {
            var z = new List<WirtZeile>();
            if (menge == null) return z;

            // AUFTRAG VF-1 (Anwenderbefund 17.09.2026): WOMIT rechnet diese Spalte?
            //
            // In der Vergleichsgruppe stand stumm eine Spalte „mit Flotte" neben einer
            // „ohne" — der Anwender hielt die Variante für „mit Speicher". Der
            // Simulationsreiter sagte es seit jeher, die Wirtschaftlichkeit nicht; und
            // gerade sie ist der Ort, an dem zwei Zahlen gegeneinander gehalten werden.
            //
            // Die Zeile steht GANZ OBEN, vor der ersten Geldzeile: Sie ist der Kontext,
            // unter dem alles darunter zu lesen ist. Stamm und Varianten dürfen
            // verschiedene Flotten führen — die Zeile ist die Antwort darauf, nicht ein
            // Hinweis auf eine Abweichung.
            //
            // Ohne lesbaren Kontext (kein Projektbezug, keine Datenbank — so laufen die
            // Zeilenproben) entfällt sie wie jede andere Zeile ohne Wert.
            Dictionary<int, string> speicher = Speicherkontexte(menge);
            if (speicher.Count > 0)
                z.Add(new WirtZeile
                {
                    Schluessel = "SPEICHER_KONTEXT",
                    Titel = MyResource.Resource.WIRT_ZEILE_SPEICHER,
                    Text = e =>
                    {
                        string t;
                        return e != null && speicher.TryGetValue(e.IdProjekt, out t) ? t : "";
                    }
                });

            z.Add(Zahl("INVESTITION", MyResource.Resource.WIRT_ZEILE_INVESTITION,
                       e => (double?)e.Investition));

            // ETAPPE K5 (Konzept § 7.4, L7) — der Investitionszuschuss als EIGENE Zeile,
            // unmittelbar unter der Investition und NEGATIV dargestellt: Er ist der
            // Betrag, um den die Anfangsauszahlung geringer ausfällt, und genau so soll
            // ihn der Leser sehen („Zuschuss: −X €"). Gespeichert ist er positiv; das
            // Vorzeichen entsteht erst hier, in der Anzeige.
            //
            // Die Zeile erscheint nur, wenn irgendein Projekt der Gruppe einen Zuschuss
            // führt — dasselbe Muster wie bei der KWKG- und der BEHG-Zeile. Sonst stünde
            // in jedem Bericht ohne Förderung eine Nullzeile.
            if (Irgendein(menge, e => e.Zuschuss > 0))
                z.Add(Zahl("ZUSCHUSS", MyResource.Resource.WIRT_ZEILE_ZUSCHUSS,
                           e => (double?)(-e.Zuschuss)));

            z.Add(Zahl("BETRIEBSKOSTEN", MyResource.Resource.WIRT_ZEILE_BETRIEBSKOSTEN,
                       e => e.BetriebskostenJahr));
            z.Add(Zahl("ENERGIEKOSTEN", MyResource.Resource.WIRT_ZEILE_ENERGIEKOSTEN,
                       e => e.EnergiekostenJahr));

            // ETAPPE B7 (Konzept § 3.5) — die ANLAGENSCHARFE AUFSCHLÜSSELUNG.
            //
            // Die Energiekostenzeile ist in fast jedem Projekt die größte laufende
            // Position und sagte bis B7 nichts über sich. Darunter steht seither je
            // Anlage eine Herleitungszeile „Menge × Preis"; die SUMME bleibt die Zeile
            // darüber — hier wird nichts zweites gerechnet.
            //
            // Die Anlagen der GANZEN Gruppe bilden die Zeilenliste, nicht die eines
            // Projekts: Stamm und Varianten führen verschiedene Anlagen, und eine
            // Zeile, die nur in einer Spalte einen Wert hat, gehört trotzdem in die
            // Tabelle — in den übrigen steht dann „—".
            foreach (string anlage in Anlagennamen(menge))
            {
                string name = anlage;      // Fangkopie für den Abschluss
                z.Add(Unter("ENERGIEKOSTEN_ANLAGE_" + Schluesselform(name),
                            string.Format(MyResource.Resource.WIRT_ENK_ZEILE, name),
                            e => AnlageKosten(e, name), ""));
            }

            // ETAPPE E7 — die Zeile hieß bis hierher in BEIDEN Tarifmodellen
            // „Stromkosten Tarif". Im Rollenmodell trägt sie aber den RESTSTROM-Betrag,
            // also die Kosten MIT Anlage — und steht damit direkt neben den vermiedenen
            // Kosten, die sich auf den Bezug OHNE Anlage beziehen. Der Titel sagt
            // seither, welche der beiden Größen gemeint ist.
            //
            // Q11 (E7b): Gerechnet wird der Betrag nur noch im Rollenmodell. Den
            // Bezugstitel trägt allein ein GESPEICHERTER Lauf aus der Zeit des
            // Zeitzonentarifs, bis „Berechnen“ ihn ersetzt.
            if (Irgendein(menge, e => e.StromkostenTarif.HasValue))
                z.Add(Zahl("STROMKOSTEN_TARIF",
                           tarif != null && tarif.Wirksam
                               ? MyResource.Resource.WIRT_ZEILE_STROMKOSTEN_RESTSTROM
                               : MyResource.Resource.WIRT_ZEILE_STROMKOSTEN_BEZUG,
                           e => e.StromkostenTarif));

            // Die BEZUGSSPITZE — eine HERLEITUNG, keine Geldzeile: Sie steht zwischen
            // den Stromkosten und dem Rest, weil sie erklärt, wie der Leistungsanteil
            // des Strompreises zustande kommt, und weil an ihr der Effekt der
            // Lastspitzenkappung abzulesen ist (Stamm gegen Speichervariante). Sie
            // erscheint nur, wenn irgendein Lauf der Gruppe Zeitreihen geführt hat —
            // ohne sie stünde in jedem Bericht eine „—"-Zeile.
            if (Irgendein(menge, e => e.BezugsspitzeKW.HasValue))
                z.Add(Zahl("BEZUGSSPITZE_STROM", MyResource.Resource.WIRT_ZEILE_BEZUGSSPITZE_STROM,
                           e => e.BezugsspitzeKW));

            if (Irgendein(menge, e => e.CO2AbgabeJahr > 0))
                z.Add(Zahl("CO2_BEHG", MyResource.Resource.WIRT_ZEILE_CO2_BEHG,
                           e => (double?)e.CO2AbgabeJahr));

            // =================================================================
            // ETAPPE B7 — DIE RUBRIK „Erlöse und Vorteile" (Konzept § 2.6)
            //
            // Bis hierher standen die Erlöspositionen verstreut zwischen den Kosten:
            // Einspeiseerlös, dann der PV-Block, dann KWKG, dann die drei Steuerzeilen,
            // dann die vermiedenen Kosten. Jede für sich richtig — zusammen aber keine
            // Aussage, und vor allem keine, die Zahlung von Ausweis trennt.
            //
            // Seit B7 sind es ZWEI Blöcke, und die Teilung ist die Aussage:
            //   BLOCK A geht in den Kapitalwert und wird summiert.
            //   BLOCK B wird AUSGEWIESEN und NIE summiert — der Vorteil steckt bereits
            //   in einer anderen Position (kleinere Bezugsrechnung), ein zweites Buchen
            //   wäre Doppelzählung (E5).
            // Die Reihenfolge folgt dem Entwurf „Ergebnis, Bandbreite, Herkunft", damit
            // die Rubrik später ohne Umbau in die neue Ergebnisansicht wandert.
            // =================================================================

            // Führt das Projekt überhaupt eine Anlage, für die die Position gilt? Daran
            // — nicht am Betrag — hängt, ob eine A-Zeile erscheint (ImmerZeigen).
            bool hatBhkw = Irgendein(menge, e => e.KwkgVbhElektrisch > 0 ||
                                                 e.KwkgErloesJahr1 > 0 ||
                                                 e.EnergiesteuerJahr1 > 0 ||
                                                 e.StromsteuerBefreiungJahr1 > 0 ||
                                                 e.EinspeiseerloesKwkJahr != 0 ||
                                                 (e.KwkgModule != null && e.KwkgModule.Count > 0));

            var blockA = new List<WirtZeile>();   // Grundlage der Summenzeile

            // AUFTRAG U6 (Konzept § 2.6, Entscheid Q15) — die A-Zeilen entstehen in
            // DIESER Liste, jede mit ihrem Anlagenbezug, und wandern erst danach nach
            // Komponenten geordnet in den Katalog (Bloecke). Die Reihenfolge innerhalb
            // einer Komponente bleibt die des Entwurfs; die GLIEDERUNG entsteht einmal
            // und nicht vier Mal in Seite, Word, Excel und Vorschau.
            var aZeilen = new List<WirtZeile>();

            // ---- A1/A2: KWK-Zuschlag nach § 7 KWKG --------------------------------
            // OHNE BHKW entsteht die Zeile gar nicht erst. „Immer zeigen" heißt: auch
            // bei Betrag 0 — nicht: in jedem Projekt. Ein reines PV- oder Kesselprojekt
            // bekäme sonst zwei Nullzeilen über Vorschriften, die es nicht betreffen.
            if (hatBhkw)
            {
                WirtZeile aKwkg = Erloes(blockA, "ERL_A_KWKG", MyResource.Resource.WIRT_ERL_A_KWKG,
                                         e => (double?)e.KwkgErloesJahr1, true);
                aKwkg.Grundtext = e => MyResource.Resource.WIRT_GRUND_KWKG;
                Zu(aZeilen, aKwkg, WirtZeile.KOMPONENTE_BHKW);
                // AUFTRAG 9d — der GRUND der Null, nicht die Bedingung: Der Zelltext
                // sagt „kein Satz gepflegt ODER Kontingent erschöpft"; hier steht,
                // was der Lauf tatsächlich festgestellt hat. Ohne Feststellung bleibt
                // die Zeile leer und entfällt (Sichtbare) — geraten wird nichts.
                Zu(aZeilen, UnterText("ERL_A_KWKG_GRUND", MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                      KwkgGrund, WirtZeile.BLOCK_A), WirtZeile.KOMPONENTE_BHKW);
            }

            // Die anlagenscharfe Aufteilung in Einspeisung (§ 7 Abs. 1) und Eigenstrom
            // (§ 7 Abs. 2) trägt der Modulnachweis des Laufs. Er reist seit B7P im
            // Nachweisumschlag des Ergebnisses mit — die beiden Unterzeilen erscheinen
            // deshalb auch beim gebuchten Stand, nicht nur im frisch gerechneten Lauf.
            if (Irgendein(menge, e => e.KwkgModule != null && e.KwkgModule.Count > 0))
            {
                Zu(aZeilen, Unter("ERL_A1_EINSPEISUNG", MyResource.Resource.WIRT_ERL_A1_EINSPEISUNG,
                                  e => KwkgAnteil(e, true), WirtZeile.BLOCK_A),
                   WirtZeile.KOMPONENTE_BHKW);
                // AUFTRAG #351 (U23/U26) — UNTER JEDER der beiden Geldzeilen der Satz,
                // mit dem sie gerechnet wurde, und die Herkunft dazu. Der Nachweis trug
                // den angesetzten Satz und die Herleitung des Vorschlags schon, verglich
                // beide aber nicht; der Leser sah 6,0000 neben einer Tranchenrechnung,
                // die 5,5667 ergibt, und musste selbst nachrechnen, ob das ein eigener
                // Wert ist. Die Stellenzahl ist die des Satzfeldes (KwkgSatzHerkunft) —
                // dieselbe Größe sieht in Feld, Rubrik, Bericht und Vorschau gleich aus.
                Zu(aZeilen, UnterText("ERL_A1_SATZ", MyResource.Resource.WIRT_ERL_A1_SATZ,
                                      e => KwkgSatzzeile(e, true), WirtZeile.BLOCK_A),
                   WirtZeile.KOMPONENTE_BHKW);
                Zu(aZeilen, Unter("ERL_A2_EIGEN", MyResource.Resource.WIRT_ERL_A2_EIGEN,
                                  e => KwkgAnteil(e, false), WirtZeile.BLOCK_A),
                   WirtZeile.KOMPONENTE_BHKW);
                Zu(aZeilen, UnterText("ERL_A2_SATZ", MyResource.Resource.WIRT_ERL_A2_SATZ,
                                      e => KwkgSatzzeile(e, false), WirtZeile.BLOCK_A),
                   WirtZeile.KOMPONENTE_BHKW);
            }
            if (Irgendein(menge, e => e.KwkgVbhElektrisch > 0))
                Zu(aZeilen, Unter("VBH_ELEKTRISCH", MyResource.Resource.WIRT_ZEILE_VBH_ELEKTRISCH,
                                  e => e.KwkgVbhElektrisch > 0 ? (double?)e.KwkgVbhElektrisch : null,
                                  WirtZeile.BLOCK_A), WirtZeile.KOMPONENTE_BHKW);

            // ---- A3: Pauschale nach § 9 KWKG (AUFTRAG U17) ------------------------
            // Sie erscheint NUR, wenn sie greift — also wenn der Schalter gesetzt ist
            // UND die Anlage unter der Leistungsgrenze von 2 kW(el) bleibt. Über der
            // Grenze bleibt der Schalter ohne Wirkung, der laufende Zuschlag rechnet
            // weiter, und eine Nullzeile über eine Vorschrift, die nicht greift, wäre
            // eine Behauptung. Deshalb KEIN „ImmerZeigen": Der Betrag ist die
            // Bedingung, nicht die Anlagenart.
            //
            // KEIN SUMMAND. Die Summe darunter ist eine €/a-Summe des Jahres 1; die
            // Pauschale ist ein EINMALBETRAG in € zum Zeitpunkt 0. Beides zu addieren
            // wäre derselbe Einheitenfehler, aus dem auch der Restwert-Barwert nicht in
            // dieser Summe steht. Im KAPITALWERT ist sie seit jeher enthalten (Index 0
            // der Erlösreihe KWKG_PAUSCHALE, unabgezinst) — diese Zeile macht sie
            // sichtbar, sie bucht nichts.
            if (Irgendein(menge, e => e.KwkgPauschaleEur > 0))
                Zu(aZeilen, new WirtZeile
                {
                    Schluessel = "ERL_A_KWKG_PAUSCHALE",
                    Titel = MyResource.Resource.WIRT_ERL_A_KWKG_PAUSCHALE,
                    Block = WirtZeile.BLOCK_A,
                    Wert = e => e != null && e.KwkgPauschaleEur > 0
                                ? (double?)e.KwkgPauschaleEur : null
                }, WirtZeile.KOMPONENTE_BHKW);

            // ---- A4/A5: Energiesteuer-Entlastung, ZWEI Zeilen (AUFTRAG U7) -------
            //
            // § 53 und § 53a Abs. 5 entlasten den Brennstoff der STROMERZEUGUNG,
            // § 54 den Heizstoff des produzierenden Gewerbes. Zwei Vorschriften, zwei
            // Bedingungen, zwei Anlagenarten. Bis U7 gab der Steuerrechner EINE Summe
            // zurück (Befund B7-1), und die Rubrik konnte nur eine Zeile zeigen, deren
            // Titel beide Paragrafen nannte — an einem Projekt mit BHKW UND Kessel war
            // der Betrag damit keiner der beiden Anlagen zuzuordnen. Seit U7 liefert
            // der Rechner beide Beträge, und jeder steht bei seiner Vorschrift.
            //
            // UNTER JEDER der beiden Zeilen die Herleitung aus dem Nachweis des Laufs:
            // Menge in der gesetzlichen Einheit, Satz, Betrag — und beim § 54 der
            // abgezogene Sockelbetrag. Sie reist im Nachweisumschlag mit (Fassung 4),
            // der gebuchte Stand trägt sie deshalb ebenso wie der frisch gerechnete.
            //
            // DER RÜCKFALL: Ein vor U7 gebuchter Stand kennt die Aufteilung nicht.
            // Dann bleibt es bei der EINEN Zeile über beide Vorschriften — zwei
            // Nullzeilen neben einer Ergebnisspalte mit Betrag wären eine Behauptung.
            // Entschieden wird über die GANZE Gruppe, damit Stamm und Varianten
            // dieselbe Tabelle zeigen und die Summe des Blocks A in jedem Fall
            // zahlengleich zur bisherigen bleibt.
            if (hatBhkw)
            {
                if (SteuerAufgeteilt(menge))
                {
                    WirtZeile a53 = Erloes(blockA, "ERL_A_ENERGIESTEUER",
                                           MyResource.Resource.WIRT_ERL_A_ENERGIESTEUER,
                                           e => (double?)e.Energiesteuer53Jahr1, true);
                    a53.Grundtext = e => MyResource.Resource.WIRT_GRUND_ENERGIESTEUER;
                    Zu(aZeilen, a53, WirtZeile.KOMPONENTE_BHKW);
                    Zu(aZeilen, UnterText("ERL_A_ENERGIESTEUER_SATZ",
                                          MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                          e => Energiesteuerzeile(e, false), WirtZeile.BLOCK_A),
                       WirtZeile.KOMPONENTE_BHKW);

                    // AUFTRAG U6 — § 54 entlastet den HEIZstoff: Diese Zeile gehoert dem
                    // KESSEL, nicht dem Blockheizkraftwerk. Erst U7 hat die beiden
                    // Betraege getrennt; ohne die Trennung haette der Anlagenbezug hier
                    // keinen Gegenstand (Voraussetzung aus der Analyse, Zeile R8).
                    WirtZeile a54 = Erloes(blockA, "ERL_A_ENERGIESTEUER_54",
                                           MyResource.Resource.WIRT_ERL_ENERGIEST_54,
                                           e => (double?)e.Energiesteuer54Jahr1, true);
                    a54.Grundtext = e => MyResource.Resource.WIRT_GRUND_NUR_PROD_GEWERBE;
                    Zu(aZeilen, a54, WirtZeile.KOMPONENTE_KESSEL);
                    Zu(aZeilen, UnterText("ERL_A_ENERGIESTEUER_54_SATZ",
                                          MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                          e => Energiesteuerzeile(e, true), WirtZeile.BLOCK_A),
                       WirtZeile.KOMPONENTE_KESSEL);
                }
                else
                {
                    // Der Rueckfall vor U7: EINE Zeile ueber beide Vorschriften. Sie ist
                    // keiner Anlage zuzuordnen — genau das war der Befund B7-1 — und
                    // steht deshalb im Block „projektweit".
                    WirtZeile aEnst = Erloes(blockA, "ERL_A_ENERGIESTEUER",
                                             MyResource.Resource.WIRT_ERL_ENERGIEST_GESAMT,
                                             e => (double?)e.EnergiesteuerJahr1, true);
                    aEnst.Grundtext = e => MyResource.Resource.WIRT_GRUND_ENERGIESTEUER;
                    Zu(aZeilen, aEnst, WirtZeile.KOMPONENTE_PROJEKTWEIT);
                }
            }

            // ---- A6: Stromsteuer-Entlastung Netzbezug (§ 9b) ---------------------
            // Sie hängt am RESTBEZUG, nicht an einer Anlage — deshalb gilt sie für
            // jedes Projekt und steht immer da (Konzept § 2.6, Klarstellung 2).
            WirtZeile aEntl = Erloes(blockA, "ERL_A_STROMST_ENTLASTUNG",
                                     MyResource.Resource.WIRT_ERL_A_STROMST_ENTLASTUNG,
                                     e => (double?)e.StromsteuerEntlastungJahr1, true);
            aEntl.Grundtext = e => MyResource.Resource.WIRT_GRUND_NUR_PROD_GEWERBE;
            Zu(aZeilen, aEntl, WirtZeile.KOMPONENTE_PROJEKTWEIT);
            // AUFTRAG 9d — der Grund des Steuerrechners: Unternehmensart, fehlender
            // Satz oder Sockelbetrag. Bis 9d stand er nur im Hinweisfeld des Laufs,
            // verbunden mit allen anderen Sätzen zu einem Text.
            Zu(aZeilen, UnterText("ERL_A_STROMST_ENTLASTUNG_GRUND",
                                  MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                  e => e != null && e.StromsteuerEntlastungJahr1 == 0
                                       ? Positionsgrund(e, SteuerPosition.STROMST_ENTLASTUNG) : "",
                                  WirtZeile.BLOCK_A), WirtZeile.KOMPONENTE_PROJEKTWEIT);

            // ---- A7: Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3) -----
            // Sie folgt dem Modus aus B6: ERLOES bucht sie (Block A), AUSWEIS zeigt sie
            // nur (Block B, weiter unten). Ein Projekt kann nur eines von beidem sein.
            bool befreiungAlsErloes = Irgendein(menge, e => e.StromsteuerBefreiungAlsErloes);
            if (Irgendein(menge, e => e.StromsteuerBefreiungJahr1 > 0) && befreiungAlsErloes)
                Zu(aZeilen, Erloes(blockA, "ERL_A_STROMST_BEFREIUNG",
                                   MyResource.Resource.WIRT_ERL_A_STROMST_BEFREIUNG,
                                   e => (double?)e.StromsteuerBefreiungJahr1, false),
                   WirtZeile.KOMPONENTE_BHKW);

            // ---- A8: Einspeiseerlös Strom ----------------------------------------
            // AUFTRAG U6 — der Anlagenbezug der SUMMENzeile: Sie gehoert der Anlage,
            // die tatsaechlich einspeist. Speisen BEIDE ein, bleibt die Summe
            // projektweit und die zwei davon-Zeilen darunter sagen, wem welcher Teil
            // gehoert — eine Summe ueber zwei Anlagen einer von beiden zuzuschlagen
            // waere eine Behauptung.
            bool pvSpeist = Irgendein(menge, e => e.EinspeiseerloesPvJahr != 0);
            bool kwkSpeist = Irgendein(menge, e => e.EinspeiseerloesKwkJahr != 0);
            string kEinsp = pvSpeist && kwkSpeist ? WirtZeile.KOMPONENTE_PROJEKTWEIT
                          : pvSpeist ? WirtZeile.KOMPONENTE_PV
                          : kwkSpeist || hatBhkw ? WirtZeile.KOMPONENTE_BHKW
                          : WirtZeile.KOMPONENTE_PROJEKTWEIT;

            WirtZeile aEinsp = Erloes(blockA, "EINSPEISEERLOES",
                                      MyResource.Resource.WIRT_ERL_A_EINSPEISUNG,
                                      e => (double?)e.EinspeiseerloesJahr, true);
            aEinsp.Grundtext = e => MyResource.Resource.WIRT_GRUND_EINSPEISUNG;
            Zu(aZeilen, aEinsp, kEinsp);
            // AUFTRAG 9d — „keine Vergütung gepflegt" und „gar keine Einspeisung im
            // Lauf" sind zwei verschiedene Befunde; der Modulnachweis unterscheidet
            // sie, weil er die eingespeiste MENGE führt.
            Zu(aZeilen, UnterText("EINSPEISEERLOES_GRUND", MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                  EinspeisungGrund, WirtZeile.BLOCK_A), kEinsp);
            // Aufschlüsselung nur, wenn beide Anteile vorkommen; bei einem reinen PV-
            // oder reinen KWK-Projekt wäre sie die Gesamtzeile ein zweites Mal.
            if (pvSpeist && kwkSpeist)
            {
                Zu(aZeilen, Unter("EINSPEISEERLOES_PV",
                                  MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_PV,
                                  e => (double?)e.EinspeiseerloesPvJahr, WirtZeile.BLOCK_A),
                   kEinsp);
                Zu(aZeilen, Unter("EINSPEISEERLOES_KWK",
                                  MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_KWK,
                                  e => (double?)e.EinspeiseerloesKwkJahr, WirtZeile.BLOCK_A),
                   kEinsp);
            }

            // ---- A9: PV-Vergütung (PV-Konzept § 6.4, Etappe P6) ------------------
            // Der Block erscheint nur, wenn irgendein Lauf der Gruppe den
            // Vergütungsdialog aktiv hatte; die MENGENzeilen des Ausweises (Kappung,
            // Vergütungsausfall, vermiedener Bezug) stehen seit B7 in Block B.
            if (Irgendein(menge, e => !string.IsNullOrEmpty(e.PvVerguetungsform)))
            {
                Zu(aZeilen, new WirtZeile
                {
                    Schluessel = "PV_FORM",
                    Titel = MyResource.Resource.WIRT_ZEILE_PV_FORM,
                    Block = WirtZeile.BLOCK_A,
                    Einzug = 1,
                    Text = e => PvFormText(e.PvVerguetungsform)
                }, WirtZeile.KOMPONENTE_PV);
                WirtZeile aw = Unter("PV_AW", MyResource.Resource.WIRT_ZEILE_PV_AW,
                                     e => e.PvAnzulegenderWert, WirtZeile.BLOCK_A);
                aw.Format = "N2"; aw.ExcelFormat = "#,##0.00";
                Zu(aZeilen, aw, WirtZeile.KOMPONENTE_PV);
                // KONZEPT § 2.16 - die HERKUNFT der Verguetung je Spalte. Zwei Varianten
                // derselben Gruppe koennen mit verschiedenen Verguetungen rechnen; ohne
                // diese Zeile stuenden Vermarktungsform und AW nebeneinander, ohne zu
                // sagen, WOHER sie kommen. Sie reist im Nachweisumschlag mit (Fassung 3)
                // - Word und Excel lesen dieselbe Definition, und der gebuchte Stand
                // traegt sie ebenso wie der frisch gerechnete.
                Zu(aZeilen, UnterText("PV_HERKUNFT", MyResource.Resource.WIRT_ZEILE_PV_HERKUNFT,
                                      PvHerkunftText, WirtZeile.BLOCK_A), WirtZeile.KOMPONENTE_PV);
                if (Irgendein(menge, e => e.PvMarktpraemie > 0))
                    Zu(aZeilen, Erloes(blockA, "PV_MARKTPRAEMIE",
                                       MyResource.Resource.WIRT_ZEILE_PV_MARKTPRAEMIE,
                                       e => (double?)e.PvMarktpraemie, false),
                       WirtZeile.KOMPONENTE_PV);
                if (Irgendein(menge, e => e.PvKompensation51a > 0))
                    Zu(aZeilen, Erloes(blockA, "PV_51A", MyResource.Resource.WIRT_ZEILE_PV_51A,
                                       e => (double?)e.PvKompensation51a, false),
                       WirtZeile.KOMPONENTE_PV);
            }

            // ---- Der Block A, nach Komponenten gegliedert (U6) --------------------
            // Kopf, dann je Komponente Kopf · Zeilen · Zwischensumme, zuletzt der Block
            // „projektweit". Die GESAMTsumme darunter bleibt unveraendert: Sie summiert
            // dieselben Summanden wie vor U6, nicht die Zwischensummen — sonst haette
            // eine vergessene Komponente die Summe stillschweigend verkuerzt.
            z.Add(Kopf("ERL_KOPF_A", MyResource.Resource.WIRT_ERL_KOPF_A, WirtZeile.BLOCK_A));
            Bloecke(z, aZeilen, blockA, WirtZeile.BLOCK_A, null);

            // ---- Summenzeile: NUR Block A ----------------------------------------
            // Sie summiert die €/a-Zeilen des Jahres 1. Der Restwert (A10) steht
            // bewusst NICHT darin: Er ist ein BARWERT über T und stünde in einer
            // €/a-Summe als Einheitenfehler. Seine Zeile bleibt unten beim Kapitalwert,
            // wo sie mit dem Nettobarwert zusammen gelesen wird. Aus demselben Grund
            // fehlt die KWKG-Pauschale (A3): ein Einmalbetrag in € zum Zeitpunkt 0.
            var summanden = new List<WirtZeile>(blockA);
            z.Add(new WirtZeile
            {
                Schluessel = "ERL_A_SUMME",
                Titel = MyResource.Resource.WIRT_ERL_A_SUMME,
                Block = WirtZeile.BLOCK_A,
                IstSumme = true,
                Wert = e => Teilsumme(summanden, e)
            });

            // =================================================================
            // BLOCK B — Ausweis, NICHT addieren
            // =================================================================
            bool hatVermieden = Irgendein(menge, e => e.VermiedenGesamtJahr != 0 ||
                                                      e.VermiedenArbeitJahr != 0);
            bool hatBefreiungAusweis = Irgendein(menge, e => e.StromsteuerBefreiungJahr1 > 0) &&
                                       !befreiungAlsErloes;
            bool hatPvAusweis = Irgendein(menge, e => PvVermiedenFlat(e).HasValue ||
                                                      e.PvKappungsverlustKwh > 0 ||
                                                      e.PvVerguetungsausfallKwh > 0);
            if (hatVermieden || hatBefreiungAusweis || hatPvAusweis)
            {
                var bZeilen = new List<WirtZeile>();

                // A7 im Modus AUSWEIS (B6): Auf selbst erzeugten und selbst verbrauchten
                // Strom entsteht gar keine Stromsteuer — der Vorteil steckt bereits in
                // der kleineren Bezugsrechnung. Der Strom stammt aus der KWK-Anlage,
                // deshalb steht die Zeile bei ihr (U6).
                if (hatBefreiungAusweis)
                    Zu(bZeilen, Ausweis("ERL_B_STROMST_BEFREIUNG",
                                        MyResource.Resource.WIRT_ZEILE_STROMST_BEFREIUNG_AUSWEIS,
                                        e => (double?)e.StromsteuerBefreiungJahr1),
                       WirtZeile.KOMPONENTE_BHKW);

                // B1 — vermiedene Stromkosten, BRUTTO, darunter die Korrektur und der
                // effektive Betrag (Konzept § 2.6, Klarstellung 1).
                List<string> vermiedenK = hatVermieden ? VermiedenKomponenten(menge)
                                                       : new List<string>();
                // Die § 9b-Korrektur erscheint NUR, wo sie gilt — beim produzierenden
                // Gewerbe mit bestimmbarer vermiedener Menge. Ohne sie IST brutto
                // wirksam, und zwei gleiche Zahlen untereinander erklären nichts; dann
                // entfällt auch die Abschlusszeile des Komponentenblocks.
                bool hatAbzug = Irgendein(menge, e => e.VermiedenEntlastung9bJahr != 0);
                if (hatVermieden && vermiedenK.Count > 0)
                {
                    // AUFTRAG U6 — je Anlage der ARBEITSANTEIL und die auf ihn
                    // entfallende entgangene § 9b-Entlastung. Verteilt hat das der Lauf
                    // (VermiedenJeAnlage); hier wird nur gelesen. Die Herleitungszeile
                    // nennt Menge, Anteil und — bei mehr als einer Anlage — die
                    // Naeherung (A12): Die Strommatrix trennt nicht nach Anlage,
                    // der Schluessel ist der Eigenverbrauch je Anlage, brutto aus
                    // der Strommatrix (Befund V-4, seit E7 Konzept § 6.3 Nr. 32).
                    foreach (string komponente in vermiedenK)
                    {
                        string k = komponente;      // Fangkopie für den Abschluss
                        Zu(bZeilen, Ausweis("VERMIEDEN_BRUTTO_" + Kennform(k),
                                            MyResource.Resource.WIRT_ERL_B1_BRUTTO,
                                            e => VermiedenBetrag(e, k, false)), k);
                        Zu(bZeilen, UnterText("VERMIEDEN_HERLEITUNG_" + Kennform(k),
                                              MyResource.Resource.WIRT_ERL_HERLEITUNG,
                                              e => Vermiedenherleitung(e, k), WirtZeile.BLOCK_B), k);
                        if (hatAbzug)
                            Zu(bZeilen, Unter("ERL_B1_ABZUG_9B_" + Kennform(k),
                                              MyResource.Resource.WIRT_ERL_B1_ABZUG_9B,
                                              e => VermiedenBetrag(e, k, true), WirtZeile.BLOCK_B), k);
                    }

                    // Der LEISTUNGSANTEIL bleibt projektweit (Anwenderentscheid Q15 vom
                    // 22.09.2026): Er haengt an der Bezugsspitze des ganzen Projekts,
                    // nicht an einer Anlage. Ohne gerechnete Spitze steht dort eine
                    // Nullzeile MIT Grund statt einer stillen 0.
                    WirtZeile leist = Unter("VERMIEDEN_LEISTUNG",
                                            MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG,
                                            e => (double?)e.VermiedenLeistungJahr,
                                            WirtZeile.BLOCK_B);
                    leist.Einzug = 0;
                    leist.ImmerZeigen = true;
                    leist.Grundtext = e => e != null && !e.BezugsspitzeKW.HasValue
                                         ? MyResource.Resource.WIRT_ERL_GRUND_KEINE_BEZUGSSPITZE : "";
                    Zu(bZeilen, leist, WirtZeile.KOMPONENTE_PROJEKTWEIT);
                }
                else if (hatVermieden)
                {
                    // DER RUECKFALL: ein Lauf ohne Aufteilung (vor U6 gebucht, oder ohne
                    // Eigenverbrauchsmengen). Dann bleibt es bei der EINEN projektweiten
                    // Kette — Komponentenbloecke ohne Zahlen waeren eine Behauptung.
                    Zu(bZeilen, Ausweis("VERMIEDEN_GESAMT", MyResource.Resource.WIRT_ERL_B1_BRUTTO,
                                        e => (double?)e.VermiedenGesamtJahr),
                       WirtZeile.KOMPONENTE_PROJEKTWEIT);
                    Zu(bZeilen, Unter("VERMIEDEN_ARBEIT",
                                      MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT,
                                      e => (double?)e.VermiedenArbeitJahr, WirtZeile.BLOCK_B),
                       WirtZeile.KOMPONENTE_PROJEKTWEIT);
                    Zu(bZeilen, Unter("VERMIEDEN_LEISTUNG",
                                      MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG,
                                      e => (double?)e.VermiedenLeistungJahr, WirtZeile.BLOCK_B),
                       WirtZeile.KOMPONENTE_PROJEKTWEIT);

                    if (hatAbzug)
                    {
                        Zu(bZeilen, Unter("ERL_B1_ABZUG_9B",
                                          MyResource.Resource.WIRT_ERL_B1_ABZUG_9B,
                                          e => (double?)(-e.VermiedenEntlastung9bJahr),
                                          WirtZeile.BLOCK_B), WirtZeile.KOMPONENTE_PROJEKTWEIT);
                        WirtZeile eff = Ausweis("ERL_B1_EFFEKTIV",
                                                MyResource.Resource.WIRT_ERL_B1_EFFEKTIV,
                                                e => (double?)e.VermiedenEffektivJahr);
                        eff.IstSumme = true;      // Zwischenergebnis des Ausweises
                        eff.IstTeilsumme = true;  // aber NICHT die Blocksumme (U6)
                        Zu(bZeilen, eff, WirtZeile.KOMPONENTE_PROJEKTWEIT);
                    }
                }

                // B2 — PV-Ausweis: vermiedener Bezug sowie Kappungs- und Ausfallmengen.
                // Der vermiedene Bezug zum Flat-Preis steht nur, wo die Aufteilung der
                // vermiedenen Kosten KEINEN PV-Anteil trägt (Flat-Tarif, Stand vor E7):
                // Im Rollentarif ersetzt der PV-Anteil die Zeile (Orchestrator-Entscheid
                // 23.09.2026, Frage 6 der Etappe E7a) — sonst stünden zwei Beträge für
                // denselben vermiedenen Bezug untereinander.
                if (Irgendein(menge, e => PvVermiedenFlat(e).HasValue))
                    Zu(bZeilen, Ausweis("PV_VERMIEDEN", MyResource.Resource.WIRT_ZEILE_PV_VERMIEDEN,
                                        e => PvVermiedenFlat(e)), WirtZeile.KOMPONENTE_PV);
                if (Irgendein(menge, e => e.PvVerguetungsausfallKwh > 0))
                {
                    Zu(bZeilen, Ausweis("PV_AUSFALL_KWH",
                                        MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_KWH,
                                        e => (double?)e.PvVerguetungsausfallKwh),
                       WirtZeile.KOMPONENTE_PV);
                    Zu(bZeilen, Unter("PV_AUSFALL_EUR",
                                      MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_EUR,
                                      e => (double?)e.PvVerguetungsausfall, WirtZeile.BLOCK_B),
                       WirtZeile.KOMPONENTE_PV);
                }
                if (Irgendein(menge, e => e.PvKappungsverlustKwh > 0))
                    Zu(bZeilen, Ausweis("PV_KAPPUNG", MyResource.Resource.WIRT_ZEILE_PV_KAPPUNG,
                                        e => (double?)e.PvKappungsverlustKwh),
                       WirtZeile.KOMPONENTE_PV);

                // Block B wird NICHT summiert; seine Komponentenbloecke schliessen
                // deshalb mit dem WIRKSAMEN Betrag der Anlage, nicht mit einer Summe
                // ueber alles, was darin steht (Mockup Kategorie 7).
                z.Add(Kopf("ERL_KOPF_B", MyResource.Resource.WIRT_ERL_KOPF_B, WirtZeile.BLOCK_B));
                Bloecke(z, bZeilen, null, WirtZeile.BLOCK_B,
                        k => hatAbzug && vermiedenK.Contains(k) ? Wirksamzeile(k) : null);
            }

            // SP-E-2: Die Zeile „Aufschläge auf den Strombezug" ist entfallen. Die
            // Preisanteile zerlegen den Arbeitspreis und stecken damit vollständig in
            // den Energiekosten; eine eigene Zeile wäre eine zweite Ansage derselben
            // Zahl.

            // ETAPPE W5-B-10 (VALERI-Abgleich): Die Ersatzbeschaffungen wurden seit W1
            // GERECHNET, aber nie AUSGEWIESEN - sie steckten stumm in den Barwerten der
            // Ausgaben. DIN EN 17463 verlangt sie als eigene Position; zusammen mit dem
            // Restwert darunter sind sie die zwei Groessen, an denen haengt, ob ein
            // Betrachtungszeitraum ueberhaupt zur Nutzungsdauer passt. Die Zeile
            // erscheint nur, wo es Ersatz gibt (n < T bei irgendeiner Position).
            if (Irgendein(menge, e => e.ErsatzBarwert != 0))
                z.Add(Zahl("ERSATZ", MyResource.Resource.WIRT_ZEILE_ERSATZ,
                           e => (double?)e.ErsatzBarwert));

            z.Add(Zahl("RESTWERT", MyResource.Resource.WIRT_ZEILE_RESTWERT,
                       e => (double?)e.RestwertBarwert));

            // ANWENDERENTSCHEID Q19 (Mockup-Prüfung § 4) — REINE ANZEIGE: Die
            // Differenzkennzahl steht ÜBER dem Nettobarwert. Sie ist die Zahl, nach der
            // entschieden wird; der absolute Barwert ist die Herleitung dazu. Gerechnet
            // wird nichts anders, beide Zeilen führen dieselben Werte wie vorher.
            //
            // ETAPPE E5 — DIE REIHENFOLGE DER KENNZAHLTAFEL des abgenommenen Mockups
            // („Lohnt es sich?", Kategorie 8): Kapitalwertdifferenz, Annuität der
            // Differenz, dynamische Amortisation, interner Zinsfuß,
            // Wärmegestehungskosten, zuletzt der Nettobarwert absolut. Der absolute
            // Barwert steht bewusst UNTER der Differenz: Er ist bei jedem
            // Versorgungskonzept negativ und taugt nicht zum Vergleich — nur der Abstand
            // zur Referenz tut das. Amortisation und Zinsfuß tragen das Kennzeichen
            // „nachrichtlich" (V‑A, Entscheid V‑3; die Annuität nach Empfehlung Q3 nicht).
            // Keine Zahl ändert sich.
            WirtZeile diff = Zahl("KAPITALWERT_DIFF", MyResource.Resource.WIRT_ZEILE_KAPITALWERT_DIFF,
                                  e => e.KapitalwertDiff);
            diff.StammAnzeige = MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ;
            z.Add(diff);

            WirtZeile ann = Zahl("ANNUITAET", MyResource.Resource.WIRT_ZEILE_ANNUITAET,
                                 e => e.AnnuitaetKW);
            ann.StammAnzeige = "—";
            ann.Nachrichtlich = IstNachrichtlich(ann.Schluessel);
            z.Add(ann);

            WirtZeile amo = Zahl("AMORTISATION", MyResource.Resource.WIRT_ZEILE_AMORTISATION,
                                 e => e.AmortisationJahre);
            amo.Format = "N1"; amo.ExcelFormat = "#,##0.0"; amo.StammAnzeige = "—";
            amo.Nachrichtlich = IstNachrichtlich(amo.Schluessel);
            // ETAPPE E5 (Q16): Amortisiert sich eine Variante im Zeitraum nicht, sagt die
            // Zelle das — statt eines stummen Strichs.
            amo.Grundtext = ValeriAusweis.AmortisationGrund;
            z.Add(amo);

            if (Irgendein(menge, e => e.IRR.HasValue))
            {
                WirtZeile irr = Zahl("IRR", MyResource.Resource.WIRT_ZEILE_IRR, e => e.IRR);
                irr.Format = "N1"; irr.ExcelFormat = "#,##0.0"; irr.StammAnzeige = "—";
                irr.Nachrichtlich = IstNachrichtlich(irr.Schluessel);
                // ETAPPE E5 (V‑A, Befund A2): kein Vorzeichenwechsel → „kein Zinsfuß
                // bestimmbar" statt eines stummen Strichs; mehr als einer → Warnung.
                irr.Grundtext = ValeriAusweis.IzfGrund;
                irr.Warntext = ValeriAusweis.IzfWarnung;
                z.Add(irr);
            }

            WirtZeile geste = Zahl("GESTEHUNGSKOSTEN", MyResource.Resource.WIRT_ZEILE_GESTEHUNGSKOSTEN,
                                   e => e.Gestehungskosten);
            geste.Format = "N3"; geste.ExcelFormat = "#,##0.000";
            z.Add(geste);

            z.Add(Zahl("NETTOBARWERT", MyResource.Resource.WIRT_ZEILE_NETTOBARWERT,
                       e => e.Kapitalwert));

            // ETAPPE E7 — die Herkunft der verwendeten Steuersätze stand bisher NUR im
            // Ergebnisreiter. Der Bericht ist aber das Dokument, mit dem der Rechtsstand
            // gegenüber Dritten nachgewiesen wird; der Reiter ist es nicht.
            if (Irgendein(menge, e => !string.IsNullOrEmpty(e.SteuerHerkunft)))
                z.Add(new WirtZeile
                {
                    Schluessel = "STEUER_HERKUNFT",
                    Titel = MyResource.Resource.WIRT_ZEILE_STEUER_HERKUNFT,
                    Text = e => e.SteuerHerkunft
                });

            // ETAPPE E2 (Befund R6) — die Kohärenzzeilen des Laufs. Bis hierher standen
            // sie NUR auf der Windows-Seite (WirtschaftlichkeitSeiteGaben); Wort- und
            // Excelbericht kannten sie nicht, und die Rubrik ebenso wenig. Als Textzeile
            // dieses EINEN Zeilenkatalogs erreichen sie alle drei Ausgaben auf einmal —
            // dieselbe Sichtbarkeitsregel, dieselbe Reihenfolge, dieselben Marken.
            KohaerenzZeilen(menge, z);

            return z;
        }

        /// <summary>
        /// ETAPPE E2 (Befund R6) — je Kohärenzhinweis eine Textzeile, so viele, wie das
        /// Ergebnis mit den meisten Hinweisen führt. Die Zeilen tragen alle denselben
        /// Titel (<c>KOH_ZEILE_TITEL</c>); ein Ergebnis ohne so viele Hinweise lässt
        /// seine Zelle leer.
        /// </summary>
        private static void KohaerenzZeilen(IList<WirtschaftlichkeitErgebnis> menge,
                                            List<WirtZeile> z)
        {
            int hoechste = 0;
            if (menge != null)
                foreach (WirtschaftlichkeitErgebnis e in menge)
                    if (e != null && e.KohaerenzHinweise != null &&
                        e.KohaerenzHinweise.Count > hoechste) hoechste = e.KohaerenzHinweise.Count;
            if (hoechste == 0) return;

            string titel = MyResource.Resource.KOH_ZEILE_TITEL;
            for (int i = 0; i < hoechste; i++)
            {
                int index = i;
                z.Add(new WirtZeile
                {
                    Schluessel = "KOHAERENZ_" + (index + 1).ToString(CultureInfo.InvariantCulture),
                    Titel = titel,
                    Text = e => Zeilentext(e, index)
                });
            }
        }

        /// <summary>Der Text EINES Kohärenzhinweises eines Ergebnisses; leer, wenn es
        /// so viele nicht führt.</summary>
        private static string Zeilentext(WirtschaftlichkeitErgebnis e, int index)
        {
            if (e == null || e.KohaerenzHinweise == null ||
                index >= e.KohaerenzHinweise.Count) return "";
            return Kohaerenzmarke(e.KohaerenzHinweise[index]);
        }

        /// <summary>
        /// ETAPPE B6 — drei Schweren, drei Marken: die positive Nennung (Fall 1)
        /// bekommt den Haken, den die Anwendung sonst für „hat geklappt" nimmt.
        ///
        /// <para>Die Marken stehen <b>hier</b> und nicht in jeder Ausgabe, damit Rubrik,
        /// Wort- und Excelbericht und die Ergebnisseite dieselbe Zeile zeigen. Sie sind
        /// typografische Zeichen ohne Wortbestand und deshalb nicht lokalisiert —
        /// dieselbe dokumentierte Ausnahme wie bei den Einheitenzeichen.</para>
        /// </summary>
        public static string Kohaerenzmarke(KohaerenzHinweis h)
        {
            if (h == null) return "";
            string marke = string.Equals(h.Schwere, KohaerenzSchwere.WARNUNG,
                                         StringComparison.Ordinal) ? "⚠ "
                         : string.Equals(h.Schwere, KohaerenzSchwere.BESTAETIGUNG,
                                         StringComparison.Ordinal) ? "✓ " : "· ";
            return marke + h.Text;
        }

        private static WirtZeile Zahl(string schluessel, string titel,
                                      Func<WirtschaftlichkeitErgebnis, double?> wert)
        {
            return new WirtZeile { Schluessel = schluessel, Titel = titel, Wert = wert };
        }

        // =====================================================================
        // ETAPPE B7 — Bausteine der Rubrik (Konzept § 2.6)
        // =====================================================================

        /// <summary>Blocküberschrift ohne Werte.</summary>
        private static WirtZeile Kopf(string schluessel, string titel, string block)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Block = block,
                IstUeberschrift = true,
                Text = e => ""          // Überschriften tragen in jeder Spalte nichts
            };
        }

        /// <summary>
        /// Eine Zeile des zahlungswirksamen Blocks A. Sie wird in
        /// <paramref name="summanden"/> vermerkt — daraus entsteht die Summenzeile, und
        /// zwar aus genau denselben Zeilen, die darüber stehen. Eine von Hand
        /// nachgeführte Summe wäre die zweite Stelle, an der jemand eine neue Position
        /// vergessen kann.
        /// </summary>
        private static WirtZeile Erloes(List<WirtZeile> summanden, string schluessel, string titel,
                                        Func<WirtschaftlichkeitErgebnis, double?> wert,
                                        bool immerZeigen)
        {
            var z = new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = WirtZeile.BLOCK_A,
                ImmerZeigen = immerZeigen
            };
            summanden.Add(z);
            return z;
        }

        /// <summary>
        /// Eine Zeile des Ausweisblocks B. Sie steht NIE in der Summe — sie wird
        /// deshalb auch keiner Summandenliste beigelegt; die Trennung ist kein Flag,
        /// das jemand setzen oder vergessen könnte, sondern zwei verschiedene Wege in
        /// die Liste.
        /// </summary>
        private static WirtZeile Ausweis(string schluessel, string titel,
                                         Func<WirtschaftlichkeitErgebnis, double?> wert)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = WirtZeile.BLOCK_B
            };
        }

        /// <summary>Unter-/Herleitungszeile eines Blocks (Einzug 1, nie in der Summe).</summary>
        private static WirtZeile Unter(string schluessel, string titel,
                                       Func<WirtschaftlichkeitErgebnis, double?> wert, string block)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = block,
                Einzug = 1
            };
        }

        /// <summary>Unterzeile mit TEXT statt Zahl (Einzug 1, nie in der Summe) — der
        /// Weg, auf dem eine Herleitung neben ihrer Geldzeile steht, ohne dass Excel
        /// eine Textzelle in eine Wertspalte bekommt (<c>ExcelWert</c> bleibt leer).</summary>
        private static WirtZeile UnterText(string schluessel, string titel,
                                           Func<WirtschaftlichkeitErgebnis, string> text, string block)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Text = text,
                Block = block,
                Einzug = 1
            };
        }

        // =====================================================================
        // AUFTRAG U6 — die Komponentengliederung INNERHALB eines Blocks
        // (Konzept § 2.6, Anwenderentscheid Q15 vom 22.09.2026)
        // =====================================================================

        /// <summary>Gibt der Zeile ihren Anlagenbezug und legt sie in die Sammlung des
        /// Blocks. Der Anlagenbezug entsteht damit an GENAU EINER Stelle je Zeile —
        /// nicht in der Ausgabe, die sie zeichnet.</summary>
        private static WirtZeile Zu(List<WirtZeile> ziel, WirtZeile zeile, string komponente)
        {
            if (zeile == null) return null;
            zeile.Komponente = komponente;
            if (ziel != null) ziel.Add(zeile);
            return zeile;
        }

        /// <summary>Sprachneutrale Kennung einer Komponente für den Zeilenschlüssel;
        /// „projektweit" ist die leere Kennung und braucht deshalb einen Namen.</summary>
        private static string Kennform(string komponente)
        {
            return string.IsNullOrEmpty(komponente) ? "PROJEKTWEIT" : komponente;
        }

        /// <summary>
        /// Schreibt die Zeilen eines Blocks nach Komponenten geordnet in den Katalog:
        /// je Komponente ein Kopf, ihre Zeilen und eine Zwischensumme; „projektweit"
        /// zuletzt. Eine Komponente ohne Zeilen bekommt keinen Kopf.
        /// </summary>
        /// <param name="summanden">Die Summandenliste des Blocks — aus ihr entsteht die
        /// Zwischensumme je Komponente. <c>null</c> = der Block kennt keine Summanden
        /// (Block B); dann entscheidet <paramref name="teilsumme"/>.</param>
        /// <param name="teilsumme">Baut die Abschlusszeile einer Komponente selbst;
        /// <c>null</c> = die gewöhnliche Zwischensumme über <paramref name="summanden"/>.</param>
        private static void Bloecke(List<WirtZeile> ziel, List<WirtZeile> zeilen,
                                    List<WirtZeile> summanden, string block,
                                    Func<string, WirtZeile> teilsumme)
        {
            if (ziel == null || zeilen == null) return;
            foreach (string komponente in WirtZeile.Komponentenfolge)
            {
                var imBlock = new List<WirtZeile>();
                foreach (WirtZeile x in zeilen)
                    if (x != null && string.Equals(x.Komponente, komponente, StringComparison.Ordinal))
                        imBlock.Add(x);
                if (imBlock.Count == 0) continue;

                ziel.Add(Komponentenkopf(komponente, block));
                ziel.AddRange(imBlock);

                WirtZeile abschluss = teilsumme != null
                                    ? teilsumme(komponente)
                                    : Teilsummenzeile(komponente, block, summanden);
                if (abschluss != null) ziel.Add(abschluss);
            }
        }

        /// <summary>Der Kopf eines Komponentenblocks — „{0}" aus
        /// <c>WIRT_ERL_KOMPONENTE</c>, für „projektweit" der Name aus
        /// <c>WIRT_ERL_PROJEKTWEIT</c>.</summary>
        private static WirtZeile Komponentenkopf(string komponente, string block)
        {
            WirtZeile kopf = Kopf("ERL_" + block + "_K_" + Kennform(komponente),
                                  string.Format(MyResource.Resource.WIRT_ERL_KOMPONENTE,
                                                WirtZeile.Komponentenname(komponente)),
                                  block);
            kopf.Komponente = komponente;
            kopf.IstKomponentenkopf = true;
            return kopf;
        }

        /// <summary>Die Zwischensumme EINER Komponente — sie summiert genau die
        /// Summanden dieser Komponente, also dieselben Zeilen, die darüber stehen.
        /// <c>null</c> = die Komponente führt keinen Summanden (etwa ein Block aus
        /// lauter Unterzeilen); dann steht dort keine Summe.</summary>
        private static WirtZeile Teilsummenzeile(string komponente, string block,
                                                 List<WirtZeile> summanden)
        {
            if (summanden == null) return null;
            var teil = new List<WirtZeile>();
            foreach (WirtZeile s in summanden)
                if (s != null && string.Equals(s.Komponente, komponente, StringComparison.Ordinal))
                    teil.Add(s);
            if (teil.Count == 0) return null;

            return new WirtZeile
            {
                Schluessel = "ERL_" + block + "_TEIL_" + Kennform(komponente),
                Titel = string.Format(MyResource.Resource.WIRT_ERL_TEILSUMME,
                                      WirtZeile.Komponentenname(komponente)),
                Block = block,
                Komponente = komponente,
                IstSumme = true,
                IstTeilsumme = true,
                Wert = e => Teilsumme(teil, e)
            };
        }

        /// <summary>Summe der Werte einer Zeilenliste für EIN Ergebnis.</summary>
        private static double? Teilsumme(List<WirtZeile> summanden,
                                         WirtschaftlichkeitErgebnis e)
        {
            if (e == null || summanden == null) return null;
            double summe = 0;
            foreach (WirtZeile s in summanden)
            {
                double? w = s == null || s.Wert == null ? null : s.Wert(e);
                if (w.HasValue) summe += w.Value;
            }
            return (double?)summe;
        }

        /// <summary>Der Abschluss eines Komponentenblocks in Block B: der WIRKSAME
        /// Betrag der Anlage (Arbeitsanteil abzüglich der entgangenen § 9b-Entlastung),
        /// keine Summe über den Block — Block B wird nicht summiert.</summary>
        private static WirtZeile Wirksamzeile(string komponente)
        {
            string k = komponente;      // Fangkopie für den Abschluss
            return new WirtZeile
            {
                Schluessel = "ERL_B1_EFFEKTIV_" + Kennform(k),
                Titel = string.Format(MyResource.Resource.WIRT_ERL_B1_WIRKSAM,
                                      WirtZeile.Komponentenname(k)),
                Block = WirtZeile.BLOCK_B,
                Komponente = k,
                IstSumme = true,
                IstTeilsumme = true,
                Wert = e =>
                {
                    VermiedenAnlageNachweis n = Vermiedenzeile(e, k);
                    return n == null ? (double?)null : (double?)n.WirksamEur;
                }
            };
        }

        /// <summary>Die Komponenten, für die der Lauf eine Aufteilung der vermiedenen
        /// Stromkosten trägt — in der Reihenfolge der Rubrik. „projektweit" ist nie
        /// darunter: Dort steht allein der Leistungsanteil (Q15).</summary>
        private static List<string> VermiedenKomponenten(IList<WirtschaftlichkeitErgebnis> menge)
        {
            var gefunden = new List<string>();
            if (menge == null) return gefunden;
            foreach (string k in WirtZeile.Komponentenfolge)
            {
                if (string.Equals(k, WirtZeile.KOMPONENTE_PROJEKTWEIT, StringComparison.Ordinal))
                    continue;
                foreach (WirtschaftlichkeitErgebnis e in menge)
                    if (Vermiedenzeile(e, k) != null) { gefunden.Add(k); break; }
            }
            return gefunden;
        }

        /// <summary>
        /// Der vermiedene Bezug der Photovoltaik zum Flat-Preis [€/a] — <c>null</c>, wo die
        /// Aufteilung der vermiedenen Kosten einen PV-Anteil trägt: Dann ersetzt der
        /// Anteil (Rollentarif, Konzept § 6.3 Nr. 32) die Zeile. Der Wert selbst bleibt
        /// gerechnet und gespeichert; nur die Rubrik zeigt ihn dann nicht.
        /// </summary>
        private static double? PvVermiedenFlat(WirtschaftlichkeitErgebnis e)
        {
            if (e == null) return null;
            return Vermiedenzeile(e, WirtZeile.KOMPONENTE_PV) != null ? null : e.PvVermiedenerBezug;
        }

        /// <summary>Die Aufteilungszeile EINER Komponente; <c>null</c> = dieses Ergebnis
        /// führt sie nicht (Anzeige „—").</summary>
        private static VermiedenAnlageNachweis Vermiedenzeile(WirtschaftlichkeitErgebnis e,
                                                              string komponente)
        {
            if (e == null || e.VermiedenJeAnlage == null) return null;
            foreach (VermiedenAnlageNachweis n in e.VermiedenJeAnlage)
                if (n != null && string.Equals(n.Komponente, komponente, StringComparison.Ordinal))
                    return n;
            return null;
        }

        /// <summary>Arbeitsanteil (<paramref name="abzug"/> = false) bzw. die entgangene
        /// § 9b-Entlastung als ABZUG (negativ) einer Komponente [€/a].</summary>
        private static double? VermiedenBetrag(WirtschaftlichkeitErgebnis e, string komponente,
                                               bool abzug)
        {
            VermiedenAnlageNachweis n = Vermiedenzeile(e, komponente);
            if (n == null) return null;
            return abzug ? (double?)(-n.Entlastung9bEur) : (double?)n.ArbeitEur;
        }

        /// <summary>
        /// Die Herleitung der Aufteilung als Klartext: Menge, Anteil und — sobald mehr
        /// als eine Anlage beteiligt ist — der Vermerk, dass der Schlüssel eine
        /// Näherung ist (Entscheid A12). Leer = keine Aufteilung; dann entfällt die
        /// Zeile wie jede andere ohne Wert.
        /// </summary>
        private static string Vermiedenherleitung(WirtschaftlichkeitErgebnis e, string komponente)
        {
            VermiedenAnlageNachweis n = Vermiedenzeile(e, komponente);
            if (n == null) return "";
            CultureInfo kultur = BerichtTexte.Kultur;
            string text = string.Format(kultur, MyResource.Resource.WIRT_ERL_B1_ANTEIL,
                                        n.MengeMWh.ToString("N1", kultur),
                                        (n.Anteil * 100.0).ToString("N1", kultur));
            if (n.IstNaeherung)
                text += " — " + MyResource.Resource.WIRT_ERL_B1_NAEHERUNG;
            return text;
        }

        /// <summary>
        /// Der Anteil des KWK-Zuschlags, der auf die EINSPEISUNG (§ 7 Abs. 1 KWKG) bzw.
        /// auf den EIGENSTROM (§ 7 Abs. 2 KWKG) entfällt [€/a], über alle Module des
        /// Laufs. <c>null</c> = kein Modulnachweis (geladener statt frisch gerechneter
        /// Stand) — dann entfällt die Zeile wie jede andere ohne Wert.
        ///
        /// <para>Gerechnet wird aus Menge × Satz je Modul, also aus denselben Größen,
        /// mit denen der KWKG-Rechner den Jahresbetrag gebildet hat. Der Eigenanteil
        /// eines Moduls ohne Tatbestand nach § 6 Abs. 3 ist dort 0 und bleibt es
        /// hier — der Satz steht dann auf 0.</para>
        /// </summary>
        private static double? KwkgAnteil(WirtschaftlichkeitErgebnis e, bool einspeisung)
        {
            if (e == null || e.KwkgModule == null || e.KwkgModule.Count == 0) return null;
            double summe = 0;
            foreach (KwkgModulNachweis n in e.KwkgModule)
            {
                if (n == null) continue;
                summe += einspeisung
                    ? n.EinspeisungMWh * 1000.0 * n.SatzEinspeisungCt / 100.0
                    : n.EigenMWh * 1000.0 * n.SatzEigenCt / 100.0;
            }
            return (double?)summe;
        }

        /// <summary>
        /// AUFTRAG #351 (U23) — der ANGESETZTE Satz der Einspeisung (§ 7 Abs. 1) bzw.
        /// des Eigenstroms (§ 7 Abs. 2) samt Herkunft, als Klartext einer Unterzeile.
        ///
        /// <para>Leer = kein Modulnachweis; dann entfällt die Zeile wie jede andere ohne
        /// Wert. Führt der Lauf MEHRERE Module, steht je Modul ein Abschnitt
        /// „Bezeichner: Satz · Herkunft" — der Bezeichner ist ein Datenwert, Doppelpunkt
        /// und Trennzeichen sind Satzzeichen (Drei-Schichten-Regel).</para>
        ///
        /// <para>Die Stellenzahl und der Vergleich mit dem Vorschlag stehen in
        /// <see cref="KwkgSatzHerkunft"/> — einmal, nicht je Ausgabeweg.</para>
        /// </summary>
        private static string KwkgSatzzeile(WirtschaftlichkeitErgebnis e, bool einspeisung)
        {
            if (e == null || e.KwkgModule == null || e.KwkgModule.Count == 0) return "";
            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;
            bool mehrere = e.KwkgModule.Count > 1;
            var teile = new List<string>();
            foreach (KwkgModulNachweis n in e.KwkgModule)
            {
                if (n == null) continue;
                string s = einspeisung
                    ? KwkgSatzHerkunft.SatzUndHerkunft(n.SatzEinspeisungCt, n.VorschlagEinspeisungCt, kultur)
                    : KwkgSatzHerkunft.SatzUndHerkunft(n.SatzEigenCt, n.VorschlagEigenCt, kultur);
                teile.Add(mehrere && !string.IsNullOrEmpty(n.Bezeichner)
                          ? n.Bezeichner + ": " + s : s);
            }
            return teile.Count == 0 ? "" : string.Join(" · ", teile.ToArray());
        }

        // =====================================================================
        // AUFTRAG U7 — die Energiesteuer in zwei Beträgen
        // =====================================================================

        /// <summary>
        /// Darf die Rubrik die Energiesteuer aufteilen? Nur, wenn KEIN Stand der
        /// Gruppe einen Betrag führt, dessen Aufteilung er nicht kennt.
        ///
        /// <para><b>Die Frage gilt der GRUPPE, nicht dem einzelnen Stand.</b> Word,
        /// Excel und Reiter bauen EINE Tabelle über alle Spalten; zeigte sie für die
        /// eine Spalte zwei Zeilen und für die andere eine, wären es zwei Tabellen.
        /// Und der zweite Fall ist der gefährliche: Ein vor U7 gebuchter Stand trüge
        /// in beiden Paragrafenzeilen 0, während seine Ergebnisspalte einen Betrag
        /// führt — die Summe des Blocks A fiele für ihn um genau diesen Betrag
        /// kleiner aus. Ein solcher Stand zieht deshalb die ganze Gruppe auf die
        /// Gesamtzeile zurück.</para>
        /// </summary>
        private static bool SteuerAufgeteilt(IList<WirtschaftlichkeitErgebnis> menge)
        {
            foreach (WirtschaftlichkeitErgebnis e in menge)
                if (e != null && !e.EnergiesteuerAufgeteilt && e.EnergiesteuerJahr1 != 0)
                    return false;
            return true;
        }

        /// <summary>
        /// AUFTRAG U7 — die Herleitung EINER der beiden Steuerzeilen als Klartext
        /// („4.797,2 MWh × 4,42 €/MWh = 21.203,4 €/a"), aus dem Nachweis des Laufs.
        ///
        /// <para>Leer = dieser Stand hat zu der Vorschrift nichts gerechnet; dann
        /// entfällt die Zeile wie jede andere ohne Wert. Führt der Lauf MEHRERE
        /// Anlagen unter derselben Vorschrift, steht je Anlage ein Abschnitt
        /// „Bezeichner: Menge × Satz = Betrag" — der Bezeichner ist ein Datenwert,
        /// Doppelpunkt und Trennzeichen sind Satzzeichen (Drei-Schichten-Regel).</para>
        ///
        /// <para>Beim § 54 schließt der SOCKELBETRAG die Kette ab: Er fällt einmal je
        /// Kalenderjahr an, nicht je Anlage, und ohne ihn ließe sich der Betrag der
        /// Zeile aus den Anlagenposten nicht nachrechnen.</para>
        /// </summary>
        public static string Energiesteuerherleitung(WirtschaftlichkeitErgebnis e, bool ist54)
        {
            if (e == null || e.EnergiesteuerNachweise == null) return "";
            CultureInfo kultur = BerichtTexte.Kultur;

            int zaehler = 0;
            foreach (EnergiesteuerNachweis n in e.EnergiesteuerNachweise)
                if (n != null && n.Ist54 == ist54) zaehler++;
            if (zaehler == 0) return "";

            bool mehrere = zaehler > 1;
            var teile = new List<string>();
            foreach (EnergiesteuerNachweis n in e.EnergiesteuerNachweise)
            {
                if (n == null || n.Ist54 != ist54) continue;
                string s = string.Format(kultur, MyResource.Resource.WIRT_ERL_ENERGIEST_HERLEITUNG,
                                         n.Menge.ToString("N1", kultur), Mengeneinheit(n.Einheit),
                                         n.SatzEur.ToString("N2", kultur),
                                         n.BetragEur.ToString("N2", kultur));
                teile.Add(mehrere && !string.IsNullOrEmpty(n.Anlage) ? n.Anlage + ": " + s : s);
            }
            if (ist54 && e.Energiesteuer54SockelJahr1 > 0)
                teile.Add(string.Format(kultur, MyResource.Resource.WIRT_ERL_ENERGIEST_SOCKEL,
                                        e.Energiesteuer54SockelJahr1.ToString("N0", kultur)));
            return string.Join(" · ", teile.ToArray());
        }

        // =====================================================================
        // AUFTRAG 9d — die Gründe je Position (Konzept § 6.3, Punkt B7-4)
        //
        // DIE REGEL DIESES ABSCHNITTS: Ein Grund BENENNT, was der Rechenweg
        // festgestellt hat. Wo er nichts festgestellt hat, bleibt die Zeile LEER und
        // entfällt über Sichtbare — eine erfundene Begründung wäre schlimmer als
        // keine, weil sie wie eine Feststellung aussieht.
        //
        // Die Zuordnung Position → Text steht HIER, an einem Ort; Ergebnisseite,
        // Wort- und Excelbericht zeigen dieselbe Zeile, weil sie denselben
        // Zeilenkatalog lesen. Als TEXTzeile erreicht der Grund auch Excel — der
        // Zelltext einer Wertspalte tut das nicht, die bleibt numerisch.
        // =====================================================================

        /// <summary>
        /// Der vom Steuerrechner festgestellte Grund zu einer Position
        /// (<see cref="SteuerPosition"/>); leer = nichts festgestellt oder ein vor
        /// 9d gebuchter Stand.
        /// </summary>
        private static string Positionsgrund(WirtschaftlichkeitErgebnis e, string position)
        {
            if (e == null || e.PositionsGruende == null) return "";
            string text;
            return e.PositionsGruende.TryGetValue(position, out text) && !string.IsNullOrEmpty(text)
                 ? text : "";
        }

        /// <summary>
        /// Die Zeile unter einer der beiden Steuerpositionen: die HERLEITUNG, wenn es
        /// etwas zu rechnen gab, sonst der GRUND der Null.
        ///
        /// <para>Der Rückfall „kein Brennstoff unter dieser Vorschrift" greift nur
        /// bei einem Lauf, der die Aufteilung selbst gerechnet hat — nur er hat
        /// nachgesehen. Für einen gebuchten Stand ohne Aufteilung bleibt die Zeile
        /// leer, statt eine Feststellung zu behaupten, die niemand getroffen hat.</para>
        /// </summary>
        private static string Energiesteuerzeile(WirtschaftlichkeitErgebnis e, bool ist54)
        {
            if (e == null) return "";

            string herleitung = Energiesteuerherleitung(e, ist54);
            if (!string.IsNullOrEmpty(herleitung)) return herleitung;

            string grund = Positionsgrund(e, ist54 ? SteuerPosition.ENERGIEST_54
                                                   : SteuerPosition.ENERGIEST_53);
            if (!string.IsNullOrEmpty(grund)) return grund;

            double betrag = ist54 ? e.Energiesteuer54Jahr1 : e.Energiesteuer53Jahr1;
            if (!e.EnergiesteuerAufgeteilt || betrag != 0) return "";

            return ist54 ? MyResource.Resource.WIRT_ERL_GRUND_KEIN_KESSELBRENNSTOFF
                         : MyResource.Resource.WIRT_ERL_GRUND_KEIN_BHKW_BRENNSTOFF;
        }

        /// <summary>
        /// Warum der KWK-Zuschlag 0 ist — aus dem MODULNACHWEIS des Laufs, also aus
        /// den Größen, mit denen der KWKG-Rechner gerechnet hat: keine
        /// zuschlagsfähige Menge, kein gepflegter Satz, erschöpftes Kontingent.
        ///
        /// <para>Ohne Modulnachweis (Ersatzweg oder gebuchter Stand vor B7P) bleibt
        /// die Zeile leer: Dann liegt keine Feststellung vor, sondern nur die
        /// Bedingung — und die steht bereits im Zelltext der Geldzeile.</para>
        /// </summary>
        private static string KwkgGrund(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || e.KwkgErloesJahr1 != 0) return "";
            if (e.KwkgModule == null || e.KwkgModule.Count == 0) return "";

            double menge = 0, saetze = 0;
            bool alleErschoepft = true;
            foreach (KwkgModulNachweis n in e.KwkgModule)
            {
                if (n == null) continue;
                menge += n.EigenMWh + n.EinspeisungMWh;
                saetze += n.SatzEigenCt + n.SatzEinspeisungCt;
                if (n.ErschoepftAbJahr <= 0 || n.ErschoepftAbJahr > 1) alleErschoepft = false;
            }

            if (menge <= 0) return MyResource.Resource.WIRT_ERL_GRUND_KWKG_MENGE;
            if (saetze <= 0) return MyResource.Resource.WIRT_ERL_GRUND_KWKG_SATZ;
            if (alleErschoepft) return MyResource.Resource.WIRT_ERL_GRUND_KWKG_KONTINGENT;
            return "";
        }

        /// <summary>
        /// Warum der Einspeiseerlös 0 ist. Die beiden Fälle sind verschieden und
        /// werden vom Modulnachweis unterschieden: Wurde gar nichts eingespeist, oder
        /// wurde eingespeist, ohne dass eine Vergütung gepflegt ist?
        /// </summary>
        private static string EinspeisungGrund(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || e.EinspeiseerloesJahr != 0) return "";
            if (e.KwkgModule == null || e.KwkgModule.Count == 0) return "";

            double eingespeist = 0;
            foreach (KwkgModulNachweis n in e.KwkgModule)
                if (n != null) eingespeist += n.EinspeisungMWh;

            return eingespeist > 0 ? MyResource.Resource.WIRT_ERL_GRUND_OHNE_VERGUETUNG
                                   : MyResource.Resource.WIRT_ERL_GRUND_KEINE_EINSPEISUNG;
        }

        /// <summary>
        /// Die MENGENeinheit zur gesetzlichen Einheit eines Satzes: „EUR/MWh" bemisst
        /// MWh, „EUR/1000l" Tausend Liter. Einheitenzeichen sind — wie überall in
        /// dieser Klasse — nicht lokalisiert; leer = unbekannte Einheit, dann nennt
        /// die Herleitung nur die Zahl. ETAPPE E7c3: öffentlich — die Energiesteuer-
        /// Vorschau der Überlagerung nennt dieselbe Einheit
        /// (<see cref="EnergiesteuerVorschauZeile.SatzEinheit"/>).
        /// </summary>
        public static string Mengeneinheit(string gesetzlich)
        {
            if (string.Equals(gesetzlich, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.Ordinal))
                return "MWh";
            if (string.Equals(gesetzlich, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.Ordinal))
                return "1000 l";
            if (string.Equals(gesetzlich, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.Ordinal))
                return "1000 kg";
            if (string.Equals(gesetzlich, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.Ordinal))
                return "GJ";
            if (string.Equals(gesetzlich, DbWerte.GESETZ_EINHEIT_EUR_T, StringComparison.Ordinal))
                return "t";
            return "";
        }

        /// <summary>
        /// ETAPPE B7 — die Anlagen der Gruppe in Ausgabereihenfolge (je Name einmal).
        /// Ohne Modulnachweis (geladener Stand) leer — dann entfällt die
        /// Aufschlüsselung, und die Energiekostenzeile steht allein wie zuvor.
        /// </summary>
        private static List<string> Anlagennamen(IList<WirtschaftlichkeitErgebnis> menge)
        {
            var namen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis e in menge)
            {
                if (e == null || e.EnergiekostenJeAnlage == null) continue;
                foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
                    if (n != null && !namen.Contains(n.Anlage)) namen.Add(n.Anlage);
            }
            return namen;
        }

        /// <summary>Die Energiekosten EINER Anlage [€/a]; <c>null</c> = dieses Projekt
        /// führt die Anlage nicht (Anzeige „—").</summary>
        private static double? AnlageKosten(WirtschaftlichkeitErgebnis e, string anlage)
        {
            if (e == null || e.EnergiekostenJeAnlage == null) return null;
            double summe = 0;
            bool gefunden = false;
            foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
                if (n != null && n.Anlage == anlage) { summe += n.KostenEur; gefunden = true; }
            return gefunden ? (double?)summe : null;
        }

        /// <summary>
        /// Sprachneutrale ASCII-Form eines Anlagennamens für den Zeilenschlüssel — der
        /// Schlüssel ist eingefroren und darf keinen Anzeigetext tragen (Drei-Schichten-
        /// Regel). Alles außerhalb von A–Z, a–z und 0–9 wird zum Unterstrich.
        /// </summary>
        private static string Schluesselform(string name)
        {
            if (string.IsNullOrEmpty(name)) return "X";
            var b = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
                b.Append((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') ? char.ToUpperInvariant(c) : '_');
            return b.ToString();
        }

        /// <summary>
        /// ETAPPE B7 — die Herleitung EINER Anlagenzeile als Klartext
        /// („4.200,00 L × 0,9500 €/L = 3.990,00 €/a"). Sie steht im Bericht als
        /// Untertabelle und im Reiter als Kurztext; leer, wenn die Anlage in diesem
        /// Ergebnis nicht vorkommt.
        /// </summary>
        public static string AnlageHerleitung(WirtschaftlichkeitErgebnis e, string anlage,
                                              System.Globalization.CultureInfo kultur)
        {
            if (e == null || e.EnergiekostenJeAnlage == null) return "";
            if (kultur == null) kultur = System.Globalization.CultureInfo.CurrentCulture;
            foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
            {
                if (n == null || n.Anlage != anlage) continue;
                return n.MengeAbrechnung.ToString("N2", kultur) + " " + n.Einheit + " × " +
                       n.PreisJeEinheit.ToString("N4", kultur) + " €/" + n.Einheit + " = " +
                       n.KostenEur.ToString("N2", kultur) + " €/a (" + n.Traeger + ")";
            }
            return "";
        }

        /// <summary>
        /// ETAPPE B7 — DIE EINE Sichtbarkeitsregel für Reiter, Word und Excel.
        ///
        /// <para><b>Der Befund, der sie erzwang.</b> Bis B7 filterte der Ergebnisreiter
        /// die Zeilenliste ein zweites Mal (über die gerade gewählten Spalten), Excel
        /// ein drittes Mal (über den Szenarioblock), Word gar nicht. Seite und Bericht
        /// konnten damit verschiedene Tabellen zeigen — genau das, was
        /// <see cref="Kennzahlen"/> seit E7 verhindern soll. Die Regel steht seither
        /// hier, und die drei Ausgaben rufen sie, statt je eine eigene zu führen.</para>
        ///
        /// <para>Über die Sichtbarkeit entscheidet die MENGE der Gruppe, nie ein
        /// einzelnes Ergebnis: Eine Zeile, die nur in der Variante einen Wert trägt,
        /// gehört auch in die Stammspalte — dort steht dann „—", und das ist die
        /// Auskunft.</para>
        /// </summary>
        public static List<WirtZeile> Sichtbare(IList<WirtZeile> zeilen,
                                                IList<WirtschaftlichkeitErgebnis> menge)
        {
            var sichtbar = new List<WirtZeile>();
            if (zeilen == null) return sichtbar;

            foreach (WirtZeile z in zeilen)
            {
                if (z == null) continue;
                if (z.IstUeberschrift || z.IstSumme || z.ImmerZeigen) { sichtbar.Add(z); continue; }
                if (menge == null) continue;
                bool hat = false;
                foreach (WirtschaftlichkeitErgebnis e in menge)
                {
                    if (e == null) continue;
                    if (z.IstText) { if (!string.IsNullOrEmpty(z.Text(e))) { hat = true; break; } }
                    else if (z.IstReferenz(e) && z.StammAnzeige != null) { hat = true; break; }
                    else if (z.Wert != null && z.Wert(e).HasValue) { hat = true; break; }
                }
                if (hat) sichtbar.Add(z);
            }

            // Eine Blocküberschrift ohne Zeilen und eine Summe ohne Summanden sind
            // Behauptungen — beide fallen weg. Geprüft wird nach dem Filtern, weil erst
            // dann feststeht, was im Block übrig blieb.
            //
            // AUFTRAG U6 — jetzt auf ZWEI Ebenen: Der Blockkopf („A — zahlungswirksam")
            // bleibt, solange IRGENDEINE Zeile des Blocks folgt; ein Komponentenkopf
            // („Blockheizkraftwerk") nur, solange eine Zeile SEINER Komponente folgt.
            // Ebenso die Summen: die Blocksumme über alles, die Zwischensumme je
            // Komponente. Ohne diese Unterscheidung fiele der Blockkopf weg, sobald
            // gleich hinter ihm ein Komponentenkopf steht — also immer.
            var ergebnis = new List<WirtZeile>();
            for (int i = 0; i < sichtbar.Count; i++)
            {
                WirtZeile z = sichtbar[i];
                if (z.IstUeberschrift && !FolgtZeile(sichtbar, i, z)) continue;
                if (z.IstTeilsumme && !StehtZeile(ergebnis, z.Block, z.Komponente, true)) continue;
                if (z.IstSumme && !z.IstTeilsumme && z.Block == WirtZeile.BLOCK_A &&
                    !StehtZeile(ergebnis, WirtZeile.BLOCK_A, null, false)) continue;
                ergebnis.Add(z);
            }
            return ergebnis;
        }

        /// <summary>true, wenn HINTER der Überschrift an <paramref name="ab"/> noch
        /// eine gewöhnliche Zeile folgt, die zu ihr gehört: bei einem BLOCKkopf jede
        /// Zeile seines Blocks (Komponentenköpfe dazwischen zählen nicht als Ende), bei
        /// einem KOMPONENTENkopf nur eine Zeile seiner Komponente.</summary>
        private static bool FolgtZeile(IList<WirtZeile> zeilen, int ab, WirtZeile kopf)
        {
            if (kopf == null) return false;
            for (int i = ab + 1; i < zeilen.Count; i++)
            {
                WirtZeile z = zeilen[i];
                if (z == null) continue;
                if (z.IstUeberschrift)
                {
                    // Ein Komponentenkopf beendet nur einen Komponentenblock; ein
                    // Blockkopf beendet beide Ebenen.
                    if (!z.IstKomponentenkopf || kopf.IstKomponentenkopf) return false;
                    continue;
                }
                if (z.Block != kopf.Block || z.IstSumme) continue;
                if (kopf.IstKomponentenkopf &&
                    !string.Equals(z.Komponente, kopf.Komponente, StringComparison.Ordinal))
                    continue;
                return true;
            }
            return false;
        }

        /// <summary>true, wenn in der bereits gefüllten Liste eine gewöhnliche Zeile
        /// des Blocks steht — die Bedingung der Summenzeile. Mit
        /// <paramref name="jeKomponente"/> zählt nur, was zur Komponente gehört.</summary>
        private static bool StehtZeile(IList<WirtZeile> zeilen, string block,
                                       string komponente, bool jeKomponente)
        {
            foreach (WirtZeile z in zeilen)
            {
                if (z == null || z.Block != block || z.IstUeberschrift || z.IstSumme) continue;
                if (jeKomponente &&
                    !string.Equals(z.Komponente, komponente, StringComparison.Ordinal)) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Der Speicherkontext je PROJEKT der Gruppe (VF-1) — einmal gelesen, nicht je
        /// Zelle: Die Zeilenliste entsteht bei jedem Szenariowechsel neu, und derselbe
        /// Text stünde sonst mehrfach in der Datenbank nachgefragt.
        /// </summary>
        private static Dictionary<int, string> Speicherkontexte(
            IList<WirtschaftlichkeitErgebnis> menge)
        {
            var karte = new Dictionary<int, string>();
            foreach (WirtschaftlichkeitErgebnis e in menge)
            {
                if (e == null || e.IdProjekt <= 0 || karte.ContainsKey(e.IdProjekt)) continue;
                string text = SpeicherAnzeigeCtrl.SpeicherKontextText(e.IdProjekt);
                if (!string.IsNullOrEmpty(text)) karte[e.IdProjekt] = text;
            }
            return karte;
        }

        /// <summary>
        /// Klartext der Vermarktungsform für ANDERE Leser — die Erklärzeile des
        /// Reiters Ertrag/Bonus nennt sie neben der Herkunft (Konzept § 2.16).
        ///
        /// <para>Eine zweite Übersetzung derselben vier Steuerwerte liefe über kurz
        /// oder lang auseinander; deshalb geht auch der Reiter durch diese eine.</para>
        /// </summary>
        public static string PvFormAnzeige(string form)
        {
            return PvFormText(form);
        }

        /// <summary>Klartext der Vermarktungsform (Persistenzwert ist ASCII, P6).</summary>
        private static string PvFormText(string form)
        {
            if (form == DbWerte.PV_VERMARKTUNG_EV) return MyResource.Resource.WIRT_ZEILE_PV_FORM_EV;
            if (form == DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE) return MyResource.Resource.WIRT_ZEILE_PV_FORM_MP;
            if (form == DbWerte.PV_VERMARKTUNG_SONSTIGE_DV) return MyResource.Resource.WIRT_ZEILE_PV_FORM_DV;
            if (form == DbWerte.PV_VERMARKTUNG_KEINE) return MyResource.Resource.WIRT_ZEILE_PV_FORM_KEINE;
            return form;
        }

        /// <summary>
        /// Die HERKUNFT der PV-Vergütung dieses Stands (Konzept § 2.16): „eigene Werte"
        /// oder „übernommen von ‹Stamm›".
        ///
        /// <para>Ein Stammprojekt führt immer eigene Werte; ein vor Schemaschritt 93
        /// gebuchter Stand ebenso — sein Nachweisumschlag kennt das Feld nicht und liest
        /// sich als <c>false</c>, und genau das traf damals zu.</para>
        ///
        /// <para>Fehlt bei einer Übernahme der NAME des Stamms (gelöschtes Projekt,
        /// älterer Umschlag mit gesetztem Kennzeichen), steht die Zeile ohne ihn da —
        /// eine leere Anführung wäre eine Behauptung über ein Projekt, das niemand
        /// benennen kann.</para>
        /// </summary>
        private static string PvHerkunftText(WirtschaftlichkeitErgebnis e)
        {
            if (e == null) return null;
            if (!e.PvVerguetungUebernommen) return MyResource.Resource.WIRT_ZEILE_PV_HERK_EIGEN;
            if (string.IsNullOrEmpty(e.PvVerguetungQuelle))
                return MyResource.Resource.WIRT_ZEILE_PV_HERK_STAMM_OHNE_NAME;
            return string.Format(BerichtTexte.Kultur,
                                 MyResource.Resource.WIRT_ZEILE_PV_HERK_STAMM,
                                 e.PvVerguetungQuelle);
        }

        private static bool Irgendein(IList<WirtschaftlichkeitErgebnis> menge,
                                      Func<WirtschaftlichkeitErgebnis, bool> bedingung)
        {
            foreach (WirtschaftlichkeitErgebnis e in menge)
                if (e != null && bedingung(e)) return true;
            return false;
        }

        // =====================================================================
        // Anzeigetexte der Steuerwerte aus Etappe E3
        // =====================================================================

        /// <summary>Anzeigetext einer Kostenart (<c>DbWerte.KOSTENART_*</c>).</summary>
        public static string KostenartText(string steuerwert)
        {
            if (string.Equals(steuerwert, DbWerte.KOSTENART_KAPITALGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_KAPITALGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_BETRIEBSGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_BETRIEBSGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_BEDARFSGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_BEDARFSGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_SONSTIGE, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_SONSTIGE;
            return MyResource.Resource.KOSTENART_OHNE;
        }

        /// <summary>Reihenfolge der Kostenarten im Bericht — nach VDI 2067.</summary>
        public static readonly string[] Kostenarten =
        {
            DbWerte.KOSTENART_KAPITALGEBUNDEN,
            DbWerte.KOSTENART_BEDARFSGEBUNDEN,
            DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
            DbWerte.KOSTENART_SONSTIGE,
            ""                                   // nicht eingeordnet
        };

        /// <summary>
        /// Anzeigetext einer Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>) in diesem Gewerk —
        /// die Spalte „Bemessung" der Betriebskostentabelle in Wort- und Tabellenbericht.
        ///
        /// <para><b>ETAPPE E8c (E8b‑Q2): EINE Wahrheit.</b> Die Methode führte eine eigene
        /// Liste und kannte 4 von 17 Arten; jede andere stand im Bericht als „fester
        /// Betrag" — auch neben einer Menge-×-Satz-Formel der Formelmappe. Der Text kommt
        /// jetzt aus dem <see cref="BemessungKatalog"/> (Ressourcen <c>BM_*</c>, beide
        /// Sprachen), derselben Quelle wie Kostendialog und Kostenseite, samt der
        /// gewerkeigenen Beschriftung („je Liter" am Pufferspeicher, „je kW elektr.
        /// Leistung" am BHKW). Der Wächter <c>BemessungstexteAlleArtenTests</c> hält jede
        /// Konstante <c>DbWerte.BEMESSUNG_*</c> gegen den Katalog.</para>
        ///
        /// <para>„fester Betrag" steht nur noch an einer festen Position: leerer Steuerwert,
        /// BETRAG — und ein unbekannter Steuerwert, weil der Rechenweg ihn wie BETRAG
        /// rechnet (<see cref="BetriebskostenCtrl.Betrag"/>: „nie stillschweigend 0").</para>
        /// </summary>
        /// <param name="komponente"><c>Tab_KostenKomponente.ID</c> der Position; 0 =
        /// unbekannt, dann der allgemeine Name der Art.</param>
        public static string BemessungText(string steuerwert, int komponente = 0)
        {
            if (string.IsNullOrEmpty(steuerwert) || BemessungKatalog.Finde(steuerwert) == null)
                steuerwert = DbWerte.BEMESSUNG_BETRAG;
            return BemessungKatalog.Anzeige(steuerwert, komponente);
        }

        /// <summary>
        /// Herleitung einer Kostenposition als Klartext („1.500 h/a × 2,50 €/h").
        /// Leer, wenn die Position fest ist (fester Betrag, fester Jahresbetrag, unbekannter
        /// Steuerwert) oder ein Szenariowert die Ableitung geschlagen hat — dann steht keine
        /// Herleitung dahinter.
        /// </summary>
        public static string Herleitung(KostenPositionNachweis n,
                                        System.Globalization.CultureInfo kultur)
        {
            if (n == null || n.SzenarioGepflegt) return "";
            // ETAPPE E8c (E8b‑Q2): „bemessen" ist, was der Rechenweg als Menge × Satz rechnet —
            // dieselbe Frage wie die Formelmappe (Stufe 3). Bis hierher galt nur BETRAG als
            // fest; ein fester JAHRESBETRAG mit gepflegter Menge bekam eine Herleitung, nach
            // der gar nicht gerechnet wird.
            if (!BetriebskostenCtrl.Bemessungsfaktor(n.Bemessung).HasValue) return "";
            if (!n.Menge.HasValue || !n.Einheitpreis.HasValue) return "";
            // ANWENDERENTSCHEID 15.09.2026: Die Satzeinheit folgt der Bezugsgröße des
            // GEWERKS — am Pufferspeicher ist sie „€/Ltr.", nicht „€/kW".
            // U33 (18.09.2026): Diese Liste sind die BETRIEBSKOSTENzeilen; ein
            // Leistungssatz heißt hier „€/kWp·a" und nicht „€/kWp".
            string text = n.Menge.Value.ToString("N2", kultur) + " " +
                          BetriebskostenCtrl.MengenEinheit(n.Bemessung, n.Komponente) + " × " +
                          n.Einheitpreis.Value.ToString("N3", kultur) + " " +
                          BetriebskostenCtrl.SatzEinheit(n.Bemessung, n.Komponente, true);
            // ETAPPE E10 (Stufe S3): Kam der Satz aus der Nutzungsdauertabelle, sagt die
            // Herleitung es — dieselbe Zeile in Wort- und Tabellenbericht und in der
            // Spalte „Herleitung" der Formelmappe (Stufe 3).
            if (string.Equals(n.SatzHerkunft, NutzungsdauerSatzCtrl.HERKUNFT_TABELLE,
                              StringComparison.Ordinal))
                text += " · " + NutzungsdauerSatzCtrl.HerkunftKurz();
            // ETAPPE E30/2 (#548, B4): Kam der Satz aus dem Hilfsenergieanteil der Anlage,
            // sagt die Herleitung es ebenso.
            else if (string.Equals(n.SatzHerkunft, HilfsenergieAusAnteil.HERKUNFT_ANLAGENANTEIL,
                                   StringComparison.Ordinal))
                text += " · " + HilfsenergieAusAnteil.HerkunftKurz();
            return text;
        }

        // =====================================================================
        // ETAPPE E8c (E8b‑Q3) — Positionen mit späterem Startjahr in der Gliederung
        // =====================================================================

        /// <summary>
        /// Toleranz der Gliederungsprobe [€/a]: Weichen Positionen und angesetzte
        /// Betriebskosten um mehr ab, warnt der Bericht (<see cref="GliederungAbweichung"/>).
        /// </summary>
        public const double GLIEDERUNG_TOLERANZ_EUR = 0.5;

        /// <summary>
        /// Zahlt die Position schon im ersten Jahr? Nein nur bei einem Startjahr ≥ 2 (KD6) —
        /// dieselbe Grenze wie die Summenschleife der Rechnung
        /// (<c>WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe</c>, <c>start &gt; 1</c>).
        /// </summary>
        public static bool LaeuftImErstenJahr(KostenPositionNachweis n)
        {
            return n == null || !n.StartJahr.HasValue || n.StartJahr.Value <= 1;
        }

        /// <summary>
        /// Die <b>Probe der Betriebskostengliederung</b> gegen die angesetzten Betriebskosten
        /// p. a. — leer, wenn beide auf <see cref="GLIEDERUNG_TOLERANZ_EUR"/> übereinstimmen
        /// oder keine Vergleichszahl vorliegt; sonst die Warnung „Gliederung unvollständig"
        /// mit beiden Beträgen. Wort- und Tabellenbericht rufen dieselbe Probe.
        ///
        /// <para><b>Verglichen werden nur die Positionen des ersten Jahres</b> (Anwenderentscheid
        /// 23.09.2026 zu E8b‑Q3, Lesart b). Die angesetzten Betriebskosten p. a.
        /// (<see cref="WirtschaftlichkeitErgebnis.BetriebskostenJahr"/>) sind die Jahr-1-Zahl
        /// der Rechnung; eine Position mit späterem Startjahr zahlt erst ab ihrem Jahr und
        /// steckt nicht darin. Bis E8c ging sie trotzdem in den Vergleich, und der Bericht
        /// meldete eine unvollständige Gliederung, wo nur ein Startjahr stand (2.400 gegen
        /// 1.800 € — die Differenz war die Wartung ab Jahr 6). Eine echte Lücke — eine
        /// Position der Rechnung, die in keinem Block der Tabelle steht, oder eine
        /// abgebrochene Nachweisliste — trifft die Jahr-1-Zahl weiterhin und warnt.</para>
        ///
        /// <para><b>Warum nicht „die Warnung nennt Startjahr und Differenz".</b> Die zweite
        /// Lesart warnte weiter, wo nichts fehlt — und eine echte Lücke neben einer
        /// Startjahr-Position ginge in ihrer Aufzählung unter. Das Startjahr steht
        /// stattdessen an der Position selbst (<see cref="HerleitungZeile"/>).</para>
        /// </summary>
        /// <param name="summeErstesJahr">Summe der Tabellenpositionen, die im ersten Jahr
        /// zahlen [€/a] (<see cref="LaeuftImErstenJahr"/>).</param>
        /// <param name="betriebskostenJahr">Die angesetzten Betriebskosten p. a. [€/a];
        /// <c>null</c> = keine Vergleichszahl, keine Probe.</param>
        public static string GliederungAbweichung(double summeErstesJahr, double? betriebskostenJahr,
                                                  CultureInfo kultur)
        {
            if (!betriebskostenJahr.HasValue ||
                Math.Abs(summeErstesJahr - betriebskostenJahr.Value) <= GLIEDERUNG_TOLERANZ_EUR)
                return "";
            return string.Format(MyResource.Resource.WIRT_BK_ABWEICHUNG,
                                 summeErstesJahr.ToString("N2", kultur),
                                 betriebskostenJahr.Value.ToString("N2", kultur));
        }

        /// <summary>
        /// Die Spalte „Herleitung" einer Position in Wort- und Tabellenbericht: ihre
        /// <see cref="Herleitung"/>, sonst — wo ein Szenariowert die Ableitung schlug — dessen
        /// Kennzeichen, und bei einem Startjahr ≥ 2 dahinter „ab Jahr X". So bleibt sichtbar,
        /// warum die Summe der Tabelle die angesetzten Betriebskosten p. a. übersteigen darf:
        /// Die Position steht in der Summe, zahlt aber erst ab ihrem Jahr.
        /// </summary>
        public static string HerleitungZeile(KostenPositionNachweis n, CultureInfo kultur)
        {
            if (n == null) return "";
            string text = Herleitung(n, kultur);
            if (text.Length == 0 && n.SzenarioGepflegt) text = MyResource.Resource.WIRT_BK_SZENARIOWERT;
            // ETAPPE E16 (V‑G3): Eine Position „alle n Jahre" sagt es in derselben Spalte —
            // „alle n Jahre ab Jahr X" statt „ab Jahr X"; X ist ohne Startjahr das Jahr 1.
            string alle = Wiederholperiode.Herleitung(n.Wiederholperiode, n.StartJahr, kultur);
            if (alle.Length > 0) return text.Length == 0 ? alle : text + " · " + alle;
            if (LaeuftImErstenJahr(n)) return text;
            string ab = string.Format(kultur, MyResource.Resource.WIRT_BK_AB_JAHR, n.StartJahr.Value);
            return text.Length == 0 ? ab : text + " · " + ab;
        }
    }

    // =========================================================================
    /// <summary>
    /// ETAPPE E7 — eine Positionsspalte der Mehrjahrestabelle.
    /// </summary>
    public sealed class MehrjahresSpalte
    {
        /// <summary>Sprachneutraler Schlüssel (ASCII, eingefroren).</summary>
        public string Schluessel = "";

        /// <summary>Anzeigetitel aus <c>MyResource</c> — bereits lokalisiert.</summary>
        public string Titel = "";

        /// <summary>Nominaler Betrag je Jahr [€], Index 0…T. Ausgaben negativ.</summary>
        public double[] JeJahr;

        /// <summary>true = Summenspalte (Netto, Barwert, kumuliert); sie bleibt auch
        /// dann stehen, wenn sie nur Nullen führt.</summary>
        public bool IstSumme;

        public double Wert(int t) { return JeJahr != null && t >= 0 && t < JeJahr.Length ? JeJahr[t] : 0; }

        /// <summary>true, wenn die Spalte irgendeinen Betrag ungleich 0 führt.</summary>
        public bool Belegt
        {
            get
            {
                if (JeJahr == null) return false;
                for (int t = 0; t < JeJahr.Length; t++) if (JeJahr[t] != 0) return true;
                return false;
            }
        }
    }

    /// <summary>
    /// ETAPPE E7 — die Mehrjahrestabelle EINES Projekts: Zeilen sind die Jahre 0…T,
    /// Spalten die Positionen des Zahlungsstroms.
    ///
    /// <para><b>Warum Jahre als Zeilen.</b> Bei T = 20 wären 21 Jahresspalten auf A4
    /// nicht darstellbar; der Kapitalwert-Verlauf im Excel-Bericht macht es seit Phase 11
    /// bereits andersherum, und die Tabelle passt damit in beide Ausgaben ohne zweites
    /// Layout.</para>
    ///
    /// <para><b>Vorzeichen.</b> Ausgaben negativ, Einnahmen positiv — dadurch ist die
    /// Summe der Positionsspalten die Spalte „Netto nominal", und die Tabelle prüft sich
    /// selbst. Die letzte Zeile schließt mit dem Restwert-Barwert auf den Nettobarwert
    /// auf.</para>
    ///
    /// <para><b>Was hier NICHT steht:</b> vermiedene Kosten und Aufschlagsbetrag. Beide
    /// stecken bereits in anderen Positionen (in der kleineren Bezugsmenge bzw. in den
    /// Energiekosten); eine eigene Zahlungszeile wäre eine Doppelzählung. Sie erscheinen
    /// unter der Tabelle als ausdrücklich benannter Nachweisblock.</para>
    /// </summary>
    public sealed class Mehrjahresbild
    {
        /// <summary>ETAPPE E15 — Schlüssel der Spalte „Risikoabzug" (nur mit Abzug belegt).</summary>
        public const string RISIKO = "RISIKO";

        /// <summary>Betrachtungszeitraum T [a].</summary>
        public int Jahre;

        /// <summary>Die belegten Positionsspalten in Ausgabereihenfolge.</summary>
        public List<MehrjahresSpalte> Spalten = new List<MehrjahresSpalte>();

        /// <summary>Restwert-Barwert zum Zeitpunkt T [€].</summary>
        public double RestwertBarwert;

        /// <summary>Kumulierter Barwert im Jahr T (ohne Restwert) [€].</summary>
        public double KumuliertT;

        /// <summary>Nettobarwert = <see cref="KumuliertT"/> + <see cref="RestwertBarwert"/>.</summary>
        public double Kapitalwert { get { return KumuliertT + RestwertBarwert; } }

        /// <summary>
        /// Baut das Bild aus dem Zahlungsbild einer Verlaufslinie.
        /// <c>null</c>, wenn keine Reihe vorliegt.
        /// </summary>
        public static Mehrjahresbild Baue(VerlaufSerie serie)
        {
            if (serie == null || serie.Bild == null || serie.Kumuliert == null) return null;
            KapitalwertRechner.Zahlungsbild b = serie.Bild;
            if (b.NominalReihe == null || b.BarwertReihe == null) return null;

            int T = b.NominalReihe.Length - 1;
            if (T < 1) return null;

            var m = new Mehrjahresbild
            {
                Jahre = T,
                RestwertBarwert = serie.RestwertBarwert,
                KumuliertT = serie.Kumuliert[Math.Min(T, serie.Kumuliert.Length - 1)]
            };

            // Investition und Ersatzbeschaffung in EINER Spalte: beides ist dieselbe
            // Art Zahlung, nur zu verschiedenen Zeitpunkten — und Jahr 0 trägt ohnehin
            // nichts anderes.
            var investErsatz = new double[T + 1];
            investErsatz[0] = -b.Investition;
            for (int t = 1; t <= T; t++)
                investErsatz[t] = b.ErsatzJeJahr != null && t < b.ErsatzJeJahr.Length
                                  ? -b.ErsatzJeJahr[t] : 0;

            m.Nimm("INVEST_ERSATZ", MyResource.Resource.WIRT_MJ_INVEST_ERSATZ, investErsatz);

            // PAKET FX3 (Anwenderentscheid R-2): Die Spalte „Betrieb" trägt seither
            // zwei verschieden fortgeschriebene Anteile — den Betriebs-Topf mit p_B und
            // den Endenergie-Topf mit p_E (Hilfsenergie „x % der Endenergie…", seit
            // PAKET FX4-b auch „% der Brennstoff-/Stromkosten").
            // KapitalwertRechner.BetriebJeJahr liefert die SUMME beider, genau damit
            // diese Tabelle unverändert bleibt: Die Summe der Positionsspalten ist
            // weiterhin die Spalte „Netto nominal", und die Selbstprüfung darunter
            // (kumuliert(T) + Restwert = Kapitalwert) bleibt gültig. Der p_E-Anteil ist
            // in KapitalwertRechner.Zahlungsbild.EndenergieAnteilJeJahr einzeln
            // ausgewiesen; eine eigene Spalte bekäme er erst mit einem eigenen
            // Anzeigetext (offener Punkt FX3-1).
            m.Nimm("BETRIEB", MyResource.Resource.WIRT_MJ_BETRIEB, Negativ(b.BetriebJeJahr, T));
            m.Nimm("ENERGIE", MyResource.Resource.WIRT_MJ_ENERGIE, Negativ(b.EnergieJeJahr, T));
            m.Nimm("BEHG", MyResource.Resource.WIRT_MJ_BEHG, Negativ(b.BehgJeJahr, T));
            m.Nimm("EINSPEISUNG", MyResource.Resource.WIRT_MJ_EINSPEISUNG,
                   Positiv(b.EinspeiseerloesJeJahr, T));

            // Die vier benannten Erlösreihen aus E4 — hier werden sie zum ersten Mal
            // einzeln sichtbar. Der KWK-Zuschlag zeigt dabei sein Auslaufen.
            m.Reihe(b, KapitalwertRechner.ErloesReihe.KWKG, MyResource.Resource.WIRT_REIHE_KWKG, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.ENERGIESTEUER,
                    MyResource.Resource.WIRT_REIHE_ENERGIESTEUER, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG,
                    MyResource.Resource.WIRT_REIHE_STROMSTEUER_BEFREIUNG, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG,
                    MyResource.Resource.WIRT_REIHE_STROMSTEUER_ENTLASTUNG, T);

            // ETAPPE E2 (Befund V-3): die PV-Vergütungsreihe. Sie ist eine ZUSATZ-Reihe
            // des Kapitalwertrechners und steckt damit in „Netto nominal", aber NICHT in
            // der Spalte „Einspeisung" (die trägt allein den konstanten Einspeiseerlös).
            // Ohne eigene Spalte fehlte sie in der Summe der Positionsspalten, und die
            // Selbstprüfung der Tabelle ging um genau diesen Betrag daneben.
            m.Reihe(b, KapitalwertRechner.ErloesReihe.PV_VERGUETUNG,
                    MyResource.Resource.WIRT_REIHE_PV, T);

            // AUFTRAG U17 — die Pauschale des § 9 KWKG. Sie braucht einen EIGENEN
            // Aufruf, weil ihr Betrag im INDEX 0 steht und Reihe() erst bei t = 1
            // beginnt; ohne die Spalte stimmte die Selbstprüfung der Tabelle im Jahr 0
            // nicht: „Netto nominal" trägt dort −I₀ + Pauschale, die Summe der
            // Positionsspalten aber nur −I₀.
            m.ReiheAbJahr0(b, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE,
                           MyResource.Resource.WIRT_REIHE_KWKG_PAUSCHALE, T);

            // ETAPPE E15 (V‑G7, Anhang F): der Risikoabzug je Periode ab Jahr 1 — er steckt in
            // „Netto nominal", also braucht er eine Spalte, sonst ginge die Selbstprüfung um
            // genau diesen Betrag daneben. Ohne Abzug entsteht keine Spalte (Nimm).
            if (b.RisikoJeJahr != null)
                m.Nimm(RISIKO, MyResource.Resource.WIRT_MJ_RISIKO, Negativ(b.RisikoJeJahr, T));

            m.Summe("NETTO", MyResource.Resource.WIRT_MJ_NETTO, Kopie(b.NominalReihe, T));
            m.Summe("BARWERT", MyResource.Resource.WIRT_MJ_BARWERT, Kopie(b.BarwertReihe, T));
            m.Summe("KUMULIERT", MyResource.Resource.WIRT_MJ_KUMULIERT, Kopie(serie.Kumuliert, T));
            return m;
        }

        private void Nimm(string schluessel, string titel, double[] werte)
        {
            var s = new MehrjahresSpalte { Schluessel = schluessel, Titel = titel, JeJahr = werte };
            if (s.Belegt) Spalten.Add(s);   // nie eine Spalte aus lauter Nullen
        }

        private void Summe(string schluessel, string titel, double[] werte)
        {
            Spalten.Add(new MehrjahresSpalte
            { Schluessel = schluessel, Titel = titel, JeJahr = werte, IstSumme = true });
        }

        private void Reihe(KapitalwertRechner.Zahlungsbild b, string name, string titel, int T)
        {
            if (!b.HatReihe(name)) return;
            var werte = new double[T + 1];
            for (int t = 1; t <= T; t++) werte[t] = b.ReihenWert(name, t);
            Nimm(name, titel, werte);
        }

        /// <summary>
        /// AUFTRAG U17 — wie <see cref="Reihe"/>, nur ab dem JAHR 0. Genau eine Reihe
        /// führt dort einen Betrag: die Pauschale nach § 9 KWKG, die einmalig zum
        /// Zeitpunkt der Investition fließt.
        ///
        /// <para><b>Ohne <c>HatReihe</c>.</b> Auch jene Wache beginnt bei t = 1 — für
        /// eine Reihe, die ihren einzigen Betrag im Index 0 trägt, meldet sie „leer".
        /// Gefiltert wird deshalb erst in <see cref="Nimm"/>, das die fertige Spalte auf
        /// einen Betrag ungleich 0 prüft — über ALLE Jahre.</para>
        /// </summary>
        private void ReiheAbJahr0(KapitalwertRechner.Zahlungsbild b, string name, string titel, int T)
        {
            var werte = new double[T + 1];
            for (int t = 0; t <= T; t++) werte[t] = b.ReihenWert(name, t);
            Nimm(name, titel, werte);
        }

        private static double[] Negativ(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 1; t <= T && t < quelle.Length; t++) z[t] = -quelle[t];
            return z;
        }

        private static double[] Positiv(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 1; t <= T && t < quelle.Length; t++) z[t] = quelle[t];
            return z;
        }

        private static double[] Kopie(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 0; t <= T && t < quelle.Length; t++) z[t] = quelle[t];
            return z;
        }
    }
}
