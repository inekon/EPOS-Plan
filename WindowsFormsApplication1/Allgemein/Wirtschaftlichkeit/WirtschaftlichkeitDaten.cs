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
        public DateTime? GeaendertAm;

        /// <summary>Kurzdarstellung als Nachweiszeile (Reiter + Bericht).</summary>
        public string Nachweis(System.Globalization.CultureInfo kultur)
        {
            return "i = " + Zinssatz.ToString("N1", kultur) + " % · T = " + Betrachtungszeitraum +
                   " a · Preissteigerung Energie " + PreissteigerungEnergie.ToString("N1", kultur) +
                   " %/a, Betrieb " + PreissteigerungBetrieb.ToString("N1", kultur) +
                   " %/a · Einspeisevergütung " + Einspeiseverguetung.ToString("N3", kultur) + " €/kWh";
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
    }
}
