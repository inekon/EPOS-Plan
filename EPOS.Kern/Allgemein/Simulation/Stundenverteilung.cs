using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Verteilung je Stunde</b> (AK3-W3a; Entwurf AK3 2.3, 2.6, Festlegung 9) — das Angebot einer Stunde
    /// auf Gebäude und Zonen, mit denselben zwei Stufen und Regeln wie der Zweipass von AK2
    /// (<c>SimulationWaermebedarf.Verteilen</c>), aber für EINE Stunde und aus dem Angebot statt aus dem Profil:
    /// <list type="number">
    /// <item><b>Projekt → Gebäude</b> proportional zum Schlüssel der Stunde (dem unbegrenzten Probeschritt, H-F6);
    /// ohne Schlüssel (ein Gebäude ohne Probeschritt) bekommt jedes gekoppelte Gebäude das ganze Angebot.
    /// Reicht das Angebot für die Summe der Schlüssel nicht und nennt es keinen Grund, heißt er
    /// <see cref="Verfuegbarkeitsgrund.Leistungsgrenze"/>.</item>
    /// <item><b>Gebäude → Zonen</b> nach dem Zonenschlüssel derselben Stunde; ein Gebäude mit höchstens einer Zone
    /// behält seinen Anteil unverändert.</item>
    /// </list>
    /// Rundungsrest und Randfälle (Summe ≤ 0, ein Teilnehmer) regelt <see cref="Verfuegbarkeitsverteilung"/> wie in
    /// AK2. Anders als der Profilweg trägt eine Stunde ohne Ausfall hier eine Schranke: Das Angebot IST die
    /// Kapazität (E102 Q-AK3-3), die AK2-Regel „ohne Ausfall keine Schranke“ (AK2-2a) gilt nicht. Reine Funktion.
    /// </summary>
    internal static class Stundenverteilung
    {
        /// <summary>
        /// Verteilt <paramref name="angebot"/> auf die Gebäude und ihre Zonen.
        /// </summary>
        /// <param name="angebot">Das Angebot der Stunde (Leistung in kW).</param>
        /// <param name="ids">Gebäude-Ids — Ordnung für den Rundungsrest.</param>
        /// <param name="gekoppelt">Je Gebäude: nimmt es an der Verteilung teil (sonst Ergebnis <c>null</c>).</param>
        /// <param name="schluesselW">Unbegrenzter Bedarf je Gebäude in der Stunde [W]; <c>null</c> = ohne Probeschritt.</param>
        /// <param name="zonenSchluessel">Je Gebäude die Zonenschlüssel der Stunde (beliebige Einheit); <c>null</c> oder höchstens ein Wert = eine Zone.</param>
        /// <returns>Je Gebäude je Zone die Verfügbarkeit der Stunde; <c>null</c> für ein ungekoppeltes Gebäude.</returns>
        internal static Anlagenverfuegbarkeit[][] Verteilen(Anlagenverfuegbarkeit angebot, IReadOnlyList<long> ids,
                                                            IReadOnlyList<bool> gekoppelt, IReadOnlyList<double> schluesselW,
                                                            IReadOnlyList<IReadOnlyList<double>> zonenSchluessel)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (gekoppelt == null) throw new ArgumentNullException(nameof(gekoppelt));
            int n = ids.Count;
            if (gekoppelt.Count != n) throw new ArgumentException("Ids und Kopplung passen nicht zusammen.", nameof(gekoppelt));
            if (schluesselW != null && schluesselW.Count != n)
                throw new ArgumentException("Ids und Schlüssel passen nicht zusammen.", nameof(schluesselW));

            Anlagenverfuegbarkeit v = angebot;
            double schranke = v.LeistungKw;
            double[] anteil;
            if (schluesselW == null)
            {
                anteil = new double[n];
                for (int i = 0; i < n; i++) anteil[i] = schranke;
            }
            else
            {
                var bedarfKw = new double[n];
                double summe = 0.0;
                for (int i = 0; i < n; i++)
                {
                    bedarfKw[i] = schluesselW[i] / 1000.0;
                    if (bedarfKw[i] > 0.0) summe += bedarfKw[i];
                }
                if (v.Grund == Verfuegbarkeitsgrund.KeineBegrenzung && summe > schranke)
                    v = v.MitGrund(Verfuegbarkeitsgrund.Leistungsgrenze);
                anteil = Verfuegbarkeitsverteilung.Verteilen(schranke, ids, bedarfKw);
            }

            var ergebnis = new Anlagenverfuegbarkeit[n][];
            for (int i = 0; i < n; i++)
            {
                if (!gekoppelt[i]) continue;
                Anlagenverfuegbarkeit gebaeude = v.MitLeistung(anteil[i]);
                IReadOnlyList<double> zonen = zonenSchluessel != null && i < zonenSchluessel.Count ? zonenSchluessel[i] : null;
                if (zonen == null || zonen.Count <= 1)
                {
                    ergebnis[i] = new[] { gebaeude };
                    continue;
                }
                var zIds = new long[zonen.Count];
                for (int z = 0; z < zIds.Length; z++) zIds[z] = z;
                double[] teil = Verfuegbarkeitsverteilung.Verteilen(gebaeude.LeistungKw, zIds, zonen);
                var jeZone = new Anlagenverfuegbarkeit[zonen.Count];
                for (int z = 0; z < jeZone.Length; z++) jeZone[z] = gebaeude.MitLeistung(teil[z]);
                ergebnis[i] = jeZone;
            }
            return ergebnis;
        }
    }
}
