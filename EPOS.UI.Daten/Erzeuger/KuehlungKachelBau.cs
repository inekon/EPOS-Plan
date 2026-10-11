using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Start;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Kachel „Kühlung“</b> im Reiter Energieerzeuger der Startseite — plattformfrei aus
    /// Kern-Controllern gebaut:
    /// <list type="bullet">
    /// <item>die <b>Kältemaschinen</b> des Projekts aus <see cref="KaeltemaschineAnlageCtrl.ListeStill"/>;</item>
    /// <item>die <b>Wärmepumpen mit Kühlfunktion</b> aus <see cref="WPCtrl.KuehlfaehigeGeraete"/> samt Kühlbetrieb
    /// und Sperrgrund;</item>
    /// <item>die Projekteinstellung <b>„Kühlung rechnen“</b> aus <see cref="KonfigurationCtrl.KuehlbetriebLesen"/>.</item>
    /// </list>
    /// Die Schalter „Wärmepumpen im Kühlbetrieb“ stehen im Erzeugerdialog der Kältemaschinen
    /// (<see cref="KaeltemaschineAnlageHuelle"/>) und schreiben beim OK über <see cref="WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten"/>
    /// — denselben Kernweg wie der Kühlschalter der Wärmepumpen-Konfiguration (<c>Tab_WP.Kuehlbetrieb</c> der
    /// Projektkopie samt Sperrgründen); Vorlauf und Hilfsstromanteil bleiben, wie sie stehen.
    /// </summary>
    internal static class KuehlungKachelBau
    {
        /// <summary>Der Bestand der Kachel für ein Projekt; <c>null</c> ohne Projekt.</summary>
        internal static KuehlungKachelDaten Daten(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            return new KuehlungKachelDaten
            {
                KuehlungRechnen = KonfigurationCtrl.KuehlbetriebLesen(idProjekt),
                Kaeltemaschinen = KaeltemaschineAnlageCtrl.ListeStill(idProjekt)
                    .Select(a => new KuehlKaeltemaschine(a.Bezeichner ?? "", Math.Max(1, a.Anzahl)))
                    .ToList(),
                Waermepumpen = Waermepumpen(idProjekt)
            };
        }

        /// <summary>
        /// Die Wärmepumpen des Projekts mit Kühlfunktion samt Kühlbetrieb und Sperrgrund — die Kachel liest daraus
        /// ihren Statuspunkt, der Erzeugerdialog der Kältemaschinen seine Schalter „Wärmepumpen im Kühlbetrieb“.
        /// </summary>
        internal static IReadOnlyList<KuehlWaermepumpe> Waermepumpen(int idProjekt)
            => idProjekt <= 0
                ? Array.Empty<KuehlWaermepumpe>()
                : WPCtrl.KuehlfaehigeGeraete(idProjekt)
                    .Select(g => new KuehlWaermepumpe(g.IdWp, g.Bezeichner ?? "", g.Kuehlbetrieb, g.Sperrgrund))
                    .ToList();

        /// <summary>Schreibt den Kühlbetrieb einer Wärmepumpe; <c>null</c> = geschrieben, sonst der Grund.</summary>
        internal static string KuehlbetriebSchreiben(int idProjekt, int idWp, bool an)
            => WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten(idWp, idProjekt, an);
    }
}
