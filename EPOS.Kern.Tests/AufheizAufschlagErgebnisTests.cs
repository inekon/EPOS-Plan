using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 194 — verwendeter Aufschlag und bemessene Aufheizzeit im Ergebnis</b> (Entscheid E99, Welle A von KP3;
    /// <see cref="AufheizAufschlagErgebnisSchema"/>): Nummer und Kette, die zwei Spalten an <c>Tab_ErgebnisGebaeude</c>,
    /// die Werte am Plan (täglich, fest, manuell, Rampe mit einer Stufe), in der Ergebniszeile und im Mehrzonengebäude,
    /// die Rundreise über <see cref="ErgebnisCtrl"/> (Schalter aus = NULL), die Gebäudetafel des Berichts (mit und ohne
    /// die Werte) und die zwei Vorlagenfelder der Katalogfassung 16.
    ///
    /// <para><b>Synthetisch</b> mit dem Prüfsatz der Rändertests: Tag/Nacht 20/17 °C, Nacht 22–6 Uhr (D = 8 h), die
    /// Grenze für n = 4 am Bemessungsfall (t_auf,max = 3 h). <b>Am Lauf</b> Projekt 1018 (Hotel).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizAufschlagErgebnisTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AufheizAufschlagErgebnisTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1018;

        private static Aufheizvorgabe An(int? h, double? p, bool fest = false)
            => new Aufheizvorgabe(true, null, null, null, fest ? DbWerte.AUFHEIZ_ART_FEST : null, h, p);

        private static Aufheizzone Standard(int? manuell = null, double heizMaxW = double.NaN)
            => AufheizRaenderTests.Zone(AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22),
                                        heizMaxW: double.IsNaN(heizMaxW) ? AufheizRaenderTests.Grenze(20.0, 3.0, -12.0, 4) : heizMaxW)
               with { ManuellH = manuell };

        // =====================================================================
        //  Schema
        // =====================================================================

        [Fact]
        public void Die_Nummer_ist_194_im_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(194, AufheizAufschlagErgebnisSchema.SCHRITT);
            Assert.Equal(StandardlastprofilSchema.SCHRITT + 1, AufheizAufschlagErgebnisSchema.SCHRITT);
            Assert.True(AufheizAufschlagErgebnisSchema.SCHRITT <= SchemaStand.Zielversion);
            Assert.Equal(Paketanhebung.Art.Ddl, Paketanhebung.Stufen.Single(x => x.Nr == AufheizAufschlagErgebnisSchema.SCHRITT).Wirkung);
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 2, AufheizAufschlagErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE);
        }

        [Fact]
        public void Die_Testkopie_fuehrt_beide_Spalten_leer_mit_Pruefklauseln()
        {
            if (!_db.Vorhanden) return;
            Assert.True(AufheizAufschlagErgebnisSchema.Vollstaendig());
            Assert.Equal(AufheizAufschlagErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE,
                         DataRepository.SpaltenVonTabelle(AufheizAufschlagErgebnisSchema.TAB_ERGEBNIS_GEBAEUDE).Count);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_ErgebnisGebaeude\" WHERE \"Aufheiz_Aufschlag_Verwendet_H\" IS NOT NULL OR \"Aufheizzeit_Bemessen_H\" IS NOT NULL")));
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", "Tab_ErgebnisGebaeude")), CultureInfo.InvariantCulture);
            Assert.Contains("\"Aufheiz_Aufschlag_Verwendet_H\" INTEGER CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Aufheizzeit_Bemessen_H\" INTEGER CHECK", ddl, StringComparison.Ordinal);
            // Die Zone erbt die Aufheizzeit; der Bericht liest nur die Gebäudezeile (E99) — an der Zone keine Spalte.
            Assert.False(DataRepository.SpalteVorhanden("Tab_ErgebnisZone", "Aufheizzeit_Bemessen_H"));
            Assert.False(DataRepository.SpalteVorhanden("Tab_ErgebnisZone", "Aufheiz_Aufschlag_Verwendet_H"));
        }

        [Fact]
        public void Die_Pruefklauseln_weisen_Werte_ausserhalb_0_bis_47_ab()
        {
            if (!_db.Vorhanden) return;
            object min = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_ErgebnisGebaeude");
            if (min == null || min is DBNull) return;
            long id = Convert.ToInt64(min, CultureInfo.InvariantCulture);
            foreach (string spalte in new[] { "Aufheiz_Aufschlag_Verwendet_H", "Aufheizzeit_Bemessen_H" })
            {
                Assert.ThrowsAny<Exception>(() => DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_ErgebnisGebaeude SET \"" + spalte + "\" = ? WHERE ID = ?", new DbParam("@w", 48), new DbParam("@i", id)));
                Assert.ThrowsAny<Exception>(() => DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_ErgebnisGebaeude SET \"" + spalte + "\" = ? WHERE ID = ?", new DbParam("@w", -1), new DbParam("@i", id)));
            }
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (var s in AufheizAufschlagErgebnisSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(AufheizAufschlagErgebnisSchema.Vollstaendig());
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE,
                         DataRepository.SpaltenVonTabelle(AufheizAufschlagErgebnisSchema.TAB_ERGEBNIS_GEBAEUDE).Count);
            var bericht = new List<string>();
            Assert.Equal(2, AufheizAufschlagErgebnisSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(AufheizAufschlagErgebnisSchema.Vollstaendig());
            Assert.Equal(0, AufheizAufschlagErgebnisSchema.Ausfuehren(null));
        }

        // =====================================================================
        //  Der Plan
        // =====================================================================

        /// <summary>
        /// <b>Täglich und fest:</b> Der verwendete Aufschlag ist n' − n der längsten Rampe (an D + 1 begrenzt), die
        /// bemessene Aufheizzeit t_auf,max nach der Formel der Festlegung 35 (n' − 1 mit n = t_auf,max + 1, ohne D).
        /// </summary>
        [Theory]
        [InlineData(null, null, false, 0, 3)]
        [InlineData(2, null, false, 2, 5)]
        [InlineData(0, 50.0, false, 2, 5)]
        [InlineData(3, 50.0, true, 3, 6)]
        [InlineData(24, 100.0, false, 5, 27)]
        [InlineData(24, 100.0, true, 5, 27)]
        public void Taeglich_und_fest_tragen_Aufschlag_der_laengsten_Rampe_und_bemessene_Zeit(
            int? h, double? p, bool fest, int aufschlag, int bemessen)
        {
            Aufheizplan plan = Aufheizoptimierung.Planen(Standard(), An(h, p, fest));
            Assert.Equal(3, plan.AufheizzeitMaxH);
            Assert.Equal(aufschlag, plan.AufschlagVerwendetH);
            Assert.Equal(bemessen, plan.AufheizzeitMitAufschlagH);
            Assert.Equal(Math.Min(4 + aufschlag, 9) - 1, plan.LaengsteRampeH);
        }

        /// <summary><b>Rampe mit einer Stufe</b> (P16): kein Aufschlag (0), die bemessene Zeit ist t_auf,max.</summary>
        [Fact]
        public void Rampe_mit_einer_Stufe_hat_Aufschlag_0()
        {
            Aufheizplan plan = Aufheizoptimierung.Planen(Standard(heizMaxW: 1.0e9), An(24, 100.0));
            Assert.All(plan.Spruenge, sp => Assert.Equal(1, sp.N));
            Assert.Equal(0, plan.AufschlagVerwendetH);
            Assert.Equal(plan.AufheizzeitMaxH, plan.AufheizzeitMitAufschlagH);
        }

        /// <summary><b>Manuell</b> (Festlegung 37): kein Aufschlag (NULL), die bemessene Zeit ist der manuelle Wert.</summary>
        [Fact]
        public void Manuell_traegt_keinen_Aufschlag_und_den_manuellen_Wert()
        {
            Aufheizplan plan = Aufheizoptimierung.Planen(Standard(manuell: 8), An(24, 100.0));
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, plan.Art);
            Assert.Null(plan.AufschlagVerwendetH);
            Assert.Equal(8, plan.AufheizzeitMitAufschlagH);
        }

        // =====================================================================
        //  Die Ergebniszeile
        // =====================================================================

        /// <summary>
        /// Ergebnisträger und Kennzahlen: Das Gebäude trägt beide Werte, die Zone keinen (sie erbt die Aufheizzeit, der
        /// Bericht liest die Gebäudezeile); kein Export; ohne Aufheizrechnung keine Zeile.
        /// </summary>
        [Fact]
        public void Ergebnistraeger_und_Kennzahlen_tragen_beide_Werte_am_Gebaeude()
        {
            Aufheizplan plan = Aufheizoptimierung.Planen(Standard(), An(2, null));
            Aufheizergebnis a = Aufheizergebnis.Bilden(plan, new double[8760], new double[8760], 0.0);
            Assert.Equal(2, a.AufheizAufschlagVerwendetH);
            Assert.Equal(5, a.AufheizzeitBemessenH);
            Aufheizkennzahlen g = GebaeudeKennzahlen.Aufheizwerte(a, zone: false);
            Assert.Equal(2, g.AufheizAufschlagVerwendetH);
            Assert.Equal(5, g.AufheizzeitBemessenH);
            Aufheizkennzahlen z = GebaeudeKennzahlen.Aufheizwerte(a, zone: true);
            Assert.Null(z.AufheizAufschlagVerwendetH);
            Assert.Null(z.AufheizzeitBemessenH);
            Assert.DoesNotContain(g.Zahlen(), kv => kv.Key.Contains("Aufschlag", StringComparison.Ordinal) ||
                                                    kv.Key.Contains("Bemessen", StringComparison.Ordinal));
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(null, zone: false));
        }

        /// <summary>
        /// <b>Mehrzonen:</b> Das Gebäude trägt den Aufschlag der Zone mit der längsten Rampe und die größte bemessene Zeit
        /// seiner Zonen.
        /// </summary>
        [Fact]
        public void Mehrzonen_Gebaeude_traegt_Aufschlag_der_laengsten_Rampe()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            Mehrzonenergebnis mit = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                                                          aufheizvorgabe: An(2, 50.0));
            List<Aufheizplan> plaene = mit.Eingaenge.Select(e => e.Aufheizplan)
                                          .Where(p => p != null && p.Bemessung != null && !p.Gekoppelt && !p.Unbeheizt).ToList();
            Assert.NotEmpty(plaene);
            Aufheizgebaeude geb = mit.Aufheizgebaeude;
            int laengste = plaene.Max(p => p.LaengsteRampeH);
            Assert.Equal(laengste, geb.LaengsteRampeH);
            Assert.Equal(plaene.Where(p => p.LaengsteRampeH == laengste).Max(p => p.AufschlagVerwendetH), geb.AufschlagVerwendetH);
            Assert.Equal(plaene.Max(p => p.AufheizzeitMitAufschlagH), geb.AufheizzeitMitAufschlagH);
            _aus.WriteLine("Mehrzonen: längste Rampe {0} h, Aufschlag {1} h, bemessen {2} h", laengste, geb.AufschlagVerwendetH,
                           geb.AufheizzeitMitAufschlagH);
        }

        /// <summary>
        /// <b>Am Lauf (1018):</b> Mit Aufschlag (2 h, 50 %) schreibt der Lauf beide Spalten und liest sie gleich zurück;
        /// mit Schalter aus bleiben beide NULL.
        /// </summary>
        [Fact]
        public void Der_Lauf_schreibt_beide_Spalten_und_Schalter_aus_laesst_sie_NULL()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(PROJEKT, An(2, 50.0)));
            var laeufer = new SimulationRunner();
            Assert.True(laeufer.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            ErgebnisGebaeudeModel zeile = laeufer.simulation_Waermebedarf.GebaeudeKennzahlenListe.Single();
            Aufheizergebnis a = laeufer.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(0).Aufheizung;
            Assert.NotNull(zeile.AufheizzeitBemessenH);
            Assert.NotNull(zeile.AufheizAufschlagVerwendetH);
            Assert.Equal(a.AufheizzeitBemessenH, zeile.AufheizzeitBemessenH);
            Assert.Equal(a.AufheizAufschlagVerwendetH, zeile.AufheizAufschlagVerwendetH);
            Assert.Equal(Aufheizoptimierung.MitAufschlag(zeile.AufheizzeitMaxH.Value + 1, An(2, 50.0)) - 1, zeile.AufheizzeitBemessenH);

            ErgebnisGebaeudeModel geladen = new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single();
            Assert.Equal(zeile.AufheizzeitBemessenH, geladen.AufheizzeitBemessenH);
            Assert.Equal(zeile.AufheizAufschlagVerwendetH, geladen.AufheizAufschlagVerwendetH);
            _aus.WriteLine("1018: t_auf,max {0} h, bemessen {1} h, Aufschlag verwendet {2} h, längste Rampe {3} h",
                           geladen.AufheizzeitMaxH, geladen.AufheizzeitBemessenH, geladen.AufheizAufschlagVerwendetH, geladen.AufheizzeitLaengsteH);

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(PROJEKT, Aufheizvorgabe.Aus));
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out fehler) > 0, fehler);
            ErgebnisGebaeudeModel aus = new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single();
            Assert.Null(aus.AufheizzeitBemessenH);
            Assert.Null(aus.AufheizAufschlagVerwendetH);
        }

        // =====================================================================
        //  Bericht und Vorlagenfelder
        // =====================================================================

        private static ErgebnisGebaeudeModel Bemessen() => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = 7, Gebaeudename = "Gebäude 1", Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
            AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE,
            AufheizArt = DbWerte.AUFHEIZ_ART_TAEGLICH, AufheizzeitMaxH = 6, AufheizAussenC = -12.0,
            AufheizLeistungKw = 60.0, AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL,
            Aufheiztage = 200, AufheizstundenH = 800, AufheizzeitLaengsteH = 8,
            AufheizAufschlagVerwendetH = 2, AufheizzeitBemessenH = 8,
        };

        private static Dictionary<string, string> Paare(ErgebnisGebaeudeModel g, (double? H, double? Prozent)? aufschlag)
            => Berichtstabellen.Gebaeudeergebnis(g, false, DE, aufschlag).Zeilen.ToDictionary(z => z.Zellen[0].Text, z => z.Zellen[1].Text);

        private static string T(string ressource) => R.ResourceManager.GetString(ressource, DE);

        [Fact]
        public void Der_Bericht_liest_beide_Werte_aus_der_Ergebniszeile()
        {
            Dictionary<string, string> p = Paare(Bemessen(), (2.0, 10.0));
            Assert.Equal("2 h", p[T(nameof(R.BV_AUFH_A3_AUFSCHLAG_VERWENDET))]);
            Assert.Equal("8 h", p[T(nameof(R.BV_AUFH_A3_ZEIT_BEMESSEN))]);
            Assert.False(p.ContainsKey(T(nameof(R.BV_AUFH_AUFSCHLAG))));

            // Rampe mit einer Stufe: Aufschlag 0 h steht als Wert da.
            ErgebnisGebaeudeModel eineStufe = Bemessen();
            eineStufe.AufheizAufschlagVerwendetH = 0;
            Assert.Equal("0 h", Paare(eineStufe, (2.0, 10.0))[T(nameof(R.BV_AUFH_A3_AUFSCHLAG_VERWENDET))]);
        }

        [Fact]
        public void Eine_aeltere_Ergebniszeile_nennt_die_Projekteinstellung_benannt()
        {
            ErgebnisGebaeudeModel alt = Bemessen();
            alt.AufheizAufschlagVerwendetH = null;
            alt.AufheizzeitBemessenH = null;
            Dictionary<string, string> p = Paare(alt, (2.0, 10.0));
            Assert.False(p.ContainsKey(T(nameof(R.BV_AUFH_A3_AUFSCHLAG_VERWENDET))));
            Assert.Equal(Tabellenzelle.STRICH, p[T(nameof(R.BV_AUFH_A3_ZEIT_BEMESSEN))]);
            Assert.Equal(string.Format(DE, T(nameof(R.BV_AUFH_A3_AUFSCHLAG_ALT_WERT)), "2 h / 10 % (es gilt der größere Wert)"),
                         p[T(nameof(R.BV_AUFH_AUFSCHLAG))]);
        }

        [Fact]
        public void Manuell_zeigt_weder_Aufschlag_noch_bemessene_Zeit_doppelt()
        {
            ErgebnisGebaeudeModel g = Bemessen();
            g.AufheizArt = DbWerte.AUFHEIZ_ART_MANUELL; g.AufheizzeitMaxH = 8; g.AufheizAufschlagVerwendetH = null; g.AufheizzeitBemessenH = 8;
            Dictionary<string, string> p = Paare(g, (2.0, 10.0));
            Assert.Equal("8 h", p["Aufheizzeit manuell"]);
            Assert.False(p.ContainsKey(T(nameof(R.BV_AUFH_A3_AUFSCHLAG_VERWENDET))));
            Assert.False(p.ContainsKey(T(nameof(R.BV_AUFH_A3_ZEIT_BEMESSEN))));
            Assert.False(p.ContainsKey(T(nameof(R.BV_AUFH_AUFSCHLAG))));
        }

        [Fact]
        public void Die_Vorlagenfelder_lesen_beide_Werte_seit_Fassung_16()
        {
            Assert.Equal(16, Vorlagenfeldkatalog.KATALOGFASSUNG);
            foreach (string s in new[] { "gebaeude.ergebnis.aufschlag_verwendet", "gebaeude.ergebnis.aufheizzeit_bemessen" })
                Assert.Equal(16, Vorlagenfeldkatalog.Finde(s).Seit);

            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(1);
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            w.Stamm.Ergebnis.Gebaeude.Add(Bemessen());
            var tafel = new DataTable();
            tafel.Columns.Add("ID", typeof(int));
            tafel.Rows.Add(7);
            Berichtswerte g7 = w.MitGebaeude(tafel.Rows[0]);
            Assert.Equal(2.0, Loese("gebaeude.ergebnis.aufschlag_verwendet", g7).Zahl);
            Assert.Equal(8.0, Loese("gebaeude.ergebnis.aufheizzeit_bemessen", g7).Zahl);
        }

        private static Platzhalterwert Loese(string marke, Berichtswerte werte)
        {
            Platzhalterwert w = Vorlagenfeldkatalog.Loese(Platzhaltersyntax.Lies("{{" + marke + "}}"), werte);
            Assert.True(w != null, marke + " ist kein Katalogschlüssel");
            return w;
        }
    }
}
