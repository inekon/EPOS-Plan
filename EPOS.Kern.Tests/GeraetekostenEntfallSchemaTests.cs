using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Die Kostenspalten der Investitionsgewerke entfallen"</b>
    /// (<see cref="GeraetekostenEntfallSchema"/>, KB-4): Nummer und Register, der Entfall der 32 Spalten,
    /// die Satzvorlage des Wechselrichters, die Prüfsummen der acht Kataloge nach dem Schritt, der leere
    /// zweite Lauf und der benannte Abbruch, wenn noch ein Kostenwert ohne Satzvorlage steht.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GeraetekostenEntfallSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_228_und_steht_im_Register()
        {
            Assert.Equal(228, GeraetekostenEntfallSchema.SCHRITT);
            // Die Kette hängt über + 1 an 227 (KB-2, Brennstoffpreise in die Trägerkarte).
            Assert.Equal(BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1, GeraetekostenEntfallSchema.SCHRITT);
            Assert.Equal(GeraetekostenEntfallSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen,
                            s => s.Nr == GeraetekostenEntfallSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
        }

        [Fact]
        public void Die_Welle_fuehrt_acht_Gewerke_und_zweiunddreissig_Spalten()
        {
            Assert.Equal(8, GeraetekostenEntfallSchema.GEWERKE.Count);
            // Der Stromspeicher gehört zu KB-5 und bleibt außen vor; sein Rechenweg liest die Spalten noch.
            Assert.DoesNotContain(GeraetekostenEntfallSchema.GEWERKE,
                                  g => g.Kopie.IndexOf("Stromspeicher", StringComparison.Ordinal) >= 0);
            Assert.Equal(32, GeraetekostenEntfallSchema.Entfall.Count());
            // Je Gewerk steht zuerst der Katalog, dann die Projektkopie — und jede Spalte genau einmal.
            Assert.Equal(32, GeraetekostenEntfallSchema.Entfall.Distinct().Count());
        }

        [Fact]
        public void Die_zweiunddreissig_Kostenspalten_sind_fort_und_stehen_in_keiner_Liste_mehr()
        {
            if (!_db.Vorhanden) return;
            Assert.True(GeraetekostenEntfallSchema.Vollstaendig());
            Assert.Equal(0, GeraetekostenEntfallSchema.Offen());
            Assert.Empty(GeraetekostenEntfallSchema.OhneVerweis());

            foreach ((string tabelle, string spalte) in GeraetekostenEntfallSchema.Entfall)
            {
                Assert.False(DataRepository.SpalteVorhanden(tabelle, spalte),
                             tabelle + "." + spalte + " steht noch.");
                Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
                if (t != null) Assert.DoesNotContain(spalte, t.Fachspalten);
            }
            // Die Kältemaschine führt ihre Fachspalten in einer eigenen Liste.
            Assert.DoesNotContain(KaeltemaschineSchema.SPALTE_MODULKOSTEN, KaeltemaschineSchema.Fachspalten);
            Assert.DoesNotContain(KaeltemaschineSchema.SPALTE_MODULKOSTEN, KaeltemaschineSchema.Grundspalten);
        }

        [Fact]
        public void Der_Investitionsverweis_des_Wechselrichters_steht_an_Katalog_und_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            foreach ((string tabelle, string spalte, string _) in GeraetekostenEntfallSchema.SPALTEN)
            {
                Assert.Equal(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION, spalte);
                Assert.True(DataRepository.SpalteVorhanden(tabelle, spalte));
            }
            // OHNE Betriebskostenspalte KEIN Betriebsverweis — eine leere Spalte wäre eine Zusage ohne Einlösung.
            Assert.False(DataRepository.SpalteVorhanden(GeraetekostenEntfallSchema.TAB_WR_STAMM,
                                                        KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE));
        }

        [Fact]
        public void Die_Satzvorlage_des_Wechselrichters_traegt_den_Stueckpreis_je_kW()
        {
            if (!_db.Vorhanden) return;
            // Der eine Wechselrichter der Testdatenbank: 1 200 € bei 2,5 kW Nennleistung -> 480 €/kW.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\", \"" + KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION + "\" AS V " +
                "FROM \"" + GeraetekostenEntfallSchema.TAB_WR_STAMM + "\"");
            Assert.NotNull(dt);
            Assert.NotEmpty(dt.Rows);
            int idVorlage = Convert.ToInt32(dt.Rows[0]["V"], CultureInfo.InvariantCulture);
            Assert.True(idVorlage > 0, "Der Wechselrichter trägt keine Satzvorlage.");

            IList<KostenVorlagenPosition> positionen = KostenVorlagenCtrl.Positionen(idVorlage);
            Assert.Single(positionen);
            Assert.Equal(GeraetekostenEntfallSchema.BEZEICHNUNG_WECHSELRICHTER, positionen[0].Bezeichnung);
            // „je kW elektrisch": die Art, die das Gewerk der Vorlage (Photovoltaik) kennt.
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, positionen[0].Bemessung);
            Assert.True(positionen[0].Satz.HasValue);
            Assert.Equal(480.0, positionen[0].Satz.Value, 3);
        }

        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;
            GeraetekostenEntfallSchema.Laufergebnis e = GeraetekostenEntfallSchema.Lauf(null);
            Assert.Equal(0, e.Handgriffe);
            Assert.Equal(0, e.Verweisspalten);
            Assert.Equal(0, e.Gesaet);
            Assert.Equal(0, e.Entfernt);
            Assert.Equal(0, e.PruefsummenNeu);
        }

        [Fact]
        public void Die_Pruefsummen_der_acht_Kataloge_gelten_nach_dem_Schritt_als_unveraendert()
        {
            if (!_db.Vorhanden) return;
            // DER NACHWEIS DER ALTEN SPALTENFOLGE: Hätte ALTE_FOLGE einen Platz falsch, hätte der Schritt die
            // Summe des Satzes NICHT als stimmig erkannt und nicht neu gebildet — der Satz trüge dann die alte
            // Summe und würde hier als „vom Anwender geändert" auffallen.
            int geprueft = 0;
            foreach (string katalog in GeraetekostenEntfallSchema.ALTE_FOLGE.Keys)
            {
                Katalogtabelle t = Katalogfassung.Tabelle(katalog);
                if (t == null || !Katalogfassung.SpaltenVorhanden(katalog)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT \"ID\", \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" AS S, " +
                    Katalogfassung.Spaltentext(spalten) + " FROM \"" + katalog + "\" WHERE \"" +
                    Katalogfassung.SPALTE_PRUEFSUMME + "\" IS NOT NULL");
                if (dt == null) continue;
                foreach (DataRow r in dt.Rows)
                {
                    Assert.Equal(Convert.ToString(r["S"], CultureInfo.InvariantCulture),
                                 Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang));
                    geprueft++;
                }
            }
            // In der Testdatenbank tragen BHKW (79), Kältemaschine (37), Wärmepumpe (8) und Wechselrichter (1)
            // einen Auslieferungsstand.
            Assert.Equal(125, geprueft);
        }

        [Fact]
        public void Ein_Kostenwert_ohne_Bezugsgroesse_bricht_den_Schritt_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            // Die Lage, gegen die die Wache steht: ein Stückpreis OHNE seine Baugröße. KB-3 sät ihn bewusst
            // nicht (kein Fantasiesatz), hier wäre sein Entfall ein stiller Datenverlust.
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_PV\" ADD COLUMN \"Modulkosten\" REAL");
            DataTable dt = DataRepository.GetDataTable("SELECT \"ID\" FROM \"Tab_PV\" ORDER BY \"ID\" LIMIT 1");
            Assert.NotNull(dt);
            Assert.NotEmpty(dt.Rows);
            int id = Convert.ToInt32(dt.Rows[0]["ID"], CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_PV\" SET \"Modulkosten\" = 500, \"Leistung\" = 0, \"" +
                KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION + "\" = NULL WHERE \"ID\" = ?",
                new DbParam("@id", id));

            IReadOnlyList<string> offen = GeraetekostenEntfallSchema.OhneVerweis();
            Assert.NotEmpty(offen);
            Assert.Contains(offen, z => z.IndexOf("Tab_PV ID " + id, StringComparison.Ordinal) >= 0);

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => GeraetekostenEntfallSchema.Lauf(null));
            Assert.Contains("ohne Satzvorlage", ex.Message, StringComparison.Ordinal);
            Assert.Contains("228", ex.Message, StringComparison.Ordinal);

            // DIE SPALTE BLEIBT STEHEN: Der Abbruch hält den Kostenwert, bis die Baugröße gepflegt ist.
            Assert.True(DataRepository.SpalteVorhanden("Tab_PV", "Modulkosten"));
        }
    }
}
