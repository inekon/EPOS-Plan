namespace EPOS.UI.Seiten;

/// <summary>
/// Die sprachneutralen Schluessel der Ansichten, die
/// <see cref="AppWurzel"/> kennt - dieselbe Drei-Schichten-Regel wie
/// <c>WindowsFormsApplication1.Masken</c> und <c>…Gewerke</c>: ASCII,
/// sprachneutral, nie ein Anzeigetext.
///
/// <para>Zwei Schluessel tragen einen Dialog, einer die Liste, zwei eine
/// Fachseite - und seit iU9-W15b einer den KI-Assistenten. Ein unbekannter
/// Schluessel tut nichts und liefert <c>false</c> - derselbe Ausgang, den
/// <c>KeineNavigation</c> im Kern liefert.</para>
/// </summary>
public static class Seitenschluessel
{
    /// <summary>Die Projektliste - der Einstieg.</summary>
    public const string Projektliste = "PROJEKTLISTE";

    /// <summary>Der Dialog „Energieträger anlegen" (<c>EnergietraegerVarianteDialog</c>).</summary>
    public const string Energietraeger = "ENERGIETRAEGER_VARIANTE";

    /// <summary>Der Dialog „BHKW-Wirtschaftlichkeit" (<c>BhkwWirtschaftlichkeitDialog</c>).</summary>
    public const string BhkwWirtschaftlichkeit = "BHKW_WIRTSCHAFTLICHKEIT";

    /// <summary>
    /// Die Simulationskonfiguration (<c>Simulation.SimulationKonfigSeite</c>,
    /// iU9-W10b.1) — die erste FACHSEITE, die iOS ueber <see cref="AppWurzel"/>
    /// erreicht. Unter Windows steht dieselbe Komponente bis W16 in einem modalen
    /// Fenster (Entscheid R-W10b-1); der Schluessel gilt fuer beide Wege.
    /// </summary>
    public const string SimulationKonfiguration = "SIMULATION_KONFIGURATION";

    /// <summary>
    /// Das Simulationsergebnis (<c>Simulation.SimulationErgebnisSeite</c>,
    /// iU9-W11b.13) — die zweite FACHSEITE, die iOS ueber <see cref="AppWurzel"/>
    /// erreicht. Unter Windows steht dieselbe Komponente bis W16 in einem modalen
    /// Fenster (Entscheid R-W11-1); der Schluessel gilt fuer beide Wege.
    /// </summary>
    public const string SimulationErgebnis = "SIMULATION_ERGEBNIS";

    /// <summary>
    /// Der KI-Hilfe-Assistent (<c>Dialoge.Hilfe.KiChatDialog</c>, iU9-W15b.7,
    /// Entscheid E-10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Klassenkopf kuendigte ihn seit iU10-2 an ("der Assistent (iL5) kommt
    /// mit iU10-9 und bringt seine eigenen Schluessel mit"). Er ist der erste
    /// Schluessel, den KEINE Kachel der Projektliste oeffnet: Unter Windows steht
    /// der Chat am Menue "Hilfe -&gt; Hilfe-Assistent (KI)…" und an F1, auf iOS am
    /// Baustein <c>KiKnopf</c> jeder Maske.
    /// </para>
    /// <para>
    /// <b>Er hat bewusst KEINEN <c>Masken.*</c>-Zwilling</b> (Befund W15b-B4): Der
    /// Chat wurde nie ueber die Sprungtabelle geoeffnet, und die Tabelle faellt mit
    /// Welle 16 ohnehin.
    /// </para>
    /// <para>
    /// Der Kern kennt denselben Wert als Zeichenkette in
    /// <c>KiChatKontext.BEREICH_JE_SEITE</c> - dort bildet er auf den Bereich
    /// "Hilfe" ab, damit der Assistent auf iOS weiss, wovon der Anwender spricht
    /// (Entscheid E-9).
    /// </para>
    /// </remarks>
    public const string KiAssistent = "KI_ASSISTENT";
}
