using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schweregrade einer Kohärenzzeile — sprachneutrale ASCII-Schlüssel (Schicht 2 der
    /// Drei-Schichten-Regel). Kein Anzeigetext: Die Beschriftung holt die Oberfläche
    /// über <c>MyResource</c>.
    /// </summary>
    public static class KohaerenzSchwere
    {
        /// <summary>Entlastung gebucht, Belastung im Preis fehlt (Fall 2 des Konzepts § 4.1);
        /// seit E7c (S‑2, Entscheid A3) auch die GESPERRTE Mischlage § 53/§ 53a neben
        /// § 54 (Fall 5) — der § 54-Betrag ist verworfen.</summary>
        public const string WARNUNG = "WARNUNG";

        /// <summary>Belastung ohne Entlastung bzw. abweichender Satz (Fälle 3 und 4).</summary>
        public const string HINWEIS = "HINWEIS";

        /// <summary>
        /// ETAPPE B6 — <b>es stimmt</b>: Fall 1 des Konzepts § 3.9, die POSITIVE
        /// Nennung. Bis B5 blieb der stimmige Fall stumm, und der Anwender konnte
        /// „keine Zeile" nicht von „nicht geprüft" unterscheiden. Die Zeile ändert
        /// nichts und warnt nicht; sie sagt, dass die Prüfung gelaufen ist und
        /// zusammenpasst.
        /// </summary>
        public const string BESTAETIGUNG = "BESTAETIGUNG";
    }

    /// <summary>
    /// EINE Zeile der Kohärenzprüfung (Konzept BHKW-Wirtschaftlichkeit § 4.1, BW2;
    /// Entscheidung BF2 „nur warnen" vom 30.08.2026).
    ///
    /// <para><b>Reine Ausgabe.</b> Die Zeile ändert keinen Rechenwert — sie benennt den
    /// Widerspruch zwischen einer gebuchten Steuergutschrift und dem Steueranteil, den
    /// der erfasste Energiepreis ausweist. Der <see cref="Betrag"/> ist immer die
    /// TATSÄCHLICH gebuchte Gutschrift aus dem einen Rechenweg
    /// (<see cref="SteuerGutschriftRechner"/>), nie eine Zweitrechnung.</para>
    /// </summary>
    public class KohaerenzHinweis
    {
        /// <summary>Steuerwert aus <see cref="KohaerenzSchwere"/>.</summary>
        public string Schwere = KohaerenzSchwere.HINWEIS;

        /// <summary>Fertig formatierter Anzeigetext (Sprache über <c>MyResource</c>).</summary>
        public string Text = "";

        /// <summary>Betroffener Betrag [€/a]; <c>null</c> = ohne Betrag (Fälle 3 und 4).</summary>
        public double? Betrag;
    }

    /// <summary>
    /// Die Größen EINES Wirtschaftlichkeitslaufs, welche die Kohärenzprüfung braucht.
    /// Alle Werte stammen aus dem Lauf selbst — die Prüfung rechnet nichts nach.
    /// </summary>
    internal sealed class KohaerenzLauf
    {
        /// <summary>Kalenderjahr des ersten Betrachtungsjahres (Förderbeginn) — bestimmt,
        /// welcher Katalogsatz für Fall 4 gilt.</summary>
        public int Jahr;

        /// <summary>Die Eingabe der Steuerrechnung dieses Laufs (Anlagen mit Träger,
        /// Heizwerten, Katalogschlüsseln und der gewählten Norm). <c>null</c> = kein
        /// Steuerpfad im Lauf; dann gibt es nichts zu prüfen.</summary>
        public SteuerEingabe Steuer;

        /// <summary>Gebuchte Energiesteuer-Entlastung im Jahr 1 [€/a].</summary>
        public double EnergiesteuerEur;

        /// <summary>Gebuchte Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 im Jahr 1 [€/a].</summary>
        public double StromsteuerBefreiungEur;

        /// <summary>ETAPPE B6: true = die Befreiung ist als ERLÖS gebucht und steckt im
        /// Kapitalwert; false = sie ist nur ausgewiesen (Vorgabe AUSWEIS). Die
        /// Stromseite prüft danach zwei ganz verschiedene Dinge.</summary>
        public bool StromsteuerBefreiungAlsErloes;

        /// <summary>Gebuchte Stromsteuer-Entlastung § 9b im Jahr 1 [€/a].</summary>
        public double StromsteuerEntlastungEur;

        /// <summary>
        /// ETAPPE E2 (Befund R5): die gebuchte CO₂-Abgabe des Jahres 1 [€/a] aus der
        /// BEHG-Reihe. 0 = keine Reihe gebucht — dann gibt es nichts doppelt zu buchen.
        /// </summary>
        public double Co2AbgabeEur;

        /// <summary>
        /// ETAPPE E2 (Befund R6): der angewandte Strommix-Vorgabewert [g CO₂/kWh],
        /// wenn die Emissionsrechnung mangels zugeordnetem Strom-Energieträger darauf
        /// zurückgefallen ist; <c>null</c> = kein Rückfall. Bis E2 war das ein
        /// Laufhinweis OHNE die Zahl (<c>02/§ 4.2</c>).
        /// </summary>
        public double? StrommixRueckfallGJeKwh;

        /// <summary>
        /// ETAPPE E7c — die Datenlücken der KWKG-Rechnung dieses Laufs (Anlagenart fehlt,
        /// Stromkennzahl fehlt); <c>null</c> oder leer = keine. Die Rechnung hat sie
        /// festgehalten, genau dort, wo sie an der Lücke den Zuschlag einer Anlage auf 0
        /// gesetzt hat — die Prüfung liest sie nur und rechnet nichts nach.
        /// </summary>
        public KwkgLuecken Kwkg;
    }

    /// <summary>
    /// ETAPPE E7c — die <b>Datenlücken der KWKG-Rechnung</b> eines Laufs, je Art die
    /// Bezeichner der betroffenen Anlagen (entdoppelt, in der Reihenfolge ihres
    /// Auftretens).
    ///
    /// <para><b>Beide Lücken kosten den Zuschlag einer Anlage</b>, und beide sind
    /// Datenlücken, keine Rechenfehler: <see cref="OhneAnlagenart"/> — das Kontingent
    /// war nach § 8 aus der Anlagenart abzuleiten, und die fehlt (Entscheid E7‑Q1,
    /// Lesart b); <see cref="OhneStromkennzahl"/> — das Kennzeichen „Vorrichtung zur
    /// Abwärmeabfuhr" steht, aber weder eine gepflegte Stromkennzahl noch P_el ÷ P_th
    /// der Gerätezeile (Entscheid E7‑Q2 (2) mit Auflage).</para>
    /// </summary>
    internal sealed class KwkgLuecken
    {
        /// <summary>Anlagen, deren Kontingent aus der Anlagenart abzuleiten war — ohne
        /// Anlagenart.</summary>
        public readonly List<string> OhneAnlagenart = new List<string>();

        /// <summary>Anlagen mit Kennzeichen „Vorrichtung zur Abwärmeabfuhr" ohne
        /// bestimmbare Stromkennzahl.</summary>
        public readonly List<string> OhneStromkennzahl = new List<string>();

        /// <summary>Nimmt einen Bezeichner in eine der beiden Listen auf — einmal.</summary>
        public static void Merke(List<string> liste, string bezeichner)
        {
            if (liste == null) return;
            string b = bezeichner ?? "";
            if (!liste.Contains(b)) liste.Add(b);
        }
    }

    /// <summary>
    /// ETAPPE B2 — Kohärenzprüfung nach Leitentscheidung BW2 des Konzepts
    /// <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> (§ 4.1, Vier-Fall-Tabelle).
    ///
    /// <para><b>Reine Leselogik, ergebnisneutral.</b> Entscheidung BF2 (30.08.2026)
    /// lautet: <i>nur warnen</i>. Die Klasse verändert deshalb keine Gutschrift, keine
    /// Reihe und keinen Kapitalwert — sie liest den Steueranteil, den der erfasste
    /// Energiepreis ausweist, vergleicht ihn mit der gebuchten Entlastung und liefert
    /// Hinweiszeilen. Eine stille Rechenänderung an einer Steuergröße wäre schlimmer als
    /// eine sichtbare Lücke (Konzept § 9, BF2).</para>
    ///
    /// <para><b>Die vier Fälle des § 4.1:</b>
    /// <list type="number">
    ///   <item>Entlastung gewählt, Anteil im Preis aktiv → <b>keine Zeile</b>. Die
    ///         positive Nennung kommt mit der Herleitungstafel (Etappe B6).</item>
    ///   <item>Entlastung gebucht, Anteil inaktiv oder 0 → <b>WARNUNG mit Betrag</b>.</item>
    ///   <item>Anteil aktiv, Entlastung nicht gewählt → <b>HINWEIS ohne Betrag</b>. Den
    ///         entgangenen Betrag zu beziffern hieße, die Entlastung ein zweites Mal zu
    ///         rechnen — genau das unterbleibt hier.</item>
    ///   <item>Gepflegter Anteil ≠ Katalogsatz des Jahres → <b>HINWEIS</b>, beide Sätze
    ///         genannt; Toleranz <see cref="TOLERANZ_CT_KWH"/>.</item>
    /// </list></para>
    ///
    /// <para><b>PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2) — Fall 5
    /// neben den vieren des Konzepts:</b> Stehen im selben Projekt eine Entlastung nach
    /// § 53 / § 53a Abs. 5 EnergieStG und eine nach § 54 nebeneinander (Projekt- oder
    /// Anlagenwahl, seit B3a je Anlage möglich), erscheint ein <b>HINWEIS</b> mit den
    /// beteiligten Anlagen und ihren Wahlen — ohne Betrag, ohne Sperre, ohne
    /// Zahlenänderung (<see cref="MischlageEnergiesteuer"/>).</para>
    ///
    /// <para><b>ETAPPE E2 — zwei Zeilen der CO₂-Seite (§ 3.9 des konsolidierten
    /// Konzepts, Befunde R5 und R6):</b> Der <b>CO₂-Doppelansatz</b> (ein aktiver
    /// CO₂-Bestandteil im Arbeitspreis eines Trägers <b>und</b> eine gebuchte
    /// BEHG-Reihe, <see cref="Co2DoppelansatzBehg"/>) erscheint als <b>WARNUNG</b> mit
    /// dem doppelt gebuchten Jahresbetrag; der <b>Strommix-Rückfall</b>
    /// (<see cref="StrommixRueckfall"/>) als <b>HINWEIS</b> mit dem angewandten Wert in
    /// g CO₂/kWh. Beide hängen an KEINEM Steuerpfad und werden deshalb vor der Prüfung
    /// auf <c>lauf.Steuer</c> erledigt. Rechenweg unverändert: Der Anwenderentscheid zu
    /// Frage Q3 der Mockup-Prüfung ist <b>Weg (a)</b> — ausweisen, nicht umrechnen.</para>
    ///
    /// <para><b>Zwei Preisseiten, EINE Leseregel.</b> Für Brennstoffe wie für Strom gilt
    /// seit SP-E-2: <c>NULL</c> heißt „kein Anteil erfasst". Beim Strom bleibt der
    /// Vorschlagssatz von 2,05 ct/kWh zwar als Zahl im Feld stehen, sein Aktiv-Schalter
    /// aber auf <c>false</c> — gerechnet wird mit ihm erst, wenn der Anwender ihn setzt.
    /// Für Fall 4 (Satzvergleich mit dem Katalog) reicht das trotzdem nicht: Ob ein
    /// Stromsteueranteil wirklich GEPFLEGT ist, sagt nur die rohe Spalte, und
    /// <see cref="StrompreisZerlegungCtrl.StromsteuerRoh"/> liest sie eigens. Einen nie erfassten Wert mit dem
    /// Katalog zu vergleichen wäre ein Vergleich des Katalogs mit sich selbst.</para>
    /// </summary>
    internal static class KohaerenzPruefung
    {
        /// <summary>Toleranz des Satzvergleichs in Fall 4 [ct/kWh] (Konzept § 4.1).</summary>
        public const double TOLERANZ_CT_KWH = 0.005;

        /// <summary>Gigajoule je Megawattstunde — für Sätze in EUR/GJ.</summary>
        private const double GJ_JE_MWH = 3.6;

        // =====================================================================
        // Einstieg
        // =====================================================================

        /// <summary>
        /// Prüft die Kohärenz zwischen gebuchten Steuergutschriften und den Steueranteilen
        /// der erfassten Energiepreise. Leere Liste = alles konsistent (Fall 1) oder kein
        /// Steuerpfad im Lauf.
        /// </summary>
        /// <param name="idProjekt">Projekt, dessen Preiszerlegung gelesen wird.</param>
        /// <param name="lauf">Die Größen des Laufs; <c>null</c> ergibt eine leere Liste.</param>
        internal static List<KohaerenzHinweis> Pruefe(int idProjekt, KohaerenzLauf lauf)
        {
            var liste = new List<KohaerenzHinweis>();
            if (idProjekt <= 0) return liste;

            CultureInfo kultur = BerichtTexte.Kultur;

            // ETAPPE E7c3 (Befund B‑6): Jede Teilprüfung läuft weiter für sich gekapselt —
            // ein Fehlschlag darf die übrigen nicht mitnehmen und den Lauf nicht —, aber
            // nicht mehr STILL: Scheitert eine, steht an ihrer Stelle die Kohärenzzeile
            // „Prüfung „X“ nicht ausführbar: <Grund>“ (Teilpruefung).

            // ETAPPE B3 Paket b: Die Doppelpflege der Hilfsenergie hängt an KEINEM
            // Steuerpfad — sie ist auch an einem reinen Wärmepumpenprojekt möglich und
            // wird deshalb VOR der Prüfung auf lauf.Steuer erledigt.
            Teilpruefung(TP_HILFSENERGIE, () => HilfsenergieDoppelpflege(idProjekt, kultur, liste), kultur, liste);

            // KONZEPT § 2.16: die Herkunft der PV-Verguetung. Sie haengt wie die
            // Doppelpflege an KEINEM Steuerpfad und steht deshalb VOR der Pruefung auf
            // lauf.Steuer.
            Teilpruefung(TP_PV_HERKUNFT, () => PvVerguetungHerkunft(idProjekt, kultur, liste), kultur, liste);

            if (lauf == null) return liste;

            // ETAPPE E2 (Befunde R5 und R6): Die beiden CO₂-Zeilen hängen an keinem
            // STEUERpfad — ein reines Kesselprojekt ohne jede Entlastungswahl kann
            // sowohl den Doppelansatz als auch den Strommix-Rückfall tragen. Sie stehen
            // deshalb VOR der Prüfung auf lauf.Steuer.
            Teilpruefung(TP_CO2_DOPPEL, () => Co2DoppelansatzBehg(idProjekt, lauf, kultur, liste), kultur, liste);
            Teilpruefung(TP_STROMMIX, () => StrommixRueckfall(lauf, kultur, liste), kultur, liste);

            // ETAPPE E7c: die Datenlücken der KWKG-Rechnung. Sie hängen an keinem
            // STEUERpfad — ein BHKW-Projekt ohne jede Entlastungswahl kann sie tragen —
            // und stehen deshalb wie die CO₂-Zeilen VOR der Prüfung auf lauf.Steuer.
            Teilpruefung(TP_KWKG, () => KwkgDatenluecken(lauf, kultur, liste), kultur, liste);

            if (lauf.Steuer == null) return liste;

            // Jede Seite für sich gekapselt: Ein Fehlschlag der Brennstoffseite darf die
            // Stromseite nicht mitnehmen — und keiner von beiden den Lauf.
            Teilpruefung(TP_BRENNSTOFF, () => Brennstoffseite(idProjekt, lauf, kultur, liste), kultur, liste);
            // PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2): Fall 5.
            // Eigener Schritt, nicht Teil von Brennstoffseite — er braucht weder
            // Energieträger noch Preiszerlegung, sondern allein die Normwahlen, und
            // dürfte deshalb nicht an deren Vorabfiltern hängenbleiben.
            Teilpruefung(TP_MISCHLAGE, () => MischlageEnergiesteuer(lauf, kultur, liste), kultur, liste);
            Teilpruefung(TP_STROM, () => Stromseite(idProjekt, lauf, kultur, liste), kultur, liste);
            // ETAPPE E2 (Befund S-5): die Erlaubnisschwelle des StromStG. Sie war der
            // einzige gesäte Katalogschlüssel der Stromsteuer OHNE Leser; ein
            // Rechenwerk gibt es dazu nicht, eine Pflicht des Betreibers schon.
            Teilpruefung(TP_ERLAUBNIS, () => ErlaubnisschwelleStrom(lauf, kultur, liste), kultur, liste);
            // ETAPPE B6: Der Einwand gegen den Modus ERLOES hängt weder an einem
            // Energieträger noch an der Preiszerlegung - er gilt aus der Sache heraus
            // und steht deshalb, wie Fall 5, als eigener Schritt.
            Teilpruefung(TP_DOPPELZAEHLUNG, () => DoppelzaehlungBefreiung(lauf, kultur, liste), kultur, liste);

            return liste;
        }

        // =====================================================================
        // ETAPPE E7c3 (Befund B‑6) — eine gescheiterte Teilprüfung wird sichtbar
        // =====================================================================

        /// <summary>Ressourcenschlüssel der zehn Teilprüfungen — ihr Anzeigename in der
        /// Zeile „Prüfung „X“ nicht ausführbar".</summary>
        internal const string TP_HILFSENERGIE = "KOH_TP_HILFSENERGIE";
        internal const string TP_PV_HERKUNFT = "KOH_TP_PV_HERKUNFT";
        internal const string TP_CO2_DOPPEL = "KOH_TP_CO2_DOPPEL";
        internal const string TP_STROMMIX = "KOH_TP_STROMMIX";
        internal const string TP_KWKG = "KOH_TP_KWKG";
        internal const string TP_BRENNSTOFF = "KOH_TP_BRENNSTOFF";
        internal const string TP_MISCHLAGE = "KOH_TP_MISCHLAGE";
        internal const string TP_STROM = "KOH_TP_STROM";
        internal const string TP_ERLAUBNIS = "KOH_TP_ERLAUBNIS";
        internal const string TP_DOPPELZAEHLUNG = "KOH_TP_DOPPELZAEHLUNG";

        /// <summary>
        /// Führt EINE Teilprüfung aus. Scheitert sie, fängt die Methode den Fehler und
        /// schreibt an ihrer Stelle eine <b>WARNUNG</b> „Prüfung „X“ nicht ausführbar:
        /// &lt;Grund&gt;“ in die Liste — die übrigen Teilprüfungen und der Lauf gehen
        /// weiter (dieselbe Kapselung wie bisher), aber der Anwender sieht, dass eine
        /// Kohärenzaussage fehlt. Bis E7c3 stand hier <c>try { … } catch { }</c>: Eine
        /// gescheiterte Prüfung sah aus wie eine bestandene (Befund B‑6).
        /// </summary>
        /// <param name="schluessel">Ressourcenschlüssel des Prüfungsnamens (<c>TP_*</c>).</param>
        internal static void Teilpruefung(string schluessel, Action pruefung, CultureInfo kultur,
                                          List<KohaerenzHinweis> liste)
        {
            try
            {
                pruefung();
            }
            catch (Exception ex)
            {
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Text = NichtAusfuehrbar(schluessel, ex, kultur)
                });
            }
        }

        /// <summary>Der Wortlaut „Prüfung „X“ nicht ausführbar: &lt;Grund&gt;“.</summary>
        internal static string NichtAusfuehrbar(string schluessel, Exception ex, CultureInfo kultur)
        {
            return string.Format(kultur, T("KOH_PRUEFUNG_NICHT_AUSFUEHRBAR",
                                           "Prüfung „{0}“ nicht ausführbar: {1}"),
                                 T(schluessel, schluessel), Fehlergrund.Text(ex));
        }

        // =====================================================================
        // Hilfsenergie — Doppelpflege Menge (Anlage) gegen Kosten (Position)
        // =====================================================================

        /// <summary>
        /// U31 — DERSELBE Hinweis für EINE Anlage, für den Kostendialog.
        ///
        /// <para><b>Eine Wahrheit.</b> Der Kostendialog stellt dieselbe Frage wie die
        /// Kohärenzgruppe des BHKW-Dialogs (Anteil an der Anlage &gt; 0 <b>und</b>
        /// aktive Hilfsenergie-Kostenposition) — er darf sie deshalb nicht selbst
        /// beantworten. Die Bedingung, die Spielarten des Positionsnamens und der
        /// Wortlaut stehen in <see cref="HilfsenergieDoppelpflege"/>; hier wird nur
        /// nach einer Anlage gefragt.</para>
        ///
        /// <para>Leerer Text = keine Doppelpflege; dann zeigt der Dialog nichts.</para>
        /// </summary>
        /// <param name="idProjekt">Das Projekt der Anlage.</param>
        /// <param name="idAnlage"><c>Tab_Energieanlagen.ID</c> der gezeigten Anlage.</param>
        internal static string HilfsenergieDoppelpflege(int idProjekt, int idAnlage)
        {
            if (idProjekt <= 0 || idAnlage <= 0) return "";

            // ETAPPE E7c3 (B‑6): Scheitert die Prüfung, zeigt der Dialog ihren Grund statt
            // einer leeren Zeile — dieselbe Zeile wie in der Kohärenzgruppe.
            var liste = new List<KohaerenzHinweis>();
            Teilpruefung(TP_HILFSENERGIE,
                         () => HilfsenergieDoppelpflege(idProjekt, BerichtTexte.Kultur, liste, idAnlage),
                         BerichtTexte.Kultur, liste);

            return liste.Count > 0 ? liste[0].Text : "";
        }

        /// <summary>
        /// ETAPPE B3 Paket b — <b>Hilfsenergie an zwei Orten gepflegt</b>.
        ///
        /// <para>Seit Paket b gibt es zwei Wege, denselben Hilfsbedarf zu erfassen: den
        /// MENGENweg über <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> (Konzept § 4.5
        /// Weg B — er mindert die zuschlagsfähige Nettostromerzeugung) und den
        /// KOSTENweg über die Betriebskostenposition
        /// <c>DbWerte.VDI_POS_HILFSENERGIE</c> (Wege A und C — sie belastet die
        /// Betriebskosten). Beide dürfen nebeneinander stehen; sinnvoll ist es selten,
        /// weil dann derselbe Strom zweimal in der Rechnung erscheint: einmal als
        /// entgangener Zuschlag und einmal als Kostenposition.</para>
        ///
        /// <para><b>Nur melden, nichts verrechnen</b> — dieselbe Haltung wie in der
        /// ganzen Klasse (BF2). Welcher der beiden Wege gemeint ist, kann die Anwendung
        /// nicht wissen; sie kann nur sagen, dass beide gesetzt sind.</para>
        ///
        /// <para><b>ETAPPE E30/2 (#544, E30‑Q2 a):</b> Seit der Anteil an der Anlage selbst
        /// Hilfsenergiekosten ergibt (<see cref="HilfsenergieAusAnteil"/>), gilt bei
        /// Doppelpflege die Kostenposition — der Anteil wird dann nicht zusätzlich
        /// bepreist; die Meldung sagt das. „Aktiv" ist dieselbe Lesart wie dort.</para>
        ///
        /// <para><b>„Aktiv" heißt: die Position trägt eine Zahl</b> — einen Satz
        /// (<c>Einheitpreis</c>, Wege A und B) oder einen Jahresbetrag
        /// (<c>EingegebenerWert</c>, Weg C). Eine Vorlagenzeile mit 0 ist eine
        /// vorbereitete, keine gepflegte Position; im Bestand steht sie an fast jedem
        /// Projekt und dürfte niemals warnen.</para>
        /// </summary>
        /// <param name="nurAnlage">U31: <c>Tab_Energieanlagen.ID</c> der EINEN Anlage,
        /// nach der gefragt ist; 0 = alle Anlagen des Projekts.</param>
        private static void HilfsenergieDoppelpflege(int idProjekt, CultureInfo kultur,
                                                     List<KohaerenzHinweis> liste,
                                                     int nurAnlage = 0)
        {
            // Die billigste Frage zuerst: Gibt es überhaupt einen gepflegten Anteil? Im
            // gesamten Bestand ist die Antwort nein (alle Spalten NULL) — dann kostet die
            // Prüfung genau eine Abfrage je Projekt und endet hier. Die Spaltenprobe für
            // ID_Anlage ist ein SELECT MAX über die ganze Tabelle und läuft erst danach.
            List<HilfsstromRechner.AnlagenAnteil> mitAnteil = AnlagenMitAnteil(idProjekt);
            if (mitAnteil.Count == 0) return;

            // Ohne ID_Anlage (Datenbank vor Schritt 45) gibt es keine anlagenscharfe
            // Zuordnung — dann lässt sich die Doppelpflege nicht feststellen.
            if (!WirtschaftlichkeitCtrl.SpalteVorhanden("Tab_ProjektWerte",
                                                        SchemaKatalog.SPALTE_PW_ID_ANLAGE))
                return;

            List<int> mitPosition = AnlagenMitHilfsenergiePosition(idProjekt);
            if (mitPosition.Count == 0) return;

            foreach (HilfsstromRechner.AnlagenAnteil a in mitAnteil)
            {
                if (nurAnlage > 0 && a.IdAnlage != nurAnlage) continue;
                if (!mitPosition.Contains(a.IdAnlage)) continue;
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Text = string.Format(kultur, T("KOH_HILFSENERGIE_DOPPELT",
                            "Hilfsenergie doppelt gepflegt (Menge an der Anlage und " +
                            "Kostenposition {0}): {1} führt einen Hilfsenergieanteil von " +
                            "{2} % und zugleich eine aktive Hilfsenergie-Kostenposition. " +
                            "Die Kosten rechnet die Kostenposition; der Anteil an der Anlage " +
                            "mindert nur den KWK-Zuschlag und wird nicht zusätzlich bepreist."),
                        DbWerte.VDI_POS_HILFSENERGIE,
                        a.Bezeichner,
                        (a.AnteilProzent ?? 0).ToString("N2", kultur))
                });
            }
        }

        // =====================================================================
        // PV-Vergütung — die Herkunft je Stand (Konzept § 2.16)
        // =====================================================================

        /// <summary>
        /// Drei Lagen ohne Rechenwirkung, in denen ein Stand NICHT das rechnet, was sein
        /// Stammprojekt rechnet (Konzept § 2.16, § 3.9):
        ///
        /// <list type="number">
        ///   <item><description><b>Die Spur der Bestandsableitung</b> (VV‑Q4): Die
        ///     Variante führt eine eigene, INAKTIVE Vergütung, während der Stamm eine
        ///     aktive führt. Schemaschritt 93 hat diese Zeile angelegt, damit der Schritt
        ///     keine Zahl ändert — gerechnet wird der flache Einspeisesatz. Ein Klick auf
        ///     „vom Stammprojekt übernehmen" löst es auf.</description></item>
        ///   <item><description><b>Der leere Stamm</b>: Die Variante übernimmt, aber die
        ///     Vergütung des Stamms ist nicht angewendet. Beide rechnen Flat — das ist
        ///     richtig, sieht im Reiter aber nach einer gepflegten Übernahme
        ///     aus.</description></item>
        ///   <item><description><b>Der FEHLENDE Stamm</b>: Die Wahl „übernehmen" steht,
        ///     ein Stammprojekt gibt es aber nicht — entweder zeigt die
        ///     Variantenverknüpfung auf ein Projekt, das es nicht (mehr) gibt, oder eine
        ///     einzeln transferierte Variante trägt die Wahl ohne jede Verknüpfung. Es
        ///     gilt dann KEINE Vergütungszeile, und der Flat-Pfad liefe unbemerkt.</description></item>
        /// </list>
        ///
        /// <para><b>Nur für Stände, die übernehmen.</b> Ein Stammprojekt führt immer
        /// eigene Werte; es gibt bei ihm nichts zu vergleichen. Die billigste Frage —
        /// die Variantenverknüpfung — steht deshalb zuerst; nur die dritte Lage braucht
        /// darüber hinaus einen Blick in die eigene Zeile eines Projekts OHNE
        /// Verknüpfung.</para>
        /// </summary>
        private static void PvVerguetungHerkunft(int idProjekt, CultureInfo kultur,
                                                 List<KohaerenzHinweis> liste)
        {
            int idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt);
            var ctrl = new ProjektPhotovoltaikCtrl();
            string nameVariante = StartseiteCtrl.Projektname(idProjekt) ?? "";

            // Lage 3a: Die Verknüpfung zeigt ins Leere — das Stammprojekt gibt es nicht.
            // Lage 3b: Gar keine Verknüpfung, aber die Wahl „übernehmen" an einer eigenen,
            //          nicht angewendeten Zeile (die Spur eines Einzeltransfers).
            if (idStamm <= 0 || idStamm == idProjekt)
            {
                ProjektPhotovoltaikModel lose = ctrl.Lies(idProjekt);
                if (lose != null && lose.UebernahmeStamm && !lose.Aktiv)
                    liste.Add(StammFehltZeile(kultur, nameVariante));
                return;
            }

            string nameStamm = StartseiteCtrl.Projektname(idStamm) ?? "";
            ProjektPhotovoltaikModel eigen = ctrl.Lies(idProjekt);
            bool uebernimmt = eigen == null || eigen.UebernahmeStamm;

            if (string.IsNullOrEmpty(nameStamm))
            {
                if (uebernimmt) liste.Add(StammFehltZeile(kultur, nameVariante));
                return;
            }

            ProjektPhotovoltaikModel stamm = ctrl.Lies(idStamm);
            if (stamm == null) return;

            if (!uebernimmt && !eigen.Aktiv && stamm.Aktiv)
            {
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_PV_EIGENE_INAKTIV",
                            "Variante „{0}\" führt eine eigene, INAKTIVE PV-Vergütung, " +
                            "während das Stammprojekt „{1}\" eine aktive führt — " +
                            "gerechnet wird der flache Einspeisesatz. „Vom Stammprojekt " +
                            "übernehmen\" im Reiter Ertrag/Bonus löst es auf."),
                        nameVariante, nameStamm)
                });
                return;
            }

            if (uebernimmt && !stamm.Aktiv)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_PV_STAMM_INAKTIV",
                            "Variante „{0}\" übernimmt die PV-Vergütung des " +
                            "Stammprojekts „{1}\"; dort ist sie nicht angewendet — " +
                            "gerechnet wird der flache Einspeisesatz."),
                        nameVariante, nameStamm)
                });
        }

        /// <summary>
        /// Die Zeile zur dritten Lage: „übernehmen" ohne Stammprojekt (Konzept § 2.16).
        /// Ohne Stamm gibt es nichts zu übernehmen — es gilt keine Vergütungszeile, und
        /// gerechnet wird der flache Einspeisesatz.
        /// </summary>
        private static KohaerenzHinweis StammFehltZeile(CultureInfo kultur, string nameVariante)
        {
            return new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_PV_STAMM_FEHLT",
                        "Variante „{0}\" führt „vom Stammprojekt übernehmen\", hat aber " +
                        "kein Stammprojekt — es gilt keine Vergütungszeile, gerechnet " +
                        "wird der flache Einspeisesatz."),
                    nameVariante)
            };
        }

        /// <summary>Anlagenzeilen des Projekts mit einem Hilfsenergieanteil &gt; 0 —
        /// über <b>alle</b> Anlagenarten, weil jede Komponente Hilfsenergie haben
        /// kann (Konzept § 5.2).</summary>
        /// <remarks>ETAPPE E7c3 (B‑6): ohne eigenes <c>try</c> und über den strengen
        /// Leseweg (<see cref="StilleDb.TabelleStreng"/>) — ein Lesefehler trifft die
        /// Teilprüfung, die ihn als „nicht ausführbar" nennt. Bis E7c3 lieferte der
        /// Engine-Modus bei einem Abfragefehler eine leere Tabelle, und die Prüfung fand
        /// still nichts. Das gilt auch für die fehlende Spalte: Eine Datenbank vor
        /// Schritt 61 erreicht den Kern nicht mehr (Schemapflege beim Start), eine
        /// fehlende Spalte ist deshalb ein Lesefehler wie jeder andere.</remarks>
        private static List<HilfsstromRechner.AnlagenAnteil> AnlagenMitAnteil(int idProjekt)
        {
            var treffer = new List<HilfsstromRechner.AnlagenAnteil>();
            DataTable dt = StilleDb.TabelleStreng(
                "SELECT ID, Bezeichner, [" +
                SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] " +
                "FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                new DbParam("@p", idProjekt));

            foreach (DataRow r in dt.Rows)
            {
                object w = r[SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL];
                if (w == DBNull.Value) continue;
                double anteil = Convert.ToDouble(w);
                if (anteil <= 0) continue;
                treffer.Add(new HilfsstromRechner.AnlagenAnteil
                {
                    IdAnlage = r["ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID"]),
                    Bezeichner = r["Bezeichner"] == DBNull.Value
                               ? "" : Convert.ToString(r["Bezeichner"]).Trim(),
                    AnteilProzent = anteil
                });
            }
            return treffer;
        }

        /// <summary>
        /// <c>ID_Anlage</c> jeder AKTIVEN Hilfsenergie-Kostenposition des Projekts.
        ///
        /// <para><b>Der Namensvergleich läuft in C#, nicht in SQL.</b> Der Katalog kennt
        /// neben <c>DbWerte.VDI_POS_HILFSENERGIE</c> die Spielarten
        /// „Hilfsenergiekosten (Strom)", „… (Pumpen)", „… (Solarpumpe)" und
        /// „… (Speicherladepumpe)" — sie sind dieselbe Kostenart unter anderem Namen und
        /// müssen mitwarnen. Ein <c>LIKE</c> dafür wäre in Access mit <c>*</c> und über
        /// OLE DB mit <c>%</c> zu schreiben; diese Falle wird hier nicht aufgestellt.</para>
        /// </summary>
        /// <remarks>ETAPPE E7c3 (B‑6): ohne eigenes <c>try</c> und über den strengen
        /// Leseweg — wie <see cref="AnlagenMitAnteil"/>; ein Lesefehler wird zur Zeile
        /// „nicht ausführbar" der Teilprüfung.</remarks>
        private static List<int> AnlagenMitHilfsenergiePosition(int idProjekt)
        {
            var treffer = new List<int>();
            DataTable dt = StilleDb.TabelleStreng(
                "SELECT f.Bezeichnung, w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "], " +
                "w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], w.EingegebenerWert " +
                "FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                "ON w.StammID = f.StammID " +
                "WHERE w.ProjektID = ? AND w.KategorieID = 2",
                new DbParam("@p", idProjekt));

            foreach (DataRow r in dt.Rows)
            {
                string name = r["Bezeichnung"] == DBNull.Value
                            ? "" : Convert.ToString(r["Bezeichnung"]).Trim();
                if (!name.StartsWith(DbWerte.VDI_POS_HILFSENERGIE,
                                     StringComparison.OrdinalIgnoreCase)) continue;

                object ida = r[SchemaKatalog.SPALTE_PW_ID_ANLAGE];
                if (ida == DBNull.Value) continue;
                int idAnlage = Convert.ToInt32(ida);
                if (idAnlage <= 0 || treffer.Contains(idAnlage)) continue;

                if (!Aktiv(r[SchemaKatalog.SPALTE_PW_EINHEITPREIS]) &&
                    !Aktiv(r["EingegebenerWert"])) continue;

                treffer.Add(idAnlage);
            }
            return treffer;
        }

        /// <summary>Ein Zahlenfeld ist „gepflegt", wenn es weder NULL noch 0 ist. Ein Wert,
        /// der keine Zahl ist, gilt als nicht gepflegt (ETAPPE E7c3: benannt statt
        /// <c>catch { }</c> — nur die Fehler der Zahlumwandlung).</summary>
        private static bool Aktiv(object wert)
        {
            if (wert == null || wert == DBNull.Value) return false;
            try { return Math.Abs(Convert.ToDouble(wert)) > 1e-9; }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                return false;
            }
        }

        // =====================================================================
        // CO₂ — Doppelansatz Arbeitspreis gegen BEHG-Reihe, Strommix-Rückfall
        // =====================================================================

        /// <summary>
        /// ETAPPE E2 (Befund R5, Mockup-Prüfung <c>01/B1</c>) — <b>CO₂ zweimal
        /// gebucht</b>.
        ///
        /// <para>Der Arbeitspreis eines Brennstoffträgers darf den CO₂-Anteil nach BEHG
        /// als Preisbestandteil ausweisen (<c>energy_project_settings</c>, Spalte
        /// <see cref="SchemaKatalog.SPALTE_BB_CO2"/> mit gesetztem Aktiv-Schalter). Dann
        /// steckt die Abgabe bereits in den Energiekosten. Bucht der Lauf zusätzlich
        /// eine BEHG-Reihe (<c>KapitalwertRechner.Rechne</c> addiert sie auf die
        /// Energiekosten), steht dieselbe Abgabe zweimal im Kapitalwert.</para>
        ///
        /// <para><b>Nur melden, nichts verrechnen</b> — dieselbe Haltung wie in der
        /// ganzen Klasse (BF2), und ausdrücklich der Anwenderentscheid zu Frage Q3 der
        /// Mockup-Prüfung: <i>Weg (a)</i>, die BEHG-Zeile bleibt, sie wird
        /// gekennzeichnet. Der genannte Betrag ist deshalb die TATSÄCHLICH gebuchte
        /// CO₂-Abgabe des Jahres 1, keine Zweitrechnung.</para>
        ///
        /// <para><b>Warum die Trägerliste aus der Preistabelle und nicht aus den
        /// Steueranlagen kommt:</b> Der Doppelansatz hängt an keinem Steuerpfad. Gefragt
        /// sind die Träger, für die das PROJEKT einen aktiven CO₂-Anteil gepflegt hat;
        /// dass überhaupt Brennstoff verbrannt wird, sagt die gebuchte Abgabe selbst
        /// (<c>Co2AbgabeEur &gt; 0</c>).</para>
        /// </summary>
        private static void Co2DoppelansatzBehg(int idProjekt, KohaerenzLauf lauf,
                                                CultureInfo kultur, List<KohaerenzHinweis> liste)
        {
            if (lauf.Co2AbgabeEur <= 0) return;

            List<string> mitAnteil = TraegerMitCo2Anteil(idProjekt);
            if (mitAnteil.Count == 0) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.WARNUNG,
                Betrag = lauf.Co2AbgabeEur,
                Text = string.Format(kultur, T("KOH_CO2_DOPPELT",
                        "CO₂ doppelt angesetzt: Der erfasste Arbeitspreis weist einen aktiven " +
                        "CO₂-Bestandteil nach BEHG aus ({0}), und der Lauf bucht zusätzlich eine " +
                        "CO₂-Abgabe von {1} €/a. Derselbe Betrag steht damit zweimal in den " +
                        "Energiekosten — verrechnet wird nichts."),
                    string.Join(", ", mitAnteil.ToArray()),
                    lauf.Co2AbgabeEur.ToString("N2", kultur))
            });
        }

        /// <summary>
        /// Energieträger des Projekts, deren Arbeitspreis einen AKTIVEN CO₂-Anteil
        /// &gt; 0 ausweist — dieselbe Leseregel wie
        /// <see cref="BrennstoffBestandteilCtrl.Read"/> (NULL heißt „kein Anteil
        /// erfasst", der Aktiv-Schalter entscheidet über die Verwendung).
        /// </summary>
        /// <remarks>ETAPPE E7c3 (B‑6): ohne eigenes <c>try</c> und über den strengen
        /// Leseweg (<see cref="StilleDb.TabelleStreng"/>) — ein Lesefehler wird zur
        /// Zeile „nicht ausführbar" der Teilprüfung CO₂-Doppelansatz.</remarks>
        private static List<string> TraegerMitCo2Anteil(int idProjekt)
        {
            var namen = new List<string>();
            DataTable dt = StilleDb.TabelleStreng(
                "SELECT * FROM [" + BrennstoffBestandteilCtrl.TABLE + "] WHERE ID_Projekt = ?",
                new DbParam("@proj", idProjekt));

            if (dt == null || dt.Rows.Count == 0) return namen;
            if (!dt.Columns.Contains(SchemaKatalog.SPALTE_BB_CO2)) return namen;

            foreach (DataRow r in dt.Rows)
            {
                double? wert = null; bool aktiv = false;
                Preisanteile.PaarNullbar(dt, r, SchemaKatalog.SPALTE_BB_CO2, ref wert, ref aktiv);
                if (!wert.HasValue || !aktiv || wert.Value <= 0) continue;

                // Der Spaltenname steht EINMAL — buchstabengetreu mit Umlaut, wie
                // BETRIEB_SQLITE.md 6.1 es verlangt.
                object id = r[ProjektEnergietraegerEindeutig.SPALTE_TRAEGER];
                if (id == null || id == DBNull.Value) continue;
                string name = TraegerName(Convert.ToInt32(id));
                if (!namen.Contains(name)) namen.Add(name);
            }
            return namen;
        }

        /// <summary>
        /// ETAPPE E2 (Befund R6) — <b>Strommix-Vorgabewert statt zugeordnetem
        /// Stromträger</b>. Bis E2 stand die Lage als gewöhnlicher Laufhinweis im
        /// Hinweisfeld des Ergebnisses, <b>ohne</b> die Zahl; § 3.9 des Konzepts führt
        /// sie als Zeile der Kohärenzgruppe. Hier steht sie mit ihrem Wert und ihrer
        /// Schwere — und erreicht damit dieselben drei Ausgaben wie jede andere
        /// Kohärenzzeile (Rubrik, Wort- und Excelbericht).
        /// </summary>
        private static void StrommixRueckfall(KohaerenzLauf lauf, CultureInfo kultur,
                                              List<KohaerenzHinweis> liste)
        {
            if (!lauf.StrommixRueckfallGJeKwh.HasValue) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_CO2_STROMMIX_RUECKFALL",
                        "CO₂-Bilanz: Dem Projekt ist kein Strom-Energieträger zugeordnet — der " +
                        "Netzbezug ist mit dem Strommix-Vorgabewert von {0} g CO₂/kWh gerechnet."),
                    lauf.StrommixRueckfallGJeKwh.Value.ToString("N0", kultur))
            });
        }

        // =====================================================================
        // ETAPPE E7c — die Datenlücken der KWKG-Rechnung
        // =====================================================================

        /// <summary>
        /// ETAPPE E7c — <b>die Datenlücken der KWKG-Rechnung</b> als Zeilen der
        /// Kohärenzgruppe, je Art EINE Zeile mit den betroffenen Anlagen.
        ///
        /// <para><b>„Anlagenart fehlt"</b> (§ 6.3 Nr. 30, Entscheid E7‑Q1 Lesart b): Das
        /// Kontingent einer Anlage war nach § 8 aus ihrer Anlagenart abzuleiten — es ist
        /// nicht gepflegt —, und die Anlagenart fehlt; der Zuschlag der Anlage ist 0. Ein
        /// gepflegtes Kontingent bleibt wirksam und löst die Zeile NICHT aus.</para>
        ///
        /// <para><b>„Stromkennzahl fehlt"</b> (Befund K‑1, Entscheid E7‑Q2 (2) mit Auflage):
        /// Eine Anlage trägt das Kennzeichen „Vorrichtung zur Abwärmeabfuhr", aber weder
        /// eine gepflegte Stromkennzahl noch P_el und P_th in der Gerätezeile — dann gibt es
        /// keinen Ersatzwert, sondern keinen KWK-Strom nach Fall 2, und der Zuschlag der
        /// Anlage ist 0.</para>
        ///
        /// <para><b>Schwere HINWEIS, ohne Betrag</b> — wie die vergleichbaren Zeilen, die
        /// eine Datenlage und ihre Folge nennen (Fall 3 „keine Entlastung gewählt",
        /// Strommix-Rückfall, PV-Vergütungsherkunft): Den entgangenen Zuschlag zu beziffern
        /// hieße, ihn ein zweites Mal zu rechnen — mit einem Wert, den es nicht gibt.</para>
        /// </summary>
        private static void KwkgDatenluecken(KohaerenzLauf lauf, CultureInfo kultur,
                                             List<KohaerenzHinweis> liste)
        {
            if (lauf.Kwkg == null) return;

            if (lauf.Kwkg.OhneAnlagenart.Count > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_KWKG_ANLAGENART_FEHLT",
                            "Anlagenart fehlt: Für {0} ist weder ein Vbh-Kontingent gepflegt noch " +
                            "eine Anlagenart erfasst — § 8 KWKG leitet ohne Anlagenart kein " +
                            "Kontingent ab, für diese Anlage wird kein Zuschlag gerechnet. " +
                            "Anlagenart oder Kontingent im BHKW-Dialog eintragen."),
                        Aufzaehlung(lauf.Kwkg.OhneAnlagenart))
                });

            if (lauf.Kwkg.OhneStromkennzahl.Count > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_KWKG_STROMKENNZAHL_FEHLT",
                            "Stromkennzahl fehlt: {0} trägt das Kennzeichen „Vorrichtung zur " +
                            "Abwärmeabfuhr“, aber weder eine gepflegte Stromkennzahl noch P_el und " +
                            "P_th in der Gerätezeile — KWK-Strom nach § 2 Nr. 16 KWKG ist nicht " +
                            "bestimmbar, für diese Anlage wird kein Zuschlag gerechnet. Die " +
                            "Stromkennzahl steht im BHKW-Dialog unter „Sätze und Herkunft“."),
                        Aufzaehlung(lauf.Kwkg.OhneStromkennzahl))
                });
        }

        /// <summary>Anlagenbezeichner als Aufzählung in Anführungszeichen — die Zeichen
        /// sind typografische Marken ohne Wortbestand und bleiben deshalb im Code
        /// (Drei-Schichten-Regel).</summary>
        private static string Aufzaehlung(List<string> bezeichner)
        {
            var teile = new List<string>();
            foreach (string b in bezeichner) teile.Add("„" + b + "“");
            return string.Join(", ", teile.ToArray());
        }

        // =====================================================================
        // Brennstoffseite — Energiesteuer § 53 / § 53a Abs. 5 / § 54
        // =====================================================================

        /// <summary>
        /// Vergleicht die gebuchte Energiesteuer-Entlastung mit dem Energiesteueranteil,
        /// den die Brennstoffpreise der beteiligten Träger ausweisen.
        ///
        /// <para><b>Eine Zeile je Fall, nicht je Träger.</b> Der gebuchte Betrag ist eine
        /// Projektgröße; ihn auf mehrere Träger aufzuteilen wäre eine Rechnung, die es
        /// nicht gibt. Die betroffenen Träger stehen deshalb aufgezählt in EINER Zeile.
        /// Fall 4 dagegen vergleicht Sätze und bekommt je Träger eine eigene Zeile.</para>
        /// </summary>
        private static void Brennstoffseite(int idProjekt, KohaerenzLauf lauf,
                                            CultureInfo kultur, List<KohaerenzHinweis> liste)
        {
            // Ein Träger, so oft er auch in Anlagen vorkommt, wird einmal geprüft.
            var traeger = new List<SteuerAnlage>();
            var gesehen = new List<int>();
            foreach (SteuerAnlage a in lauf.Steuer.Anlagen)
            {
                if (a == null || a.CarrierId <= 0 || a.BrennstoffMWh <= 0) continue;
                if (gesehen.Contains(a.CarrierId)) continue;
                gesehen.Add(a.CarrierId);
                traeger.Add(a);
            }
            if (traeger.Count == 0) return;

            var ctrl = new BrennstoffBestandteilCtrl();
            var ohneAnteil = new List<string>();     // Träger ohne ausgewiesene Energiesteuer
            var mitAnteil = new List<string>();      // Träger mit ausgewiesener Energiesteuer

            foreach (SteuerAnlage a in traeger)
            {
                BrennstoffBestandteilModel m = ctrl.Read(idProjekt, a.CarrierId);
                string name = TraegerName(a.CarrierId);

                if (!m.Energiesteuer.HasValue)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_FEHLT", "kein Anteil erfasst"))); continue; }
                if (!m.Energiesteuer_Aktiv)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_INAKTIV", "Anteil abgeschaltet"))); continue; }
                if (m.Energiesteuer.Value <= 0)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_NULL", "Anteil 0 ct/kWh"))); continue; }

                mitAnteil.Add(name);

                // ---- Fall 4: gepflegter Satz gegen den Katalogsatz des Jahres ----
                Fall4Brennstoff(a, name, m.Energiesteuer.Value, lauf.Jahr, kultur, liste);
            }

            // ---- Fall 2: Entlastung gebucht, im Preis nicht ausgewiesen ----
            if (lauf.EnergiesteuerEur > 0 && ohneAnteil.Count > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Betrag = lauf.EnergiesteuerEur,
                    Text = string.Format(kultur, T("KOH_FALL2_ENERGIESTEUER",
                            "Die Energiesteuer-Gutschrift von {0} €/a setzt voraus, dass der erfasste " +
                            "Brennstoffpreis die Energiesteuer enthält. Im Preis ist sie nicht " +
                            "ausgewiesen: {1}."),
                        lauf.EnergiesteuerEur.ToString("N2", kultur),
                        string.Join(", ", ohneAnteil.ToArray()))
                });

            // ---- Fall 3: Anteil ausgewiesen, aber keine Entlastung gewählt ----
            //
            // Bewusst an der WAHL festgemacht, nicht am Betrag: Eine gewählte Norm, die
            // an einer Bedingung scheitert (Nutzungsgrad, Unternehmensart, Einheit),
            // begründet SteuerGutschriftRechner bereits selbst. Eine zweite Meldung
            // darüber wäre Rauschen.
            //
            // ETAPPE B3 Paket a: „gewählt" heißt seither Projektwahl ODER Anlagenwahl
            // (BF6). Ohne diese Erweiterung meldete die Prüfung „keine Entlastung
            // gewählt", während eine Anlage längst nach § 53 entlastet wird — genau der
            // Widerspruch, den sie aufdecken soll.
            if (mitAnteil.Count > 0 && !EntlastungGewaehlt(lauf.Steuer))
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_FALL3_ENERGIESTEUER",
                            "Der erfasste Brennstoffpreis weist eine Energiesteuer aus, es ist aber " +
                            "keine Entlastung gewählt (§ 53 / § 53a Abs. 5 / § 54): {0}."),
                        string.Join(", ", mitAnteil.ToArray()))
                });

            // ---- Fall 1: es stimmt (ETAPPE B6) ----
            //
            // Die Bedingung ist die GENAUE Umkehrung der beiden Fälle darüber: eine
            // gewählte Entlastung, ein ausgewiesener Anteil bei JEDEM beteiligten
            // Träger — und kein Träger, der ohne Anteil dasteht. Die Zeile trägt
            // KEINEN Betrag: Sie bestätigt die Herkunft, sie bilanziert nicht.
            if (mitAnteil.Count > 0 && ohneAnteil.Count == 0 && EntlastungGewaehlt(lauf.Steuer))
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.BESTAETIGUNG,
                    Text = string.Format(kultur, T("KOH_FALL1_ENERGIESTEUER",
                            "Energiesteuer: Wahl und Preisanteil stimmen überein ({0})."),
                        string.Join(", ", mitAnteil.ToArray()))
                });
        }

        /// <summary>
        /// ETAPPE B3 Paket a — true, sobald IRGENDEINE Entlastungsnorm im Spiel ist:
        /// entweder als Projektwahl oder als eigene Wahl einer Anlage
        /// (<c>Tab_Energieanlagen.Energiesteuer_Wahl</c>, BF6).
        ///
        /// <para>Die Aufteilungsmethode bleibt außen vor — sie sagt nur, WIE bemessen
        /// wird, nicht OB entlastet wird.</para>
        /// </summary>
        private static bool EntlastungGewaehlt(SteuerEingabe e)
        {
            if (Gewaehlt(e.EnergiesteuerWahl)) return true;
            foreach (SteuerAnlage a in e.Anlagen)
                if (a != null && Gewaehlt(a.EnergiesteuerWahl)) return true;
            return false;
        }

        /// <summary>Eine Wahl ist gesetzt, wenn sie weder leer noch
        /// <c>KEINE</c> ist — dasselbe Rückfallmuster wie im
        /// <see cref="SteuerGutschriftRechner"/>.</summary>
        private static bool Gewaehlt(string wahl)
        {
            if (string.IsNullOrEmpty(wahl)) return false;
            string w = wahl.Trim();
            return w.Length > 0 &&
                   !string.Equals(w, DbWerte.ENERGIESTEUER_WAHL_KEINE, StringComparison.Ordinal);
        }

        /// <summary>
        /// Fall 4 für einen Brennstoffträger: gepflegter Anteil gegen den <b>Regelsatz
        /// nach § 2 EnergieStG</b> des Jahres.
        ///
        /// <para><b>Warum gegen § 2 und nicht gegen den Entlastungssatz.</b> Im Preis
        /// steckt der volle Steuersatz — die Entlastung nach § 53a Abs. 5 bzw. § 54 ist
        /// die Rückerstattung eines Teils davon. Ein Vergleich gegen den Teilsatz würde
        /// jeden korrekt erfassten Preis als Abweichung melden.</para>
        ///
        /// <para>Ohne zugeordneten Katalogschlüssel (Biogas, Holz, Fernwärme …), ohne
        /// Satz im Jahr oder ohne belegbare Einheitenumrechnung entsteht KEINE Zeile:
        /// Eine geratene Vergleichszahl wäre schlimmer als keine.</para>
        /// </summary>
        private static void Fall4Brennstoff(SteuerAnlage a, string name, double anteilCtKwh,
                                            int jahr, CultureInfo kultur,
                                            List<KohaerenzHinweis> liste)
        {
            if (string.IsNullOrEmpty(a.SchluesselSatzVoll)) return;

            GesetzParameter p = new GesetzKatalog().WertMitHerkunft(a.SchluesselSatzVoll, jahr);
            if (p == null || !p.Wert.HasValue) return;

            double? katalogCt = InCtKwh(p.Wert.Value, p.Einheit, a);
            if (!katalogCt.HasValue)
            {
                // ETAPPE B6 - offener Punkt 6 der Liste "Nach B6". Bis hierher schwieg
                // Fall 4 in JEDER Lage, in der die Umrechnung nicht traegt. Eine davon
                // ist erklaerbar und haeufig genug, um benannt zu werden: Der
                // Katalogsatz steht je 1.000 kg, das Projekt rechnet je Liter (oder
                // umgekehrt). Die Bruecke braeuchte die Dichte, und
                // energy_carrier.density ist im ganzen Bestand leer - der Pruefer KANN
                // hier nicht vergleichen. Das ist etwas anderes als "kein Befund", und
                // der Anwender soll es wissen.
                EinheitNichtVergleichbar(p, a, name, jahr, kultur, liste);
                return;
            }
            if (Math.Abs(anteilCtKwh - katalogCt.Value) <= TOLERANZ_CT_KWH) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL4_ENERGIESTEUER",
                        "{0}: Der erfasste Energiesteueranteil {1} ct/kWh weicht vom Katalogsatz " +
                        "{2} {3} des Jahres {4} ab — das sind {5} ct/kWh."),
                    name,
                    anteilCtKwh.ToString("N4", kultur),
                    p.Wert.Value.ToString("N2", kultur), p.Einheit,
                    jahr.ToString(CultureInfo.InvariantCulture),
                    katalogCt.Value.ToString("N4", kultur))
            });
        }

        /// <summary>
        /// ETAPPE B6 — <b>Der Satz steht in einer Einheit, die sich nicht umrechnen
        /// laesst.</b> Die gesetzlichen Saetze der Heizstoffe stehen teils je 1.000 kg,
        /// teils je 1.000 Liter; ein Projekt rechnet in genau einer dieser Einheiten.
        /// Passen sie nicht zusammen, braeuchte die Bruecke die Dichte des Traegers —
        /// und <c>energy_carrier.density</c> ist im gesamten Bestand leer.
        ///
        /// <para><b>Die Zeile sagt genau das und nichts weiter</b> (BF2): Sie nennt den
        /// Traeger, die Einheit des Katalogsatzes und die Abrechnungseinheit des
        /// Projekts. Sie rechnet nichts, sie warnt nicht — sie unterscheidet
        /// „geprueft, keine Abweichung" von „nicht pruefbar".</para>
        ///
        /// <para>Nur fuer die beiden Tausender-Einheiten; jede andere Luecke
        /// (Satz fehlt, kein Heizwert, kein Katalogschluessel) bleibt stumm, weil sie
        /// keine erklaerbare Ursache hat, die dem Anwender hilft.</para>
        /// </summary>
        private static void EinheitNichtVergleichbar(GesetzParameter p, SteuerAnlage a, string name,
                                                     int jahr, CultureInfo kultur,
                                                     List<KohaerenzHinweis> liste)
        {
            if (a == null || a.EffHi <= 0) return;

            string e = (p.Einheit ?? "").Trim();
            string erwartet =
                string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase) ? "kg"
              : string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase) ? "l"
              : null;
            if (erwartet == null) return;

            string ist = (a.Abrechnungseinheit ?? "").Trim();
            if (ist.Length == 0) return;                                   // gar keine Angabe: andere Luecke
            if (string.Equals(ist, erwartet, StringComparison.OrdinalIgnoreCase)) return;   // passt doch

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL4_EINHEIT_UNVERGLEICHBAR",
                        "{0}: Der Katalogsatz des Jahres {1} steht in {2}, das Projekt rechnet " +
                        "je {3}. Ohne Dichte des Energieträgers lässt sich beides nicht " +
                        "ineinander umrechnen — der Energiesteueranteil im Preis bleibt hier " +
                        "ungeprüft."),
                    name,
                    jahr.ToString(CultureInfo.InvariantCulture),
                    p.Einheit, ist)
            });
        }

        // =====================================================================
        // Fall 5 — Mischlage § 53 / § 53a Abs. 5 neben § 54
        // =====================================================================

        /// <summary>
        /// PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2) —
        /// <b>Fall 5: zwei Entlastungswelten im selben Projekt.</b>
        ///
        /// <para>Seit B3a wählt jede Anlage ihre Entlastungsnorm selbst (Rückfall:
        /// Projektwahl). Damit ist eine Lage möglich, die es vorher nicht gab: eine
        /// Anlage rechnet nach § 53 / § 53a Abs. 5 EnergieStG (Entlastung für die
        /// Stromerzeugung bzw. die gekoppelte Erzeugung), eine andere nach § 54
        /// (Heizstoffe im produzierenden Gewerbe). Rechnerisch ist das sauber — jede
        /// Anlage bringt ihre eigene Brennstoffmenge mit, nichts wird zweimal
        /// entlastet. Im ANTRAG ist es eine Stolperstelle: § 54 nimmt Mengen aus, die
        /// bereits nach § 53 / § 53a entlastet wurden, und die Verfahren laufen als
        /// getrennte Anträge beim Hauptzollamt.</para>
        ///
        /// <para><b>Nur melden, nichts sperren, nichts verrechnen</b> — dieselbe Haltung
        /// wie in der ganzen Klasse (BF2). Die Zeile ändert keine Zahl; sie benennt die
        /// beteiligten Anlagen samt ihrer Wahl und deren Herkunft (Projekt- oder
        /// Anlagenwahl).</para>
        ///
        /// <para><b>Angesetzt wird an der WAHL, nicht am gebuchten Betrag</b> — genau wie
        /// in Fall 3. Eine gewählte Norm, die an einer Bedingung scheitert
        /// (Unternehmensart, Nutzungsgrad, Sockelbetrag), begründet der
        /// <see cref="SteuerGutschriftRechner"/> bereits selbst; eine zweite Meldung
        /// darüber wäre Rauschen. Ein Betrag steht deshalb NICHT an der Zeile: Die
        /// gebuchte Energiesteuer-Entlastung ist die Summe BEIDER Seiten und würde als
        /// „betroffener Betrag" in die Irre führen.</para>
        ///
        /// <para><b>Ohne Brennstoffeinsatz keine Zeile.</b> Eine Anlage mit
        /// <c>BrennstoffMWh = 0</c> bringt keine Menge mit, die doppelt entlastet werden
        /// könnte — dieselbe Vorbedingung wie in <see cref="Brennstoffseite"/>.</para>
        /// </summary>
        private static void MischlageEnergiesteuer(KohaerenzLauf lauf, CultureInfo kultur,
                                                   List<KohaerenzHinweis> liste)
        {
            // ETAPPE E7c — S‑2 (Entscheid A3 vom 20.09.2026): Die Mischlage ist
            // GESPERRT — der Steuerrechner verwirft den § 54-Betrag. Welche Anlagen auf
            // welcher Seite stehen, sagt DIESELBE Prüfung, die dort sperrt
            // (SteuerGutschriftRechner.Mischlage); die Zeile steht also genau dann, wenn
            // die Sperre greift. Schwere WARNUNG statt HINWEIS: Die Zeile benennt eine
            // Rechenwirkung, keine bloße Stolperstelle im Antrag.
            List<SteuerAnlage> strom, gewerbe;
            if (!SteuerGutschriftRechner.Mischlage(lauf.Steuer, out strom, out gewerbe)) return;

            var seiteStrom = new List<string>();   // § 53 / § 53a Abs. 5
            var seiteGewerbe = new List<string>(); // § 54

            foreach (SteuerAnlage a in strom)
            {
                bool eigen;
                string wahl = WirksameWahl(a, lauf.Steuer, out eigen);
                string norm = string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_53A, StringComparison.Ordinal)
                    ? T("KOH_NORM_53A", "§ 53a Abs. 5") : T("KOH_NORM_53", "§ 53");
                seiteStrom.Add(MitGrund(AnlagenName(a), norm + ", " + Herkunft(eigen)));
            }
            foreach (SteuerAnlage a in gewerbe)
            {
                bool eigen;
                WirksameWahl(a, lauf.Steuer, out eigen);
                seiteGewerbe.Add(MitGrund(AnlagenName(a), T("KOH_NORM_54", "§ 54") + ", " + Herkunft(eigen)));
            }

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.WARNUNG,
                Text = string.Format(kultur, T("KOH_FALL5_MISCHLAGE_SPERRE",
                        "Im Projekt stehen zwei Entlastungswelten nebeneinander: {0} gegen {1}. " +
                        "Diese Mischlage ist gesperrt — der § 54-Betrag wird verworfen (0 €), " +
                        "gerechnet wird allein die Entlastung nach § 53 / § 53a Abs. 5. § 54 " +
                        "EnergieStG nimmt Mengen aus, die bereits nach § 53 / § 53a entlastet " +
                        "wurden; wer beide Entlastungen beantragen will, wählt für jede Anlage " +
                        "dieselbe Welt."),
                    string.Join(", ", seiteStrom.ToArray()),
                    string.Join(", ", seiteGewerbe.ToArray()))
            });
        }

        /// <summary>ETAPPE E7c: „Anlagenwahl" bzw. „Projektwahl" — woher die Wahl stammt.</summary>
        private static string Herkunft(bool eigen)
        {
            return eigen ? T("KOH_HERKUNFT_ANLAGE", "Anlagenwahl")
                         : T("KOH_HERKUNFT_PROJEKT", "Projektwahl");
        }

        /// <summary>
        /// PAKET FX5-b — die für eine Anlage GELTENDE Entlastungsnorm: ihr eigener Wert,
        /// ersatzweise der Projektwert (B3a, BF6). <paramref name="eigen"/> sagt, welcher
        /// der beiden es war.
        /// <para><b>Spiegelt <c>SteuerGutschriftRechner.Wahl</c></b>, das dort privat ist.
        /// Dieselbe Regel, dieselbe Behandlung von <c>null</c> und Leerstring — wer eine
        /// von beiden ändert, muss die andere mitziehen.</para>
        /// </summary>
        private static string WirksameWahl(SteuerAnlage a, SteuerEingabe e, out bool eigen)
        {
            string wahl = a.EnergiesteuerWahl == null ? null : a.EnergiesteuerWahl.Trim();
            eigen = !string.IsNullOrEmpty(wahl);
            return eigen ? wahl : e.EnergiesteuerWahl;
        }

        /// <summary>Anzeigename einer Anlagenzeile; ohne Bezeichner der Trägername.</summary>
        private static string AnlagenName(SteuerAnlage a)
        {
            string s = (a.Bezeichner ?? "").Trim();
            return s.Length > 0 ? s : TraegerName(a.CarrierId);
        }

        // =====================================================================
        // Stromseite — Stromsteuer § 9b und § 9 Abs. 1 Nr. 3
        // =====================================================================

        /// <summary>
        /// Vergleicht die gebuchten Stromsteuergutschriften mit dem Stromsteueranteil der
        /// Preiszerlegung „Strompreis Details".
        ///
        /// <para><b>Der Aktiv-Schalter des Anteils entscheidet.</b> Seit SP-E-2 zerlegen
        /// die Anteile den Arbeitspreis; ein aktiver Stromsteueranteil &gt; 0 heißt
        /// damit: Die Stromsteuer steckt im angesetzten Bezugspreis. Es gibt keinen
        /// Projektschalter mehr, der das ganze Feld abschalten könnte.</para>
        /// </summary>
        private static void Stromseite(int idProjekt, KohaerenzLauf lauf,
                                       CultureInfo kultur, List<KohaerenzHinweis> liste)
        {
            bool gebucht9b = lauf.StromsteuerEntlastungEur > 0;
            bool gebucht913 = lauf.StromsteuerBefreiungEur > 0;
            bool prodGewerbe =
                string.Equals(lauf.Steuer.Unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                              StringComparison.Ordinal) ||
                string.Equals(lauf.Steuer.Unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                              StringComparison.Ordinal);

            // Nichts gebucht und keine Entlastungsberechtigung — es gibt nichts zu prüfen
            // und nichts zu melden (sonst stünde an jedem Projekt eine Zeile).
            if (!gebucht9b && !gebucht913 && !prodGewerbe) return;

            int carrier = StrompreisZerlegungCtrl.StromCarrierId(idProjekt);
            if (carrier <= 0)
            {
                // ANWENDERENTSCHEID 22.09.2026 — DIESELBE REGEL WIE AUF DER KOSTENSEITE.
                // Führt das Projekt keinen Erzeuger, der Strom verwendet, dann FEHLT der
                // Stromträger nicht, er wird nicht gebraucht: Ein Befund darüber wäre eine
                // Aufgabe ohne Gegenstand. Gefragt wird die EINE Fassung
                // (ProjektEnergietraegerCtrl), nicht eine zweite hier.
                if (!ProjektEnergietraegerCtrl.BrauchtStromTraeger(idProjekt)) return;

                Fall2Strom(lauf, T("KOH_GRUND_KEIN_STROMTRAEGER",
                    "dem Projekt ist kein Strom-Energieträger zugeordnet"), kultur, liste);
                return;
            }

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(idProjekt, carrier);

            string grund = null;
            if (!m.AusDatenbank)
                grund = T("KOH_GRUND_KEIN_STROMTRAEGER",
                    "dem Projekt ist kein Strom-Energieträger zugeordnet");
            else if (!m.Stromsteuer_Aktiv)
                grund = T("KOH_GRUND_STROM_INAKTIV", "die Komponente Stromsteuer ist abgeschaltet");
            else if (m.Stromsteuer <= 0)
                grund = T("KOH_GRUND_STROM_NULL", "die Komponente Stromsteuer steht auf 0 ct/kWh");

            if (grund != null) { Fall2Strom(lauf, grund, kultur, liste); return; }

            // ---- Ab hier ist die Stromsteuer im angesetzten Preis enthalten ----

            // Fall 3: Belastung ohne Entlastung — nur für Berechtigte und nur bei
            // vorhandenem Netzbezug (ohne Bezug gibt es nichts zu entlasten; den Sockel
            // von 250 €/a begründet SteuerGutschriftRechner selbst).
            if (!gebucht9b && prodGewerbe && lauf.Steuer.NetzbezugMWh > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_FALL3_STROMSTEUER",
                            "Der erfasste Strompreis weist eine Stromsteuer von {0} ct/kWh aus, " +
                            "eine Entlastung nach § 9b StromStG wird nicht gebucht."),
                        m.Stromsteuer.ToString("N4", kultur))
                });

            // Fall 4: nur gegen einen WIRKLICH gepflegten Wert (siehe Klassenkommentar).
            double? roh = StrompreisZerlegungCtrl.StromsteuerRoh(idProjekt, carrier);
            if (roh.HasValue) Fall4Strom(roh.Value, lauf.Jahr, kultur, liste);
        }

        /// <summary>
        /// Die beiden Fall-2-Zeilen der Stromseite — je Vorschrift eine, mit dem
        /// tatsächlich gebuchten Betrag.
        /// </summary>
        private static void Fall2Strom(KohaerenzLauf lauf, string grund, CultureInfo kultur,
                                       List<KohaerenzHinweis> liste)
        {
            if (lauf.StromsteuerEntlastungEur > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Betrag = lauf.StromsteuerEntlastungEur,
                    Text = string.Format(kultur, T("KOH_FALL2_STROMST_9B",
                            "Die Stromsteuer-Entlastung nach § 9b StromStG von {0} €/a setzt voraus, " +
                            "dass der erfasste Strompreis die Stromsteuer enthält. Im Preis ist sie " +
                            "nicht ausgewiesen ({1})."),
                        lauf.StromsteuerEntlastungEur.ToString("N2", kultur), grund)
                });

            // ETAPPE B6 — hier stand bis B5 eine zweite Zeile zu § 9 Abs. 1 Nr. 3: Die
            // Befreiung werde als Erlös gebucht, obwohl der Preis die Stromsteuer nicht
            // ausweise. Sie ist entfallen, und zwar ersatzlos, weil
            // DoppelzaehlungBefreiung dasselbe sagt und mehr: Der Einwand gilt dem
            // Buchen als Erlös überhaupt, nicht erst dem Preisanteil, und die Bedingung
            // „nur richtig, wenn der Bezugspreis die Stromsteuer enthält" steht in ihrem
            // Text. Zwei Warnungen zu EINER Sache sind keine doppelte Sorgfalt, sondern
            // eine Zumutung — und im Modus AUSWEIS (Vorgabe) wäre die alte Zeile
            // ohnehin falsch gewesen: Dass der Preis die Stromsteuer nicht enthält, ist
            // dann kein Widerspruch, sondern genau die Lage, die den Ausweis richtig
            // macht.
        }

        /// <summary>
        /// ETAPPE B6 — <b>Doppelzählung § 9 Abs. 1 Nr. 3 StromStG</b>: Der Modus ERLOES
        /// bucht die Befreiung als Erlösreihe in den Kapitalwert. Auf selbst erzeugten
        /// und selbst verbrauchten Strom entsteht aber gar keine Stromsteuer — der
        /// Vorteil steckt bereits in der kleineren Bezugsrechnung. Die Zeile steht
        /// deshalb IMMER, wenn der Modus ERLOES einen Betrag bucht; sie hängt nicht am
        /// Preisanteil, denn der Einwand gilt unabhängig davon.
        ///
        /// <para><b>Nur warnen</b> (BF2): Der Anwender kann die Lage kennen — etwa wenn
        /// sein Bezugspreis die Stromsteuer auf den Eigenverbrauch mitträgt. Die
        /// Rechnung bleibt, wie er sie gewählt hat.</para>
        /// </summary>
        private static void DoppelzaehlungBefreiung(KohaerenzLauf lauf, CultureInfo kultur,
                                                    List<KohaerenzHinweis> liste)
        {
            if (lauf == null || !lauf.StromsteuerBefreiungAlsErloes) return;
            if (lauf.StromsteuerBefreiungEur <= 0) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.WARNUNG,
                Betrag = lauf.StromsteuerBefreiungEur,
                Text = string.Format(kultur, T("KOH_DOPPEL_STROMST_9_1_3",
                        "Doppelzählung möglich: Die Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 " +
                        "StromStG von {0} €/a ist als Erlös gebucht. Der Vorteil steckt bereits in " +
                        "der kleineren Bezugsrechnung — als Erlös ist er nur richtig, wenn der " +
                        "angesetzte Bezugspreis die Stromsteuer auf den Eigenverbrauch enthält."),
                    lauf.StromsteuerBefreiungEur.ToString("N2", kultur))
            });
        }

        /// <summary>Fall 4 der Stromseite: gepflegter Anteil gegen <c>STROMST_REGELSATZ</c>.</summary>
        private static void Fall4Strom(double anteilCtKwh, int jahr, CultureInfo kultur,
                                       List<KohaerenzHinweis> liste)
        {
            GesetzParameter p = new GesetzKatalog()
                .WertMitHerkunft(DbWerte.GESETZ_STROMST_REGELSATZ, jahr);
            if (p == null || !p.Wert.HasValue) return;

            // Der Stromsteuersatz steht in EUR/MWh. Anders als beim Erdgas gibt es hier
            // keine Brennwertbrücke — eine Kilowattstunde Strom ist eine Kilowattstunde.
            double? katalogCt = InCtKwh(p.Wert.Value, p.Einheit, null);
            if (!katalogCt.HasValue) return;
            if (Math.Abs(anteilCtKwh - katalogCt.Value) <= TOLERANZ_CT_KWH) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL4_STROMSTEUER",
                        "Der erfasste Stromsteueranteil {0} ct/kWh weicht vom Katalogsatz {1} {2} " +
                        "des Jahres {3} ab — das sind {4} ct/kWh."),
                    anteilCtKwh.ToString("N4", kultur),
                    p.Wert.Value.ToString("N2", kultur), p.Einheit,
                    jahr.ToString(CultureInfo.InvariantCulture),
                    katalogCt.Value.ToString("N4", kultur))
            });
        }

        /// <summary>
        /// ETAPPE E2 (Befund S-5) — <b>Erlaubnispflicht nach StromStG</b>.
        ///
        /// <para>Wer Strom in einer Anlage ab der Erlaubnisschwelle erzeugt und
        /// entnimmt, braucht dafür eine Erlaubnis des Hauptzollamts. Das ist eine
        /// Pflicht des Betreibers, kein Rechenwerk: Der Satz
        /// <c>STROMST_ERLAUBNISSCHWELLE</c> stand als einziger Schlüssel der
        /// Stromsteuer im Katalog, ohne dass ihn jemand las (<c>02/§ 3.2</c>). Die
        /// Zeile nennt Schwelle und betroffene Anlagen und ändert nichts.</para>
        ///
        /// <para>Sie hängt NICHT daran, ob die Befreiung § 9 Abs. 1 Nr. 3 gewährt wird —
        /// die Erlaubnispflicht gilt auch dort, wo gar nichts befreit ist.</para>
        /// </summary>
        private static void ErlaubnisschwelleStrom(KohaerenzLauf lauf, CultureInfo kultur,
                                                   List<KohaerenzHinweis> liste)
        {
            if (lauf == null || lauf.Steuer == null || lauf.Steuer.Anlagen == null) return;

            GesetzParameter p = new GesetzKatalog()
                .WertMitHerkunft(DbWerte.GESETZ_STROMST_ERLAUBNISSCHWELLE, lauf.Jahr);
            if (p == null || !p.Wert.HasValue || p.Wert.Value <= 0) return;

            var ueber = new List<string>();
            foreach (SteuerAnlage a in lauf.Steuer.Anlagen)
            {
                if (a == null || !a.Stromerzeuger) continue;
                if (a.PelKW < p.Wert.Value) continue;
                string name = a.Klartext(kultur);
                if (!ueber.Contains(name)) ueber.Add(name);
            }
            if (ueber.Count == 0) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_STROMST_ERLAUBNIS",
                        "Erlaubnispflicht nach StromStG: Ab {0} kW elektrischer Nennleistung " +
                        "braucht der Betreiber eine Erlaubnis des Hauptzollamts. Betroffen: {1}. " +
                        "Auf die Rechnung wirkt das nicht."),
                    p.Wert.Value.ToString("N0", kultur),
                    string.Join(", ", ueber.ToArray()))
            });
        }

        // =====================================================================
        // Datenzugriff
        // =====================================================================

        // ETAPPE E18 (E18‑Q6 a): Der rohe Leseweg des Stromsteueranteils steht jetzt in
        // StrompreisZerlegungCtrl.StromsteuerRoh — derselbe Weg für Fall 4 und für die
        // Anzeige im Dialog „BHKW-Wirtschaftlichkeit".

        /// <summary>Anzeigename eines Energieträgers; nicht lesbar = „#Id". ETAPPE E7c3
        /// (B‑6): ein benannter Anzeigerückfall — die Zeile, die den Namen trägt, erscheint
        /// trotzdem, und die Kennung macht den Träger auffindbar.</summary>
        private static string TraegerName(int carrierId)
        {
            try
            {
                object v = StilleDb.ScalarStreng(
                    "SELECT [name] FROM energy_carrier WHERE id = ?",
                    new DbParam("@id", carrierId));
                string s = (v == null || v == DBNull.Value) ? "" : Convert.ToString(v).Trim();
                return s.Length > 0 ? s : ("#" + carrierId.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                // Anzeigerückfall: die Kennung statt des Namens (siehe Kopf).
                return "#" + carrierId.ToString(CultureInfo.InvariantCulture);
            }
        }

        // =====================================================================
        // Einheitenkette
        // =====================================================================

        /// <summary>
        /// Bringt einen Katalogsatz in ct/kWh — <b>dieselbe Kette wie die Schnellwahl des
        /// Dialogs</b> (<c>ucBrennstoffBestandteile.InCtKwh</c>, Konzept § 6.2). Beide
        /// Wege müssen zur selben Zahl kommen, sonst meldete Fall 4 eine Abweichung
        /// gegen den Wert, den die Schnellwahl selbst eingetragen hat.
        /// <c>null</c> = nicht belegbar (dann entsteht keine Zeile).
        /// </summary>
        /// <param name="a">Trägerangaben für die Umrechnung; <c>null</c> = reine
        /// Energieeinheit ohne Abrechnungsbezug (Strom).</param>
        private static double? InCtKwh(double wert, string einheit, SteuerAnlage a)
        {
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            {
                // EUR/MWh bemisst sich beim Gas am BRENNWERT; der Arbeitspreis entsteht
                // dagegen aus Preis ÷ Hi. Umgerechnet wird deshalb mit Hs/Hi — dieselbe
                // Regel wie in SteuerGutschriftRechner.MengeInGesetzlicherEinheit. Fehlt
                // der Brennwert (oder gibt es keinen — Strom), bleibt der Faktor 1.
                double faktor = (a != null && a.EffHi > 0 && a.EffHs > 0) ? a.EffHs / a.EffHi : 1.0;
                return wert / 10.0 * faktor;
            }

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.OrdinalIgnoreCase))
                return wert * GJ_JE_MWH / 10.0;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "l", a);

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "kg", a);

            return null;
        }

        /// <summary>
        /// Satz je 1.000 Abrechnungseinheiten → ct/kWh:
        /// <c>Satz ÷ 1000 [€/Einheit] × 100 [ct/€] ÷ Hi [kWh/Einheit]</c>. Passt die
        /// Abrechnungseinheit nicht, bleibt es bei <c>null</c>: Die Brücke Liter ↔
        /// Kilogramm bräuchte die Dichte, und <c>energy_carrier.density</c> ist im
        /// gesamten Bestand leer.
        /// </summary>
        private static double? JeTausendEinheiten(double wert, string erwartet, SteuerAnlage a)
        {
            if (a == null || a.EffHi <= 0) return null;
            if (a.Abrechnungseinheit == null) return null;
            if (!string.Equals(a.Abrechnungseinheit.Trim(), erwartet, StringComparison.OrdinalIgnoreCase))
                return null;
            return wert / (10.0 * a.EffHi);
        }

        // =====================================================================
        // Texte
        // =====================================================================

        /// <summary>„Name (Grund)" — die Aufzählungsform der Trägerliste.</summary>
        private static string MitGrund(string name, string grund)
        {
            return name + " (" + grund + ")";
        }

        /// <summary>
        /// MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe Muster wie
        /// <c>ucFuelSettings.TKd4</c> und <c>UcWirtschaftlichkeit.T</c>. Die Schlüssel
        /// tragen den Präfix <c>KOH_</c>; der Rückfall greift auf einer Ressourcendatei
        /// ohne die neuen Einträge.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch (Exception ex) when (ex is System.Resources.MissingManifestResourceException ||
                                       ex is System.Resources.MissingSatelliteAssemblyException ||
                                       ex is InvalidOperationException)
            {
                // ETAPPE E7c3 (B‑6): benannt — nur die Fehler der Ressourcensuche; der
                // deutsche Rückfalltext ist der Zweck dieser Methode.
                return rueckfall;
            }
        }
    }
}
