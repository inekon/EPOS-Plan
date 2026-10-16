using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>KBP-4b</b> — die Regeln des Typtage-Editors im Dialog der Bedarfsprofile (Konzept Kältebedarfsprofile 3.4), ohne
    /// Datenbank und ohne Oberfläche: Kopie und Vergleich einer <see cref="KaeltetyptagEingabe"/>, die Vorbelegung beim
    /// ersten Wechsel auf Typtage aus dem Wochenprofil, Anlegen und Entfernen eines Typtags und die Hinweise des Dialogs —
    /// dieselben Aussagen wie die Hinweise <c>SIMENG_TYPTAG_*</c> der <see cref="Typtagschicht"/>.
    /// </summary>
    public static class Kaeltetyptageingabe
    {
        /// <summary>Eine tiefe Kopie (Typtage, Stunden, Matrix); <c>null</c> ergibt eine leere Eingabe.</summary>
        public static KaeltetyptagEingabe Kopie(KaeltetyptagEingabe quelle)
        {
            var e = new KaeltetyptagEingabe();
            if (quelle == null) return e;
            foreach (Kaeltetyptag t in quelle.Typtage)
                e.Typtage.Add(new Kaeltetyptag
                {
                    Nr = t.Nr, Bezeichner = t.Bezeichner ?? "", Tagesmenge = t.Tagesmenge,
                    Stunden = t.Stunden != null ? (double[])t.Stunden.Clone() : new double[KaeltetyptagSchema.STUNDEN]
                });
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int a = 0; a < 3; a++) e.Matrix[m, a] = quelle.Matrix[m, a];
            return e;
        }

        /// <summary>Tragen beide Eingaben dieselben Typtage (Reihenfolge der Nummern) und dieselbe Matrix?</summary>
        public static bool Gleich(KaeltetyptagEingabe a, KaeltetyptagEingabe b)
        {
            a ??= new KaeltetyptagEingabe();
            b ??= new KaeltetyptagEingabe();
            if (a.Typtage.Count != b.Typtage.Count) return false;
            for (int i = 0; i < a.Typtage.Count; i++)
            {
                Kaeltetyptag x = a.Typtage[i], y = b.Typtage[i];
                if (x.Nr != y.Nr || !string.Equals(x.Bezeichner, y.Bezeichner, StringComparison.Ordinal) ||
                    !x.Tagesmenge.Equals(y.Tagesmenge)) return false;
                for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++)
                    if (!(x.Stunden?[h] ?? 0.0).Equals(y.Stunden?[h] ?? 0.0)) return false;
            }
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int t = 0; t < 3; t++)
                    if (a.Matrix[m, t] != b.Matrix[m, t]) return false;
            return true;
        }

        /// <summary>
        /// Die Vorbelegung beim ersten Wechsel auf Typtage: drei Typtage aus dem Wochenprofil — 1 Werktag (Mittel Montag
        /// bis Freitag), 2 Samstag, 3 Sonn-/Feiertag (Sonntag) —, die Stunden je Tag in Prozent der Tagesspitze, die
        /// Tagesmenge als Verhältnis der Tagessumme zur Werktagssumme; die Matrix setzt in allen zwölf Monaten Werktag → 1,
        /// Samstag → 2, Sonn-/Feiertag → 3. So zeigt die Reihe die Wochenform des Profils gleichmäßig über das Jahr
        /// (Monatsanteile entfallen bei Typtagen). Ohne brauchbares Wochenprofil tragen alle drei eine flache Form mit
        /// Tagesmenge 1. <paramref name="namen"/> = die drei Bezeichner.
        /// </summary>
        public static KaeltetyptagEingabe AusWoche(double[] woche, IReadOnlyList<string> namen)
        {
            var tage = new double[3][];
            for (int i = 0; i < 3; i++) tage[i] = new double[KaeltetyptagSchema.STUNDEN];
            bool brauchbar = woche != null && woche.Length == Kaelteprofileingabe.WOCHE && Kaelteprofileingabe.PruefeWoche(woche) == null;
            if (brauchbar)
            {
                for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++)
                {
                    double s = 0;
                    for (int d = 0; d < Kaelteprofileingabe.SAMSTAG; d++) s += woche[d * Kaelteprofileingabe.STUNDEN + h];
                    tage[0][h] = s / Kaelteprofileingabe.SAMSTAG;
                    tage[1][h] = woche[Kaelteprofileingabe.SAMSTAG * Kaelteprofileingabe.STUNDEN + h];
                    tage[2][h] = woche[Kaelteprofileingabe.SONNTAG * Kaelteprofileingabe.STUNDEN + h];
                }
            }
            double bezug = Summe(tage[0]);
            if (!(bezug > 0)) bezug = Math.Max(Summe(tage[1]), Summe(tage[2]));
            var e = new KaeltetyptagEingabe();
            for (int i = 0; i < 3; i++)
            {
                double summe = Summe(tage[i]), spitze = 0;
                foreach (double x in tage[i]) spitze = Math.Max(spitze, x);
                var t = new Kaeltetyptag
                {
                    Nr = i + 1,
                    Bezeichner = namen != null && i < namen.Count && !string.IsNullOrWhiteSpace(namen[i])
                        ? namen[i].Trim() : (i + 1).ToString(CultureInfo.InvariantCulture),
                };
                if (!brauchbar)
                {
                    for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++) t.Stunden[h] = 100.0;
                    t.Tagesmenge = 1.0;
                }
                else if (spitze > 0)
                {
                    for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++) t.Stunden[h] = Math.Round(100.0 * tage[i][h] / spitze, 3);
                    t.Tagesmenge = bezug > 0 ? Math.Round(summe / bezug, 4) : 1.0;
                }
                else t.Tagesmenge = 0.0;      // ein Tag ohne Bedarf bleibt ohne Bedarf (kein Hinweis „ohne Stunden“)
                e.Typtage.Add(t);
            }
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int a = 0; a < 3; a++) e.Matrix[m, a] = a + 1;
            return e;
        }

        /// <summary>Die kleinste freie Nummer 1 … 6; 0, wenn schon sechs Typtage stehen.</summary>
        public static int FreieNummer(KaeltetyptagEingabe e)
        {
            for (int nr = 1; nr <= KaeltetyptagSchema.NR_MAX; nr++)
                if (e?.Typtag(nr) == null) return nr;
            return 0;
        }

        /// <summary>
        /// Legt einen Typtag mit der kleinsten freien Nummer an (flache Form, Tagesmenge 1); <c>null</c>, wenn schon sechs
        /// stehen.
        /// </summary>
        public static Kaeltetyptag Anlegen(KaeltetyptagEingabe e, string bezeichner)
        {
            int nr = FreieNummer(e);
            if (e == null || nr == 0) return null;
            var t = new Kaeltetyptag
            {
                Nr = nr,
                Bezeichner = string.IsNullOrWhiteSpace(bezeichner) ? nr.ToString(CultureInfo.InvariantCulture) : bezeichner.Trim(),
                Tagesmenge = 1.0
            };
            for (int h = 0; h < KaeltetyptagSchema.STUNDEN; h++) t.Stunden[h] = 100.0;
            int platz = 0;
            while (platz < e.Typtage.Count && e.Typtage[platz].Nr < nr) platz++;
            e.Typtage.Insert(platz, t);
            return t;
        }

        /// <summary>Entfernt den Typtag <paramref name="nr"/>; die Zellen, die auf ihn zeigten, werden leer.</summary>
        public static bool Entfernen(KaeltetyptagEingabe e, int nr)
        {
            Kaeltetyptag t = e?.Typtag(nr);
            if (t == null) return false;
            e.Typtage.Remove(t);
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int a = 0; a < 3; a++)
                    if (e.Matrix[m, a] == nr) e.Matrix[m, a] = 0;
            return true;
        }

        /// <summary>
        /// Die Hinweise des Dialogs zur Eingabe der Zeile <paramref name="name"/> — dieselben Aussagen wie die Hinweise der
        /// Typtagschicht im Lauf: leere Zellen der Matrix (oder Zellen ohne Typtag), Typtage ohne Stundenwerte, die in der
        /// Matrix stehen und eine Tagesmenge tragen, und eine Eingabe, die im ganzen Jahr keinen Bedarf ergibt.
        /// </summary>
        public static List<string> Hinweise(KaeltetyptagEingabe e, string name)
        {
            var hinweise = new List<string>();
            e ??= new KaeltetyptagEingabe();
            var leer = new List<string>();
            var ohneStunden = new SortedSet<int>();
            bool bedarf = false;
            for (int m = 0; m < KaeltetyptagSchema.MONATE; m++)
                for (int a = 0; a < 3; a++)
                {
                    Kaeltetyptag t = e.Matrix[m, a] != 0 ? e.Typtag(e.Matrix[m, a]) : null;
                    if (t == null)
                    {
                        leer.Add((m + 1).ToString(CultureInfo.InvariantCulture) + "/" + KaeltetyptagSchema.TAGESARTEN[a]);
                        continue;
                    }
                    if (!(t.Formsumme() > 0)) { if (t.Tagesmenge > 0) ohneStunden.Add(t.Nr); }
                    else if (t.Tagesmenge > 0) bedarf = true;
                }
            if (leer.Count > 0)
                hinweise.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_TYPTAG_ZELLEN_LEER, "", name ?? "",
                                           leer.Count, string.Join(", ", leer)));
            foreach (int nr in ohneStunden)
                hinweise.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_TYPTAG_OHNE_STUNDEN, "", name ?? "", nr));
            if (!bedarf)
                hinweise.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_TYPTAG_OHNE_BEDARF, "", name ?? ""));
            return hinweise;
        }

        private static double Summe(double[] werte)
        {
            double s = 0;
            foreach (double x in werte) s += x;
            return s;
        }
    }
}
