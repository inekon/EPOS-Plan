using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Prüfhilfen des Gebäudedialogs</b> (Katalogauswahl V1, Stufe 4): Die Knöpfe des Satzes (Fläche und Verbrauch…,
/// Simulation…, Export, Datei erneut lesen, Gebäudetyp) stehen in der Satz-Überlagerung; „Bearbeiten" der Detailzeile
/// öffnet sie — wie die Hand.
/// </summary>
internal static class GebaeudeDialogProbe
{
    /// <summary>Öffnet die Satz-Überlagerung über „Bearbeiten" der Detailzeile, sofern sie nicht schon offen ist.</summary>
    public static void SatzOeffnen(IRenderedComponent<GebaeudeDialog> cut)
    {
        if (cut.Instance.Zweispalten?.SatzUeberlagerungOffen == true) return;
        // AW3b: Mit der Knopfleiste am Satz oeffnet „Vergrößern" den Projektsatz (Bearbeiten… ist dort „Hülle und
        // Zonen"); ohne Editor, im Assistenten und beim Katalogsatz ist es „Bearbeiten…" am Satz.
        IElement? k = cut.FindAll(".epos-zweispalten-satzknoepfe .epos-zweispalten-vergroessern").FirstOrDefault()
                   ?? cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt, .epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog").FirstOrDefault();
        if (k is not null && !k.HasAttribute("disabled")) k.Click();
    }

    /// <summary>Ein Knopf des Satzes — in der Satz-Überlagerung.</summary>
    public static IElement SatzKnopf(IRenderedComponent<GebaeudeDialog> cut, string selektor)
    {
        SatzOeffnen(cut);
        return cut.Find(selektor);
    }

    /// <summary>Alle Knöpfe des Satzes zu einem Selektor — in der Satz-Überlagerung (leer, wenn es keinen gibt).</summary>
    public static IReadOnlyList<IElement> SatzKnoepfe(IRenderedComponent<GebaeudeDialog> cut, string selektor)
    {
        SatzOeffnen(cut);
        return cut.FindAll(selektor);
    }
}
