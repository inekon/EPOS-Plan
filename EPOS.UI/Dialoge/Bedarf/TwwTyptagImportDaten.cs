namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der Stand der eingespielten Typtage für die Oberfläche</b> (Umsetzungskonzept
/// Zapfprofilgenerator 4.2, Kapitel 6; Stufe Z4b, Gruppe 2) — die Anzeige des Importdialogs und
/// die Grundlage der Wahl im Zapfprofil-Dialog.
///
/// <para><b>Nur Beschreibung, kein Wert der Richtlinie</b>: Quelle, Ausgabe und Tag des
/// Einspielens sind Angaben des Pakets; Zonen, Gebäudearten, Typtagcodes und Zeitraster sagen,
/// was eingespielt IST — nie, wie groß ein Faktor ist. Die Zeilen selbst bleiben in der Datenbank
/// des Anwenders.</para>
/// </summary>
public sealed class TwwTyptagStandDaten
{
    /// <summary>Sind Typtage eingespielt?</summary>
    public bool Vorhanden { get; set; }

    /// <summary>Die Zahl der eingespielten Zeilen.</summary>
    public int Zeilen { get; set; }

    /// <summary>Die Quelle, die das Paket nennt.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Die Ausgabe, die das Paket nennt; leer = ohne Angabe.</summary>
    public string Ausgabe { get; set; } = "";

    /// <summary>Der Tag des Einspielens (ISO).</summary>
    public string DatumImport { get; set; } = "";

    /// <summary>Die Klimazonen des Pakets.</summary>
    public List<int> Klimazonen { get; set; } = new();

    /// <summary>Die Gebäudearten des Pakets.</summary>
    public List<string> Gebaeudearten { get; set; } = new();

    /// <summary>Die Typtagcodes des Pakets.</summary>
    public List<string> Typtage { get; set; } = new();

    /// <summary>Die Zeitraster der Tagesgänge [min]; leer = das Paket führt keine.</summary>
    public List<int> AufloesungenMin { get; set; } = new();

    /// <summary>Führt das Paket Tagesgänge?</summary>
    public bool MitTagesgaenge { get; set; }

    /// <summary>
    /// Warum der Typtagweg nicht verfügbar ist (keine Tabelle, nichts eingespielt); leer =
    /// verfügbar. Der Grund steht als Kurztext am gesperrten Schalter — nie eine stille Sperre.
    /// </summary>
    public string Grund { get; set; } = "";
}

/// <summary>
/// <b>Der Bericht der Prüfung eines Pakets</b> — was ein Einspielen ablegen würde, oder der
/// benannte Grund, aus dem das Paket nicht taugt (mit Datei und Zeile). Geprüft wird OHNE
/// Schreibzugriff: Der eingespielte Stand bleibt, bis „Einspielen" gedrückt ist.
/// </summary>
public sealed class TwwTyptagPruefberichtDaten
{
    /// <summary>Ist das Paket abgelehnt? Dann steht der Grund in <see cref="Abbruch"/>.</summary>
    public bool Abgebrochen { get; set; }

    /// <summary>Der Grund der Ablehnung (Datei, Zeile, Spalte) in der Oberflächensprache.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Eine Zeile, die zusammenfasst, was das Paket trägt.</summary>
    public string Zusammenfassung { get; set; } = "";

    /// <summary>Die Angaben des Pakets als Paare „Beschriftung · Wert".</summary>
    public List<TwwTyptagAngabeDaten> Angaben { get; set; } = new();

    /// <summary>Die benannten Hinweise des Lesers.</summary>
    public List<string> Hinweise { get; set; } = new();
}

/// <summary>Eine Zeile der Standanzeige: Beschriftung und Wert (beides schon in der Oberflächensprache).</summary>
/// <param name="Bezeichnung">Die Beschriftung.</param>
/// <param name="Wert">Der Wert als fertiger Text.</param>
public sealed record TwwTyptagAngabeDaten(string Bezeichnung, string Wert);

/// <summary>
/// <b>Was ein Einspielen oder ein Löschen ergeben hat</b>: die Meldung für die Statuszeile, der
/// Stand danach und die Hinweise. <see cref="Ok"/> ist <c>false</c>, wenn nichts geschrieben
/// wurde — dann nennt <see cref="Meldung"/> den Grund, und der frühere Stand ist unverändert.
/// </summary>
public sealed class TwwTyptagErgebnisDaten
{
    /// <summary>Ist geschrieben worden?</summary>
    public bool Ok { get; set; }

    /// <summary>Die Meldung in der Oberflächensprache.</summary>
    public string Meldung { get; set; } = "";

    /// <summary>Der Stand danach.</summary>
    public TwwTyptagStandDaten Stand { get; set; } = new();

    /// <summary>Die benannten Hinweise des Einspielens.</summary>
    public List<string> Hinweise { get; set; } = new();
}

/// <summary>
/// Die Anzeigetexte des Dialogs „VDI-4655-Typtage" (Stufe Z4b, Gruppe 2) — EIN Parameter statt
/// vieler.
/// </summary>
/// <remarks>
/// <para>Hausregel „ab etwa zehn Anzeigetexten ein Bündel" (<c>EPOS.UI/CLAUDE.md</c>):
/// Beschriftungen, kein Zustand. Jede Eigenschaft trägt ihren Ressourcenschlüssel
/// (<c>ZPGT_</c>) im Kommentar; der Vorgabewert ist der deutsche Rückfall und gleicht dem Wert
/// der neutralen Ressource (Wache <c>TwwTyptagImportDialogTests</c>).</para>
/// </remarks>
public sealed class TwwTyptagImportTexte
{
    /// <summary><c>ZPGT_TITEL</c></summary>
    public string Titel { get; set; } = "VDI-4655-Typtage";

    /// <summary><c>ZPGT_HINWEIS_LIZENZ</c></summary>
    public string HinweisLizenz { get; set; } = "Das Programm bringt keine Werte der Richtlinie mit. Die Typtage spielt der lizenzierte Anwender aus seinem eigenen Paket ein; sie bleiben in dieser Datenbank und gehen weder in die Auslieferung noch in einen Projekttransfer.";

    /// <summary><c>ZPGT_HINWEIS_FORMAT</c></summary>
    public string HinweisFormat { get; set; } = "Ein Paket ist ein ZIP-Archiv oder ein Ordner mit sechs CSV-Dateien: Typtage, Klimazonen, Kalendertage je Zone, Faktoren der Tagesenergie, Kennwerte des Verfahrens und wahlfrei Tagesgänge — Kopfzeile mit Spaltennamen, Trenner Semikolon oder Komma, Zahlen mit Punkt.";

    /// <summary><c>ZPGT_GRP_STAND</c></summary>
    public string GruppeStand { get; set; } = "Eingespielter Stand";

    /// <summary><c>ZPGT_GRP_PRUEFUNG</c></summary>
    public string GruppePruefung { get; set; } = "Prüfung des Pakets";

    /// <summary><c>ZPGT_LEER</c></summary>
    public string Leer { get; set; } = "Es sind keine Typtage eingespielt — ohne sie ist der Typtagweg des Jahresgangs nicht verfügbar.";

    /// <summary><c>ZPGT_LBL_QUELLE</c></summary>
    public string LabelQuelle { get; set; } = "Quelle";

    /// <summary><c>ZPGT_LBL_AUSGABE</c></summary>
    public string LabelAusgabe { get; set; } = "Ausgabe";

    /// <summary><c>ZPGT_LBL_DATUM</c></summary>
    public string LabelDatum { get; set; } = "Eingespielt am";

    /// <summary><c>ZPGT_LBL_ZONEN</c></summary>
    public string LabelZonen { get; set; } = "Klimazonen";

    /// <summary><c>ZPGT_LBL_GEBAEUDEARTEN</c></summary>
    public string LabelGebaeudearten { get; set; } = "Gebäudearten";

    /// <summary><c>ZPGT_LBL_TYPTAGE</c></summary>
    public string LabelTyptage { get; set; } = "Typtage";

    /// <summary><c>ZPGT_LBL_AUFLOESUNG</c></summary>
    public string LabelAufloesung { get; set; } = "Zeitraster der Tagesgänge";

    /// <summary><c>ZPGT_LBL_ZEILEN</c></summary>
    public string LabelZeilen { get; set; } = "Zeilen";

    /// <summary><c>ZPGT_SP_ANGABE</c></summary>
    public string SpalteAngabe { get; set; } = "Angabe";

    /// <summary><c>ZPGT_SP_WERT</c></summary>
    public string SpalteWert { get; set; } = "Wert";

    /// <summary><c>ZPGT_WERT_OHNE</c></summary>
    public string WertOhne { get; set; } = "ohne Angabe";

    /// <summary><c>ZPGT_WERT_OHNE_GAENGE</c></summary>
    public string WertOhneGaenge { get; set; } = "keine — die Tagesform kommt aus dem Tagesgangsatz der Zone";

    /// <summary><c>ZPGT_BTN_PAKET</c></summary>
    public string KnopfPaket { get; set; } = "Paket wählen…";

    /// <summary><c>ZPGT_BTN_EINSPIELEN</c></summary>
    public string KnopfEinspielen { get; set; } = "Einspielen";

    /// <summary><c>ZPGT_BTN_LOESCHEN</c></summary>
    public string KnopfLoeschen { get; set; } = "Löschen";

    /// <summary><c>ZPGT_BTN_BEENDEN</c></summary>
    public string KnopfBeenden { get; set; } = "Beenden";

    /// <summary><c>ZPGT_DATEIFILTER</c></summary>
    public string Dateifilter { get; set; } = "Typtagpaket (*.zip;*.csv)|*.zip;*.csv|Alle Dateien (*.*)|*.*";

    /// <summary><c>ZPGT_WAHL_TITEL</c></summary>
    public string WahlTitel { get; set; } = "Paket der Typtage wählen";

    /// <summary><c>ZPGT_KEIN_PAKET</c></summary>
    public string KeinPaket { get; set; } = "Es ist kein Paket gewählt.";

    /// <summary><c>ZPGT_ABBRUCH</c></summary>
    public string Abbruch { get; set; } = "Das Paket ist abgelehnt: {0}";

    /// <summary><c>ZPGT_ZUSAMMENFASSUNG</c></summary>
    public string Zusammenfassung { get; set; } = "Das Paket trägt {0} Klimazone(n), {1} Gebäudeart(en) und {2} Typtag(e) — {3} Zeile(n).";

    /// <summary><c>ZPGT_HINWEISE</c></summary>
    public string Hinweise { get; set; } = "Hinweise";

    /// <summary><c>ZPGT_FRAGE_ERSETZEN</c></summary>
    public string FrageErsetzen { get; set; } = "Ein Paket ist schon eingespielt ({0} Zeilen). Das neue ersetzt es vollständig. Einspielen?";

    /// <summary><c>ZPGT_FRAGE_LOESCHEN</c></summary>
    public string FrageLoeschen { get; set; } = "Die eingespielten Typtage werden entfernt; danach ist der Typtagweg nicht mehr verfügbar. Löschen?";

    /// <summary><c>ZPGT_MSG_EINGESPIELT</c></summary>
    public string MeldungEingespielt { get; set; } = "{0} Zeile(n) eingespielt, {1} ersetzt.";

    /// <summary><c>ZPGT_MSG_GELOESCHT</c></summary>
    public string MeldungGeloescht { get; set; } = "{0} Zeile(n) entfernt.";

    /// <summary><c>ZPGT_MSG_NICHTS</c></summary>
    public string MeldungNichts { get; set; } = "Es war nichts zu löschen.";
}
