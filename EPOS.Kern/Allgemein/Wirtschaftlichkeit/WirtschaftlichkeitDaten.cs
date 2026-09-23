using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DTOs des Wirtschaftlichkeitsmoduls (Konzept_Wirtschaftlichkeit.md, Kap. 5/6;
    // Phase 6 = Ausbaustufe W1: Kapitalwertmethode nach DIN EN 17463).
    //
    // Entschieden (11.08.2026, Kap. 7):
    //  - Referenzszenario: das STAMMPROJEKT ist die Unterlassensalternative —
    //    Kapitalwert einer Variante = Barwert der Differenz-Zahlungsströme
    //    Variante − Stamm. Der Stamm selbst zeigt seinen Nettokosten-Barwert.
    //  - Vorgabewerte: Zinssatz 3,0 % · Betrachtungszeitraum 20 a (je Stamm editierbar).
    //  - Restwert: linear (Investition × Restnutzungsdauer / Nutzungsdauer), abgezinst.
    //  - Strompreise: aus der Kostenmaske (energy_project_settings), KEINE Doppel-
    //    pflege — hier werden nur Einspeisevergütung und Preissteigerungen geführt.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// ETAPPE W5‑B‑9 (Anwenderentscheid 09.09.2026): der <b>Szenario-Parametersatz</b>
    /// eines Projekts — sechs Größen, mit denen Best und Worst von der
    /// Erwartungsrechnung abweichen.
    ///
    /// <para><b>Warum es ihn gibt.</b> Bis dahin unterschieden sich die drei Szenarien
    /// ausschließlich über die ZEILENwerte
    /// (<c>Tab_ProjektWerte.BestCase</c>/<c>WorstCase</c>) — und die stehen im Bestand
    /// bei nahezu jeder Position auf 0. <c>Szenariowert</c> fällt dann nach dem
    /// VALERI-Muster auf den Erwartungswert zurück, und alle drei Szenarien rechnen
    /// dieselbe Zahl. Der Anwenderbefund vom 08.09.2026 lautete deshalb: Die Szenarien
    /// liefern identische Ergebnisse. Dieser Satz spannt die Bandbreite auf der
    /// PROJEKTebene auf, wo VALERI (DIN EN 17463) sie erwartet.</para>
    ///
    /// <para><b>Jedes Feld ist nullbar, und <c>null</c> heißt VORGABE</b> — nicht 0.
    /// Ein Feld, das der Anwender nie angefasst hat, zieht bei einer geänderten
    /// Projektangabe automatisch mit (i wechselt von 3 auf 4 % → die Best-Vorgabe folgt
    /// auf 3 %). Die Vorgaben stehen in <see cref="VORGABE_ZINS_PUNKTE"/> und den
    /// Konstanten daneben.</para>
    ///
    /// <para><b>Vorzeichen einheitlich: <c>+</c> heißt mehr bzw. länger.</b> Eine
    /// höhere Investition ist ungünstig, ein höherer Ertrag günstig, eine längere
    /// Nutzungsdauer günstig — die Vorgaben tragen dem Rechnung, die Felder selbst
    /// kennen keine Wertung.</para>
    ///
    /// <para><b>ERWARTET bekommt keinen Satz.</b> Es <i>ist</i> der Projektparametersatz;
    /// eigene Felder wären eine zweite Wahrheit für dieselbe Zahl — und die Zusage
    /// „Erwartet rechnet zahlengleich wie vor dieser Etappe“ hätte keinen Anker mehr.</para>
    /// </summary>
    public class SzenarioSatz
    {
        /// <summary>Vorgabe-Abstand des Kalkulationszinses [%-Punkte].</summary>
        public const double VORGABE_ZINS_PUNKTE = 1.0;

        /// <summary>Vorgabe-Abstand beider Preissteigerungen [%-Punkte].</summary>
        public const double VORGABE_PREIS_PUNKTE = 1.0;

        /// <summary>Vorgabe der Investitionsänderung [%].</summary>
        public const double VORGABE_INVEST_PROZENT = 10.0;

        /// <summary>Vorgabe der Ertragsänderung [%].</summary>
        public const double VORGABE_ERTRAG_PROZENT = 10.0;

        /// <summary>Vorgabe der Nutzungsdaueränderung [a].</summary>
        public const double VORGABE_DAUER_JAHRE = 2.0;

        /// <summary>Untergrenze der abgeleiteten Nutzungsdauer [a]. Alles darunter heißt
        /// im <c>KapitalwertRechner</c> „keine Nutzungsdauer gepflegt“ (n = T, kein Ersatz,
        /// kein Restwert) — diese Bedeutung darf eine Änderung nicht versehentlich
        /// auslösen.</summary>
        public const double DAUER_UNTERGRENZE = 1.0;

        /// <summary>BEST oder WORST (<see cref="WirtschaftlichkeitSzenario"/>).</summary>
        public string Szenario = WirtschaftlichkeitSzenario.BEST;

        /// <summary>Kalkulationszins [%]; <c>null</c> = Vorgabe (i ∓ 1 %-Pkt).</summary>
        public double? Zinssatz;

        /// <summary>Preissteigerung Energie [%/a]; <c>null</c> = Vorgabe (p_E ∓ 1 %-Pkt).</summary>
        public double? PreissteigerungEnergie;

        /// <summary>Preissteigerung Betrieb [%/a]; <c>null</c> = Vorgabe (p_B ∓ 1 %-Pkt).</summary>
        public double? PreissteigerungBetrieb;

        /// <summary>Änderung der Investition [%], + = teurer; <c>null</c> = Vorgabe (∓ 10 %).
        /// Sie greift NUR auf Positionen ohne gepflegten Szenariowert (Vorrangregel).</summary>
        public double? InvestitionAenderung;

        /// <summary>Änderung der Erträge [%], + = höher; <c>null</c> = Vorgabe (± 10 %).
        /// Sie greift auf den Einspeiseerlös und die PV-Vergütungsreihe — nicht auf die
        /// gesetzlichen Erlösreihen (KWKG, Energie-/Stromsteuer).</summary>
        public double? ErtragAenderung;

        /// <summary>Änderung der Nutzungsdauer [a], + = länger; <c>null</c> = Vorgabe (± 2 a).
        /// Sie greift NUR auf Positionen ohne gepflegte Szenario-Nutzungsdauer.</summary>
        public double? NutzungsdauerAenderung;

        /// <summary>
        /// ETAPPE W5‑B‑12 (Anwenderentscheid 09.09.2026): Preissteigerung der
        /// kapitalgebundenen Kosten p_I [%/a] dieses Szenarios; <c>null</c> = Vorgabe
        /// (Erwartet-p_I ∓ 1 %-Pkt, siehe <see cref="PreisInvestWirksam"/>).
        /// </summary>
        public double? PreissteigerungInvestition;

        /// <summary>true, wenn kein einziges Feld gepflegt ist — dann gelten durchweg die
        /// Vorgaben (Statuszeile der Seite und Herleitungszeile des Dialogs).</summary>
        public bool NurVorgaben
        {
            get
            {
                return !Zinssatz.HasValue && !PreissteigerungEnergie.HasValue &&
                       !PreissteigerungBetrieb.HasValue && !InvestitionAenderung.HasValue &&
                       !ErtragAenderung.HasValue && !NutzungsdauerAenderung.HasValue &&
                       !PreissteigerungInvestition.HasValue;
            }
        }

        /// <summary>Das Vorzeichen der Vorgaben: BEST = −1 (billiger, weniger Zins),
        /// WORST = +1. Jede andere Zeichenkette wird wie WORST behandelt — den Satz
        /// gibt es nur für diese zwei.</summary>
        private double Richtung
        {
            get
            {
                return string.Equals(Szenario, WirtschaftlichkeitSzenario.BEST,
                                     StringComparison.Ordinal) ? -1.0 : 1.0;
            }
        }

        /// <summary>Wirksamer Kalkulationszins [%] — nie negativ.</summary>
        public double ZinsWirksam(double projektZins)
        {
            double z = Zinssatz ?? (projektZins + Richtung * VORGABE_ZINS_PUNKTE);
            return z < 0 ? 0 : z;
        }

        /// <summary>Wirksame Preissteigerung Energie [%/a].</summary>
        public double PreisEnergieWirksam(double projektwert)
        {
            return PreissteigerungEnergie ?? (projektwert + Richtung * VORGABE_PREIS_PUNKTE);
        }

        /// <summary>Wirksame Preissteigerung Betrieb [%/a].</summary>
        public double PreisBetriebWirksam(double projektwert)
        {
            return PreissteigerungBetrieb ?? (projektwert + Richtung * VORGABE_PREIS_PUNKTE);
        }

        /// <summary>
        /// ETAPPE W5‑B‑12: wirksame Preissteigerung der kapitalgebundenen Kosten
        /// p_I [%/a] dieses Szenarios.
        ///
        /// <para><b>Der Bezugswert ist das ERWARTET-p_I</b>
        /// (<see cref="WirtschaftlichkeitParameter.PreisInvestWirksam"/>), nicht das
        /// p_B des Szenarios. Im Regelfall — p_I des Projekts nicht gepflegt, also
        /// „wie p_B", und das Szenario-p_B ebenfalls nicht gepflegt — kommt dabei
        /// genau das wirksame p_B dieses Szenarios heraus; ist Erwartet-p_I dagegen
        /// gepflegt, spannt sich die Bandbreite um DIESEN Wert. Beides ist dieselbe
        /// ∓1‑%‑Punkt-Regel wie bei p_E und p_B — der Satz greift immer am
        /// Erwartungswert seiner eigenen Größe an.</para>
        /// </summary>
        public double PreisInvestWirksam(double erwartetWert)
        {
            return PreissteigerungInvestition ?? (erwartetWert + Richtung * VORGABE_PREIS_PUNKTE);
        }

        /// <summary>Wirksame Investitionsänderung [%], + = teurer.</summary>
        public double InvestWirksam
        {
            get { return InvestitionAenderung ?? (Richtung * VORGABE_INVEST_PROZENT); }
        }

        /// <summary>Wirksame Ertragsänderung [%], + = höher. Die Vorgabe zeigt GEGEN die
        /// Richtung: Im Best-Fall bringt die Anlage MEHR ein.</summary>
        public double ErtragWirksam
        {
            get { return ErtragAenderung ?? (-Richtung * VORGABE_ERTRAG_PROZENT); }
        }

        /// <summary>Wirksame Nutzungsdaueränderung [a], + = länger. Vorgabe wie beim
        /// Ertrag gegen die Richtung: Im Best-Fall hält die Anlage länger.</summary>
        public double DauerWirksam
        {
            get { return NutzungsdauerAenderung ?? (-Richtung * VORGABE_DAUER_JAHRE); }
        }

        /// <summary>Der Investitionsfaktor (1,0 = keine Änderung); nie negativ.</summary>
        public double InvestFaktor
        {
            get { double f = 1.0 + InvestWirksam / 100.0; return f < 0 ? 0 : f; }
        }

        /// <summary>Der Ertragsfaktor (1,0 = keine Änderung); nie negativ.</summary>
        public double ErtragFaktor
        {
            get { double f = 1.0 + ErtragWirksam / 100.0; return f < 0 ? 0 : f; }
        }

        /// <summary>Die Nutzungsdauer einer Position [a] nach dieser Etappe.
        /// Zeilen ohne (sinnvoll) gepflegte Nutzungsdauer (n &lt; 1) bleiben unberührt —
        /// dort heißt der Wert „wie der Betrachtungszeitraum“, und das ist keine Dauer,
        /// die man verlängern könnte.</summary>
        public double DauerFuer(double dauer)
        {
            if (dauer < DAUER_UNTERGRENZE) return dauer;
            double d = dauer + DauerWirksam;
            return d < DAUER_UNTERGRENZE ? DAUER_UNTERGRENZE : d;
        }

        /// <summary>Ein Satz aus lauter Vorgaben (alle Felder <c>null</c>).</summary>
        public static SzenarioSatz Vorgabe(string szenario)
        {
            return new SzenarioSatz { Szenario = szenario ?? WirtschaftlichkeitSzenario.BEST };
        }

        /// <summary>Flache Kopie — der Dialog arbeitet auf einer, damit „Abbrechen“
        /// wirklich abbricht.</summary>
        public SzenarioSatz Kopie() { return (SzenarioSatz)MemberwiseClone(); }

        /// <summary>Nachweiszeile des Satzes (Seite, Dialog, Bericht) — die WIRKSAMEN
        /// Zahlen, nicht die gepflegten.</summary>
        public string Nachweis(WirtschaftlichkeitParameter p, System.Globalization.CultureInfo kultur)
        {
            double zins = p != null ? p.Zinssatz : 0;
            double pe = p != null ? p.PreissteigerungEnergie : 0;
            double pb = p != null ? p.PreissteigerungBetrieb : 0;
            // ETAPPE W5‑B‑12: p_I gehört in die Annahmenzeile, sobald es sie gibt — der
            // Satz indiziert die Ersatzbeschaffungen, und eine Bandbreite, deren
            // Annahmen nicht vollständig dastehen, ist keine offengelegte Annahme.
            double pi = p != null ? p.PreisInvestWirksam : 0;
            return "i = " + ZinsWirksam(zins).ToString("N1", kultur) + " % · p_E = " +
                   PreisEnergieWirksam(pe).ToString("N1", kultur) + " %/a · p_B = " +
                   PreisBetriebWirksam(pb).ToString("N1", kultur) + " %/a · p_I = " +
                   PreisInvestWirksam(pi).ToString("N1", kultur) + " %/a · Investition " +
                   InvestWirksam.ToString("+0.#;-0.#;0", kultur) + " % · Erträge " +
                   ErtragWirksam.ToString("+0.#;-0.#;0", kultur) + " % · Nutzungsdauer " +
                   DauerWirksam.ToString("+0.#;-0.#;0", kultur) + " a";
        }
    }

    /// <summary>Parametersatz eines Rechenlaufs (Tab_ProjektWirtschaftlichkeit,
    /// eine Zeile je STAMMprojekt — gilt für die ganze Vergleichsgruppe).</summary>
    public class WirtschaftlichkeitParameter
    {
        public int IdStamm;

        /// <summary>
        /// KONZEPT § 2.9 — das <b>wählbare Vergleichsprojekt</b> der Gruppe:
        /// <c>Tab_Projekt.ID</c> des Standes, gegen den alle Differenzkennzahlen
        /// rechnen (Kapitalwertdifferenz, Annuität, dynamische Amortisation, interner
        /// Zinsfuß, Sensitivität der Differenz). <b>0 = Stammprojekt</b> — die Vorgabe
        /// und damit das Bestandsverhalten.
        ///
        /// <para>Sie steht an der RAHMENZEILE und nicht an der Variante, weil die
        /// Referenz wie Zins und Betrachtungszeitraum je Gruppe gilt. Persistenz:
        /// <see cref="SchemaKatalog.SPALTE_PW_REFERENZPROJEKT"/> (Schemaschritt 92,
        /// nullbar; NULL = Stamm).</para>
        ///
        /// <para>Sie ist der VORGABEWERT der Differenzrechnung, nicht ihr einziger:
        /// <c>WirtschaftlichkeitCtrl.Berechne</c> nimmt die Referenz als Parameter, und
        /// die Vergleichssicht „Zwei Stände" (§ 2.15) übergibt dort A, ohne diesen Wert
        /// anzufassen.</para>
        /// </summary>
        public int IdReferenzprojekt;

        public double Zinssatz = 3.0;                 // Kalkulationszins [%]
        public int Betrachtungszeitraum = 20;         // T [a]
        public double PreissteigerungEnergie = 0.0;   // [%/a]
        public double PreissteigerungBetrieb = 0.0;   // [%/a]
        public double Einspeiseverguetung = 0.0;      // [€/kWh] für PV-Überschuss

        // ---- Stufe W2 (Phase 7) ----
        public double CO2Preis = 0.0;                 // BEHG [€/t] auf Brennstoff-CO₂ (0 = aus)

        // ETAPPE BK1a — die vier KWKG-Rechengrößen des Projekts (Satz Eigen, Satz
        // Einspeisung, Vbh-Kontingent, Jahresdeckel) sind mit Schemaschritt 90
        // entfallen. Sie stehen seit Schritt 89 an der Anlage
        // (Tab_Energieanlagen.KWKG_*), und beide Rechenwege lesen sie dort.

        // ---- Stufe W3 (Phase 8) ----
        public int IdKraftwerkspark = 0;              // Tab_Kraftwerkspark.ID (0 = keine Emissionsbilanz)
        public double RefKesselWirkungsgrad = 90.0;   // Referenzkessel der getrennten Erzeugung [%]
        public int RefKesselIdBrennstoff = 3;         // Tab_Brennstoff_Stamm.ID (Vorgabe 3 = Erdgas E)

        // ---- KWKG 2025 (Phase 9, Konzept Kap. 8) ----
        /// <summary>Bestell-/Genehmigungs- bzw. Dauerbetriebsdatum (§ 6 KWKG 2025).
        /// null = Förderfähigkeit ungeprüft (Hinweis im Ergebnis).</summary>
        public DateTime? KwkgStichtag;
        /// <summary>Geplante Inbetriebnahme — bestimmt zugleich den Förderbeginn
        /// (Kalenderjahr) der Vbh-Staffel; null = aktuelles Jahr + 1.</summary>
        public DateTime? KwkgInbetriebnahme;
        /// <summary>Abschlag für Negativpreis-Stunden [% der vergüteten Vbh]
        /// (§ 7 Abs. 5, W2-Näherung laut Kap. 8.5.4).</summary>
        public double KwkgAbschlagNegativ = 0.0;

        // ---- ETAPPE E4 — Angaben der Steuerprüfung (Migrationsschritt 20) ----
        //
        // Die gesetzlichen Bedingungen der Energie- und Stromsteuerentlastung werden
        // ERFASST statt angenommen. Jeder Vorgabewert ist der Wert, der KEINE
        // Gutschrift auslöst — ohne ausdrückliche Angabe ändert sich an einer
        // Bestandsrechnung nichts.

        /// <summary>
        /// Unternehmensart des Betreibers, Steuerwert aus <c>DbWerte.UNTERNEHMENSART_*</c>.
        /// Voraussetzung der Entlastung nach § 9b StromStG (und des § 54 EnergieStG).
        /// Vorgabe: kein produzierendes Gewerbe ⇒ keine Stromsteuer-Entlastung.
        /// </summary>
        public string Unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;

        /// <summary>Räumlicher Zusammenhang gegeben (4,5-km-Regel, § 12b StromStV) —
        /// eine der vier Bedingungen der Befreiung nach § 9 Abs. 1 Nr. 3 StromStG.</summary>
        public bool RaeumlicherZusammenhang;

        /// <summary>Hocheffizienz nach Anhang III der Richtlinie (EU) 2023/1791
        /// nachgewiesen (§ 2 StromStG) — zweite Bedingung derselben Befreiung.</summary>
        public bool HocheffizienzNachweis;

        /// <summary>
        /// Jahresnutzungsgrad der KWK-Anlage [%] im Sinne des § 3 Abs. 3 EnergieStG;
        /// Schwelle 70 % für § 53a EnergieStG. <c>null</c> = nicht gepflegt (die
        /// Begründung unterscheidet das von „gepflegt und zu niedrig").
        /// </summary>
        public double? Jahresnutzungsgrad;

        /// <summary>
        /// Gewählte Energiesteuerentlastung, Steuerwert aus
        /// <c>DbWerte.ENERGIESTEUER_WAHL_*</c>. Vorgabe <c>KEINE</c> — § 53 und § 53a
        /// schließen einander aus, und ihre Kombination ist rechtlich ungeklärt
        /// (Grundlagen, Abschnitt 6 Punkt 1); der Anwender wählt die Norm.
        /// </summary>
        public string EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_KEINE;

        /// <summary>
        /// Aufteilungsmethode des Brennstoffs auf Strom und Wärme, Steuerwert aus
        /// <c>DbWerte.AUFTEILUNG_*</c>. Vorgabe <c>VOLLER_BRENNSTOFF</c> — das rechtlich
        /// belegte Verfahren (§ 53 Abs. 2 Satz 1 EnergieStG i.V.m. der Dienstvorschrift
        /// Energieerzeugung: „Wärme — genutzt oder ungenutzt — wird nicht betrachtet").
        /// </summary>
        public string AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

        /// <summary>
        /// Modus, in dem § 9 Abs. 1 Nr. 3 StromStG in die Wirtschaftlichkeit eingeht,
        /// Steuerwert aus <c>DbWerte.STROMST_BEFREIUNG_MODUS_*</c> (Schemaschritt 88).
        /// Vorgabe <c>AUSWEIS</c>: Die Befreiung wird gerechnet und gezeigt, geht aber
        /// nicht in die Erlöse und damit nicht in den Kapitalwert.
        ///
        /// <para><b>Warum das die Vorgabe ist.</b> Auf selbst erzeugten und selbst
        /// verbrauchten Strom entsteht gar keine Stromsteuer; der Vorteil steckt bereits
        /// in der kleineren Bezugsrechnung. Als Erlös gebucht stünde er ein zweites Mal
        /// in der Rechnung. <c>ERLOES</c> ist deshalb nur richtig, wenn der angesetzte
        /// Bezugspreis die Stromsteuer auf den Eigenverbrauch enthält — die
        /// Kohärenzprüfung sagt das mit einer eigenen Zeile.</para>
        /// </summary>
        public string StromsteuerBefreiungModus = DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS;

        /// <summary>true, wenn <see cref="StromsteuerBefreiungModus"/> ausdrücklich
        /// <c>ERLOES</c> führt. Leer, NULL und jeder unbekannte Wert bedeuten
        /// AUSWEIS — dieselbe tolerante Leseregel wie beim
        /// Nachhaltigkeitsnachweis der Biomasse.</summary>
        public bool StromsteuerBefreiungAlsErloes
        {
            get
            {
                return string.Equals(StromsteuerBefreiungModus,
                                     DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES,
                                     StringComparison.Ordinal);
            }
        }

        // ---- ETAPPE E5 — eine Projektangabe (Migrationsschritt 21) ----
        //
        // Der Schalter „Aufschläge in der Wirtschaftlichkeit berücksichtigen" stand hier
        // bis zum Anwenderentscheid SP-E-2: Die Preisanteile ZERLEGEN den Arbeitspreis
        // und kommen nicht mehr auf ihn, also gibt es nichts an- oder abzuschalten. Die
        // Spalte Aufschlaege_Anwenden ist mit Schemaschritt 85 entfallen
        // (StrompreisAltspalten).

        /// <summary>
        /// Vergütung für eingespeisten <b>KWK</b>-Strom [€/kWh]; <c>null</c> = nicht
        /// gepflegt (wirkt wie 0).
        ///
        /// <para><b>Behebt einen Bestandsmangel.</b> Bis E5 bewertete der Flat-Pfad nur
        /// den PV-Überschuss; eingespeister BHKW-Strom bekam gar keinen Strompreis,
        /// sondern nur den KWK-Zuschlag — und das Feld dafür war ohne
        /// Photovoltaik-Gruppe im Parameterdialog nicht einmal sichtbar
        /// (<c>Form_WirtschaftlichkeitParameter</c>). Ökonomisch ist das grob falsch.</para>
        /// </summary>
        public double? EinspeiseverguetungKWK;

        // ---- LEITENTSCHEIDUNGEN L12 und L13 (Migrationsschritt 23) ----
        //
        // Vier Angaben, die den Rechenweg der Emissionsbilanz und die Bemessung der
        // BEHG-Abgabe SICHTBAR machen. Jede Vorgabe ist der Wert, der das heutige
        // Verhalten fortführt — aufgelöst und ausgewiesen von <c>BilanzKonvention</c>.

        /// <summary>
        /// Bilanzjahr der Emissionsrechnung; <c>0</c> = nicht gepflegt. Dann gilt
        /// <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c> (2026, letztes Jahr des alten
        /// Rechtsstands) — eine feste Zahl statt der Systemuhr, damit ein gespeichertes
        /// Projekt in fünf Jahren dieselben Zahlen liefert.
        /// </summary>
        public int BilanzJahr;

        /// <summary>
        /// Bewertung des KWK-Stroms in der Emissionsbilanz, Steuerwert aus
        /// <c>DbWerte.EMISSIONSMETHODE_*</c>. Vorgabe <c>KATALOG</c>: Der Rechenweg
        /// folgt dem Gültig-ab-Datum des Verdrängungsstrommix im Katalog (L12).
        /// </summary>
        public string EmissionsMethode = DbWerte.EMISSIONSMETHODE_KATALOG;

        /// <summary>
        /// Bilanzierungskonvention für Biomasse, Steuerwert aus
        /// <c>DbWerte.BIOMASSE_KONVENTION_*</c>. Vorgabe <c>NULLANSATZ</c> — die
        /// Annahme, die der Bestand still trifft (L13).
        /// </summary>
        public string BiomasseKonvention = DbWerte.BIOMASSE_KONVENTION_NULL;

        /// <summary>
        /// Nachhaltigkeitsnachweis nach § 8 EBeV 2030 vorhanden. Vorgabe <c>true</c> —
        /// der Bestand rechnet biogene Brennstoffe ohne CO₂-Abgabe, also so, als läge
        /// der Nachweis vor. In der Datenbank steht dafür eine TEXT-Spalte, kein
        /// YESNO: Access hätte eine neue YESNO-Spalte mit <c>False</c> belegt und damit
        /// jedem Altprojekt den Nachweis entzogen (L13, Migrationsschritt 23).
        /// </summary>
        public bool NachhaltigkeitsnachweisBiomasse = true;

        // ---- ETAPPE K6 — die KWKG-Pauschale des Projekts (Schritt 28) ----
        //
        // Von den Angaben aus Konzept § 8.1 (HF6) steht hier nur noch die Pauschale.
        // Tatbestand und Anlagenart des PROJEKTS sind mit Schemaschritt 90 entfallen,
        // der Kostenanteil mit Schemaschritt 91 — alle drei werden je Anlage gepflegt
        // und gelesen (Tab_Energieanlagen.KWKG_Eigenstromfall, KWKG_Anlagenart,
        // KWKG_Kostenanteil).

        /// <summary>
        /// Pauschale nach § 9 KWKG (Anlagen bis 2 kW<sub>el</sub>): einmalige
        /// Vorauszahlung von 4 ct/kWh für 60.000 Vbh statt der laufenden Abrechnung.
        /// Vorgabe <c>false</c> — Access belegt die YESNO-Spalte in jeder Bestandszeile
        /// mit <c>False</c>, und das ist zugleich der bestandswahrende Wert.
        /// </summary>
        public bool KwkgPauschalmodus;

        // ---- ETAPPE W5‑B‑12 — p_I und die nicht monetären Wirkungen (Schritt 72) ----

        /// <summary>
        /// Preisänderungssatz der KAPITALGEBUNDENEN Kosten p_I [%/a] (VDI 2067 Blatt 1):
        /// Er indiziert die Ersatzbeschaffungen und die Preisbasis des Restwerts
        /// (<c>KapitalwertRechner.Rechne</c>, Parameter <c>preisstInvestProzent</c>).
        ///
        /// <para><b><c>null</c> heißt „wie p_B" — nicht „0 %".</b> Eine 0 als Vorbelegung
        /// hätte behauptet, Investitionsgüter würden nie teurer; diese Aussage hat
        /// niemand getroffen. Der einzige gepflegte Satz im Haus, der eine allgemeine
        /// Kostensteigerung ausdrückt, ist <see cref="PreissteigerungBetrieb"/> —
        /// deshalb der Rückfall dorthin (<see cref="PreisInvestWirksam"/>).</para>
        /// </summary>
        public double? PreissteigerungInvestition;

        /// <summary>
        /// VALERI-Lücke G6: die NICHT MONETÄREN Wirkungen der Maßnahme als Freitext —
        /// Versorgungssicherheit, Arbeitsschutz, Komfort, Außenwirkung, Erfüllung einer
        /// Auflage. DIN EN 17463 verlangt diese qualitative Beschreibung; ein
        /// Kapitalwert ohne sie behauptet mehr, als er weiß.
        ///
        /// <para><c>null</c> bzw. leer = nichts erfasst; Bericht und Seite lassen die
        /// Zeile dann weg, statt eine leere zu drucken.</para>
        /// </summary>
        public string NichtMonetaer;

        /// <summary>
        /// Das WIRKSAME p_I [%/a] des Erwartungsfalls: der gepflegte Satz, sonst p_B.
        /// Diese eine Stelle trägt die Nullsemantik — Rechenlauf, Dialog, Bericht und
        /// die Szenariovorgaben fragen sie, statt den Rückfall je Ort zu wiederholen.
        /// </summary>
        public double PreisInvestWirksam
        {
            get { return PreissteigerungInvestition ?? PreissteigerungBetrieb; }
        }

        public DateTime? GeaendertAm;

        /// <summary>
        /// ETAPPE W5‑B‑9: der Szenario-Parametersatz für BEST. Nie <c>null</c> — ein Satz
        /// aus lauter Vorgaben ist der Regelfall (Migrationsschritt 71 legt die zwölf
        /// Spalten NULL an, und NULL heißt Vorgabe).
        /// </summary>
        public SzenarioSatz SatzBest = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);

        /// <inheritdoc cref="SatzBest"/>
        public SzenarioSatz SatzWorst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);

        /// <summary>
        /// Der Satz eines Szenarios — <c>null</c> für ERWARTET und für jede unbekannte
        /// Zeichenkette. <b>Genau dieses <c>null</c> ist die Zusage</b>, dass der
        /// Erwartungsfall den Rechenweg von vor W5‑B‑9 geht: Jeder Aufrufer prüft auf
        /// <c>null</c> und fässt dann nichts an.
        /// </summary>
        public SzenarioSatz SatzFuer(string szenario)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal))
                return SatzBest;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal))
                return SatzWorst;
            return null;
        }

        /// <summary>
        /// ETAPPE W5‑B‑9: der Parametersatz, mit dem ein Szenario RECHNET.
        ///
        /// <para>Für ERWARTET ist es <c>this</c> — <b>dieselbe Referenz</b>, nicht eine
        /// wertgleiche Kopie. Damit ist die Zahlengleichheit des Erwartungsfalls keine
        /// Behauptung, sondern eine Eigenschaft des Codes.</para>
        ///
        /// <para>Für BEST und WORST eine flache Kopie mit ersetztem Zins und ersetzten
        /// Preissteigerungen. <b>Alles übrige bleibt stehen</b> — Betrachtungszeitraum,
        /// Einspeisevergütung, KWKG, Steuern, Bilanzierung sind Rechtsstände und Preise,
        /// keine Szenariogrößen. Investitions-, Ertrags- und Nutzungsdaueränderung wirken
        /// nicht hier, sondern in der EINGABE (Vorrangregel je Zeile).</para>
        /// </summary>
        public WirtschaftlichkeitParameter FuerSzenario(string szenario)
        {
            SzenarioSatz s = SatzFuer(szenario);
            if (s == null) return this;
            WirtschaftlichkeitParameter k = Kopie();
            k.Zinssatz = s.ZinsWirksam(Zinssatz);
            k.PreissteigerungEnergie = s.PreisEnergieWirksam(PreissteigerungEnergie);
            k.PreissteigerungBetrieb = s.PreisBetriebWirksam(PreissteigerungBetrieb);
            // ETAPPE W5‑B‑12: p_I wird wie Zins und die beiden anderen Preissätze
            // ERSETZT — und zwar als GEPFLEGTER Wert. Bliebe das Feld null, fiele die
            // Kopie über PreisInvestWirksam auf ihr eigenes (schon ersetztes) p_B
            // zurück und das Szenario rechnete an seinem Satz vorbei.
            k.PreissteigerungInvestition = s.PreisInvestWirksam(PreisInvestWirksam);
            return k;
        }

        /// <summary>Kurzdarstellung als Nachweiszeile (Reiter + Bericht).</summary>
        public string Nachweis(System.Globalization.CultureInfo kultur)
        {
            // ETAPPE W5‑B‑12: p_I steht IMMER da, auch ungepflegt — dann mit seiner
            // Herkunft „wie Betrieb". Ein Satz, der die Ersatzbeschaffungen fortschreibt,
            // gehört zu den Annahmen des Laufs; ihn nur bei Pflege zu nennen hieße, den
            // Regelfall zu verschweigen.
            string t = "i = " + Zinssatz.ToString("N1", kultur) + " % · T = " + Betrachtungszeitraum +
                   " a · Preissteigerung Energie " + PreissteigerungEnergie.ToString("N1", kultur) +
                   " %/a, Betrieb " + PreissteigerungBetrieb.ToString("N1", kultur) +
                   " %/a, Investition/Ersatz " + PreisInvestWirksam.ToString("N1", kultur) +
                   " %/a (" + (PreissteigerungInvestition.HasValue ? "gepflegt" : "wie Betrieb") +
                   ") · Einspeisevergütung " + Einspeiseverguetung.ToString("N3", kultur) + " €/kWh";
            if (CO2Preis > 0)
                t += " · CO₂ (BEHG) " + CO2Preis.ToString("N0", kultur) + " €/t";
            // ETAPPE BK1 — die Zeile nennt nur noch die PROJEKTWEITEN KWK-Angaben, und
            // ob sie überhaupt erscheint, entscheidet KwkgAktivierung (die EINE Regel,
            // die auch der Rechenkern zieht). Satz, Deckel und Kontingent stehen seit
            // Schemaschritt 89 je Anlage; sie hier als EINE Projektzahl zu nennen wäre
            // die doppelte Wahrheit, die BK1 auflöst — die Sätze je Modul führt die
            // Nachweistafel (KwkgModulNachweis).
            if (KwkgAktivierung.IstAktiv(IdStamm))
            {
                t += " · KWKG (Sätze je Anlage";
                if (KwkgAbschlagNegativ > 0)
                    t += ", Negativpreis-Abschlag " + KwkgAbschlagNegativ.ToString("N1", kultur) + " %";
                t += KwkgStichtag.HasValue
                    ? ", Stichtag " + KwkgStichtag.Value.ToString("dd.MM.yyyy", kultur)
                    : ", Stichtag ungeprüft";
                if (KwkgInbetriebnahme.HasValue)
                    t += ", Förderbeginn " + KwkgInbetriebnahme.Value.ToString("dd.MM.yyyy", kultur);
                t += ")";
            }
            // ETAPPE E4: die Steuerangaben gehören in die Nachweiszeile, sobald sie
            // überhaupt eine Gutschrift auslösen können. Ohne Wahl und ohne
            // produzierendes Gewerbe bleibt die Zeile unverändert wie bisher.
            if (!string.Equals(EnergiesteuerWahl, DbWerte.ENERGIESTEUER_WAHL_KEINE, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(EnergiesteuerWahl))
            {
                t += " · Energiesteuer " + EnergiesteuerWahl + " (" + AufteilungMethode + ")";
                if (Jahresnutzungsgrad.HasValue)
                    t += ", Nutzungsgrad " + Jahresnutzungsgrad.Value.ToString("N1", kultur) + " %";
            }
            if (!string.Equals(Unternehmensart, DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(Unternehmensart))
                t += " · Unternehmensart " + Unternehmensart;
            if (HocheffizienzNachweis || RaeumlicherZusammenhang)
                t += " · Stromsteuer: hocheffizient " + (HocheffizienzNachweis ? "ja" : "nein") +
                     ", räumlicher Zusammenhang " + (RaeumlicherZusammenhang ? "ja" : "nein");
            if (EinspeiseverguetungKWK.HasValue && EinspeiseverguetungKWK.Value != 0)
                t += " · Einspeisevergütung KWK " +
                     EinspeiseverguetungKWK.Value.ToString("N3", kultur) + " €/kWh";
            return t;
        }

        /// <summary>Flache Kopie (z. B. für den Kapitalwert-Verlauf mit abweichendem
        /// Betrachtungszeitraum, Phase 11) — die gespeicherten Parameter bleiben unberührt.</summary>
        public WirtschaftlichkeitParameter Kopie()
        {
            return (WirtschaftlichkeitParameter)MemberwiseClone();
        }
    }

    /// <summary>Referenzkessel der getrennten Erzeugung — seit Phase 11 aus dem
    /// Heizkessel des Stammprojekts (Tab_Heizkessel) ermittelt, nicht mehr im
    /// Parameterdialog gepflegt.</summary>
    public class ReferenzkesselInfo
    {
        public bool Gefunden;
        public string Bezeichner = "";
        public double WirkungsgradProzent;
        public int IdBrennstoff;
        public string BrennstoffName = "";
    }

    /// <summary>Eine Verlaufslinie des Kapitalwert-Diagramms (Phase 11):
    /// kumulierte diskontierte Zahlungsströme je Jahr 0…N (ohne Restwert —
    /// Kapitalwert = Endwert + Restwert-Barwert).</summary>
    public class VerlaufSerie
    {
        public int IdProjekt;
        public string Anzeige = "";
        public bool IstStamm;
        public double[] Kumuliert;      // Index = Jahr 0…N
        public double RestwertBarwert;  // zum gewählten Horizont
        public string Fehlgrund;        // != null → keine Reihe

        /// <summary>
        /// ETAPPE E7 — das vollständige Zahlungsbild dieser Linie: Netto- und
        /// Barwertreihe UND die Jahresreihen der einzelnen Positionen (Betrieb,
        /// Energie, CO₂-Abgabe, Ersatz, Einspeiseerlös, KWK-Zuschlag, die drei
        /// Steuergutschriften). Es ist die Datengrundlage der Mehrjahrestabelle.
        ///
        /// <para><c>null</c> bei den Differenzlinien und bei jeder Linie mit
        /// <see cref="Fehlgrund"/> — die Differenz zweier Zahlungsbilder ist kein
        /// Zahlungsbild, und ein Bild ohne Rechnung gibt es nicht.</para>
        /// </summary>
        public KapitalwertRechner.Zahlungsbild Bild;
    }

    /// <summary>Ergebnis der Verlaufsrechnung über einen frei wählbaren Horizont
    /// (auch &gt; T; dann wird mit verlängertem Betrachtungszeitraum neu gerechnet).</summary>
    public class WirtschaftlichkeitVerlauf
    {
        public int Jahre;
        public string Szenario = "";
        /// <summary>Absolute kumulierte Barwerte je Projekt (inkl. Stamm).</summary>
        public List<VerlaufSerie> Absolut = new List<VerlaufSerie>();
        /// <summary>Differenz Stand − Referenz (Nulldurchgang = dynamische Amortisation);
        /// die Referenz ist <see cref="IdReferenz"/>, ohne Wahl der Stamm.</summary>
        public List<VerlaufSerie> Differenz = new List<VerlaufSerie>();

        /// <summary>
        /// ETAPPE E6 — die Referenz, gegen die <see cref="Differenz"/> läuft
        /// (<c>Tab_Projekt.ID</c>, aufgelöst wie in <c>Berechne</c>: in Sicht 2 A, sonst die
        /// Gruppenreferenz, ohne Wahl der Stamm). Ihre Linie in <see cref="Absolut"/> trägt
        /// den Namen, mit dem ein Kopf „Δ ‹Stand› − ‹Referenz›" sie nennt. 0 = keine
        /// Rechnung.
        /// </summary>
        public int IdReferenz;
    }

    /// <summary>
    /// Der Tarifsatz Strom eines STAMMS (<c>Tab_ProjektTarif</c>, eine Zeile je Stamm):
    /// das <b>Rollenmodell</b> der Etappe E5 — Bezugstarif ohne Anlage, Reststromtarif
    /// mit Anlage, Einspeisetarif — für die Differenzmethode der vermiedenen Kosten.
    /// Aktiv = false → die Preise des Stromträgers aus der Kostenverwaltung gelten.
    ///
    /// <para><b>Kein Zeitzonentarif</b> (Entscheid Q11, Anwender 22.09.2026: „kein
    /// HT/NT"). Das vereinfachte Zonenmodell der Stufe W3 — Winter/Sommer × HT/NT mit
    /// je vier Zonenpreisen für Bezug und Einspeisung und einer zweistufigen
    /// Leistungspreis-Staffel — entfällt: Der Kern liest dessen Spalten nicht mehr und
    /// schreibt sie nicht mehr (sie bleiben bis zu einem späteren Aufräumschritt in der
    /// Tabelle stehen), die Staffel steht seit Schemaschritt 104 am Stromträger der
    /// Kostenverwaltung (<see cref="LeistungspreisStaffel"/>). Ein Satz mit dem Modus
    /// <c>ZONEN</c> (oder leer — Bestand vor Schritt 21) ist ein Satz des alten
    /// Zonenmodells und rechnet nicht (<see cref="Wirksam"/>); Schemaschritt 104 löscht
    /// ihn samt der mit ihm gerechneten gespeicherten Ergebnisse (Entscheid E7b‑Q4).</para>
    /// </summary>
    public class TarifParameter
    {
        public int IdStamm;
        public bool Aktiv;

        /// <summary>
        /// Die Winterspanne als Monatsspanne (über den Jahreswechsel möglich). Sie
        /// trennt im Lastbild Sommer- und Wintermaximum — die Bemessung des
        /// Leistungspreismodells <c>STAFFEL</c> der beiden Bezugsrollen.
        /// </summary>
        public int WinterVonMonat = 10;    // Oktober …
        public int WinterBisMonat = 3;     // … März (über den Jahreswechsel)

        // ---- ETAPPE E5 — Rollenmodell (Migrationsschritt 21) ----

        /// <summary>
        /// Tarifmodus, Steuerwert aus <c>DbWerte.TARIF_MODUS_*</c>. Nur <c>ROLLEN</c>
        /// rechnet; <c>ZONEN</c> ist der Wert des entfallenen Zonenmodells, und er ist
        /// die Vorbelegung, damit ein neu angelegter Satz erst mit dem Speichern des
        /// Dialogs (der <c>ROLLEN</c> schreibt) rechnet.
        /// </summary>
        public string Modus = DbWerte.TARIF_MODUS_ZONEN;

        /// <summary>Preisstand des Tarifsatzes; null = nicht gepflegt (nur Ausweis,
        /// keine Rechenwirkung). Der Altkatalog kannte ihn nur als Fließtext.</summary>
        public DateTime? GueltigAb;

        /// <summary>Bezugstarif OHNE BHKW — Referenz der vermiedenen Kosten.</summary>
        public TarifRolle Bezug = NeueRolle("BEZUG");

        /// <summary>Reststromtarif MIT BHKW — kleinere Abnahme, meist teurer.</summary>
        public TarifRolle Reststrom = NeueRolle("RESTSTROM");

        /// <summary>
        /// Einspeisetarif — Arbeits- und Grundpreis, KEIN Leistungspreis.
        ///
        /// <para>Begründet: Im Altkatalog sind Sollleistung und Reduktionsfaktoren des
        /// Einspeiseblatts leer oder 0, es gibt keinen aktiven Lesepfad, und der
        /// Leistungserlös der Einspeisung war fest 0 (Befund 11 der Analyse, von der
        /// Datenseite bestätigt in Abschnitt 7.1).</para>
        /// </summary>
        public TarifRolle Einspeisung = NeueRolle("EINSPEISUNG");

        /// <summary>true, wenn das Rollenmodell der Etappe E5 gilt.</summary>
        public bool RollenModus
        { get { return string.Equals(Modus, DbWerte.TARIF_MODUS_ROLLEN, StringComparison.Ordinal); } }

        /// <summary>
        /// Rechnet dieser Tarifsatz? Nur ein AKTIVER Satz im ROLLENmodell (Q11, Etappe
        /// E7b): Einen Zeitzonentarif gibt es nicht mehr, ein Satz im Zonenmodell wirkt
        /// nicht. Wer fragt, ob ein Lauf Stundenreihen braucht, fragt deshalb diese
        /// Eigenschaft und nicht <see cref="Aktiv"/>.
        /// </summary>
        public bool Wirksam
        { get { return Aktiv && RollenModus; } }

        /// <summary>Eine Rolle mit vier leeren Staffelstufen (Vorbelegung MONATLICH).</summary>
        private static TarifRolle NeueRolle(string rolle)
        {
            var r = new TarifRolle { Rolle = rolle };
            for (int i = 0; i < 4; i++) r.Stufen.Add(new LeistungsStufe());
            return r;
        }

        /// <summary>Der Nachweis des Tarifsatzes in einer Zeile (Parameterzeile der Seite,
        /// Wort- und Excelbericht).</summary>
        public string Nachweis(System.Globalization.CultureInfo kultur)
        {
            if (!Aktiv) return "Tarifstruktur inaktiv (Flat-Preise der Kostenmaske)";
            if (RollenModus)
            {
                string t = "Tarif aktiv (Rollenmodell): Bezug " +
                    Bezug.ArbeitspreisEurKWh.ToString("N4", kultur) + " €/kWh (" +
                    Bezug.Leistungsmodell + ") · Reststrom " +
                    Reststrom.ArbeitspreisEurKWh.ToString("N4", kultur) + " €/kWh (" +
                    Reststrom.Leistungsmodell + ") · Einspeisung " +
                    Einspeisung.ArbeitspreisEurKWh.ToString("N4", kultur) + " €/kWh · Winter " +
                    WinterVonMonat + "–" + WinterBisMonat;
                if (GueltigAb.HasValue)
                    t += " · Preisstand " + GueltigAb.Value.ToString("dd.MM.yyyy", kultur);
                return t;
            }
            // Q11 (Anwender 22.09.2026, „kein HT/NT"): Einen Zeitzonentarif gibt es
            // nicht mehr. Ein Satz, der noch aktiv auf dem Zonenmodell steht, rechnet
            // nicht — der Nachweis sagt das, statt Zonenpreise zu zeigen, die nicht gelten.
            return MyResource.Resource.WIRT_TARIF_NACHWEIS_ZONEN;
        }
    }

    /// <summary>Ein Kraftwerkspark-Katalogeintrag (Tab_Kraftwerkspark, Stufe W3).</summary>
    public class Kraftwerkspark
    {
        public int Id;
        public string Bezeichner = "";
        public double WirkungsgradProzent = 100;   // el. Wirkungsgrad; 100 % = Faktoren je kWh Strom
        public double CO2;                         // g/kWh Brennstoff
        public double SO2;                         // mg/kWh Brennstoff
        public double NOx;                         // mg/kWh Brennstoff
        public double NetzverlusteProzent;
    }

    /// <summary>
    /// Emissionsbilanz gekoppelte vs. getrennte Erzeugung (Konzept Kap. 2.8, W3):
    /// getrennt = dieselbe Brennstoff-Wärme im Referenzkessel + derselbe KWK-Strom
    /// im Referenz-Kraftwerkspark. null = mangels Faktoren nicht bestimmbar.
    /// </summary>
    public class EmissionsBilanz
    {
        public int IdProjekt;
        public double? CO2GekoppeltT;      // t/a
        public double? CO2GetrenntT;
        public double? SO2GekoppeltKg;     // kg/a
        public double? SO2GetrenntKg;
        public double? NOxGekoppeltKg;     // kg/a
        public double? NOxGetrenntKg;
        public string ParkName = "";
        public string Hinweis;             // z. B. fehlende Faktoren

        /// <summary>
        /// Der Berechnungsmodus, in dem die CO₂-Zeilen dieser Bilanz entstanden sind
        /// (Etappe E5, Konzept F7): <c>CO2</c> oder <c>CO2E</c>. Reiter, Word und Excel
        /// beschriften die Zeile danach (<see cref="EmissionsAusweis.BilanzZeile"/>) —
        /// nicht nach der Einstellung, die beim Drucken gerade gilt. SO₂ und NOx sind
        /// vom Modus nicht berührt.
        /// </summary>
        public string Modus = DbWerte.EMISSION_MODUS_CO2;

        // ---- LEITENTSCHEIDUNGEN L12 und L13 ----

        /// <summary>
        /// Die angewandten Bilanzierungsregeln dieser Rechnung (L12/L13) — Grundlage
        /// des Ausweises in Reiter, Word und Excel. <c>null</c> nur, wenn die Bilanz
        /// gar nicht erst gerechnet wurde.
        /// </summary>
        public BilanzKonvention Konvention;

        /// <summary>
        /// Biogenes Verbrennungs-CO₂ [t/a], das die Konvention
        /// <c>BIOMASSE_KONVENTION_VERBRENNUNG</c> dem gekoppelten System zurechnet;
        /// 0 beim Nullansatz oder ohne biogenen Brennstoff. Steht getrennt, damit im
        /// Bericht sichtbar bleibt, welcher Teil der gekoppelten Emission aus der
        /// gewählten Konvention stammt und welcher aus dem Brennstoffkatalog.
        /// </summary>
        public double CO2BiogenT;

        /// <summary>
        /// Gutschrift des KWK-Stroms in der getrennten Referenz [t CO₂/a] — je nach
        /// Methode aus dem Kraftwerkspark, aus dem Substitutionsfaktor oder 0. Der
        /// Betrag, um den sich die Bilanz durch den Methodenwechsel L12 verschiebt.
        /// </summary>
        public double CO2GutschriftStromT;

        public double? CO2VermeidungT
        {
            get
            {
                return (CO2GekoppeltT.HasValue && CO2GetrenntT.HasValue)
                    ? (double?)(CO2GetrenntT.Value - CO2GekoppeltT.Value) : null;
            }
        }
    }

    /// <summary>Szenariennamen (durchgängig Worst / Erwartet / Best, VALERI-Vorbild).</summary>
    public static class WirtschaftlichkeitSzenario
    {
        public const string ERWARTET = "Erwartet";
        public const string BEST = "Best";
        public const string WORST = "Worst";
        public static readonly string[] Alle = { ERWARTET, BEST, WORST };
    }

    /// <summary>
    /// Ergebnis der Kapitalwertrechnung für EIN Projekt und EIN Szenario
    /// (persistiert in Tab_ErgebnisWirtschaftlichkeit; FK ID_Ergebnis bindet
    /// das Ergebnis an den Simulationslauf, Konzept Kap. 5.5).
    /// Alle Kennzahlen nullable: null = nicht bestimmbar (Anzeige „—", nie 0).
    /// </summary>
    public class WirtschaftlichkeitErgebnis
    {
        public int IdProjekt;
        public int IdErgebnis;                 // Tab_Ergebnis.ID des zugrunde liegenden Laufs
        public string Szenario = WirtschaftlichkeitSzenario.ERWARTET;
        public bool IstStamm;
        public string Anzeige = "";            // Varianten-/Projektname für UI und Bericht
        public DateTime Zeitstempel = DateTime.Now;

        // Zahlungsgerüst (Jahr 1 bzw. t=0)

        /// <summary>
        /// Investitionssumme [€] (Kategorie 1, Szenariowert) — <b>vor</b> Abzug eines
        /// Zuschusses. Das ist die Zahl, die der Anwender erfasst hat, und die
        /// Bezugsgröße jeder prozentualen Betriebskostenbemessung.
        /// <para>Die tatsächliche Anfangsauszahlung ist
        /// <c>Investition − <see cref="Zuschuss"/></c>; sie steckt im
        /// <see cref="Kapitalwert"/> und wird nicht getrennt abgelegt, damit es zu ihr
        /// keine zweite Wahrheit gibt.</para>
        /// </summary>
        public double Investition;

        /// <summary>
        /// ETAPPE K5 (Konzept § 7.4, L7): angesetzter Investitionszuschuss [€], positiv.
        /// 0 = kein Zuschuss erfasst. Er mindert I₀ einmalig — keine
        /// Ersatzbeschaffung, kein Restwert. Ausgewiesen wird er als eigene, negativ
        /// dargestellte Zeile („Zuschuss: −X €").
        /// </summary>
        public double Zuschuss;

        public double? BetriebskostenJahr;     // [€/a] (Kategorie 2, Szenariowert)
        public double? EnergiekostenJahr;      // [€/a] (KostenEmissionRechner; null = Preise fehlen)
        public double EinspeiseerloesJahr;     // [€/a] (PV-Überschuss × Einspeisevergütung)

        // Barwerte über T
        public double? BarwertAusgaben;        // Betrieb + Energie + Ersatzbeschaffungen [€]
        public double? BarwertEinnahmen;       // Einspeiseerlöse [€]
        public double RestwertBarwert;         // linearer Restwert, abgezinst [€]

        /// <summary>
        /// ETAPPE W5‑B‑10 (VALERI-Abgleich, 09.09.2026): Barwert der
        /// ERSATZBESCHAFFUNGEN [€], positiv.
        ///
        /// <para>Gerechnet wurden sie seit W1 (eine Position mit Nutzungsdauer n &lt; T
        /// wird in t = n, 2n, … erneut beschafft), ausgewiesen aber nie: Sie steckten
        /// stumm in <see cref="BarwertAusgaben"/>. DIN EN 17463 verlangt sie als eigene
        /// Position — zusammen mit dem Restwert sind sie die zwei Größen, an denen
        /// hängt, ob ein Betrachtungszeitraum überhaupt zur Nutzungsdauer passt.</para>
        ///
        /// <para><b>Reiner Ausweis.</b> Der Kapitalwert ist unverändert; diese Zahl wird
        /// aus dem fertigen Zahlungsbild abgeleitet und nirgends aufsummiert.</para>
        /// </summary>
        public double ErsatzBarwert;

        // Stufe W2 (Phase 7)
        public double CO2AbgabeJahr;           // BEHG-Abgabe im Jahr 1 [€/a] (0 = aus/kein Brennstoff)
        public double KwkgErloesJahr1;         // KWKG-Bonus im Jahr 1 [€/a] (0 = aus/kein BHKW)

        /// <summary>
        /// ETAPPE E2 (Leitentscheidung L6): die erreichten ELEKTRISCHEN
        /// Vollbenutzungsstunden [h/a], leistungsgewichtet über alle BHKW-Module —
        /// die Größe, mit der die KWKG-Deckelung rechnet.
        ///
        /// <para>Bis E2 wurde dafür die Summe THERMISCHER Vbh verwendet
        /// (<c>Ergebnis.BHKW.Betriebsstunden_Gesamt</c>); sie kann 8.760 h überschreiten
        /// und setzte den Zuschlag bei Mehrmodulanlagen zu hoch an. Der Wert steht hier,
        /// damit Reiter und Bericht die Bemessungsgrundlage ausweisen können statt nur
        /// ihr Ergebnis.</para>
        ///
        /// <para>0 = kein BHKW im Lauf, kein KWK-Strom oder keine elektrische
        /// Nennleistung gepflegt.</para>
        /// </summary>
        public double KwkgVbhElektrisch;       // h/a

        /// <summary>
        /// AUFTRAG U17 — die pauschale Vorauszahlung nach § 9 KWKG [€], EINMALIG im
        /// Jahr 0 und nicht abgezinst. 0 = die Pauschale greift in diesem Lauf nicht
        /// (nicht gewählt, über der Leistungsgrenze von 2 kW<sub>el</sub>, ohne
        /// gepflegte Nennleistung oder ohne Katalogwerte).
        ///
        /// <para><b>Die Einheit ist € und nicht €/a.</b> Deshalb steht der Betrag in
        /// einer eigenen Zeile des Blocks A und NICHT in dessen €/a-Summe — derselbe
        /// Grund, aus dem der Restwert dort nicht steht. Im Kapitalwert ist er seit
        /// jeher enthalten: Die Erlösreihe <c>KWKG_PAUSCHALE</c> trägt ihn im Index 0,
        /// den <c>KapitalwertRechner</c> unabgezinst auf den Startwert bucht. Diese
        /// Zahl ist reiner AUSWEIS und wird nirgends aufsummiert.</para>
        ///
        /// <para>Sie reist im Nachweisumschlag mit
        /// (<see cref="ErgebnisNachweisUmschlag"/>), nicht in einer eigenen
        /// Ergebnisspalte: Der Referenzexport liest <c>Tab_Ergebnis</c> per
        /// <c>SELECT *</c>, und eine neue Spalte änderte jeden Vergleich.</para>
        /// </summary>
        public double KwkgPauschaleEur;        // €, einmalig im Jahr 0

        // ---- ETAPPE E4 — Steuergutschriften, Jahr 1 der jahresscharfen Reihen ----
        //
        // 0 = keine Gutschrift. Der GRUND steht immer in Hinweis (nie eine stille Null):
        // nicht gewählt, Bedingung nicht erfüllt, Satz nicht gepflegt oder Menge nicht
        // in die gesetzliche Einheit umrechenbar.

        /// <summary>Energiesteuer-Entlastung GESAMT im Jahr 1 [€/a] — die Summe aus
        /// <see cref="Energiesteuer53Jahr1"/> und <see cref="Energiesteuer54Jahr1"/>.
        /// Sie steht in der Ergebnisspalte und in der Erlösreihe des Kapitalwerts;
        /// die Aufteilung darunter ändert an ihr nichts (U7).</summary>
        public double EnergiesteuerJahr1;

        /// <summary>
        /// AUFTRAG U7 — der Anteil nach <b>§ 53 bzw. § 53a Abs. 5 EnergieStG</b>
        /// [€/a]: der Brennstoff der Stromerzeugung, also das Blockheizkraftwerk.
        /// Gültig nur, wenn <see cref="EnergiesteuerAufgeteilt"/> gesetzt ist.
        /// </summary>
        public double Energiesteuer53Jahr1;

        /// <summary>
        /// AUFTRAG U7 — der Anteil nach <b>§ 54 EnergieStG</b> [€/a] nach Abzug des
        /// Sockelbetrags: der Heizstoff des produzierenden Gewerbes, also auch der
        /// Kessel. Gültig nur, wenn <see cref="EnergiesteuerAufgeteilt"/> gesetzt ist.
        /// </summary>
        public double Energiesteuer54Jahr1;

        /// <summary>
        /// AUFTRAG U7 — der abgezogene Sockelbetrag des § 54 [€/a]; 0 = keine
        /// § 54-Position oder kein Sockel im Katalog.
        /// </summary>
        public double Energiesteuer54SockelJahr1;

        /// <summary>
        /// AUFTRAG U7 — <b>ist die Aufteilung bekannt?</b> Ein frisch gerechneter Lauf
        /// kennt sie immer; ein vor U7 gebuchter Stand trägt sie nicht
        /// (Nachweisumschlag der Fassung ≤ 3) und liest sich deshalb mit
        /// <c>false</c>.
        ///
        /// <para><b>Wozu der Merker.</b> Ohne ihn wären 0 und 0 von „beide Paragrafen
        /// haben 0 ergeben" nicht zu unterscheiden — und die Rubrik zeigte für einen
        /// alten Stand zwei Nullzeilen, obwohl die Ergebnisspalte einen Betrag führt.
        /// Ist er nicht gesetzt, bleibt es bei der EINEN Zeile über beide Vorschriften;
        /// die Summe des Blocks A ist damit in jedem Fall zahlengleich.</para>
        /// </summary>
        public bool EnergiesteuerAufgeteilt;

        /// <summary>
        /// AUFTRAG U7 — je gerechneter Energiesteuerposition eine Zeile mit Paragraf,
        /// Menge, Satz und Betrag; sie trägt die Herleitung der beiden Rubrikzeilen.
        /// Leer = keine Entlastung gerechnet oder gebuchter Stand vor U7.
        /// <b>Im Nachweisumschlag persistiert</b> (Fassung 4).
        /// </summary>
        public List<EnergiesteuerNachweis> EnergiesteuerNachweise =
            new List<EnergiesteuerNachweis>();

        /// <summary>
        /// AUFTRAG 9d (Konzept § 6.3, Punkt B7-4) — die Begründung JE POSITION der
        /// Erlösrubrik: Schlüssel ist eine Kennung aus <c>SteuerPosition</c>, Wert der
        /// Satz, mit dem der Steuerrechner die Null begründet hat.
        ///
        /// <para>Bis 9d stand dieselbe Auskunft ausschließlich in
        /// <see cref="Hinweis"/> — als EIN mit „ | " verbundener Text über alle
        /// Positionen. Die Rubrik konnte daraus keine Zeile bedienen und nannte
        /// deshalb nur die BEDINGUNG der Position. Leer = der Lauf hat zu dieser
        /// Position nichts festgestellt (oder ein Stand vor 9d).
        /// <b>Im Nachweisumschlag persistiert</b> (Fassung 5).</para>
        /// </summary>
        public Dictionary<string, string> PositionsGruende =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG im Jahr 1
        /// [€/a] — Regelsatz auf den KWK-Eigenverbrauch.</summary>
        public double StromsteuerBefreiungJahr1;

        /// <summary>
        /// ETAPPE B6: true = die Befreiung ist in diesem Lauf als Erlösreihe gebucht und
        /// steckt im Kapitalwert (Modus <c>ERLOES</c>); false = sie ist nur AUSGEWIESEN
        /// (Vorgabe <c>AUSWEIS</c>) — dann steht der Betrag in
        /// <see cref="StromsteuerBefreiungJahr1"/>, aber in keiner Zahlungsreihe.
        ///
        /// <para>Der Merker wandert mit ins Ergebnis, damit die Vergleichstabelle ihre
        /// Zeile beschriften kann, ohne den Parametersatz zu kennen — auch nach dem
        /// Laden eines gespeicherten Laufs.</para>
        /// </summary>
        public bool StromsteuerBefreiungAlsErloes;

        /// <summary>Stromsteuer-Entlastung nach § 9b StromStG im Jahr 1 [€/a] —
        /// Entlastungssatz auf den Netzbezug abzüglich Sockelbetrag.</summary>
        public double StromsteuerEntlastungJahr1;

        /// <summary>
        /// Herkunft der verwendeten Steuersätze (Fundstelle, Wert, Einheit, Gültigkeits-
        /// jahr und Status je Satz) — aus <c>GesetzKatalog.WertMitHerkunft</c> gebildet.
        /// <c>null</c> = keine Gutschrift gerechnet, also auch kein Satz verwendet.
        /// </summary>
        public string SteuerHerkunft;

        // ---- ETAPPE E5 — Strom und Erlöse nach der Differenzmethode ----
        //
        // Alle vier Werte sind 0, solange der Tarif nicht im ROLLEN-Modus steht bzw.
        // keine Stundenreihen vorliegen. Sie sind AUSWEIS, kein zweiter Rechenweg:
        // Der Kapitalwert nimmt die vermiedenen Kosten nicht als Erlös auf — er rechnet
        // mit den TATSÄCHLICHEN Reststromkosten, in denen die Einsparung bereits steckt.
        // Eine zusätzliche Erlöszeile wäre eine Doppelzählung.

        /// <summary>Vermiedene Kosten, Arbeitsanteil [€/a] (Etappe E5).</summary>
        public double VermiedenArbeitJahr;

        /// <summary>
        /// Vermiedene Kosten, Leistungsanteil [€/a] — <b>regelmäßig negativ</b>, weil der
        /// Reststrom-Leistungspreis über dem Bezugs-Leistungspreis liegt. Eigene Zeile,
        /// weil genau das die Kernaussage der Rechnung ist (Konzept 4.3).
        /// </summary>
        public double VermiedenLeistungJahr;

        /// <summary>Vermiedene Kosten gesamt [€/a] (Arbeit + Leistung + Grundpreis).</summary>
        public double VermiedenGesamtJahr;

        /// <summary>
        /// ETAPPE B7 (Konzept § 2.6, Klarstellung 1) — die vermiedene Strommenge
        /// [MWh/a]; 0 = nicht bestimmbar (keine Stundenreihen, kein Rollenmodell).
        ///
        /// <para>Sie steht in keiner eigenen Ergebnisspalte, weil sie ausschliesslich den
        /// AUSWEIS traegt: Der Kapitalwert rechnet unveraendert mit den tatsaechlichen
        /// Reststromkosten, in denen die Einsparung bereits steckt. Seit B7P reist sie im
        /// Nachweisumschlag mit (<see cref="ErgebnisNachweisUmschlag"/>) — ein
        /// gespeicherter Lauf traegt die Korrekturzeile der Rubrik also ebenso wie der
        /// frisch gerechnete.</para>
        /// </summary>
        public double VermiedenMengeMWh;

        /// <summary>
        /// ETAPPE B7 — die ENTGANGENE Entlastung nach § 9b StromStG [€/a]:
        /// Entlastungssatz × <see cref="VermiedenMengeMWh"/>, und nur bei einem
        /// Unternehmen des produzierenden Gewerbes (bzw. der Land- und Forstwirtschaft).
        ///
        /// <para><b>Warum es sie gibt.</b> Die Differenzmethode rechnet beide Seiten mit
        /// demselben Arbeitspreis, und der traegt die Stromsteuer mit dem vollen Satz.
        /// Wer nach § 9b entlastet wird, vermeidet mit dem Bezug auch die Entlastung —
        /// der ausgewiesene Vorteil ist um genau diesen Betrag zu hoch. Im KAPITALWERT
        /// ist das seit jeher richtig erfasst (die § 9b-Reihe rechnet auf den kleineren
        /// Netzbezug); allein der Ausweis wies brutto aus.</para>
        /// </summary>
        public double VermiedenEntlastung9bJahr;

        /// <summary>
        /// ETAPPE B7 — true, wenn der Lauf ein Unternehmen des produzierenden Gewerbes
        /// (oder der Land- und Forstwirtschaft) gerechnet hat. Die Rubrik kennzeichnet
        /// damit A5, A6 und B1. Im Nachweisumschlag mitgespeichert
        /// (<see cref="ErgebnisNachweisUmschlag"/>).
        /// </summary>
        public bool ProduzierendesGewerbe;

        /// <summary>Vermiedene Kosten EFFEKTIV [€/a] — brutto abzueglich der entgangenen
        /// § 9b-Entlastung (B7). Ohne Korrektur ist sie gleich dem Bruttobetrag.</summary>
        public double VermiedenEffektivJahr
        {
            get { return VermiedenGesamtJahr - VermiedenEntlastung9bJahr; }
        }

        /// <summary>
        /// AUFTRAG U6 (Q15/A12, 22.09.2026) — die AUFTEILUNG der vermiedenen Stromkosten
        /// auf die Anlagen. Leer = keine Aufteilung im Lauf (kein Rollenmodell, keine
        /// Eigenverbrauchsmengen) — dann steht die Rubrik wie vor U6 bei EINER
        /// projektweiten Zeile.
        ///
        /// <para><b>Nur Ausweis, nie Rechenweg.</b> Die Summe der Zeilen ist zahlengleich
        /// der projektweiten Groesse, aus der sie entstanden sind
        /// (<see cref="VermiedenArbeitJahr"/>, <see cref="VermiedenMengeMWh"/>,
        /// <see cref="VermiedenEntlastung9bJahr"/>); verteilt wird, nicht gerechnet. Der
        /// LEISTUNGSANTEIL bleibt projektweit (Anwenderentscheid Q15) und steht deshalb
        /// in keiner dieser Zeilen.</para>
        ///
        /// <para>Im Nachweisumschlag mitgespeichert (Fassung 6) — der gebuchte Stand
        /// traegt die Komponentenbloecke ebenso wie der frisch gerechnete.</para>
        /// </summary>
        public List<VermiedenAnlageNachweis> VermiedenJeAnlage =
            new List<VermiedenAnlageNachweis>();

        /// <summary>
        /// Betrag, um den die Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession,
        /// Vertrieb) die Energiekosten des Jahres 1 erhöhen [€/a]. 0 = Schalter aus oder
        /// nichts gepflegt. Der Wert steht getrennt, damit die Wirkung sichtbar bleibt,
        /// statt in den Energiekosten zu verschwinden.
        /// </summary>
        public double AufschlagJahr;

        // ---- ETAPPE E7 — Aufschlüsselungen und Nachweise für den Bericht ----
        //
        // Alle vier Größen sind AUSGABE: Sie ändern keine Rechnung, sondern zerlegen
        // bzw. begründen Zahlen, die es vorher nur als Summe gab.

        /// <summary>
        /// Einspeiseerlös aus dem <b>PV-Überschuss</b> [€/a] (Etappe E7).
        /// <para><see cref="EinspeiseerloesJahr"/> verschmolz bis dahin PV-Überschuss und
        /// KWK-Einspeisung zu einer Zahl — zwei Mengen mit zwei Preisen und zwei
        /// Rechtsgrundlagen. Die Summe der beiden Teile ist der Gesamtbetrag.</para>
        /// </summary>
        public double EinspeiseerloesPvJahr;

        /// <summary>Einspeiseerlös aus der <b>KWK-Einspeisung</b> [€/a] (Etappe E7).</summary>
        public double EinspeiseerloesKwkJahr;

        // ---- ETAPPE P6 (PV-Konzept § 6.4) — Ausweis des PV-Vergütungsdialogs ----
        //
        // Gefüllt nur, wenn der Dialog aktiv war (PvVerguetungsform nicht leer).
        // Alles AUSWEIS aus PvErloesErgebnis — der Kapitalwert nimmt die Reihe
        // ErloesReihe.PV_VERGUETUNG; hier steht ihre Herkunft für Reiter und Bericht.

        /// <summary>Vermarktungsform des Laufs (DbWerte.PV_VERMARKTUNG_*); leer = Dialog inaktiv.</summary>
        public string PvVerguetungsform = "";

        /// <summary>Anzulegender Wert AW_mix [ct/kWh] (§ 23c-Mix der gerundeten Klassenwerte).</summary>
        public double? PvAnzulegenderWert;

        /// <summary>Marktprämie im Jahr 1 [€/a]; 0 = feste EV bzw. keine Prämie.</summary>
        public double PvMarktpraemie;

        /// <summary>§ 51 EEG: entfallene Vergütungsmenge [kWh/a], Jahr-1-Sicht.</summary>
        public double PvVerguetungsausfallKwh;

        /// <summary>§ 51 EEG: entgangener Betrag [€/a].</summary>
        public double PvVerguetungsausfall;

        /// <summary>§ 51a EEG: Gutschrift im letzten Vergütungsjahr [€, nominal].</summary>
        public double PvKompensation51a;

        /// <summary>60-%-Kappung (§ 9 Abs. 2 EEG): verlorene Menge [kWh/a]; 0 = keine Kappung.</summary>
        public double PvKappungsverlustKwh;

        // ---- KONZEPT § 2.16 — die HERKUNFT der Vergütung dieses Stands ----
        //
        // Sie ist kein Rechenwert, sondern die Aussage, ohne die zwei Varianten
        // derselben Gruppe mit verschiedenen Vergütungen nicht zu unterscheiden sind.
        // Beide Felder reisen im ErgebnisNachweisUmschlag mit (Fassung 3) — deshalb
        // nennt der Bericht die Herkunft auch beim gebuchten Stand, ohne neue Spalte in
        // Tab_Ergebnis.

        /// <summary>Rechnet dieser Stand mit der Vergütung seines Stammprojekts?
        /// <c>false</c> = eigene Werte (beim Stamm immer).</summary>
        public bool PvVerguetungUebernommen;

        /// <summary>Name des Stammprojekts bei Übernahme; leer bei eigenen Werten.</summary>
        public string PvVerguetungQuelle = "";

        /// <summary>
        /// Vermiedener Netzbezug durch PV-Eigenverbrauch [€/a], INFORMATIV —
        /// Jahr-1-Sicht: (Erzeugung − Überschuss) × Strom-Arbeitspreis derselben
        /// Vorrangkette wie die Energiekosten. KEIN Bestandteil des Kapitalwerts:
        /// Der rechnet mit den tatsächlichen Reststromkosten, in denen die
        /// Einsparung bereits steckt (dieselbe Begründung wie bei den
        /// E5-Ausweiszeilen). null = kein Strompreis ermittelbar.
        /// </summary>
        public double? PvVermiedenerBezug;

        /// <summary>
        /// Ein Nachweis je BHKW-Modul der KWKG-Rechnung (Etappe E7, Übergabepunkt 1 aus
        /// E6). Leer = kein modulscharfer Lauf (Ersatzweg, kein BHKW oder kein
        /// gepflegter Satz).
        ///
        /// <para><b>Im Nachweisumschlag persistiert</b>
        /// (<see cref="ErgebnisNachweisUmschlag"/>, Etappe B7P): Die Zeilen entstehen bei
        /// jedem Lauf neu, aber der zuletzt gebuchte Stand trägt sie mit. Der
        /// Rückfallpfad auf gespeicherte Ergebnisse — Reiter, Word, Excel und Gruppe 5
        /// des BHKW-Dialogs — zeigt den Block deshalb auch ohne frischen Lauf.</para>
        /// </summary>
        public List<KwkgModulNachweis> KwkgModule = new List<KwkgModulNachweis>();

        /// <summary>
        /// Die Betriebskostenpositionen dieses Szenarios mit Kostenart, Bemessungsart und
        /// Herleitung (Etappe E7, Zweck der E3-Spalte <c>Kostenart</c>). Leer = keine
        /// Positionen oder Datenbank ohne die Spalten aus Migrationsschritt 19.
        /// <b>Im Nachweisumschlag persistiert</b> — dieselbe Begründung wie oben
        /// (<see cref="ErgebnisNachweisUmschlag"/>).
        /// </summary>
        public List<KostenPositionNachweis> Betriebskosten = new List<KostenPositionNachweis>();

        /// <summary>
        /// ETAPPE B7 — die Energiekosten je Anlage (Konzept § 3.5). Die Zeilen entstehen
        /// im Rechenlauf aus den Modulmengen; als Ergebnisspalte steht allein die Summe,
        /// die Zeilen selbst reisen seit B7P im Nachweisumschlag mit
        /// (<see cref="ErgebnisNachweisUmschlag"/>) — wie <see cref="KwkgModule"/>.
        /// </summary>
        public List<EnergieAnlageNachweis> EnergiekostenJeAnlage =
            new List<EnergieAnlageNachweis>();

        /// <summary>
        /// ETAPPE B2 (Konzept BHKW-Wirtschaftlichkeit § 4.1, BW2/BF2) — die Zeilen der
        /// Kohärenzprüfung: Widersprüche zwischen einer gebuchten Steuergutschrift und
        /// dem Steueranteil, den der erfasste Energiepreis ausweist. Leere Liste =
        /// konsistent (Fall 1) oder kein Steuerpfad im Lauf.
        ///
        /// <para><b>Reine Ausgabe, ohne jede Rechenwirkung</b> — Entscheidung BF2 lautet
        /// „nur warnen". Keine Gutschrift, keine Reihe und kein Kapitalwert ändern sich
        /// dadurch.</para>
        ///
        /// <para><b>Im Nachweisumschlag persistiert</b>, wie <see cref="KwkgModule"/> und
        /// <see cref="Betriebskosten"/> (<see cref="ErgebnisNachweisUmschlag"/>, B7P): Die
        /// Zeilen entstehen bei jedem Lauf neu und werden mit ihm gebucht; der
        /// Rückfallpfad auf gespeicherte Ergebnisse zeigt sie deshalb ebenfalls. Ist ein
        /// gespeicherter Umschlag unlesbar, steht hier genau EINE Zeile, die das
        /// sagt.</para>
        /// </summary>
        public List<KohaerenzHinweis> KohaerenzHinweise = new List<KohaerenzHinweis>();

        public double? IRR;                    // interner Zinsfuß der Differenzreihe [%] (null beim Stamm/nie)

        /// <summary>
        /// ETAPPE E5 (V‑A, Befund A2) — die Zahl der <b>Vorzeichenwechsel</b> der
        /// Differenzreihe gegen die Referenz, derselben Reihe, deren Nullstelle der
        /// interne Zinsfuß ist (<see cref="KapitalwertRechner.Vorzeichenwechsel(KapitalwertRechner.Zahlungsbild, KapitalwertRechner.Zahlungsbild)"/>).
        ///
        /// <para>Mehr als einer: Der Zinsfuß ist mehrdeutig (DIN EN 17463 Anhang C) —
        /// Kachel und Bericht warnen (<see cref="ValeriAusweis.IzfWarnung"/>). Keiner:
        /// Es gibt keinen Zinsfuß, und die Zelle nennt den Grund
        /// (<see cref="ValeriAusweis.IzfGrund"/>).</para>
        ///
        /// <para><c>null</c> = nicht gezählt: die Referenz selbst, ein Stand ohne
        /// Differenzrechnung oder ein gebuchter Stand, dessen Nachweisumschlag älter als
        /// Fassung 7 ist. Reiner Ausweis, im Nachweisumschlag persistiert — keine
        /// Kennzahl ändert sich dadurch.</para>
        /// </summary>
        public int? IrrVorzeichenwechsel;

        /// <summary>E5: Wechselt die Differenzreihe mehr als einmal das Vorzeichen?</summary>
        public bool IrrMehrdeutig
        {
            get { return IrrVorzeichenwechsel.HasValue && IrrVorzeichenwechsel.Value > 1; }
        }

        /// <summary>
        /// ETAPPE E5 (Konzept § 6.3 Nr. 31, entschieden 22.09.2026) — dieses Ergebnis
        /// wurde aus der Datenbank geladen und trägt <b>keinen Nachweisumschlag</b>
        /// (<c>Nachweis_Json</c> leer): Es ist vor B7P gebucht oder sein Umschlag
        /// konnte nicht geschrieben werden. Seine Unterzeilen (Module, Anlagen,
        /// Positionen, Kohärenz) fehlen deshalb, bis der nächste Rechenlauf ihn
        /// schreibt — Ansicht und Bericht kennzeichnen das mit
        /// <see cref="ValeriAusweis.NachweisKennzeichen"/>, statt die Lücke still zu
        /// lassen. Kein Nachziehlauf (Entscheid Nr. 31).
        ///
        /// <para>Ein frisch gerechnetes Ergebnis trägt immer <c>false</c>: Es hält seine
        /// Nachweise im Speicher.</para>
        /// </summary>
        public bool OhneNachweis;

        // Stufe W3 (Phase 8)
        public double? StromkostenTarif;       // Reststromkosten nach Rollentarif [€/a] (null = Flat-Rechnung)
        public string Hinweis;                 // nicht-fataler Hinweis (z. B. Tarif ohne Stundenreihen)

        /// <summary>
        /// <b>Bezugsspitze Strom [kW]</b> — Jahresmaximum des Netzbezugs im
        /// Viertelstundenraster dieses Laufs (<see cref="Netzbezugsspitze"/>);
        /// <c>null</c> = der Lauf führte keine Zeitreihen.
        ///
        /// <para><b>Herleitung, keine Zahlung.</b> Sie ist die Basis des
        /// Strom-Leistungspreises und die Zahl, an der der Vergleich den Effekt der
        /// Lastspitzenkappung zeigt: Stamm gegen Speichervariante, bei nahezu gleicher
        /// Arbeit.</para>
        ///
        /// <para><b>Im Nachweisumschlag persistiert</b>, wie <see cref="KwkgModule"/> und
        /// <see cref="Betriebskosten"/> (<see cref="ErgebnisNachweisUmschlag"/>, B7P):
        /// Gespeichert wird die Spitze DIESES Laufs, zusammen mit seinen übrigen
        /// Ergebniszahlen — sie beschreibt damit denselben Lauf wie sie.</para>
        /// </summary>
        public double? BezugsspitzeKW;

        // Kennzahlen
        public double? Kapitalwert;            // absoluter Nettobarwert des Projekts [€]
        public double? KapitalwertDiff;        // KW gegenüber Stamm [€] (null beim Stamm)
        public double? AnnuitaetKW;            // KapitalwertDiff × a(i,T) [€/a] (null beim Stamm)
        public double? AmortisationJahre;      // dynamisch, ohne Restwert (null = nie/Stamm)
        public double? Gestehungskosten;       // Wärmegestehungskosten [€/kWh]

        /// <summary>null = Rechnung vollständig; sonst Begründung („kein Arbeitspreis …").</summary>
        public string Fehlgrund;
    }

    /// <summary>
    /// ETAPPE E7 — der Nachweis EINES BHKW-Moduls in der KWKG-Rechnung.
    ///
    /// <para>Seit E6 entsteht der KWK-Zuschlag als eine Reihe je Modul mit eigenem Satz,
    /// eigenem Inbetriebnahmejahr, eigenem Jahresdeckel und eigenem Kontingent. Bis E7
    /// stand davon nur eine Aufzählung im Hinweisfeld — bei drei Modulen eine
    /// unlesbare Zeile (E6-Protokoll, Übergabepunkt 1). Hier steht dieselbe Auskunft
    /// strukturiert, damit der Bericht eine Tabelle daraus bauen kann.</para>
    ///
    /// <para><b>Alle Werte sind die TATSÄCHLICH angesetzten</b>, nicht die
    /// vorgeschlagenen. <see cref="HerleitungEigen"/> und
    /// <see cref="HerleitungEinspeisung"/> nennen zusätzlich die Tranchenrechnung des
    /// § 7 KWKG zu diesem Modul — erst damit ist der angesetzte Satz nachvollziehbar
    /// und eine Abweichung vom Katalog sichtbar.</para>
    /// </summary>
    /// <summary>
    /// ETAPPE B7 (Konzept § 3.5) — die ENERGIEKOSTEN EINER ANLAGE: Menge × Preis, so
    /// wie sie in die Jahressumme eingegangen sind.
    ///
    /// <para><b>Warum die Aufschlüsselung.</b> Die Zeile „Energiekosten [€/a]" ist in
    /// jedem Projekt die größte laufende Position und zugleich die einzige, die bis B7
    /// gar nichts über sich sagte. Ob sie hoch ist, weil ein Kessel viel verbraucht oder
    /// weil ein Preis falsch gepflegt ist, war an ihr nicht abzulesen — und wer sie
    /// nachrechnen wollte, musste den Rechenweg lesen.</para>
    ///
    /// <para><b>Nur Ausweis.</b> Die Summe der Zeilen ist die vorhandene Zahl; hier wird
    /// nichts gerechnet, was dort nicht schon gerechnet wurde. Der GRUNDPREIS je Träger
    /// steckt bewusst nicht in den Anlagenzeilen — er fällt einmal je Träger an, nicht
    /// je Anlage, und stünde an jeder Anlage anteilig als erfundene Zahl.</para>
    /// </summary>
    public class EnergieAnlageNachweis
    {
        /// <summary>Bezeichner der Anlage — ein Datenwert des Anwenders, kein
        /// Anzeigetext; leer = unbenannte Modulzeile.</summary>
        public string Anlage = "";

        /// <summary>Name des Energieträgers, mit dem gerechnet wurde.</summary>
        public string Traeger = "";

        /// <summary>Einsatz der Anlage [MWh/a], heizwertbezogen (Strom: Netzbezug).</summary>
        public double MengeMWh;

        /// <summary>Menge in der ABRECHNUNGSEINHEIT des Trägers (Liter, kg, m³, kWh) —
        /// die Größe, mit der der Arbeitspreis multipliziert wurde.</summary>
        public double MengeAbrechnung;

        /// <summary>Abrechnungseinheit des Trägers; leer = je kWh abgerechnet.</summary>
        public string Einheit = "";

        /// <summary>Arbeitspreis je Abrechnungseinheit [€].</summary>
        public double PreisJeEinheit;

        /// <summary>Kosten dieser Anlage [€/a] — ohne Grund- und Leistungspreis.</summary>
        public double KostenEur;
    }

    /// <summary>
    /// AUFTRAG U6 (Befund R8, Anwenderentscheid Q15/A12 vom 22.09.2026) — der Anteil
    /// EINER Komponente an den vermiedenen Stromkosten.
    ///
    /// <para><b>Warum es ihn gibt.</b> Die vermiedenen Stromkosten entstehen als
    /// Differenz zweier PROJEKTweiter Rollenrechnungen (Bezug ohne Anlage gegen
    /// Reststrom mit Anlage). Wer fragt „was bringt das Blockheizkraftwerk?", bekommt
    /// aus dieser Differenz keine Antwort — sie kennt die Anlage nicht. Die Strommatrix
    /// hilft nicht weiter: Sie trennt nicht nach Anlage (Befund R8).</para>
    ///
    /// <para><b>Die Naeherung V‑4, ausgewiesen.</b> Verteilt wird nach dem
    /// Eigenverbrauch je Anlage (<see cref="EigenMWh"/>) — seit E7 (Konzept § 6.3
    /// Nr. 32) BRUTTO aus der Strommatrix: das Blockheizkraftwerk mit seinem
    /// Eigenverbrauch nach der min-Regel, die Photovoltaik mit ihrer Eigennutzung; der
    /// Hilfsstrom beruehrt die vermiedene Menge nicht. Bei genau EINER Anlage ist das
    /// exakt; bei mehreren ist es eine Annahme, und <see cref="IstNaeherung"/> sagt es.
    /// Modulscharfe Stundenreihen waeren die Alternative — ein Simulationsthema, kein
    /// Rubrikthema (A12).</para>
    ///
    /// <para><b>Es wird verteilt, nicht gerechnet:</b> Die Summe von
    /// <see cref="MengeMWh"/>, <see cref="ArbeitEur"/> und
    /// <see cref="Entlastung9bEur"/> ueber alle Zeilen ist zahlengleich der
    /// projektweiten Groesse. Kein Kapitalwert, keine Reihe, keine Summe aendert sich.</para>
    /// </summary>
    public class VermiedenAnlageNachweis
    {
        /// <summary>Sprachneutrale Kennung der Komponente
        /// (<c>WirtZeile.KOMPONENTE_BHKW</c>, <c>_PV</c>) — kein Anzeigetext.</summary>
        public string Komponente = "";

        /// <summary>Bezeichner der Anlage — ein Datenwert des Anwenders; leer = die
        /// Komponente steht fuer mehrere Anlagen (Sammelzeile der Technik).</summary>
        public string Anlage = "";

        /// <summary>Eigenverbrauch dieser Komponente [MWh/a] — der VERTEILSCHLUESSEL,
        /// nicht das Ergebnis.</summary>
        public double EigenMWh;

        /// <summary>Anteil am gesamten Eigenverbrauch [0…1].</summary>
        public double Anteil;

        /// <summary>Zugeteilte vermiedene Menge [MWh/a].</summary>
        public double MengeMWh;

        /// <summary>Zugeteilter ARBEITSanteil der vermiedenen Kosten [€/a]. Der
        /// Leistungsanteil bleibt projektweit (Q15) und steht hier nie.</summary>
        public double ArbeitEur;

        /// <summary>Zugeteilte entgangene Entlastung nach § 9b StromStG [€/a],
        /// positiv; in der Rubrik steht sie als Abzug.</summary>
        public double Entlastung9bEur;

        /// <summary>true, wenn der Verteilschluessel eine Naeherung ist (mehr als eine
        /// Komponente) — die Zwischensumme sagt es dann (A12).</summary>
        public bool IstNaeherung;

        /// <summary>Wirksamer Anteil dieser Komponente [€/a] — Arbeit abzueglich der
        /// entgangenen Entlastung.</summary>
        public double WirksamEur
        {
            get { return ArbeitEur - Entlastung9bEur; }
        }

        /// <summary>
        /// AUFTRAG U6 — <b>der Verteilschluessel, an einer Stelle</b>: Er nimmt die
        /// Eigenverbrauchsmengen je Komponente (<see cref="EigenMWh"/> der uebergebenen
        /// Zeilen) und legt die projektweiten Groessen anteilig darauf.
        ///
        /// <para><b>Verteilt, nicht gerechnet.</b> Die LETZTE Zeile bekommt den Rest,
        /// damit die Summe der Anteile die projektweite Groesse BITGENAU trifft — eine
        /// Rubrik, deren Bloecke um ein Rundungsbit neben der Ausgangsgroesse liegen,
        /// waere ein Fehler, den niemand mehr findet.</para>
        ///
        /// <para>Ohne Eigenverbrauch (Summe 0) kommt eine LEERE Liste zurueck; die
        /// Rubrik bleibt dann bei der einen projektweiten Kette. Eine Aufteilung ohne
        /// Schluessel waere eine Behauptung.</para>
        /// </summary>
        /// <param name="schluessel">Je Komponente eine Zeile mit
        /// <see cref="Komponente"/>, <see cref="Anlage"/> und <see cref="EigenMWh"/>.</param>
        public static List<VermiedenAnlageNachweis> Verteile(
            IList<VermiedenAnlageNachweis> schluessel,
            double mengeMWh, double arbeitEur, double entlastung9bEur)
        {
            var zeilen = new List<VermiedenAnlageNachweis>();
            if (schluessel == null) return zeilen;

            double summe = 0;
            foreach (VermiedenAnlageNachweis s in schluessel)
                if (s != null && s.EigenMWh > 0) summe += s.EigenMWh;
            if (summe <= 0) return zeilen;

            foreach (VermiedenAnlageNachweis s in schluessel)
            {
                if (s == null || s.EigenMWh <= 0) continue;
                zeilen.Add(new VermiedenAnlageNachweis
                {
                    Komponente = s.Komponente ?? "",
                    Anlage = s.Anlage ?? "",
                    EigenMWh = s.EigenMWh,
                    Anteil = s.EigenMWh / summe
                });
            }
            if (zeilen.Count == 0) return zeilen;

            bool naeherung = zeilen.Count > 1;
            double restMenge = mengeMWh, restArbeit = arbeitEur, restEntlastung = entlastung9bEur;
            for (int i = 0; i < zeilen.Count; i++)
            {
                VermiedenAnlageNachweis n = zeilen[i];
                n.IstNaeherung = naeherung;
                if (i == zeilen.Count - 1)
                {
                    n.MengeMWh = restMenge;
                    n.ArbeitEur = restArbeit;
                    n.Entlastung9bEur = restEntlastung;
                }
                else
                {
                    n.MengeMWh = mengeMWh * n.Anteil;
                    n.ArbeitEur = arbeitEur * n.Anteil;
                    n.Entlastung9bEur = entlastung9bEur * n.Anteil;
                    restMenge -= n.MengeMWh;
                    restArbeit -= n.ArbeitEur;
                    restEntlastung -= n.Entlastung9bEur;
                }
            }
            return zeilen;
        }
    }

    /// <summary>
    /// AUFTRAG U7 (Befund B7‑1) — der Nachweis EINER Energiesteuerposition: welche
    /// Vorschrift, welche Anlage, welche Menge, welcher Satz, welcher Betrag.
    ///
    /// <para><b>Warum es ihn gibt.</b> Bis U7 kam die Energiesteuer als EINE Summe
    /// aus dem Rechner zurück. Die Rubrik konnte deshalb weder § 53/§ 53a von § 54
    /// trennen noch sagen, aus welcher Menge und welchem Satz ein Betrag entstanden
    /// ist — die Herleitung stand allenfalls als Fließtext in der Herkunftszeile.
    /// Hier steht sie als Zahl, einmal, und Rubrik, Wort- und Excelbericht schreiben
    /// sie ab, statt sie nachzurechnen.</para>
    ///
    /// <para><b>Nur Ausweis.</b> Die Summe der Beträge ist der gerechnete Wert; aus
    /// diesen Zeilen wird nichts gerechnet, was der Steuerrechner nicht schon
    /// gerechnet hat. Sie reisen im Nachweisumschlag mit
    /// (<see cref="ErgebnisNachweisUmschlag"/>, Fassung 4) — der gebuchte Stand
    /// trägt seine Herleitung damit ebenso wie der frisch gerechnete.</para>
    /// </summary>
    public class EnergiesteuerNachweis
    {
        /// <summary>§ 53 EnergieStG — voller Satz auf den Brennstoff der Stromerzeugung.</summary>
        public const string PARAGRAF_53 = "53";

        /// <summary>§ 53a Abs. 5 EnergieStG — Teilsatz, hocheffiziente KWK.</summary>
        public const string PARAGRAF_53A = "53A";

        /// <summary>§ 54 EnergieStG — Heizstoffe des produzierenden Gewerbes.</summary>
        public const string PARAGRAF_54 = "54";

        /// <summary>Bezeichner der Anlage — ein Datenwert des Anwenders, kein
        /// Anzeigetext; leer = unbenannte Anlagenzeile.</summary>
        public string Anlage = "";

        /// <summary>Die angewandte Vorschrift, sprachneutral: <see cref="PARAGRAF_53"/>,
        /// <see cref="PARAGRAF_53A"/> oder <see cref="PARAGRAF_54"/>.</summary>
        public string Paragraf = "";

        /// <summary>Menge in der GESETZLICHEN Einheit des Satzes (also bereits
        /// umgerechnet — bei Erdgas brennwertbezogen).</summary>
        public double Menge;

        /// <summary>Gesetzliche Einheit des Satzes (<c>DbWerte.GESETZ_EINHEIT_*</c>,
        /// etwa <c>EUR/MWh</c>) — sie bestimmt zugleich die Einheit von
        /// <see cref="Menge"/>.</summary>
        public string Einheit = "";

        /// <summary>Angesetzter Satz [€ je gesetzlicher Mengeneinheit].</summary>
        public double SatzEur;

        /// <summary>Betrag dieser Position [€/a], <b>vor</b> dem Sockelbetrag des
        /// § 54 — der fällt einmal je Lauf an, nicht je Anlage.</summary>
        public double BetragEur;

        /// <summary>true, wenn die Position zu § 54 gehört (Heizstoff, Sockelbetrag,
        /// produzierendes Gewerbe) — die Trennung, an der die zwei Rubrikzeilen
        /// hängen.</summary>
        public bool Ist54
        {
            get { return string.Equals(Paragraf, PARAGRAF_54, StringComparison.Ordinal); }
        }
    }

    public class KwkgModulNachweis
    {
        /// <summary>Bezeichner der Anlage (Datenwert aus <c>Tab_Energieanlagen</c>).</summary>
        public string Bezeichner = "";

        /// <summary>Elektrische Nennleistung [kW].</summary>
        public double PelKW;

        /// <summary>Angesetzte elektrische Vollbenutzungsstunden dieses Moduls [h/a]
        /// — <b>brutto</b>, siehe <see cref="StromNettoMWh"/>.</summary>
        public double VbhElektrisch;

        // ------------- ETAPPE B3 Paket b — die Mengenherleitung (§ 4.3) -------------
        //
        // Fünf Zahlen, aus denen die Herleitungstafel (BW8, Etappe B6) die Zeile
        // „so kommt der Zuschlag zu seiner Menge" ohne jede Zweitrechnung bauen kann:
        //   Brutto − Hilfsstrom = Netto,  Netto = Eigen + Einspeisung.
        // Alle Werte sind die TATSÄCHLICH angesetzten dieses Laufs.

        /// <summary>Stromerzeugung dieses Moduls [MWh/a] an der Klemme (brutto).</summary>
        public double StromBruttoMWh;

        /// <summary>
        /// Hilfsstrom dieser Anlage [MWh/a] — <c>Hilfsenergie_Anteil × Brennstoff</c>
        /// (<see cref="HilfsstromRechner.MengeMWh"/>). 0 = kein Anteil gepflegt.
        /// </summary>
        public double HilfsstromMWh;

        /// <summary>Zuschlagsfähige Nettostromerzeugung [MWh/a] =
        /// <see cref="StromBruttoMWh"/> − <see cref="HilfsstromMWh"/>, bei 0 geklemmt.</summary>
        public double StromNettoMWh;

        /// <summary>Anteil dieser Anlage am selbst genutzten KWK-Strom [MWh/a] —
        /// die Bezugsmenge des Satzes nach § 7 Abs. 2.</summary>
        public double EigenMWh;

        /// <summary>Anteil dieser Anlage an der KWK-Einspeisung [MWh/a] —
        /// die Bezugsmenge des Satzes nach § 7 Abs. 1.</summary>
        public double EinspeisungMWh;

        /// <summary>Angesetzter Satz auf selbst genutzten KWK-Strom [ct/kWh].</summary>
        public double SatzEigenCt;

        /// <summary>Angesetzter Satz auf eingespeisten KWK-Strom [ct/kWh].</summary>
        public double SatzEinspeisungCt;

        /// <summary>true, wenn die Sätze aus der Anlagenzeile stammen; false = Projektwert.</summary>
        public bool SatzAusAnlage;

        /// <summary>Angesetztes Vbh-Kontingent [h].</summary>
        public double KontingentH;

        /// <summary>Fester Jahresdeckel [h/a]; 0 = degressive Staffel des § 8 Abs. 4.</summary>
        public double JahresdeckelH;

        /// <summary>Erstes Förderjahr dieses Moduls (Inbetriebnahmejahr bzw. Projektwert).</summary>
        public int Foerderbeginn;

        /// <summary>Zuschlag dieses Moduls im ersten Betrachtungsjahr [€/a].</summary>
        public double Jahr1Eur;

        /// <summary>
        /// Erstes Betrachtungsjahr OHNE Zuschlag, weil das Kontingent erschöpft ist;
        /// 0 = das Kontingent reicht über den ganzen Betrachtungszeitraum.
        /// </summary>
        public int ErschoepftAbJahr;

        /// <summary>Herleitung des Eigenstromsatzes nach § 7 KWKG (Tranchen, Norm, Jahr).</summary>
        public string HerleitungEigen = "";

        /// <summary>Herleitung des Einspeisesatzes nach § 7 KWKG.</summary>
        public string HerleitungEinspeisung = "";

        // ------------- AUFTRAG #351 (U23) — der Vorschlag als ZAHL -------------
        //
        // Bis hierher trug der Nachweis den ANGESETZTEN Satz und die HERLEITUNG des
        // Vorschlags, verglich beide aber nicht — der Leser einer Erlösrubrik sah
        // 6,0000 ct/kWh neben einer Tranchenrechnung, die 5,5667 ergibt, und musste
        // selbst nachrechnen, ob das ein eigener Wert war. Die beiden Felder tragen
        // den Vorschlag als Zahl mit; der Vergleich läuft dann ohne Katalog und ohne
        // Datenbank, also überall dort, wo die Zeile angezeigt wird
        // (<see cref="KwkgSatzHerkunft.Vermerk"/>).
        //
        // NULLBAR MIT ABSICHT: Ein gebuchter Stand aus der Zeit vor diesem Auftrag
        // führt die Felder nicht. 0 hieße dort „Vorschlag 0,0000 ct/kWh" und
        // erzeugte an jeder Zeile einen Vermerk „eigener Wert" — null heißt
        // „kein Vorschlag bekannt", und der Vermerk bleibt weg.

        /// <summary>Vorgeschlagener Eigenstromsatz nach § 7 KWKG [ct/kWh];
        /// <c>null</c> = kein Vorschlag bekannt.</summary>
        public double? VorschlagEigenCt;

        /// <summary>Vorgeschlagener Einspeisesatz nach § 7 KWKG [ct/kWh];
        /// <c>null</c> = kein Vorschlag bekannt.</summary>
        public double? VorschlagEinspeisungCt;
    }

    /// <summary>
    /// ETAPPE E7 — eine Betriebskostenposition mit ihrer Herleitung.
    ///
    /// <para>Etappe E3 hat <c>Kostenart</c>, <c>Bemessung</c>, <c>Menge</c> und
    /// <c>Einheitpreis</c> an die Kostenposition geschrieben und im Protokoll
    /// festgehalten, ihr Zweck sei „die Gliederung des Berichts in Etappe E7". Genau
    /// dafür steht diese Zeile: Sie trägt, was die Rechnung angesetzt hat, und woraus
    /// sie es gebildet hat.</para>
    /// </summary>
    public class KostenPositionNachweis
    {
        /// <summary>ANWENDERBEFUND W5‑B‑7 (08.09.2026): <c>Tab_ProjektWerte.ID</c> —
        /// der Schlüssel, unter dem der Dialog Kostenverwaltung und die Anlagentabelle
        /// der Kostenseite ihre Zeile wiederfinden. 0 = unbekannt (Datenbank ohne
        /// Schritt 19, dann ist die Liste ohnehin leer).</summary>
        public int Id;

        /// <summary>W5‑B‑7: <c>Tab_ProjektWerte.KomponentenID</c> — für die Summe je
        /// Komponente.</summary>
        public int Komponente;

        /// <summary>W5‑B‑7: <c>Tab_ProjektWerte.ID_Anlage</c> (Ä 20); 0 = ohne
        /// Anlagenzuordnung — für die Summe je Anlagenzeile.</summary>
        public int Anlage;

        /// <summary>Bezeichnung aus <c>Tab_Kostenfaktor</c>.</summary>
        public string Bezeichnung = "";

        /// <summary>Kostengruppe des Projekts (Freitext <c>Tab_ProjektWerte.Gruppe</c>).</summary>
        public string Gruppe = "";

        /// <summary>Kostenart nach VDI 2067, Steuerwert <c>DbWerte.KOSTENART_*</c>;
        /// leer = nicht eingeordnet.</summary>
        public string Kostenart = "";

        /// <summary>Bemessungsart, Steuerwert <c>DbWerte.BEMESSUNG_*</c>.</summary>
        public string Bemessung = DbWerte.BEMESSUNG_BETRAG;

        /// <summary>Bezugsmenge der Bemessung; <c>null</c> = nicht gepflegt.</summary>
        public double? Menge;

        /// <summary>Satz der Bemessung; <c>null</c> = nicht gepflegt.</summary>
        public double? Einheitpreis;

        /// <summary>Angesetzter Jahresbetrag [€/a] — positiv Ausgabe, negativ Einnahme.</summary>
        public double BetragJahr;

        /// <summary>true = Erlösposition (Betrag negativ).</summary>
        public bool IstErloes;

        /// <summary>true, wenn ein gepflegter Best-/Worst-Case-Betrag die Ableitung
        /// geschlagen hat (VALERI-Muster) — dann steht kein Menge × Preis dahinter.</summary>
        public bool SzenarioGepflegt;
    }

    /// <summary>
    /// Eine Zeile der Sensitivitätsanalyse (W2, Szenario Erwartet): Kapitalwert
    /// der Variante (vs. Stamm) bei −Δ / Basis / +Δ eines Einflussparameters.
    /// </summary>
    public class SensitivitaetZeile
    {
        public int IdProjekt;                  // Variante
        public string Parameter = "";          // Anzeigename inkl. Δ (z. B. "Zinssatz ±1 %-Pkt")
        public double? KwMinus;
        public double? KwBasis;
        public double? KwPlus;

        // ---- ETAPPE E5 (V‑A, V‑G6) — die Steigungsspalte der Sensitivität ----
        //
        // DIN EN 17463 (7.2) weist je Größe die STEIGUNG aus: um wie viel Euro der
        // Kapitalwert je Prozent(punkt) Änderung reagiert. Die Tafel führte bis hierher
        // nur die drei Kapitalwerte. Die Steigung ist eine reine Ableitung der beiden
        // Randwerte — gerechnet wird nichts Neues, und persistiert wird nichts Neues:
        // Die Stufe der Zeile steht in ihrem eigenen Text („±1 %-Pkt", „±10 %").

        /// <summary>
        /// Der Ausschlag ±Δ der Zeile in der Einheit ihres Parameters (%-Punkte bei Zins
        /// und Preissteigerung, % bei Investition und Energiekosten); <c>null</c> = die
        /// Zeile variiert keine stetige Größe (Wegfall des KWKG-Zuschlags) und hat
        /// deshalb keine Steigung.
        /// </summary>
        public double? Schritt;

        /// <summary><c>true</c>: <see cref="Schritt"/> zählt in Prozentpunkten (Zins,
        /// Preissteigerung); <c>false</c>: in Prozent der Größe (Investition,
        /// Energiekosten).</summary>
        public bool SchrittInProzentpunkten;

        /// <summary>
        /// Die Steigung [€ je %-Punkt bzw. € je %]: (KW(+Δ) − KW(−Δ)) / (2 · Δ) — der
        /// mittlere Differenzenquotient über die Zeile. <c>null</c> ohne Schritt oder
        /// ohne einen der beiden Randwerte.
        /// </summary>
        public double? Steigung
        {
            get
            {
                if (!Schritt.HasValue || Schritt.Value <= 0 ||
                    !KwMinus.HasValue || !KwPlus.HasValue) return null;
                return (KwPlus.Value - KwMinus.Value) / (2.0 * Schritt.Value);
            }
        }

        /// <summary>Die Einheit der Steigung — „€/%-Pkt." oder „€/%"; leer ohne
        /// Steigung.</summary>
        public string SteigungEinheit
        {
            get
            {
                if (!Schritt.HasValue) return "";
                return SchrittInProzentpunkten
                     ? MyResource.Resource.WIRT_SENS_EINHEIT_PUNKT
                     : MyResource.Resource.WIRT_SENS_EINHEIT_PROZENT;
            }
        }

        /// <summary>
        /// Liest den Ausschlag aus dem Parametertext einer gespeicherten Zeile
        /// („Zinssatz ±1 %-Pkt", „Investition Variante ±10 %"). Die Zeile trägt ihre
        /// Stufe seit W2 im Text; eine eigene Spalte gibt es nicht, und ein Schemaschritt
        /// allein für eine Ableitung wäre eine zweite Wahrheit.
        /// </summary>
        /// <returns><c>true</c>, wenn der Text eine Stufe „±x %" oder „±x %-Pkt" nennt.</returns>
        public static bool SchrittAusParameter(string parameter, out double schritt,
                                               out bool inProzentpunkten)
        {
            schritt = 0;
            inProzentpunkten = false;
            if (string.IsNullOrEmpty(parameter)) return false;
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                parameter, @"±\s*(\d+(?:[.,]\d+)?)\s*%(-Pkt)?");
            if (!m.Success) return false;
            double wert;
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                                 System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out wert) ||
                wert <= 0) return false;
            schritt = wert;
            inProzentpunkten = m.Groups[2].Success;
            return true;
        }
    }

    /// <summary>
    /// Schnittstelle des Wirtschaftlichkeits-Providers (Berichtskonzept Kap. 6):
    /// der Berichts-Baustein und der UI-Reiter lesen dieselben persistierten
    /// Ergebnisse — Reiter, Word und Excel zeigen garantiert identische Zahlen.
    /// </summary>
    public interface IWirtschaftlichkeitProvider
    {
        /// <summary>Persistierte Ergebnisse der Projekte (alle Szenarien; leer = nie berechnet).</summary>
        List<WirtschaftlichkeitErgebnis> LadeErgebnisse(List<int> projektIds);

        /// <summary>Parametersatz des Stammprojekts (Vorgabewerte, falls nie gespeichert).</summary>
        WirtschaftlichkeitParameter LadeParameter(int idStamm);

        /// <summary>Persistierte Sensitivitätszeilen der Varianten (W2; leer = nie berechnet).</summary>
        List<SensitivitaetZeile> LadeSensitivitaet(List<int> projektIds);

        /// <summary>Persistierte Strommengen-Matrizen (W3; leer = Tarif inaktiv/nie berechnet).</summary>
        Dictionary<int, StromMatrix> LadeStromMatrix(List<int> projektIds);

        /// <summary>Tarifparameter des Stammprojekts (Vorgabewerte, falls nie gespeichert).</summary>
        TarifParameter LadeTarif(int idStamm);
    }
}
