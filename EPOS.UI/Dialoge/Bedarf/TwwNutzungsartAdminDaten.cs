using EPOS.UI.Bausteine;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Der KATALOGDIALOG „Brauchwasser-Nutzungsarten" (Umsetzungskonzept
//  Zapfprofilgenerator 5.4; Stufe Z4, Gruppe 3): Katalogliste, Stammblatt der gewählten
//  Nutzungsart, der Editor hinter „Neu…", „Ändern…" und „Speichern unter…" und der
//  Katalogimport. Nur Daten: Der Dialog kennt keinen Kern-Typ — die Hülle
//  (ZapfprofilHuelle.Katalogdialog) übersetzt aus TwwNutzungsartCtrl und zurück.
// =====================================================================================

/// <summary>
/// <b>Die gewählte Nutzungsart im Stammblatt</b> — fertig in der Oberflächensprache formatiert:
/// Kopf (Name, Unterzeile, Schloss, Kennzahlen), die lesenden Gruppen Kennwerte, Jahres- und
/// Wochengang, Tagesgang, Zapfkategorien und Herkunft, die zwei Vorschaubilder und die
/// benannten Sperrgründe von „Ändern…" und „Löschen". Der interne Beleg steht nie darin
/// (Konzept 6 (e)).
/// </summary>
public sealed class TwwNutzungsartDetailDaten
{
    /// <summary>Die Id der Katalogzeile (<c>Tab_TwwNutzungsart_STAMM.ID</c>).</summary>
    public int Id { get; set; }

    /// <summary>Der Bezeichner.</summary>
    public string Name { get; set; } = "";

    /// <summary>Die Katalogversion — mit dem Bezeichner der natürliche Schlüssel.</summary>
    public string Katalogversion { get; set; } = "";

    /// <summary>Die Unterzeile des Stammblatts („Katalogversion · Stand · Bezugsart").</summary>
    public string Unterzeile { get; set; } = "";

    /// <summary>Gehört die Zeile zur Auslieferung (Schloss)?</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Benutzt eine Zone eines Projekts die Zeile?</summary>
    public bool Benutzt { get; set; }

    /// <summary>Die Projekte, deren Zonen die Zeile benutzen.</summary>
    public IReadOnlyList<string> Projekte { get; set; } = Array.Empty<string>();

    /// <summary>Warum „Ändern…" nur als „Speichern unter" geht; leer = an Ort und Stelle.</summary>
    public string AendernGrund { get; set; } = "";

    /// <summary>Warum „Löschen" gesperrt ist (Auslieferung, benutzt); leer = löschbar.</summary>
    public string LoeschGrund { get; set; } = "";

    /// <summary>Die Kennzahlen im Kopf des Stammblatts.</summary>
    public IReadOnlyList<Stammblattkennzahl> Kennzahlen { get; set; } = Array.Empty<Stammblattkennzahl>();

    /// <summary>Bezugsart, Bedarf je Niveau samt Bandbreite, Bezugstemperaturen, Bilanzgrenze, Kalender, Ferienfaktor.</summary>
    public IReadOnlyList<Stammblattwert> Kennwerte { get; set; } = Stammblattwert.Keine;

    /// <summary>Monatsfaktoren und Wochenfaktoren.</summary>
    public IReadOnlyList<Stammblattwert> Gaenge { get; set; } = Stammblattwert.Keine;

    /// <summary>Der Tagesgangsatz und seine Vollständigkeit.</summary>
    public IReadOnlyList<Stammblattwert> Tagesgang { get; set; } = Stammblattwert.Keine;

    /// <summary>Je Zapfkategorie eine Zeile; ohne Kategorien der Hinweis „rechnet nur deterministisch".</summary>
    public IReadOnlyList<Stammblattwert> Kategorien { get; set; } = Stammblattwert.Keine;

    /// <summary>Quelle je Wertgruppe, Stand, Vorlage und Verwendung.</summary>
    public IReadOnlyList<Stammblattwert> Herkunft { get; set; } = Stammblattwert.Keine;

    /// <summary>Der Tagesgang je Einheit am mittleren Niveau (Bild der Zapfprofil-Vorschau); <c>null</c> ohne vollständigen Satz.</summary>
    public Zeichenmodell? TagesgangModell { get; set; }

    /// <summary>Die Monatsmengen je Einheit am mittleren Niveau; <c>null</c> wie oben.</summary>
    public Zeichenmodell? JahresgangModell { get; set; }

    /// <summary>Warum keine Vorschau steht; leer, wenn beide Bilder da sind.</summary>
    public string VorschauGrund { get; set; } = "";
}

/// <summary>Was der Editor tut: eine neue Zeile anlegen, eine freie Zeile ändern oder die Werte als neue Zeile speichern.</summary>
public enum TwwEditorModus
{
    /// <summary>„Neu…": eine eigene Zeile (Status eigen); die Werte der gewählten Zeile sind vorbelegt.</summary>
    Neu = 0,

    /// <summary>„Ändern…" an einer freien Zeile (eigen, unbenutzt): an Ort und Stelle.</summary>
    Aendern = 1,

    /// <summary>„Speichern unter…" bzw. „Ändern…" an einer gesperrten Zeile: eine neue Zeile, die Vorlage bleibt.</summary>
    SpeichernUnter = 2
}

/// <summary>
/// <b>Die Eingaben des Editors</b> — die schreibbaren Werte einer Nutzungsart, wie sie im Editor
/// stehen (leer = <c>null</c>): Kennung, Bezugsart, Bedarf je Niveau samt Bandbreite,
/// Bezugstemperaturen, Bilanzgrenze, Kalenderart, Ferienfaktor, Monatsfaktoren und der
/// Tagesgangsatz. Die Wochenfaktoren bearbeitet „Tagesgang…"; der Editor übernimmt sie aus der
/// Bezugszeile. Status, Schloss, Vorlage und Provenienz setzt der Kern.
/// </summary>
public sealed class TwwNutzungsartEntwurfDaten
{
    /// <summary>Drei Niveaus.</summary>
    public const int NIVEAUS = 3;

    /// <summary>Zwölf Monate.</summary>
    public const int MONATE = 12;

    /// <summary>Die Bezugszeile: bei „Ändern" die Zeile selbst, sonst die Vorlage; 0 = keine.</summary>
    public int IdBezug { get; set; }

    public string Bezeichner { get; set; } = "";
    public string Katalogversion { get; set; } = "";

    /// <summary>Die Bezugsart (Id wie im Kern, 1 … 7); 0 = keine.</summary>
    public int Bezugsart { get; set; }

    /// <summary>Bedarf je Einheit und Tag [kWh] je Niveau (niedrig, mittel, hoch).</summary>
    public double?[] Bedarf { get; set; } = new double?[NIVEAUS];

    /// <summary>Untere Grenze der Bandbreite je Niveau; leer = keine Angabe.</summary>
    public double?[] BedarfMin { get; set; } = new double?[NIVEAUS];

    /// <summary>Obere Grenze der Bandbreite je Niveau; leer = keine Angabe.</summary>
    public double?[] BedarfMax { get; set; } = new double?[NIVEAUS];

    /// <summary>Die Zapftemperatur, auf die sich der Bedarf bezieht [°C].</summary>
    public double? Zapftemperatur { get; set; }

    /// <summary>Die Kaltwassertemperatur, auf die sich der Bedarf bezieht [°C].</summary>
    public double? Kaltwasser { get; set; }

    /// <summary>Die Bilanzgrenze (Id wie im Kern, 1 … 3); 0 = keine.</summary>
    public int Bilanzgrenze { get; set; }

    /// <summary>Die Kalenderart (Id wie im Kern, 1 … 5); 0 = keine.</summary>
    public int Kalenderart { get; set; }

    /// <summary>Der Ferienfaktor; leer = keiner.</summary>
    public double? Ferienfaktor { get; set; }

    /// <summary>Die zwölf Monatsfaktoren; beim Speichern normiert der Kern auf das Mittel 1.</summary>
    public double?[] Monatsfaktoren { get; set; } = new double?[MONATE];

    /// <summary>Der Tagesgangsatz (Id des Katalogs); 0 = keiner.</summary>
    public int IdTagesgangsatz { get; set; }

    /// <summary>Eine tiefe Kopie — der Editor arbeitet nie auf dem hereingereichten Stand.</summary>
    public TwwNutzungsartEntwurfDaten Kopie() => new()
    {
        IdBezug = IdBezug,
        Bezeichner = Bezeichner,
        Katalogversion = Katalogversion,
        Bezugsart = Bezugsart,
        Bedarf = (double?[])Bedarf.Clone(),
        BedarfMin = (double?[])BedarfMin.Clone(),
        BedarfMax = (double?[])BedarfMax.Clone(),
        Zapftemperatur = Zapftemperatur,
        Kaltwasser = Kaltwasser,
        Bilanzgrenze = Bilanzgrenze,
        Kalenderart = Kalenderart,
        Ferienfaktor = Ferienfaktor,
        Monatsfaktoren = (double?[])Monatsfaktoren.Clone(),
        IdTagesgangsatz = IdTagesgangsatz
    };
}

/// <summary>
/// <b>Der Stand des Editors beim Öffnen</b>: Modus, der vorbelegte Entwurf, die Wahllisten
/// (Bezugsart, Bilanzgrenze, Kalenderart, vollständige Tagesgangsätze), die Wochenfaktoren der
/// Bezugszeile als Text und der Hinweis zum Modus (bei „Speichern unter" der Sperrgrund). Kann der
/// Editor nicht schreiben (keine Tabellen, Zeile fort), nennt <see cref="Grund"/> es.
/// </summary>
public sealed class TwwNutzungsartEditorDaten
{
    public TwwEditorModus Modus { get; set; }
    public TwwNutzungsartEntwurfDaten Entwurf { get; set; } = new();
    public IReadOnlyList<(int Id, string Text)> Bezugsarten { get; set; } = Array.Empty<(int, string)>();
    public IReadOnlyList<(int Id, string Text)> Bilanzgrenzen { get; set; } = Array.Empty<(int, string)>();
    public IReadOnlyList<(int Id, string Text)> Kalenderarten { get; set; } = Array.Empty<(int, string)>();
    public IReadOnlyList<(int Id, string Text)> Tagesgangsaetze { get; set; } = Array.Empty<(int, string)>();

    /// <summary>Die zwölf Monatsnamen der Oberflächensprache (Beschriftung der Monatsfaktoren).</summary>
    public IReadOnlyList<string> Monatsnamen { get; set; } = Array.Empty<string>();

    /// <summary>Die Wochenfaktoren der Bezugszeile als Text („Mo 20 % · … · So 0 %").</summary>
    public string Wochenfaktoren { get; set; } = "";

    /// <summary>Der Hinweis zum Modus; bei „Speichern unter" nach „Ändern…" der Sperrgrund der Zeile.</summary>
    public string Hinweis { get; set; } = "";

    /// <summary>Kann der Editor schreiben? Sonst nennt <see cref="Grund"/> den Grund.</summary>
    public bool Verfuegbar { get; set; } = true;

    public string Grund { get; set; } = "";
}

/// <summary>
/// Das Ergebnis eines Schreibwegs des Katalogdialogs (Editor, Löschen): geschrieben, die Id der
/// Zeile, die jetzt die Werte trägt (bei „Speichern unter" die neue), und bei einer Ablehnung die
/// benannte Meldung samt Kennung (der Ausgang des Kerns als Ressourcenschlüssel).
/// </summary>
public sealed record TwwNutzungsartSpeicherErgebnis(bool Ok, int Id, string Meldung, string Kennung);

/// <summary>Wie ein Eintrag des Pakets im Import ausging.</summary>
public enum TwwImportausgangDaten
{
    Angelegt = 0,
    Uebersprungen = 1,
    Abgelehnt = 2,

    /// <summary>Die vorhandene Zeile trägt jetzt die Werte des Pakets (Bedarfstag, Parameter).</summary>
    Ersetzt = 3
}

/// <summary>Zu welcher Tabelle eine Zeile des Importberichts gehört — der Bericht zeigt sie in Gruppen.</summary>
public enum TwwImportbereichDaten
{
    Nutzungsart = 0,
    Bedarfstag = 1,
    Parameter = 2
}

/// <summary>Eine Zeile des Importberichts — fertig formatiert.</summary>
/// <param name="Nutzungsart">Bezeichner und Katalogversion aus dem Paket (oder „Zeile n").</param>
/// <param name="Ausgang">Der Ausgang.</param>
/// <param name="Ausgangstext">Der Ausgang in der Oberflächensprache.</param>
/// <param name="Grund">Der benannte Grund; leer bei „angelegt" unter eigenem Namen.</param>
/// <param name="IdNeu">Die Id der angelegten oder ersetzten Zeile; 0 sonst.</param>
public sealed record TwwImportzeileDaten(string Nutzungsart, TwwImportausgangDaten Ausgang, string Ausgangstext,
                                         string Grund, int IdNeu)
{
    /// <summary>Die Tabelle, zu der die Zeile gehört (Vorgabe: die Nutzungsarten).</summary>
    public TwwImportbereichDaten Bereich { get; init; }
}

/// <summary>
/// <b>Der Bericht eines Katalogimports</b>: die Zusammenfassung, je Eintrag eine Zeile in ihrer
/// Gruppe (Bedarfstage, Parameter, Nutzungsarten), die
/// Hinweise (übergangene Dateien und Zeilen, Ersetzungen) und — wenn das Paket als Ganzes
/// abgelehnt ist — der Grund; dann ist nichts geändert.
/// </summary>
public sealed class TwwImportberichtDaten
{
    public bool Abgebrochen { get; set; }
    public string Abbruch { get; set; } = "";
    public string Zusammenfassung { get; set; } = "";
    public List<TwwImportzeileDaten> Zeilen { get; set; } = new();
    public List<string> Hinweise { get; set; } = new();

    /// <summary>War es ein Prüflauf? Dann ist nichts geschrieben, und die Zeilen sagen „würde …".</summary>
    public bool Pruefmodus { get; set; }

    /// <summary>Die Ids der angelegten Nutzungsarten — die erste wird nach dem Import gewählt.</summary>
    public IReadOnlyList<int> NeueIds => Zeilen
        .Where(z => z.Ausgang == TwwImportausgangDaten.Angelegt && z.Bereich == TwwImportbereichDaten.Nutzungsart)
        .Select(z => z.IdNeu).ToList();

    /// <summary>Die Zeilen einer Gruppe, in ihrer Reihenfolge.</summary>
    public IReadOnlyList<TwwImportzeileDaten> ZeilenVon(TwwImportbereichDaten bereich)
        => Zeilen.Where(z => z.Bereich == bereich).ToList();
}
