using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Anschluss der Zonen an die gelesenen Projektgebäude</b> (Stufe G3, Entscheid
    /// A14/E27; Softwarearchitektur 2.9) — die EINE Stelle, an der ein Gebäude seine Zonen
    /// bekommt: <see cref="ProjektGebaeudeCtrl.ReadAll"/> ruft sie nach dem Lesen der Sicht.
    /// Lauf (<c>SimulationWaermebedarf.Waermebedarf_berechnen</c>) und Auskunft
    /// (<see cref="GebaeudeBedarfCtrl"/>) lesen ihre Gebäude beide über diesen Leser — so sieht
    /// die Auskunft dieselbe Zone wie der Lauf, ohne sie ein zweites Mal zu lesen.
    ///
    /// <para><b>Noch ohne Datenbankleser.</b> Die Tabellen der Zonen und Bauteile entstehen in
    /// einer eigenen Welle; ihr Leser (je Projekt, nie je Zone — Softwarearchitektur 2.9) wird
    /// an <see cref="Leser"/> angeschlossen. Bis dahin ist <see cref="Leser"/> leer: Kein
    /// Gebäude trägt eine Zone, jedes rechnet den Klassenweg, und der Referenzlauf bleibt
    /// byte-gleich.</para>
    ///
    /// <para><b>Prozessweiter Zustand:</b> Wer <see cref="Leser"/> in einem Test belegt, gehört
    /// in die Sammlung „Testdatenbank" und stellt ihn in einem <c>finally</c> zurück.</para>
    /// </summary>
    internal static class GebaeudeZonenanschluss
    {
        /// <summary>
        /// Der Leser der Zonen EINES Projekts: liefert zu jedem Gebäude mit Zonen
        /// (<c>Tab_Gebaeude.ID</c> = <see cref="ProjektGebaeudeModel.ID_Gebaeude"/>) seine Zonen,
        /// geordnet. <c>null</c> = kein Leser angeschlossen — keine Zonen.
        /// </summary>
        internal static Func<int, IReadOnlyDictionary<int, IReadOnlyList<GebaeudeZonensatz>>> Leser;

        /// <summary>
        /// Hängt jedem Gebäude des Projekts <paramref name="idProjekt"/> seine Zonen an
        /// (<see cref="ProjektGebaeudeModel.Zonen"/>); ein Gebäude ohne Eintrag bekommt keine.
        /// Ein Aufruf je Projekt, nicht je Gebäude.
        /// </summary>
        internal static void Anschliessen(int idProjekt, IList<ProjektGebaeudeModel> gebaeude)
        {
            Func<int, IReadOnlyDictionary<int, IReadOnlyList<GebaeudeZonensatz>>> leser = Leser;
            if (leser == null || gebaeude == null || gebaeude.Count == 0) return;

            IReadOnlyDictionary<int, IReadOnlyList<GebaeudeZonensatz>> zonen = leser(idProjekt);
            if (zonen == null) return;
            foreach (ProjektGebaeudeModel g in gebaeude)
                if (g != null && zonen.TryGetValue(g.ID_Gebaeude, out IReadOnlyList<GebaeudeZonensatz> z))
                    g.Zonen = z;
        }
    }
}
