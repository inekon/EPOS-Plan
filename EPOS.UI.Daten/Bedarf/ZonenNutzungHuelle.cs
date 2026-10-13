using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Blatts „Nutzung und Kalender“</b> (MA-2, Entwurf Geführter Modellaufbau 2 und 4, E137 F5/F6): baut aus
    /// <see cref="ZonenNutzungCtrl"/> den <see cref="ZonenNutzungWeg"/> des Blatts — Stand aller Zonen, Profile des Katalogs,
    /// Profil zuweisen bzw. Vorschlag übernehmen, Projektvorgabe für Ferien und Heizperiode, Kalender kopieren, Sollwerte setzen
    /// und Zurücknehmen. Ausnahmen des Kerns werden zu benannten Meldungen. Plattformfrei, kein SQL.
    /// </summary>
    internal static class ZonenNutzungHuelle
    {
        /// <summary>Der Weg für das Blatt; <c>null</c> ohne Projektgebäude.</summary>
        internal static ZonenNutzungWeg Weg(int idGebaeude, string name)
        {
            if (idGebaeude <= 0) return null;
            var ctrl = new ZonenNutzungCtrl(idGebaeude);
            return new ZonenNutzungWeg
            {
                Gebaeudename = name ?? "",
                Lesen = () =>
                {
                    try { return ctrl.Lesen(); }
                    catch (Exception ex) when (ex is not OutOfMemoryException) { return null; }
                },
                Profile = () =>
                {
                    try { return ZonenNutzungCtrl.Profile(); }
                    catch (Exception ex) when (ex is not OutOfMemoryException) { return Array.Empty<ZonenNutzungsprofil>(); }
                },
                ProfilZuweisen = (zonen, profil) => Sicher(ctrl, () => ctrl.ProfilZuweisen(zonen, profil)),
                ProjektvorgabeUebernehmen = zonen => Sicher(ctrl, () => ctrl.ProjektvorgabeUebernehmen(zonen)),
                KalenderKopieren = (quelle, zonen) => Sicher(ctrl, () => ctrl.KalenderKopieren(quelle, zonen)),
                SollwerteSetzen = (zonen, tag, nacht) => Sicher(ctrl, () => ctrl.SollwerteSetzen(zonen, tag, nacht)),
                Zuruecknehmen = () => Sicher(ctrl, ctrl.Zuruecknehmen),
            };
        }

        private static ZonenNutzungsergebnis Sicher(ZonenNutzungCtrl ctrl, Func<ZonenNutzungsergebnis> f)
        {
            try { return f(); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                ZonenNutzungsstand stand = null;
                try { stand = ctrl.Lesen(); } catch (Exception e2) when (e2 is not OutOfMemoryException) { stand = null; }
                return new ZonenNutzungsergebnis(false, ex.Message, 0, stand);
            }
        }
    }
}
