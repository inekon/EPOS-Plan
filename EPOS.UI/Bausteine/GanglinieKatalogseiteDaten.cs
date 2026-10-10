#nullable enable

using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die Wege der Ganglinien-Katalogseite</b> (<see cref="GanglinieKatalogseite"/>) — was die
/// Hülle aus den Controllern des Kerns und aus der Plattform hereinreicht. Ein Satz je
/// Zeitreihenart; die Fachlogik bleibt im Controller der Art.
///
/// <para><b>Kein Delegat, kein Knopf:</b> Ohne <see cref="Loeschen"/> fehlt „Löschen", ohne
/// <see cref="Schloss"/> das Schloss. Ohne <see cref="Einlesen"/> steht „Import…" nur, wenn
/// <see cref="ImportAbgelehnt"/> den Grund nennt — dann weich gesperrt, der Klick meldet ihn
/// (eine Plattform, die keinen Dateiweg hat, lehnt benannt ab, statt den Knopf still zu
/// verlieren).</para>
/// </summary>
public sealed class GanglinienKatalogwege
{
    /// <summary>Der Katalog samt Kennzahlen (Bezeichner, Beschreibung, Jahresarbeit, Spitze).</summary>
    public Func<Task<IReadOnlyList<Katalogfilterzeile>>>? Katalogzeilen { get; init; }

    /// <summary>Je Bezeichner die Projekte, die die Ganglinie führen — EINE Abfrage je Liste.</summary>
    public Func<Task<IReadOnlyDictionary<string, IReadOnlyList<string>>>>? Verwendung { get; init; }

    /// <summary>Die letzte Prüfung vor dem Löschen: Gibt es eine Projektzuordnung?</summary>
    public Func<string, Task<bool>>? HatProjektzuordnung { get; init; }

    /// <summary>Löscht einen Katalogsatz; <c>false</c> = er blieb stehen.</summary>
    public Func<string, Task<bool>>? Loeschen { get; init; }

    /// <summary>„Schloss setzen…"/„Schloss aufheben…" — der Schreibweg des Auslieferungskennzeichens.</summary>
    public Schlossweg? Schloss { get; init; }

    /// <summary>Der Dateiwähler der Plattform (Filter → Pfad, <c>null</c> = abgebrochen).</summary>
    public Func<string, Task<string?>>? DateiWaehlen { get; init; }

    /// <summary>Die verlustfreie Originalablage im Ganglinienordner (optional).</summary>
    public Func<string, Task<AblageErgebnis>>? Ablegen { get; init; }

    /// <summary>Öffnet die gewählte Datei mit der Systemanwendung (optional).</summary>
    public Func<string, Task<bool>>? MitSystemOeffnen { get; init; }

    /// <summary>
    /// Liest, prüft den Namen und schreibt — die ganze Kette im Kern. <b>Ohne ihn kein
    /// Import</b>, siehe <see cref="ImportAbgelehnt"/>.
    /// </summary>
    public Func<string, IProgress<ImportFortschritt>, Task<GanglinienKatalogimport>>? Einlesen { get; init; }

    /// <summary>
    /// <b>Die Nennleistung beim Import</b> (nur die PV-Ganglinie): Ist der Weg gesetzt, fragt die
    /// Überlagerung die Nennleistung [kWp] ab und belegt sie nach der Dateiwahl hiermit vor
    /// (Dateikopf, sonst die Spitze der Reihe). <c>null</c> = keine Abfrage.
    /// </summary>
    public Func<string, Task<GanglinienNennleistungsvorschlag>>? NennleistungVorschlagen { get; init; }

    /// <summary>
    /// Die Prüfung der Nennleistung gegen die Reihe (Spitze [kW], Nennleistung [kWp]) — der Satz des
    /// Kerns, "" ohne Hinweis. Die Regel steht im Kern, nicht hier.
    /// </summary>
    public Func<double, double?, string>? NennleistungPruefen { get; init; }

    /// <summary>
    /// Das Einlesen MIT der abgefragten Nennleistung [kWp]; gesetzt, wenn
    /// <see cref="NennleistungVorschlagen"/> gesetzt ist — sonst gilt <see cref="Einlesen"/>.
    /// </summary>
    public Func<string, double?, IProgress<ImportFortschritt>, Task<GanglinienKatalogimport>>? EinlesenMitNennleistung { get; init; }

    /// <summary>
    /// <b>Die Nennleistung eines Katalogsatzes nachträglich bearbeiten</b> (nur die PV-Ganglinie): Ist der Weg
    /// gesetzt, trägt die Leiste „Nennleistung bearbeiten…"; er schreibt den Wert [kWp] (<c>null</c> = nicht
    /// gepflegt) an den Katalogsatz des Namens — sofort, wie Löschen und Schloss. Ein Auslieferungssatz bleibt
    /// gesperrt (die Sperre steht im Kern).
    /// </summary>
    public Func<string, double?, Task<GanglinienNennleistungsschrieb>>? NennleistungSchreiben { get; init; }

    /// <summary>
    /// Der Prüfhinweis zu einer Nennleistung [kWp] für einen Katalogsatz (Name) — derselbe Satz wie beim Import,
    /// gegen die Spitze der Reihe im Raster der Datei; "" ohne Hinweis.
    /// </summary>
    public Func<string, double?, string>? NennleistungPruefenFuer { get; init; }

    /// <summary>Der Ganglinienordner als Anzeigetext; leer = keine Zeile.</summary>
    public string Ordner { get; init; } = "";

    /// <summary>
    /// Warum diese Plattform nicht importiert; nur ohne <see cref="Einlesen"/> von Belang.
    /// Leer = kein Knopf.
    /// </summary>
    public string ImportAbgelehnt { get; init; } = "";

    /// <summary>
    /// <b>Das Lesen über den Optionendialog „Format und Vorschau“</b>: Ist der Weg gesetzt, öffnet die
    /// Import-Überlagerung nach der Dateiwahl den Optionendialog (Vorbelegung aus der Formaterkennung
    /// der Art) und liest die Datei mit den bestätigten Optionen über die Importkette
    /// (<c>GanglinienImportAblauf.OhneAblage</c>). Abbrechen im Optionendialog bricht den Import ab;
    /// „Einlesen“ schreibt danach über <see cref="EinlesenGelesen"/>.
    /// </summary>
    public Func<string, GanglinienImportRueckrufe, Task<GanglinienImportErgebnis>>? Lesen { get; init; }

    /// <summary>Zerlegt die Datei mit gesetzten Optionen neu (für „Vorschau aktualisieren“ des Optionendialogs).</summary>
    public Func<string, GanglinienImportOptionen, Task<GanglinienVorschau?>>? Vorschau { get; init; }

    /// <summary>
    /// Die Vorbelegung der Nennleistung aus der schon gelesenen Reihe (nur PV-Ganglinie) — Dateikopf
    /// vor Spitze; gilt statt <see cref="NennleistungVorschlagen"/>, wenn <see cref="Lesen"/> gesetzt ist.
    /// </summary>
    public Func<string, GanglinienImportErgebnis, Task<GanglinienNennleistungsvorschlag>>? NennleistungAusLesung { get; init; }

    /// <summary>
    /// Schreibt die über <see cref="Lesen"/> gelesene Reihe in den Katalog (Pfad, Lesung, Nennleistung
    /// [kWp] oder <c>null</c>); gesetzt, wenn <see cref="Lesen"/> gesetzt ist.
    /// </summary>
    public Func<string, GanglinienImportErgebnis, double?, IProgress<ImportFortschritt>, Task<GanglinienKatalogimport>>? EinlesenGelesen { get; init; }

    /// <summary>
    /// <b>Die Ganglinie eines Katalogsatzes als Jahresbild</b> (Name → Bild und Kennzahlen,
    /// <c>ZeitreihenAdminWege.Ansicht</c>); der Wirt zeigt sie in seiner Satzansicht. Ohne ihn kein Bild.
    /// </summary>
    public Func<string, Task<Ganglinienansicht>>? Ansicht { get; init; }
}

/// <summary>
/// Die Vorbelegung der Nennleistung vor dem Import (<see cref="GanglinienKatalogwege.NennleistungVorschlagen"/>):
/// <paramref name="VorschlagKwp"/> <c>null</c> = keine lesbare Datei; <paramref name="AusDateikopf"/> sagt, ob der
/// Wert im Dateikopf stand oder die Spitze <paramref name="SpitzeKw"/> der Reihe ist.
/// </summary>
public sealed record GanglinienNennleistungsvorschlag(double? VorschlagKwp, bool AusDateikopf, double SpitzeKw);

/// <summary>Wie das Schreiben einer Nennleistung ausgegangen ist (<see cref="GanglinienKatalogwege.NennleistungSchreiben"/>).</summary>
/// <param name="Erfolgreich">Trägt der Katalogsatz den Wert?</param>
/// <param name="Meldung">Der Grund eines Misserfolgs, sonst die Statuszeile.</param>
/// <param name="Hinweis">Der Prüfhinweis (Spitze über Nennleistung × 1,1); leer ohne.</param>
public sealed record GanglinienNennleistungsschrieb(bool Erfolgreich, string Meldung, string Hinweis);

/// <summary>Wie ein Import auf der Katalogseite ausgegangen ist.</summary>
/// <param name="Erfolgreich">Steht der neue Satz im Katalog?</param>
/// <param name="IstFehler">Ist <paramref name="Meldung"/> ein Fehler (sonst ein Hinweis)?</param>
/// <param name="Bezeichner">Der Name des Satzes.</param>
/// <param name="Meldung">Der Grund eines Misserfolgs; leer bei Erfolg.</param>
/// <param name="Protokoll">Format, Anzahl Werte, Raster und Kennzahlen; leer, wenn nicht gelesen.</param>
public sealed record GanglinienKatalogimport(bool Erfolgreich, bool IstFehler, string Bezeichner,
                                             string Meldung, string Protokoll);

/// <summary>
/// <b>Die Beschriftungen der Ganglinien-Katalogseite</b> — Vorgabe sind die Texte der
/// Solarthermie-Ganglinie; eine andere Zeitreihenart reicht ihre eigenen herein.
/// </summary>
public sealed class GanglinienKatalogtexte
{
    /// <summary>„Import…" — <c>ADM_BTN_IMPORT</c>.</summary>
    public string ImportKnopf { get; set; } = Resource.ADM_BTN_IMPORT;

    /// <summary>Titel der Import-Überlagerung — <c>SGAD_GRP_EINLESEN</c>.</summary>
    public string ImportTitel { get; set; } = Resource.SGAD_GRP_EINLESEN;

    /// <summary>Was für eine Datei erwartet wird — <c>SGAD_LBL_STUNDENWERTE</c>.</summary>
    public string ImportHinweis { get; set; } = Resource.SGAD_LBL_STUNDENWERTE;

    /// <summary>„Datei Basis Ordner:" — <c>SGAD_LBL_ORDNER</c>.</summary>
    public string LabelOrdner { get; set; } = Resource.SGAD_LBL_ORDNER;

    /// <summary>„Gewählte Datei:" — <c>SGAD_LBL_DATEI</c>.</summary>
    public string LabelDatei { get; set; } = Resource.SGAD_LBL_DATEI;

    /// <summary>„Datei Auswählen…" — <c>SGAD_BTN_DATEI</c>.</summary>
    public string DateiKnopf { get; set; } = Resource.SGAD_BTN_DATEI;

    /// <summary>Der Filter des Dateiwählers — <c>WBAD_DATEIFILTER</c> (CSV, TXT, Excel).</summary>
    public string Dateifilter { get; set; } = Resource.WBAD_DATEIFILTER;

    /// <summary>„Datei bearbeiten…" — <c>SGAD_BTN_ANZEIGEN</c>.</summary>
    public string AnzeigenKnopf { get; set; } = Resource.SGAD_BTN_ANZEIGEN;

    /// <summary>„Datei Einlesen…" — <c>SGAD_BTN_EINLESEN</c>.</summary>
    public string EinlesenKnopf { get; set; } = Resource.SGAD_BTN_EINLESEN;

    /// <summary>„Abbrechen" — <c>ALLG_BTN_ABBRECHEN</c>.</summary>
    public string AbbrechenKnopf { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary>„Ganglinie Löschen" — <c>SGAD_BTN_LOESCHEN</c>.</summary>
    public string LoeschenKnopf { get; set; } = Resource.SGAD_BTN_LOESCHEN;

    /// <summary>Titel der Rückfrage — <c>PSP_TITEL_LOESCHEN</c>.</summary>
    public string LoeschenTitel { get; set; } = Resource.PSP_TITEL_LOESCHEN;

    /// <summary>Die Rückfrage, <c>{0}</c> = Name — <c>PSP_MELDUNG_WIRKLICH_LOESCHEN</c>.</summary>
    public string Loeschfrage { get; set; } = Resource.PSP_MELDUNG_WIRKLICH_LOESCHEN;

    /// <summary>Die Statuszeile danach, <c>{0}</c> = Name — <c>WBAD_MSG_GELOESCHT</c>.</summary>
    public string Geloescht { get; set; } = Resource.WBAD_MSG_GELOESCHT;

    /// <summary>Ein Projekt führt die Ganglinie — <c>WBAD_MSG_PROJEKTZUORDNUNG</c>.</summary>
    public string Projektzuordnung { get; set; } = Resource.WBAD_MSG_PROJEKTZUORDNUNG;

    /// <summary>Das offene Projekt führt sie, <c>{0}</c> = Name — <c>SGL_MSG_IM_PROJEKT</c>.</summary>
    public string ImProjekt { get; set; } = Resource.SGL_MSG_IM_PROJEKT;

    /// <summary>Auslieferungssatz bzw. Löschen gescheitert — <c>SGAD_MSG_SCHREIBGESCHUETZT</c>.</summary>
    public string Schreibgeschuetzt { get; set; } = Resource.SGAD_MSG_SCHREIBGESCHUETZT;

    /// <summary>„Ja" — <c>ALLG_BTN_JA</c>.</summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary>„Nein" — <c>ALLG_BTN_NEIN</c>.</summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    /// <summary>Die Abfrage der Nennleistung im Import — <c>PVG_LBL_NENNLEISTUNG</c>.</summary>
    public string LabelNennleistung { get; set; } = Resource.PVG_LBL_NENNLEISTUNG;

    /// <summary>Herkunft der Vorbelegung: Dateikopf — <c>PVG_IMP_NENN_AUS_KOPF</c>.</summary>
    public string NennleistungAusKopf { get; set; } = Resource.PVG_IMP_NENN_AUS_KOPF;

    /// <summary>Herkunft der Vorbelegung: Spitze der Reihe, {0} = kW — <c>PVG_IMP_NENN_AUS_SPITZE</c>.</summary>
    public string NennleistungAusSpitze { get; set; } = Resource.PVG_IMP_NENN_AUS_SPITZE;

    /// <summary>Der Knopf der Leiste — <c>PVG_BTN_NENNLEISTUNG</c>.</summary>
    public string NennleistungKnopf { get; set; } = Resource.PVG_BTN_NENNLEISTUNG;

    /// <summary>Der Titel der Überlagerung — <c>PVG_TITEL_NENNLEISTUNG</c>.</summary>
    public string NennleistungTitel { get; set; } = Resource.PVG_TITEL_NENNLEISTUNG;

    /// <summary>Was ein leeres Feld heißt — <c>PVG_NENN_LEER_HINWEIS</c>.</summary>
    public string NennleistungLeer { get; set; } = Resource.PVG_NENN_LEER_HINWEIS;

    /// <summary>Katalogsatz, nicht Projektkopie — <c>PVG_NENN_PROJEKTKOPIEN</c>.</summary>
    public string NennleistungProjektkopien { get; set; } = Resource.PVG_NENN_PROJEKTKOPIEN;

    /// <summary>Sperrgrund am Auslieferungssatz — <c>PVG_MSG_NENN_SCHREIBGESCHUETZT</c>.</summary>
    public string NennleistungGesperrt { get; set; } = Resource.PVG_MSG_NENN_SCHREIBGESCHUETZT;

    /// <summary>Der Name des Satzes in der Überlagerung — <c>HZK_LBL_NAME</c>.</summary>
    public string LabelName { get; set; } = Resource.HZK_LBL_NAME;

    /// <summary>OK der Überlagerung — <c>ALLG_BTN_OK</c>.</summary>
    public string OkKnopf { get; set; } = Resource.ALLG_BTN_OK;
}
