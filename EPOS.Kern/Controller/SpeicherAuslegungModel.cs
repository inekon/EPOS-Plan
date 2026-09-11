using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>Die Quelle wird gespeichert; inaktive Eingaben bleiben erhalten.</summary>
    public enum SpeicherAuslegungQuelle { Epos = 0, Datei = 1, Keine = 2, Preisprofil = 3 }
    public enum SpeicherKostenQuelle { Dialog = 0, Kostenmodul = 1 }

    /// <summary>
    /// Wie streng ein Lauf fehlende — und tatsächlich gebrauchte — Kostensätze nimmt
    /// (Befund #185).
    /// </summary>
    /// <remarks>
    /// Der STUDIENLAUF vergleicht Speichervarianten wirtschaftlich; ohne Kostensätze hat
    /// er kein Ergebnis und bricht deshalb weiterhin ab — mit dem Ausweg im Meldungstext.
    /// Der PROJEKTLAUF rechnet dagegen das Betriebsverhalten des ganzen Projekts; die
    /// spezifischen Kostensätze gehen dort in KEINE Netz-, SoC- oder Energiegröße ein
    /// (sie erreichen allein <c>FlottenWirtschaftlichkeit</c>). Ein fehlender Kostensatz
    /// darf einen Projektlauf deshalb nicht kippen: Betrieb und Netzwirkung werden
    /// gerechnet, Kapitalwert und Jahreskonten sind „nicht bewertbar“.
    /// </remarks>
    public enum KostenPflicht
    {
        /// <summary>Fehlende, aber gebrauchte Sätze sind ein Fehler.</summary>
        Studienlauf = 0,

        /// <summary>Fehlende Sätze werden mit 0 angesetzt und als nicht bewertbar gekennzeichnet.</summary>
        Projektlauf = 1
    }

    /// <summary>Spezifische Nettokosten. Kapazität und entladene Energie sind verschiedene Basen.</summary>
    public sealed class SpeicherKostensaetze
    {
        public double InvestEurProKw { get; set; }
        public double InvestEurProKwh { get; set; }
        public double BetriebEurProKwJahr { get; set; }
        public double BetriebEurProKwhJahr { get; set; }
        public double BetriebEurProKwhEntladen { get; set; }
        public bool InvestVorhanden { get; set; }
        public bool BetriebVorhanden { get; set; }

        /// <summary>
        /// Die Sätze wurden für einen Projektlauf mit 0 angesetzt, weil sie gebraucht
        /// werden, aber fehlen (<see cref="KostenPflicht.Projektlauf"/>). Betrieb und
        /// Netzwirkung sind gerechnet; Kapitalwert und Jahreskonten sind es nicht.
        /// </summary>
        public bool NichtBewertbar { get; set; }

        public string Herkunft { get; set; } = "";
        public List<string> AusgelassenePositionen { get; set; } = new();
    }

    /// <summary>Quellen und Kosten eines gespeicherten Auslegungsauftrags, ohne Datenbankzugriff.</summary>
    public sealed class SpeicherAuslegungKonfiguration
    {
        public SpeicherKostenQuelle Investitionsquelle { get; set; }
        public SpeicherKostenQuelle Betriebsquelle { get; set; }
        public SpeicherKostensaetze DirekteKosten { get; set; } = new();
        /// <summary>
        /// Für den Lauf bereits aufgelöste Kostensätze. Die Optimierung liest keine
        /// Quelle und keine Datenbank erneut, sondern verwendet ausschließlich diese Kopie.
        /// </summary>
        public SpeicherKostensaetze VerwendeteKosten { get; set; } = new();
        public SpeicherAuslegungQuelle Lastquelle { get; set; }
        public SpeicherAuslegungQuelle PvQuelle { get; set; }
        public SpeicherAuslegungQuelle Preisquelle { get; set; }
        public KostenprofilModel Strompreisprofil { get; set; }
        public SpeicherZeitreihe LastDatei { get; set; }
        public SpeicherZeitreihe PvDatei { get; set; }
        public SpeicherZeitreihe PreisDatei { get; set; }
        public bool EposModelljahrZuordnen { get; set; }
        public string Profilname { get; set; } = "";
        public long Revision { get; set; }
        /// <summary>Physisch gleichzeitig betriebene Einheiten innerhalb dieser Variante.</summary>
        public SpeicherEngine.FlottenStudieKonfiguration Flotte { get; set; }
        /// <summary>Die gewählte Flotte wird im gewöhnlichen Projektlauf statt des Einzelspeichers gefahren.</summary>
        public bool FlotteImProjektAktiv { get; set; }
        /// <summary>Expliziter Rückweg zum Einzelbetrieb; erneutes Speichern der Flotte hebt ihn auf.</summary>
        public bool FlottenProjektbetriebDeaktiviert { get; set; }
        public bool FlottenGroessenOptimieren { get; set; }
        public List<SpeicherEngine.FlottenPrognoseSnapshot> FlottenPrognosen { get; set; } = new();
        public string FlottenPrognoseDatei { get; set; } = "";
        public SpeicherFlottenCsvOptionen FlottenPrognoseCsvOptionen { get; set; }
        public List<SpeicherEngine.FlottenProjektjahr> FlottenProjektjahre { get; set; } = new();
        public string FlottenJahresdatenDatei { get; set; } = "";
        public SpeicherFlottenCsvOptionen FlottenJahresCsvOptionen { get; set; }
        /// <summary>Nur zum Lesen älterer Profile erhalten. Modellachsen werden automatisch aus der Reihenlänge gebildet.</summary>
        public int FlottenModelljahr { get; set; } = 2026;
        /// <summary>Schützt nach der einmaligen Bedienvorbelegung ausdrücklich geänderte Auswahlwerte.</summary>
        public int FlottenBedienvorgabenVersion { get; set; }
    }

    public sealed class SpeicherAuslegungProfil
    {
        public string Name { get; set; } = "";
        public SpeicherOptimierungEingaben Eingaben { get; set; } = new();
    }

    /// <summary>Ein Dateiwähler liefert Inhalt, nicht einen künftig erforderlichen Dateipfad.</summary>
    public sealed class SpeicherImportDatei
    {
        public string Dateiname { get; set; } = "";
        public byte[] Inhalt { get; set; } = Array.Empty<byte>();
    }

    public static class SpeicherAuslegungKopie
    {
        public static readonly JsonSerializerOptions JsonOptionen = new() { IncludeFields = true };
        public static T Von<T>(T wert) => wert == null ? default :
            JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(wert, JsonOptionen), JsonOptionen);
    }
}
