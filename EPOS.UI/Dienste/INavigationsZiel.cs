namespace EPOS.UI.Dienste;

/// <summary>
/// Das Blazor-Ende von <c>WindowsFormsApplication1.INavigation</c>.
///
/// <para><b>Wozu.</b> Der Rechenkern oeffnet Masken ueber
/// <c>Dienste.Navigation.OeffneMaske("Form_BHKWAdmin", …)</c> - er kennt einen
/// SCHLUESSEL, nicht das Fenster. Unter Windows beantwortet
/// <c>WinFormsNavigation</c> das mit einem <c>ShowDialog</c>. Auf iOS gibt es
/// kein Fenster: Dort meldet sich die gerade gezeichnete Wurzelkomponente
/// (<see cref="EPOS.UI.Seiten.AppWurzel"/>) als Ziel an, und der iOS-Adapter
/// <c>IosNavigation</c> reicht den Schluessel hierher weiter.</para>
///
/// <para><b>Warum die Schnittstelle in EPOS.UI liegt und nicht im Kern.</b> Der
/// Kern hat mit <c>INavigation</c> bereits seine Sicht auf die Sache. Diese hier
/// ist die Gegenrichtung - was eine OBERFLAECHE anbieten muss, damit ein
/// Plattformadapter sie ansprechen kann. Sie gehoert damit zur
/// Oberflaechenbibliothek, genau wie <see cref="IHilfeDienst"/>.</para>
///
/// <para><b>Rueckgabe <c>false</c> heisst „diese Maske gibt es hier nicht".</b>
/// Das ist derselbe Zustand, den <c>KeineNavigation</c> im Kern liefert, und
/// ein gueltiger: Der Aufrufer wertet ihn wie „Abbrechen" und aendert nichts.
/// In iU10 kennt die Wurzel genau zwei Schluessel; alles Weitere kommt mit dem
/// Assistenten (iU10-9 / iL5).</para>
/// </summary>
public interface INavigationsZiel
{
    /// <summary>
    /// Oeffnet die Maske zu einem sprachneutralen Schluessel
    /// (<c>WindowsFormsApplication1.Masken.*</c> bzw. die beiden Schluessel aus
    /// <see cref="EPOS.UI.Seiten.Seitenschluessel"/>).
    /// </summary>
    /// <param name="maske">Der Maskenschluessel; ASCII, nie ein Anzeigetext.</param>
    /// <param name="argumente">Zusatzangaben der jeweiligen Maske.</param>
    /// <returns><c>false</c>, wenn der Schluessel hier unbekannt ist.</returns>
    bool OeffneMaske(string maske, params object[] argumente);

    /// <summary>Zeichnet die gerade sichtbare Ansicht neu.</summary>
    void Auffrischen();
}

/// <summary>
/// Der Halter des gerade gezeichneten <see cref="INavigationsZiel"/>.
///
/// <para><b>Warum ein statischer Halter.</b> Dasselbe Muster wie
/// <c>WindowsFormsApplication1.Dienste</c> im Kern (Entscheidungsregister § 2.6):
/// Der Plattformadapter <c>IosNavigation</c> ist ein einfaches Objekt ohne
/// Blazor-Bezug und kann die Wurzelkomponente nicht per Einspritzung bekommen -
/// sie entsteht erst, wenn die <c>BlazorWebView</c> zeichnet. Die Wurzel meldet
/// sich deshalb selbst an und beim Verwerfen wieder ab.</para>
///
/// <para><c>null</c> heisst „es zeichnet gerade keine Oberflaeche". Der Adapter
/// laeuft dann leer und liefert <c>false</c> - genau wie <c>KeineNavigation</c>
/// im Kern.</para>
/// </summary>
public static class Navigationsziel
{
    /// <summary>Die zuletzt gezeichnete Wurzel; <c>null</c> = keine Oberflaeche.</summary>
    public static INavigationsZiel? Aktuell { get; set; }
}
