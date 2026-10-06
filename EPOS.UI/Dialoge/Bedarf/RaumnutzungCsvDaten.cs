using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>Die Art einer Zeilenmeldung des CSV-Imports (Konzept Nutzungsprofile NP-F11).</summary>
public enum RaumnutzungCsvMeldungsart
{
    /// <summary>Der Wert steht im Profil.</summary>
    Uebernommen = 0,

    /// <summary>Spalte oder Wert bewusst nicht gelesen.</summary>
    Ignoriert = 1,

    /// <summary>Wert oder Zeile mit Grund nicht übernommen.</summary>
    Fehler = 2,
}

/// <summary>Der Stand einer Profilzeile der Vorschau.</summary>
public enum RaumnutzungCsvStand
{
    /// <summary>„Übernehmen" legt das Profil an.</summary>
    Neu = 0,

    /// <summary>Die Zielkategorie trägt ein Profil gleichen Namens — die Rückfrage ersetzt oder überspringt es.</summary>
    Vorhanden = 1,

    /// <summary>Die Zeile wird mit Grund nicht übernommen.</summary>
    Abgelehnt = 2,
}

/// <summary>Eine gewählte Datei: Name zur Anzeige und Inhalt; gelesen wird erst in der Hülle.</summary>
public sealed record RaumnutzungCsvDatei(string Name, byte[] Inhalt);

/// <summary>Eine Zeilenmeldung: Zeile der Datei (Kopfzeile = 1), Spalte, Art und Grund.</summary>
public sealed record RaumnutzungCsvMeldungDaten(int Zeile, string Spalte, RaumnutzungCsvMeldungsart Art, string Grund);

/// <summary>Eine Profilzeile der Vorschau samt Zählung ihrer Meldungen.</summary>
public sealed record RaumnutzungCsvProfilzeile(int Zeile, string Nummer, string Bezeichner, RaumnutzungCsvStand Stand, string Grund,
                                               int Uebernommen, int Ignoriert, int Fehler);

/// <summary>Die Vorschau des Imports zu einer Zielkategorie — ohne zu schreiben.</summary>
/// <param name="Abbruch">Warum die Datei als Ganzes nicht gelesen wird; <c>null</c> = gelesen.</param>
/// <param name="Profile">Die Profilzeilen in Dateireihenfolge.</param>
/// <param name="Meldungen">Alle Meldungen nach Zeile geordnet.</param>
public sealed record RaumnutzungCsvVorschau(string? Abbruch, IReadOnlyList<RaumnutzungCsvProfilzeile> Profile,
                                            IReadOnlyList<RaumnutzungCsvMeldungDaten> Meldungen)
{
    /// <summary>Die Zeilen, die „Übernehmen" schreibt (neu oder vorhanden).</summary>
    public int Uebernehmbar => Profile.Count(p => p.Stand != RaumnutzungCsvStand.Abgelehnt);

    /// <summary>Die Zeilen, zu denen die Rückfrage ersetzen/überspringen gehört.</summary>
    public int Vorhanden => Profile.Count(p => p.Stand == RaumnutzungCsvStand.Vorhanden);

    /// <summary>Die Meldungen, die die Vorschau als Liste zeigt: ignoriert und Fehler.</summary>
    public IEnumerable<RaumnutzungCsvMeldungDaten> Hinweise => Meldungen.Where(m => m.Art != RaumnutzungCsvMeldungsart.Uebernommen);
}

/// <summary>
/// <b>Der Weg des CSV-Imports und -Exports</b> der Nutzungsprofile (Stufe NP4a; Konzept Nutzungsprofile 6.1, 6.4) — ein
/// Bündel von Delegaten, das <c>RaumnutzungCsvHuelle</c> aus <c>RaumnutzungCtrl</c> und <c>Dienste.Datei</c> füllt.
/// „Kein Delegat, kein Knopf": Was fehlt, bietet die Komponente nicht an. Die Dateiwahl gehört der Plattform; die
/// Komponente sieht nur Name und Bytes.
/// </summary>
public sealed class RaumnutzungCsvWeg
{
    /// <summary>Die Texte der Komponente (Präfix <c>RNP_CSV_</c>).</summary>
    public RaumnutzungCsvTexte Texte { get; init; } = new();

    /// <summary>Lässt die Plattform eine Datei wählen und liest sie; <c>null</c> = abgebrochen. Ein Lesefehler kommt als Ausnahme mit Text.</summary>
    public Func<Task<RaumnutzungCsvDatei?>>? DateiWaehlen { get; init; }

    /// <summary>Liest die Datei und gleicht sie mit der Zielkategorie ab (<c>null</c> = neue Kategorie); schreibt nichts.</summary>
    public Func<RaumnutzungCsvDatei, long?, RaumnutzungCsvVorschau>? Pruefen { get; init; }

    /// <summary>
    /// Schreibt die übernehmbaren Profile: Datei, Zielkategorie (<c>null</c> = neue mit dem Namen), Name der neuen Kategorie,
    /// vorhandene ersetzen (<c>true</c>) oder überspringen.
    /// </summary>
    public Func<RaumnutzungCsvDatei, long?, string, bool, RaumnutzungErgebnis>? Uebernehmen { get; init; }

    /// <summary>Schreibt eine Kategorie als CSV-Datei (Dateiwahl der Plattform); <c>Ok</c> mit leerer Meldung = abgebrochen.</summary>
    public Func<long, Task<RaumnutzungErgebnis>>? Exportieren { get; init; }
}

/// <summary>Das Textbündel der Komponente <c>RaumnutzungCsvAustausch</c>; jede Eigenschaft nennt ihren Schlüssel, der deutsche Wert ist der Rückfall.</summary>
public sealed class RaumnutzungCsvTexte
{
    /// <summary><c>RNP_CSV_BTN_IMPORT</c></summary>
    public string KnopfImport { get; set; } = "CSV importieren…";
    /// <summary><c>RNP_CSV_BTN_EXPORT</c></summary>
    public string KnopfExport { get; set; } = "CSV exportieren…";
    /// <summary><c>RNP_CSV_LBL_IMPORT</c></summary>
    public string TitelImport { get; set; } = "CSV-Import";
    /// <summary><c>RNP_CSV_LBL_EXPORT</c></summary>
    public string TitelExport { get; set; } = "CSV-Export";
    /// <summary><c>RNP_CSV_LBL_DATEI</c> — {0} = Dateiname.</summary>
    public string Datei { get; set; } = "Datei: {0}";
    /// <summary><c>RNP_CSV_LBL_ZIEL</c></summary>
    public string Ziel { get; set; } = "Zielkategorie";
    /// <summary><c>RNP_CSV_LBL_NEUE</c></summary>
    public string NeueKategorie { get; set; } = "(neue Kategorie)";
    /// <summary><c>RNP_CSV_LBL_NAME</c></summary>
    public string NameNeu { get; set; } = "Name der neuen Kategorie";
    /// <summary><c>RNP_CSV_LBL_KATEGORIE</c></summary>
    public string Kategorie { get; set; } = "Kategorie";
    /// <summary><c>RNP_CSV_LBL_ZEILE</c></summary>
    public string Zeile { get; set; } = "Zeile";
    /// <summary><c>RNP_CSV_LBL_SPALTE</c></summary>
    public string Spalte { get; set; } = "Spalte";
    /// <summary><c>RNP_CSV_LBL_ART</c></summary>
    public string Art { get; set; } = "Art";
    /// <summary><c>RNP_CSV_LBL_GRUND</c></summary>
    public string Grund { get; set; } = "Grund";
    /// <summary><c>RNP_CSV_LBL_NUMMER</c></summary>
    public string Nummer { get; set; } = "Nr.";
    /// <summary><c>RNP_CSV_LBL_PROFIL</c></summary>
    public string Profil { get; set; } = "Profil";
    /// <summary><c>RNP_CSV_LBL_STAND</c></summary>
    public string Stand { get; set; } = "Stand";
    /// <summary><c>RNP_CSV_LBL_WERTE</c></summary>
    public string Werte { get; set; } = "Werte";
    /// <summary><c>RNP_CSV_LBL_MELDUNGEN</c></summary>
    public string Meldungen { get; set; } = "Meldungen";
    /// <summary><c>RNP_CSV_ART_UEBERNOMMEN</c></summary>
    public string ArtUebernommen { get; set; } = "übernommen";
    /// <summary><c>RNP_CSV_ART_IGNORIERT</c></summary>
    public string ArtIgnoriert { get; set; } = "ignoriert";
    /// <summary><c>RNP_CSV_ART_FEHLER</c></summary>
    public string ArtFehler { get; set; } = "Fehler";
    /// <summary><c>RNP_CSV_STAND_NEU</c></summary>
    public string StandNeu { get; set; } = "neu";
    /// <summary><c>RNP_CSV_STAND_VORHANDEN</c></summary>
    public string StandVorhanden { get; set; } = "vorhanden";
    /// <summary><c>RNP_CSV_STAND_ABGELEHNT</c></summary>
    public string StandAbgelehnt { get; set; } = "nicht übernommen";
    /// <summary><c>RNP_CSV_TXT_ZAEHLUNG</c> — {0} übernommen, {1} ignoriert, {2} Fehler.</summary>
    public string Zaehlung { get; set; } = "{0} übernommen, {1} ignoriert, {2} Fehler";
    /// <summary><c>RNP_CSV_TXT_FORMAT</c></summary>
    public string Format { get; set; } = "UTF-8, Semikolon, Dezimalpunkt; eine Kopfzeile mit den Spaltennamen, eine Zeile je Profil.";
    /// <summary><c>RNP_CSV_TXT_HINWEISE</c></summary>
    public string Hinweise { get; set; } = "Gezeigt sind die ignorierten Werte und die Fehler; „Übernehmen“ schreibt nur die Profile im Stand „neu“ oder „vorhanden“.";
    /// <summary><c>RNP_CSV_TXT_KEINE_HINWEISE</c></summary>
    public string KeineHinweise { get; set; } = "Alle Werte werden übernommen.";
    /// <summary><c>RNP_CSV_BTN_UEBERNEHMEN</c></summary>
    public string KnopfUebernehmen { get; set; } = "Übernehmen";
    /// <summary><c>RNP_CSV_BTN_ABBRECHEN</c></summary>
    public string KnopfAbbrechen { get; set; } = "Abbrechen";
    /// <summary><c>RNP_CSV_BTN_SPEICHERN</c></summary>
    public string KnopfSpeichern { get; set; } = "Speichern…";
    /// <summary><c>RNP_CSV_BTN_ERSETZEN</c></summary>
    public string KnopfErsetzen { get; set; } = "Ersetzen";
    /// <summary><c>RNP_CSV_BTN_UEBERSPRINGEN</c></summary>
    public string KnopfUeberspringen { get; set; } = "Überspringen";
    /// <summary><c>RNP_CSV_FRAGE_VORHANDEN</c> — {0} = Zahl der vorhandenen Profile.</summary>
    public string FrageVorhanden { get; set; } = "{0} Profile gibt es in der Zielkategorie schon. Ersetzen oder überspringen?";
    /// <summary><c>RNP_CSV_TITEL_VORHANDEN</c></summary>
    public string TitelVorhanden { get; set; } = "Vorhandene Profile";
    /// <summary><c>RNP_CSV_MSG_NICHTS</c> — die Sperre von „Übernehmen" ohne übernehmbares Profil.</summary>
    public string SperreNichts { get; set; } = "Die Datei enthält kein übernehmbares Profil.";
    /// <summary><c>RNP_CSV_SPERRE_NAME</c></summary>
    public string SperreName { get; set; } = "Der Name der neuen Kategorie fehlt.";
}
