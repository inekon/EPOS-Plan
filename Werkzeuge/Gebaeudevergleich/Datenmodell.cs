using System.Collections.Generic;

namespace Gebaeudevergleich
{
    /// <summary>Die Ampel einer Gebäude- oder Projektzeile; die Reihenfolge ist die Schwere.</summary>
    internal enum Ampel
    {
        /// <summary>Die Abweichung ist durch eine Regel erklärt.</summary>
        Erklaert = 0,

        /// <summary>Keine Regel erklärt die Abweichung; die Hinweisregeln nennen Spuren.</summary>
        ZuPruefen = 1,

        /// <summary>Ein Weg ist gescheitert, ein Wert unplausibel oder ein Datenfehler erkannt.</summary>
        Fehler = 2,
    }

    /// <summary>Das Ergebnis EINES Rechenwegs an einem Gebäude — Kennzahlen oder Fehlergrund.</summary>
    internal sealed class Wegergebnis
    {
        /// <summary><c>DbWerte.GEBAEUDE_MODELL_TAGESBILANZ</c> oder <c>…_VDI6007</c>.</summary>
        internal string Weg = "";

        internal bool Ok;

        /// <summary>Kurzer Fehlercode (<c>BauweiseUnplausibel</c>, <c>Ausnahme:…</c>, <c>NichtEndlich</c> …); leer bei Erfolg.</summary>
        internal string Fehlercode = "";

        /// <summary>Der Fehlergrund, bereinigt (Fehler und Warnungen des Protokolls oder der Ausnahmetext).</summary>
        internal string Fehlertext = "";

        /// <summary>Alle Meldungen des Aufrufs, bereinigt, in einer Zeile.</summary>
        internal string Meldungen = "";

        internal double JahrMwh = double.NaN;
        internal double SpitzeKw = double.NaN;
        internal double TagesmittelKw = double.NaN;
        internal double Q95Kw = double.NaN;
        internal double VollbenutzungsstundenH = double.NaN;
        internal double NachtanteilProzent = double.NaN;
        internal double Heizstunden = double.NaN;
        internal double[] MonateMwh = new double[12];
        internal double[] StundenKw;

        /// <summary>Jahreswärme / (spez · Bezugsfläche) [%]; nur Flächenangabe mit Katalogwert.</summary>
        internal double? KatalogtrefferProzent;

        /// <summary>Jahreswärme / angegebener Verbrauch [%]; nur Verbrauchsangabe.</summary>
        internal double? VerbrauchstrefferProzent;

        // Nur VDI-Weg.
        internal double? RaumtemperaturMittelC;
        internal int? Ueberhitzungsstunden;
        internal double? KaelteMwh;
        internal double? KaeltespitzeKw;

        // Aus dem Ergebnis (Merkmale).
        internal bool KuehlbetriebProjekt;
        internal bool KuehlungAktiv;
        internal bool Gekoppelt;

        /// <summary>Die Rechenzeit [s] — nur fürs Protokoll, nie in eine CSV (T11).</summary>
        internal double Sekunden;
    }

    /// <summary>Die Merkmale einer Gebäudezeile — aus der gelesenen Zeile und dem Ergebnis.</summary>
    internal sealed class Merkmale
    {
        internal bool IstFlaeche;
        internal string Einheit = "";
        internal double Bezugswert;
        internal double SpezWaermeverbrauch;
        internal double Nutzflaeche;
        internal double? Skalierung;
        internal double Bauweise;
        internal double? BauweiseJeM2;
        internal double? FensteranteilProzent;
        internal double? SuedanteilProzent;
        internal double InnereGewinneW;
        internal double? InnereGewinneWm2;
        internal string WgNwg = "";
        internal bool IstNwg;
        internal string Gebaeudeart = "";
        internal string Baualtersklasse = "";
        internal double SollTagC;
        internal double SollNachtC;
        internal double AbsenkungK;
        internal double Luftwechsel;
        internal bool Zone;
        internal bool HeizkreisAktiv;
        internal bool FesteNennleistung;
        internal bool KuehlungWirksam;
        internal bool Gekoppelt;

        /// <summary>Der Spaltenwert <c>Gebaeude_Modell</c> (leer = NULL, rechnet VDI 6007).</summary>
        internal string SpalteModell = "";

        /// <summary>Der angegebene Verbrauch [kWh/a] bei Verbrauchsangabe (<c>GebaeudeVorbereitung.VerbrauchNeu</c>).</summary>
        internal double VerbrauchNeuKwh;

        /// <summary>Die Bauform-Gruppe: kurzer Hash über die Gebäudezeile ohne IDs, Namen, Beschreibung.</summary>
        internal string Bauform = "";
    }

    /// <summary>Das Urteil der Ursachenregeln über eine Gebäudezeile.</summary>
    internal sealed class Regelbefund
    {
        internal Ampel Ampel;

        /// <summary>„Datenfehler" bei U‑BW, sonst leer.</summary>
        internal string Vermerk = "";

        /// <summary>Die Codes der Regeln, die greifen (<c>U-MW</c>, <c>U-SP</c> …), in fester Reihenfolge.</summary>
        internal List<string> Codes = new List<string>();
    }

    /// <summary>Eine Gebäudezeile des Vergleichs.</summary>
    internal sealed class Gebaeudezeile
    {
        internal int IdProjekt;
        internal int IdZ;
        internal int IdGebaeude;
        internal string Projektname = "";
        internal string Gebaeudename = "";
        internal Merkmale Merkmale = new Merkmale();
        internal Wegergebnis Alt = new Wegergebnis();
        internal Wegergebnis Neu = new Wegergebnis();
        internal Regelbefund Befund = new Regelbefund();

        /// <summary>Beide Wege gerechnet?</summary>
        internal bool BeideOk => Alt.Ok && Neu.Ok;
    }

    /// <summary>Eine Projektzeile des Vergleichs.</summary>
    internal sealed class Projektzeile
    {
        internal int Id;
        internal string Name = "";
        internal int Klimaregion;
        internal bool Uebersprungen;
        internal string Grund = "";
        internal List<Gebaeudezeile> Gebaeude = new List<Gebaeudezeile>();

        /// <summary>Summen über die Gebäude, die auf BEIDEN Wegen gerechnet sind.</summary>
        internal double SummeAltMwh;
        internal double SummeNeuMwh;

        /// <summary>Spitze der addierten Stundenreihen derselben Gebäude [kW].</summary>
        internal double SpitzeAltKw;
        internal double SpitzeNeuKw;

        internal int GebaeudeGerechnet;

        /// <summary>Die Zahl der zugeordneten Gebäude — auch bei einem übersprungenen Projekt.</summary>
        internal int GebaeudeZugeordnet;

        internal Ampel Ampel;
    }
}
