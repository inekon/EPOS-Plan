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
/// als eine 0. Seit Stufe G1 prüft der Dialog seine Pflichtzahlen und Regeln an EINER
/// Stelle im OK-Weg (Umsetzungskonzept Gebäudesimulation 2.4, Konzept 4.8); die früher
/// 17 Pflichtfelder des ersten Reiters waren PFLICHT
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
/// Winterferienbeginn entstehen seit G1 im OK-Weg des Dialogs (früher beim Übernehmen
/// des zweiten Reiters, wie
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
    /// Wird im OK-Weg des Dialogs gesetzt.
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

    // ------------------------------------ Stufe G1: VDI-6007-Struktur (Konzept 2.4)
    //
    // Alle nullbar: Der Dialog schreibt null, nicht die Vorgabe (Vorbild
    // PvModellFelder) - die Vorgabe setzt der Kern beim Rechnen ein.

    /// <summary>
    /// Der Rechenweg (<c>Gebaeude_Modell</c>): <c>DbWerte.GEBAEUDE_MODELL_*</c> oder
    /// <c>null</c> = keine Angabe, es gilt die Vorgabe des Programms
    /// (<c>Gebaeuderechenweg.OhneAngabe</c>).
    /// </summary>
    public string? Modell { get; set; }

    /// <summary>Randbedingung der Bodenplatte (<c>DbWerte.GRUND_*</c>); <c>null</c> = Erdreich.</summary>
    public string? GrundflaecheRandbedingung { get; set; }

    /// <summary>Kellertemperatur [°C] bei Randbedingung Keller; <c>null</c> = Vorgabe.</summary>
    public double? Kellertemperatur { get; set; }

    /// <summary>Fensterfläche Ost [m²]; <c>null</c> = die Hälfte des Bestandsfelds Ost + West.</summary>
    public double? FensterflaecheOst { get; set; }

    /// <summary>Fensterfläche West [m²]; <c>null</c> = die Hälfte des Bestandsfelds Ost + West.</summary>
    public double? FensterflaecheWest { get; set; }

    /// <summary>Rahmenanteil der Fenster [–]; <c>null</c> = Vorgabe.</summary>
    public double? Rahmenanteil { get; set; }

    /// <summary>Verschattungsfaktor [–]; <c>null</c> = Vorgabe.</summary>
    public double? Verschattungsfaktor { get; set; }

    /// <summary>Masseanteil außen [–]; <c>null</c> = Vorgabe.</summary>
    public double? MasseanteilAussen { get; set; }

    /// <summary>Innenflächenfaktor [–]; <c>null</c> = Vorgabe.</summary>
    public double? Innenflaechenfaktor { get; set; }

    /// <summary>Strahlungsanteil der Heizung [–]; <c>null</c> = Vorgabe.</summary>
    public double? HeizungStrahlungsanteil { get; set; }

    /// <summary>Heizleistungsgrenze [kW]; <c>null</c> = unbegrenzt.</summary>
    public double? HeizleistungMax { get; set; }

    /// <summary>Außenbauteile mit Strahlung (0/1-Spalte, <c>NOT NULL DEFAULT 0</c>).</summary>
    public bool AussenbauteileStrahlung { get; set; }

    /// <summary>Infiltration [1/h] (Stufe G2); <c>null</c> = Vorgabe bzw. Luftwechselrate.</summary>
    public double? LuftwechselInfiltration { get; set; }

    /// <summary>Nutzerlüftung [1/h] (Stufe G2); <c>null</c> = Vorgabe bzw. Luftwechselrate.</summary>
    public double? LuftwechselNutzer { get; set; }

    /// <summary>Sommerlüftungsregel (0/1-Spalte, <c>NOT NULL DEFAULT 0</c>, Stufe G2).</summary>
    public bool Sommerlueftung { get; set; }

    // ------------------------------------ Stufe KU1: Kühlung (Kühlkonzept 7.1, 8.1; KU-S1)
    //
    // Teil der VDI-Struktur und immer sichtbar (E20). NULL-erhaltend wie die Modellparameter:
    // Der Dialog schreibt null, nicht die Vorgabe.

    /// <summary>„Gebäude wird gekühlt" (<c>Kuehlung_Aktiv</c>, 0/1-Spalte, <c>NOT NULL DEFAULT 0</c>).</summary>
    public bool KuehlungAktiv { get; set; }

    /// <summary>Kühlsollwert [°C] (<c>Kuehl_Sollwert</c>); <c>null</c> = Kühlung aus (F-K1).</summary>
    public double? KuehlSollwert { get; set; }

    /// <summary>Kühlleistungsgrenze [kW] (<c>Kuehlleistung_Max</c>); <c>null</c> = unbegrenzt.</summary>
    public double? KuehlleistungMax { get; set; }

    /// <summary>
    /// Kühlsollwert der Nacht [°C] (<c>Kuehl_Sollwert_Nacht</c>) — gelesen erst mit der Stufe
    /// KU3; der Dialog zeigt ihn nicht und reicht ihn unverändert durch, auch bei „Speichern
    /// unter".
    /// </summary>
    public double? KuehlSollwertNacht { get; set; }

    // ------------------ Stufe AK1: Wärmeübergabe (Anlagenkopplung 8.1, 9.1; AK-S1, Schritt 122)
    //
    // Teil der VDI-Struktur und immer sichtbar (E20). NULL-erhaltend: Der Dialog schreibt null,
    // nicht die Vorgabe — NULL heißt „ideal" (Art), „Vorgabe der Art" (Exponent, Auslegung),
    // „hergeleitet" (Außentemperatur, Nennleistung) bzw. „die vier Bestandssollwerte" (Profil).

    /// <summary>„Übergabe rechnen" (<c>Heizkreis_Aktiv</c>, 0/1-Spalte, <c>NOT NULL DEFAULT 0</c>).</summary>
    public bool HeizkreisAktiv { get; set; }

    /// <summary>Übergabeart (<c>Uebergabe_Art</c>, <c>DbWerte.UEBERGABE_*</c>); <c>null</c> = ideal.</summary>
    public string? UebergabeArt { get; set; }

    /// <summary>Exponent der Übergabe [–] (<c>Uebergabe_Exponent</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? UebergabeExponent { get; set; }

    /// <summary>Nennleistung der Übergabe [kW] (<c>Uebergabe_Leistung_Nenn</c>); <c>null</c> = hergeleitet (8.4).</summary>
    public double? UebergabeLeistungNennKw { get; set; }

    /// <summary>Auslegungsvorlauf [°C] (<c>Auslegung_Vorlauf</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? AuslegungVorlauf { get; set; }

    /// <summary>Auslegungsrücklauf [°C] (<c>Auslegung_Ruecklauf</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? AuslegungRuecklauf { get; set; }

    /// <summary>Raumtemperatur im Auslegungspunkt [°C] (<c>Auslegung_Raumtemperatur</c>); <c>null</c> = Soll am Tag.</summary>
    public double? AuslegungRaumtemperatur { get; set; }

    /// <summary>Auslegungs-Außentemperatur [°C] (<c>Auslegung_Aussentemperatur</c>); <c>null</c> = kältestes Tagesmittel (H10).</summary>
    public double? AuslegungAussentemperatur { get; set; }

    /// <summary>„Heizkurve fahren" (<c>Heizkurve_Aktiv</c>, 0/1-Spalte, <c>NOT NULL DEFAULT 0</c>).</summary>
    public bool HeizkurveAktiv { get; set; }

    /// <summary>Niveau der Heizkurve [K] (<c>Heizkurve_Niveau</c>); <c>null</c> = 0.</summary>
    public double? HeizkurveNiveau { get; set; }

    /// <summary>Steilheit der Heizkurve [–] (<c>Heizkurve_Steilheit</c>); <c>null</c> = 1,0.</summary>
    public double? HeizkurveSteilheit { get; set; }

    /// <summary>Proportionalband des Raumreglers [K] (<c>Regler_Proportionalband</c>); <c>null</c> = 1,0 K (H1, E25).</summary>
    public double? ReglerProportionalband { get; set; }

    /// <summary>
    /// Sollwert-Zeitprogramm (<c>Sollwertprofil</c>): 168 Werte im Format von
    /// <c>AnlagenkopplungSchema.WochenprofilSchreiben</c>; <c>null</c> = die vier Bestandssollwerte (4.3).
    /// </summary>
    public string? Sollwertprofil { get; set; }

    // ------------------ Stufe AK1: Kühlübergabe (E37, Anlagenkopplung 8.1; KAK-S1, Schritt 135)
    //
    // Der Unterabschnitt „Kühlübergabe" der Gruppe „Kühlung" — sichtbar nur mit Kühlung, die Werte
    // reisen unsichtbar mit. NULL-erhaltend wie die Wärmeübergabe: NULL heißt „ideal" (Art),
    // „Vorgabe der Art" (Exponent, Auslegung, Vorlaufgrenze), „Kühlsollwert" (Raum) bzw.
    // „hergeleitet aus dem Auslegungstag" (Nennleistung, A2).

    /// <summary>„Kühlübergabe rechnen" (<c>Kuehluebergabe_Aktiv</c>, 0/1-Spalte, <c>NOT NULL DEFAULT 0</c>; A1).</summary>
    public bool KuehluebergabeAktiv { get; set; }

    /// <summary>Kühlübergabeart (<c>Kuehl_Uebergabe_Art</c>, <c>DbWerte.KUEHLUEBERGABE_*</c>); <c>null</c> = ideal.</summary>
    public string? KuehlUebergabeArt { get; set; }

    /// <summary>Exponent der Kühlübergabe [–] (<c>Kuehl_Uebergabe_Exponent</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? KuehlUebergabeExponent { get; set; }

    /// <summary>Nennleistung der Kühlübergabe [kW], sensibel (<c>Kuehl_Uebergabe_Leistung_Nenn</c>); <c>null</c> = aus dem Auslegungstag (A2).</summary>
    public double? KuehlUebergabeLeistungNennKw { get; set; }

    /// <summary>Auslegungsvorlauf der Kühlübergabe [°C] (<c>Kuehl_Auslegung_Vorlauf</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? KuehlAuslegungVorlauf { get; set; }

    /// <summary>Auslegungsrücklauf der Kühlübergabe [°C] (<c>Kuehl_Auslegung_Ruecklauf</c>); <c>null</c> = Vorgabe der Art.</summary>
    public double? KuehlAuslegungRuecklauf { get; set; }

    /// <summary>Raumtemperatur im Auslegungspunkt der Kühlübergabe [°C] (<c>Kuehl_Auslegung_Raumtemperatur</c>); <c>null</c> = Kühlsollwert.</summary>
    public double? KuehlAuslegungRaumtemperatur { get; set; }

    /// <summary>Untere Grenze des Kaltwasser-Vorlaufs [°C] (<c>Kuehl_Vorlaufgrenze</c>) — eine Vorgabe, keine Taupunktrechnung; <c>null</c> = Vorgabe der Art.</summary>
    public double? KuehlVorlaufgrenze { get; set; }

    /// <summary>
    /// Eine TIEFE Kopie — der Arbeitsstand des Dialogs. Der hereingereichte Satz bleibt
    /// bis zum OK unberührt (Hausregel „Geschrieben wird im OK-Weg").
    /// </summary>
    public GebaeudeKatalogDaten Kopie()
    {
        var k = (GebaeudeKatalogDaten)MemberwiseClone();
        k.Ferienbeginn = (int[])(Ferienbeginn ?? new int[4]).Clone();
        k.Ferienende = (int[])(Ferienende ?? new int[4]).Clone();
        return k;
    }
}
