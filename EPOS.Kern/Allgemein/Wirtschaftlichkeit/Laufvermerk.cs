using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Anwenderentscheid 02.10.2026 (Register EZ‑19, Konzept § 2.15) — der <b>Laufvermerk</b>
    /// eines gespeicherten Ergebnisses: die Stände des Laufs, aus dem es stammt, als Text der
    /// Spalte <see cref="WirtschaftlichkeitCtrl.SPALTE_LAUF_STAENDE"/> (aufsteigend, Komma,
    /// <c>1030,1031,1033</c>), und die Frage der Ergebnisseite, ob die Wahl die Gruppenregel
    /// gegenüber dem Lauf der gezeigten Ergebnisse geändert hat.
    ///
    /// <para><b>Warum ein Vermerk je Zeile.</b> Ohne ihn kannte die Seite den Lauf der
    /// gespeicherten Ergebnisse nur, solange sie ihn selbst gerechnet hatte; nach einem
    /// Seitenwechsel nahm sie die Wahl beim ersten Laden als Lauf. Ein Stand, der aus einem
    /// älteren Lauf stammt, trägt jetzt dessen Stände — tragen die gewählten Stände
    /// verschiedene Vermerke, wird jeder dieser Läufe gegen die Wahl geprüft.</para>
    ///
    /// <para><b>Die Prüfung bleibt die des Kerns</b>
    /// (<see cref="WirtschaftlichkeitCtrl.StromGruppenregelGeaendert"/>): Ein Band gibt es nur,
    /// wenn die Wahl die Gruppenregel eines dieser Läufe ändert, nicht bei jedem Haken. Eine
    /// Zeile ohne Vermerk (Altbestand) zählt zum Lauf, den die Seite selbst kennt — dem
    /// zuletzt gerechneten oder der Wahl beim ersten Laden —, wie vor dem Vermerk.</para>
    /// </summary>
    public static class Laufvermerk
    {
        /// <summary>
        /// Der Vermerk eines Laufs: seine gültigen Stände je einmal, aufsteigend, mit Komma
        /// ohne Leerzeichen. <c>""</c>, wenn der Lauf keinen gültigen Stand führt.
        /// </summary>
        public static string Schreiben(IEnumerable<int> staende)
        {
            List<int> ids = Ordnen(staende);
            var teile = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++) teile[i] = ids[i].ToString(CultureInfo.InvariantCulture);
            return string.Join(",", teile);
        }

        /// <summary>
        /// Die Stände eines Vermerks, aufsteigend und je einmal. Leer bei <c>null</c>, leerem
        /// Text (Altbestand) oder ohne lesbare Kennung; ein unlesbarer Teil fällt weg.
        /// </summary>
        public static List<int> Lesen(string vermerk)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(vermerk)) return ids;
            foreach (string teil in vermerk.Split(','))
            {
                int id;
                if (int.TryParse(teil.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                    ids.Add(id);
            }
            return Ordnen(ids);
        }

        /// <summary>
        /// Die Läufe, aus denen die Ergebnisse der <paramref name="gewaehlt"/>en Stände stammen —
        /// je Lauf einmal, in der Folge der Ergebnisse. Eine Zeile ohne Vermerk zählt zu
        /// <paramref name="ohneVermerk"/> (dem Lauf, den die Seite selbst kennt); ist der leer,
        /// trägt sie nichts bei. Keine Ergebnisse, kein Lauf.
        /// </summary>
        public static List<List<int>> Laeufe(IEnumerable<WirtschaftlichkeitErgebnis> ergebnisse,
                                             IEnumerable<int> gewaehlt, IEnumerable<int> ohneVermerk)
        {
            var laeufe = new List<List<int>>();
            if (ergebnisse == null) return laeufe;
            List<int> wahl = Ordnen(gewaehlt);
            List<int> ersatz = Ordnen(ohneVermerk);

            foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
            {
                if (e == null || !wahl.Contains(e.IdProjekt)) continue;
                List<int> lauf = Lesen(e.LaufStaende);
                if (lauf.Count == 0) lauf = ersatz;
                if (lauf.Count == 0) continue;
                bool bekannt = false;
                foreach (List<int> l in laeufe)
                    if (Gleich(l, lauf)) { bekannt = true; break; }
                if (!bekannt) laeufe.Add(lauf);
            }
            return laeufe;
        }

        /// <summary>
        /// <b>Sind die gezeigten Ergebnisse unter der Wahl veraltet?</b> — ja, wenn die
        /// <paramref name="gewaehlt"/>en Stände die Gruppenregel eines der
        /// <see cref="Laeufe"/> ändern, aus denen ihre Ergebnisse stammen
        /// (<see cref="WirtschaftlichkeitCtrl.StromGruppenregelGeaendert"/>). Ohne Ergebnisse
        /// gibt es nichts, was veralten könnte.
        /// </summary>
        public static bool GruppenregelVeraltet(IEnumerable<WirtschaftlichkeitErgebnis> ergebnisse,
                                                IEnumerable<int> gewaehlt, IEnumerable<int> ohneVermerk)
        {
            List<int> wahl = Ordnen(gewaehlt);
            foreach (List<int> lauf in Laeufe(ergebnisse, wahl, ohneVermerk))
                if (WirtschaftlichkeitCtrl.StromGruppenregelGeaendert(lauf, wahl)) return true;
            return false;
        }

        /// <summary>Gültige Kennungen (&gt; 0) je einmal, aufsteigend.</summary>
        private static List<int> Ordnen(IEnumerable<int> staende)
        {
            var ids = new List<int>();
            if (staende != null)
                foreach (int id in staende)
                    if (id > 0 && !ids.Contains(id)) ids.Add(id);
            ids.Sort();
            return ids;
        }

        private static bool Gleich(List<int> a, List<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
