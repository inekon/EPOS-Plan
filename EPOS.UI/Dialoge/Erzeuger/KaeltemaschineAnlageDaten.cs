using System;
using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Eine Kältemaschinen-Anlage des Projekts</b> im Erzeugerdialog (KU3-4c) — der Feldsatz, in den Maske und
/// Hilfe-Assistent schreiben. Die Hülle (<c>KaeltemaschineAnlageHuelle</c>) bildet ihn auf
/// <c>KaeltemaschineAnlageModel</c> des Kerns ab; geschrieben wird erst mit „OK".
/// </summary>
public sealed class KaeltemaschineAnlageDaten
{
    /// <summary><c>Tab_Energieanlagen.ID</c>; 0 = im Dialog gewählt, beim OK angelegt.</summary>
    public int AnlagenId { get; set; }

    /// <summary>Der Katalogsatz einer NEUEN Anlage (<c>Tab_Kaeltemaschine_STAMM.ID</c>); 0 bei einer gespeicherten.</summary>
    public int StammId { get; set; }

    /// <summary>Die Projektkopie (<c>ID_Kaeltemaschine</c>); <c>null</c> bei einer neuen oder nach dem Löschen des Geräts.</summary>
    public int? GeraetId { get; set; }

    /// <summary>Anzeigename der Anlage.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Anzahl gleicher Maschinen; <c>null</c> = leeres Feld (die Prüfung lehnt es ab).</summary>
    public int? Anzahl { get; set; } = 1;

    /// <summary>Kaltwasservorlauf [°C]; <c>null</c> = kleinste Stützstelle der Kennlinie.</summary>
    public double? KuehlVorlauf { get; set; }

    /// <summary>Hilfsstromanteil [0…1); <c>null</c> = kein Zuschlag.</summary>
    public double? KuehlHilfsstromanteil { get; set; }

    /// <summary>Kühlträger (<c>Kuehl_ID_Carrier</c>); <c>null</c> = Stromträger des Projekts.</summary>
    public int? KuehlCarrierId { get; set; }

    /// <summary>Abrechnung über einen eigenen Zähler — wirkt nur bei abweichendem Kühlträger.</summary>
    public bool KuehlEigenerZaehler { get; set; }

    /// <summary>Die Kennwerte des Geräts zum Lesen; <c>null</c> = keine bekannt.</summary>
    public KaeltemaschineGeraetwerte? Geraet { get; set; }

    /// <summary>
    /// KM4-E3-b: die Lesezeile „Zulässigkeit“ des Kältemittels dieser Anlage mit dem Inbetriebnahmejahr des Projekts,
    /// fertig von der Hülle (Kern: <c>KaeltemittelZulaessigkeitszeile</c>); leer = keine Zeile (neue Anlage). Nur
    /// lesend, keine Eingabe — sie zählt nicht zu <see cref="GleicheEingaben"/>.
    /// </summary>
    public string Zulaessigkeit { get; set; } = "";

    /// <summary>
    /// K-F5b: die Wahl des Rückkühlwerks — <c>null</c> = keins (Festwerte der Rückkühlart), &gt; 0 = Katalogsatz
    /// (<c>Tab_Rueckkuehlwerk_STAMM.ID</c>), aus dem die Anlage beim OK eine eigene Projektkopie bekommt,
    /// <see cref="RUECKKUEHLWERK_PROJEKTKOPIE"/> = die gespeicherte Projektkopie ohne Katalogsatz (bleibt, wie sie ist).
    /// Geschrieben wird erst im OK-Weg über <c>KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen</c>.
    /// </summary>
    public int? RueckkuehlwerkWahl { get; set; }

    /// <summary>
    /// K-F5b: die Lesezeilen der GESPEICHERTEN Projektkopie des Rückkühlwerks, fertig von der Hülle; <c>null</c> = keins
    /// gewählt. Nur lesend — zählt nicht zu <see cref="GleicheEingaben"/>.
    /// </summary>
    public RueckkuehlwerkLesung? Rueckkuehlwerk { get; set; }

    /// <summary>Der Wahlwert der gespeicherten Projektkopie, deren Katalogsatz es nicht (mehr) gibt.</summary>
    public const int RUECKKUEHLWERK_PROJEKTKOPIE = -1;

    /// <summary>Eine unabhängige Kopie (die Kennwerte werden geteilt, sie sind nur zu lesen).</summary>
    public KaeltemaschineAnlageDaten Kopie() => (KaeltemaschineAnlageDaten)MemberwiseClone();

    /// <summary>Tragen beide Sätze dieselben Eingaben?</summary>
    public bool GleicheEingaben(KaeltemaschineAnlageDaten? b)
        => b is not null && Bezeichner == b.Bezeichner && Anzahl == b.Anzahl && KuehlVorlauf == b.KuehlVorlauf
           && KuehlHilfsstromanteil == b.KuehlHilfsstromanteil && KuehlCarrierId == b.KuehlCarrierId
           && KuehlEigenerZaehler == b.KuehlEigenerZaehler && RueckkuehlwerkWahl == b.RueckkuehlwerkWahl;
}

/// <summary>
/// <b>Die Lesezeilen eines Rückkühlwerks</b> (K-F5b) — Katalogsatz oder Projektkopie, fertig für die Gruppe
/// „Rückkühlung“ der <c>KaeltemaschineKonfiguration</c>: Bauart, Nennleistung, Annäherung samt Weg, Ventilator samt
/// Regelung, Schaltung der freien Kühlung, je mit „Vorgabe: …“ bei leeren Feldern. Die Einordnung (Verdunstungs-
/// kühlanlage, Ventilator gepflegt) trägt die Hinweise; ein Steuerwert ist hier nie ein Anzeigetext.
/// </summary>
public sealed class RueckkuehlwerkLesung
{
    /// <summary>Name des Satzes.</summary>
    public string Bezeichner { get; init; } = "";

    /// <summary>Der Katalogsatz, aus dem eine Projektkopie stammt (am Katalogsatz seine eigene Id); <c>null</c> = keiner.</summary>
    public int? StammId { get; init; }

    /// <summary>Die Lesezeilen (Bezeichnung, Wert) in Anzeigefolge.</summary>
    public IReadOnlyList<(string Bezeichnung, string Wert)> Zeilen { get; init; } = Array.Empty<(string, string)>();

    /// <summary>Nasse oder befeuchtende Bauart — Verdunstungskühlanlage (42. BImSchV), braucht die Luftfeuchte.</summary>
    public bool Verdunstungskuehlanlage { get; init; }

    /// <summary>Gepflegte Ventilatorleistung [kW]; <c>null</c> = der Hilfsstrom der Rückkühlung an der Maschine gilt.</summary>
    public double? VentilatorKw { get; init; }
}

/// <summary>Ein Katalogsatz der Klappliste „Rückkühlwerk“ (K-F5b): Id, Anzeigetext und seine Lesezeilen.</summary>
public sealed record RueckkuehlwerkEintrag(int Id, string Text, RueckkuehlwerkLesung Lesung);

/// <summary>
/// <b>Die Gaben der Gruppe „Rückkühlung“</b> (K-F5b) — projektweit: die Katalogsätze der Rückkühlwerke und ob das Klima
/// des Projekts eine Luftfeuchte führt. Die Hülle baut sie aus dem Kern; <c>null</c> = keine Gruppe.
/// </summary>
public sealed class RueckkuehlungGaben
{
    /// <summary>Die Katalogsätze, nach Bezeichner.</summary>
    public IReadOnlyList<RueckkuehlwerkEintrag> Katalog { get; init; } = Array.Empty<RueckkuehlwerkEintrag>();

    /// <summary>Führt jede Stunde des Projektklimas eine Luftfeuchte? Sonst rechnet ein nasses Rückkühlwerk trocken.</summary>
    public bool KlimaMitFeuchte { get; init; }
}

/// <summary>
/// <b>Die Kennwerte eines Kältemaschinen-Geräts</b> — Katalogsatz oder Projektkopie, fertig für die Anzeige.
/// Die Rückkühlart kommt als Anzeigetext; sie ist hier nie ein Steuerwert.
/// </summary>
public sealed class KaeltemaschineGeraetwerte
{
    public string Bezeichner { get; init; } = "";
    public string Firma { get; init; } = "";
    public string Typ { get; init; } = "";
    public string Rueckkuehlart { get; init; } = "";
    public double? Nennkaelteleistung { get; init; }
    public double? NennEer { get; init; }
    public double? Mindestteillast { get; init; }
    public double? KaltwasserMin { get; init; }

    // KM3-E3-b (Fachkonzept Teillast und Takten 7.2): die Lesewerte der Teillastrechnung, fertige Texte.
    /// <summary>Teillastrechnung samt Herkunft der Kurve (wie bisher · linear · Kurve · Vorgabekurve · verworfen).</summary>
    public string TeillastWeg { get; init; } = "";
    /// <summary>Verdichterregelung; „keine Angabe“ ohne Pflege.</summary>
    public string Verdichterregelung { get; init; } = "";
    /// <summary>Taktverlustfaktor C_d; ohne Pflege „0,9 (Vorgabe)“.</summary>
    public string Taktverlustfaktor { get; init; } = "";
    /// <summary>Weg am Kennfeldrand; ohne Pflege „Randwert (Vorgabe)“.</summary>
    public string Kennfeldrand { get; init; } = "";
    /// <summary>Rechnet die Maschine Teillast und Takten (Weg gesetzt)?</summary>
    public bool TeillastGesetzt { get; init; }
    /// <summary>Die Kaltwassertemperaturen der Kennlinie, aufsteigend und ohne Doppel.</summary>
    public IReadOnlyList<double> Kaltwasserstuetzstellen { get; init; } = Array.Empty<double>();

    /// <summary>Die Saisonkennzahl der Projektkopie (K-A, <c>Saisonkennzahl</c>); <c>null</c> = keine.</summary>
    public double? Saisonkennzahl { get; init; }

    /// <summary>Die Art der Saisonkennzahl als Kürzel (etwa „SEER“); leer = keine.</summary>
    public string SaisonkennzahlArt { get; init; } = "";

    /// <summary>Die Geräteart als Anzeigetext (<c>KaeltemaschineStammCtrl.GeraeteartText</c>); leer = keine.</summary>
    public string Geraeteart { get; init; } = "";

    /// <summary>Kältemittel (Anzeigetext) — für den Gerätesteckbrief (KA‑E‑25).</summary>
    public string Kaeltemittel { get; init; } = "";

    /// <summary>GWP des Kältemittels [—]; beschreibend.</summary>
    public double? Gwp { get; init; }

    /// <summary>
    /// Welle KB-4: die Gerätekosten als Lesewert der Satzvorlage [€] (<c>KaeltemaschineModel.Geraetekosten</c>);
    /// <c>null</c> ohne Vorlage. Gepflegt werden sie in der Kostenverwaltung.
    /// </summary>
    public double? Geraetekosten { get; init; }

    /// <summary>
    /// K-F5b: Ist die Maschine luftgekühlt (Rückkühlart <c>LUFT</c> oder leer — so rechnet der Lauf)? Dann trägt sie
    /// ihren Verflüssiger selbst und bekommt kein Rückkühlwerk.
    /// </summary>
    public bool Luftgekuehlt { get; init; } = true;
}
