using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;

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
//  Die Parameterseite (R1 samt P1, P2, P4, P5) und der Speicherparameterblock
// =========================================================================

/// <summary>
/// Die Felder des Stromspeicher-Parameterblatts (Vorlaeufer
/// <c>InitStromspeicherParameter</c> :5690-5990, 302 Zeilen mit 29 programmatischen
/// Steuerelementen).
///
/// <para><b>Sie stehen seit W11b‑B‑28 im ERGEBNISreiter „Stromspeicher"</b>
/// (<c>SpeicherParameterBlock.razor</c>) und nicht mehr als Blatt P3 im Reiter
/// „Parameter" — Anwenderwunsch 10.09.2026. Der Weg der Daten bleibt derselbe: Die
/// Huelle fuellt sie in <c>ParameterDaten.Speicher</c>, die Seite reicht sie durch.</para>
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

    /// <summary>Lade-/Entladeleistung [kW] — Gerätedatum aus <c>Tab_Stromspeicher</c>.</summary>
    public double LadeleistungKw;

    /// <summary>Nennkapazität [kWh] — Gerätedatum aus <c>Tab_Stromspeicher</c>.</summary>
    public double KapazitaetKwh;

    /// <summary>
    /// Dürfen <see cref="LadeleistungKw"/> und <see cref="KapazitaetKwh"/> geändert
    /// werden? (W11b‑B‑28)
    ///
    /// <para>Sie gehören der ANLAGE und nicht der Variante: Varianten desselben
    /// Speichers teilen sich EINE Gerätekopie in <c>Tab_Stromspeicher</c>. Deshalb
    /// setzt die Hülle das Feld auf dieselbe Bedingung, unter der auch
    /// <c>StromspeicherSimCtrl.UebernehmeAuslegung</c> schreibt: <b>genau eine</b>
    /// <c>SP_TYP</c>-Anlage im Projekt. Sonst bleiben die Felder gesperrt und tragen
    /// den bisherigen Hinweis <c>SP_PARAM_HINWEIS_LADELEISTUNG</c>.</para>
    /// </summary>
    public bool GeraetegroesseAenderbar;

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
/// Die Reihenauswahl zu EINER Preisquelle (W11b‑B‑28) — Beschriftung, Wählbarkeit,
/// Liste und vorbelegte Id.
///
/// <para><b>Wozu ein eigener Datensatz.</b> Im Parameterblock ist der Wechsel der
/// Preisquelle keine Schreib-, sondern eine ANZEIGEfrage: Aus „Spotmarkt" wird
/// „Preisreihe" mit den Reihen aus <c>Tab_Preisreihe</c>, aus „Profil" wird
/// „Kostenprofil" mit denen aus <c>Tab_Kostenprofil</c>, und beim Fixpreis ist die
/// Liste leer und gesperrt. Bis W11b‑B‑28 kam diese Umstellung nur mit dem nächsten
/// vollständigen Lesen der Seite — der Block bleibt beim Wechsel der Quelle aber
/// stehen, und eine falsch beschriftete Liste wäre schlimmer als eine unveränderte.</para>
/// </summary>
public sealed class SpeicherPreisreihenDaten
{
    /// <summary>Beschriftung der Reihenauswahl — „Preisreihe" oder „Kostenprofil".</summary>
    public string Label = "";

    /// <summary>Ist die Liste überhaupt wählbar? (Beim Fixpreis nicht.)</summary>
    public bool Moeglich;

    public IReadOnlyList<(int Id, string Text)> Reihen = Array.Empty<(int, string)>();

    /// <summary>Die zu dieser Quelle gepflegte Id (Preisreihe bzw. Kostenprofil); 0 = keine.</summary>
    public int Id;
}

/// <summary>
/// Der Stand der Parameterseite (R1). Die Unterblaetter haengen an
/// <c>Tab_Einstellungen.Tool_1..6</c> — „Bedarf" ist immer dabei
/// (<c>UpdateTabPages</c> :2843-2865); das Blatt „Stromspeicher" ist mit W11b‑B‑28
/// in den Ergebnisreiter gezogen und steht nicht mehr darunter.
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

    // ---- Stromspeicher: kein Blatt dieses Reiters mehr (W11b-B-28), aber
    //      weiterhin Teil DIESES Datensatzes - die Huelle liest die Parameterseite
    //      in einem Zug, und der Ergebnisreiter "Stromspeicher" bekommt sie von der
    //      Seite durchgereicht.
    public SpeicherParameterDaten Speicher = new SpeicherParameterDaten();

    // ---- P4: Wärmepumpe ----
    public bool Heizstab;

    // ---- P5: Heizkessel ----
    public double Bereitschaft;
}

/// <summary>
/// Die sprachneutralen Schluessel der Parameter-Unterblaetter. Sie ersetzen die
/// <c>TabPage</c>-Namen des Vorlaeufers und tragen keinen Umlaut mehr (Befund
/// W11-B30).
///
/// <para><b>Es sind VIER</b> (W11b‑B‑28, Anwenderwunsch 10.09.2026). Der Vorlaeufer
/// fuehrte fuenf (Befund W11-B1), und der Reiter fuehrte sie bis dahin auch:
/// Bedarf, BHKW, <b>Stromspeicher</b>, Waermepumpe, Heizkessel. Das
/// Stromspeicherblatt ist als <c>SpeicherParameterBlock</c> in den ERGEBNISreiter
/// „Stromspeicher" gezogen — dorthin, wo die Zahlen stehen, zu denen es gehoert.
/// Sein Schluessel entfaellt ersatzlos; <c>BlattZuTool</c> der Huelle liefert fuer
/// <c>ERZEUGER_STROMSPEICHER</c> kein Blatt mehr.</para>
/// </summary>
public static class ParameterBlatt
{
    public const string Bedarf = "BEDARF";
    public const string Bhkw = "BHKW";
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
    /// <summary>Übernommene Projektflotte und tatsächlicher letzter Flottenlauf.</summary>
    public bool FlotteImProjektAktiv;
    public SpeicherEngine.FlottenStudieKonfiguration? AktiveFlotte;
    public bool FlottenAenderungOhneNeuenLauf;
    public SpeicherFlottenErgebnis? Flottenergebnis;
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
/// <param name="Bereich">DATENZOOM (Windows-Abnahme 05.09.2026): das Rechteck, das der
/// Anwender im Bild aufgezogen hat, in Bildanteilen; <c>null</c> = die volle Ansicht.
/// Die Hülle lässt den Kern daraus einen Achsenbereich machen — nur die
/// Jahresganglinien werten ihn aus, alle übrigen Bilder übergehen ihn.</param>
public sealed record Bildauftrag(string Bild, bool Sortiert = false, int Kanal = -1,
                                 IReadOnlyList<string>? Reihen = null, double Zahl = 0.0,
                                 Diagrammbereich? Bereich = null)
{
    /// <summary>Der Zwischenspeicherschluessel — er trennt zwei Schalterstellungen.</summary>
    public string Schluessel =>
        Bild + "|" + (Sortiert ? "1" : "0") + "|" + Kanal + "|" + Zahl.ToString("R") + "|" +
        (Reihen is null ? "" : string.Join(",", Reihen)) +
        (Bereich is null ? "" : "|" + Bereich);
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
    public const string SpeicherBetrieb = "SPEICHER_BETRIEB";
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

    // ---- Die Parameter schreiben SOFORT, feldweise (wie der Vorlaeufer) ----

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
    /// Ein Feld der Speichervariante bzw. der Speicheranlage — der Schluessel benennt
    /// das Feld (<see cref="SpeicherFeld"/>), der Wert steht als Zeichenkette darin
    /// (Zahlen invariant, Schalter "1"/"0"). Die Rueckmeldung traegt den Grund einer
    /// Abweisung (Pruefung) oder eines Fehlschlags — sonst die Bestaetigung.
    /// </summary>
    /// <remarks>
    /// <b>Der EINZIGE Schreibweg der Speicherparameter</b> (W11b‑B‑29,
    /// Anwenderentscheid 1 vom 10.09.2026: "Sofort schreiben, wie ueberall sonst im
    /// Programm."). Ihn nehmen der Parameterblock des Ergebnisreiters
    /// (<c>SpeicherParameterBlock</c>, jedes Feld einzeln und sofort) UND die
    /// Auslegungsoptimierung fuer ihren Leistungspreis L_P
    /// (<c>OptimierungLeistungspreis</c>, W11b‑E‑3). Der gepufferte Satzweg aus
    /// W11b‑B‑28 ist ersatzlos entfallen: Sein Puffer starb beim Reiterwechsel, und
    /// der Anwender fand den Knopf am Blockende nicht.
    /// <para><b>Geprueft wird VOR dem Schreiben</b>, je Feld die Regel, die zu ihm
    /// gehoert (<c>SpeicherParameterPruefung</c> im Kern). Ein Verstoss weist ab, und
    /// es geht nichts in die Datenbank; der Wert bleibt im Feld stehen, damit der
    /// Anwender zu Ende tippen kann.</para>
    /// </remarks>
    public Func<string, string, Rueckmeldung>? SpeicherfeldSchreiben;

    /// <summary>
    /// Die Reihenauswahl zu einer Preisquelle — sie LIEST nur (kein Schreibweg).
    /// Ohne Delegat bleibt die Liste so stehen, wie sie geliefert wurde.
    /// </summary>
    public Func<string, SpeicherPreisreihenDaten>? SpeicherPreisreihen;

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

    // ---- Die Auslegungsoptimierung des Stromspeichers (W11b-B-5) ----
    //
    // Bis zur Windows-Abnahme V2 (07.09.2026) stand hier EIN Delegat: die
    // Sprungbruecke (Func<string, Task<bool>> Sprung) mit dem einzigen Schluessel
    // Sprungziel.SpeicherOptimierung. Sie oeffnete Form_SpeicherOptimierung modal
    // ueber der WebView. Die Maske ist gefallen (Befunde „Texte ueberschneiden
    // sich" und „Dialog stuerzt nach kurzer Zeit ab"); an ihre Stelle tritt die
    // Ueberlagerung SpeicherOptimierungDialog, und aus dem einen Schluessel werden
    // diese fuenf benannten Wege. Damit ist auch das letzte Sprungziel weg.

    /// <summary>
    /// Liest den Vorschlag fuer den Suchraum samt der aktuellen Auslegung
    /// (Datenbankzugriff, Bedienfaden). Ohne Delegat steht der Vorschlag des
    /// Fachkonzepts da.
    /// </summary>
    public Func<SpeicherOptimierungVorgaben>? OptimierungVorgaben;
    public Func<bool>? FlottenProjektAktiv;
    public Func<SpeicherFlottenErgebnis, Task<string>>? FlottenProjektUebernehmen;
    public Func<Task<string>>? FlottenProjektDeaktivieren;
    public Func<SpeicherOptimierungEingaben, Action<double?, string>,
                Task<SpeicherFlottenErgebnis>>? OptimierungFlottenRechnen;

    /// <summary>
    /// Rechnet die Rastersuche im Hintergrund; <paramref name="melder"/> bekommt
    /// Anteil und Text. OHNE Delegat gibt es die Optimierung gar nicht — die
    /// Rastersuche braucht einen gelaufenen Simulationsdurchgang.
    /// </summary>
    public Func<SpeicherOptimierungEingaben, Action<double?, string>,
                Task<SpeicherOptimierungErgebnis>>? OptimierungRechnen;

    /// <summary>Bricht einen laufenden Suchlauf ab; ohne Delegat kein Knopf.</summary>
    public Action? OptimierungAbbrechen;

    /// <summary>
    /// Uebernimmt Kapazitaet [kWh] und Leistung [kW] des Bestpunkts in die
    /// Geraetedaten. Neu gerechnet wird bewusst nicht.
    /// </summary>
    public Func<double, double, Rueckmeldung>? OptimierungUebernehmen;

    /// <summary>Schreibt den uebergebenen CSV-Text in eine Datei; ohne Delegat kein Knopf.</summary>
    public Func<string, Task<Rueckmeldung>>? OptimierungCsv;

    /// <summary>Speichert den aktuellen Auslegungsauftrag unter dem reservierten Standnamen.</summary>
    public Func<SpeicherOptimierungEingaben, Task<string>>? OptimierungEinstellungenSpeichern;

    /// <summary>Speichert ein benanntes Auslegungsprofil und liefert die frisch gelesenen Vorgaben.</summary>
    public Func<SpeicherOptimierungEingaben, string,
                Task<SpeicherOptimierungVorgaben>>? OptimierungProfilSpeichern;

    /// <summary>Waehlt und liest eine CSV-Datei in der Plattformhuelle.</summary>
    public Func<Task<SpeicherImportDatei>>? OptimierungDateiWaehlen;

    /// <summary>
    /// Zeichnet das Bild „Lastgang und Speicherbetrieb" des Bestpunkts NEU
    /// (Befund W11b-B-25, Windows-Abnahme 09.09.2026).
    /// </summary>
    /// <remarks>
    /// Die drei Angaben sind die drei Schalterstellungen des Bildes: ganzes Jahr
    /// statt der Woche um die Jahresspitze, die gewaehlten Reihen
    /// (<c>SpeicherOptimierungCtrl.REIHE_*</c>; leer = alle) und der Datenzoom
    /// (W11b-B-24). Neu gerechnet wird dabei EIN Jahreslauf des Bestpunkts, nicht
    /// das Raster. OHNE Delegat zeigt der Dialog das Bild des Laufs und bietet
    /// keine Umschalter an.
    /// </remarks>
    public Func<bool, IReadOnlyList<string>, Diagrammbereich?,
                SpeicherOptimierungBetriebsbild>? OptimierungBetrieb;

    /// <summary>
    /// Schreibt den Leistungspreis L_P [EUR/(kW*a)] SOFORT in die aktive
    /// Speichervariante (Anwenderentscheid W11b-E-3, 10.09.2026).
    /// </summary>
    /// <remarks>
    /// Es ist dasselbe Feld <c>Tab_StromspeicherVariante.L_P</c>, das der Reiter
    /// „Parameter" und die Peak-Shaving-Maske pflegen - EINE Pflegestelle, kein
    /// zweiter Wert daneben. OHNE Delegat zeigt der Dialog das Feld nur an und
    /// schreibt nichts („Kein Delegat ist kein Knopf").
    /// </remarks>
    public Action<double>? OptimierungLeistungspreis;

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
/// Die Feldschluessel der Speicherparameter — sprachneutral und ASCII
/// (Drei-Schichten-Regel). Sie benennen das Feld, das
/// <see cref="SimulationErgebnisDienste.SpeicherfeldSchreiben"/> setzt.
///
/// <para><b>Zwei Ziele.</b> Die meisten Schluessel benennen ein Feld der aktiven
/// VARIANTE (<c>Tab_StromspeicherVariante</c>); <see cref="Kapazitaet"/> und
/// <see cref="Leistung"/> dagegen die Groesse der ANLAGE
/// (<c>Tab_Stromspeicher</c>, W11b‑B‑29). Der Unterschied ist keine Feinheit: Die
/// Geraetegroesse gehoert allen Varianten desselben Speichers gemeinsam, und die
/// Huelle schreibt sie deshalb ueber denselben Weg wie die Auslegungsoptimierung
/// (<c>StromspeicherSimCtrl.UebernehmeAuslegung</c>) samt seiner Wache "genau eine
/// SP-Anlage im Projekt".</para>
/// </summary>
public static class SpeicherFeld
{
    public const string SoCMin = "SOC_MIN";
    public const string SoCMax = "SOC_MAX";
    public const string Ladeschwelle = "LADESCHWELLE";

    /// <summary>Nennkapazitaet [kWh] — Geraetedatum der ANLAGE, nicht der Variante.</summary>
    public const string Kapazitaet = "KAPAZITAET";

    /// <summary>Lade-/Entladeleistung [kW] — Geraetedatum der ANLAGE, nicht der Variante.</summary>
    public const string Leistung = "LEISTUNG";

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
