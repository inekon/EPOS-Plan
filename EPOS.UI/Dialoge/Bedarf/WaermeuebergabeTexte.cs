using EPOS.UI.Bausteine;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Beschriftungen der Gruppe „Wärmeübergabe"</b> des Gebäudedialogs (Konzept
/// Anlagenkopplung 9.1, 9.2; AK1 Welle 3) — ein Bündel, geführt als
/// <see cref="GebaeudeHuelleTexte.Uebergabe"/>; jede Eigenschaft nennt ihren Ressourcenschlüssel.
/// Der Rückfall ist der deutsche Text. Die Gruppe heißt „Wärmeübergabe", nicht „Heizkreis" —
/// „Heizkreis" ist die Wärmesenke des Anlagenschemas (H11).
/// </summary>
public sealed class WaermeuebergabeTexte
{
    /// <summary><c>GEBK_GRP_WAERMEUEBERGABE</c></summary>
    public string Gruppe { get; set; } = "Wärmeübergabe";

    /// <summary><c>GEBK_LBL_HEIZKREIS_AKTIV</c></summary>
    public string LabelHeizkreisAktiv { get; set; } = "Übergabe rechnen (statt idealer Regelung)";

    /// <summary><c>GEBK_LBL_UEBERGABE_ART</c></summary>
    public string LabelArt { get; set; } = "Übergabeart :";

    /// <summary><c>GEBK_UEBERGABE_IDEAL</c></summary>
    public string ArtIdeal { get; set; } = "ideal (keine Übergabe)";

    /// <summary><c>GEBK_UEBERGABE_RADIATOR</c></summary>
    public string ArtRadiator { get; set; } = "Radiator";

    /// <summary><c>GEBK_UEBERGABE_FLAECHE</c></summary>
    public string ArtFlaeche { get; set; } = "Flächenheizung";

    /// <summary><c>GEBK_UEBERGABE_KONVEKTOR</c></summary>
    public string ArtKonvektor { get; set; } = "Konvektor";

    /// <summary><c>GEBK_LBL_UEBERGABE_EXPONENT</c></summary>
    public string LabelExponent { get; set; } = "Exponent :";

    /// <summary><c>GEBK_LBL_AUSLEGUNG_VORLAUF</c></summary>
    public string LabelAuslegungVorlauf { get; set; } = "Auslegung Vorlauf :";

    /// <summary><c>GEBK_LBL_AUSLEGUNG_RUECKLAUF</c></summary>
    public string LabelAuslegungRuecklauf { get; set; } = "Auslegung Rücklauf :";

    /// <summary><c>GEBK_LBL_AUSLEGUNG_RAUM</c></summary>
    public string LabelAuslegungRaum { get; set; } = "Auslegung Raum :";

    /// <summary><c>GEBK_LBL_AUSLEGUNG_AUSSEN</c></summary>
    public string LabelAuslegungAussen { get; set; } = "Auslegungs-Außentemperatur :";

    /// <summary><c>GEBK_LBL_UEBERGABE_NENNLEISTUNG</c></summary>
    public string LabelNennleistung { get; set; } = "Nennleistung der Übergabe :";

    /// <summary><c>GEBK_LBL_HEIZKURVE_AKTIV</c></summary>
    public string LabelHeizkurveAktiv { get; set; } = "Heizkurve fahren";

    /// <summary><c>GEBK_LBL_HEIZKURVE_NIVEAU</c></summary>
    public string LabelHeizkurveNiveau { get; set; } = "Niveau der Heizkurve :";

    /// <summary><c>GEBK_LBL_HEIZKURVE_STEILHEIT</c></summary>
    public string LabelHeizkurveSteilheit { get; set; } = "Steilheit der Heizkurve :";

    /// <summary><c>GEBK_LBL_PROPORTIONALBAND</c></summary>
    public string LabelProportionalband { get; set; } = "Proportionalband des Raumreglers :";

    /// <summary><c>GEBK_BAND_FREI</c> — der vierte Eintrag der Schnellwahl.</summary>
    public string BandFrei { get; set; } = "frei";

    /// <summary><c>GEBK_LBL_BAND_FREI</c> — Beschriftung des freien Werts.</summary>
    public string LabelBandFrei { get; set; } = "Proportionalband (frei) :";

    /// <summary><c>GEBK_VORGABE_HERGELEITET</c> — Platzhalter eines hergeleiteten Feldes ohne Zahl.</summary>
    public string VorgabeHergeleitet { get; set; } = "Vorgabe: hergeleitet";

    /// <summary><c>GEBK_LBL_SOLLWERTPROFIL</c> — Überschrift des Zeitprogramms.</summary>
    public string LabelSollwertprofil { get; set; } = "Sollwert-Zeitprogramm";

    // ---- Herleitungszeilen ----------------------------------------------------------

    /// <summary><c>GEBK_ZEILE_UEBERGABE_AUS</c></summary>
    public string ZeileAus { get; set; } = "Ohne Haken rechnet das Gebäude wie bisher mit idealer Regelung.";

    /// <summary><c>GEBK_ZEILE_UEBERGABE_IDEAL</c></summary>
    public string ZeileIdeal { get; set; }
        = "Übergabeart „ideal“: Das Gebäude rechnet wie ohne Haken mit idealer Regelung. Gerechnet wird die Übergabe mit Radiator, Flächenheizung oder Konvektor.";

    /// <summary>
    /// <c>GEBK_ZEILE_UEBERGABE_ART</c> — „{0}" Art, „{1}" Exponent, „{2}" Vorlauf, „{3}" Rücklauf [°C],
    /// „{4}" Strahlungsanteil der Heizung (H12).
    /// </summary>
    public string ZeileArt { get; set; }
        = "Vorgaben der Übergabeart {0}: Exponent {1}, Auslegung {2}/{3} °C, Strahlungsanteil der Heizung {4} (gilt bei leerem Feld) — Vorgaben von EPOS-Plan, keine Normwerte; jedes Feld überschreibt sie.";

    /// <summary><c>GEBK_ZEILE_AUSLEGUNG_RAUM</c> — „{0}" Soll am Tag [°C].</summary>
    public string ZeileRaum { get; set; } = "Auslegung Raum leer: das Soll am Tag, {0} °C.";

    /// <summary><c>GEBK_ZEILE_AUSLEGUNG_AUSSEN</c> — „{0}" hergeleitete Außentemperatur [°C].</summary>
    public string ZeileAussen { get; set; }
        = "Auslegungs-Außentemperatur leer: das kälteste Tagesmittel der Klimareihe des Projekts, abgerundet — {0} °C.";

    /// <summary><c>GEBK_ZEILE_AUSLEGUNG_AUSSEN_OHNE</c></summary>
    public string ZeileAussenOhne { get; set; }
        = "Auslegungs-Außentemperatur leer: das kälteste Tagesmittel der Klimareihe des Projekts, abgerundet; die Zahl steht hier, sobald das Gebäude in einem Projekt mit Klimaregion geöffnet ist.";

    /// <summary>
    /// <c>GEBK_ZEILE_NENNLEISTUNG</c> — „{0}" Nennleistung [kW], „{1}" Außen-, „{2}" Raumtemperatur
    /// des Auslegungspunkts [°C].
    /// </summary>
    public string ZeileNennleistung { get; set; }
        = "Nennleistung leer: die stationäre Heizlast dieses Gebäudes bei {1} °C außen und {2} °C innen — {0} kW. Kein Nachweis nach DIN EN 12831; im Projekt skaliert sie mit dem Gebäude.";

    /// <summary><c>GEBK_ZEILE_NENNLEISTUNG_OHNE</c></summary>
    public string ZeileNennleistungOhne { get; set; }
        = "Nennleistung leer: die stationäre Heizlast des Gebäudes im Auslegungspunkt, kein Nachweis nach DIN EN 12831; die Zahl steht hier, sobald das Gebäude in einem Projekt mit Klimaregion geöffnet ist.";

    /// <summary><c>GEBK_ZEILE_HERLEITUNG_BEFUND</c> — „{0}" der Grund.</summary>
    public string ZeileHerleitungBefund { get; set; } = "Keine hergeleitete Zahl: {0}";

    /// <summary>
    /// <c>GEBK_ZEILE_HEIZKURVE_AN</c> — „{0}" Auslegungsvorlauf, „{1}" Auslegungs-Außentemperatur,
    /// „{2}" Vorgabe Niveau [K], „{3}" Vorgabe Steilheit.
    /// </summary>
    public string ZeileHeizkurveAn { get; set; }
        = "Heizkurve: Der Vorlauf folgt der Außentemperatur durch den Auslegungspunkt ({0} °C bei {1} °C); das Niveau verschiebt, die Steilheit neigt die Kurve (Vorgabe {2} K und {3}).";

    /// <summary><c>GEBK_ZEILE_HEIZKURVE_AUS</c></summary>
    public string ZeileHeizkurveAus { get; set; }
        = "Ohne Heizkurve fährt das Gebäude einen festen Vorlauf: den höchsten projektierten Vorlauf der Wärmeerzeuger des Heizkanals, ohne solche Anlage den Auslegungsvorlauf.";

    /// <summary><c>GEBK_ZEILE_PROPORTIONALBAND</c> — „{0}" Vorgabe [K], „{1}" größter Wert [K].</summary>
    public string ZeileBand { get; set; }
        = "Proportionalband: Schnellwahl 0,5 / 1 / 2 K oder frei von 0 bis {1} K; 0 K ist die ideale Regelung mit Grenze. Leer gilt die Vorgabe {0} K.";

    /// <summary><c>GEBK_ZEILE_UEBERGABE_PROJEKT</c></summary>
    public string ZeileProjekt { get; set; }
        = "Gerechnet wird die Wärmeübergabe nur in Projekten mit der Projekteinstellung Anlagenkopplung „Heizkreis (AK1)“ (Simulationskonfiguration).";

    /// <summary><c>GEBK_ZEILE_UEBERGABE_BESTANDSWEG</c> — bis zur Stufe GA (Löschliste).</summary>
    public string ZeileBestandsweg { get; set; }
        = "Tagesbilanz (Bestandsweg) rechnet keine Anlagenkopplung — diese Eingaben gelten, sobald das Gebäude auf VDI 6007 rechnet.";

    /// <summary><c>GEBK_ZEILE_SOLLWERTPROFIL_OHNE</c></summary>
    public string ZeileProfilOhne { get; set; }
        = "Ohne Zeitprogramm gelten die Sollwerte des Gebäudes (Tag, Nacht, Wochenende); das Raster zeigt sie. Die Ferien wirken in beiden Fällen darüber.";

    /// <summary><c>GEBK_ZEILE_SOLLWERTPROFIL_MIT</c></summary>
    public string ZeileProfilMit { get; set; }
        = "Das Zeitprogramm gilt je Wochenstunde (Montag 00 Uhr bis Sonntag 23 Uhr); die Ferienzeiträume wirken darüber mit dem Feriensollwert.";

    // ---- Meldungen der Prüfung -------------------------------------------------------

    /// <summary><c>GEBK_MSG_UEB_BEREICH</c> — „{0}" Feld, „{1}" kleinster, „{2}" größter Wert.</summary>
    public string MeldungBereich { get; set; } = "{0} muss zwischen {1} und {2} liegen.";

    /// <summary><c>GEBK_MSG_UEB_ART_UNBEKANNT</c> — „{0}" die gespeicherte Art.</summary>
    public string MeldungArtUnbekannt { get; set; }
        = "Die Übergabeart „{0}“ ist unbekannt; bitte Radiator, Flächenheizung, Konvektor oder ideal wählen.";

    /// <summary><c>GEBK_MSG_UEB_NENNLEISTUNG</c></summary>
    public string MeldungNennleistung { get; set; } = "Die Nennleistung der Übergabe muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_UEB_VORLAUF_RAUM</c> — „{0}" Vorlauf, „{1}" Raum [°C].</summary>
    public string MeldungVorlaufRaum { get; set; }
        = "Der Auslegungsvorlauf {0} °C liegt nicht über der Auslegungs-Raumtemperatur {1} °C — die Übergabe gäbe keine Wärme ab.";

    /// <summary><c>GEBK_MSG_UEB_RUECKLAUF</c> — „{0}" Rücklauf, „{1}" Raum, „{2}" Vorlauf [°C].</summary>
    public string MeldungRuecklauf { get; set; }
        = "Der Auslegungsrücklauf {0} °C muss zwischen der Auslegungs-Raumtemperatur {1} °C und dem Auslegungsvorlauf {2} °C liegen.";

    /// <summary><c>GEBK_MSG_UEB_AUSSEN_RAUM</c> — „{0}" Außen, „{1}" Raum [°C].</summary>
    public string MeldungAussenRaum { get; set; }
        = "Die Auslegungs-Außentemperatur {0} °C muss unter der Auslegungs-Raumtemperatur {1} °C liegen.";

    /// <summary><c>GEBK_MSG_UEB_PROFIL_WERTZAHL</c> — „{0}" gefunden, „{1}" erwartet.</summary>
    public string MeldungProfilWertzahl { get; set; } = "Das Sollwert-Zeitprogramm hat {0} statt {1} Werte.";

    /// <summary><c>GEBK_MSG_UEB_PROFIL_KEINE_ZAHL</c> — „{0}" die Stelle.</summary>
    public string MeldungProfilKeineZahl { get; set; } = "Das Sollwert-Zeitprogramm enthält an Stelle {0} keine Zahl.";

    /// <summary><c>GEBK_MSG_UEB_PROFIL_WERT</c> — „{0}" Stelle, „{1}" Wert, „{2}"/„{3}" Grenzen [°C].</summary>
    public string MeldungProfilWert { get; set; }
        = "Das Sollwert-Zeitprogramm hat an Stelle {0} den Wert {1} °C; zulässig sind {2} bis {3} °C.";

    /// <summary>Die Beschriftungen des Wochenrasters (Baustein).</summary>
    public WochenrasterTexte Raster { get; set; } = new();

    /// <summary>Der Anzeigename einer Übergabeart (Steuerwert → Text); NULL und „ideal" heißen ideal.</summary>
    public string Artname(string? art) => art switch
    {
        WindowsFormsApplication1.DbWerte.UEBERGABE_RADIATOR => ArtRadiator,
        WindowsFormsApplication1.DbWerte.UEBERGABE_FLAECHE => ArtFlaeche,
        WindowsFormsApplication1.DbWerte.UEBERGABE_KONVEKTOR => ArtKonvektor,
        _ => ArtIdeal
    };
}
