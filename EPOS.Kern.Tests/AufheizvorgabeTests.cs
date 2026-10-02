using System;
using System.Data;
using System.Globalization;
using System.IO;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projekteinstellung der Aufheizoptimierung</b> (Entwurf KP3, Grundsatz 5; Festlegung 24) —
    /// der Record <see cref="Aufheizvorgabe"/>, <see cref="KonfigurationCtrl.AufheizvorgabeLesen"/> und
    /// <see cref="KonfigurationCtrl.AufheizvorgabeSchreiben"/>, die Naht
    /// <c>SimulationWaermebedarf.AufheizvorgabeProjekt</c>.
    ///
    /// <para><b>Geprüft wird:</b> die Normalisierung des Records ((a) und „täglich" werden NULL, ein leeres
    /// Zahlenfeld NULL, ein getippter Wert bleibt, der Schalter aus behält die Werte) und seine wirksamen
    /// Werte; ohne Zeile und ohne Spalte heißt es „aus"; der Rundlauf Schreiben → Lesen samt der
    /// gespeicherten Form; ein Wert außerhalb der Prüfklauseln scheitert ohne Spur; die Naht liest einmal
    /// je Lauf und Auskunft; das Speichern der Kaskade, das Projektduplikat (der Weg der Variante) und der
    /// Projekttransfer tragen die Einstellung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizvorgabeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1007 „Laurentiuskirche" mit genau einem Einstellungssatz.</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";

        /// <summary>Ein Referenzprojekt mit Einstellungssatz für das Speichern der Kaskade.</summary>
        private const int REFERENZ = 1030;

        /// <summary>Eine gepflegte Einstellung: an, Bemessung (b) mit 3 K, ρ = 25 %, fest.</summary>
        private static readonly Aufheizvorgabe GEPFLEGT =
            new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 3.0, 0.25, DbWerte.AUFHEIZ_ART_FEST);

        // =============================================================================
        //  Teil 1 - der Record (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// <b>Festlegung 24:</b> (a) und „täglich" werden NULL, ein leerer Text ebenso; ein Text ohne Rand;
        /// NaN und ±∞ heißen „leer"; ein getippter Wert bleibt, auch wenn er der Vorgabe gleicht.
        /// </summary>
        [Fact]
        public void Die_Normalisierung_folgt_Festlegung_24()
        {
            var a = new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, null, null, DbWerte.AUFHEIZ_ART_TAEGLICH);
            Assert.True(a.An);
            Assert.Null(a.Bemessung);
            Assert.Null(a.Art);
            Assert.Equal(new Aufheizvorgabe(true, null, null, null, null), a);

            var b = new Aufheizvorgabe(false, "  " + DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG + " ", 2.0, 0.2, " ");
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, b.Bemessung);
            Assert.Equal(2.0, b.AbzugK);      // getippt wie die Vorgabe - bleibt
            Assert.Equal(0.2, b.Reserve);     // getippt wie die Vorgabe - bleibt
            Assert.Null(b.Art);
            Assert.NotEqual(new Aufheizvorgabe(false, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, null, null, null), b);

            var c = new Aufheizvorgabe(true, "", double.NaN, double.PositiveInfinity, "");
            Assert.Null(c.Bemessung);
            Assert.Null(c.AbzugK);
            Assert.Null(c.Reserve);
            Assert.Null(c.Art);

            // Ein Wert außerhalb der Prüfklauseln bleibt stehen - das Schreiben scheitert an der Spalte.
            var d = new Aufheizvorgabe(true, "X", 11.0, 0.0, "MONATLICH");
            Assert.Equal("X", d.Bemessung);
            Assert.Equal(11.0, d.AbzugK);
            Assert.Equal(0.0, d.Reserve);
            Assert.Equal("MONATLICH", d.Art);
        }

        /// <summary>Die wirksamen Werte setzen die Vorgaben ein: (a), 2 K, 20 %, täglich.</summary>
        [Fact]
        public void Die_wirksamen_Werte_setzen_die_Vorgaben_ein()
        {
            Aufheizvorgabe aus = Aufheizvorgabe.Aus;
            Assert.False(aus.An);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, aus.BemessungWirksam);
            Assert.False(aus.MitAbzug);
            Assert.Equal(2.0, aus.AbzugWirksamK);
            Assert.Equal(0.2, aus.ReserveWirksam);
            Assert.Equal(DbWerte.AUFHEIZ_ART_TAEGLICH, aus.ArtWirksam);
            Assert.False(aus.IstFest);

            Assert.True(GEPFLEGT.MitAbzug);
            Assert.Equal(3.0, GEPFLEGT.AbzugWirksamK);
            Assert.Equal(0.25, GEPFLEGT.ReserveWirksam);
            Assert.True(GEPFLEGT.IstFest);

            // Der Schalter aus behält die übrigen Werte (Festlegung 24).
            Aufheizvorgabe ausGepflegt = new Aufheizvorgabe(false, GEPFLEGT.Bemessung, GEPFLEGT.AbzugK, GEPFLEGT.Reserve, GEPFLEGT.Art);
            Assert.False(ausGepflegt.An);
            Assert.Equal(GEPFLEGT.Bemessung, ausGepflegt.Bemessung);
            Assert.Equal(GEPFLEGT.Reserve, ausGepflegt.Reserve);
            Assert.NotEqual(Aufheizvorgabe.Aus, ausGepflegt);
        }

        // =============================================================================
        //  Teil 2 - Lesen und Schreiben
        // =============================================================================

        /// <summary>Jedes Projekt der Testdatenbank liest „aus"; ohne Zeile und mit ungültiger ID ebenso.</summary>
        [Fact]
        public void Ohne_Zeile_und_im_Bestand_heisst_es_aus()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(987654));
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(0));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSchreiben(987654, GEPFLEGT));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, null));
        }

        /// <summary>
        /// Eine Datenbank vor KP-S2 (die fünf Spalten fehlen): Lesen heißt „aus", Schreiben liefert
        /// <c>false</c> und lässt die Zeile stehen.
        /// </summary>
        [Fact]
        public void Ohne_Spalten_heisst_es_aus()
        {
            if (!_db.Vorhanden) return;

            foreach (string sp in new[] { "Aufheizoptimierung", "Aufheiz_Bemessung", "Aufheiz_Abzug_K", "Aufheiz_Reserve", "Aufheiz_Art" })
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"" + sp + "\"");

            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = " + PROJEKT));

            // Nur EINE fehlende Spalte genügt für „aus".
            Assert.Equal(5, AufheizvorgabeSchema.Ausfuehren(null));
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"Aufheiz_Art\"");
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
        }

        /// <summary>
        /// <b>Der Rundlauf:</b> Was geschrieben wird, kommt gleich zurück, und die Datenbank führt die
        /// normalisierte Form — (a) und „täglich" als NULL, der Schalter als 0/1, ein getippter Wert, wie er
        /// ist; der Schalter aus behält die übrigen Spalten.
        /// </summary>
        [Fact]
        public void Schreiben_und_Lesen_ergeben_dieselbe_Einstellung()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            Assert.Equal("1|STUNDE_ABZUG|3|0.25|FEST", Roh(PROJEKT));

            var vorgabe = new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 2.0, null, DbWerte.AUFHEIZ_ART_TAEGLICH);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, vorgabe));
            Assert.Equal(vorgabe, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            Assert.Equal("1||2||", Roh(PROJEKT));

            var ausGepflegt = new Aufheizvorgabe(false, GEPFLEGT.Bemessung, GEPFLEGT.AbzugK, GEPFLEGT.Reserve, GEPFLEGT.Art);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, ausGepflegt));
            Assert.Equal(ausGepflegt, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            Assert.Equal("0|STUNDE_ABZUG|3|0.25|FEST", Roh(PROJEKT));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, Aufheizvorgabe.Aus));
            Assert.Equal("0||||", Roh(PROJEKT));
        }

        /// <summary>
        /// Ein Wert außerhalb der Prüfklauseln scheitert an der Spalte: <c>false</c>, und die Zeile bleibt
        /// in allen fünf Spalten, wie sie war (ein <c>UPDATE</c>, keine halbe Einstellung).
        /// </summary>
        [Fact]
        public void Ein_ungueltiger_Wert_scheitert_ohne_Spur()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            foreach (Aufheizvorgabe falsch in new[]
                     {
                         new Aufheizvorgabe(true, null, null, 0.0, null),        // ρ = 0 (Festlegung 15)
                         new Aufheizvorgabe(true, null, null, 1.2, null),
                         new Aufheizvorgabe(true, null, 10.5, null, null),
                         new Aufheizvorgabe(true, null, -1.0, null, null),
                         new Aufheizvorgabe(true, "STUNDE_PLUS", null, null, null),
                         new Aufheizvorgabe(true, null, null, null, "MONATLICH"),
                     })
            {
                Assert.False(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, falsch), falsch.ToString());
                Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT));
            }
        }

        /// <summary>
        /// <b>Die Naht</b> <c>SimulationWaermebedarf.AufheizvorgabeProjekt</c> liest EINMAL je Lauf und
        /// Auskunft: Eine Änderung danach sieht sie erst nach dem Vorbereitungsschritt; der Setter ist die
        /// Testnaht, <c>null</c> setzt „aus".
        /// </summary>
        [Fact]
        public void Die_Naht_liest_einmal_je_Lauf_und_Auskunft()
        {
            if (!_db.Vorhanden) return;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            Assert.Equal(Aufheizvorgabe.Aus, sim.AufheizvorgabeProjekt);

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            Assert.Equal(Aufheizvorgabe.Aus, sim.AufheizvorgabeProjekt);   // einmal gelesen

            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            Assert.Equal(GEPFLEGT, sim.AufheizvorgabeProjekt);             // je Lauf neu

            sim.AufheizvorgabeProjekt = null;
            Assert.Equal(Aufheizvorgabe.Aus, sim.AufheizvorgabeProjekt);
        }

        // =============================================================================
        //  Teil 3 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>
        /// Die Konfigurationsseite speichert die Kaskade als Löschen und Neuanlegen der ganzen Zeile. Eine
        /// gepflegte Einstellung steht danach weiter da, auch ein Schalter aus mit gepflegten Werten; „aus und
        /// leer" bleibt leer.
        /// </summary>
        [Fact]
        public void Das_Speichern_der_Kaskade_erhaelt_die_Einstellung()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(REFERENZ, GEPFLEGT));
            long idVorher = Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ);
            Assert.True(Kaskadendienste(REFERENZ).Speichern());
            Assert.NotEqual(idVorher, Zahl("SELECT ID FROM Tab_Einstellungen WHERE ID_Projekt = " + REFERENZ));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));

            var ausGepflegt = new Aufheizvorgabe(false, null, 4.0, null, DbWerte.AUFHEIZ_ART_FEST);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(REFERENZ, ausGepflegt));
            Assert.True(Kaskadendienste(REFERENZ).Speichern());
            Assert.Equal(ausGepflegt, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(REFERENZ, Aufheizvorgabe.Aus));
            Assert.True(Kaskadendienste(REFERENZ).Speichern());
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(REFERENZ));
        }

        /// <summary>
        /// Das Projektduplikat und die Variante (derselbe Kopierweg) tragen die Einstellung mit; ein Projekt
        /// auf „aus" bleibt „aus".
        /// </summary>
        [Fact]
        public void Projektduplikat_und_Variante_tragen_die_Einstellung()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Aufheizung");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(neu));

            int variante = new VariantenCtrl().AnlegenAusStamm(PROJEKT, PROJEKTNAME, "Rampe", out string fehler);
            Assert.True(variante > 0, "Variante: " + fehler);
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(variante));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, Aufheizvorgabe.Aus));
            int aus = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Aufheizung aus");
            Assert.True(aus > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(aus));
            Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(variante));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die Einstellung über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Einstellung()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT, GEPFLEGT));
            string ordner = Path.Combine(Path.GetTempPath(), "epos-aufheiz-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Aufheizung", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(GEPFLEGT, KonfigurationCtrl.AufheizvorgabeLesen(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static SimulationKonfigDienste Kaskadendienste(int idProjekt)
            => (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt).Gaben()["Dienste"];

        /// <summary>Die fünf Spalten, wie sie in der Datenbank stehen: <c>Schalter|Bemessung|Abzug|Reserve|Art</c>, NULL leer.</summary>
        private static string Roh(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Aufheizoptimierung, Aufheiz_Bemessung, Aufheiz_Abzug_K, Aufheiz_Reserve, Aufheiz_Art " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("?", projekt));
            Assert.True(dt != null && dt.Rows.Count == 1, "Kein eindeutiger Einstellungssatz für " + projekt + ".");
            var teile = new string[5];
            for (int i = 0; i < 5; i++)
                teile[i] = dt.Rows[0][i] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0][i], CultureInfo.InvariantCulture);
            return string.Join("|", teile);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
