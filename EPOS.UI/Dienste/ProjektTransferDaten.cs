using WindowsFormsApplication1;

namespace EPOS.UI.Dienste;

/// <summary>
/// Die Vorschau auf ein <c>.wpx</c>-Paket, gelesen aus dem <c>manifest.json</c>
/// OHNE die Datenbank anzufassen (iU9-W15a).
///
/// <para>Der Vorlaeufer <c>Form_ProjektExportImport.ZeigePaketInfo</c> setzte daraus
/// eine Zeile zusammen und schrieb sie in ein Label. Hier reisen die Werte EINZELN;
/// welchen Satz sie ergeben, entscheidet die Oberflaeche — sie muss ihn uebersetzen
/// koennen (Befund W15a-B36: die Maske war zu 0 % uebersetzt).</para>
/// </summary>
/// <param name="Quellprojekt">Name des exportierten Projekts.</param>
/// <param name="Exportdatum">Ausgabezeitpunkt, bereits als Text der Programmsprache; leer, wenn unlesbar.</param>
/// <param name="Schemastand">Der Migrationsstand des Pakets (0 = V1-Altpaket).</param>
/// <param name="Varianten">Die mitgereisten Variantenprojekte.</param>
/// <param name="Fehler">Leer, wenn das Paket lesbar ist; sonst der Grund.</param>
public sealed record PaketVorschau(
    string Quellprojekt,
    string Exportdatum,
    int Schemastand,
    IReadOnlyList<string> Varianten,
    string Fehler);

/// <summary>
/// Ergebnis eines Importlaufs (iU9-W15a).
/// </summary>
/// <param name="Id">Die Id des importierten Stammprojekts; <c>-1</c> bei Misserfolg.</param>
/// <param name="Name">Der Name, unter dem es angelegt wurde.</param>
/// <param name="Bericht">Die Zeilen des Importberichts (<c>LetzterBericht</c>).</param>
/// <param name="Fehler">Leer bei Erfolg; sonst der Grund.</param>
public sealed record ImportErgebnis(
    int Id,
    string Name,
    IReadOnlyList<string> Bericht,
    string Fehler);

/// <summary>
/// Alles, was der Dialog „Projekt exportieren / importieren" an DATEN und WEGEN
/// braucht (iU9-W15a.0h).
///
/// <para><b>Warum ein Buendel und keine zwoelf Parameter.</b> Dasselbe Muster wie
/// <c>BhkwDialogDaten</c>: Die Huelle laedt in einem Zug, was der Dialog braucht, und
/// die Reihenfolge der Ladeschritte bleibt an EINER Stelle.</para>
///
/// <para><b>Die vier Delegaten sind der Grund, warum dieser Dialog ueberhaupt auf iOS
/// laufen kann.</b> Der Vorlaeufer schrieb an drei Stellen unmittelbar ins Dateisystem:
/// <c>SaveFileDialog</c>/<c>OpenFileDialog</c> (Windows-Fenster), die Sicherungskopie
/// NEBEN die Datenbank (77 MB je Import, ohne Aufraeumen — Befund W15a-B28) und den
/// Importbericht NEBEN die Paketdatei (auf iOS ein Fremdpfad aus dem Dokumentenwaehler,
/// in den die App nicht schreiben darf — W15a-B29). Alle vier kommen hier als Delegat
/// herein; unter Windows verhalten sie sich wie bisher, auf iOS anders.</para>
///
/// <para><b>Kein Delegat ist kein Fehler, sondern kein Knopf</b> (Hausregel A-18 aus
/// iU9-W2): Ist <see cref="SicherungAnlegen"/> nicht gesetzt, zeigt der Dialog den
/// Schalter „Sicherungskopie" gar nicht erst.</para>
/// </summary>
/// <param name="Projekte">Die waehlbaren Projekte (<c>ProjektCtrl.NamenListe</c>).</param>
/// <param name="Varianten">Die Variantenprojekte eines Stammprojekts, nach Namen sortiert.</param>
/// <param name="Exportieren">Schreibt das Paket; Rueckgabe <c>true</c> bei Erfolg.</param>
/// <param name="PaketLesen">Zeigt einen Dateiwaehler und liefert den gewaehlten Paketpfad; <c>null</c> = abgebrochen.</param>
/// <param name="PaketSchreiben">Zeigt einen Speichern-Dialog mit Namensvorschlag und liefert den Zielpfad; <c>null</c> = abgebrochen.</param>
/// <param name="Vorschau">Liest <c>manifest.json</c> aus einem Paket, ohne die Datenbank anzufassen.</param>
/// <param name="Importieren">Fuehrt den Import aus.</param>
/// <param name="SicherungAnlegen">Legt eine Sicherungskopie der Datenbank an und liefert ihren Pfad; wirft bei Misserfolg. <c>null</c> = kein Schalter.</param>
/// <param name="BerichtSchreiben">Legt den Importbericht ab und liefert den Zielpfad; <c>null</c> = kein Ablegen, dann steht der Bericht nur im Dialog.</param>
public sealed record ProjektTransferDaten(
    IReadOnlyList<ProjektKopfZeile> Projekte,
    Func<string, IReadOnlyList<string>> Varianten,
    Func<string, IReadOnlyList<string>, string, IProgress<string>?, bool> Exportieren,
    Func<string?>? PaketLesen,
    Func<string, string?>? PaketSchreiben,
    Func<string, PaketVorschau> Vorschau,
    Func<string, string, ProjektExportImportCtrl.BeiVorhandenem, IProgress<string>?, ImportErgebnis> Importieren,
    Func<string>? SicherungAnlegen,
    Func<string, string, string?>? BerichtSchreiben);
