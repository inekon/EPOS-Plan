using System.Collections.Generic;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Farbklang eines Chips — die Zuordnung aus Konzept 3
/// (Vorlaeufer <c>ErzeugerKarte.ChipStil</c>:98-120).
/// </summary>
public enum ChipStil
{
    /// <summary>Grauer Rahmen (Zweitsenke, Betriebsmodus).</summary>
    Neutral,

    /// <summary>Blau (Waermequelle).</summary>
    Quelle,

    /// <summary>Blau gestrichelt (Quelle ist ein Pufferspeicher = Kaskade).</summary>
    QuelleKaskade,

    /// <summary>Koralle (Haupt- und Zweitsenke auf einen Puffer).</summary>
    Senke,

    /// <summary>Nur Fuellflaeche, kein Rahmen (Temperaturpaar, Modulausweis).</summary>
    Flaeche,

    /// <summary>Amber (Temperatur-Warnregel, Warnkriterien).</summary>
    Warnung
}

/// <summary>
/// Editor, den ein Doppelklick auf DIESEN Chip oeffnet — der Ersatz fuer den
/// Spalten-Dispatcher der alten Uebersicht (Vorlaeufer <c>ErzeugerKarte.ChipZiel</c>).
///
/// <para><see cref="Keines"/> oeffnet den Standard-Editor der Kachel — genauso wie eine
/// Spalte, die nicht in der Whitelist stand, frueher nichts tat.</para>
/// </summary>
public enum ChipZiel
{
    Keines,
    Quelle,
    Senke,
    Zweitsenke,
    Modus,
    Prioritaet
}

/// <summary>Ein Chip, so wie ihn die Konfigurationsseite beschreibt.</summary>
/// <param name="Text">Der sichtbare Text; leer = kein Chip.</param>
/// <param name="Stil">Der Farbklang.</param>
/// <param name="Hinweis">Kurzhinweis (Mouseover und Sprachausgabe); leer = keiner.</param>
/// <param name="Ziel">Der Editor hinter dem Doppelklick.</param>
public sealed record ChipDaten(
    string Text,
    ChipStil Stil = ChipStil.Neutral,
    string Hinweis = "",
    ChipZiel Ziel = ChipZiel.Keines);

/// <summary>
/// Nimmt die Komponente an der Simulation teil?
///
/// <para>Das ist die Auswahl, die im Vorlaeufer die vier Waermeerzeuger-Auswahlfelder
/// samt ihren Haken und die beiden Strom-Auswahlfelder trafen: Sie entschieden, WELCHE
/// im Projekt vorhandene Technologie in <c>Tab_Einstellungen.Tool_1..6</c> landet und
/// damit gerechnet wird. Die Kacheln bilden genau das ab — sie sind an dieser Stelle
/// NICHT nur Anzeige.</para>
/// </summary>
public enum Kachelzustand
{
    /// <summary>In der Simulation (steht in Tool_1..6).</summary>
    Aufgenommen,

    /// <summary>Im Katalog waehlbar, aber nicht aufgenommen — leerer Auswahlplatz.</summary>
    Verfuegbar
}

/// <summary>
/// Alles, was eine ERZEUGERKACHEL fuer ihren Aufbau braucht
/// (Vorlaeufer <c>ErzeugerKarte.Aufbau</c>:173-200).
/// </summary>
public sealed class ErzeugerKachelDaten
{
    /// <summary>Der Schluessel, den die Kachel bei Ereignissen meldet.</summary>
    public string Schluessel = "";

    /// <summary>Kaskadenrang; leer = kein Rang (Strom- und Speicherseite).</summary>
    public string Rang = "";

    public string Titel = "";

    public IReadOnlyList<ChipDaten> Chips = new List<ChipDaten>();

    public Kachelzustand Zustand = Kachelzustand.Aufgenommen;

    /// <summary>▲▼ anbieten (nur bei aufgenommenen Waermeerzeugern).</summary>
    public bool Reihenfolge;

    public bool AufMoeglich;
    public bool AbMoeglich;

    /// <summary>+ bzw. × anbieten (Auswahl-Mechanik).</summary>
    public bool Umschaltbar;

    /// <summary>✎ anbieten (oeffnet den Senkendialog).</summary>
    public bool Editierbar;

    /// <summary>
    /// Chips des AUFKLAPPBAREN Detailbereichs. Leer = die Kachel hat keinen
    /// Detailbereich und sieht aus wie bisher.
    /// </summary>
    public IReadOnlyList<ChipDaten> Detailchips = new List<ChipDaten>();

    /// <summary>Zustand des Detailbereichs (die Seite merkt ihn sich).</summary>
    public bool Aufgeklappt;

    /// <summary>Die Kachel ist das im Schema markierte Element (oder umgekehrt).</summary>
    public bool Hervorgehoben;
}

/// <summary>
/// Alles, was eine SPEICHERKACHEL anzeigt
/// (Vorlaeufer <c>SpeicherKarte.Daten</c>:107-147).
/// </summary>
public sealed class SpeicherKachelDaten
{
    /// <summary><c>Tab_Pufferspeicher.ID</c> — Kontext fuer den Editor-Aufruf.</summary>
    public int IdPuffer;

    public string Bezeichner = "";

    /// <summary>Verwendungs-Badge (Heizung | Warmwasser | …), bereits uebersetzt.</summary>
    public string Verwendung = "";

    /// <summary>Schicht-Badge („5 Schichten"); leer bei einem Ein-Zonen-Speicher.</summary>
    public string Schichtung = "";

    /// <summary>Volumen mit Einheit, z. B. „778 l"; leer = nicht gepflegt.</summary>
    public string Volumen = "";

    /// <summary>Temperaturpaar, z. B. „55 / 45 °C"; leer = nicht gepflegt.</summary>
    public string Temperaturpaar = "";

    public int LaderAnzahl;
    public int AbnehmerAnzahl;

    /// <summary>Die Zeilen der Detailkarte (Lader, Versorgt, Quelle fuer, …).</summary>
    public IReadOnlyList<string> Detailzeilen = new List<string>();

    /// <summary>Beschriftung unter dem Schwellenband, z. B. „Schwellen 10 / 70 / 95 %".</summary>
    public string Schwellentext = "";

    public double SchwelleEin = 10.0;
    public double SchwelleAusNachrang = 95.0;
    public double SchwelleAus = 95.0;

    /// <summary>Der Speicher ist das im Schema markierte Element (oder umgekehrt).</summary>
    public bool Hervorgehoben;
}
