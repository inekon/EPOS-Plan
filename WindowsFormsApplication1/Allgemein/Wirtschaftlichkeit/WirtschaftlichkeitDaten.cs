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

        public string Nachweis(System.Globalization.CultureInfo kultur)
        {
            if (!Aktiv) return "Tarifstruktur inaktiv (Flat-Preise der Kostenmaske)";
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
