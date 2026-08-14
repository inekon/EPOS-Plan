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
}
