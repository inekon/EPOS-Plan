namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte des Katalogdialogs „Brauchwasser-Nutzungsarten" samt Editor, Vorschau und
/// Katalogimport (Umsetzungskonzept Zapfprofilgenerator 5.4) — EIN Parameter statt vieler.
/// </summary>
/// <remarks>
/// <para>Hausregel „ab etwa zehn Anzeigetexten ein Bündel" (<c>EPOS.UI/CLAUDE.md</c>): Beschriftungen,
/// kein Zustand — der steht in <see cref="TwwNutzungsartDetailDaten"/> und
/// <see cref="TwwNutzungsartEditorDaten"/>. Jede Eigenschaft trägt ihren Ressourcenschlüssel
/// (<c>ZPGK_</c>) im Kommentar; der Vorgabewert ist der deutsche Rückfall und gleicht dem Wert der
/// neutralen Ressource (Wache <c>TwwNutzungsartAdminDialogTests</c>). Platzhalter <c>{0}</c> …
/// füllt der Dialog.</para>
/// </remarks>
public sealed class TwwNutzungsartAdminTexte
{
    // ------------------------------------------------------------ Kopf, Liste und Stammblatt

    /// <summary><c>ZPGK_TITEL</c></summary>
    public string Titel { get; set; } = "Brauchwasser-Nutzungsarten";

    /// <summary><c>ZPGK_LEER</c></summary>
    public string LeerKatalog { get; set; } = "Der Katalog führt noch keine Nutzungsart — „Neu…“ legt eine an, „Import…“ spielt ein Paket ein.";

    /// <summary><c>ZPGK_KZ_BEDARF_MITTEL</c></summary>
    public string KennzahlBedarf { get; set; } = "Bedarf mittel";

    /// <summary><c>ZPGK_EINHEIT_BEDARF</c></summary>
    public string EinheitBedarf { get; set; } = "kWh/(Einheit·d)";

    /// <summary><c>ZPGK_EINHEIT_GRAD</c></summary>
    public string EinheitGrad { get; set; } = "°C";

    /// <summary><c>ZPGK_GRP_KENNWERTE</c></summary>
    public string GruppeKennwerte { get; set; } = "Kennwerte";

    /// <summary><c>ZPGK_GRP_GAENGE</c></summary>
    public string GruppeGaenge { get; set; } = "Jahres- und Wochengang";

    /// <summary><c>ZPGK_GRP_TAGESGANG</c></summary>
    public string GruppeTagesgang { get; set; } = "Tagesgang";

    /// <summary><c>ZPGK_GRP_KATEGORIEN</c></summary>
    public string GruppeKategorien { get; set; } = "Zapfkategorien";

    /// <summary><c>ZPGK_GRP_HERKUNFT</c></summary>
    public string GruppeHerkunft { get; set; } = "Herkunft";

    // ------------------------------------------------------------ Beschriftungen

    /// <summary><c>ZPGK_LBL_BEZEICHNER</c></summary>
    public string LabelBezeichner { get; set; } = "Nutzungsart";

    /// <summary><c>ZPGK_LBL_KATALOGVERSION</c></summary>
    public string LabelKatalogversion { get; set; } = "Katalogversion";

    /// <summary><c>ZPGK_LBL_STATUS</c></summary>
    public string LabelStatus { get; set; } = "Stand";

    /// <summary><c>ZPGK_LBL_BEZUGSART</c></summary>
    public string LabelBezugsart { get; set; } = "Bezugsart";

    /// <summary><c>ZPGK_LBL_BEDARF_NIEDRIG</c></summary>
    public string LabelBedarfNiedrig { get; set; } = "Bedarf niedrig";

    /// <summary><c>ZPGK_LBL_BEDARF_MITTEL</c></summary>
    public string LabelBedarfMittel { get; set; } = "Bedarf mittel";

    /// <summary><c>ZPGK_LBL_BEDARF_HOCH</c></summary>
    public string LabelBedarfHoch { get; set; } = "Bedarf hoch";

    /// <summary><c>ZPGK_LBL_UNTERE_GRENZE</c></summary>
    public string LabelUntereGrenze { get; set; } = "{0} – untere Grenze";

    /// <summary><c>ZPGK_LBL_OBERE_GRENZE</c></summary>
    public string LabelObereGrenze { get; set; } = "{0} – obere Grenze";

    /// <summary><c>ZPGK_LBL_ZAPFTEMPERATUR</c></summary>
    public string LabelZapftemperatur { get; set; } = "Zapftemperatur (Bezug)";

    /// <summary><c>ZPGK_LBL_KALTWASSER</c></summary>
    public string LabelKaltwasser { get; set; } = "Kaltwassertemperatur (Bezug)";

    /// <summary><c>ZPGK_LBL_BILANZGRENZE</c></summary>
    public string LabelBilanzgrenze { get; set; } = "Bilanzgrenze";

    /// <summary><c>ZPGK_LBL_KALENDER</c></summary>
    public string LabelKalender { get; set; } = "Kalender";

    /// <summary><c>ZPGK_LBL_FERIENFAKTOR</c></summary>
    public string LabelFerienfaktor { get; set; } = "Ferienfaktor";

    /// <summary><c>ZPGK_LBL_MONATSFAKTOREN</c></summary>
    public string LabelMonatsfaktoren { get; set; } = "Monatsfaktoren (Mittel 1)";

    /// <summary><c>ZPGK_LBL_WOCHENFAKTOREN</c></summary>
    public string LabelWochenfaktoren { get; set; } = "Wochenfaktoren Mo–So (Summe 1)";

    /// <summary><c>ZPGK_LBL_TAGESGANGSATZ</c></summary>
    public string LabelTagesgangsatz { get; set; } = "Tagesgangsatz";

    /// <summary><c>ZPGK_LBL_VORLAGE</c></summary>
    public string LabelVorlage { get; set; } = "Vorlage";

    /// <summary><c>ZPGK_LBL_VERWENDET</c></summary>
    public string LabelVerwendet { get; set; } = "Verwendet in";

    /// <summary><c>ZPGK_LBL_QUELLE_BEDARF</c></summary>
    public string LabelQuelleBedarf { get; set; } = "Herkunft Bedarf";

    /// <summary><c>ZPGK_LBL_QUELLE_JAHRESGANG</c></summary>
    public string LabelQuelleJahresgang { get; set; } = "Herkunft Jahresgang";

    /// <summary><c>ZPGK_LBL_QUELLE_WOCHENGANG</c></summary>
    public string LabelQuelleWochengang { get; set; } = "Herkunft Wochengang";

    /// <summary><c>ZPGK_LBL_QUELLE_TAGESGANG</c></summary>
    public string LabelQuelleTagesgang { get; set; } = "Herkunft Tagesgang {0}";

    /// <summary><c>ZPGK_WERT_BANDBREITE</c></summary>
    public string WertBandbreite { get; set; } = "{0} ({1} … {2})";

    /// <summary><c>ZPGK_WERT_KATEGORIE</c></summary>
    public string WertKategorie { get; set; } = "{0} l/min · σ {1} l/min · {2} min · Anteil {3}";

    /// <summary><c>ZPGK_WERT_KAPPUNG</c></summary>
    public string WertKappung { get; set; } = "{0} · höchstens {1} l/min";

    /// <summary><c>ZPGK_WERT_KEINE_KATEGORIEN</c></summary>
    public string WertKeineKategorien { get; set; } = "keine — die Nutzungsart rechnet nur deterministisch";

    /// <summary><c>ZPGK_WERT_SATZ_VOLLSTAENDIG</c></summary>
    public string WertSatzVollstaendig { get; set; } = "alle vier Tagtypen";

    /// <summary><c>ZPGK_WERT_SATZ_UNVOLLSTAENDIG</c></summary>
    public string WertSatzUnvollstaendig { get; set; } = "unvollständig — nicht alle vier Tagtypen";

    /// <summary><c>ZPGK_WERT_NIRGENDS</c></summary>
    public string WertNirgends { get; set; } = "in keinem Projekt";

    /// <summary><c>ZPGK_WERT_OHNE_VORLAGE</c></summary>
    public string WertOhneVorlage { get; set; } = "keine";

    // ------------------------------------------------------------ Knöpfe

    /// <summary><c>ZPGK_BTN_AENDERN</c></summary>
    public string KnopfAendern { get; set; } = "Ändern…";

    /// <summary><c>ZPGK_BTN_TAGESGANG</c></summary>
    public string KnopfTagesgang { get; set; } = "Tagesgang…";

    /// <summary><c>ZPGK_BTN_GRAFIK</c></summary>
    public string KnopfGrafik { get; set; } = "Grafik…";

    /// <summary><c>ZPGK_BTN_KATEGORIEN</c></summary>
    public string KnopfKategorien { get; set; } = "Kategorien…";

    /// <summary><c>ZPGK_BTN_NEU</c></summary>
    public string KnopfNeu { get; set; } = "Neu…";

    /// <summary><c>ZPGK_BTN_SPEICHERN_UNTER</c></summary>
    public string KnopfSpeichernUnter { get; set; } = "Speichern unter…";

    /// <summary><c>ZPGK_BTN_LOESCHEN</c></summary>
    public string KnopfLoeschen { get; set; } = "Löschen";

    /// <summary><c>ZPGK_BTN_TYPTAGE</c></summary>
    public string KnopfTyptage { get; set; } = "VDI-4655-Typtage…";

    /// <summary><c>ZPGK_BTN_IMPORT</c></summary>
    public string KnopfImport { get; set; } = "Import…";

    /// <summary><c>ZPGK_BTN_BEENDEN</c></summary>
    public string KnopfBeenden { get; set; } = "Beenden";

    // ------------------------------------------------------------ Sperrgründe und Meldungen

    /// <summary><c>ZPGK_GRUND_AUSLIEFERUNG</c></summary>
    public string GrundAuslieferung { get; set; } = "Die Nutzungsart gehört zur Auslieferung und ist unveränderlich — „Speichern unter…“ legt eine eigene Version an.";

    /// <summary><c>ZPGK_GRUND_BENUTZT</c></summary>
    public string GrundBenutzt { get; set; } = "Die Nutzungsart ist in {0} benutzt und damit unveränderlich — „Speichern unter…“ legt eine neue Version an.";

    /// <summary><c>ZPGK_GRUND_LOESCHEN_AUSLIEFERUNG</c></summary>
    public string GrundLoeschenAuslieferung { get; set; } = "Eine Nutzungsart der Auslieferung wird nicht gelöscht.";

    /// <summary><c>ZPGK_GRUND_LOESCHEN_BENUTZT</c></summary>
    public string GrundLoeschenBenutzt { get; set; } = "In {0} benutzt — nicht löschbar.";

    /// <summary><c>ZPGK_GRUND_KEINE_WAHL</c></summary>
    public string GrundKeineWahl { get; set; } = "Bitte zuerst eine Nutzungsart wählen.";

    /// <summary><c>ZPGK_GRUND_OHNE_WEG</c></summary>
    public string GrundOhneWeg { get; set; } = "In dieser Umgebung nicht verfügbar.";

    /// <summary><c>ZPGK_FRAGE_LOESCHEN</c></summary>
    public string FrageLoeschen { get; set; } = "Soll die Nutzungsart „{0}“ gelöscht werden?";

    /// <summary><c>ZPGK_MSG_GELOESCHT</c></summary>
    public string MeldungGeloescht { get; set; } = "„{0}“ ist gelöscht.";

    /// <summary><c>ZPGK_MSG_GESPEICHERT</c></summary>
    public string MeldungGespeichert { get; set; } = "„{0}“ ist gespeichert.";

    /// <summary><c>ZPGK_MSG_TAGESGANG</c></summary>
    public string MeldungTagesgang { get; set; } = "Der Tagesgang von „{0}“ ist gespeichert.";

    /// <summary><c>ZPGK_MSG_KATEGORIEN</c></summary>
    public string MeldungKategorien { get; set; } = "Die Zapfkategorien von „{0}“ sind gespeichert.";

    /// <summary><c>ZPGK_MSG_IMPORTIERT</c></summary>
    public string MeldungImportiert { get; set; } = "Import: {0}";

    // ------------------------------------------------------------ Vorschau

    /// <summary><c>ZPGK_GRAFIK_TITEL</c></summary>
    public string GrafikTitel { get; set; } = "Vorschau der Nutzungsart";

    /// <summary><c>ZPGK_GRAFIK_HINWEIS</c></summary>
    public string GrafikHinweis { get; set; } = "Die Vorschau zeigt eine Einheit der Bezugsart am mittleren Bedarfsniveau; Wochengang, Kalender, Ferien und Zirkulation wirken erst in der Rechnung des Projekts.";

    /// <summary><c>ZPGK_KEIN_BILD</c></summary>
    public string KeinBild { get; set; } = "Keine Vorschau — der Tagesgangsatz ist unvollständig.";

    /// <summary><c>ZPGK_BILD_TAGESGANG</c></summary>
    public string BildTagesgang { get; set; } = "Tagesgang je Einheit, Bedarf mittel";

    /// <summary><c>ZPGK_BILD_JAHRESGANG</c></summary>
    public string BildJahresgang { get; set; } = "Monatsmengen je Einheit, Bedarf mittel";

    // ------------------------------------------------------------ Import

    /// <summary><c>ZPGK_IMPORT_TITEL</c></summary>
    public string ImportTitel { get; set; } = "Katalog importieren";

    /// <summary><c>ZPGK_IMPORT_HINWEIS</c></summary>
    public string ImportHinweis { get; set; } = "Ein Paket besteht aus den Dateien Tab_TwwTagesgangsatz_STAMM.csv, Tab_TwwTagesgang_STAMM.csv, Tab_TwwNutzungsart_STAMM.csv und Tab_TwwZapfkategorie_STAMM.csv, wahlfrei dazu Tab_TwwBedarfstag_STAMM.csv, Tab_TwwBedarfstagEreignis_STAMM.csv und Tab_TwwParameter_STAMM.csv — je Tabelle eine CSV-Datei mit Kopfzeile, Trenner Semikolon oder Komma, Zahlen mit Punkt. Gewählt wird ein ZIP-Archiv oder eine Datei des Paketordners.";

    /// <summary><c>ZPGK_IMPORT_HERKUNFT</c></summary>
    public string ImportHerkunft { get; set; } = "Eingespielte Zeilen tragen den Stand „Import“ und die Herkunftsart „Import“; frei verfügbare und fiktive Werte behalten ihre Herkunftsart. Eine vorhandene Nutzungsart bleibt unverändert: Gleicher Inhalt wird übersprungen, abweichender kommt als eigene Version „(Import n)“. Ein Bedarfstag und ein Parameter werden dagegen ersetzt — auch eine Zeile der Auslieferung; der Bericht nennt jede Ersetzung.";

    /// <summary><c>ZPGK_IMPORT_DATEI</c></summary>
    public string ImportDatei { get; set; } = "Paket wählen…";

    /// <summary><c>ZPGK_IMPORT_DATEIFILTER</c></summary>
    public string ImportDateifilter { get; set; } = "Katalogpaket (*.zip;*.csv)|*.zip;*.csv";

    /// <summary><c>ZPGK_IMPORT_STARTEN</c></summary>
    public string ImportStarten { get; set; } = "Importieren";

    /// <summary><c>ZPGK_IMPORT_KEIN_PAKET</c></summary>
    public string ImportKeinPaket { get; set; } = "Bitte zuerst ein Paket wählen.";

    /// <summary><c>ZPGK_IMPORT_ZUSAMMENFASSUNG</c></summary>
    public string ImportZusammenfassung { get; set; } = "{0} angelegt · {1} ersetzt · {2} übersprungen · {3} abgelehnt";

    /// <summary><c>ZPGK_IMPORT_ANGELEGT</c></summary>
    public string ImportAngelegt { get; set; } = "angelegt";

    /// <summary><c>ZPGK_IMPORT_UEBERSPRUNGEN</c></summary>
    public string ImportUebersprungen { get; set; } = "übersprungen";

    /// <summary><c>ZPGK_IMPORT_ABGELEHNT</c></summary>
    public string ImportAbgelehnt { get; set; } = "abgelehnt";

    /// <summary><c>ZPGK_IMPORT_ZEILE</c></summary>
    public string ImportZeile { get; set; } = "Zeile {0}";

    /// <summary><c>ZPGK_IMPORT_SP_NUTZUNGSART</c></summary>
    public string ImportSpalteNutzungsart { get; set; } = "Nutzungsart";

    /// <summary><c>ZPGK_IMPORT_SP_ERGEBNIS</c></summary>
    public string ImportSpalteErgebnis { get; set; } = "Ergebnis";

    /// <summary><c>ZPGK_IMPORT_SP_GRUND</c></summary>
    public string ImportSpalteGrund { get; set; } = "Grund";

    /// <summary><c>ZPGK_IMPORT_ABBRUCH</c></summary>
    public string ImportAbbruch { get; set; } = "Das Paket ist abgelehnt — {0}";

    /// <summary><c>ZPGK_IMPORT_HINWEISE</c></summary>
    public string ImportHinweise { get; set; } = "Hinweise";

    /// <summary><c>ZPGK_IMPORT_ERSETZT</c></summary>
    public string ImportErsetzt { get; set; } = "ersetzt";

    /// <summary><c>ZPGK_IMPORT_W_ANGELEGT</c></summary>
    public string ImportWuerdeAnlegen { get; set; } = "würde anlegen";

    /// <summary><c>ZPGK_IMPORT_W_ERSETZT</c></summary>
    public string ImportWuerdeErsetzen { get; set; } = "würde ersetzen";

    /// <summary><c>ZPGK_IMPORT_W_UEBERSPRUNGEN</c></summary>
    public string ImportWuerdeUeberspringen { get; set; } = "würde überspringen";

    /// <summary><c>ZPGK_IMPORT_W_ABGELEHNT</c></summary>
    public string ImportWuerdeAblehnen { get; set; } = "würde ablehnen";

    /// <summary><c>ZPGK_IMPORT_GRP_BEDARFSTAGE</c></summary>
    public string ImportGruppeBedarfstage { get; set; } = "Bedarfstage";

    /// <summary><c>ZPGK_IMPORT_GRP_PARAMETER</c></summary>
    public string ImportGruppeParameter { get; set; } = "Parameter";

    /// <summary><c>ZPGK_IMPORT_GRP_NUTZUNGSARTEN</c></summary>
    public string ImportGruppeNutzungsarten { get; set; } = "Nutzungsarten";

    /// <summary><c>ZPGK_IMPORT_SP_BEDARFSTAG</c></summary>
    public string ImportSpalteBedarfstag { get; set; } = "Bedarfstag";

    /// <summary><c>ZPGK_IMPORT_SP_PARAMETER</c></summary>
    public string ImportSpalteParameter { get; set; } = "Schlüssel";

    /// <summary><c>ZPGK_IMPORT_PRUEFEN</c></summary>
    public string ImportPruefen { get; set; } = "Nur prüfen, nichts schreiben";

    /// <summary><c>ZPGK_IMPORT_PRUEFHINWEIS</c></summary>
    public string ImportPruefhinweis { get; set; } = "Der Prüflauf ändert nichts; die Spalte „Ergebnis“ sagt, was ein Import täte.";

    // ------------------------------------------------------------ Editor

    /// <summary><c>ZPGK_ED_TITEL_NEU</c></summary>
    public string EditorTitelNeu { get; set; } = "Neue Nutzungsart";

    /// <summary><c>ZPGK_ED_TITEL_AENDERN</c></summary>
    public string EditorTitelAendern { get; set; } = "Nutzungsart ändern";

    /// <summary><c>ZPGK_ED_TITEL_SPEICHERN_UNTER</c></summary>
    public string EditorTitelSpeichernUnter { get; set; } = "Nutzungsart speichern unter";

    /// <summary><c>ZPGK_ED_HINWEIS_NEU</c></summary>
    public string EditorHinweisNeu { get; set; } = "Vorbelegt sind die Werte der gewählten Nutzungsart; die neue Zeile trägt den Stand „eigen“, ihre Wertgruppen die Herkunft Eigenkonstruktion.";

    /// <summary><c>ZPGK_ED_HINWEIS_AENDERN</c></summary>
    public string EditorHinweisAendern { get; set; } = "Die Zeile wird an Ort und Stelle geändert; geänderte Wertgruppen tragen danach die Herkunft Eigenkonstruktion.";

    /// <summary><c>ZPGK_ED_HINWEIS_SPEICHERN_UNTER</c></summary>
    public string EditorHinweisSpeichernUnter { get; set; } = "Die Werte werden als neue Zeile mit eigener Katalogversion gespeichert; die Vorlage bleibt unverändert.";

    /// <summary><c>ZPGK_ED_GRP_KENNUNG</c></summary>
    public string EditorGruppeKennung { get; set; } = "Kennung";

    /// <summary><c>ZPGK_ED_GRP_BEDARF</c></summary>
    public string EditorGruppeBedarf { get; set; } = "Bedarf je Einheit und Tag";

    /// <summary><c>ZPGK_ED_GRP_ZEIT</c></summary>
    public string EditorGruppeZeit { get; set; } = "Kalender und Jahresgang";

    /// <summary><c>ZPGK_ED_WOCHE</c></summary>
    public string EditorWoche { get; set; } = "Wochenfaktoren: {0} — sie werden über „Tagesgang…“ bearbeitet.";

    /// <summary><c>ZPGK_ED_MONATE_MITTEL</c></summary>
    public string EditorMonateMittel { get; set; } = "Mittel der Monatsfaktoren {0} — beim Speichern auf 1 normiert.";

    /// <summary><c>ZPGK_ED_MONATE_EINS</c></summary>
    public string EditorMonateEins { get; set; } = "Die Monatsfaktoren haben das Mittel 1.";

    /// <summary><c>ZPGK_ED_PFLICHT</c></summary>
    public string EditorPflicht { get; set; } = "Es fehlen Angaben: {0}";

    /// <summary><c>ZPGK_ED_FEHLEINGABE</c></summary>
    public string EditorFehleingabe { get; set; } = "Bitte die markierten Eingaben berichtigen: {0}";

    /// <summary><c>ZPGK_ED_KEINE_WAHL</c></summary>
    public string EditorKeineWahl { get; set; } = "— bitte wählen —";

    /// <summary><c>ZPGK_ED_NICHT_GESPEICHERT</c></summary>
    public string EditorNichtGespeichert { get; set; } = "Die Nutzungsart wurde nicht gespeichert — {0}";

    /// <summary><c>ZPGK_ED_NICHT_GELOESCHT</c></summary>
    public string EditorNichtGeloescht { get; set; } = "Die Nutzungsart wurde nicht gelöscht — {0}";
}
