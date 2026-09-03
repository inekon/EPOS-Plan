using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="INavigation"/>: Sie reicht die
/// Maskenschluessel des Kerns an die gerade gezeichnete Blazor-Wurzel weiter.
///
/// <para><b>Die Aufrufrichtung.</b> Der Kern kennt nur einen SCHLUESSEL
/// (<c>Masken.*</c>, <c>Gewerke.*</c>) und ueberlaesst das Bauen der
/// Oberflaeche. Unter Windows beantwortet <c>WinFormsNavigation</c> das mit
/// einem <c>ShowDialog</c>. Auf iOS gibt es kein zweites Fenster: Der
/// Schluessel geht an <c>EPOS.UI.Dienste.Navigationsziel.Aktuell</c>, also an
/// <c>AppWurzel</c>, und die tauscht ihre Ansicht.</para>
///
/// <para><b>Diese Datei kennt keine iOS-API</b> - sie ist reine Weiterleitung
/// und laesst sich deshalb ohne Mac uebersetzen und pruefen.</para>
///
/// <para><b>Was iU10 kann und was nicht.</b> Bekannt sind die zwei
/// umgestellten Dialoge und die Projektliste. <see cref="OeffneGewerk"/> und
/// <see cref="AnsichtAktualisieren"/> schreiben eine Protokollzeile und tun
/// sonst nichts: Die Gewerkslisten des Detailformulars gibt es auf iOS noch
/// gar nicht. Ein unbekannter Schluessel liefert <c>false</c> - derselbe
/// Ausgang wie <see cref="KeineNavigation"/>, und der Aufrufer wertet ihn wie
/// „Abbrechen". Mit dem Assistenten (iU10-9, iL5) kommen die uebrigen
/// Schluessel dazu.</para>
/// </summary>
public sealed class IosNavigation : INavigation
{
    /// <inheritdoc/>
    public void OeffneGewerk(string gewerk, int idProjekt, string projektname)
    {
        Protokoll("Navigation: Gewerk '" + (gewerk ?? "") + "' fuer Projekt " + idProjekt +
                  " - auf iOS noch ohne Ansicht (iU10-9).");
    }

    /// <inheritdoc/>
    public bool OeffneMaske(string maske, params object[] argumente)
    {
        INavigationsZiel? ziel = Navigationsziel.Aktuell;
        if (ziel == null)
        {
            Protokoll("Navigation: '" + (maske ?? "") + "' - keine Oberflaeche angemeldet.");
            return false;
        }

        return ziel.OeffneMaske(Uebersetze(maske), argumente);
    }

    /// <inheritdoc/>
    /// <remarks>Es gibt keine Menueleiste - die Wurzel entscheidet selbst, was sie zeigt.</remarks>
    public void MenueAktualisieren()
    {
    }

    /// <inheritdoc/>
    public void AnsichtAktualisieren(string bereich)
    {
        // Die drei Bereichsschluessel (VARIANTEN, BERICHTE_KOSTEN,
        // PROJEKT_DETAIL) gehoeren zur Startmaske von Windows. Auf iOS gibt es
        // nur die eine Wurzel; sie wird vollstaendig aufgefrischt.
        Navigationsziel.Aktuell?.Auffrischen();
    }

    /// <summary>
    /// Uebersetzt einen Maskenschluessel des Kerns in einen Seitenschluessel
    /// von EPOS.UI. Unbekanntes bleibt unveraendert - die Wurzel antwortet dann
    /// selbst mit <c>false</c>.
    /// </summary>
    private static string Uebersetze(string maske)
    {
        if (string.IsNullOrEmpty(maske)) return "";

        // In iU10 gibt es genau eine Zuordnung: Das Detailformular des
        // Projekts entspricht der Projektliste. Die uebrigen Masken haben auf
        // iOS noch kein Gegenstueck.
        if (maske == Masken.ProjektDetail || maske == Masken.ProjektAuswahl)
            return Seitenschluessel.Projektliste;

        return maske;
    }

    private static void Protokoll(string zeile)
    {
        try { Console.WriteLine(zeile); } catch { }
    }
}
