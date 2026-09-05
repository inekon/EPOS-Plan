using System;
using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>
/// Die Datenformen der Katalog-Dublettensuche (iU9-W14c.5).
///
/// <para><b>Warum sie hier stehen.</b> Der Dialog fragt seine Hülle in kleinen
/// Schritten: erst prüfen, dann fragen, dann tun. Jeder Schritt hat ein Ergebnis mit
/// mehr als einem Wert — ein <c>bool</c> allein sagt nicht, WARUM ein Satz stehen
/// bleibt. Der Vorläufer löste das mit vier <c>MessageBox</c> mitten im Handler;
/// hier kommt der Grund als Wert zurück, und die Komponente entscheidet, ob daraus
/// ein Banner oder eine Rückfrage wird.</para>
/// </summary>
public static class KatalogDublettenDaten
{
}

/// <summary>Das Ergebnis eines Scans: der Baum, die Statuszeile und neue Protokollzeilen.</summary>
/// <param name="Baum">Die Wurzeln des Befundbaums (<c>DublettenBaum.Bauen</c>).</param>
/// <param name="Status">Die Statuszeile — leer, wenn „der Baum spricht".</param>
/// <param name="Protokoll">Zeilen, die beim Scan angefallen sind (Lesefehler).</param>
public sealed record Scanergebnis(
    IReadOnlyList<WindowsFormsApplication1.DublettenKnoten> Baum,
    string Status,
    IReadOnlyList<string> Protokoll);

/// <summary>Ein Zwischenstand des Scans für den Baustein <c>Fortschritt</c>.</summary>
/// <param name="Anteil">0…1, oder <c>null</c> für „unbestimmt".</param>
/// <param name="Text">Was gerade läuft.</param>
public sealed record Scanmeldung(double? Anteil, string Text);

/// <summary>
/// Das Ergebnis einer schreibenden Aktion: Protokollzeilen und eine Meldung.
/// </summary>
/// <param name="Erfolgreich">Hat die Aktion geschrieben?</param>
/// <param name="Protokoll">Was ins Sitzungsprotokoll gehört.</param>
/// <param name="Meldung">Was ins Banner gehört; leer = nichts zu sagen.</param>
public sealed record Aktionsergebnis(
    bool Erfolgreich,
    IReadOnlyList<string> Protokoll,
    string Meldung = "")
{
    public static Aktionsergebnis Nichts(string meldung = "")
        => new(false, Array.Empty<string>(), meldung);
}

/// <summary>
/// Die drei Schranken vor dem Löschen eines Satzes (<c>btnLoeschen_Click</c>).
///
/// <para>Sie laufen in dieser Reihenfolge: <see cref="Gesperrt"/> hält an,
/// <see cref="KeineVerwendungspruefung"/> meldet und <b>lässt weiterlaufen</b>,
/// <see cref="Verwendet"/> fragt nach — und danach kommt in jedem Fall die
/// Endbestätigung.</para>
/// </summary>
/// <param name="Gesperrt">Auslieferungssatz — er bleibt stehen.</param>
/// <param name="KeineVerwendungspruefung">Der Katalog führt keine.</param>
/// <param name="Verwendet">Die Fundstellen als fertiger Text; leer = keine.</param>
/// <param name="Fehler">Die Verwendungsprüfung ist gescheitert (Befund W14c-B44).</param>
/// <param name="Name">Der Name des Satzes (für die Rückfrage).</param>
/// <param name="Id">Die Id des Satzes (für die Rückfrage).</param>
public sealed record Loeschpruefung(
    bool Gesperrt,
    bool KeineVerwendungspruefung,
    string Verwendet,
    string Fehler,
    string Name,
    int Id);

/// <summary>
/// Der Vorlauf zum Umbenennen: Auslieferungssätze bleiben gesperrt, sonst kommt der
/// heutige Name als Vorbelegung der Namensabfrage.
/// </summary>
/// <param name="Gesperrt">Auslieferungssatz — er wird nicht umbenannt.</param>
/// <param name="Name">Der heutige Name.</param>
public sealed record Umbenennung(bool Gesperrt, string Name);
