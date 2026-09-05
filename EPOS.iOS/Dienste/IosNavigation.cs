using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="WindowsFormsApplication1.INavigation"/>: Sie reicht die
/// Maskenschluessel des Kerns an die gerade gezeichnete Blazor-Wurzel weiter.
///
/// <para><b>Die Aufrufrichtung.</b> Der Kern kennt nur einen SCHLUESSEL
/// (<c>Masken.*</c>) und ueberlaesst das Bauen der
/// Oberflaeche. Unter Windows beantwortet <c>WinFormsNavigation</c> das mit
/// einem <c>ShowDialog</c>. Auf iOS gibt es kein zweites Fenster: Der
/// Schluessel geht an <c>EPOS.UI.Dienste.Navigationsziel.Aktuell</c>, also an
/// <c>AppWurzel</c>, und die tauscht ihre Ansicht.</para>
///
/// <para><b>Diese Datei kennt keine iOS-API</b> - sie ist reine Weiterleitung
/// und laesst sich deshalb ohne Mac uebersetzen und pruefen.</para>
///
/// <para><b>Was iU10 kann und was nicht.</b> Bekannt sind die zwei
/// umgestellten Dialoge und die Projektliste. <see cref="AnsichtAktualisieren"/>
/// schreibt eine Protokollzeile und tut sonst nichts. Ein unbekannter
/// Schluessel liefert <c>false</c> - derselbe
/// Ausgang wie <see cref="KeineNavigation"/>, und der Aufrufer wertet ihn wie
/// „Abbrechen". Mit dem Assistenten (iU10-9, iL5) kommen die uebrigen
/// Schluessel dazu.</para>
/// </summary>
public sealed class IosNavigation : WindowsFormsApplication1.INavigation   // voll qualifiziert: MAUI fuehrt Microsoft.Maui.Controls.INavigation als globales using (CS0104, dritter CI-Lauf 03.09.2026)
{
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

        // In iU10 gibt es genau eine Zuordnung: Die Projektauswahl entspricht der
        // Projektliste. Die uebrigen Masken haben auf iOS noch kein Gegenstueck.
        //
        // iU9-W16b.1 (E-7, K6-a): Masken.ProjektDetail stand hier daneben - das
        // Detailformular des Projekts, das auf iOS ohnehin nie gebaut wurde. Es ist
        // unter Windows geloescht, der Schluessel gibt es nicht mehr.
        if (maske == Masken.ProjektAuswahl)
            return Seitenschluessel.Projektliste;

        return maske;
    }

    private static void Protokoll(string zeile)
    {
        try { Console.WriteLine(zeile); } catch { }
    }
}
