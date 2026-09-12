namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Der Feldsatz EINES Gebäude-Katalogsatzes (iU9-W9.1) — das plattformfreie Abbild von
/// <c>Tab_Gebaeude_STAMM</c>, wie ihn die beiden abgelösten Masken
/// <c>Form_Gebaeude1</c> (37 Kartenzeilen) und <c>Form_Gebaeude2</c> (41) zusammen
/// bearbeiteten.
///
/// <para><b>Ein Satz, zwei Reiter.</b> Der Vorläufer verteilte ihn auf zwei Fenster; das
/// zweite bekam mit <c>frm.model = model</c> DASSELBE Objekt und schrieb hinein. Genau
/// das bleibt: EIN Feldsatz, zwei Reiter darauf.</para>
///
/// <para><b>Die Zahlen sind <c>double?</c></b>, weil ein leeres Feld etwas anderes ist
/// als eine 0. Die 17 Felder des ersten Reiters sind PFLICHT
/// (<c>InitModelFromControls</c>:154-172 ruft <c>ZahlPruefen</c> ohne
/// <c>leerErlaubt</c>); die 28 des zweiten dürfen leer bleiben und zählen dann als 0
/// (<c>Text2Wert</c>:109).</para>
///
/// <para><b>Die abgeleiteten Größen stehen bis auf eine NICHT hier.</b>
/// <c>Bewohner</c>, <c>gesamte_Fensterflaeche</c> und <c>Wohnflaeche</c> rechnet die
/// Hülle beim Schreiben aus (wie <c>InitModelFromControls</c>). Die <c>Bauweise</c>
/// steht seit dem Entscheid des Anwenders vom 04.09.2026 (W9‑O‑2) hier: Sie hängt
/// jetzt an der BAUART-Klappliste, und die bedient der Dialog — siehe
/// <see cref="Bauweise"/>. Die vier Flags
/// <c>Wochenende</c>, <c>Ferien</c>, <c>WW_Bedarf</c> und der gehobene
/// Winterferienbeginn entstehen beim Übernehmen des zweiten Reiters (wie
/// <c>btn_Speichern_Click</c>).</para>
/// </summary>
public sealed class GebaeudeKatalogDaten
{
    // ------------------------------------------------------------------ Kopf

    /// <summary>Der Bezeichner des Katalogsatzes (<c>Tab_Gebaeude_STAMM.Bezeichner</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Gebäudetyp aus <c>Abfrage_Gebaeudetypen</c>.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Freitext.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Die Gebäudeart aus <c>Abfrage_Gebaeudearten</c>.</summary>
    public string Gebaeudeart { get; set; } = "";

    /// <summary>
    /// Die Verwendung — <b>Steuerwert</b> „Wohngebaeude" bzw. „Nicht Wohngebaeude"
    /// (Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>), nie ein Anzeigetext.
    /// </summary>
    public string Verwendung { get; set; } = "Wohngebaeude";

    /// <summary>Index der Baualtersklasse (0 = 'A' … 20 = 'U').</summary>
    public int Baualtersklasse { get; set; }

    /// <summary>Index der Bauart (0 = leicht, 1 = schwer, 2 = sehr schwer).</summary>
    public int Bauart { get; set; } = 1;

    /// <summary>
    /// Die gespeicherte <c>Bauweise</c> (<c>Tab_Gebaeude_STAMM.Bauweise</c>) —
    /// Wohnfläche × 20 / 50 / 100.
    ///
    /// <para><b>Entscheid des Anwenders vom 04.09.2026 zu W9‑O‑2 (Befund W9‑B6).</b>
    /// Der Vorläufer bildete sie aus dem Index der GEBÄUDEART-Klappliste, obwohl er die
    /// Bauart aus derselben Größe abgeleitet ANZEIGTE. Seither bestimmt die
    /// <b>Bauart</b>-Klappliste die Bauweise: Der Dialog führt sie bei jeder Bauartwahl
    /// und unmittelbar vor jedem Schreiben nach
    /// (<c>Gebaeudebauweise.BauweiseAusBauart</c>) — die Anzeige ist damit zum ersten
    /// Mal auch die Eingabe. Beim Laden geht der Rundweg zurück: die Bauart kommt aus
    /// dieser Größe (<c>Gebaeudebauweise.BauartAusBauweise</c>).</para>
    /// </summary>
    public double Bauweise { get; set; }

    // ------------------------------------------------------- Kenngrößen (5)

    public double? WohnflaecheGesamt { get; set; }
    public double? FlaecheNutzer { get; set; }
    public double? Waermegewinne { get; set; }
    public double? Fensterdurchlassgrad { get; set; }
    public double? Raumhoehe { get; set; }

    // ---------------------------------------------------------- Flächen (7)

    public double? FensterflaecheNord { get; set; }
    public double? FensterflaecheSued { get; set; }
    public double? FensterflaecheOstWest { get; set; }
    public double? FlaecheAussenwand { get; set; }
    public double? Dachflaeche { get; set; }
    public double? Grundflaeche { get; set; }
    public double? SonstigeFlaechen { get; set; }

    // ---------------------------------------------------------- U-Werte (5)

    public double? UWertAussenwand { get; set; }
    public double? UWertFenster { get; set; }
    public double? UWertDachflaeche { get; set; }
    public double? UWertGrundflaeche { get; set; }
    public double? UWertSonstiges { get; set; }

    // ------------------------------------------- Reiter 2: Raumtemperaturen

    public double? SollTag { get; set; }
    public double? NachtAbsenkung { get; set; }
    public double? MaxTemperatur { get; set; }
    public double? WochenendAbsenkung { get; set; }
    public double? SollFerien { get; set; }

    // ------------------------------------------ Reiter 2: Wärmebrücken (3)

    public double? WbvkFensterWand { get; set; }
    public double? WbvkAussenwandKeller { get; set; }
    public double? WbvkWandDach { get; set; }

    // ---------------------------------------- Reiter 2: Anschlussmaße (3)

    public double? AnschlussFensterWand { get; set; }
    public double? AnschlussWandDach { get; set; }
    public double? AnschlussAussenwandKeller { get; set; }

    // ------------------------------------------------ Reiter 2: Sonstiges

    public double? Luftwechselrate { get; set; }

    // --------------------------------------------------- Reiter 2: Ferien

    /// <summary>
    /// Die vier Ferienbeginne als JAHRESTAG (Winter, Ostern, Sommer, Herbst);
    /// 0 und 366 heißen „keine Angabe".
    /// </summary>
    public int[] Ferienbeginn { get; set; } = new int[4];

    /// <summary>Die vier Ferienenden als Jahrestag.</summary>
    public int[] Ferienende { get; set; } = new int[4];

    // ------------------------------------------- abgeleitet, aus dem Bestand

    /// <summary>
    /// <c>Wochenende</c> (0/1) — 1, sobald eine Wochenendabsenkung eingetragen ist.
    /// Wird beim Übernehmen des zweiten Reiters gesetzt.
    /// </summary>
    public double Wochenende { get; set; }

    /// <summary>Dasselbe für <c>Ferien</c> (0/1).</summary>
    public double Ferien { get; set; }

    /// <summary>
    /// <c>WW_Bedarf</c> — der Vorläufer setzte ihn beim Speichern des zweiten Reiters
    /// bedingungslos auf 0 (<c>btn_Speichern_Click</c>:201).
    /// </summary>
    public double WwBedarf { get; set; }

    /// <summary>Der spezifische Wärmeverbrauch aus dem Bestand — unverändert übernommen.</summary>
    public double SpezWaermeverbrauch { get; set; }

    /// <summary>Der Wärmebedarf aus dem Bestand — unverändert übernommen.</summary>
    public double Waermebedarf { get; set; }
}
