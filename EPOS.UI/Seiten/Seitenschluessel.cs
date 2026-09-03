namespace EPOS.UI.Seiten;

/// <summary>
/// Die sprachneutralen Schluessel der Ansichten, die
/// <see cref="AppWurzel"/> kennt - dieselbe Drei-Schichten-Regel wie
/// <c>WindowsFormsApplication1.Masken</c> und <c>…Gewerke</c>: ASCII,
/// sprachneutral, nie ein Anzeigetext.
///
/// <para>Zwei Schluessel tragen einen Dialog, einer die Liste. Mehr kennt iU10
/// nicht; der Assistent (iL5) kommt mit iU10-9 und bringt seine eigenen
/// Schluessel mit. Ein unbekannter Schluessel tut nichts und liefert
/// <c>false</c> - derselbe Ausgang, den <c>KeineNavigation</c> im Kern
/// liefert.</para>
/// </summary>
public static class Seitenschluessel
{
    /// <summary>Die Projektliste - der Einstieg.</summary>
    public const string Projektliste = "PROJEKTLISTE";

    /// <summary>Der Dialog „Energieträger anlegen" (<c>EnergietraegerVarianteDialog</c>).</summary>
    public const string Energietraeger = "ENERGIETRAEGER_VARIANTE";

    /// <summary>Der Dialog „BHKW-Wirtschaftlichkeit" (<c>BhkwWirtschaftlichkeitDialog</c>).</summary>
    public const string BhkwWirtschaftlichkeit = "BHKW_WIRTSCHAFTLICHKEIT";
}
