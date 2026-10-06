using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Spanne und die bemessene Zeit neben dem Feld „Aufheizzeit manuell (h)" (Entwurf KP3, Festlegung 40;
    /// P15 (b)): <see cref="BemessenH"/> ist t_auf,max zum Übernehmen (<c>null</c> bei unerreichbarer Bemessung),
    /// [<see cref="VonH"/>; <see cref="BisH"/>] die Spanne aus τ₂.
    /// </summary>
    internal sealed record Aufheizvorschlag(int? BemessenH, int VonH, int BisH)
    {
        /// <summary>Liegt <paramref name="stunden"/> in der Spanne (beide Ränder eingeschlossen)?</summary>
        internal bool Enthaelt(int stunden) => stunden >= VonH && stunden <= BisH;
    }

    /// <summary>
    /// <b>Die Aufheizauskunft eines Gebäudes und die manuelle Aufheizzeit</b> (Entwurf KP3, Welle O2; E59, E60,
    /// Festlegungen 40 und 41) — die Kernseite der Gruppe „Aufheizung" im Bedarfsdialog und des Feldes
    /// „Aufheizzeit manuell (h)" im Reiter „Konditionierung". Sie ruft die Bemessung des Laufs
    /// (<see cref="SimulationWaermebedarf.AufheizbemessungEinesGebaeudes"/>) und schreibt sie nicht ab.
    /// </summary>
    internal static class AufheizauskunftCtrl
    {
        /// <summary>
        /// Die Auskunft EINES Gebäudes ohne Jahreslauf — mit dem Klimakalender und der Aufheizvorgabe des Projekts.
        /// Ist die Optimierung ausgeschaltet, liefert sie <c>null</c>, es sei denn <paramref name="auchOhneSchalter"/>:
        /// Dann bemisst sie mit den gespeicherten Werten der Vorgabe, als wäre der Schalter an (Festlegung 41: „der
        /// Bedarfsdialog nimmt bei Schalter aus die Auskunft"); die Bemessung bleibt dieselbe Rechnung, die der Lauf
        /// mit eingeschaltetem Schalter führte. Gelesen, nicht geschrieben.
        /// </summary>
        internal static Aufheizauskunft Gebaeude(int idProjekt, int idKlimaregion, ProjektGebaeudeModel gebaeude,
                                                 bool auchOhneSchalter)
        {
            if (gebaeude == null || idProjekt <= 0 || idKlimaregion <= 0) return null;
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(idKlimaregion);
            if (!sim.AufheizvorgabeProjekt.An && !auchOhneSchalter) return null;
            // E97: der benannte Parameter statt der Testnaht AufheizvorgabeProjekt (Setter) - gilt nur für diesen Aufruf.
            return sim.AufheizbemessungEinesGebaeudes(gebaeude, auchOhneSchalter: auchOhneSchalter);
        }

        /// <summary>
        /// Dasselbe für die gespeicherte Projektkopie <paramref name="idGebaeude"/> (<c>Tab_Gebaeude.ID</c>), gelesen
        /// wie der Lauf (<see cref="ProjektGebaeudeCtrl.ReadAll"/>); <c>null</c> ohne Klimaregion oder ohne Gebäude.
        /// </summary>
        internal static Aufheizauskunft Projektgebaeude(int idProjekt, int idGebaeude, bool auchOhneSchalter)
        {
            if (idProjekt <= 0 || idGebaeude <= 0) return null;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            if (projekt.m_ID_Klimaregion <= 0) return null;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            for (int i = 0; i < ctrl.rows; i++)
                if (ctrl.items[i].ID_Gebaeude == idGebaeude)
                    return Gebaeude(idProjekt, projekt.m_ID_Klimaregion, ctrl.items[i], auchOhneSchalter);
            return null;
        }

        /// <summary>Ist die Aufheizoptimierung des Projekts eingeschaltet (<see cref="KonfigurationCtrl.AufheizvorgabeLesen"/>)?</summary>
        internal static bool SchalterAn(int idProjekt)
            => idProjekt > 0 && KonfigurationCtrl.AufheizvorgabeLesen(idProjekt).An;

        /// <summary>
        /// <b>Die Vorschläge der manuellen Aufheizzeit</b> (Festlegung 40, P15 (b)): t_u = max(1, t_auf,max),
        /// t_o = min(47, ⌈τ₂ · ln 10⌉); liegt t_u über t_o, gilt [t_o; t_u]; bei unerreichbarer Bemessung (W1) entfällt
        /// die bemessene Zeit, und die Spanne ist [1; t_o]. <c>null</c> ohne τ₂ (kein Sprung, gekoppelt, Tagesbilanz,
        /// Befund) — dann gibt es keinen Vorschlag.
        /// </summary>
        internal static Aufheizvorschlag Vorschlag(Aufheizauskunft a)
        {
            if (a == null || !(a.Tau2H is double tau2) || double.IsNaN(tau2) || double.IsInfinity(tau2) || tau2 <= 0.0)
                return null;
            int min = AufheizManuellSchema.MANUELL_MIN_H, max = AufheizManuellSchema.MANUELL_MAX_H;
            int oben = (int)Math.Min(max, Math.Max(min, Math.Ceiling(tau2 * Math.Log(10.0))));
            if (a.Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR || !(a.AufheizzeitMaxH is int bemessen))
                return a.Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR ? new Aufheizvorschlag(null, min, oben) : null;
            int unten = Math.Max(min, bemessen);
            return unten <= oben ? new Aufheizvorschlag(bemessen, unten, oben) : new Aufheizvorschlag(bemessen, oben, unten);
        }

        /// <summary>Die manuelle Aufheizzeit der Projektkopie [h]; <c>null</c> = Art des Projekts oder ohne Spalte.</summary>
        internal static int? ManuellLesen(int idGebaeude)
        {
            if (idGebaeude <= 0 || !DataRepository.SpalteVorhanden(GebaeudeSchema.TAB_GEBAEUDE, AufheizManuellSchema.SPALTE_MANUELL))
                return null;
            object w = DataRepository.ExecuteScalar(
                "SELECT [" + AufheizManuellSchema.SPALTE_MANUELL + "] FROM [" + GebaeudeSchema.TAB_GEBAEUDE + "] WHERE [ID] = ?",
                new DbParam("@id", idGebaeude));
            return w == null || w is DBNull ? null : Convert.ToInt32(w, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Schreibt die manuelle Aufheizzeit</b> der Projektkopie (Festlegungen 37, 38, 40) — gerufen im OK-Weg des
        /// Gebäudedialogs (Schritt 1, Gebäudedaten). <c>null</c> schreibt NULL (Art des Projekts), sonst gilt die harte
        /// Grenze 1–47 h. Rückgabe: <c>null</c> bei Erfolg, sonst der benannte Grund; ohne Spalte (Datenbank vor dem
        /// Schritt) wird benannt abgelehnt, nie still übergangen.
        /// </summary>
        internal static string ManuellSchreiben(int idGebaeude, int? stunden)
        {
            if (idGebaeude <= 0) return MyResource.Resource.GEBZ_MSG_GEBAEUDE;
            if (stunden is int h && (h < AufheizManuellSchema.MANUELL_MIN_H || h > AufheizManuellSchema.MANUELL_MAX_H))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_AUFH_MANUELL_MSG_BEREICH,
                                     AufheizManuellSchema.MANUELL_MIN_H, AufheizManuellSchema.MANUELL_MAX_H);
            if (!DataRepository.SpalteVorhanden(GebaeudeSchema.TAB_GEBAEUDE, AufheizManuellSchema.SPALTE_MANUELL))
                return MyResource.Resource.KOND_AUFH_MANUELL_MSG_OHNE_SPALTE;
            var wert = new DbParam("@h", DbParamTyp.Integer) { Wert = stunden };
            bool ok = DataRepository.ExecuteSQL(
                "UPDATE [" + GebaeudeSchema.TAB_GEBAEUDE + "] SET [" + AufheizManuellSchema.SPALTE_MANUELL + "] = ? WHERE [ID] = ?",
                wert, new DbParam("@id", idGebaeude));
            return ok ? null : MyResource.Resource.GEBZ_MSG_GEBAEUDE;
        }
    }
}
