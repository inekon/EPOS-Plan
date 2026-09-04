using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Ein waehlbarer Steuerwert samt Anzeigetext (iU9-W11b) — die Drei-Schichten-Regel
/// als Datensatz: Der <see cref="Wert"/> steht in der Datenbank
/// (<c>DbWerte.SP_*</c>), der <see cref="Text"/> auf dem Bildschirm.
/// </summary>
public sealed record Steuerwahl(string Wert, string Text);

/// <summary>
/// Eine Zeile eines Ergebnisrasters — Modul-, Puffer- und Brennstoffzeilen der
/// Reiter teilen sich diese Form, weil sie alle dasselbe sind: ein Name und
/// eine Handvoll bereits formatierter Zahlen.
/// </summary>
/// <param name="Zellen">Die Zellen in Spaltenreihenfolge, fertig formatiert.</param>
/// <param name="Hinweis">Mouseover-Text der Zeile; leer = keiner.</param>
public sealed record Rasterzeile(IReadOnlyList<string> Zellen, string Hinweis = "");

/// <summary>
/// Eine Brennstoffzeile des Heizkessel- bzw. BHKW-Reiters: Beschriftung, Wert und
/// die Praesenzregel „sichtbar, wenn der Jahreswert &gt; 0 ist ODER ein Kessel des
/// Projekts diesen Brennstoff fuehrt" (Vorlaeufer
/// <c>KesselBrennstoffZeilenAnpassen</c> :1134-1195).
/// </summary>
public sealed record Brennstoffzeile(string Bezeichnung, double Wert, bool Sichtbar);

// =========================================================================
//  Die Parameterseite (R1 samt P1…P5)
// =========================================================================

/// <summary>
/// Die Felder des Stromspeicher-Parameterblatts P3 (Vorlaeufer
/// <c>InitStromspeicherParameter</c> :5690-5990, 302 Zeilen mit 29 programmatischen
/// Steuerelementen).
/// </summary>
public sealed class SpeicherParameterDaten
{
    /// <summary>Gibt es ueberhaupt eine aktive Variante? Ohne sie sind die Felder Attrappen.</summary>
    public bool VarianteVorhanden;

    /// <summary>Fusszeile: welche Variante wird hier bearbeitet (bzw. „keine").</summary>
    public string Variantenstatus = "";

    public double SoCMinProzent;
    public double SoCMaxProzent;

    /// <summary>Das kWh-Aequivalent des SoC-Bandes (Abnahmebefund 1); leer = unbestimmt.</summary>
    public string SoCMinKwh = "";
    public string SoCMaxKwh = "";

    public double Ladeschwellwert;

    /// <summary>Geraetedatum, nur Anzeige.</summary>
    public double LadeleistungKw;

    /// <summary>Geraetedatum, nur Anzeige.</summary>
    public double KapazitaetKwh;

    public string Betriebsart = "";
    public string Berechnungsart = "";
    public IReadOnlyList<Steuerwahl> Betriebsarten = Array.Empty<Steuerwahl>();
    public IReadOnlyList<Steuerwahl> Berechnungsarten = Array.Empty<Steuerwahl>();

    /// <summary>Der Kompatibilitaetsmodus ist nur bei bestimmten Berechnungsarten waehlbar.</summary>
    public bool Kompatibilitaet;
    public bool KompatibilitaetMoeglich;

    public bool LadenAusPv;
    public bool LadenAusBhkw;
    public bool Netzentladung;

    /// <summary>Ausbaustufe 11 — sichtbar, aber dauerhaft gesperrt.</summary>
    public bool BhkwStromgefuehrt;

    public double Kapitalzins;
    public double Nutzungsdauer;
    public double Leistungspreis;
    public double Netzladeaufschlag;

    public string Preisquelle = "";
    public IReadOnlyList<Steuerwahl> Preisquellen = Array.Empty<Steuerwahl>();

    /// <summary>Beschriftung der Reihenauswahl — „Preisreihe" oder „Kostenprofil".</summary>
    public string PreisreiheLabel = "";
    public int PreisreiheId;
    public IReadOnlyList<(int Id, string Text)> Preisreihen = Array.Empty<(int, string)>();
    public bool PreisreiheMoeglich;
    public bool Aufschlag;

    /// <summary>Die Preisvorschau; <see cref="PreisinfoWarnung"/> faerbt sie.</summary>
    public string Preisinfo = "";
    public bool PreisinfoWarnung;
}

/// <summary>
/// Der Stand der Parameterseite (R1). Die fuenf Unterblaetter haengen an
/// <c>Tab_Einstellungen.Tool_1..6</c> — „Bedarf" ist immer dabei
/// (<c>UpdateTabPages</c> :2843-2865).
/// </summary>
public sealed class ParameterDaten
{
    /// <summary>
    /// Die Unterblaetter in der Reihenfolge, in der der Vorlaeufer sie einhaengte:
    /// „Bedarf" immer zuerst, danach die Erzeuger in der Reihenfolge von
    /// <c>Tool_1..6</c> (<c>UpdateTabPages</c> :2848-2865). Die Schluessel stehen in
    /// <see cref="ParameterBlatt"/>.
    /// </summary>
    public IReadOnlyList<string> Unterblaetter = new[] { ParameterBlatt.Bedarf };

    // ---- P1: Wärme-/Strombedarf (immer) ----
    public double Netzverluste;
    public string NetzverlusteEinheit = "%";
    public IReadOnlyList<string> NetzverlusteEinheiten = new[] { "%" };

    // ---- P2: BHKW ----

    /// <summary>0 = wärmegeführt, 1 = stromgeführt, 2 = ohne Einspeisung.</summary>
    public int Betriebsart;
    public int UntersteLeistungsgrenze;

    // ---- P3: Stromspeicher ----
    public SpeicherParameterDaten Speicher = new SpeicherParameterDaten();

    // ---- P4: Wärmepumpe ----
    public bool Heizstab;

    // ---- P5: Heizkessel ----
    public double Bereitschaft;
}

/// <summary>
/// Die sprachneutralen Schluessel der fuenf Parameter-Unterblaetter (Befund
/// W11-B1: es sind FUENF, nicht vier). Sie ersetzen die <c>TabPage</c>-Namen des
/// Vorlaeufers und tragen keinen Umlaut mehr (Befund W11-B30).
/// </summary>
public static class ParameterBlatt
{
    public const string Bedarf = "BEDARF";
    public const string Bhkw = "BHKW";
    public const string Stromspeicher = "STROMSPEICHER";
    public const string Waermepumpe = "WAERMEPUMPE";
    public const string Heizkessel = "HEIZKESSEL";
}

// =========================================================================
//  Die Reiterdaten, die nicht schon als Kern-DTO vorliegen
// =========================================================================

/// <summary>
/// Der Bedarfsreiter (R4). Die vier Zahlen kommen aus
/// <c>SimulationErgebnisCtrl.Bedarf</c>; hier stehen nur die Kanalzeilen und
/// ihre Praesenz.
/// </summary>
public sealed class BedarfDaten
{
    public double WaermelastMaxKw;
    public double WaermebedarfGesamtMwh;
    public double StrombedarfMaxKw;
    public double StrombedarfGesamtMwh;

    /// <summary>Die drei Kanaele (Heizung, Brauchwasser, Prozesswärme) in MWh.</summary>
    public IReadOnlyList<double> KanalMwh = Array.Empty<double>();

    /// <summary>Beschriftung je Kanal.</summary>
    public IReadOnlyList<string> Kanalnamen = Array.Empty<string>();

    /// <summary>Führt der Lauf diesen Kanal? Nur dann steht sein Schalter da.</summary>
    public IReadOnlyList<bool> KanalDa = Array.Empty<bool>();
}

/// <summary>
/// Der Uebersichtsreiter (R2 + <c>NavigatorUebersicht</c>). Die 13 Zahlen und die
/// sechs Summen stehen im Kern-DTO; hier steht, was die Anzeige darum herum
/// braucht.
/// </summary>
public sealed class UebersichtDaten
{
    /// <summary>Die Praesenzregel je Erzeuger — sie blendet Zeilen und Ringsegmente aus.</summary>
    public bool Waermepumpe, Heizstab, Heizkessel, Solarthermie, Bhkw, Photovoltaik, Stromspeicher;

    /// <summary>Der Mittelwert der beiden Ringe in Prozent.</summary>
    public double WaermedeckungProzent;
    public double StromdeckungProzent;

    /// <summary>Gibt es überhaupt einen Bedarf? Ohne ihn kein Ring (Befund W11-B36).</summary>
    public bool WaermebedarfVorhanden;
    public bool StrombedarfVorhanden;

    public double ReststromMwh;
    public double RestwaermeMwh;

    /// <summary>Der Eigenanteil je Erzeuger und Bedarfskanal (<c>FillTableWithData</c>).</summary>
    public IReadOnlyList<Rasterzeile> Eigenanteil = Array.Empty<Rasterzeile>();

    /// <summary>Die Spaltenköpfe des Eigenanteil-Rasters.</summary>
    public IReadOnlyList<string> EigenanteilSpalten = Array.Empty<string>();
}

/// <summary>
/// Die Autarkie-Analyse (<c>DashboardForm</c>). Die Zahlen rechnet die Huelle je
/// Kapazitaet neu — die Kapazitaet ist eine Was-wäre-wenn-Groesse und wird
/// NICHT gespeichert (Befund W11-B32).
/// </summary>
public sealed class AutarkieDaten
{
    public bool HatPv = true;
    public bool HatSolarthermie = true;

    public double AutarkiePvProzent;
    public double DeckungStProzent;

    /// <summary>Ohne Wärmebedarf steht hier „nicht benötigt" statt einer Zahl.</summary>
    public bool DeckungStBekannt = true;

    public double NutzungsgradStProzent;
    public double Co2ErsparnisKg;
    public double SpeichernutzenKwh;
    public double SpeicherKwh;
}

/// <summary>
/// Eine waehlbare Reihe der beiden Ganglinien-Reiter: sprachneutraler Schluessel,
/// Anzeigetext und ob der Lauf sie ueberhaupt fuehrt.
/// </summary>
/// <param name="Schluessel">Der Serienschluessel — nie ein Anzeigetext.</param>
/// <param name="Text">Die Beschriftung des Schalters.</param>
/// <param name="Vorhanden">
/// Fuehrt der Lauf die Reihe? Fehlende werden AUSGEBLENDET und abgewaehlt — sonst
/// naehme der CSV-Export eine unsichtbare Spalte mit
/// (<c>NavigatorStrom.CheckboxenAnordnen</c> :266).
/// </param>
public sealed record Ganglinienreihe(string Schluessel, string Text, bool Vorhanden);

/// <summary>Der Waermegang-Reiter (<c>NavigatorWaerme</c>).</summary>
public sealed class WaermegangDaten
{
    /// <summary>Die bis zu fuenf Erzeugerreihen in Kaskadenreihenfolge.</summary>
    public IReadOnlyList<Ganglinienreihe> Erzeuger = Array.Empty<Ganglinienreihe>();

    /// <summary>Die Speicherfuellstaende; leer = das Projekt fuehrt keinen Speicher.</summary>
    public IReadOnlyList<Ganglinienreihe> Speicher = Array.Empty<Ganglinienreihe>();

    /// <summary>
    /// „Gesamt" und die Bedarfsarten mit Jahressumme &gt; 0; der erste Eintrag traegt
    /// den Steuerwert <c>-1</c> (Produktion), die uebrigen den Kanalindex.
    /// </summary>
    public IReadOnlyList<(int Kanal, string Text)> Bedarfsarten = Array.Empty<(int, string)>();
}

/// <summary>Der Stromgang-Reiter (<c>NavigatorStrom</c>).</summary>
public sealed class StromgangDaten
{
    /// <summary>Verbrauchsstapel, Erzeugungslinien und die Kontrolllinie „Gesamt".</summary>
    public IReadOnlyList<Ganglinienreihe> Reihen = Array.Empty<Ganglinienreihe>();
}

/// <summary>
/// Der Stromspeicher-Reiter (R10). Kopfzeile, zwoelf Kacheln, 39 Kennzahlzeilen
/// (die kommen als <c>SpeicherKennzahlenBlock.Zeile</c> aus dem Kern), Ampel und
/// Warnzeile.
/// </summary>
public sealed class SpeicherErgebnisDaten
{
    /// <summary>Gab es ueberhaupt einen Speicherlauf?</summary>
    public bool LaufVorhanden;

    /// <summary>Kopfzeile: Variante, Betriebsart, Berechnungsart — bzw. „noch kein Lauf".</summary>
    public string Kopf = "";

    /// <summary>Die zwoelf Kacheln in ihrer Reihenfolge (Titel, Wert).</summary>
    public IReadOnlyList<(string Titel, string Wert)> Kacheln = Array.Empty<(string, string)>();

    /// <summary>Die 39 Kennzahlzeilen aus <c>SpeicherKennzahlenBlock.Zeilen</c>.</summary>
    public IReadOnlyList<WindowsFormsApplication1.SpeicherKennzahlenBlock.Zeile> Kennzahlen
        = Array.Empty<WindowsFormsApplication1.SpeicherKennzahlenBlock.Zeile>();

    /// <summary>Gibt es einen Vergleichslauf? Ohne ihn bleibt die Vergleichsspalte weg.</summary>
    public bool MitVergleich;

    /// <summary>Die Zyklenampel; <see cref="AmpelWarnung"/> faerbt sie.</summary>
    public string Ampel = "";
    public bool AmpelWarnung;

    /// <summary>Warnzeile „Lauf ohne jede Erzeugung"; leer = keine.</summary>
    public string Erzeugungshinweis = "";

    /// <summary>Vergleichen laesst sich erst ab zwei Varianten (Fachkonzept 7.3).</summary>
    public bool MehrereVarianten;
}

/// <summary>
/// Eine Zeile des Variantenvergleichs (<c>Form_SpeicherVariantenVergleich</c>).
/// Die Zahlen sind bereits formatiert — die Rechnung steht in der Huelle.
/// </summary>
public sealed class Vergleichszeile
{
    public int IdEnergieanlage;
    public bool Aktiv;
    public bool Gerechnet;

    public string Bezeichnung = "";
    public string Betriebsart = "";
    public string Berechnungsart = "";

    public string Kapazitaet = "";
    public string Leistung = "";
    public string Investition = "";
    public string Ertrag = "";
    public string DeltaJ = "";
    public string Amortisation = "";
    public string Kapitalwert = "";
    public string Vollzyklen = "";

    /// <summary>Grund einer Fehlerzeile — Mouseover-Text.</summary>
    public string Hinweis = "";
}

/// <summary>Das Ergebnis eines Variantenvergleichslaufs.</summary>
public sealed class VergleichDaten
{
    public IReadOnlyList<Vergleichszeile> Zeilen = Array.Empty<Vergleichszeile>();

    /// <summary>Index der besten Zeile nach ΔJ; −1 = keine.</summary>
    public int BesteZeile = -1;

    /// <summary>Statuszeile; <see cref="StatusWarnung"/> faerbt sie.</summary>
    public string Status = "";
    public bool StatusWarnung;

    /// <summary>Es gibt Varianten, aber keine ist aktiv — der Langtext dazu.</summary>
    public bool HinweisKeineAktive;

    /// <summary>Das Protokoll der Laeufe (mehrzeilig).</summary>
    public string Protokoll = "";
}

// =========================================================================
//  Der Gesamtstand einer Auffrischung
// =========================================================================

/// <summary>
/// Was die Ergebnisseite nach EINEM Lauf zeigt (iU9-W11b.1). Die Huelle traegt es
/// zusammen; die Seite und ihre Reiter rechnen nichts nach.
/// </summary>
public sealed class SimulationErgebnisDaten
{
    public int IdProjekt;

    /// <summary>Die Schema-Migration ist nicht durchgekommen (ADR-001): alles gesperrt.</summary>
    public bool Gesperrt;
    public string Sperrgrund = "";

    /// <summary>Liegt ein vollstaendiger Lauf vor? Nur dann darf gespeichert werden.</summary>
    public bool ErgebnisGueltig;

    /// <summary>Die Parameterseite.</summary>
    public ParameterDaten Parameter = new ParameterDaten();

    // ---- Welche Fachreiter zeigt die Leiste? (BefuelleQuellenListe :2876-2970) ----
    public bool ReiterWaermepumpe;
    public bool ReiterHeizkessel;
    public bool ReiterBhkw;
    public bool ReiterSolarthermie;
    public bool ReiterPhotovoltaik;
    public bool ReiterStromspeicher;

    // ---- Die Zahlen je Reiter; null = der Lauf fuehrt die Komponente nicht ----
    public WindowsFormsApplication1.SimulationErgebnisCtrl.UebersichtKennzahlen? Kennzahlen;
    public UebersichtDaten Uebersicht = new UebersichtDaten();
    public BedarfDaten Bedarf = new BedarfDaten();
    public WindowsFormsApplication1.SimulationErgebnisCtrl.WaermepumpeErgebnis? Waermepumpe;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.HeizkesselErgebnis? Heizkessel;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.SolarthermieErgebnis? Solarthermie;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.BhkwErgebnis? Bhkw;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.PhotovoltaikErgebnis? Photovoltaik;
    public SpeicherErgebnisDaten Speicher = new SpeicherErgebnisDaten();
    public AutarkieDaten Autarkie = new AutarkieDaten();
    public WaermegangDaten Waermegang = new WaermegangDaten();
    public StromgangDaten Stromgang = new StromgangDaten();

    /// <summary>Die zehn Brennstoffzeilen des Heizkesselreiters samt Praesenz.</summary>
    public IReadOnlyList<Brennstoffzeile> KesselBrennstoffe = Array.Empty<Brennstoffzeile>();

    /// <summary>Die Brennstoffzeilen des BHKW — nur die mit Verbrauch &gt; 0.</summary>
    public IReadOnlyList<Brennstoffzeile> BhkwBrennstoffe = Array.Empty<Brennstoffzeile>();

    /// <summary>Die Erdreich-Kurztexte der VDI-4640-Pruefung; leer = keine.</summary>
    public IReadOnlyList<string> ErdreichHinweise = Array.Empty<string>();
    public bool ErdreichWarnung;

    /// <summary>Fuehrt der Lauf Speichertemperaturen? Nur dann steht das Unterblatt da.</summary>
    public bool Speichertemperaturen;

    /// <summary>Die Meldungen des Laufs (Warnungen und Hinweise); leer = keine.</summary>
    public string Laufmeldungen = "";
    public int LaufmeldungenAnzahl;
}

// =========================================================================
//  Die Datenseite — die Huelle legt sie ein
// =========================================================================

/// <summary>
/// Ein Bildauftrag der Ergebnisseite: welcher Reiter, in welcher Schalterstellung.
/// Die Seite bildet daraus ihren Zwischenspeicherschluessel — zwoelf PNG je Lauf
/// im Voraus zu rechnen waere zu teuer (Risiko der Vermessung § 11.5).
/// </summary>
/// <param name="Bild">Sprachneutraler Bildschluessel (<c>BEDARF_WAERME</c>, …).</param>
/// <param name="Sortiert">Dauerlinie statt Ganglinie.</param>
/// <param name="Kanal">Bedarfsart des Wärmegangs; −1 = Produktion.</param>
/// <param name="Reihen">Die gewaehlten Serienschluessel; leer = alle.</param>
/// <param name="Zahl">Freier Zahlenparameter (Was-wäre-wenn-Kapazitaet der Autarkie).</param>
public sealed record Bildauftrag(string Bild, bool Sortiert = false, int Kanal = -1,
                                 IReadOnlyList<string>? Reihen = null, double Zahl = 0.0)
{
    /// <summary>Der Zwischenspeicherschluessel — er trennt zwei Schalterstellungen.</summary>
    public string Schluessel =>
        Bild + "|" + (Sortiert ? "1" : "0") + "|" + Kanal + "|" + Zahl.ToString("R") + "|" +
        (Reihen is null ? "" : string.Join(",", Reihen));
}

/// <summary>Die sprachneutralen Bildschluessel der Ergebnisseite.</summary>
public static class Bilder
{
    public const string BedarfWaerme = "BEDARF_WAERME";
    public const string BedarfStrom = "BEDARF_STROM";
    public const string UebersichtKuchen = "UEBERSICHT_KUCHEN";
    public const string RingWaerme = "RING_WAERME";
    public const string RingStrom = "RING_STROM";
    public const string WpProduktion = "WP_PRODUKTION";
    public const string WpStromverbrauch = "WP_STROMVERBRAUCH";
    public const string WpLeistungTemperatur = "WP_LEISTUNG_TEMPERATUR";
    public const string Speichertemperaturen = "SPEICHERTEMPERATUREN";
    public const string Heizkessel = "HEIZKESSEL";
    public const string Solarthermie = "SOLARTHERMIE";
    public const string Bhkw = "BHKW";
    public const string Photovoltaik = "PHOTOVOLTAIK";
    public const string SpeicherSoc = "SPEICHER_SOC";
    public const string AutarkieMonate = "AUTARKIE_MONATE";
    public const string Waermegang = "WAERMEGANG";
    public const string Stromgang = "STROMGANG";
}

// Das Ergebnis eines Schreib- oder Rechenwegs meldet der Datensatz
// EPOS.UI.Seiten.Simulation.Rueckmeldung (SimulationKonfigDaten.cs, iU9-W10b.1) —
// dieselbe Frage, dieselbe Antwortform, deshalb kein zweiter Typ.

/// <summary>
/// Die Datenseite der Ergebnisseite — die Windows-Huelle legt sie ein
/// (iU9-W11b.1). Ohne Delegat geschieht an der Stelle nichts, und ein Knopf ohne
/// Delegat erscheint gar nicht erst.
/// </summary>
public sealed class SimulationErgebnisDienste
{
    /// <summary>Traegt alles zusammen, was die Seite zeigt. Nie <c>null</c>.</summary>
    public Func<int, SimulationErgebnisDaten>? Laden;

    /// <summary>
    /// Startet den Lauf. Die Huelle faehrt ihn in <c>Task.Run</c>, meldet den
    /// Fortschritt ueber <paramref name="melder"/> und nimmt den Abbruch entgegen;
    /// die Rueckmeldung traegt bei einem Abbruch dessen Grund.
    /// </summary>
    public Func<Action<double?, string>, Task<Rueckmeldung>>? Laufen;

    /// <summary>Bricht den laufenden Lauf ab.</summary>
    public Action? Abbrechen;

    /// <summary>Speichert das Ergebnis nach <c>Tab_Ergebnis*</c>.</summary>
    public Func<Rueckmeldung>? Speichern;

    /// <summary>Rendert EIN Bild — erst beim Betreten des Reiters, dann zwischengespeichert.</summary>
    public Func<Bildauftrag, byte[]?>? Bild;

    // ---- Die Parameterseite schreibt SOFORT, feldweise (wie der Vorlaeufer) ----

    /// <summary>Netzverluste und ihre Einheit.</summary>
    public Action<double, string>? NetzverlusteSchreiben;

    /// <summary>Die BHKW-Betriebsart (0/1/2).</summary>
    public Action<int>? BetriebsartSchreiben;

    /// <summary>Die unterste Leistungsgrenze der BHKW-Module.</summary>
    public Action<int>? LeistungsgrenzeSchreiben;

    /// <summary>Der Heizstabschalter der Wärmepumpe.</summary>
    public Action<bool>? HeizstabSchreiben;

    /// <summary>Die Betriebsbereitschaft des Heizkessels.</summary>
    public Action<double>? BereitschaftSchreiben;

    /// <summary>
    /// Ein Feld der Speichervariante — der Schluessel benennt das Feld
    /// (<see cref="SpeicherFeld"/>), der Wert steht als Zeichenkette darin.
    /// </summary>
    public Action<string, string>? SpeicherfeldSchreiben;

    // ---- Was die Seite oeffnet ----

    /// <summary>Parametersatz der Konfigurationsseite (W10b) als Ueberlagerung.</summary>
    public Func<IReadOnlyDictionary<string, object>>? KonfigurationGaben;

    /// <summary>Parametersatz des Bedarfsergebnis-Dialogs (W8); true = Wärme, false = Strom.</summary>
    public Func<bool, IReadOnlyDictionary<string, object>>? BedarfGaben;

    /// <summary>Parametersatz des Wärmepumpendialogs (W7).</summary>
    public Func<IReadOnlyDictionary<string, object>>? WaermepumpenGaben;

    /// <summary>Nimmt das Ergebnis des Wärmepumpendialogs entgegen (true = übernommen).</summary>
    public Action<bool>? WaermepumpenFertig;

    /// <summary>Rechnet den Variantenvergleich; <paramref name="melder"/> zaehlt n von m.</summary>
    public Func<Action<double?, string>, Task<VergleichDaten>>? VergleichRechnen;

    /// <summary>Setzt eine Variante aktiv; die Rueckmeldung traegt den Fehlertext.</summary>
    public Func<int, Rueckmeldung>? VarianteAktivSetzen;

    /// <summary>Schreibt die Vergleichstabelle als CSV.</summary>
    public Func<Task<Rueckmeldung>>? VergleichCsv;

    /// <summary>Die Sprungbruecke — heute nur <c>Sprungziel.SpeicherOptimierung</c>.</summary>
    public Func<string, Task<bool>>? Sprung;

    // ---- Die vier CSV-Exporte ----

    public Action? CsvBedarf;
    public Action? CsvWaermepumpe;
    public Action? CsvHeizkessel;
    public Action? CsvSpeicher;

    /// <summary>CSV des Wärmegangs — nur die angehakten Reihen, immer chronologisch.</summary>
    public Action<int, IReadOnlyList<string>, IReadOnlyList<string>>? CsvWaermegang;

    /// <summary>CSV des Stromgangs — nur die angehakten Reihen.</summary>
    public Action<IReadOnlyList<string>>? CsvStromgang;

    /// <summary>Rechnet die Autarkie-Kacheln zu einer Was-wäre-wenn-Kapazitaet neu.</summary>
    public Func<double, AutarkieDaten>? AutarkieRechnen;
}

/// <summary>
/// Die Feldschluessel der Speichervariante — sprachneutral und ASCII
/// (Drei-Schichten-Regel). Sie benennen das Feld, das
/// <see cref="SimulationErgebnisDienste.SpeicherfeldSchreiben"/> setzt.
/// </summary>
public static class SpeicherFeld
{
    public const string SoCMin = "SOC_MIN";
    public const string SoCMax = "SOC_MAX";
    public const string Ladeschwelle = "LADESCHWELLE";
    public const string Betriebsart = "BETRIEBSART";
    public const string Berechnungsart = "BERECHNUNGSART";
    public const string Kompatibilitaet = "KOMPATIBILITAET";
    public const string LadenPv = "LADEN_PV";
    public const string LadenBhkw = "LADEN_BHKW";
    public const string Netzentladung = "NETZENTLADUNG";
    public const string Kapitalzins = "KAPITALZINS";
    public const string Nutzungsdauer = "NUTZUNGSDAUER";
    public const string Leistungspreis = "LEISTUNGSPREIS";
    public const string Netzladeaufschlag = "NETZLADEAUFSCHLAG";
    public const string Preisquelle = "PREISQUELLE";
    public const string Preisreihe = "PREISREIHE";
    public const string Aufschlag = "AUFSCHLAG";
}
