using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DIE VERGLEICHSWAHL des Bereichs „Berichte &amp; Kosten" (Anwenderwunsch 08.09.2026,
    /// W5‑B‑5): welche Versionen einer Vergleichsgruppe die Seiten Übersicht, Kosten und
    /// Wirtschaftlichkeit nebeneinander stellen. EINE Auswahl für alle drei Seiten — wer in
    /// der Übersicht eine Variante abwählt, sieht sie auch in Kosten und Wirtschaftlichkeit
    /// nicht mehr, und umgekehrt.
    ///
    /// <para><b>Gemerkt wird das ABGEWÄHLTE, nicht das Gewählte</b> — wie die Liste des
    /// Vorläufers <c>UcWirtschaftlichkeit.AktualisiereListe</c> („bewahreAuswahl"): Eine neu
    /// angelegte Variante ist damit von selbst im Vergleich, und der Stamm ist als Referenz
    /// immer dabei. Die Ids sind <c>Tab_Projekt.ID</c> und über alle Gruppen eindeutig; ein
    /// Stammwechsel braucht deshalb kein Zurücksetzen.</para>
    /// </summary>
    public sealed class Vergleichsauswahl
    {
        private readonly HashSet<int> _abgewaehlt = new HashSet<int>();

        /// <summary>Die Auswahl hat sich geändert.</summary>
        public event Action Geaendert;

        /// <summary>Ist die Version im Vergleich? Der Stamm immer.</summary>
        public bool IstGewaehlt(int idProjekt, int idStamm)
        {
            return idProjekt == idStamm || !_abgewaehlt.Contains(idProjekt);
        }

        /// <summary>Die gewählten Ids der Gruppe in deren Reihenfolge (Stamm eingeschlossen).</summary>
        public List<int> Gewaehlte(IEnumerable<int> gruppe, int idStamm)
        {
            var l = new List<int>();
            if (gruppe == null) return l;
            foreach (int id in gruppe)
                if (IstGewaehlt(id, idStamm)) l.Add(id);
            return l;
        }

        /// <summary>
        /// Übernimmt eine neue Auswahl für die Gruppe: Was in <paramref name="gruppe"/> steht
        /// und nicht in <paramref name="gewaehlt"/>, gilt als abgewählt; der Stamm bleibt
        /// gewählt. Versionen anderer Gruppen bleiben, wie sie waren.
        /// </summary>
        public void Setzen(IEnumerable<int> gewaehlt, IEnumerable<int> gruppe, int idStamm)
        {
            var an = new HashSet<int>(gewaehlt ?? new int[0]);
            bool geaendert = false;
            foreach (int id in gruppe ?? new int[0])
            {
                if (id == idStamm) continue;
                if (an.Contains(id)) geaendert |= _abgewaehlt.Remove(id);
                else geaendert |= _abgewaehlt.Add(id);
            }
            if (!geaendert) return;
            Action h = Geaendert;
            if (h != null) h();
        }

        /// <summary>Zahl der abgewählten Versionen (Prüfhilfe).</summary>
        public int AnzahlAbgewaehlt { get { return _abgewaehlt.Count; } }
    }
}
