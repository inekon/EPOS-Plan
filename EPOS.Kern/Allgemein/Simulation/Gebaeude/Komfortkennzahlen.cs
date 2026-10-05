using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Komfortkennzahlen eines Gebäudes</b> (Anlagenkopplung 5.5, F8, F9; Stufe AK2-2b): drei Zahlen, nicht
    /// eine — die Stunden der Nutzungszeit, in denen die Raumluft um mehr als die Schwelle
    /// (<see cref="GebaeudeFestwerte.KOMFORT_SCHWELLE_K"/>) neben dem Sollwert liegt, die Summe dieser Abweichungen
    /// (Kelvinstunden) und die längste zusammenhängende Folge solcher Stunden.
    ///
    /// <para><b>Heizseite:</b> Unterschreitung = θ_soll(h) − θ_air(h) &gt; Schwelle. <b>Kälteseite</b>
    /// (spiegelbildlich, nur mit wirksamer Kühlung): Überschreitung = θ_air(h) − θ_kühl(h) &gt; Schwelle.</para>
    ///
    /// <para><b>Nutzungszeit</b> ist die der übrigen Kennzahlen (<see cref="GebaeudeModellErgebnis.NutzungBei"/>:
    /// Nachtzeit bzw. Personenkalender, ohne Rampenstunden der Aufheizoptimierung) <b>und</b> ein endlicher
    /// Sollwert — eine Stunde mit Heizung „aus" (NaN) zählt nicht, eine Stunde der Nachtabsenkung auch nicht.</para>
    ///
    /// <para><b>Zonen → Gebäude:</b> Eine Gebäudestunde zählt, wenn <b>mindestens eine</b> Zone (beheizt bzw.
    /// gekühlt) die Schwelle reißt; die Kelvinstunden sind das flächengewichtete Mittel der Abweichungen der
    /// reißenden Zonen über alle Zonen der Seite (eine kleine Zone wiegt wenig); die längste Strecke läuft über die
    /// Gebäudestunden. Mit einer Zone ist das die Rechnung der Zone.</para>
    /// </summary>
    internal sealed record Komfortkennzahlen(int Stunden, double Kelvinstunden, int LaengsteStrecke)
    {
        /// <summary>Keine Stunde, keine Kelvinstunde.</summary>
        internal static readonly Komfortkennzahlen Keine = new Komfortkennzahlen(0, 0.0, 0);

        /// <summary>Die gezählten Stunden je Stunde des Jahres (für die Zusammenfassung über Gebäude); <c>null</c> bei <see cref="Keine"/>.</summary>
        internal bool[] Maske { get; init; }

        /// <summary>
        /// <b>Die Zusammenfassung über Gebäude</b> (Projektwerte, F8): Stunden = Stunden, in denen mindestens ein
        /// Gebäude zählt; Kelvinstunden = Summe; längste Strecke = Maximum über die Gebäude. <c>null</c> ohne Teil.
        /// </summary>
        internal static Komfortkennzahlen Projekt(IEnumerable<Komfortkennzahlen> gebaeude)
        {
            Komfortkennzahlen[] teile = (gebaeude ?? Enumerable.Empty<Komfortkennzahlen>()).Where(k => k != null).ToArray();
            if (teile.Length == 0) return null;
            var maske = new bool[8760];
            foreach (Komfortkennzahlen k in teile)
                if (k.Maske != null)
                    for (int h = 0; h < Math.Min(8760, k.Maske.Length); h++) maske[h] |= k.Maske[h];
            return new Komfortkennzahlen(maske.Count(b => b), teile.Sum(k => k.Kelvinstunden), teile.Max(k => k.LaengsteStrecke))
            {
                Maske = maske,
            };
        }

        /// <summary>
        /// <b>Die Heizseite eines Ergebnisses</b>: am Mehrzonengebäude über die beheizten Zonen, sonst am
        /// Ergebnis selbst. <c>null</c>, wenn keine Zone beheizt ist.
        /// </summary>
        internal static Komfortkennzahlen Heizseite(GebaeudeModellErgebnis e, double schwelle = GebaeudeFestwerte.KOMFORT_SCHWELLE_K)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            List<(GebaeudeModellErgebnis, double)> teile = e.Zonen != null && e.Zonen.Count > 0
                ? e.Zonen.Where(z => z.IstBeheizt).Select(z => (z.Ergebnis, z.Nutzflaeche_M2)).ToList()
                : new List<(GebaeudeModellErgebnis, double)> { (e, 1.0) };
            return teile.Count == 0 ? null : Zaehlen(teile, Unterschreitung, schwelle);
        }

        /// <summary>
        /// <b>Die Kälteseite eines Ergebnisses</b> (F9): über die Zonen bzw. das Ergebnis mit wirksamer Kühlung.
        /// <c>null</c> ohne wirksame Kühlung — dann ist nichts zu erheben.
        /// </summary>
        internal static Komfortkennzahlen Kuehlseite(GebaeudeModellErgebnis e, double schwelle = GebaeudeFestwerte.KOMFORT_SCHWELLE_K)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            List<(GebaeudeModellErgebnis, double)> teile = e.Zonen != null && e.Zonen.Count > 0
                ? e.Zonen.Where(z => z.Ergebnis.KuehlungWirksam).Select(z => (z.Ergebnis, z.Nutzflaeche_M2)).ToList()
                : e.KuehlungWirksam ? new List<(GebaeudeModellErgebnis, double)> { (e, 1.0) } : new List<(GebaeudeModellErgebnis, double)>();
            return teile.Count == 0 ? null : Zaehlen(teile, Ueberschreitung, schwelle);
        }

        /// <summary>
        /// <b>Die Zählung aus drei Reihen</b> (ohne Gebäudemodell, für Proben): Sollwert, Raumluft, Nutzungsmaske je
        /// Stunde. Heizseite: Sollwert − Raumluft; Kälteseite (<paramref name="kuehlseite"/>): Raumluft − Sollwert.
        /// </summary>
        internal static Komfortkennzahlen AusReihen(double[] soll, double[] luft, bool[] nutzung, double schwelle,
                                                    bool kuehlseite = false)
        {
            if (soll == null) throw new ArgumentNullException(nameof(soll));
            if (luft == null) throw new ArgumentNullException(nameof(luft));
            int n = Math.Min(soll.Length, luft.Length);
            var abweichung = new double[n];
            for (int h = 0; h < n; h++)
                abweichung[h] = (nutzung == null || nutzung[h]) && Endlich(soll[h])
                    ? (kuehlseite ? luft[h] - soll[h] : soll[h] - luft[h])
                    : double.NaN;
            return AusAbweichungen(new[] { abweichung }, new[] { 1.0 }, schwelle);
        }

        /// <summary>
        /// <b>Die Zählung über Teile</b> (Zonen): je Teil die Abweichung je Stunde (NaN = zählt nicht) und das
        /// Gewicht (Nutzfläche). Eine Stunde zählt, wenn ein Teil die Schwelle reißt; Kelvinstunden flächengewichtet.
        /// Gewichte ohne positive Summe zählen gleich.
        /// </summary>
        internal static Komfortkennzahlen AusAbweichungen(IReadOnlyList<double[]> abweichungen, IReadOnlyList<double> gewichte,
                                                          double schwelle)
        {
            if (abweichungen == null) throw new ArgumentNullException(nameof(abweichungen));
            int teile = abweichungen.Count;
            if (teile == 0) return Keine;
            var w = new double[teile];
            double summe = 0.0;
            for (int i = 0; i < teile; i++)
            {
                w[i] = gewichte != null && i < gewichte.Count && gewichte[i] > 0.0 ? gewichte[i] : 0.0;
                summe += w[i];
            }
            for (int i = 0; i < teile; i++) w[i] = summe > 0.0 ? w[i] / summe : 1.0 / teile;

            int laenge = abweichungen.Min(a => a.Length);
            var maske = new bool[laenge];
            int stunden = 0, lauf = 0, laengste = 0;
            double kelvin = 0.0;
            for (int h = 0; h < laenge; h++)
            {
                bool reisst = false;
                double kh = 0.0;
                for (int i = 0; i < teile; i++)
                {
                    double d = abweichungen[i][h];
                    if (!(d > schwelle)) continue;
                    reisst = true;
                    kh += w[i] * d;
                }
                if (reisst)
                {
                    stunden++;
                    maske[h] = true;
                    kelvin += kh;
                    lauf++;
                    if (lauf > laengste) laengste = lauf;
                }
                else
                {
                    lauf = 0;
                }
            }
            return new Komfortkennzahlen(stunden, kelvin, laengste) { Maske = maske };
        }

        private static Komfortkennzahlen Zaehlen(List<(GebaeudeModellErgebnis R, double A)> teile,
                                                 Func<GebaeudeModellErgebnis, int, double> abweichung, double schwelle)
        {
            var reihen = new List<double[]>(teile.Count);
            foreach ((GebaeudeModellErgebnis r, double _) in teile)
            {
                var a = new double[8760];
                for (int h = 0; h < 8760; h++) a[h] = abweichung(r, h);
                reihen.Add(a);
            }
            return AusAbweichungen(reihen, teile.Select(t => t.A).ToList(), schwelle);
        }

        private static double Unterschreitung(GebaeudeModellErgebnis r, int h)
        {
            if (r.Heizsollwert == null || !r.NutzungBei(h)) return double.NaN;
            double soll = r.Heizsollwert[h];
            return Endlich(soll) ? soll - r.Raumtemperatur[h] : double.NaN;
        }

        private static double Ueberschreitung(GebaeudeModellErgebnis r, int h)
        {
            if (!r.KuehlungWirksam || !r.NutzungBei(h)) return double.NaN;
            double soll = r.KuehlsollwertBei(h);
            return Endlich(soll) ? r.Raumtemperatur[h] - soll : double.NaN;
        }

        private static bool Endlich(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }
}
