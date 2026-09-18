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

        // =====================================================================
        // KONZEPT § 2.15 — die Vergleichssicht der Ergebnisansicht
        // =====================================================================

        /// <summary>
        /// Die Sicht der Wirtschaftlichkeitsseite (§ 2.15, VG‑Q3): alle Varianten gegen
        /// die Referenz oder zwei Stände. Sie steht HIER, neben den Häkchen, weil sie
        /// dieselbe Lebensdauer hat — die Sitzung — und weil sie nur von derselben Seite
        /// und ihrem Bericht gelesen wird; Übersicht und Kosten kennen sie nicht.
        ///
        /// <para>Beim Wechsel der Vergleichsgruppe und beim Neustart gilt Sicht 1
        /// (<see cref="GruppeGewechselt"/>).</para>
        /// </summary>
        public Vergleichssicht Sicht { get; } = new Vergleichssicht();

        /// <summary>
        /// Trägt die Gruppe überhaupt zwei Stände? Sonst ist Sicht 2 gesperrt — eine
        /// Paarwahl mit einem Stand wäre eine Differenz gegen sich selbst.
        /// </summary>
        public static bool PaarMoeglich(IList<int> gewaehlte)
        {
            return gewaehlte != null && gewaehlte.Count >= 2;
        }

        /// <summary>
        /// Wechselt die Sicht. Beim Wechsel nach <see cref="Vergleichssicht.PAAR"/>
        /// belegt sie A und B vor: <b>A = Referenz der Gruppe, B = die erste andere
        /// Variante</b> in der Reihenfolge der Vergleichsgruppen-Liste. Eine bereits
        /// getroffene, noch gültige Paarwahl bleibt stehen.
        /// </summary>
        /// <param name="sicht"><see cref="Vergleichssicht.ALLE"/> oder <see cref="Vergleichssicht.PAAR"/>.</param>
        /// <param name="gewaehlte">Die angehakten Stände in Listenreihenfolge.</param>
        /// <param name="idGruppenreferenz">Die Referenz der Gruppe (§ 2.9); 0 = Stamm.</param>
        /// <param name="idStamm">Die Id des Stammprojekts.</param>
        /// <returns>true, wenn sich etwas geändert hat.</returns>
        public bool SichtWaehlen(int sicht, IList<int> gewaehlte, int idGruppenreferenz, int idStamm)
        {
            int neu = sicht == Vergleichssicht.PAAR && PaarMoeglich(gewaehlte)
                    ? Vergleichssicht.PAAR : Vergleichssicht.ALLE;
            bool geaendert = Sicht.Sicht != neu;
            Sicht.Sicht = neu;

            if (neu == Vergleichssicht.PAAR &&
                (!Gueltig(Sicht.IdA, gewaehlte) || !Gueltig(Sicht.IdB, gewaehlte) ||
                 Sicht.IdA == Sicht.IdB))
            {
                int a = idGruppenreferenz > 0 && Gueltig(idGruppenreferenz, gewaehlte)
                      ? idGruppenreferenz
                      : Gueltig(idStamm, gewaehlte) ? idStamm : gewaehlte[0];
                int b = 0;
                foreach (int id in gewaehlte) if (id != a) { b = id; break; }
                geaendert |= Sicht.IdA != a || Sicht.IdB != b;
                Sicht.IdA = a; Sicht.IdB = b;
            }

            if (geaendert) Melde();
            return geaendert;
        }

        /// <summary>
        /// Setzt A und B. <b>A ≠ B ist ohne Meldung gesichert</b>: Wer den Stand wählt,
        /// der auf der anderen Seite steht, tauscht damit — die Oberfläche führt ihn in
        /// der Gegenliste ohnehin nicht.
        /// </summary>
        public bool PaarWaehlen(int idA, int idB)
        {
            if (idA <= 0 || idB <= 0) return false;
            if (idA == idB) return false;
            if (Sicht.IdA == idA && Sicht.IdB == idB) return false;
            Sicht.IdA = idA; Sicht.IdB = idB;
            Melde();
            return true;
        }

        /// <summary>
        /// Tauscht A und B (VG‑Q7). Er spart zwei Listenwahlen und macht die
        /// Vorzeichenregel sichtbar: Der Tausch dreht das Vorzeichen von
        /// Kapitalwertdifferenz und Annuität.
        /// </summary>
        public bool Tauschen()
        {
            if (Sicht.IdA <= 0 || Sicht.IdB <= 0 || Sicht.IdA == Sicht.IdB) return false;
            int h = Sicht.IdA; Sicht.IdA = Sicht.IdB; Sicht.IdB = h;
            Melde();
            return true;
        }

        /// <summary>
        /// RANDFALL (§ 2.15): Steht A oder B nicht mehr im Vergleich — gelöscht oder
        /// abgehakt —, fällt die Ansicht auf Sicht 1 zurück, und zwar <b>benannt</b>.
        /// Da die Wahl in der Sitzung liegt, bleibt nichts zu bereinigen.
        /// </summary>
        /// <returns>Die Warnzeile des Rückfalls; <c>null</c> = kein Rückfall.</returns>
        public string Nachziehen(IList<int> gewaehlte)
        {
            if (Sicht.Sicht != Vergleichssicht.PAAR) return null;
            if (PaarMoeglich(gewaehlte) &&
                Gueltig(Sicht.IdA, gewaehlte) && Gueltig(Sicht.IdB, gewaehlte) &&
                Sicht.IdA != Sicht.IdB) return null;

            Sicht.Sicht = Vergleichssicht.ALLE;
            Melde();
            return MyResource.Resource.WIRT_SICHT_PAAR_RUECKFALL;
        }

        /// <summary>
        /// Beim Wechsel der Vergleichsgruppe gilt wieder Sicht 1 (§ 2.15): Die Ids der
        /// Paarwahl gehören einer anderen Gruppe und sagen über die neue nichts.
        /// </summary>
        public void GruppeGewechselt()
        {
            if (Sicht.Sicht == Vergleichssicht.ALLE && Sicht.IdA == 0 && Sicht.IdB == 0) return;
            Sicht.Sicht = Vergleichssicht.ALLE;
            Sicht.IdA = 0; Sicht.IdB = 0;
            Melde();
        }

        private static bool Gueltig(int id, IList<int> gewaehlte)
        {
            if (id <= 0 || gewaehlte == null) return false;
            foreach (int g in gewaehlte) if (g == id) return true;
            return false;
        }

        private void Melde()
        {
            Action h = Geaendert;
            if (h != null) h();
        }
    }
}
