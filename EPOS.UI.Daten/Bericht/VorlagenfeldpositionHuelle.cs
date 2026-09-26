using System;
using System.Collections.Generic;
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
            int varianten = 0;
            for (int i = 0; i < gruppe.Count; i++)
            {
                VariantenCtrl.VarianteInfo vi = gruppe[i];
                if (!vi.IstStamm) varianten++;
                if (vi.IdProjekt == idProjekt) return new Vorlagenfeldposition(i + 1, vi.IstStamm ? 0 : varianten);
            }
            return null;
        }
    }
}
