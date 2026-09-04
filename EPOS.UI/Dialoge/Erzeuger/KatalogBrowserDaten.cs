using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Eine Zeile der Katalogliste (iU9-W14a.1).
/// </summary>
/// <param name="Id">Primärschlüssel im Katalog — der Anker der Auswahl.</param>
/// <param name="Bezeichner">Erste Spalte; zugleich der Schlüssel jeder Aktion.</param>
/// <param name="Eigenschaften">
/// Zweite Spalte, mehrzeilig. Leer, wo die Liste einspaltig ist (Heizkessel,
/// Pufferspeicher — im Vorläufer eine <c>ListBox</c> statt eines
/// <c>DataGridView</c>).
/// </param>
/// <param name="Geschuetzt">
/// Schreibgeschützter Auslieferungssatz. Nur der BHKW-Browser zeichnet ihn grau
/// (<c>Form_BHKWAdmin.cs:202-203</c>); die drei anderen kennen die Kennzeichnung nicht.
/// </param>
public sealed record BrowserZeile(int Id, string Bezeichner, string Eigenschaften = "",
                                  bool Geschuetzt = false);

/// <summary>
/// Ein Detailfeld mit seinem aktuellen Wert.
/// </summary>
/// <remarks>
/// <para><b>Warum Text und nicht Zahl.</b> Die vier Vorläufer zeigen ihre Kennwerte
/// unterschiedlich: Der Heizkessel formatiert Leistung und Investitionskosten mit
/// <c>F2</c>, die drei anderen zeigen roh. Das ist Bestand und bleibt bitgleich — die
/// Hülle liefert deshalb den fertigen ANZEIGETEXT, und nur die editierbaren Felder des
/// Speicherwegs werden von der Komponente wieder als Zahl gelesen.</para>
/// <para>Der Schalter „Brennwertkessel" kommt sprachneutral als <c>"1"</c> oder
/// <c>"0"</c>; <see cref="Schalterwert"/> liest ihn.</para>
/// </remarks>
public sealed class BrowserFeldwert
{
    public required string Schluessel { get; init; }
    public required string Bezeichnung { get; init; }
    public string Einheit { get; init; } = "";
    public string Wert { get; set; } = "";

    /// <summary>Textfeld, Zahlenfeld, Ganzzahlfeld oder Schalter.</summary>
    public required WindowsFormsApplication1.BrowserFeldArt Art { get; init; }

    /// <summary>Schreibt der Speicherweg des Browsers dieses Feld zurück?</summary>
    public bool Editierbar { get; init; }

    /// <summary>Der Feldname, den eine Prüfmeldung nennt — die Beschriftung ohne „:".</summary>
    public string Feldname => (Bezeichnung ?? "").TrimEnd(' ', ':');

    /// <summary>Der Schalterwert; nur beim Feld „Brennwertkessel" belegt.</summary>
    public bool Schalterwert
    {
        get => Wert == "1";
        set => Wert = value ? "1" : "0";
    }
}

/// <summary>
/// Was der Katalogbrowser beim Verlassen meldet (iU9-W14a.1).
/// </summary>
/// <param name="Bestaetigt">
/// Hat der Anwender „OK" gedrückt? <b>Angleichung E-1:</b> Drei der vier Vorläufer
/// setzten überhaupt kein <c>DialogResult</c> und lieferten deshalb IMMER
/// <c>false</c> (Befund W14-B4); „OK" heißt jetzt OK.
/// </param>
/// <param name="Bezeichner">Der zuletzt gewählte Eintrag, leer wenn keiner.</param>
public sealed record BrowserErgebnis(bool Bestaetigt, string Bezeichner);

/// <summary>
/// Der Satz Delegaten, mit dem die Hülle den Browser an ihren Katalog hängt.
/// </summary>
/// <remarks>
/// Alles, was in die Datenbank greift, steht hier — die Komponente kennt weder
/// <c>DataRepository</c> noch einen Controller (<c>EPOS.UI/CLAUDE.md</c>).
/// </remarks>
public sealed class KatalogBrowserWege
{
    /// <summary>Die Liste zu den beiden Filterstellungen (Index in die Klapplisten).</summary>
    public Func<int, int, IReadOnlyList<BrowserZeile>>? Liste { get; init; }

    /// <summary>Die Detailfelder eines Eintrags; <c>null</c>, wenn es ihn nicht gibt.</summary>
    public Func<string, IReadOnlyList<BrowserFeldwert>?>? Detail { get; init; }

    /// <summary>Gibt es den Bezeichner schon? Der Vorabtest von „Neu…".</summary>
    public Func<string, bool>? Existiert { get; init; }

    /// <summary>Löscht einen Eintrag und sagt, warum es nicht ging.</summary>
    public Func<string, KatalogSpeicherErgebnis>? Loeschen { get; init; }

    /// <summary>
    /// Schreibt die editierbaren Felder zurück — nur bei Heizkessel und BHKW belegt.
    /// Zweiter Parameter: Darf der Schreibschutz übergangen werden (BHKW-Rückfrage)?
    /// </summary>
    public Func<string, IReadOnlyList<BrowserFeldwert>, bool, KatalogSpeicherErgebnis>? Speichern { get; init; }

    /// <summary>Trägt der Eintrag den Schreibschutz der Auslieferung? Nur beim BHKW belegt.</summary>
    public Func<string, bool>? IstGeschuetzt { get; init; }
}
