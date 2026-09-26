using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// BV-E1 (Konzept 10.2) — die Beschriftungen der Gruppe „Vorlage" der Berichtsseite: Word-Vorlage,
/// die vier Knöpfe, das Menü „…", die Prüfzeile, die Sperrgründe, der Namensdialog von
/// „Neue Vorlage…", die Titel der Überlagerungen „Prüfliste" und „Platzhalterkatalog" und die
/// Rückfälle der erweiterten Startrückfrage — dazu (BV-E2) der Grund an einem Häkchen, dessen
/// Kapitel die Vorlage nicht führt, der am Häkchen „Deckblatt", wenn die Vorlage es selbst trägt, und
/// die leise Zeile, die statt der Häkchen steht.
///
/// <para>Ein BÜNDEL nach der Bauart <see cref="WirtschaftlichkeitSeiteTexte"/> (Hausregel
/// EPOS.UI: ab etwa zehn Anzeigetexten eines, EIN <c>[Parameter]</c>). Es füllt sich SELBST aus
/// <c>MyResource</c> in der Oberflächensprache; ein fehlender Schlüssel fällt auf den deutschen
/// Wortlaut zurück — so zeichnet die Seite auch ohne Gaben. Jede Eigenschaft nennt ihren
/// Ressourcenschlüssel (<c>BK_BER_VORLAGE_*</c>; die zwei Überlagerungstitel teilen den Schlüssel
/// mit dem Titel ihres Dialogs, <c>VF_PRUEF_TITEL</c> und <c>VF_KATALOG_TITEL</c>). Es trägt
/// Beschriftungen, keinen Zustand.</para>
/// </summary>
public sealed class BerichtSeiteVorlagentexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    // ---- Gruppe und Wahl ------------------------------------------------------------

    /// <summary>BK_BER_VORLAGE_GRUPPE — die Überschrift der Gruppe.</summary>
    public string Gruppe { get; set; } = T("BK_BER_VORLAGE_GRUPPE", "Vorlage:");

    /// <summary>BK_BER_VORLAGE_LBL_WORD — die Beschriftung des Auswahlfeldes.</summary>
    public string LabelWord { get; set; } = T("BK_BER_VORLAGE_LBL_WORD", "Word-Vorlage:");

    /// <summary>BK_BER_VORLAGE_LBL_EXCEL — die Beschriftung der Zeile „Excel-Vorlage“ (BV-E7; nur bei Ausgabe Excel oder Beide).</summary>
    public string LabelExcel { get; set; } = T("BK_BER_VORLAGE_LBL_EXCEL", "Excel-Vorlage:");

    /// <summary>BK_BER_VORLAGE_MITGELIEFERT — der Kurztext des Schlosses neben dem Feld.</summary>
    public string Mitgeliefert { get; set; } = T("BK_BER_VORLAGE_MITGELIEFERT",
        "Mitgelieferte Vorlage – nur lesen; eine eigene entsteht über „Neue Vorlage…“.");

    /// <summary>BK_BER_VORLAGE_NICHT_WAEHLBAR — Rückfall, wenn ein gesperrter Eintrag keinen eigenen Grund trägt.</summary>
    public string NichtWaehlbar { get; set; } = T("BK_BER_VORLAGE_NICHT_WAEHLBAR",
        "Diese Vorlage ist nicht wählbar.");

    // ---- Knöpfe -----------------------------------------------------------------------

    /// <summary>BK_BER_VORLAGE_BTN_NEU</summary>
    public string KnopfNeu { get; set; } = T("BK_BER_VORLAGE_BTN_NEU", "Neue Vorlage…");

    /// <summary>BK_BER_VORLAGE_TIP_NEU — Kurztext am freien Knopf.</summary>
    public string KurztextNeu { get; set; } = T("BK_BER_VORLAGE_TIP_NEU",
        "Legt eine Kopie der Standardvorlage im Vorlagenordner an – der erste Schritt zu einer eigenen Vorlage.");

    /// <summary>BK_BER_VORLAGE_BTN_HINZUFUEGEN</summary>
    public string KnopfHinzufuegen { get; set; } = T("BK_BER_VORLAGE_BTN_HINZUFUEGEN", "Hinzufügen…");

    /// <summary>BK_BER_VORLAGE_TIP_HINZUFUEGEN</summary>
    public string KurztextHinzufuegen { get; set; } = T("BK_BER_VORLAGE_TIP_HINZUFUEGEN",
        "Kopiert eine vorhandene Word- oder Excel-Vorlage in den Vorlagenordner und prüft sie.");

    /// <summary>BK_BER_VORLAGE_BTN_PRUEFEN</summary>
    public string KnopfPruefen { get; set; } = T("BK_BER_VORLAGE_BTN_PRUEFEN", "Prüfen");

    /// <summary>BK_BER_VORLAGE_TIP_PRUEFEN</summary>
    public string KurztextPruefen { get; set; } = T("BK_BER_VORLAGE_TIP_PRUEFEN",
        "Prüft die gewählte Vorlage vollständig: Platzhalter, Orte, Formatvorlagen und Bildrahmen.");

    /// <summary>BK_BER_VORLAGE_BTN_PLATZHALTER</summary>
    public string KnopfPlatzhalter { get; set; } = T("BK_BER_VORLAGE_BTN_PLATZHALTER", "Platzhalter…");

    /// <summary>BK_BER_VORLAGE_TIP_PLATZHALTER</summary>
    public string KurztextPlatzhalter { get; set; } = T("BK_BER_VORLAGE_TIP_PLATZHALTER",
        "Zeigt alle Platzhalter, die eine Vorlage tragen kann, mit ihrer Schreibweise.");

    /// <summary>BK_BER_VORLAGE_BTN_MENUE — die Beschriftung des Menüknopfes.</summary>
    public string KnopfMenue { get; set; } = T("BK_BER_VORLAGE_BTN_MENUE", "…");

    /// <summary>BK_BER_VORLAGE_MENUE_KURZTEXT — Name des Menüknopfes für Zeiger und Sprachausgabe.</summary>
    public string MenueKurztext { get; set; } = T("BK_BER_VORLAGE_MENUE_KURZTEXT",
        "Weitere Handlungen zur gewählten Vorlage");

    // ---- Prüfzeile und Sperren ----------------------------------------------------------

    /// <summary>BK_BER_VORLAGE_BTN_ANZEIGEN — „anzeigen" hinter einer Prüfzeile mit Befunden.</summary>
    public string Anzeigen { get; set; } = T("BK_BER_VORLAGE_BTN_ANZEIGEN", "anzeigen");

    /// <summary>BK_BER_VORLAGE_GESPERRT_LAUF — der Grund an jedem Knopf der Gruppe während eines Laufs.</summary>
    public string GesperrtLauf { get; set; } = T("BK_BER_VORLAGE_GESPERRT_LAUF",
        "Während ein Bericht entsteht, bleibt die Vorlage, wie sie ist.");

    /// <summary>BK_BER_VORLAGE_GESPERRT_BESCHAEFTIGT — der Grund, solange eine Handlung der Gruppe läuft.</summary>
    public string GesperrtBeschaeftigt { get; set; } = T("BK_BER_VORLAGE_GESPERRT_BESCHAEFTIGT",
        "Die Vorlage wird gerade bearbeitet – bitte einen Moment.");

    // ---- „Neue Vorlage…" (NamensDialog) ----------------------------------------------------

    /// <summary>BK_BER_VORLAGE_NEU_TITEL — der Titel der Überlagerung.</summary>
    public string NeuTitel { get; set; } = T("BK_BER_VORLAGE_NEU_TITEL", "Neue Vorlage");

    /// <summary>BK_BER_VORLAGE_NEU_FRAGE</summary>
    public string NeuFrage { get; set; } = T("BK_BER_VORLAGE_NEU_FRAGE", "Name der neuen Vorlage:");

    /// <summary>BK_BER_VORLAGE_NEU_HINWEIS — die Zeile über dem Feld.</summary>
    public string NeuHinweis { get; set; } = T("BK_BER_VORLAGE_NEU_HINWEIS",
        "Die neue Vorlage ist eine Kopie der gewählten mitgelieferten Vorlage im Vorlagenordner; bearbeitet wird sie in Word.");

    /// <summary>BK_BER_VORLAGE_NEU_MUSTER — der Titel der Wahl des Musters (Standardvorlage oder Kurzbericht, BV-E5).</summary>
    public string NeuMuster { get; set; } = T("BK_BER_VORLAGE_NEU_MUSTER", "Kopie von:");

    /// <summary>BK_BER_VORLAGE_EXPORT_FRAGE — die Frage des Namensdialogs von „In den Vorlagenordner exportieren…".</summary>
    public string ExportFrage { get; set; } = T("BK_BER_VORLAGE_EXPORT_FRAGE", "Name der Kopie im Vorlagenordner:");

    /// <summary>BK_BER_VORLAGE_EXPORT_HINWEIS — die Zeile über dem Feld beim Export.</summary>
    public string ExportHinweis { get; set; } = T("BK_BER_VORLAGE_EXPORT_HINWEIS",
        "Die Kopie ist bearbeitbar und steht danach unter den eigenen Vorlagen; gewählt bleibt die aktuelle Vorlage.");

    /// <summary>BK_BER_VORLAGE_NEU_LEER — die Meldung bei leerem Namen.</summary>
    public string NeuLeer { get; set; } = T("BK_BER_VORLAGE_NEU_LEER", "Bitte einen Namen für die Vorlage eingeben.");

    /// <summary>ALLG_BTN_OK</summary>
    public string Ok { get; set; } = T("ALLG_BTN_OK", "OK");

    /// <summary>ALLG_BTN_ABBRECHEN</summary>
    public string Abbrechen { get; set; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    // ---- Überlagerungen ------------------------------------------------------------------

    /// <summary>VF_PRUEF_TITEL — der Titel der Überlagerung „Prüfliste" (derselbe Schlüssel wie im Dialog).</summary>
    public string PrueflisteTitel { get; set; } = T("VF_PRUEF_TITEL", "Prüfliste der Vorlage");

    /// <summary>VF_KATALOG_TITEL — der Titel der Überlagerung „Platzhalterkatalog".</summary>
    public string KatalogTitel { get; set; } = T("VF_KATALOG_TITEL", "Platzhalterkatalog");

    // ---- Rückfälle der erweiterten Startrückfrage ------------------------------------------

    /// <summary>BK_BER_VORLAGE_WEG_EIGENE — wenn die Hülle den Weg ohne Beschriftung liefert.</summary>
    public string WegEigene { get; set; } = T("BK_BER_VORLAGE_WEG_EIGENE", "Mit meiner Vorlage");

    /// <summary>BK_BER_VORLAGE_WEG_STANDARD</summary>
    public string WegStandard { get; set; } = T("BK_BER_VORLAGE_WEG_STANDARD", "Mit Standardvorlage");

    /// <summary>BK_BER_VORLAGE_WEG_ABBRECHEN</summary>
    public string WegAbbrechen { get; set; } = T("BK_BER_VORLAGE_WEG_ABBRECHEN", "Abbrechen");

    // ---- BV-E2: die Häkchen folgen der Vorlage (Konzept 10.2, „Häkchen (BV-Q1 c)") ----------

    /// <summary>
    /// BK_BER_VORLAGE_NICHT_ENTHALTEN — der Grund am Häkchen eines Bausteins, dessen Kapitel die
    /// gewählte Vorlage nicht führt (Kurztext des weich gesperrten Eintrags).
    /// </summary>
    public string NichtEnthalten { get; set; } = T("BK_BER_VORLAGE_NICHT_ENTHALTEN",
        "in dieser Vorlage nicht enthalten");

    /// <summary>
    /// BK_BER_VORLAGE_MSG_NICHT_ENTHALTEN — die Meldung nach dem Klick auf ein solches Häkchen;
    /// <c>{0}</c> = Titel des Bausteins.
    /// </summary>
    public string MeldungNichtEnthalten { get; set; } = T("BK_BER_VORLAGE_MSG_NICHT_ENTHALTEN",
        "„{0}“ ist in dieser Vorlage nicht enthalten – das Häkchen bleibt gespeichert und wirkt wieder, sobald eine Vorlage das Kapitel führt.");

    /// <summary>
    /// BK_BER_VORLAGE_INHALT_AUS_VORLAGE — die leise Zeile STATT der Häkchen, wenn die Vorlage nur
    /// Einzelplatzhalter führt (weder <c>{{bericht.inhalt}}</c> noch ein Kapitel) und nur Word entsteht.
    /// </summary>
    public string InhaltAusVorlage { get; set; } = T("BK_BER_VORLAGE_INHALT_AUS_VORLAGE",
        "Den Inhalt bestimmt die Vorlage – sie führt einzelne Platzhalter, aber kein Kapitel.");

    /// <summary>
    /// BK_BER_VORLAGE_DECKBLATT_AUS_VORLAGE — der Grund am Häkchen „Deckblatt", wenn die Vorlage das
    /// Deckblatt selbst aus Platzhaltern trägt (Kurztext des weich gesperrten Eintrags).
    /// </summary>
    public string DeckblattAusVorlage { get; set; } = T("BK_BER_VORLAGE_DECKBLATT_AUS_VORLAGE",
        "Deckblatt kommt aus der Vorlage");

    /// <summary>
    /// BK_BER_VORLAGE_MSG_DECKBLATT_AUS_VORLAGE — die Meldung nach dem Klick auf dieses Häkchen;
    /// <c>{0}</c> = Titel des Bausteins.
    /// </summary>
    public string MeldungDeckblattAusVorlage { get; set; } = T("BK_BER_VORLAGE_MSG_DECKBLATT_AUS_VORLAGE",
        "„{0}“ kommt aus der Vorlage – sie trägt das Deckblatt selbst; das Häkchen bleibt gespeichert und wirkt wieder, sobald eine Vorlage das Kapitel führt.");

    // -----------------------------------------------------------------
    //  Das Ergebnis eines Laufs: Erfolgszeile und einklappbare Hinweise
    // -----------------------------------------------------------------

    /// <summary>BK_BER_STATUS_ERSTELLT — die Erfolgszeile ohne Vorlage; <c>{0}</c> = Dateiname(n).</summary>
    public string ErstelltZeile { get; set; } = T("BK_BER_STATUS_ERSTELLT", "Bericht erstellt: {0}");

    /// <summary>BK_BER_ERG_ERSTELLT_VORLAGE — die Erfolgszeile; <c>{0}</c> = Dateiname(n), <c>{1}</c> = Vorlage.</summary>
    public string ErstelltZeileVorlage { get; set; } = T("BK_BER_ERG_ERSTELLT_VORLAGE",
        "Bericht erstellt: {0} — Vorlage „{1}“");

    /// <summary>BK_BER_ERG_OEFFNEN — der Knopf an der Erfolgszeile (Kurztext: der volle Pfad).</summary>
    public string KnopfOeffnen { get; set; } = T("BK_BER_ERG_OEFFNEN", "Öffnen");

    /// <summary>BK_BER_ERG_HINWEISE_EINER — die Klappzeile mit einem Hinweis.</summary>
    public string HinweiseEiner { get; set; } = T("BK_BER_ERG_HINWEISE_EINER", "1 Hinweis zum Bericht");

    /// <summary>BK_BER_ERG_HINWEISE_MEHRERE — die Klappzeile; <c>{0}</c> = Zahl der Hinweise.</summary>
    public string HinweiseMehrere { get; set; } = T("BK_BER_ERG_HINWEISE_MEHRERE", "{0} Hinweise zum Bericht");

    /// <summary>BK_BER_ERG_ANZEIGEN — rechts in der eingeklappten Zeile.</summary>
    public string HinweiseAnzeigen { get; set; } = T("BK_BER_ERG_ANZEIGEN", "anzeigen");

    /// <summary>BK_BER_ERG_AUSBLENDEN — rechts in der aufgeklappten Zeile.</summary>
    public string HinweiseAusblenden { get; set; } = T("BK_BER_ERG_AUSBLENDEN", "ausblenden");
}

