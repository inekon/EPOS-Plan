using System.Linq;
using EPOS.UI.Seiten.Start;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Kachel „Gebäudekühlung“</b> im Reiter „Kältebedarf“ der Startseite (Konzept Kältebedarf
    /// 3.5) — plattformfrei aus Kern-Controllern gebaut, nach dem Muster <see cref="KuehlungKachelBau"/>:
    /// <list type="bullet">
    /// <item>die Projektgebäude samt „Gebäude wird gekühlt“ aus <see cref="ProjektGebaeudeCtrl.ReadAll"/>;</item>
    /// <item>die Projekteinstellung „Kühlung rechnen“ aus <see cref="KonfigurationCtrl.KuehlbetriebLesen"/>.</item>
    /// </list>
    /// Die Gebäudekühlung hat kein Bit im Komponentenbestand: Sie ist keine eigene Komponente, sondern ein
    /// Merkmal der Gebäude.
    /// </summary>
    internal static class GebaeudekuehlungKachelBau
    {
        /// <summary>Der Stand der Kachel für ein Projekt; <c>null</c> ohne Projekt.</summary>
        internal static GebaeudekuehlungDaten Daten(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            var gebaeude = new ProjektGebaeudeCtrl();
            gebaeude.ReadAll(idProjekt);

            return new GebaeudekuehlungDaten(
                KonfigurationCtrl.KuehlbetriebLesen(idProjekt),
                gebaeude.items.Count(g => g.Kuehlung_Aktiv),
                gebaeude.items.Count);
        }
    }
}
