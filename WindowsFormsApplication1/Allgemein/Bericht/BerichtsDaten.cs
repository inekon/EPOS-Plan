using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DTOs des Berichtsmoduls (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 8.1).
    // Der BerichtsDatenSammler befüllt diese Klassen ausschließlich lesend über
    // Repository/Controller — die Generatoren (Word/Excel, Phase 2/4) und der
    // Berichtsdialog arbeiten nur noch auf diesem Baum, nie auf offenen Formularen.
    // ---------------------------------------------------------------------------

    /// <summary>Gesamter Datenbestand eines Berichtslaufs (Stamm + Varianten).</summary>
    public class BerichtsDaten
    {
        public int IdStamm;
        public string Stammprojektname = "";
        public DateTime ErstelltAm = DateTime.Now;

        /// <summary>Stamm zuerst, danach die gewählten Varianten.</summary>
        public List<VariantenDaten> Varianten = new List<VariantenDaten>();

        /// <summary>Hinweise, die im Bericht bzw. der Abschlussmeldung erscheinen.</summary>
        public List<string> Warnungen = new List<string>();
    }

    /// <summary>Alle Daten eines einzelnen Projekts (Stamm oder Variante).</summary>
    public class VariantenDaten
    {
        public int IdProjekt;
        public string Projektname = "";
        public string Variantenname = "";     // leer beim Stamm
        public bool IstStamm;

        /// <summary>Projektstammdaten (Tab_Projekt).</summary>
        public ProjektModel Projekt;

        /// <summary>Kompletter Ergebnisbaum des letzten Simulationslaufs (null = keiner).</summary>
        public ErgebnisModel Ergebnis;

        /// <summary>Zeitstempel des Simulationslaufs (null = kein Ergebnis).</summary>
        public DateTime? SimulationsStand;

        /// <summary>true, wenn beim Sammeln frisch simuliert wurde.</summary>
        public bool FrischSimuliert;

        /// <summary>Ergebnis fehlte vor dem Sammeln bzw. war älter als die letzte Projektänderung.</summary>
        public bool ErgebnisFehlte;
        public bool ErgebnisVeraltet;

        /// <summary>Brennstoffmengen je Erzeuger (EnergieMengen.BaueBrennstoffmengen; null = nicht ermittelbar).</summary>
        public DataTable Brennstoffmengen;

        /// <summary>Detail-Daten (Klimaregion, Gebäude, Anlage, Komponenten) für
        /// Projektbeschreibung, Kenndaten-Tabellen und Abweichungserkennung (Phase 2).</summary>
        public ProjektDetails Details;

        /// <summary>Kennzahlwerte je Katalogschlüssel (null = für dieses Projekt nicht verfügbar).</summary>
        public Dictionary<string, double?> Kennzahlen = new Dictionary<string, double?>();

        // Verrechnete Kosten-/Emissionswerte (KostenEmissionRechner, Phase 5) —
        // null = mangels Preisen/Faktoren nicht bestimmbar (Anzeige „—").
        public double? Energiekosten;      // €/a (Brennstoffe + Netzstrom inkl. Grundpreise)
        public double? StromkostenNetz;    // €/a (Netzbezug)
        public double? CO2Gesamt;          // t/a
        public double? CO2Spezifisch;      // g/kWh Wärme

        /// <summary>Zeitreihen aus der In-Memory-Simulation (Phase 3; bis dahin null).</summary>
        public ZeitreihenSatz Zeitreihen;

        /// <summary>Abweichungen dieser Variante gegenüber dem Stamm (Phase 2; beim Stamm leer).</summary>
        public List<Abweichung> Abweichungen = new List<Abweichung>();

        /// <summary>Fehlertext, falls dieses Projekt beim Sammeln scheiterte (Bericht läuft weiter).</summary>
        public string Fehler;

        /// <summary>Anzeigename: Variantenname, sonst Projektname.</summary>
        public string Anzeige
        {
            get { return IstStamm ? "Stamm" : (string.IsNullOrEmpty(Variantenname) ? Projektname : Variantenname); }
        }
    }

    /// <summary>Eine Zeile der Abweichungstabelle „Merkmal · Stamm · Variante" (Kap. 4, Baustein 4).</summary>
    public class Abweichung
    {
        public string Gewerk = "";      // z. B. "Wärmepumpe", "Gebäude", "Anlage"
        public string Merkmal = "";     // z. B. "Vorlauftemperatur"
        public string WertStamm = "";
        public string WertVariante = "";
    }

    /// <summary>
    /// Stundenreihen der In-Memory-Simulation für die Ganglinien (Kap. 6.2).
    /// Befüllt vom ZeitreihenExtraktor nach einem frischen Simulationslauf.
    /// Einheiten: Energie in kWh je Stunde, SOC in kWh, Temperatur in °C.
    /// </summary>
    public class ZeitreihenSatz
    {
        public const int Stunden = 8760;

        // Standard-Schlüssel (Reihen können je Projekt fehlen — immer prüfen).
        public const string WAERMEBEDARF = "Waermebedarf";
        public const string TEMPERATUR = "Temperatur";
        public const string STROMBEDARF = "Strombedarf";
        public const string WP_WAERME = "WP_Waerme";
        public const string WP_STROM = "WP_Strom";
        public const string HEIZSTAB = "Heizstab";
        public const string BHKW_WAERME = "BHKW_Waerme";
        public const string BHKW_STROM = "BHKW_Strom";
        public const string KESSEL_WAERME = "Kessel_Waerme";
        public const string SOLAR_WAERME = "Solar_Waerme";
        public const string PV_GENUTZT = "PV_Genutzt";
        public const string PV_UEBERSCHUSS = "PV_Ueberschuss";
        public const string NETZBEZUG = "Netzbezug";
        public const string WAERMEREST = "Waermerest";
        public const string PUFFER_SOC = "Puffer_SOC";
        public const string PV_SPEICHER_SOC = "PVSpeicher_SOC";

        public Dictionary<string, double[]> Reihen = new Dictionary<string, double[]>();

        public double[] Hole(string schluessel)
        { return Reihen.ContainsKey(schluessel) ? Reihen[schluessel] : null; }

        public bool Hat(string schluessel)
        {
            double[] r = Hole(schluessel);
            if (r == null) return false;
            for (int i = 0; i < r.Length; i++) if (r[i] != 0) return true;
            return false;
        }
    }
}
