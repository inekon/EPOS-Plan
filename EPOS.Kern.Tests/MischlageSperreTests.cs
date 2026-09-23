using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>S‑2 (Entscheid A3 vom 20.09.2026): die Mischlage § 53 / § 53a
    /// Abs. 5 neben § 54 ist gesperrt</b> — der § 54-Betrag wird verworfen (0), mit
    /// Begründung an der § 54-Zeile und einer Kohärenzzeile der Schwere WARNUNG an Stelle
    /// des früheren Hinweises (Fall 5).
    ///
    /// <para><b>A/B an 1030</b> (produzierendes Gewerbe, BHKW nach § 53 als Projektwahl,
    /// Kessel nach § 54 als Anlagenwahl; gemessen 23.09.2026): vorher § 53 6.369,49 € +
    /// § 54 7.987,41 € (nach Sockel), Kapitalwert −20.388.846,98 €; nachher § 54 0 €,
    /// Kapitalwert −20.507.679,50 € (−118.832,52 €) — genau der Wert der Probe „nur § 53".
    /// Die dreizehn Basisprojekte tragen keine Energiesteuerwahl und bleiben gleich.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class MischlageSperreTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int KESSEL = 11334;

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Probe " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
        }

        private static void Wahl(string projektwahl, string kesselwahl)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Unternehmensart = ?, Energiesteuer_Wahl = ? WHERE ID_Projekt = ?",
                new DbParam("@u", DbWerte.UNTERNEHMENSART_PROD_GEWERBE),
                new DbParam("@w", projektwahl),
                new DbParam("@p", PROJEKT));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET Energiesteuer_Wahl = ? WHERE ID = ?",
                new DbParam("@w", (object)kesselwahl ?? DBNull.Value),
                new DbParam("@id", KESSEL));
        }

        [Fact]
        public void Die_Mischlage_verwirft_den_Paragraf_54_Betrag_mit_Begruendung_und_Warnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Wahl(DbWerte.ENERGIESTEUER_WAHL_53, DbWerte.ENERGIESTEUER_WAHL_54);
            WirtschaftlichkeitErgebnis e = Rechne();

            Assert.Equal(6369.49, e.Energiesteuer53Jahr1, 2);
            Assert.Equal(0.0, e.Energiesteuer54Jahr1, 6);          // vorher 7.987,41
            Assert.Equal(-20507679.50, e.Kapitalwert.Value, 2);     // vorher −20.388.846,98

            string grund;
            Assert.True(e.PositionsGruende.TryGetValue(SteuerPosition.ENERGIEST_54, out grund));
            Assert.StartsWith("§ 54 EnergieStG gesperrt", grund);

            Assert.Contains(e.KohaerenzHinweise, k => k.Schwere == KohaerenzSchwere.WARNUNG &&
                                                      k.Text.Contains("Diese Mischlage ist gesperrt"));
        }

        /// <summary>Ohne Mischlage (alles § 54) bleibt der § 54-Betrag, und die Zeile
        /// fehlt — die Sperre greift nur in der Mischlage.</summary>
        [Fact]
        public void Ohne_Mischlage_bleibt_Paragraf_54_und_die_Zeile_fehlt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Wahl(DbWerte.ENERGIESTEUER_WAHL_54, null);
            WirtschaftlichkeitErgebnis e = Rechne();

            Assert.Equal(9585.57, e.Energiesteuer54Jahr1, 2);
            Assert.Equal(-20459832.25, e.Kapitalwert.Value, 2);
            Assert.DoesNotContain(e.KohaerenzHinweise, k => k.Text.Contains("Mischlage"));
        }
    }
}
