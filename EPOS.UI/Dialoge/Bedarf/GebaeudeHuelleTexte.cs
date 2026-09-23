namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der VDI-6007-Struktur des Gebäude-Katalogeditors (Stufe G1;
/// Umsetzungskonzept Gebäudesimulation 2.3–2.6, 2.9) — EIN Parameter statt vierzig.
/// </summary>
/// <remarks>
/// <para><b>Warum gebündelt.</b> Hausregel „ab etwa zehn Anzeigetexten ein Bündel"
/// (<c>EPOS.UI/CLAUDE.md</c>). Die Bestandstexte des Editors bleiben einzelne Parameter;
/// alles, was mit der Hülltabelle, den Wärmeleitwerten, den Fenstern nach Orientierung,
/// den Modellparametern, dem Schalter „Rechenweg" und den Prüfregeln neu dazukommt, steht
/// hier. Beschriftungen, kein Zustand.</para>
/// <para>Jede Eigenschaft trägt ihren Ressourcenschlüssel im Kommentar; der Vorgabewert
/// ist der deutsche Rückfall. Begriffe nach Glossar § 13 „Gebäudehülle und
/// Gebäudemodell".</para>
/// </remarks>
public sealed class GebaeudeHuelleTexte
{
    // ------------------------------------------------------------ Reiter und Gruppen

    /// <summary><c>GEBK_REITER_HUELLE</c></summary>
    public string ReiterHuelle { get; set; } = "Gebäude und Hülle";

    /// <summary><c>GEBK_REITER_TEMPERATUREN</c></summary>
    public string ReiterTemperaturen { get; set; } = "Temperaturen und Ferien";

    /// <summary><c>GEBK_GRP_HUELLE</c></summary>
    public string GruppeHuelle { get; set; } = "Hülle: Transmission je Bauteil";

    /// <summary><c>GEBK_GRP_LEITWERTE</c></summary>
    public string GruppeLeitwerte { get; set; } = "Wärmeleitwerte";

    /// <summary><c>GEBK_GRP_FENSTER_ORIENTIERUNG</c></summary>
    public string GruppeFenster { get; set; } = "Fenster nach Orientierung";

    /// <summary><c>GEBK_GRP_MODELLPARAMETER</c></summary>
    public string GruppeModellparameter { get; set; } = "Modellparameter (VDI 6007)";

    /// <summary><c>GEBK_GRP_TAGESBILANZ</c></summary>
    public string GruppeTagesbilanz { get; set; } = "Tagesbilanz (Bestandsweg)";

    // ------------------------------------------------------------ U·A-Tabelle

    /// <summary><c>GEBK_SP_BAUTEIL</c></summary>
    public string SpalteBauteil { get; set; } = "Bauteil";

    /// <summary><c>GEBK_SP_U_PSI</c></summary>
    public string SpalteKennwert { get; set; } = "U bzw. ψ";

    /// <summary><c>GEBK_SP_A_L</c></summary>
    public string SpalteGroesse { get; set; } = "A bzw. L";

    /// <summary><c>GEBK_SP_RANDBEDINGUNG</c></summary>
    public string SpalteRandbedingung { get; set; } = "Randbedingung";

    /// <summary><c>GEBK_SP_UA</c></summary>
    public string SpalteLeitwert { get; set; } = "U·A [W/K]";

    /// <summary><c>GEBK_ZEILE_AUSSENWAND</c></summary>
    public string ZeileAussenwand { get; set; } = "Außenwand";

    /// <summary><c>GEBK_ZEILE_FENSTER</c></summary>
    public string ZeileFenster { get; set; } = "Fenster";

    /// <summary><c>GEBK_ZEILE_DACH</c></summary>
    public string ZeileDach { get; set; } = "Dach";

    /// <summary><c>GEBK_ZEILE_BODENPLATTE</c></summary>
    public string ZeileBodenplatte { get; set; } = "Bodenplatte";

    /// <summary><c>GEBK_ZEILE_SONSTIGES</c></summary>
    public string ZeileSonstiges { get; set; } = "Sonstiges";

    /// <summary><c>GEBK_ZEILE_WB_FENSTER</c></summary>
    public string ZeileWbFenster { get; set; } = "Wärmebrücke Fenster–Wand";

    /// <summary><c>GEBK_ZEILE_WB_KELLER</c></summary>
    public string ZeileWbKeller { get; set; } = "Wärmebrücke Außenwand–Keller";

    /// <summary><c>GEBK_ZEILE_WB_DACH</c></summary>
    public string ZeileWbDach { get; set; } = "Wärmebrücke Wand–Dach";

    /// <summary><c>GEBK_RAND_AUSSENLUFT</c></summary>
    public string RandAussenluft { get; set; } = "Außenluft";

    /// <summary><c>GEBK_RAND_ERDREICH</c></summary>
    public string RandErdreich { get; set; } = "Erdreich";

    /// <summary><c>GEBK_RAND_KELLER</c></summary>
    public string RandKeller { get; set; } = "Keller (unbeheizt)";

    /// <summary><c>GEBK_LBL_KELLERTEMPERATUR</c></summary>
    public string LabelKellertemperatur { get; set; } = "Kellertemperatur :";

    /// <summary><c>GEBK_HINWEIS_FENSTER_SUMME</c></summary>
    public string HinweisFensterflaeche { get; set; }
        = "Die Fensterfläche ist die Summe der vier Orientierungen.";

    // ------------------------------------------------------------ Wärmeleitwerte

    /// <summary><c>GEBK_LBL_HT</c></summary>
    public string LabelHT { get; set; } = "H_T Transmission :";

    /// <summary><c>GEBK_LBL_HVE</c></summary>
    public string LabelHVe { get; set; } = "H_ve Lüftung :";

    /// <summary><c>GEBK_LBL_HGES</c></summary>
    public string LabelHGes { get; set; } = "H_ges gesamt :";

    /// <summary><c>GEBK_LBL_HT_GEWICHTET</c></summary>
    public string LabelHTGewichtet { get; set; } = "H_T gewichtet (Tagesbilanz) :";

    /// <summary><c>GEBK_HINWEIS_GEWICHTE</c></summary>
    public string HinweisGewichte { get; set; }
        = "Der Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken mit 0,83, das Dach mit 0,95 "
        + "und die Bodenplatte mit 0,45. Das Stundenmodell rechnet ungewichtet.";

    /// <summary><c>GEBK_HINWEIS_LUEFTUNG</c></summary>
    public string HinweisLueftung { get; set; }
        = "H_ve = Luftwechselrate · Nutzfläche · Raumhöhe · 0,34 Wh/(m³K).";

    // ------------------------------------------------------------ Fenster

    /// <summary><c>GEBK_LBL_FF_OST</c></summary>
    public string LabelFFOst { get; set; } = "Fensterfläche Ost :";

    /// <summary><c>GEBK_LBL_FF_WEST</c></summary>
    public string LabelFFWest { get; set; } = "Fensterfläche West :";

    /// <summary><c>GEBK_LBL_FF_SUMME_OW</c></summary>
    public string LabelFFSummeOstWest { get; set; } = "Summe Ost + West :";

    /// <summary><c>GEBK_LBL_FF_GESAMT</c></summary>
    public string LabelFFGesamt { get; set; } = "gesamte Fensterfläche :";

    /// <summary><c>GEBK_FELD_FF_OST</c></summary>
    public string FeldFFOst { get; set; } = "Fensterfläche Ost";

    /// <summary><c>GEBK_FELD_FF_WEST</c></summary>
    public string FeldFFWest { get; set; } = "Fensterfläche West";

    // ------------------------------------------------------------ Modellparameter

    /// <summary><c>GEBK_LBL_RAHMENANTEIL</c></summary>
    public string LabelRahmenanteil { get; set; } = "Rahmenanteil :";

    /// <summary><c>GEBK_LBL_VERSCHATTUNG</c></summary>
    public string LabelVerschattung { get; set; } = "Verschattungsfaktor :";

    /// <summary><c>GEBK_LBL_MASSEANTEIL</c></summary>
    public string LabelMasseanteil { get; set; } = "Masseanteil außen :";

    /// <summary><c>GEBK_LBL_INNENFLAECHENFAKTOR</c></summary>
    public string LabelInnenflaechenfaktor { get; set; } = "Innenflächenfaktor :";

    /// <summary><c>GEBK_LBL_HEIZUNG_STRAHLUNG</c></summary>
    public string LabelHeizungStrahlung { get; set; } = "Strahlungsanteil Heizung :";

    /// <summary><c>GEBK_LBL_HEIZLEISTUNG_MAX</c></summary>
    public string LabelHeizleistungMax { get; set; } = "Heizleistungsgrenze :";

    /// <summary><c>GEBK_LBL_AUSSEN_STRAHLUNG</c></summary>
    public string LabelAussenStrahlung { get; set; } = "Außenbauteile mit Strahlung";

    /// <summary><c>GEBK_LBL_INFILTRATION</c></summary>
    public string LabelInfiltration { get; set; } = "Infiltration :";

    /// <summary><c>GEBK_LBL_NUTZERLUEFTUNG</c></summary>
    public string LabelNutzerlueftung { get; set; } = "Nutzerlüftung :";

    /// <summary><c>GEBK_LBL_SOMMERLUEFTUNG</c></summary>
    public string LabelSommerlueftung { get; set; } = "Sommerlüftung";

    /// <summary><c>GEBK_HINWEIS_LUFTWECHSEL</c> — „{0}" der Luftwechsel, „{1}" seine Herkunft.</summary>
    public string HinweisLuftwechsel { get; set; } = "VDI 6007 rechnet mit {0} 1/h ({1}).";

    /// <summary><c>GEBK_HERKUNFT_INFILTRATION_NUTZER</c></summary>
    public string HerkunftInfiltrationNutzer { get; set; } = "Infiltration + Nutzerlüftung";

    /// <summary><c>GEBK_HERKUNFT_LUFTWECHSELRATE</c></summary>
    public string HerkunftLuftwechselrate { get; set; } = "Luftwechselrate des Gebäudes";

    /// <summary><c>GEBK_HERKUNFT_VORGABE</c></summary>
    public string HerkunftVorgabe { get; set; } = "Vorgabe";

    /// <summary><c>GEBK_HINWEIS_SOMMERLUEFTUNG</c> — „{0}" Schwelle [°C], „{1}" Luftwechsel [1/h].</summary>
    public string HinweisSommerlueftung { get; set; }
        = "Sommerlüftung: Ist die Raumluft über {0} °C und die Außenluft mindestens 2 K kühler, steigt der Luftwechsel auf {1} 1/h.";

    /// <summary><c>GEBK_VORGABE</c> — „Vorgabe {0}", Platzhalter im leeren Feld.</summary>
    public string VorgabeFormat { get; set; } = "Vorgabe {0}";

    /// <summary><c>GEBK_VORGABE_UNBEGRENZT</c></summary>
    public string VorgabeUnbegrenzt { get; set; } = "Vorgabe: unbegrenzt";

    /// <summary><c>GEBK_HINWEIS_MODELLPARAMETER</c></summary>
    public string HinweisModellparameter { get; set; }
        = "Leere Felder rechnen mit der Vorgabe. Die Werte gelten für den Rechenweg VDI 6007.";

    // ------------------------------------------------------------ Rechenweg

    /// <summary><c>GEBK_LBL_RECHENWEG</c></summary>
    public string LabelRechenweg { get; set; } = "Rechenweg :";

    /// <summary><c>GEBK_RECHENWEG_VDI6007</c></summary>
    public string RechenwegVdi6007 { get; set; } = "VDI 6007";

    /// <summary><c>GEBK_RECHENWEG_TAGESBILANZ</c></summary>
    public string RechenwegTagesbilanz { get; set; } = "Tagesbilanz";

    /// <summary><c>GEBK_ZEILE_RECHENWEG_VDI6007</c></summary>
    public string ZeileRechenwegVdi6007 { get; set; }
        = "VDI 6007: Raumtemperatur, Kühlbedarf und Spitzenlast je Stunde.";

    /// <summary><c>GEBK_ZEILE_RECHENWEG_TAGESBILANZ</c></summary>
    public string ZeileRechenwegTagesbilanz { get; set; }
        = "Tagesbilanz: der eingefrorene Bestandsweg mit gewichteten Bauteilen; "
        + "er kennt weder Raumtemperatur noch Kühllast noch Anlagenkopplung.";

    /// <summary><c>GEBK_ZEILE_RECHENWEG_VORGABE</c> — „{0}" ist der Rechenweg der Vorgabe.</summary>
    public string ZeileRechenwegVorgabe { get; set; }
        = "Am Gebäude ist kein Rechenweg eingetragen; es gilt die Vorgabe des Programms: {0}.";

    // ------------------------------------------------------------ Kühlung (Stufe KU1, Kühlkonzept 8.1)

    /// <summary><c>GEBK_GRP_KUEHLUNG</c></summary>
    public string GruppeKuehlung { get; set; } = "Kühlung";

    /// <summary><c>GEBK_LBL_KUEHLUNG_AKTIV</c></summary>
    public string LabelKuehlungAktiv { get; set; } = "Gebäude wird gekühlt";

    /// <summary><c>GEBK_LBL_KUEHL_SOLLWERT</c></summary>
    public string LabelKuehlSollwert { get; set; } = "Kühlsollwert :";

    /// <summary><c>GEBK_LBL_KUEHLLEISTUNG_MAX</c></summary>
    public string LabelKuehlleistungMax { get; set; } = "Kühlleistungsgrenze :";

    /// <summary><c>GEBK_VORGABE_KUEHLUNG_AUS</c> — Platzhalter des leeren Kühlsollwerts.</summary>
    public string VorgabeKuehlungAus { get; set; } = "Vorgabe: Kühlung aus";

    /// <summary>
    /// <c>GEBK_ZEILE_KUEHLUNG_AN</c> — „{0}" Mindestabstand [K], „{1}" höchster Heizsollwert
    /// [°C], „{2}" Maximalraumtemperatur [°C].
    /// </summary>
    public string ZeileKuehlungAn { get; set; }
        = "Der Kühlsollwert muss mindestens {0} K über dem höchsten Heizsollwert liegen ({1} °C). "
        + "Ohne Kühlsollwert bleibt die Kühlung aus, und die Überhitzung wird an der "
        + "Maximalraumtemperatur ({2} °C) gezählt.";

    /// <summary><c>GEBK_ZEILE_KUEHLUNG_AUS</c> — „{0}" Maximalraumtemperatur [°C].</summary>
    public string ZeileKuehlungAus { get; set; }
        = "Ohne Haken wird das Gebäude nicht gekühlt: Es läuft frei, und die Überhitzung wird an der Maximalraumtemperatur ({0} °C) gezählt.";

    /// <summary><c>GEBK_ZEILE_KUEHLUNG_PROJEKT</c></summary>
    public string ZeileKuehlungProjekt { get; set; }
        = "Gerechnet wird die Kühlung nur in Projekten mit der Projekteinstellung "
        + "„Kühlung rechnen“ (Simulationskonfiguration).";

    /// <summary><c>GEBK_ZEILE_KUEHLUNG_BESTANDSWEG</c> — bis zur Stufe GA (Löschliste).</summary>
    public string ZeileKuehlungBestandsweg { get; set; }
        = "Tagesbilanz (Bestandsweg) liefert keine Kühllast — diese Eingaben gelten, sobald das "
        + "Gebäude auf VDI 6007 rechnet.";

    /// <summary><c>GEBK_MSG_KUEHLSOLLWERT_BEREICH</c> — „{0}" kleinster, „{1}" größter Wert [°C].</summary>
    public string MeldungKuehlsollwertBereich { get; set; }
        = "Der Kühlsollwert muss zwischen {0} und {1} °C liegen.";

    /// <summary>
    /// <c>GEBK_MSG_KUEHLSOLLWERT_HEIZUNG</c> — „{0}" Kühlsollwert, „{1}" höchster Heizsollwert
    /// [°C], „{2}" Mindestabstand [K].
    /// </summary>
    public string MeldungKuehlsollwertHeizung { get; set; }
        = "Der Kühlsollwert {0} °C liegt nicht mindestens {2} K über dem höchsten Heizsollwert "
        + "{1} °C — Heizung und Kühlung arbeiteten gegeneinander.";

    /// <summary><c>GEBK_MSG_KUEHLLEISTUNG</c></summary>
    public string MeldungKuehlleistung { get; set; } = "Die Kühlleistungsgrenze muss größer als 0 sein.";

    // ------------------------------------------------------------ Schreibweg

    /// <summary><c>GEBK_HINWEIS_SPEICHERN_UNTER</c></summary>
    public string HinweisSpeichernUnter { get; set; }
        = "„Speichern unter“ legt einen neuen Katalogsatz unter dem eingegebenen Namen an "
        + "und schließt wie OK.";

    // ------------------------------------------------------------ Prüfregeln (Konzept 4.8)

    /// <summary><c>GEBK_MSG_UNGUELTIG</c> — „{0}" ist der Feldname.</summary>
    public string MeldungUngueltig { get; set; } = "Der Wert im Feld „{0}“ ist ungültig.";

    /// <summary><c>GEBK_MSG_NUTZFLAECHE</c></summary>
    public string MeldungNutzflaeche { get; set; } = "Die Nutzfläche muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_FLAECHE_NUTZER</c></summary>
    public string MeldungFlaecheNutzer { get; set; }
        = "Die Fläche je Nutzer muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_RAUMHOEHE</c></summary>
    public string MeldungRaumhoehe { get; set; } = "Die Raumhöhe muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_LUFTWECHSEL</c></summary>
    public string MeldungLuftwechsel { get; set; } = "Die Luftwechselrate muss größer als 0 sein.";

    /// <summary><c>GEBK_MSG_G_WERT</c></summary>
    public string MeldungGWert { get; set; }
        = "Der Fensterdurchlaßgrad muss größer als 0 und höchstens 1 sein.";

    /// <summary><c>GEBK_MSG_U_BEREICH</c> — „{0}" ist das Bauteil.</summary>
    public string MeldungUBereich { get; set; }
        = "Der U-Wert {0} muss zwischen 0,1 und 6 W/(m²K) liegen.";

    /// <summary><c>GEBK_MSG_BAUWEISE</c></summary>
    public string MeldungBauweise { get; set; }
        = "Die Bauweise muss zwischen 5 und 200 Wh/(m²K) je m² Nutzfläche liegen.";

    /// <summary><c>GEBK_MSG_RREST</c></summary>
    public string MeldungRRest { get; set; }
        = "Die Bauteile ergeben keinen positiven Restwiderstand; das mittlere U der opaken "
        + "Bauteile liegt über 4,17 W/(m²K).";

    /// <summary><c>GEBK_MSG_OST_WEST</c></summary>
    public string MeldungOstWest { get; set; }
        = "Fensterfläche Ost und West bitte beide eingeben oder beide leer lassen.";
}
