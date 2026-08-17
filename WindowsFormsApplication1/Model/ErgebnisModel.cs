using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Ergebnis-Datenmodell (Kopf + Detailmodelle je Simulationsart).
    // Pro Simulationsart eine eigene Detailklasse/-tabelle, da die Ergebnisgroessen
    // stark variieren. Fuer Auswertungen/Berichte ist damit "alles zu Waermepumpe"
    // usw. gezielt abfragbar.
    // ---------------------------------------------------------------------------

    // Kopf eines Simulationslaufs (eine Zeile in Tab_Ergebnis).
    public class ErgebnisModel
    {
        public int ID;
        public int ID_Projekt;
        public string Bezeichner = "";
        public DateTime Zeitstempel;
        public int ID_Klimaregion;

        // Welche Simulationsarten dieser Lauf enthaelt.
        public bool Sim_Energiebedarf;
        public bool Sim_Waermepumpe;
        public bool Sim_Heizkessel;
        public bool Sim_Solarthermie;
        public bool Sim_BHKW;
        public bool Sim_PV;
        public bool Sim_Stromspeicher;

        // Detailergebnisse je Art (null = fuer diesen Lauf nicht vorhanden).
        public ErgebnisEnergiebedarfModel Energiebedarf;
        public ErgebnisWaermepumpeModel Waermepumpe;
        public ErgebnisBHKWModel BHKW;
        public ErgebnisHeizkesselModel Heizkessel;
        public ErgebnisSolarthermieModel Solarthermie;
        public ErgebnisPhotovoltaikModel Photovoltaik;

        // Pufferspeicher des Laufs (Tab_ErgebnisPufferspeicher, Konzept 6.6):
        // eine Zeile je beteiligtem Speicher - Senkenspeicher UND Quellspeicher.
        // Leere Liste = dieser Lauf hatte keinen Speicher.
        public List<ErgebnisPufferspeicherModel> Pufferspeicher = new List<ErgebnisPufferspeicherModel>();

        // Stromspeicher des Laufs (Tab_ErgebnisStromspeicher, Fachkonzept
        // Stromspeicher 7.1): eine Zeile je gerechneter Speicheranlage.
        // Leere Liste = dieser Lauf hatte keinen Stromspeicher; das Flag
        // Sim_Stromspeicher sagt, ob die Speicherrechnung ueberhaupt lief.
        public List<ErgebnisStromspeicherModel> Stromspeicher = new List<ErgebnisStromspeicherModel>();

        public ErgebnisModel()
        {
            Zeitstempel = DateTime.Now;
        }
    }

    // Detail: Waerme-/Strombedarf (Tab_ErgebnisEnergiebedarf).
    public class ErgebnisEnergiebedarfModel
    {
        public double Waermebedarf_Gesamt;   // MWh
        public double Waermelast_Max;         // kW
        public double Strombedarf_Gesamt;     // MWh
        public double Strombedarf_Max;        // kW
        public double Waermerestbedarf;       // MWh (Restwärmebedarf nach allen Erzeugern, sim.Restwaerme)
        public double Stromrestbedarf;        // MWh (Reststrombedarf/Netzbezug, sim.Reststrom)
    }

    // Detail: Waermepumpe-Aggregat (Tab_ErgebnisWaermepumpe) + Modulliste.
    public class ErgebnisWaermepumpeModel
    {
        public double Waermebedarf;               // MWh/a
        public double Restwaermebedarf;           // MWh/a
        public double Waermeproduktion_WP;        // MWh/a
        public double Stromverbrauch_WP;          // MWh/a
        public double Stromverbrauch_Heizstab;    // MWh/a
        public double Kapazitaet_Pufferspeicher;  // kWh
        public double Min_Spitzenkesselleistung;  // kW
        public double Waermebedarfsdeckung;       // %
        public double Vollbenutzungsstunden;      // h/a
        public double? Bivalenzpunkt;             // Grad C (null = kein Bivalenzpunkt)

        public List<ErgebnisWaermepumpeModulModel> Module = new List<ErgebnisWaermepumpeModulModel>();
    }

    // Eine WP-Modulzeile (Tab_ErgebnisWaermepumpeModul).
    public class ErgebnisWaermepumpeModulModel
    {
        public string Modul = "";
        public double Leistung;           // kW
        public double Waermeproduktion;   // MWh/a
        public double Stromverbrauch;     // MWh/a
        public double Heizstab;           // MWh/a
        public double Betriebsstunden;    // h/a
    }

    // Detail: BHKW-Aggregat (Tab_ErgebnisBHKW) + Modulliste.
    public class ErgebnisBHKWModel
    {
        public double Waermebedarf;                 // MWh/a
        public double Restwaermebedarf;             // MWh/a
        public double Strombedarf;                  // MWh/a
        public double Reststrombedarf;              // MWh/a
        public double Waermeproduktion;             // MWh/a
        public double Waermeueberschuss;            // MWh/a
        public double Stromproduktion;              // MWh/a
        public double Betriebsstunden_Gesamt;       // h/a
        public double Betriebsstunden_Durchschnitt; // h/a
        public double Waermebedarfsdeckung;         // %
        public double Strombedarfsdeckung;          // %
        public double Gasverbrauch;
        public double Oelverbrauch;
        public double Koks;
        public double Rapsoelverbrauch;
        public double Holzverbrauch;
        public double Kohle;
        public double Sonstigverbrauch;
        public double Pellets;
        public double TierischeFette;
        public List<ErgebnisBHKWModulModel> Module = new List<ErgebnisBHKWModulModel>();
    }

    // Eine BHKW-Modulzeile (Tab_ErgebnisBHKWModul).
    public class ErgebnisBHKWModulModel
    {
        public string Modul = "";
        public double Waermeproduktion;   // MWh/a
        public double Stromproduktion;    // MWh/a
        public string Brennstoff = "";
        public double Verbrauch = 0.0;          // MWh/a
        public int CarrierId;             // energy_carrier.id (0 = keine Zuordnung)
    }

    // Detail: Heizkessel/Spitzenkessel-Aggregat (Tab_ErgebnisHeizkessel) + Modulliste.
    public class ErgebnisHeizkesselModel
    {
        public double Waermebedarf;             // MWh/a
        public double Restwaermebedarf;         // MWh/a
        public double Waermeproduktion;         // MWh/a (Waerme Spitzenkessel)
        public double Strombedarf;              // MWh/a
        public double Reststrombedarf;          // MWh/a
        public double Waermebedarfsdeckung;     // %
        public double Stromverbrauch;           // MWh/a (Hilfsstrom Kessel)
        public double Maximale_Kesselleistung;  // kW
        public double Gasspitze;                // kW

        // ETAPPE D4: Wärme, die die Kessel in der Kaskade aus ihrem QUELLPUFFER bezogen
        // haben (SimulationSPK.Quellwaerme_gesamt, hier in MWh/a wie die übrigen
        // Wärmegrößen dieser Zeile). Ohne Quellbezug exakt 0.
        public double Quellwaerme;              // MWh/a
        // Brennstoffverbrauch je Traeger (MWh/a)
        public double Gasverbrauch;
        public double Oelverbrauch;
        public double Koks;
        public double Rapsoelverbrauch;
        public double Holzverbrauch;
        public double Kohle;
        public double Sonstigverbrauch;
        public double Pellets;
        public double TierischeFette;

        public List<ErgebnisHeizkesselModulModel> Module = new List<ErgebnisHeizkesselModulModel>();
    }

    // Eine Heizkessel-Modulzeile (Tab_ErgebnisHeizkesselModul).
    public class ErgebnisHeizkesselModulModel
    {
        public string Modul = "";
        public double Waerme_Gas;          // MWh/a (Gas/Biogas/Rapsoel/Holz...)
        public double Waerme_Oel;          // MWh/a
        public double Waermeproduktion;   // MWh/a
        public string Brennstoff = "";
        public double Verbrauch = 0.0;          // MWh/a
        public int CarrierId;              // energy_carrier.id (0 = keine Zuordnung)

        public double Jahresnutzungsgrad;  // %
    }

    // Detail: Solarthermie-Aggregat (Tab_ErgebnisSolarthermie) + Kollektor-Liste.
    public class ErgebnisSolarthermieModel
    {
        public double Waermebedarf;          // MWh/a
        public double Restwaermebedarf;      // MWh/a
        public double Waermeproduktion;      // MWh/a (Gesamte Waermeleistung der Module)
        public double Waermebedarfsdeckung;  // %
        public double Ueberschuss;           // MWh/a

        public List<ErgebnisSolarthermieModulModel> Module = new List<ErgebnisSolarthermieModulModel>();
    }

    // Eine Solarkollektor-Zeile (Tab_ErgebnisSolarthermieModul).
    public class ErgebnisSolarthermieModulModel
    {
        public string Modul = "";
        public double Flaeche;           // m^2 (Aperturflaeche gesamt)
        public long Anzahl;
        public double Waermeproduktion;  // MWh/a
        public double Ueberschuss;       // MWh/a
    }

    // Detail: Photovoltaik-Aggregat (Tab_ErgebnisPhotovoltaik) + Modul-Liste.
    public class ErgebnisPhotovoltaikModel
    {
        public double Strombedarf;           // MWh/a
        public double Reststrombedarf;       // MWh/a
        public double Stromproduktion;       // MWh/a (Gesamte Stromerzeugung der Module)
        public double Strombedarfsdeckung;   // %
        public double Ueberschuss;           // MWh/a
        public double MaxSolareLeistung;     // W/m^2

        public List<ErgebnisPhotovoltaikModulModel> Module = new List<ErgebnisPhotovoltaikModulModel>();
    }

    // Eine PV-Modul-Zeile (Tab_ErgebnisPhotovoltaikModul).
    public class ErgebnisPhotovoltaikModulModel
    {
        public string Modul = "";
        public double Flaeche;          // m^2 gesamt
        public long Anzahl;             // Modulanzahl
        public double Stromproduktion;  // MWh/a
    }

    // ---------------------------------------------------------------------------
    // Detail: Stromspeicher (Tab_ErgebnisStromspeicher) - der Kennzahlenblock aus
    // Fachkonzept Stromspeicher 7.1, eine Zeile je gerechneter Speicheranlage.
    //
    // AUSSCHLIESSLICH SKALARE. Ergebniszeitreihen (SoC-Gang, Geldwert je Intervall,
    // Netzbezug vor/nach) werden bewusst NICHT persistiert (AP0-Entscheid vom
    // 16.08.2026, Frage 2): Ein Jahreslauf liegt im Millisekundenbereich, Neurechnen
    // ist billiger als Speichern, und fuer Ergebniszeitreihen gibt es im Bestand kein
    // Muster - alle Tab_Ergebnis*-Tabellen fuehren Skalare. Wer die Reihen dauerhaft
    // braucht, exportiert sie als CSV (7.2).
    //
    // Anders als die Geschwister dieser Datei traegt der Satz KEINE Modulliste: die
    // Aufteilung auf mehrere Speicher IST die Liste (eine Zeile je Anlage), und die
    // Variantengliederung haengt an ID_Energieanlage (Fachkonzept 7.3).
    // ---------------------------------------------------------------------------
    public class ErgebnisStromspeicherModel
    {
        // --- Kopf ---

        /// <summary>Anlagenzeile (Tab_Energieanlagen.ID) der gerechneten Variante, 0 = unbekannt.</summary>
        public int ID_Energieanlage;

        /// <summary>Bezeichner der Anlage bzw. Variante zum Zeitpunkt der Rechnung.</summary>
        public string Bezeichner = "";

        /// <summary>Betriebsart des Laufs (DbWerte.SP_BETRIEBSART_*) - festgehalten, weil die Variante danach umgestellt werden kann.</summary>
        public string Betriebsart = "";

        /// <summary>Berechnungsart des Laufs (DbWerte.SP_BERECHNUNG_*).</summary>
        public string Berechnungsart = "";

        // --- Energie (7.1, Block 1) ---

        public double Ladung_PV;             // kWh/a aus PV-Ueberschuss
        public double Ladung_BHKW;           // kWh/a aus BHKW-Ueberschuss
        public double Ladung_Netz;           // kWh/a aus dem Netz (Graustrom, AP10)
        public double Ladung_Gesamt;         // kWh/a
        public double Entladung_Gesamt;      // kWh/a
        public double Verluste_Gesamt;       // kWh/a (Lade- und Entladeverluste)
        public double Netzbezug_Mit;         // kWh/a mit Speicher
        public double Netzbezug_Ohne;        // kWh/a ohne Speicher
        public double Einspeisung_Mit;       // kWh/a mit Speicher
        public double Einspeisung_Ohne;      // kWh/a ohne Speicher
        public double Eigenverbrauchsquote;  // % (mit Speicher)
        public double Autarkiegrad;          // % (mit Speicher)

        // --- Speicher (7.1, Block 2) ---

        public double Vollzyklen;             // - aequivalente Vollzyklen p. a. (n_zyk)
        public double SoC_Min;                // kWh Jahresminimum
        public double SoC_Mittel;             // kWh Jahresmittel
        public double SoC_Max;                // kWh Jahresmaximum
        public double Zeitanteil_Untergrenze; // % der Intervalle an SoC_min
        public double Zeitanteil_Obergrenze;  // % der Intervalle an SoC_max
        public double Zyklen_Hochrechnung;    // - Zyklen ueber die Nutzungsdauer (gegen N_zyk)

        // --- Wirtschaft (7.1, Block 3) ---

        public double Ertrag_Bezugsersparnis;       // EUR/a vermiedener Netzbezug
        public double Ertrag_Verguetung_Entgangen;  // EUR/a entgangene Einspeiseverguetung (Abzug)
        public double Ertrag_Netzerloes;            // EUR/a Verkauf ins Netz (AP10)
        public double Kosten_Ladung;                // EUR/a Ladekosten (Netzladung, AP10)
        public double Ertrag_Leistungspreis;        // EUR/a Leistungspreisersparnis (Peak-Shaving)
        public double Verschleisskosten;            // EUR/a K_ver - eigene Betriebskostenzeile (5.4)
        public double Investition;                  // EUR   I = c_cap*C_nom + c_pow*P + I_fix
        public double Annuitaet;                    // EUR/a
        public double Jahresueberschuss;            // EUR/a Delta J
        public double Ertrag_Jahr1;                 // EUR/a E_a,1 (unskaliertes Referenzjahr)
        public double Ertrag_Aequivalent;           // EUR/a E_a,aeq (degradationsaequivalent)
        public double Amortisation_Statisch;        // a     T_stat
        public double Amortisation_Dynamisch;       // a     T_dyn
        public double Kapitalwert;                  // EUR   NPV

        /// <summary>
        /// Verwendete Preisversion (Fachkonzept 4.1, Stichtagsregel) - damit ein
        /// Ergebnis reproduzierbar bleibt, auch wenn der Preis danach neu versioniert
        /// wird. Leer, solange das Preismodul (AP4) fehlt.
        /// </summary>
        public string Preisversion = "";
    }
}
