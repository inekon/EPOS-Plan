namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die drei Betriebsarten des Gebäude-Katalogeditors: einen Katalogsatz bearbeiten, einen neuen
/// anlegen (Gebäudedialog, Import, „Neu…" der Verwaltung) und ein Gebäude im Projekt bearbeiten.
///
/// <para>Die Gebäudeverwaltung (Menü Administration › Gebäude) ist eine eigene Komponente
/// (<c>GebaeudeAdminDialog</c>) und bearbeitet im Stammblatt; den Editor ruft sie nur für „Neu…"
/// in der Betriebsart <see cref="Neu"/>.</para>
/// </summary>
public enum GebaeudeKatalogModus
{
    /// <summary>
    /// Ein vorhandener Katalogsatz wird bearbeitet („DB ändern"). „Überschreiben" und
    /// „Speichern unter" sind frei.
    /// </summary>
    Bearbeiten,

    /// <summary>
    /// Ein neuer Katalogsatz entsteht („DB neu"). Nur „Speichern" ist frei — derselbe
    /// Knopf wie „Speichern unter", mit anderem Text.
    /// </summary>
    Neu,

    /// <summary>
    /// Ein Gebäude IM PROJEKT (Gebäudesimulation G3, Welle D2; Knopf „Hülle und Zonen…" des
    /// Gebäudedialogs): Bearbeitet wird die Projektkopie (<c>Tab_Gebaeude</c>), nicht der
    /// Katalogsatz, samt Zonen und Bauteilen. OK schreibt in benannten Schritten Gebäudedaten,
    /// übernommene Katalogaufbauten und Zonen; „Speichern unter" legt einen Katalogsatz an — ohne
    /// Zonen, nach einer Rückfrage. Der Name der Projektkopie bleibt.
    /// </summary>
    Projekt
}

/// <summary>
/// Das Ergebnis eines Schreibversuchs im Gebäudekatalog (iU9-W9.1) — dieselbe Form wie
/// <c>KatalogSpeicherErgebnis</c> der Welle 6, aber mit eigenem Namen, damit der
/// Bedarfsordner nicht am Erzeugerordner hängt.
/// </summary>
/// <param name="Erfolg">Wurde geschrieben?</param>
/// <param name="Meldung">Der Grund, wenn nicht — z. B. die ReadOnly-Sperre.</param>
public sealed record GebaeudeKatalogErgebnis(bool Erfolg, string Meldung);
