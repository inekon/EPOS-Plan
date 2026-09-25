namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Kennungen der beiden Wahlen des Messdaten-Dialogs</b> (Stufe Z5, Gruppe 3) — gemessene
/// Größe und Zeitrechnung der Zeitstempel.
///
/// <para>Die Oberfläche kennt <b>keinen Aufzählungstyp des Kerns</b> (Hausregel
/// <c>EPOS.UI/CLAUDE.md</c>): Die Zahlen stehen hier, die Hülle bildet sie auf
/// <c>ZapfMessgroesse</c> und <c>Messzeitstempel</c> ab — und ein Fall des Hüllentests hält beide
/// Seiten gleich (<c>ZapfprofilHuelleMessreihenTests</c>). <see cref="GroesseAusKopf"/> ist
/// <b>kein</b> Wert der Ablage: Er sagt „aus der Kopfzeile lesen“.</para>
/// </summary>
public static class TwwMessreihenwahl
{
    /// <summary>Die gemessene Größe kommt aus der Einheit der Kopfzeile (Vorgabe).</summary>
    public const int GroesseAusKopf = 0;

    /// <summary>Energie je Zeitschritt [kWh].</summary>
    public const int GroesseEnergie = 1;

    /// <summary>Volumen je Zeitschritt [m³].</summary>
    public const int GroesseVolumen = 2;

    /// <summary>Mittlere Leistung im Zeitschritt [kW].</summary>
    public const int GroesseLeistung = 3;

    /// <summary>Ortszeit mit Sommerzeitumstellung (Vorgabe).</summary>
    public const int ZeitOrtszeit = 0;

    /// <summary>Normalzeit — keine Umstellung erwartet.</summary>
    public const int ZeitNormalzeit = 1;
}

/// <summary>
/// <b>Eine eingespielte Messreihe für die Liste</b> (Umsetzungskonzept Zapfprofilgenerator 4.8,
/// Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3) — der Kopf, wie ihn eine Zeile braucht: schon
/// formatierte Texte, keine Fachklasse des Kerns und <b>kein Wert der Messung</b> außer der Menge,
/// die das Objekt selbst beschreibt und deshalb nur in dieser Datenbank steht.
/// </summary>
public sealed class TwwMessreiheDaten
{
    /// <summary>Die Bezeichnung — der Schlüssel der Reihe im Projekt.</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Die gemessene Größe als Text („Energie", „Volumen", „Leistung").</summary>
    public string Groesse { get; set; } = "";

    /// <summary>Die gemessene Größe als Kennung der Ablage (für die Wahl im Dialog).</summary>
    public int GroesseId { get; set; }

    /// <summary>Das Zeitraster [min].</summary>
    public int AufloesungMin { get; set; }

    /// <summary>Der Beginn (ISO, wie die Ablage ihn führt).</summary>
    public string Beginn { get; set; } = "";

    /// <summary>Die Zahl der Zeitschritte.</summary>
    public int Schritte { get; set; }

    /// <summary>Die Länge der Reihe in Tagen [d] — auch gebrochen.</summary>
    public double Tage { get; set; }

    /// <summary>Die Zahl der Nullläufe (gefüllte Lücke ODER Stunde ohne Zapfung).</summary>
    public int Nulllaeufe { get; set; }

    /// <summary>Der Anteil der Nullläufe an den Zeitschritten [-].</summary>
    public double Nullanteil { get; set; }

    /// <summary>Woher die Reihe kommt (Anwenderangabe oder Dateiname).</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Der Tag des Einspielens (ISO).</summary>
    public string DatumImport { get; set; } = "";
}

/// <summary>
/// <b>Der Stand der Messreihen eines Projekts</b>: die Reihen, die Vorgaben der Eingaben und —
/// wenn es keine geben kann — der benannte Grund.
///
/// <para><b>Sie gehören zum Projekt</b> (Konzept Kapitel 9 K5): Die Zeilen tragen die
/// Projekt-Id, reisen mit einer Projektkopie und einem <c>.wpx</c>-Paket und stehen in <b>keiner</b>
/// Auslieferungsvorlage. Der Dialog sagt das als Herleitungszeile.</para>
/// </summary>
public sealed class TwwMessreihenstandDaten
{
    /// <summary>Führt die Datenbank die Tabelle der Messreihen?</summary>
    public bool TabelleDa { get; set; }

    /// <summary>Die eingespielten Reihen, nach Bezeichnung geordnet.</summary>
    public List<TwwMessreiheDaten> Reihen { get; set; } = new();

    /// <summary>
    /// Warum es keine Reihe gibt (keine Tabelle, nichts eingespielt, kein gespeichertes Projekt);
    /// leer = es stehen Reihen da. Der Grund steht als leise Zeile, nie als stille Leere.
    /// </summary>
    public string Grund { get; set; } = "";

    /// <summary>Die Vorgabe der Lückenschwelle [-] aus dem Parametersatz.</summary>
    public double LueckenschwelleVorgabe { get; set; } = 0.05;

    /// <summary>Die kürzeste Reihe für einen Kalibriervorschlag [d] aus dem Parametersatz.</summary>
    public int MindesttageVorschlag { get; set; } = 30;
}

/// <summary>
/// <b>Was der Anwender zum Einlesen mitgibt</b> (Stufe Z5, Gruppe 3) — der Arbeitsstand der
/// Eingaben über der Dateiwahl. Er wird bei jedem Wechsel neu geprüft; geschrieben wird erst mit
/// „Einspielen".
/// </summary>
public sealed class TwwMessreiheneingabeDaten
{
    /// <summary>Die Bezeichnung der Reihe; leer = der Dateiname.</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Die Quelle (Zähler, Objekt, Zeitraum); leer = der Dateiname.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>
    /// Die gemessene Größe; <c>null</c> = aus der Einheit der Kopfzeile lesen. Trägt der Kopf keine
    /// Einheit, lehnt der Leser benannt ab — dann ist dieses Feld die Antwort.
    /// </summary>
    public int? GroesseId { get; set; }

    /// <summary>Der höchste zugelassene Anteil gefüllter Lücken [-]; <c>null</c> = die Vorgabe.</summary>
    public double? LueckenschwelleAnteil { get; set; }

    /// <summary>Sind die Zeitstempel Ortszeit (mit Sommerzeit) oder Normalzeit?</summary>
    public int ZeitstempelId { get; set; }

    /// <summary>Eine Tiefenkopie — der Dialog führt einen Arbeitsstand.</summary>
    public TwwMessreiheneingabeDaten Kopie() => new()
    {
        Bezeichnung = Bezeichnung,
        Quelle = Quelle,
        GroesseId = GroesseId,
        LueckenschwelleAnteil = LueckenschwelleAnteil,
        ZeitstempelId = ZeitstempelId
    };
}

/// <summary>
/// <b>Der Bericht der Prüfung einer Datei</b> — was ein Einspielen ablegen würde, oder der
/// benannte Grund, aus dem die Datei nicht taugt (mit Datei und Zeile). Geprüft wird <b>ohne
/// Schreibzugriff</b>: Der eingespielte Stand bleibt, bis „Einspielen" gedrückt ist.
/// </summary>
public sealed class TwwMessreihenpruefungDaten
{
    /// <summary>Ist die Datei abgelehnt? Dann steht der Grund in <see cref="Abbruch"/>.</summary>
    public bool Abgebrochen { get; set; }

    /// <summary>Der Grund der Ablehnung (Datei, Zeile, Spalte) in der Oberflächensprache.</summary>
    public string Abbruch { get; set; } = "";

    /// <summary>Eine Zeile, die zusammenfasst, was die Datei trägt.</summary>
    public string Zusammenfassung { get; set; } = "";

    /// <summary>Die Angaben der gelesenen Reihe als Paare „Beschriftung · Wert".</summary>
    public List<TwwMessreihenangabeDaten> Angaben { get; set; } = new();

    /// <summary>Die benannten Hinweise des Lesers (Lücken, Sommerzeit, Schalttag).</summary>
    public List<string> Hinweise { get; set; } = new();

    /// <summary>Die Bezeichnung, die die gelesene Reihe tragen würde (Vorschlag für das Feld).</summary>
    public string Bezeichnung { get; set; } = "";

    /// <summary>Trägt das Projekt schon eine Reihe dieser Bezeichnung? Dann fragt „Einspielen" zurück.</summary>
    public bool ErsetztVorhandene { get; set; }
}

/// <summary>Eine Zeile einer Angabenliste: Beschriftung und Wert, beides schon in der Oberflächensprache.</summary>
/// <param name="Bezeichnung">Die Beschriftung.</param>
/// <param name="Wert">Der Wert als fertiger Text.</param>
public sealed record TwwMessreihenangabeDaten(string Bezeichnung, string Wert);

/// <summary>
/// <b>Was ein Einspielen oder ein Löschen ergeben hat</b>: die Meldung für die Statuszeile, der
/// Stand danach und die Hinweise. <see cref="Ok"/> ist <c>false</c>, wenn nichts geschrieben wurde
/// — dann nennt <see cref="Meldung"/> den Grund, und der frühere Stand ist unverändert.
/// </summary>
public sealed class TwwMessreihenergebnisDaten
{
    /// <summary>Ist geschrieben worden?</summary>
    public bool Ok { get; set; }

    /// <summary>Die Meldung in der Oberflächensprache.</summary>
    public string Meldung { get; set; } = "";

    /// <summary>Der Stand danach.</summary>
    public TwwMessreihenstandDaten Stand { get; set; } = new();

    /// <summary>Die benannten Hinweise des Einspielens.</summary>
    public List<string> Hinweise { get; set; } = new();

    /// <summary>Die Bezeichnung der geschriebenen Reihe; leer = nichts geschrieben.</summary>
    public string Bezeichnung { get; set; } = "";
}

/// <summary>
/// Die Anzeigetexte des Dialogs „Messdaten" (Stufe Z5, Gruppe 3) — EIN Parameter statt vieler.
/// </summary>
/// <remarks>
/// <para>Hausregel „ab etwa zehn Anzeigetexten ein Bündel" (<c>EPOS.UI/CLAUDE.md</c>):
/// Beschriftungen, kein Zustand. Jede Eigenschaft trägt ihren Ressourcenschlüssel
/// (<c>ZPGM_</c>) im Kommentar; der Vorgabewert ist der deutsche Rückfall und gleicht dem Wert der
/// neutralen Ressource (Wache <c>TwwMessreihenDialogTests</c>).</para>
/// </remarks>
public sealed class TwwMessreihenTexte
{
    /// <summary><c>ZPGM_TITEL</c></summary>
    public string Titel { get; set; } = "Messdaten";

    /// <summary><c>ZPGM_HINWEIS_PROJEKT</c></summary>
    public string HinweisProjekt { get; set; } = "Messreihen gehören zum Projekt: Sie reisen mit einer Projektkopie und einem Projektpaket und stehen in keiner Auslieferungsvorlage.";

    /// <summary><c>ZPGM_HINWEIS_FORMAT</c></summary>
    public string HinweisFormat { get; set; } = "Eine Datei ist eine CSV-Tabelle mit Kopfzeile: ein Zeitstempel oder Datum und Uhrzeit getrennt, dazu eine Wertspalte — Trenner Semikolon, Tabulator oder Komma, Zahlen mit Punkt oder Komma.";

    /// <summary><c>ZPGM_HINWEIS_NULLLAEUFE</c></summary>
    public string HinweisNulllaeufe { get; set; } = "Ein Nulllauf ist eine gefüllte Lücke oder eine gemessene Stunde ohne Zapfung; welche von beiden, sagt die Ablage nicht.";

    /// <summary><c>ZPGM_GRP_LISTE</c></summary>
    public string GruppeListe { get; set; } = "Eingespielte Messreihen";

    /// <summary><c>ZPGM_GRP_EINLESEN</c></summary>
    public string GruppeEinlesen { get; set; } = "Datei einlesen";

    /// <summary><c>ZPGM_GRP_PRUEFUNG</c></summary>
    public string GruppePruefung { get; set; } = "Prüfung der Datei";

    /// <summary><c>ZPGM_LEER</c></summary>
    public string Leer { get; set; } = "Es ist keine Messreihe eingespielt — ohne sie gibt es keinen Vergleich und keine Kalibrierung aus der Messung.";

    /// <summary><c>ZPGM_SP_BEZEICHNUNG</c></summary>
    public string SpalteBezeichnung { get; set; } = "Bezeichnung";

    /// <summary><c>ZPGM_SP_GROESSE</c></summary>
    public string SpalteGroesse { get; set; } = "Größe";

    /// <summary><c>ZPGM_SP_AUFLOESUNG</c></summary>
    public string SpalteAufloesung { get; set; } = "Auflösung";

    /// <summary><c>ZPGM_SP_BEGINN</c></summary>
    public string SpalteBeginn { get; set; } = "Beginn";

    /// <summary><c>ZPGM_SP_TAGE</c></summary>
    public string SpalteTage { get; set; } = "Tage";

    /// <summary><c>ZPGM_SP_NULLLAEUFE</c></summary>
    public string SpalteNulllaeufe { get; set; } = "Nullläufe";

    /// <summary><c>ZPGM_SP_QUELLE</c></summary>
    public string SpalteQuelle { get; set; } = "Quelle";

    /// <summary><c>ZPGM_SP_IMPORT</c></summary>
    public string SpalteImport { get; set; } = "Eingespielt am";

    /// <summary><c>ZPGM_SP_ANGABE</c></summary>
    public string SpalteAngabe { get; set; } = "Angabe";

    /// <summary><c>ZPGM_SP_WERT</c></summary>
    public string SpalteWert { get; set; } = "Wert";

    /// <summary><c>ZPGM_SP_AKTION</c></summary>
    public string SpalteAktion { get; set; } = "Handlung";

    /// <summary><c>ZPGM_LBL_BEZEICHNUNG</c></summary>
    public string LabelBezeichnung { get; set; } = "Bezeichnung";

    /// <summary><c>ZPGM_LBL_QUELLE</c></summary>
    public string LabelQuelle { get; set; } = "Quelle";

    /// <summary><c>ZPGM_LBL_GROESSE</c></summary>
    public string LabelGroesse { get; set; } = "Gemessene Größe";

    /// <summary><c>ZPGM_LBL_LUECKENSCHWELLE</c></summary>
    public string LabelLueckenschwelle { get; set; } = "Höchster Anteil gefüllter Lücken";

    /// <summary><c>ZPGM_LBL_ZEITSTEMPEL</c></summary>
    public string LabelZeitstempel { get; set; } = "Zeitstempel";

    /// <summary><c>ZPGM_GROESSE_KOPF</c></summary>
    public string GroesseKopf { get; set; } = "aus der Kopfzeile";

    /// <summary><c>ZPGM_GROESSE_ENERGIE</c></summary>
    public string GroesseEnergie { get; set; } = "Energie (kWh)";

    /// <summary><c>ZPGM_GROESSE_VOLUMEN</c></summary>
    public string GroesseVolumen { get; set; } = "Volumen (m³)";

    /// <summary><c>ZPGM_GROESSE_LEISTUNG</c></summary>
    public string GroesseLeistung { get; set; } = "Leistung (kW)";

    /// <summary><c>ZPGM_ZEIT_ORTSZEIT</c></summary>
    public string ZeitOrtszeit { get; set; } = "Ortszeit mit Sommerzeit";

    /// <summary><c>ZPGM_ZEIT_NORMALZEIT</c></summary>
    public string ZeitNormalzeit { get; set; } = "Normalzeit ohne Umstellung";

    /// <summary><c>ZPGM_BTN_DATEI</c></summary>
    public string KnopfDatei { get; set; } = "Datei wählen…";

    /// <summary><c>ZPGM_BTN_EINSPIELEN</c></summary>
    public string KnopfEinspielen { get; set; } = "Einspielen";

    /// <summary><c>ZPGM_BTN_LOESCHEN</c></summary>
    public string KnopfLoeschen { get; set; } = "Löschen";

    /// <summary><c>ZPGM_BTN_BEENDEN</c></summary>
    public string KnopfBeenden { get; set; } = "Beenden";

    /// <summary><c>ZPGM_DATEIFILTER</c></summary>
    public string Dateifilter { get; set; } = "Messreihe (*.csv;*.txt)|*.csv;*.txt|Alle Dateien (*.*)|*.*";

    /// <summary><c>ZPGM_WAHL_TITEL</c></summary>
    public string WahlTitel { get; set; } = "Datei der Messreihe wählen";

    /// <summary><c>ZPGM_KEINE_DATEI</c></summary>
    public string KeineDatei { get; set; } = "Es ist keine Datei gewählt.";

    /// <summary><c>ZPGM_ABBRUCH</c></summary>
    public string Abbruch { get; set; } = "Die Datei ist abgelehnt: {0}";

    /// <summary><c>ZPGM_PRUEFUNG_LAEUFT</c></summary>
    public string PruefungLaeuft { get; set; } = "Die Datei wird geprüft …";

    /// <summary><c>ZPGM_PRUEFUNG_ABGEBROCHEN</c></summary>
    public string PruefungAbgebrochen { get; set; } = "Die Prüfung ist abgebrochen; es ist nichts eingespielt.";

    /// <summary><c>ZPGM_BERICHT_OFFEN</c></summary>
    public string BerichtOffen { get; set; } = "Bezeichnung und Quelle stehen im Bericht erst nach der nächsten Prüfung; eingespielt wird, was in den Feldern steht.";

    /// <summary><c>ZPGM_ZUSAMMENFASSUNG</c></summary>
    public string Zusammenfassung { get; set; } = "Die Datei trägt {0} Zeitschritt(e) im Raster {1} min — {2} Tag(e).";

    /// <summary><c>ZPGM_HINWEISE</c></summary>
    public string Hinweise { get; set; } = "Hinweise";

    /// <summary><c>ZPGM_LBL_SCHRITTE</c></summary>
    public string LabelSchritte { get; set; } = "Zeitschritte";

    /// <summary><c>ZPGM_LBL_TAGE</c></summary>
    public string LabelTage { get; set; } = "Tage";

    /// <summary><c>ZPGM_LBL_AUFLOESUNG</c></summary>
    public string LabelAufloesung { get; set; } = "Auflösung";

    /// <summary><c>ZPGM_LBL_BEGINN</c></summary>
    public string LabelBeginn { get; set; } = "Beginn";

    /// <summary><c>ZPGM_LBL_ENDE</c></summary>
    public string LabelEnde { get; set; } = "Ende";

    /// <summary><c>ZPGM_LBL_LUECKEN</c></summary>
    public string LabelLuecken { get; set; } = "Gefüllte Lücken";

    /// <summary><c>ZPGM_LBL_SCHALTTAGE</c></summary>
    public string LabelSchalttage { get; set; } = "Schalttage";

    /// <summary><c>ZPGM_FRAGE_ERSETZEN</c></summary>
    public string FrageErsetzen { get; set; } = "Das Projekt führt schon eine Messreihe „{0}“. Die neue ersetzt sie vollständig. Einspielen?";

    /// <summary><c>ZPGM_FRAGE_LOESCHEN</c></summary>
    public string FrageLoeschen { get; set; } = "Die Messreihe „{0}“ wird aus dem Projekt entfernt. Löschen?";

    /// <summary><c>ZPGM_MSG_EINGESPIELT</c></summary>
    public string MeldungEingespielt { get; set; } = "„{0}“ eingespielt: {1} Zeile(n), {2} ersetzt.";

    /// <summary><c>ZPGM_MSG_GELOESCHT</c></summary>
    public string MeldungGeloescht { get; set; } = "„{0}“ entfernt: {1} Zeile(n).";

    /// <summary><c>ZPGM_MSG_NICHTS</c></summary>
    public string MeldungNichts { get; set; } = "Es war nichts zu löschen.";

    /// <summary><c>ZPGM_KEIN_PROJEKT</c></summary>
    public string KeinProjekt { get; set; } = "Messreihen brauchen ein gespeichertes Projekt.";

    /// <summary><c>ZPGM_WERT_OHNE</c></summary>
    public string WertOhne { get; set; } = "ohne Angabe";
}
