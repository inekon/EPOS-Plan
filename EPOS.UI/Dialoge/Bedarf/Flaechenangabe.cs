using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Art der Angabe als Anzeigetext</b> (Anwenderentscheid 06.10.2026): Die Flächenangabe
/// eines Gebäudes heißt auf der Maske „Nutzfläche [m²]" — bei einem Wohngebäude wie bei einem
/// Gewerbebau. Gespeichert ist sie unverändert als Steuerwert <see cref="GESPEICHERT"/> in
/// <c>Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche</c>; der Rechenweg vergleicht diesen
/// Wert. Die Zuordnung gilt deshalb NUR für die Anzeige: Vergleiche, Auswahl und Rückgabe
/// bleiben auf dem gespeicherten Wert, und jede andere Bedarfsart (ein Verbrauch) steht so da,
/// wie sie gespeichert ist.
/// </summary>
public static class Flaechenangabe
{
    /// <summary>Der gespeicherte Steuerwert der Flächenangabe — wörtlich, nie übersetzt.</summary>
    public const string GESPEICHERT = "Wohnfläche [m²]";

    /// <summary>Der Anzeigetext eines gespeicherten Werts: die Flächenangabe als „Nutzfläche [m²]", alles andere unverändert.</summary>
    public static string Anzeige(string? gespeichert)
        => string.Equals(gespeichert, GESPEICHERT, System.StringComparison.Ordinal)
            ? Resource.GEB_TXT_EINHEIT_FLAECHE
            : gespeichert ?? "";
}
