using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // =========================================================================
    // ERSATZ fuer die beiden Datentraeger, die ChartRenderer.cs ausserhalb seiner
    // eigenen Datei anfasst (Umsetzungskonzept iOS, Paket iU7-3).
    //
    // WARUM ERSATZ UND NICHT VERLINKT. Beide Typen stehen in Dateien, die weit
    // mehr enthalten als sie selbst:
    //
    //   ZeitreihenSatz  steht in Allgemein\Bericht\BerichtsDaten.cs - zusammen mit
    //                   BerichtsDaten und VariantenDaten, die ProjektModel,
    //                   ErgebnisModel, ProjektDetails, WirtschaftlichkeitErgebnis,
    //                   DbWerte und System.Data.DataTable nach sich ziehen.
    //   VerlaufSerie    steht in Allgemein\Wirtschaftlichkeit\WirtschaftlichkeitDaten.cs
    //                   und traegt ein Feld vom Typ KapitalwertRechner.Zahlungsbild.
    //
    // Diese Dateien zu verlinken haette praktisch den ganzen Rechenkern in eine
    // Probe gezogen, die eine EINZIGE Frage beantworten soll: Zeichnet der
    // Renderer ohne Windows? EPOS.Kern verlinkt beide Dateien (Stand 03.09.2026)
    // nicht, ein ProjectReference darauf half also auch nicht.
    //
    // Die Klassen sind WORTGLEICHE AUSZUEGE aus dem Bestand - dieselben Namen,
    // dieselben Felder, dieselben Methodenruempfe; weggelassen ist nur, was der
    // Renderer nicht liest. Weicht der Bestand hier ab, bricht der Build dieser
    // Probe, und genau das soll er dann auch.
    // =========================================================================

    /// <summary>
    /// Auszug aus <c>Allgemein\Bericht\BerichtsDaten.cs</c> - nur die Teile, die
    /// <c>ChartRenderer</c> liest (Schluessel, Reihen, Speicherreihen,
    /// Beschriftungen, Hole/Hat/Beschriftung).
    /// </summary>
    public class ZeitreihenSatz
    {
        public const int Stunden = 8760;

        public const string WAERMEBEDARF = "Waermebedarf";
        public const string TEMPERATUR = "Temperatur";
        public const string STROMBEDARF = "Strombedarf";
        public const string WP_WAERME = "WP_Waerme";
        public const string WP_STROM = "WP_Strom";
        public const string HEIZSTAB = "Heizstab";
        public const string BHKW_WAERME = "BHKW_Waerme";
        public const string BHKW_STROM = "BHKW_Strom";
        public const string BHKW_UEBERSCHUSS = "BHKW_Ueberschuss";
        public const string KESSEL_WAERME = "Kessel_Waerme";
        public const string SOLAR_WAERME = "Solar_Waerme";
        public const string PV_GENUTZT = "PV_Genutzt";
        public const string PV_UEBERSCHUSS = "PV_Ueberschuss";
        public const string NETZBEZUG = "Netzbezug";
        public const string WAERMEREST = "Waermerest";
        public const string PV_SPEICHER_SOC = "PVSpeicher_SOC";

        public const string PUFFER_PRAEFIX = "PUFFER_";
        public const string QUELLE_PRAEFIX = "QUELLE_";

        public const string SUFFIX_T_OBEN = "_TOBEN";
        public const string SUFFIX_T_UNTEN = "_TUNTEN";
        public const string QUELLTEMP_PRAEFIX = "QUELLTEMP_";

        public Dictionary<string, double[]> Reihen = new Dictionary<string, double[]>();

        /// <summary>Schluessel der Waermespeicher-Fuellstandsreihen in stabiler Reihenfolge.</summary>
        public List<string> Speicherreihen = new List<string>();

        /// <summary>Anzeigetext je Schluessel; fehlt einer, ist der Schluessel selbst der Text.</summary>
        public Dictionary<string, string> Beschriftungen = new Dictionary<string, string>();

        public string Beschriftung(string schluessel)
        {
            string t;
            return (Beschriftungen.TryGetValue(schluessel, out t) && !string.IsNullOrEmpty(t))
                ? t : schluessel;
        }

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

    /// <summary>
    /// Auszug aus <c>Allgemein\Wirtschaftlichkeit\WirtschaftlichkeitDaten.cs</c> -
    /// eine Verlaufslinie des Kapitalwert-Diagramms. Das Feld <c>Bild</c>
    /// (KapitalwertRechner.Zahlungsbild) fehlt hier: Der Renderer liest es nicht.
    /// </summary>
    public class VerlaufSerie
    {
        public int IdProjekt;
        public string Anzeige = "";
        public bool IstStamm;
        public double[] Kumuliert;      // Index = Jahr 0…N
        public double RestwertBarwert;
        public string Fehlgrund;
    }
}
