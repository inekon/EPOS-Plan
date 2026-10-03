using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Seiten.Pufferspeicher;

// ====================================================================================
// DIE DATENSEITE DER PUFFERSPEICHER-AUSLEGUNG (Konzept Pufferspeicher-Auslegung, Abschnitt 6).
//
// Die Ansicht kennt weder Datenbank noch Fachklassen des Kerns: Sie bekommt einen
// Startstand (PufferAuslegungStartDaten) und einen Satz Wege (PufferAuslegungDienste),
// die in PufferAuslegungCtrl führen — gebaut von PufferAuslegungHuelle in EPOS.UI.Daten.
// Zeichenketten statt Aufzählungen des Kerns (Vorlage, Sperrprofil, Zirkulationsweg,
// Übergabeart): Es sind die Spaltenwerte von Tab_PufferAuslegung, sprachneutral.
// ====================================================================================

/// <summary>Die vier Schritte der Ablaufleiste (Konzept 6) — sprachneutrale Schlüssel.</summary>
public static class PufferAuslegungSchritt
{
    /// <summary>Schritt 1 — Speicherklasse, Vorlage, Erzeuger, Nutzungsprofil.</summary>
    public const string Anlage = "ANLAGE";

    /// <summary>Schritt 2 — Bedarfsreihen und Randbedingungen.</summary>
    public const string Bedarf = "BEDARF";

    /// <summary>Schritt 3 — die Kriterienkarten.</summary>
    public const string Kriterien = "KRITERIEN";

    /// <summary>Schritt 4 — Zonen, Empfehlung, Kennzahlen, Hinweise, Übernahme.</summary>
    public const string Ergebnis = "ERGEBNIS";

    /// <summary>Alle Schritte in ihrer Reihenfolge.</summary>
    public static readonly IReadOnlyList<string> Alle = new[] { Anlage, Bedarf, Kriterien, Ergebnis };
}

/// <summary>
/// Die Tiefe der Ansicht (Konzept 6): <b>Schnell</b> zeigt die Pflichtangaben und nur die
/// Kriterien, die die Vorlage einschaltet; <b>Standard</b> alle Kriterienkarten samt
/// Rechenweg und die Ziele; <b>Experte</b> dazu die Expertenfelder (Expertenweg der Sperrzeit,
/// kleinste Dauerleistung, eigenes Sperrfenster, Brauchwasser-Temperaturen, Zirkulation).
/// Die Stufe blendet Tiefe ein, nie Pflichtangaben aus.
/// </summary>
public enum PufferAuslegungStufe
{
    Schnell,
    Standard,
    Experte
}

/// <summary>Die Spaltenwerte von <c>Tab_PufferAuslegung.Sperrprofil</c>.</summary>
public static class PufferSperrprofilWert
{
    public const string Keine = "KEINE";
    public const string ZweiMalZwei = "ZWEI_MAL_ZWEI";
    public const string DreiMalZwei = "DREI_MAL_ZWEI";
    public const string Eigen = "EIGEN";

    /// <summary>Alle Profile in der Reihenfolge der Wahl.</summary>
    public static readonly IReadOnlyList<string> Alle = new[] { Keine, ZweiMalZwei, DreiMalZwei, Eigen };
}

/// <summary>
/// Der ARBEITSSTAND der Ansicht — die Eingaben, aus denen die Hülle den Eingang des Kerns
/// baut. <c>null</c> in einem nullbaren Feld heißt „Vorgabe des Kerns".
/// </summary>
public sealed class PufferAuslegungEingabeDaten
{
    // ---- Schritt 1 ----
    public bool KlasseHeizung { get; set; }
    public bool KlasseBrauchwasser { get; set; }
    public bool KlasseProzess { get; set; }

    /// <summary>Die Vorlage (<c>WP_MONO</c>, <c>WP_BIVALENT</c>, <c>BHKW</c>, <c>KESSEL</c>, <c>FESTBRENNSTOFF</c>, <c>SOLAR</c>, <c>PROZESS</c>).</summary>
    public string Vorlage { get; set; } = "WP_MONO";

    /// <summary>Wärmepumpe leistungsgeregelt statt Fixed-Speed.</summary>
    public bool WpGeregelt { get; set; }

    /// <summary>Der Zweiterzeuger ist in der Sperre frei (sonst: Heizstab, mitgesperrt).</summary>
    public bool ZweiterzeugerFrei { get; set; }

    // ---- Schritt 2 ----
    /// <summary>Übergabeart (<c>RADIATOR</c>, <c>FLAECHE</c>, <c>KONVEKTOR</c>, <c>LUEFTER</c>); leer = keine Angabe.</summary>
    public string Uebergabeart { get; set; } = "";
    public double? HeizgrenzeC { get; set; }
    public double? AuslegungsheizlastKw { get; set; }
    public double? AnlagenvolumenL { get; set; }

    /// <summary>Das Sperrprofil (<see cref="PufferSperrprofilWert"/>).</summary>
    public string Sperrprofil { get; set; } = PufferSperrprofilWert.Keine;
    public double? SperrbeginnH { get; set; }
    public double? SperrdauerH { get; set; }

    /// <summary>Expertenweg der Sperrzeit (K4e aus dem Lastgang) bemisst statt Gleichung 23.</summary>
    public bool SperrzeitExpertenweg { get; set; }

    public double? MindestlaufzeitMin { get; set; }
    public double? MindestleistungKw { get; set; }
    public double? StartzielJeTag { get; set; }

    /// <summary>Ziel-Deckungsgrad in Prozent (der Kern rechnet mit dem Anteil).</summary>
    public double? DeckungszielProzent { get; set; }

    // ---- Brauchwasserzone (Experte) ----
    public double? DeltaTBK { get; set; }
    public double? TPufferObenC { get; set; }

    /// <summary>Weg des Zirkulationszuschlags (<c>ZAPFPROFIL</c>, <c>PROJEKT</c>, <c>ANTEIL</c>, <c>JE_WE</c>); leer = automatisch.</summary>
    public string ZirkulationWeg { get; set; } = "";
    public double? Wohneinheiten { get; set; }

    // ---- Schritt 3 ----
    /// <summary>
    /// Die Kriterienschalter, die von der Vorlage ABWEICHEN (Kennung → an/aus). Leer = die
    /// Vorlage gilt; ein Vorlagenwechsel leert sie.
    /// </summary>
    public Dictionary<string, bool> Kriterien { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Eine tiefe Kopie — der Arbeitsstand ist entkoppelt vom Startstand.</summary>
    public PufferAuslegungEingabeDaten Kopie()
    {
        PufferAuslegungEingabeDaten k = (PufferAuslegungEingabeDaten)MemberwiseClone();
        k.Kriterien = new Dictionary<string, bool>(Kriterien, StringComparer.Ordinal);
        return k;
    }
}

/// <summary>Eine Zeile der Herkunft: Feld, Quelle als Marke (übersetzt) und Klartext des Kerns.</summary>
public sealed record PufferHerkunftDaten(string Feld, string Quelle, string Marke, string Text);

/// <summary>Eine Vorlagenkarte: Schlüssel, Titel, Untertitel, Beschreibung, Kriterienschalter, Faustwert.</summary>
public sealed record PufferVorlageDaten(string Schluessel, string Titel, string Untertitel, string Beschreibung,
                                        IReadOnlyDictionary<string, bool> Kriterien, string Faustwert);

/// <summary>Eine Bedarfsreihe als Kennzahl: Kanal, Spitze [kW], Jahressumme [kWh] (Quelleneinheit).</summary>
public sealed record PufferReiheDaten(string Kanal, double SpitzeKw, double JahrKwh);

/// <summary>Eine Wahl der Ansicht: sprachneutraler Wert und Anzeigetext.</summary>
public sealed record PufferWahlDaten(string Wert, string Text);

/// <summary>
/// Der STARTSTAND der Ansicht: der vorbelegte Arbeitsstand samt Herkunft, die Vorlagen, die
/// Bedarfsreihen als Kennzahlen, Erzeuger, Puffer, Zapfprofil und das erste Ergebnis.
/// </summary>
public sealed class PufferAuslegungStartDaten
{
    public int IdProjekt { get; set; }

    /// <summary>Der Projektpuffer, auf den die Auslegung zielt; <c>null</c> = ein neuer.</summary>
    public int? IdPuffer { get; set; }

    public string Projektname { get; set; } = "";
    public string PufferBezeichner { get; set; } = "";

    /// <summary>Woher die Ansicht kam (Einstieg) — eine Zeile im Kopf; leer = keine.</summary>
    public string Einstieg { get; set; } = "";

    /// <summary>Ein Fehler beim Vorbelegen — benannt; die Ansicht zeigt ihn statt der Schritte.</summary>
    public string Fehler { get; set; } = "";

    public PufferAuslegungEingabeDaten Eingabe { get; set; } = new();
    public IReadOnlyList<PufferHerkunftDaten> Herkunft { get; set; } = Array.Empty<PufferHerkunftDaten>();

    /// <summary>Das abgeleitete Nutzungsprofil (Anzeige) und seine Herkunft.</summary>
    public string Nutzungsprofil { get; set; } = "";
    public string NutzungsprofilHerkunft { get; set; } = "";
    public bool NutzungsprofilVorgabe { get; set; }

    public IReadOnlyList<PufferVorlageDaten> Vorlagen { get; set; } = Array.Empty<PufferVorlageDaten>();
    public IReadOnlyList<PufferWahlDaten> Uebergabearten { get; set; } = Array.Empty<PufferWahlDaten>();
    public IReadOnlyList<PufferWahlDaten> Zirkulationswege { get; set; } = Array.Empty<PufferWahlDaten>();

    /// <summary>Die Bedarfsreihen als Kennzahlen; leer, wenn sie nicht rechenbar sind.</summary>
    public IReadOnlyList<PufferReiheDaten> Reihen { get; set; } = Array.Empty<PufferReiheDaten>();

    /// <summary>Warum die Bedarfsreihen fehlen (benannt weitergereicht); leer = sie liegen vor.</summary>
    public string ReihenFehler { get; set; } = "";

    // ---- Erzeuger ----
    /// <summary>Die Zeilen des Erzeugers (Rang, Leistung, Zweiterzeuger) als Klartext.</summary>
    public IReadOnlyList<string> Erzeuger { get; set; } = Array.Empty<string>();
    public bool IstWaermepumpe { get; set; }

    // ---- Puffer ----
    public double VorlaufC { get; set; }
    public double RuecklaufC { get; set; }
    /// <summary>Schwellen als Anteil 0 … 1 (Vorgabe, wenn der Puffer keine führt).</summary>
    public double SchwelleEin { get; set; }
    public double SchwelleAus { get; set; }

    // ---- Zapfprofil ----
    public bool ZapfVorhanden { get; set; }
    public string ZapfTopologie { get; set; } = "";
    public double? ZapfDmaxKwh { get; set; }
    public double? ZapfPersonen { get; set; }
    public double? ZapfTagesbedarfL { get; set; }

    // ---- Übernahme ----
    /// <summary>Der Vorschlag für die Bezeichnung eines neuen Speichers (Formatzeichenfolge mit {0} = Liter).</summary>
    public string BezeichnerMuster { get; set; } = "";

    /// <summary>Hat eine gespeicherte Auslegung die Vorbelegung überschrieben?</summary>
    public bool Gespeichert { get; set; }

    /// <summary>Das erste Ergebnis; <c>null</c> = noch nicht gerechnet.</summary>
    public PufferAuslegungErgebnisDaten? Ergebnis { get; set; }

    /// <summary>Der letzte Probelauf dieser Sitzung für Projekt und Puffer; <c>null</c> = kein Lauf.</summary>
    public PufferProbelaufDaten? Probelauf { get; set; }

    /// <summary>Die Marke der Herkunft eines Felds (letzter Eintrag gewinnt); leer = keine.</summary>
    public PufferHerkunftDaten? HerkunftVon(string feld)
    {
        PufferHerkunftDaten? h = null;
        foreach (PufferHerkunftDaten z in Herkunft) if (z.Feld == feld) h = z;
        return h;
    }

    /// <summary>Alle Herkunftszeilen eines Felds.</summary>
    public IEnumerable<PufferHerkunftDaten> HerkunftAlle(string feld) => Herkunft.Where(h => h.Feld == feld);
}

/// <summary>Der Zustand eines Ergebnisses — nie ein vorbelegtes DTO als Ergebnis (Hausregel).</summary>
public enum PufferErgebnisZustand
{
    NichtGerechnet,
    Gerechnet,
    Fehler
}

/// <summary>Ein Kriterium einer Zone (Konzept 3.3).</summary>
public sealed record PufferKriteriumDaten(string Kennung, string Name, double? VolumenL, bool Aktiv, bool Gueltig,
                                          string Herkunft, string Rechenweg);

/// <summary>Das Betriebsbild einer Zone (Zweipunktsimulation D2).</summary>
public sealed record PufferBetriebsbildDaten(double VolumenL, double StartsJeTag, int StartsHeizperiode, int Heizstunden,
                                             double Deckungsgrad, double MittlereLaufzeitH);

/// <summary>Das Ergebnis einer Zone.</summary>
public sealed record PufferZoneDaten(string Zone, string Name, string Bemessend, string BemessendName, double VolumenL,
                                     bool KeinPuffer, IReadOnlyList<PufferKriteriumDaten> Kriterien,
                                     PufferBetriebsbildDaten? Betriebsbild);

/// <summary>Eine Zeile der Warnliste: Code, Stufe, Text (Ressource), Klartext des Kerns, Herkunft, Zone.</summary>
public sealed record PufferWarnungDaten(string Code, bool Warnung, string Text, string Klartext, string Herkunft, string Zone);

/// <summary>Die Kennzahlen der Empfehlung (Konzept 3.3–3.5).</summary>
public sealed class PufferKennzahlDaten
{
    public double Nutzanteil { get; set; }
    public double SpreizungK { get; set; }
    public double AuslegungsheizlastKw { get; set; }
    public double AnlagenvolumenL { get; set; }
    public double? BandMinL { get; set; }
    public double? BandMaxL { get; set; }
    public double? BandWpMinL { get; set; }
    public double? BandWpMaxL { get; set; }
    public double KapazitaetKwh { get; set; }
    public double? VerlustKwhJeTag { get; set; }
    public double? VerlustWJeK { get; set; }
    public double? VerlustKwhJeJahr { get; set; }
    public bool VerlustAusKatalog { get; set; }
    public bool VerlustExtrapoliert { get; set; }
    public double? StartsJeTag { get; set; }
    public int? StartsHeizperiode { get; set; }
    public double? LiterJePerson { get; set; }
    public double? ZonenanteilHeizung { get; set; }
    public int SchichtenMindest { get; set; } = 1;
    public double? FaustwertL { get; set; }
}

/// <summary>Das Ergebnis der Auslegung als Anzeige.</summary>
public sealed class PufferAuslegungErgebnisDaten
{
    public PufferErgebnisZustand Zustand { get; set; } = PufferErgebnisZustand.NichtGerechnet;

    /// <summary>Der Grund bei <see cref="PufferErgebnisZustand.Fehler"/> — benannt.</summary>
    public string Grund { get; set; } = "";

    public IReadOnlyList<PufferZoneDaten> Zonen { get; set; } = Array.Empty<PufferZoneDaten>();
    public double SummeL { get; set; }
    public double EmpfehlungL { get; set; }
    public bool UeberListenende { get; set; }
    public bool AnPraxisgrenze { get; set; }

    /// <summary>„Heizzone: Sperrzeit" — Zone und Kriterium, die die größte Zone bemessen; leer = keines.</summary>
    public string Bemessend { get; set; } = "";

    /// <summary>Der Katalogvorschlag als Anzeige (Bezeichnung · Volumen); leer = keiner.</summary>
    public string Katalogvorschlag { get; set; } = "";

    public PufferKennzahlDaten Kennzahlen { get; set; } = new();
    public IReadOnlyList<PufferWarnungDaten> Warnungen { get; set; } = Array.Empty<PufferWarnungDaten>();

    /// <summary>Das Kriterium mit der Kennung aus der ersten Zone, die es führt; <c>null</c> = keine.</summary>
    public PufferKriteriumDaten? Kriterium(string kennung)
    {
        foreach (PufferZoneDaten z in Zonen)
            foreach (PufferKriteriumDaten k in z.Kriterien)
                if (k.Kennung == kennung) return k;
        return null;
    }

    /// <summary>Bemisst das Kriterium eine Zone?</summary>
    public bool Bemisst(string kennung) => Zonen.Any(z => z.Bemessend == kennung);

    /// <summary>Ein Ergebnis im Zustand <see cref="PufferErgebnisZustand.Fehler"/>.</summary>
    public static PufferAuslegungErgebnisDaten MitFehler(string grund)
        => new() { Zustand = PufferErgebnisZustand.Fehler, Grund = grund ?? "" };
}

/// <summary>Was übernommen werden soll: neuer Speicher oder der gewählte, mit Bezeichnung.</summary>
public sealed record PufferUebernahmeDaten(bool Neu, string Bezeichner, bool SperrprofilSchreiben = false);

/// <summary>Die Antwort der Übernahme: Erfolg, Text der Statuszeile, Puffer-ID (bei Erfolg).</summary>
public sealed record PufferUebernahmeErgebnis(bool Erfolg, string Text, int IdPuffer);

/// <summary>
/// Die WEGE der Ansicht — jeder führt in <c>PufferAuslegungCtrl</c>. <b>Kein Delegat ist kein
/// Knopf:</b> Ohne <see cref="Speichern"/> oder <see cref="Uebernehmen"/> fehlt der Knopf.
/// </summary>
/// <param name="Rechnen">Rechnet den Arbeitsstand (ohne Datenbank, Reihen in der Hülle).</param>
/// <param name="Speichern">Speichert Eingaben und Ergebnis; <c>null</c> = gespeichert, sonst der Grund.</param>
/// <param name="Uebernehmen">Speichert und übernimmt die Empfehlung in den Projektpuffer.</param>
/// <param name="Probelauf">Rechnet das Projekt einmal mit der Empfehlung (Jahressimulation, nichts wird gespeichert).</param>
public sealed record PufferAuslegungDienste(
    Func<PufferAuslegungEingabeDaten, PufferAuslegungErgebnisDaten> Rechnen,
    Func<PufferAuslegungEingabeDaten, string?>? Speichern = null,
    Func<PufferAuslegungEingabeDaten, PufferUebernahmeDaten, PufferUebernahmeErgebnis>? Uebernehmen = null,
    Func<PufferAuslegungEingabeDaten, System.Threading.Tasks.Task<PufferProbelaufDaten>>? Probelauf = null);

/// <summary>Die Starts eines Erzeugertyps im Probelauf (Name aus den Ressourcen).</summary>
/// <param name="AusReihe">Der Lauf zählt keine Starts (Gerät ohne Mindestleistung); gezählt sind die Einschaltflanken.</param>
public sealed record PufferProbelaufStartsDaten(string Erzeuger, int StartsJahr, int StartsHeizperiode, double StartsJeTag,
                                                bool Rang1, bool AusReihe = false);

/// <summary>Füllstand des Puffers in einem Monat (Anteil 0 … 1).</summary>
public sealed record PufferFuellstandMonatDaten(int Monat, double Min, double Mittel, double Max);

/// <summary>
/// Der PROBELAUF der Jahressimulation mit der Empfehlung (Welle P4b): Starts, Deckung, Füllstand und
/// Dauer — nur Anzeige, nichts davon wird gespeichert oder übernommen.
/// </summary>
public sealed class PufferProbelaufDaten
{
    public bool Erfolg { get; set; }

    /// <summary>Der benannte Grund, wenn der Lauf nicht möglich war; leer bei Erfolg.</summary>
    public string Fehler { get; set; } = "";

    public DateTime Zeitpunkt { get; set; }
    public double DauerSekunden { get; set; }
    public double VolumenL { get; set; }
    public IReadOnlyList<PufferProbelaufStartsDaten> Starts { get; set; } = Array.Empty<PufferProbelaufStartsDaten>();

    /// <summary>Deckung des Wärmebedarfs (0 … 1); <c>null</c> ohne Bedarf.</summary>
    public double? Deckung { get; set; }

    public IReadOnlyList<PufferFuellstandMonatDaten> Monate { get; set; } = Array.Empty<PufferFuellstandMonatDaten>();

    /// <summary>Tag (1 … 365), an dem die kälteste Woche beginnt; 0 = ohne Reihe.</summary>
    public int KaeltesteWocheTag { get; set; }
    public double? WocheMin { get; set; }
    public double? WocheMittel { get; set; }
    public double? WocheMax { get; set; }

    /// <summary>Starts je Tag der Auslegung (D2), gegen die der Lauf gehalten wurde; <c>null</c> = keine Schätzung.</summary>
    public double? AuslegungStartsJeTag { get; set; }

    /// <summary>Hinweis <c>PA-STARTS-ABWEICHUNG</c>: Starts je Tag weichen um mehr als 30 % ab.</summary>
    public bool Abweichung { get; set; }

    /// <summary>Der Hinweistext der Abweichung (Ressource, mit Zahlen); leer ohne Abweichung.</summary>
    public string AbweichungText { get; set; } = "";

    /// <summary>Die Starts des Erzeugers an Rang 1; <c>null</c> = keiner im Lauf.</summary>
    public PufferProbelaufStartsDaten? Rang1 => Starts.FirstOrDefault(s => s.Rang1);

    public static PufferProbelaufDaten MitFehler(string grund) => new() { Erfolg = false, Fehler = grund ?? "" };
}
