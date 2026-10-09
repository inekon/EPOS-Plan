#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Gebäude- und Kesselseite der Bivalenzherleitung eines Projekts (UB‑E1) — was
    /// <see cref="BivalenzQuelle.Projekt"/> aus der Datenbank liest.
    /// </summary>
    internal sealed class BivalenzProjektdaten
    {
        /// <summary>Die Übergabe der gekoppelten Gebäude [kW]; <c>null</c> = ohne Kopplung (Stufe aus oder kein gekoppeltes Gebäude).</summary>
        internal BivalenzGebaeudedaten? Gebaeude { get; init; }

        /// <summary>Summe der Kesselleistungen des Projekts [kW]; NaN ohne Kessel.</summary>
        internal double KesselleistungKw { get; init; } = double.NaN;

        /// <summary>
        /// Gekoppelt, aber ohne Lauf nicht herleitbar: ein Gebäude mit Zonen, ein Gebäude mit Verbrauchsangabe
        /// (der Faktor auf das wirkliche Gebäude entsteht erst im Lauf) oder ein Projekt ohne Klimaregion.
        /// </summary>
        internal bool Unvollstaendig { get; init; }

        /// <summary>Der benannte Grund des Eingangsbauers, der ein gekoppeltes Gebäude ablehnt; leer ohne Fehler.</summary>
        internal string Befund { get; init; } = "";

        /// <summary><c>ID_Gebaeude</c> je Eintrag von <see cref="BivalenzGebaeudedaten.Zonen"/> (gleiche Reihenfolge) — für die Raumtemperatur der Stunde im Lauf.</summary>
        internal IReadOnlyList<int> GebaeudeIds { get; init; } = Array.Empty<int>();
    }

    /// <summary>
    /// <b>Die Datenquelle der Bivalenzherleitung</b> (Umsetzungskonzept Übergabegrenze 5.1, 6.4): liest für den
    /// Konfigurationsdialog der Wärmepumpe die Übergabe der gekoppelten Gebäude, die Kesselleistung und das Kennfeld
    /// bei Höchstvorlauf. Sie ruft den Rechenweg des Laufs (<see cref="SimulationWaermebedarf.UebergabeEingang"/>,
    /// derselbe Klimakalender) und schreibt nichts. Keine Rechenwirkung in UB‑E1.
    ///
    /// <para><b>Leistungseinheit kW.</b> Die Übergabe des Eingangsbauers gilt dem Katalogbau (W); sie wird mit dem
    /// Faktor des Laufs (<see cref="SimulationWaermebedarf.Flaechenfaktor"/>) auf das wirkliche Gebäude gebracht —
    /// eine fest eingetragene Nennleistung gilt ihm ohnehin. Mehrere gekoppelte Gebäude stehen als je eine Zone
    /// nebeneinander (Φ_UE,max = Summe), die Heizlast ist ihre Summe.</para>
    /// </summary>
    internal static class BivalenzQuelle
    {
        /// <summary>Die Gebäude- und Kesselseite des Projekts. Wirft nicht: ein Fehler des Eingangsbauers steht im Befund.</summary>
        internal static BivalenzProjektdaten Projekt(int idProjekt)
        {
            if (idProjekt <= 0) return new BivalenzProjektdaten();
            double kessel = KesselleistungKw(idProjekt);

            string stufe = KonfigurationCtrl.AnlagenkopplungLesen(idProjekt);
            if (!Waermeuebergabe.StufeAn(stufe)) return new BivalenzProjektdaten { KesselleistungKw = kessel };

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            var gekoppelt = new List<ProjektGebaeudeModel>();
            for (int i = 0; i < ctrl.rows; i++)
                if (Waermeuebergabe.KopplungWirksamFuer(ctrl.items[i], stufe)) gekoppelt.Add(ctrl.items[i]);
            if (gekoppelt.Count == 0) return new BivalenzProjektdaten { KesselleistungKw = kessel };

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            if (projekt.m_ID_Klimaregion <= 0) return new BivalenzProjektdaten { KesselleistungKw = kessel, Unvollstaendig = true };

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);

            var zonen = new List<Uebergabezone>();
            var ids = new List<int>();
            double heizlast = 0.0, aussen = double.PositiveInfinity, raum = double.NegativeInfinity;
            foreach (ProjektGebaeudeModel item in gekoppelt)
            {
                if (GebaeudeZonensatz.HatZonen(item))
                    return new BivalenzProjektdaten { KesselleistungKw = kessel, Unvollstaendig = true };
                double? faktor = sim.Flaechenfaktor(item);
                if (faktor is not double f || !(f > 0.0) || double.IsInfinity(f))
                    return new BivalenzProjektdaten { KesselleistungKw = kessel, Unvollstaendig = true };
                GebaeudeModellEingang e;
                try
                {
                    e = sim.UebergabeEingang(item);
                }
                catch (GebaeudeModellException ex)
                {
                    return new BivalenzProjektdaten { KesselleistungKw = kessel, Befund = ex.Message };
                }
                Uebergabekennwerte u = e.Uebergabe;
                double phiNKw = item.Uebergabe_Leistung_Nenn ?? u.PhiNW / 1000.0 * f;
                zonen.Add(new Uebergabezone(phiNKw, u.AuslegungVorlaufC, u.AuslegungRuecklaufC, u.AuslegungRaumC, u.Exponent));
                ids.Add(item.ID_Gebaeude);
                heizlast += e.AuslegungsheizlastW / 1000.0 * f;
                aussen = Math.Min(aussen, e.AuslegungAussentemperaturC);
                raum = Math.Max(raum, u.AuslegungRaumC);
            }

            return new BivalenzProjektdaten
            {
                KesselleistungKw = kessel,
                GebaeudeIds = ids,
                Gebaeude = new BivalenzGebaeudedaten
                {
                    Zonen = zonen,
                    HeizlastN = heizlast,
                    AuslegungAussenC = aussen,
                    AuslegungRaumC = raum,
                },
            };
        }

        /// <summary>Summe der thermischen Leistungen der Projektkessel [kW] (<c>Tab_Heizkessel.Ptherm</c>); NaN ohne Kessel.</summary>
        internal static double KesselleistungKw(int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT SUM(k.Ptherm) AS Summe FROM Tab_Energieanlagen a JOIN Tab_Heizkessel k ON k.ID = a.ID_Kessel "
                + "WHERE a.ID_Projekt = ? AND a.ID_Kessel > 0",
                new DbParam("@projekt", idProjekt));
            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["Summe"] == DBNull.Value) return double.NaN;
            double summe = Convert.ToDouble(dt.Rows[0]["Summe"]);
            return summe > 0.0 ? summe : double.NaN;
        }

        /// <summary>
        /// Das Kennfeld der Wärmepumpe bei <paramref name="hoechstvorlaufC"/> über der Quelltemperatur — aus den
        /// Wärmekennlinien der Anlage (Projektkopie vor Katalog, <see cref="WaermepumpeKennlinienCtrl.FuerAnlage"/>).
        /// Leer ohne Kennlinie.
        /// </summary>
        internal static IReadOnlyList<Kennfeldpunkt> Kennfeld(int idWp, double hoechstvorlaufC)
            => KennfeldBeiVorlauf(WaermepumpeKennlinienCtrl.FuerAnlage(idWp).Satz, hoechstvorlaufC);

        /// <summary>
        /// Die Leistungsreihe bei <paramref name="vorlaufC"/>, linear über den Vorlauf zwischen den zwei
        /// einschließenden Stufen interpoliert (je Quelltemperatur; jede Stufe linear über der Quelltemperatur,
        /// außerhalb gehalten); unter der kleinsten und über der größten Stufe gilt die Randstufe. Ohne Datenbank.
        /// </summary>
        internal static IReadOnlyList<Kennfeldpunkt> KennfeldBeiVorlauf(KennlinienSatz? satz, double vorlaufC)
        {
            if (satz == null || double.IsNaN(vorlaufC)) return Array.Empty<Kennfeldpunkt>();
            var stufen = satz.Leistung.Where(r => r.Punkte != null && r.Punkte.Count > 0)
                                      .OrderBy(r => r.Vorlauf).ToList();
            if (stufen.Count == 0) return Array.Empty<Kennfeldpunkt>();

            ChartRenderer.KennlinienReihe unten = stufen[0], oben = stufen[stufen.Count - 1];
            if (vorlaufC <= unten.Vorlauf) return Reihe(unten);
            if (vorlaufC >= oben.Vorlauf) return Reihe(oben);
            for (int i = 1; i < stufen.Count; i++)
            {
                if (vorlaufC > stufen[i].Vorlauf) continue;
                unten = stufen[i - 1];
                oben = stufen[i];
                break;
            }
            double w = (vorlaufC - unten.Vorlauf) / (oben.Vorlauf - unten.Vorlauf);
            var temperaturen = unten.Punkte.Select(p => p.Temperatur).Concat(oben.Punkte.Select(p => p.Temperatur))
                                    .Distinct().OrderBy(t => t).ToList();
            var ergebnis = new List<Kennfeldpunkt>(temperaturen.Count);
            foreach (double t in temperaturen)
                ergebnis.Add(new Kennfeldpunkt(t, Wert(unten, t) + w * (Wert(oben, t) - Wert(unten, t))));
            return ergebnis;
        }

        private static IReadOnlyList<Kennfeldpunkt> Reihe(ChartRenderer.KennlinienReihe r)
            => r.Punkte.Select(p => new Kennfeldpunkt(p.Temperatur, p.Wert)).OrderBy(p => p.AussenC).ToList();

        private static double Wert(ChartRenderer.KennlinienReihe r, double t)
            => new Kennfeldgerade(r.Punkte.Select(p => new Kennfeldpunkt(p.Temperatur, p.Wert))).Leistung(t);
    }
}
