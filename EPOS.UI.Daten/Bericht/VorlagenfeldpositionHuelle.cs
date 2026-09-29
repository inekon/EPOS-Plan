using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dienste;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Position eines Stands für die Platzhaltermarken</b> (Konzept Berichtsvorlagen 4.5, 9.4): wo ein Projekt in
    /// der Folge des Berichts steht — die Folge, die <see cref="BerichtsDatenSammler"/> bildet: das Stammprojekt vorn, dann
    /// die Varianten in der Reihenfolge der Gruppe (<see cref="VariantenCtrl.LadeGruppe"/>). Gezählt wird mit ALLEN
    /// Varianten der Gruppe; ein Bericht mit einer Teilauswahl zählt nur die gewählten — das sagt der Hinweis der Marke.
    /// </summary>
    internal static class VorlagenfeldpositionHuelle
    {
        /// <summary>Die Position des Projekts; <c>null</c>, wenn sie sich nicht bestimmen lässt.</summary>
        internal static Vorlagenfeldposition Von(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                var ctrl = new VariantenCtrl();
                int stammRef = ctrl.StammRefDerVariante(idProjekt);
                int stamm = stammRef > 0 ? stammRef : idProjekt;
                return Aus(ctrl.LadeGruppe(stamm, ""), idProjekt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Die Position des Projekts in einer Gruppe (Stamm zuerst); <c>null</c>, wenn es nicht darin steht.</summary>
        internal static Vorlagenfeldposition Aus(IReadOnlyList<VariantenCtrl.VarianteInfo> gruppe, int idProjekt)
        {
            if (gruppe == null) return null;
            return Je(gruppe.Select(vi => (vi.IdProjekt, vi.IstStamm)))
                .TryGetValue(idProjekt, out Vorlagenfeldposition p) ? p : null;
        }

        /// <summary>
        /// Die Positionen aller Projekte einer Gruppe in ihrer Folge (Stamm zuerst, dann die Varianten), je Projekt einmal —
        /// für eine Seite, die mehrere Stände zeigt (Wirtschaftlichkeit: Sensitivität, Mehrjahrestafel, Zahlungsstrom).
        /// </summary>
        internal static Dictionary<int, Vorlagenfeldposition> Je(IEnumerable<(int IdProjekt, bool IstStamm)> gruppe)
        {
            var je = new Dictionary<int, Vorlagenfeldposition>();
            if (gruppe == null) return je;
            int stand = 0, varianten = 0;
            foreach ((int id, bool istStamm) in gruppe)
            {
                stand++;
                if (!istStamm) varianten++;
                je.TryAdd(id, new Vorlagenfeldposition(stand, istStamm ? 0 : varianten));
            }
            return je;
        }
    }
}
