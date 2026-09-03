namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die drei Betriebsarten des Gebäude-Katalogeditors (iU9-W9.1) — wörtlich aus
/// <c>Form_Gebaeude1_Load</c>:21-44, wo sie über die beiden Schalter <c>m_bNeu</c> und
/// <c>m_bAdmin</c> aufgespannt wurden.
///
/// <para>Ein Aufzählungstyp statt zweier Schalter: Von den vier Kombinationen der beiden
/// Schalter waren nur drei gemeint, und die vierte (<c>m_bNeu &amp;&amp; m_bAdmin</c>)
/// hätte sich selbst widersprochen.</para>
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
    /// Katalogverwaltung (Menü → Gebäudeverwaltung). Der Name wird zur Klappliste ALLER
    /// Katalogsätze, „Überschreiben" ist frei und „Speichern" gesperrt.
    /// </summary>
    Admin
}

/// <summary>
/// Das Ergebnis eines Schreibversuchs im Gebäudekatalog (iU9-W9.1) — dieselbe Form wie
/// <c>KatalogSpeicherErgebnis</c> der Welle 6, aber mit eigenem Namen, damit der
/// Bedarfsordner nicht am Erzeugerordner hängt.
/// </summary>
/// <param name="Erfolg">Wurde geschrieben?</param>
/// <param name="Meldung">Der Grund, wenn nicht — z. B. die ReadOnly-Sperre.</param>
public sealed record GebaeudeKatalogErgebnis(bool Erfolg, string Meldung);
