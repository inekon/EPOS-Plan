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

    /// <summary>Parametersatz eines Rechenlaufs (Tab_ProjektWirtschaftlichkeit,
    /// eine Zeile je STAMMprojekt — gilt für die ganze Vergleichsgruppe).</summary>
    public class WirtschaftlichkeitParameter
    {
        public int IdStamm;
        public double Zinssatz = 3.0;                 // Kalkulationszins [%]
        public int Betrachtungszeitraum = 20;         // T [a]
        public double PreissteigerungEnergie = 0.0;   // [%/a]
        public double PreissteigerungBetrieb = 0.0;   // [%/a]
        public double Einspeiseverguetung = 0.0;      // [€/kWh] für PV-Überschuss

        // ---- Stufe W2 (Phase 7) ----
        public double CO2Preis = 0.0;                 // BEHG [€/t] auf Brennstoff-CO₂ (0 = aus)
        public double KwkgBonus = 0.0;                // [ct/kWh] KWK-Eigenstrom (0 = aus)

        /// <summary>Vbh-Deckel-OVERRIDE [h/a]; 0 = degressive Staffel des KWKG 2025
        /// aus dem Katalog Tab_KWKG_Staffel (Phase 9, Konzept Kap. 8.3/8.5.1).</summary>
        public double KwkgVbhJahresdeckel = 0;
        public double KwkgVbhKontingent = 30000;      // kumuliertes Vbh-Kontingent

        // ---- Stufe W3 (Phase 8) ----
        public double KwkgBonusEinspeisung = 0.0;     // [ct/kWh] KWK-Einspeisung (0 = wie Eigenstrom aus)
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

        // ---- ETAPPE E5 — zwei Projektangaben (Migrationsschritt 21) ----

        /// <summary>
        /// Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzessionsabgabe, Vertrieb)
        /// in der Jahreskostenrechnung berücksichtigen. <b>Vorgabe: aus.</b>
        ///
        /// <para>Die Aufschläge sind seit dem Stromspeicherpaket je Energieträger
        /// gepflegt (<c>energy_project_settings.Aufschlag_*</c>, Vorschlagswerte in
        /// Summe 11,746 ct/kWh), wirkten aber ausschließlich in der Speichersimulation.
        /// Gemessen an den neun Referenzprojekten (Protokoll W4_E5, Abschnitt 4) hebt
        /// ihre Berücksichtigung die Energiekosten um rund <b>32 %</b> und
        /// verschlechtert den Kapitalwert um rund <b>30 %</b> — eine stille Übernahme
        /// hätte jede gespeicherte Altrechnung entwertet. Deshalb eine ausdrückliche
        /// Angabe je Projekt.</para>
        ///
        /// <para><b>Zusammenspiel mit der Stromsteuer aus E4:</b> Der Aufschlagsblock
        /// enthält die Stromsteuer (2,05 ct/kWh ≙ 20,50 €/MWh) als BELASTUNG, die
        /// Entlastung nach § 9b StromStG (20,00 €/MWh) als GUTSCHRIFT. Beide zusammen
        /// sind kein Doppelansatz, sondern die zwei Seiten derselben Vorschrift.
        /// Steht dieser Schalter dagegen auf AUS und ist § 9b aktiv, enthält der
        /// Kapitalwert eine Entlastung ohne die zugehörige Belastung — das Ergebnis
        /// weist genau darauf hin.</para>
        /// </summary>
        public bool AufschlaegeAnwenden;

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

        public DateTime? GeaendertAm;

        /// <summary>Kurzdarstellung als Nachweiszeile (Reiter + Bericht).</summary>
        public string Nachweis(System.Globalization.CultureInfo kultur)
        {
            string t = "i = " + Zinssatz.ToString("N1", kultur) + " % · T = " + Betrachtungszeitraum +
                   " a · Preissteigerung Energie " + PreissteigerungEnergie.ToString("N1", kultur) +
                   " %/a, Betrieb " + PreissteigerungBetrieb.ToString("N1", kultur) +
                   " %/a · Einspeisevergütung " + Einspeiseverguetung.ToString("N3", kultur) + " €/kWh";
            if (CO2Preis > 0)
                t += " · CO₂ (BEHG) " + CO2Preis.ToString("N0", kultur) + " €/t";
            if (KwkgBonus > 0 || KwkgBonusEinspeisung > 0)
            {
                t += " · KWKG " + KwkgBonus.ToString("N2", kultur) + "/" +
                     KwkgBonusEinspeisung.ToString("N2", kultur) + " ct/kWh (";
                t += KwkgVbhJahresdeckel > 0
                    ? "Deckel fest " + KwkgVbhJahresdeckel.ToString("N0", kultur) + " Vbh/a"
                    : "Vbh-Staffel KWKG 2025";
                t += ", Kontingent " + KwkgVbhKontingent.ToString("N0", kultur) + " Vbh";
                if (KwkgAbschlagNegativ > 0)
                    t += ", Negativpreis-Abschlag " + KwkgAbschlagNegativ.ToString("N1", kultur) + " %";
                t += KwkgStichtag.HasValue
                    ? ", Stichtag " + KwkgStichtag.Value.ToString("dd.MM.yyyy", kultur)
                    : ", Stichtag ungeprüft";
                if (KwkgInbetriebnahme.HasValue)
                    t += ", IBN " + KwkgInbetriebnahme.Value.ToString("dd.MM.yyyy", kultur);
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
            // ETAPPE E5: Der Aufschlagsschalter gehört in die Nachweiszeile, sobald er
            // an ist — er verändert den größten Kostenposten um rund ein Drittel.
            if (AufschlaegeAnwenden)
                t += " · Aufschläge auf den Strombezug berücksichtigt";
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
    }

    /// <summary>Ergebnis der Verlaufsrechnung über einen frei wählbaren Horizont
    /// (auch &gt; T; dann wird mit verlängertem Betrachtungszeitraum neu gerechnet).</summary>
    public class WirtschaftlichkeitVerlauf
    {
        public int Jahre;
        public string Szenario = "";
        /// <summary>Absolute kumulierte Barwerte je Projekt (inkl. Stamm).</summary>
        public List<VerlaufSerie> Absolut = new List<VerlaufSerie>();
        /// <summary>Differenz Variante − Stamm (Nulldurchgang = dynamische Amortisation).</summary>
        public List<VerlaufSerie> Differenz = new List<VerlaufSerie>();
    }

    /// <summary>
    /// Vereinfachtes Tarifmodell (Stufe W3, Entscheidung 11.08.2026): Winterzeitraum
    /// als Monatsspanne, EIN HT-Fenster Mo–Fr, je vier Zonenpreise für Bezug und
    /// Einspeisung, zweistufige Leistungspreis-Staffel. Eine Zeile je STAMM in
    /// Tab_ProjektTarif; Aktiv = false → Flat-Preise der Kostenmaske gelten weiter.
    /// </summary>
    public class TarifParameter
    {
        public int IdStamm;
        public bool Aktiv;

        public int WinterVonMonat = 10;    // Oktober …
        public int WinterBisMonat = 3;     // … März (über den Jahreswechsel)
        public int HtVonStunde = 6;        // HT Mo–Fr [von, bis)
        public int HtBisStunde = 22;

        // Bezugspreise [€/kWh]
        public double PreisBezugWinterHT;
        public double PreisBezugWinterNT;
        public double PreisBezugSommerHT;
        public double PreisBezugSommerNT;

        // Einspeisepreise [€/kWh] (PV- und KWK-Einspeisung)
        public double PreisEinspWinterHT;
        public double PreisEinspWinterNT;
        public double PreisEinspSommerHT;
        public double PreisEinspSommerNT;

        // Leistungspreis-Staffel: bis Grenze Preis 1, darüber Preis 2 [€/kW·a]
        public double StaffelGrenzeKW;
        public double StaffelPreis1EurKW;
        public double StaffelPreis2EurKW;

        // ---- ETAPPE E5 — Rollenmodell (Migrationsschritt 21) ----
        //
        // Additiv: Alles oberhalb bleibt unverändert und wird weiter gelesen. Modus
        // ZONEN (Vorbelegung) = Bestandsverhalten der Stufe W3; ROLLEN schaltet auf
        // Bezugs-, Reststrom- und Einspeisetarif mit der Differenzmethode um.

        /// <summary>Tarifmodus, Steuerwert aus <c>DbWerte.TARIF_MODUS_*</c>.</summary>
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

        /// <summary>Eine Rolle mit vier leeren Staffelstufen (Vorbelegung MONATLICH).</summary>
        private static TarifRolle NeueRolle(string rolle)
        {
            var r = new TarifRolle { Rolle = rolle };
            for (int i = 0; i < 4; i++) r.Stufen.Add(new LeistungsStufe());
            return r;
        }

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
            return "Tarif aktiv: Winter " + WinterVonMonat + "–" + WinterBisMonat +
                   " · HT Mo–Fr " + HtVonStunde + "–" + HtBisStunde + " Uhr · Bezug W/S HT/NT " +
                   PreisBezugWinterHT.ToString("N3", kultur) + "/" + PreisBezugWinterNT.ToString("N3", kultur) + "/" +
                   PreisBezugSommerHT.ToString("N3", kultur) + "/" + PreisBezugSommerNT.ToString("N3", kultur) +
                   " €/kWh · Leistungspreis " + StaffelPreis1EurKW.ToString("N0", kultur) + "/" +
                   StaffelPreis2EurKW.ToString("N0", kultur) + " €/kW (Grenze " +
                   StaffelGrenzeKW.ToString("N0", kultur) + " kW)";
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
        public double Investition;             // I₀ [€] (Kategorie 1, Szenariowert)
        public double? BetriebskostenJahr;     // [€/a] (Kategorie 2, Szenariowert)
        public double? EnergiekostenJahr;      // [€/a] (KostenEmissionRechner; null = Preise fehlen)
        public double EinspeiseerloesJahr;     // [€/a] (PV-Überschuss × Einspeisevergütung)

        // Barwerte über T
        public double? BarwertAusgaben;        // Betrieb + Energie + Ersatzbeschaffungen [€]
        public double? BarwertEinnahmen;       // Einspeiseerlöse [€]
        public double RestwertBarwert;         // linearer Restwert, abgezinst [€]

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

        // ---- ETAPPE E4 — Steuergutschriften, Jahr 1 der jahresscharfen Reihen ----
        //
        // 0 = keine Gutschrift. Der GRUND steht immer in Hinweis (nie eine stille Null):
        // nicht gewählt, Bedingung nicht erfüllt, Satz nicht gepflegt oder Menge nicht
        // in die gesetzliche Einheit umrechenbar.

        /// <summary>Energiesteuer-Entlastung nach § 53 bzw. § 53a Abs. 5 EnergieStG
        /// im Jahr 1 [€/a] — nur auf den BHKW-Brennstoff, nie auf Kessel.</summary>
        public double EnergiesteuerJahr1;

        /// <summary>Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG im Jahr 1
        /// [€/a] — Regelsatz auf den KWK-Eigenverbrauch.</summary>
        public double StromsteuerBefreiungJahr1;

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
        /// Betrag, um den die Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession,
        /// Vertrieb) die Energiekosten des Jahres 1 erhöhen [€/a]. 0 = Schalter aus oder
        /// nichts gepflegt. Der Wert steht getrennt, damit die Wirkung sichtbar bleibt,
        /// statt in den Energiekosten zu verschwinden.
        /// </summary>
        public double AufschlagJahr;

        public double? IRR;                    // interner Zinsfuß der Differenzreihe [%] (null beim Stamm/nie)

        // Stufe W3 (Phase 8)
        public double? StromkostenTarif;       // Bezugskosten nach Tarifmatrix [€/a] (null = Flat-Rechnung)
        public string Hinweis;                 // nicht-fataler Hinweis (z. B. Tarif ohne Stundenreihen)

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
