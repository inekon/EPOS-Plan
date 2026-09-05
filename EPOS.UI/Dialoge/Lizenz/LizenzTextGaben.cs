using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Lizenz;

/// <summary>
/// Ein Abschnitt des erzeugten Rechtstextes: eine Ueberschrift oder ein Absatz
/// (iU9-W15c.11).
///
/// <para>Der Vorlaeufer schrieb die 27 Abschnitte mit
/// <c>SchreibeUeberschrift</c>/<c>SchreibeAbsatz</c> in eine <c>RichTextBox</c> und
/// setzte dabei Schriftschnitt und Farbe je Zeile. Hier ist die UNTERSCHEIDUNG die
/// Aussage — wie sie aussieht, entscheidet die Gestaltung.</para>
/// </summary>
/// <param name="IstUeberschrift"><c>true</c> = Ueberschrift, sonst Absatz.</param>
/// <param name="Text">Der Text; er kann Umbrueche enthalten.</param>
public sealed record RechtsAbschnitt(bool IstUeberschrift, string Text);

/// <summary>
/// Der Zustand der ersten Registerkarte: der Vertragstext samt seiner Herkunft
/// (iU9-W15c.11).
///
/// <para><b>Drei Quellen, eine Form.</b> Der Text kommt aus einer oertlichen
/// Vertragsdatei, aus dem Zwischenspeicher der zuletzt geholten Fassung oder frisch
/// von <c>epos-plan.de</c>; welche es war, sagt <paramref name="Quelle"/>. Die
/// Komponente entscheidet das nicht — sie zeigt, was die Huelle liefert
/// (<c>LizenzTextCtrl</c> im Kern).</para>
/// </summary>
/// <param name="Text">Der anzuzeigende Vertragstext.</param>
/// <param name="Quelle">Pfad oder Adresse, aus der er stammt.</param>
/// <param name="Stand">Das Aenderungsdatum der Online-Fassung; leer, wenn unbekannt.</param>
public sealed record LizenzTextGaben(string Text, string Quelle, string Stand)
{
    /// <summary>Ein leerer Stand — die Vorbelegung, solange nichts geladen ist.</summary>
    public static LizenzTextGaben Leer { get; } = new("", "", "");
}
