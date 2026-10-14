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
    /// <b>Schemaschritt „Brennstoffpreise in die Trägerkarte"</b>
    /// (<see cref="BrennstoffpreiseTraegerkarteSchema"/>, KB-2): Nummer und Register, der Entfall der sechs
    /// Spalten, die Prüfsumme des Brennstoffkatalogs nach dem Schritt und die vier Zusagen des Übertrags —
    /// ein leerer Trägerpreis bekommt den Katalogwert, ein gepflegter bleibt, der Leistungspreis bekommt den
    /// Zeitbezug JAHR, ein zweiter Lauf ändert nichts.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BrennstoffpreiseTraegerkarteSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_227_und_steht_im_Register()
        {
            Assert.Equal(227, BrennstoffpreiseTraegerkarteSchema.SCHRITT);
            // Die Kette hängt über + 1 an 226 (KB-3, Saat der Katalogkosten).
            Assert.Equal(KatalogkostenSaatSchema.SCHRITT + 1, BrennstoffpreiseTraegerkarteSchema.SCHRITT);
            // Das Ziel steht auf dem Nachfolger (BK-5/BK-6, BauteilkatalogAbschnittSchema), der über + 1 hier anhängt.
            Assert.Equal(BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen,
                            s => s.Nr == BrennstoffpreiseTraegerkarteSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
        }

        [Fact]
        public void Die_sechs_Preisspalten_sind_fort_und_stehen_in_keiner_Liste_mehr()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BrennstoffpreiseTraegerkarteSchema.Vollstaendig());
            Assert.Equal(0, BrennstoffpreiseTraegerkarteSchema.Offen());
            Assert.Equal(0, BrennstoffpreiseTraegerkarteSchema.Uebertragbar());

            foreach ((string _, string standardspalte) in BrennstoffpreiseTraegerkarteSchema.PAARE)
            {
                Assert.False(DataRepository.SpalteVorhanden(BrennstoffpreiseTraegerkarteSchema.TAB_STAMM, standardspalte));
                Assert.False(DataRepository.SpalteVorhanden(BrennstoffpreiseTraegerkarteSchema.TAB_PROJEKT, standardspalte));
                // Der Katalog der Fassungen, die Projektkopie und der Projektdialog führen die Spalte nicht mehr.
                Assert.DoesNotContain(standardspalte,
                                      Katalogfassung.Tabelle(BrennstoffpreiseTraegerkarteSchema.TAB_STAMM).Fachspalten);
                Assert.DoesNotContain(standardspalte, ProjektBrennstoffe.FACHSPALTEN);
                Assert.DoesNotContain(standardspalte, ProjektBrennstoffe.BEARBEITBAR);
            }
            // Die Textspalte der Preiseinheit bleibt - sie ist Katalogtext, kein Preis.
            Assert.Contains("PreisEinheit", ProjektBrennstoffe.FACHSPALTEN);
        }

        [Fact]
        public void Die_Pruefsumme_eines_unveraenderten_Satzes_wird_neu_gebildet_die_eines_geaenderten_nicht()
        {
            if (!_db.Vorhanden) return;
            // In der Testdatenbank trägt kein Brennstoff einen Auslieferungsstand (ReadOnly = 0, kein Schlüssel,
            // keine Prüfsumme) — die Lage einer Anwenderdatenbank wird deshalb hier hergestellt.
            SpaltenZurueckholen();
            (int _, int stimmig) = Traeger(0);
            (int _, int geaendert) = Traeger(1);
            Setze(stimmig, 0.09, 120.0, 12.0);
            Setze(geaendert, 0.07, 110.0, 11.0);

            string alt = BrennstoffpreiseTraegerkarteSchema.AltePruefsumme(stimmig);
            Assert.False(string.IsNullOrEmpty(alt));
            const string falsch = "0000000000000000000000000000000000000000000000000000000000000000";
            Summe(stimmig, alt);
            Summe(geaendert, falsch);

            BrennstoffpreiseTraegerkarteSchema.Laufergebnis e = BrennstoffpreiseTraegerkarteSchema.Lauf(null);
            Assert.Equal(1, e.PruefsummenNeu);

            // Der unveränderte Satz trägt die Summe der NEUEN Spaltenfolge — er gilt weiter als unverändert.
            Katalogtabelle t = Katalogfassung.Tabelle(BrennstoffpreiseTraegerkarteSchema.TAB_STAMM);
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\", \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\", " + Katalogfassung.Spaltentext(spalten) +
                " FROM \"" + BrennstoffpreiseTraegerkarteSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                new DbParam("@id", stimmig));
            Assert.NotNull(dt);
            Assert.Single(dt.Rows);
            string jetzt = Convert.ToString(dt.Rows[0][Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture);
            Assert.Equal(Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], Katalogfassung.LeseOhneVorgang), jetzt);
            Assert.NotEqual(alt, jetzt);

            // Der geänderte Satz behält seine Summe — er bleibt als geändert erkennbar.
            Assert.Equal(falsch, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" FROM \"" +
                BrennstoffpreiseTraegerkarteSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                new DbParam("@id", geaendert)), CultureInfo.InvariantCulture));
        }

        private static void Summe(int idBrennstoff, string summe)
            => DataRepository.ExecuteNonQuery(
                "UPDATE \"" + BrennstoffpreiseTraegerkarteSchema.TAB_STAMM + "\" SET \"" +
                Katalogfassung.SPALTE_PRUEFSUMME + "\" = ? WHERE \"ID\" = ?",
                new DbParam("@s", summe), new DbParam("@id", idBrennstoff));

        [Fact]
        public void Der_Uebertrag_nimmt_den_leeren_Traegerpreis_und_laesst_den_gepflegten()
        {
            if (!_db.Vorhanden) return;
            SpaltenZurueckholen();

            // Zwei Träger mit verschiedenen Brennstoffarten: der erste leer, der zweite gepflegt.
            (int leer, int brennstoffLeer) = Traeger(0);
            (int gepflegt, int brennstoffGepflegt) = Traeger(1);
            Assert.NotEqual(brennstoffLeer, brennstoffGepflegt);

            Setze(brennstoffLeer, 0.09, 120.0, 12.0);
            Setze(brennstoffGepflegt, 0.09, 120.0, 12.0);
            DataRepository.ExecuteNonQuery(
                "UPDATE \"energy_carrier\" SET \"price_work\" = 0, \"price_base\" = 0, \"price_power\" = 0, " +
                "\"price_power_modus\" = NULL WHERE \"id\" = ?", new DbParam("@id", leer));
            DataRepository.ExecuteNonQuery(
                "UPDATE \"energy_carrier\" SET \"price_work\" = 7.5, \"price_base\" = 99, \"price_power\" = 44, " +
                "\"price_power_modus\" = ? WHERE \"id\" = ?",
                new DbParam("@m", DbWerte.LEISTUNGSPREIS_MODUS_MONAT), new DbParam("@id", gepflegt));

            Assert.True(BrennstoffpreiseTraegerkarteSchema.Uebertragbar() >= 3);
            BrennstoffpreiseTraegerkarteSchema.Laufergebnis e = BrennstoffpreiseTraegerkarteSchema.Lauf(null);
            Assert.True(e.Uebertragen >= 3, "Der Übertrag hat " + e.Uebertragen + " Preise gesetzt.");
            Assert.Equal(6, e.Entfernt);

            // Der leere Träger hat die Katalogwerte und den Zeitbezug JAHR.
            Assert.Equal(0.09, Wert(leer, "price_work"), 6);
            Assert.Equal(120.0, Wert(leer, "price_base"), 6);
            Assert.Equal(12.0, Wert(leer, "price_power"), 6);
            Assert.Equal(DbWerte.LEISTUNGSPREIS_MODUS_JAHR, Text(leer, "price_power_modus"));

            // Der gepflegte Träger bleibt, wie er war - samt seinem Zeitbezug.
            Assert.Equal(7.5, Wert(gepflegt, "price_work"), 6);
            Assert.Equal(99.0, Wert(gepflegt, "price_base"), 6);
            Assert.Equal(44.0, Wert(gepflegt, "price_power"), 6);
            Assert.Equal(DbWerte.LEISTUNGSPREIS_MODUS_MONAT, Text(gepflegt, "price_power_modus"));

            // Zweiter Lauf: nichts mehr zu tun.
            Assert.True(BrennstoffpreiseTraegerkarteSchema.Vollstaendig());
            Assert.Equal(0, BrennstoffpreiseTraegerkarteSchema.Ausfuehren(null));
            Assert.Equal(0.09, Wert(leer, "price_work"), 6);
            Assert.Equal(7.5, Wert(gepflegt, "price_work"), 6);
        }

        // =====================================================================
        //  Hilfen - die Spalten für den Übertrag noch einmal herstellen
        // =====================================================================

        /// <summary>
        /// Holt die sechs Spalten zurück, die der Schritt in der Arbeitskopie längst entfernt hat — nur so lässt
        /// sich der Übertrag an gesetzten Werten messen. Beide Tabellen sind STRICT, deshalb mit Typ.
        /// </summary>
        private static void SpaltenZurueckholen()
        {
            foreach (string tabelle in new[] { BrennstoffpreiseTraegerkarteSchema.TAB_STAMM,
                                               BrennstoffpreiseTraegerkarteSchema.TAB_PROJEKT })
                foreach ((string _, string standardspalte) in BrennstoffpreiseTraegerkarteSchema.PAARE)
                    if (!DataRepository.SpalteVorhanden(tabelle, standardspalte))
                        DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + standardspalte +
                                                       "\" REAL DEFAULT 0");
            Assert.Equal(6, BrennstoffpreiseTraegerkarteSchema.Offen());
        }

        /// <summary>Der <paramref name="rang"/>-te Träger mit Brennstoffverweis: seine Kennung und Brennstoffart.</summary>
        private static (int Traeger, int Brennstoff) Traeger(int rang)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT c.\"id\", c.\"ID_Brennstoff\" FROM \"energy_carrier\" AS c " +
                "JOIN \"" + BrennstoffpreiseTraegerkarteSchema.TAB_STAMM + "\" AS b ON b.\"ID\" = c.\"ID_Brennstoff\" " +
                "GROUP BY c.\"ID_Brennstoff\" ORDER BY c.\"ID_Brennstoff\"");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > rang, "Die Testdatenbank führt zu wenige Träger mit Brennstoffverweis.");
            DataRow r = dt.Rows[rang];
            return (Convert.ToInt32(r[0], CultureInfo.InvariantCulture), Convert.ToInt32(r[1], CultureInfo.InvariantCulture));
        }

        private static void Setze(int idBrennstoff, double arbeit, double grund, double leistung)
            => DataRepository.ExecuteNonQuery(
                "UPDATE \"" + BrennstoffpreiseTraegerkarteSchema.TAB_STAMM + "\" SET \"Standard_Arbeitspreis\" = ?, " +
                "\"Standard_Grundpreis\" = ?, \"Standard_Leistungspreis\" = ? WHERE \"ID\" = ?",
                new DbParam("@a", arbeit), new DbParam("@g", grund), new DbParam("@l", leistung),
                new DbParam("@id", idBrennstoff));

        private static double Wert(int idTraeger, string spalte)
            => Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT \"" + spalte + "\" FROM \"energy_carrier\" WHERE \"id\" = ?",
                new DbParam("@id", idTraeger)), CultureInfo.InvariantCulture);

        private static string Text(int idTraeger, string spalte)
            => Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"" + spalte + "\" FROM \"energy_carrier\" WHERE \"id\" = ?",
                new DbParam("@id", idTraeger)), CultureInfo.InvariantCulture);
    }
}
