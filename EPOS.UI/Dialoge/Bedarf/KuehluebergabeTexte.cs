namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Beschriftungen des Unterabschnitts „Kühlübergabe"</b> der Gruppe „Kühlung" (E37;
/// Anlagenkopplung 7.2, 8.1, 9.1) — ein Bündel, geführt als
/// <see cref="GebaeudeHuelleTexte.Kuehluebergabe"/>; jede Eigenschaft nennt ihren
/// Ressourcenschlüssel, der Rückfall ist der deutsche Text. Der Abschnitt heißt „Kühlübergabe",
/// nicht „Kältekreis" — der Kältekreis ist die Senke des Anlagenschemas. Die Feldbeschriftungen
/// unterscheiden sich von denen der Wärmeübergabe, weil der Feldname die Fehleingabe trägt.
/// </summary>
public sealed class KuehluebergabeTexte
{
    /// <summary><c>GEBK_UABS_KUEHLUEBERGABE</c> — die Überschrift des Unterabschnitts.</summary>
    public string Unterabschnitt { get; set; } = "Kühlübergabe";

    /// <summary><c>GEBK_LBL_KUEHLUEBERGABE_AKTIV</c></summary>
    public string LabelAktiv { get; set; } = "Kühlübergabe rechnen (statt idealer Kühlung)";

    /// <summary><c>GEBK_LBL_KUEHL_UEBERGABE_ART</c></summary>
    public string LabelArt { get; set; } = "Kühlübergabeart :";

    /// <summary><c>GEBK_KUEHLUEBERGABE_IDEAL</c></summary>
    public string ArtIdeal { get; set; } = "ideal (keine Kühlübergabe)";

    /// <summary><c>GEBK_KUEHLUEBERGABE_KUEHLDECKE</c></summary>
    public string ArtKuehldecke { get; set; } = "Kühldecke";

    /// <summary><c>GEBK_KUEHLUEBERGABE_FLAECHENKUEHLUNG</c></summary>
    public string ArtFlaechenkuehlung { get; set; } = "Flächenkühlung";

    /// <summary><c>GEBK_KUEHLUEBERGABE_GEBLAESEKONVEKTOR</c></summary>
    public string ArtGeblaesekonvektor { get; set; } = "Gebläsekonvektor";

    /// <summary><c>GEBK_LBL_KUEHL_UEBERGABE_EXPONENT</c></summary>
    public string LabelExponent { get; set; } = "Exponent der Kühlübergabe :";

    /// <summary><c>GEBK_LBL_KUEHL_UEBERGABE_NENNLEISTUNG</c></summary>
    public string LabelNennleistung { get; set; } = "Nennleistung der Kühlübergabe :";

    /// <summary><c>GEBK_LBL_KUEHL_AUSLEGUNG_VORLAUF</c></summary>
    public string LabelAuslegungVorlauf { get; set; } = "Auslegung Kühlvorlauf :";

    /// <summary><c>GEBK_LBL_KUEHL_AUSLEGUNG_RUECKLAUF</c></summary>
    public string LabelAuslegungRuecklauf { get; set; } = "Auslegung Kühlrücklauf :";

    /// <summary><c>GEBK_LBL_KUEHL_AUSLEGUNG_RAUM</c></summary>
    public string LabelAuslegungRaum { get; set; } = "Auslegung Raum (Kühlung) :";

    /// <summary><c>GEBK_LBL_KUEHL_VORLAUFGRENZE</c></summary>
    public string LabelVorlaufgrenze { get; set; } = "Untere Vorlaufgrenze :";

    /// <summary><c>GEBK_VORGABE_HERGELEITET</c> — Platzhalter eines hergeleiteten Feldes ohne Zahl.</summary>
    public string VorgabeHergeleitet { get; set; } = "Vorgabe: hergeleitet";

    /// <summary><c>GEBK_VORGABE_KEINE_GRENZE</c> — Platzhalter der Vorlaufgrenze einer Art ohne Grenze.</summary>
    public string VorgabeKeineGrenze { get; set; } = "Vorgabe: keine Grenze";

    // ---- Herleitungszeilen ----------------------------------------------------------

    /// <summary><c>GEBK_ZEILE_KUEHLUEBERGABE_AUS</c></summary>
    public string ZeileAus { get; set; }
        = "Ohne Haken rechnet die Kühlung ideal: Das Gebäude wird auf den Kühlsollwert gekühlt, ohne Kühlübergabe.";

    /// <summary><c>GEBK_ZEILE_KUEHLUEBERGABE_IDEAL</c></summary>
    public string ZeileIdeal { get; set; }
        = "Kühlübergabeart „ideal“: Das Gebäude rechnet wie ohne Haken mit idealer Kühlung. Gerechnet wird die Kühlübergabe mit Kühldecke, Flächenkühlung oder Gebläsekonvektor.";

    /// <summary>
    /// <c>GEBK_ZEILE_KUEHLUEBERGABE_ART</c> — „{0}" Art, „{1}" Exponent, „{2}" Vorlauf, „{3}" Rücklauf
    /// [°C], „{4}" Strahlungsanteil, „{5}" die Vorlaufgrenze der Art (Zahl mit Einheit oder „keine").
    /// </summary>
    public string ZeileArt { get; set; }
        = "Vorgaben der Kühlübergabeart {0}: Exponent {1}, Auslegung {2}/{3} °C, Strahlungsanteil {4}, Vorlaufgrenze {5} (gilt bei leerem Feld) — Vorgaben von EPOS-Plan, keine Normwerte; jedes Feld überschreibt sie.";

    /// <summary><c>GEBK_ZEILE_KUEHL_GRENZE_KEINE</c> — der Text für „{5}" einer Art ohne Grenze.</summary>
    public string GrenzeKeine { get; set; } = "keine";

    /// <summary><c>GEBK_ZEILE_KUEHL_AUSLEGUNG_RAUM</c> — „{0}" Kühlsollwert [°C].</summary>
    public string ZeileRaum { get; set; } = "Auslegung Raum leer: der Kühlsollwert, {0} °C.";

    /// <summary>
    /// <c>GEBK_ZEILE_KUEHL_NENNLEISTUNG</c> — „{0}" Nennleistung [kW], „{1}" Auslegungstag, „{2}"
    /// Tagesmittel der Außenluft [°C].
    /// </summary>
    public string ZeileNennleistung { get; set; }
        = "Nennleistung leer: die Kühllast des Auslegungstags {1} (höchstes Tagesmittel der Außenluft, {2} °C) — {0} kW, sensibel. Kein Normnachweis; im Projekt skaliert sie mit dem Gebäude.";

    /// <summary><c>GEBK_ZEILE_KUEHL_NENNLEISTUNG_OHNE</c></summary>
    public string ZeileNennleistungOhne { get; set; }
        = "Nennleistung leer: die Kühllast des Auslegungstags (höchstes Tagesmittel der Außenluft), sensibel, kein Normnachweis; die Zahl steht hier, sobald das Gebäude in einem Projekt mit Klimaregion geöffnet ist.";

    /// <summary><c>GEBK_ZEILE_HERLEITUNG_BEFUND</c> — „{0}" der Grund.</summary>
    public string ZeileHerleitungBefund { get; set; } = "Keine hergeleitete Zahl: {0}";

    /// <summary><c>GEBK_ZEILE_KUEHL_VORLAUF_ANLAGE</c> — „{0}" Kaltwasser-Vorlauf [°C].</summary>
    public string ZeileVorlaufAnlage { get; set; }
        = "Kaltwasser-Vorlauf fest {0} °C: der Kühl-Vorlauf der Wärmepumpe im Kühlbetrieb dieses Projekts.";

    /// <summary><c>GEBK_ZEILE_KUEHL_VORLAUF_GEMISCHT</c> — „{0}" Vorlauf der Anlage, „{1}" Vorlaufgrenze [°C].</summary>
    public string ZeileVorlaufGemischt { get; set; }
        = "Kaltwasser-Vorlauf fest {1} °C: Die Wärmepumpe im Kühlbetrieb liefert {0} °C, das Gebäude mischt auf die Vorlaufgrenze hoch.";

    /// <summary><c>GEBK_ZEILE_KUEHL_VORLAUF_AUSLEGUNG</c> — „{0}" Kaltwasser-Vorlauf [°C].</summary>
    public string ZeileVorlaufAuslegung { get; set; }
        = "Kaltwasser-Vorlauf fest {0} °C: Keine Wärmepumpe im Kühlbetrieb führt einen Kühl-Vorlauf — es gilt der Auslegungsvorlauf, mindestens die Vorlaufgrenze.";

    /// <summary><c>GEBK_ZEILE_KUEHL_VORLAUF_OHNE</c></summary>
    public string ZeileVorlaufOhne { get; set; }
        = "Der Kaltwasser-Vorlauf ist fest: der kälteste Kühl-Vorlauf der Wärmepumpen im Kühlbetrieb, ohne solche Anlage der Auslegungsvorlauf; liegt er unter der Vorlaufgrenze, mischt das Gebäude hoch.";

    /// <summary><c>GEBK_ZEILE_KUEHL_GRENZE</c></summary>
    public string ZeileGrenze { get; set; }
        = "Die Vorlaufgrenze ist eine Vorgabe, keine gerechnete Taupunktgrenze: EPOS-Plan prüft nicht, ob an der Kühlfläche Tauwasser ausfällt.";

    /// <summary><c>GEBK_ZEILE_KUEHL_GRENZE_UEBER_AUSLEGUNG</c> — „{0}" Grenze, „{1}" Auslegungsvorlauf [°C].</summary>
    public string ZeileGrenzeUeberAuslegung { get; set; }
        = "Die Vorlaufgrenze {0} °C liegt über dem Auslegungsvorlauf {1} °C — die Nennleistung wird nie erreicht.";

    /// <summary><c>GEBK_ZEILE_KUEHL_SENSIBEL</c></summary>
    public string ZeileSensibel { get; set; }
        = "Die Kühlübergabe rechnet sensibel, ohne Entfeuchtung — jede Kältezahl ist eine sensible Zahl.";

    /// <summary><c>GEBK_ZEILE_KUEHL_WIRKSAM</c></summary>
    public string ZeileWirksam { get; set; }
        = "In diesem Projekt wirksam: Es rechnet mit Anlagenkopplung und Kühlbetrieb.";

    /// <summary><c>GEBK_ZEILE_KUEHL_OHNE_SOLLWERT</c></summary>
    public string ZeileOhneSollwert { get; set; }
        = "Nicht wirksam: Ohne Kühlsollwert wird das Gebäude nicht gekühlt; die Eingaben der Kühlübergabe ruhen.";

    /// <summary><c>GEBK_ZEILE_KUEHL_PROJEKT_OHNE_STUFE</c></summary>
    public string ZeileProjektOhneStufe { get; set; }
        = "In diesem Projekt nicht wirksam: Es rechnet ohne Anlagenkopplung — gerechnet wird ideale Kühlung.";

    /// <summary><c>GEBK_ZEILE_KUEHL_PROJEKT_OHNE_KAELTE</c></summary>
    public string ZeileProjektOhneKaelte { get; set; }
        = "In diesem Projekt nicht wirksam: Es rechnet keine Kälte (Projekteinstellung „Kühlung rechnen“ aus).";

    /// <summary><c>GEBK_ZEILE_KUEHL_PROJEKT</c></summary>
    public string ZeileProjekt { get; set; }
        = "Gerechnet wird die Kühlübergabe nur in Projekten mit Anlagenkopplung „Heizkreis (AK1)“ und der Projekteinstellung „Kühlung rechnen“ (Simulationskonfiguration).";

    // ---- Meldungen der Prüfung -------------------------------------------------------

    /// <summary><c>GEBK_MSG_UEB_BEREICH</c> — „{0}" Feld, „{1}" kleinster, „{2}" größter Wert.</summary>
    public string MeldungBereich { get; set; } = "{0} muss zwischen {1} und {2} liegen.";

    /// <summary><c>GEBK_MSG_KUEHL_ART_UNBEKANNT</c> — „{0}" die gespeicherte Art.</summary>
    public string MeldungArtUnbekannt { get; set; }
        = "Die Kühlübergabeart „{0}“ ist unbekannt; bitte Kühldecke, Flächenkühlung, Gebläsekonvektor oder ideal wählen.";

    /// <summary><c>GEBK_MSG_KUEHL_NENNLEISTUNG</c></summary>
    public string MeldungNennleistung { get; set; } = "Die Nennleistung der Kühlübergabe muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_KUEHL_REIHENFOLGE</c> — „{0}" Vorlauf, „{1}" Rücklauf, „{2}" Raum [°C].</summary>
    public string MeldungReihenfolge { get; set; }
        = "Der Auslegungspunkt der Kühlübergabe muss aufsteigen: Vorlauf {0} °C unter Rücklauf {1} °C unter Raumtemperatur {2} °C.";

    /// <summary>Der Anzeigename einer Kühlübergabeart (Steuerwert → Text); NULL und „ideal" heißen ideal.</summary>
    public string Artname(string? art) => art switch
    {
        WindowsFormsApplication1.DbWerte.KUEHLUEBERGABE_KUEHLDECKE => ArtKuehldecke,
        WindowsFormsApplication1.DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG => ArtFlaechenkuehlung,
        WindowsFormsApplication1.DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR => ArtGeblaesekonvektor,
        _ => ArtIdeal
    };
}
