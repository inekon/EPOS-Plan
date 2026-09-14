using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die ZULÄSSIGKEIT je Komponente (Anwenderwunsch 14.09.2026: „nur die zugelassenen
    /// Energieträger für die ausgewählte Komponente"). Gemessen wird an der
    /// Testdatenbank, nicht an geratenen Gruppennamen: Die Gruppen kommen aus
    /// <c>energy_carrier.group_code</c>, die Kategorien aus
    /// <c>Tab_BrennstoffKategorien</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerZulaessigkeitTests
    {
        /// <summary>Gasheizkessel des Projekts 1030 (Brennstoff 3 = Erdgas E, Kategorie 1 = Gas).</summary>
        private const int KESSEL_GAS = 1018330;

        /// <summary>Elektrokessel „eloBLOCK VE 28" des Projekts 1017 (Brennstoff 13).</summary>
        private const int KESSEL_STROM = 1017237;

        /// <summary>BHKW „A-Tron_21_F" (Brennstoff 8 = Heizöl L, Kategorie 2 = Öl).</summary>
        private const int BHKW_OEL = 1018146;

        /// <summary>„Elektrische Energie" (Gruppe Strom).</summary>
        private const int TRAEGER_STROM = 60;

        /// <summary>„Erdgas E" (Gruppe Gas).</summary>
        private const int TRAEGER_ERDGAS = 63;

        /// <summary>„Heizöl EL" (Gruppe Öl).</summary>
        private const int TRAEGER_HEIZOEL = 56;

        /// <summary>„Fernwärme" (Gruppe Fernwärme).</summary>
        private const int TRAEGER_FERNWAERME = 51;

        private static string Text(IReadOnlyList<string> gruppen)
        {
            return gruppen == null ? "<alle>" : string.Join("|", gruppen);
        }

        // =================================================================
        // Die elektrische Welt
        // =================================================================

        [Theory]
        [InlineData(DbWerte.ERZEUGER_WAERMEPUMPE)]
        [InlineData(DbWerte.ERZEUGER_PHOTOVOLTAIK)]
        [InlineData(DbWerte.ERZEUGER_STROMSPEICHER)]
        [InlineData(EnergietraegerZulaessigkeit.ERZEUGER_HEIZSTAB)]
        public void Die_elektrische_Welt_bekommt_genau_die_Stromgruppe(string erzeugerart)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(erzeugerart);

            Assert.NotNull(gruppen);
            Assert.Single(gruppen);
            // Der Gruppenname steht in der Datenbank, nicht in diesem Test.
            Assert.Equal(GruppeDesTraegers(TRAEGER_STROM), gruppen[0]);

            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig(erzeugerart, TRAEGER_STROM));
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(erzeugerart, TRAEGER_ERDGAS));
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(erzeugerart, TRAEGER_HEIZOEL));
        }

        [Fact]
        public void Der_zulaessige_Katalog_einer_Waermepumpe_fuehrt_nur_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string strom = GruppeDesTraegers(TRAEGER_STROM);
            List<EnergyCarrier> katalog =
                EnergietraegerZulaessigkeit.ZulaessigerKatalog(DbWerte.ERZEUGER_WAERMEPUMPE);

            Assert.NotEmpty(katalog);
            foreach (EnergyCarrier c in katalog)
                Assert.Equal(strom, c.GroupCode);
            Assert.Contains(katalog, c => c.ID == TRAEGER_STROM);
        }

        // =================================================================
        // Heizkessel: die Brennstoffkategorie des Geraets
        // =================================================================

        [Fact]
        public void Ein_Gaskessel_bekommt_die_Gasgruppen_und_keinen_Strom()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(
                DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_GAS);

            Assert.NotNull(gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_ERDGAS), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_STROM), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_HEIZOEL), gruppen);

            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_GAS, TRAEGER_ERDGAS));
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_GAS, TRAEGER_STROM));
        }

        [Fact]
        public void Ein_Elektrokessel_bekommt_die_Stromgruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(
                DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_STROM);

            Assert.NotNull(gruppen);
            Assert.Equal(new[] { GruppeDesTraegers(TRAEGER_STROM) }, gruppen);
        }

        [Fact]
        public void Ein_Heizkessel_ohne_Geraet_bleibt_ohne_Einengung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppen(DbWerte.ERZEUGER_HEIZKESSEL));
            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_HEIZKESSEL, TRAEGER_FERNWAERME));
        }

        // =================================================================
        // BHKW
        // =================================================================

        [Fact]
        public void Ein_BHKW_ohne_Geraet_bekommt_gasfoermig_und_fluessig()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen =
                EnergietraegerZulaessigkeit.ZulaessigeGruppen(DbWerte.ERZEUGER_BHKW);

            Assert.NotNull(gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_ERDGAS), gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_HEIZOEL), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_STROM), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_FERNWAERME), gruppen);
        }

        [Fact]
        public void Ein_Oel_BHKW_bekommt_nur_die_Oelgruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(
                DbWerte.ERZEUGER_BHKW, BHKW_OEL);

            Assert.NotNull(gruppen);
            Assert.Equal(new[] { GruppeDesTraegers(TRAEGER_HEIZOEL) }, gruppen);
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_BHKW, BHKW_OEL, TRAEGER_ERDGAS));
        }

        // =================================================================
        // Solarthermie und Pufferspeicher - kein Traeger
        // =================================================================

        [Theory]
        [InlineData(DbWerte.ERZEUGER_SOLARTHERMIE)]
        [InlineData(DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER)]
        public void Solarthermie_und_Pufferspeicher_tragen_keinen_Energietraeger(string erzeugerart)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.False(EnergietraegerZulaessigkeit.MitTraeger(erzeugerart));

            IReadOnlyList<string> gruppen =
                EnergietraegerZulaessigkeit.ZulaessigeGruppen(erzeugerart);
            Assert.NotNull(gruppen);
            Assert.Empty(gruppen);

            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(erzeugerart, TRAEGER_STROM));
            Assert.Empty(EnergietraegerZulaessigkeit.ZulaessigerKatalog(erzeugerart));
        }

        // =================================================================
        // Ohne Komponentenkontext
        // =================================================================

        [Fact]
        public void Ohne_Erzeugerart_gilt_keine_Einengung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppen(null));
            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppen(""));
            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig("", TRAEGER_ERDGAS));
            Assert.True(EnergietraegerZulaessigkeit.MitTraeger(""));

            // Der ungefilterte Katalog ist derselbe, den die Verwaltung listet.
            Assert.Equal(KostenSummenCtrl.GetAllCarriers(0).Count,
                         EnergietraegerZulaessigkeit.ZulaessigerKatalog(null).Count);
        }

        [Fact]
        public void Eine_leere_Gruppenliste_laesst_nichts_durch_eine_fehlende_alles()
        {
            Assert.True(EnergietraegerZulaessigkeit.PasstGruppe(null, "Gas"));
            Assert.False(EnergietraegerZulaessigkeit.PasstGruppe(new string[0], "Gas"));
            Assert.True(EnergietraegerZulaessigkeit.PasstGruppe(new[] { "Gas" }, "gas"));
            Assert.False(EnergietraegerZulaessigkeit.PasstGruppe(new[] { "Gas" }, ""));
        }

        // =================================================================
        // Hilfe
        // =================================================================

        /// <summary>Der Gruppenname eines Trägers — aus der Datenbank, nicht aus dem Test.</summary>
        private static string GruppeDesTraegers(int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT group_code FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }
    }
}
