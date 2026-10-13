using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Gerätekosten EINES Katalogsatzes als Lesewert</b> — die Anzeige der Gruppe „Kosten"
    /// von Stammblatt und Aufklapper, seit die Kostenspalten der Gerätekataloge entfallen sind
    /// (Welle KB-4, Schemaschritt <see cref="GeraetekostenEntfallSchema.SCHRITT"/>,
    /// Anwenderentscheid 13.10.2026 „eine Kostenbasis, keine Doppelhaltung").
    ///
    /// <para>
    /// <b>Die eine Basis ist die Satzvorlage.</b> Gepflegt werden die Kosten in der
    /// Kostenverwaltung; der Katalogsatz verweist über
    /// <c>ID_KostenVorlageInvestition</c> auf seine Investitionsvorlage. Was hier entsteht, ist
    /// deshalb keine zweite Zahl, sondern dieselbe Zahl, nur gelesen: Summe ihrer Positionen für
    /// EIN Stück.
    /// </para>
    /// <para>
    /// <b>Warum nicht <see cref="TechnikPlanwertCtrl"/>.</b> Der Planwert dort gehört einer
    /// ANLAGE: Er nimmt die Stückzahl der Anlagenzeile und trennt die vier BHKW-Nebenposten ab,
    /// weil die Kostenverwaltung sie als eigene Zeilen führt. Der Katalogsatz hat keine Anlage —
    /// seine Anzeige rechnet mit einem Stück und zeigt die Vorlage als EINE Summe. Die
    /// Bezugsgröße nennt beide Male dieselbe Liste (<see cref="Katalogkostenfeld.Bezugsspalte"/>).
    /// </para>
    /// </summary>
    internal static class Katalogkostenanzeige
    {
        /// <summary>
        /// Die Investitionssumme der Satzvorlage dieser Zeile als Anzeigetext in der Kultur des
        /// Anwenders — ohne Vorlage, ohne Betrag oder ohne Bezugsgröße ein LEERER Text: Lieber
        /// keine Zahl als eine geratene (Vermerk der Oberfläche nennt die Kostenverwaltung).
        /// </summary>
        internal static string Investitionstext(string tabelle, DataRow r)
        {
            double betrag = Investition(tabelle, r);
            return betrag > 0 ? betrag.ToString("0.##", CultureInfo.CurrentCulture) : "";
        }

        /// <summary>
        /// Die Investitionssumme der Satzvorlage dieser Zeile [€] für EIN Stück; 0 heißt „keine
        /// Vorlage oder nichts darin, was sich an diesem Gewerk bemessen lässt".
        /// </summary>
        internal static double Investition(string tabelle, DataRow r)
        {
            if (r == null || string.IsNullOrEmpty(tabelle)) return 0;

            Katalogkostengewerk gewerk = Gewerk(tabelle);
            if (gewerk == null) return 0;

            int idVorlage = Ganz(r, KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION);
            if (idVorlage <= 0) return 0;

            double summe = 0;
            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(idVorlage))
            {
                if (p == null || p.IstErloes || !p.Satz.HasValue) continue;
                double? betrag = Positionsbetrag(gewerk, r, p);
                if (betrag.HasValue && betrag.Value > 0) summe += betrag.Value;
            }
            return summe;
        }

        /// <summary>
        /// Der Betrag EINER Vorlagenposition an diesem Satz: ein absoluter Satz ist der Betrag,
        /// ein spezifischer wird mit der Baugröße eines Stücks multipliziert. <c>null</c> = die
        /// Position trägt an diesem Gewerk nichts bei (unbekannte Bemessung, fehlende oder leere
        /// Bezugsgröße) — dieselbe Regel wie beim Planwert der Anlage.
        /// </summary>
        private static double? Positionsbetrag(Katalogkostengewerk gewerk, DataRow r,
                                               KostenVorlagenPosition p)
        {
            if (Katalogkostenabgleich.Absolut(p.Bemessung)) return p.Satz.Value;

            Katalogkostenfeld feld = null;
            foreach (Katalogkostenfeld kandidat in gewerk.Felder)
                if (string.Equals(kandidat.Bemessung, p.Bemessung, StringComparison.Ordinal) &&
                    !string.IsNullOrEmpty(kandidat.Bezugsspalte)) { feld = kandidat; break; }
            if (feld == null) return null;

            double groesse = Zahl(r, feld.Bezugsspalte) /
                             (feld.Bezugsteiler == 0 ? 1.0 : feld.Bezugsteiler);
            return groesse > 0 ? (double?)(p.Satz.Value * groesse) : null;
        }

        /// <summary>Das Gewerk zu einer Katalog- oder Projekttabelle; <c>null</c> = keines.</summary>
        private static Katalogkostengewerk Gewerk(string tabelle)
        {
            foreach (Katalogkostengewerk g in GeraetekostenEntfallSchema.GEWERKE)
                if (string.Equals(g.Katalog, tabelle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(g.Kopie, tabelle, StringComparison.OrdinalIgnoreCase)) return g;
            return null;
        }

        private static double Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static int Ganz(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
