using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Der Träger der Kategorie ANIMAL_FAT (Gruppe „Tierische Fette") — ein
        /// BHKW-Brennstoff mit EIGENEM Kategoriecode, nicht Teil der flüssigen.
        /// </summary>
        private const int TRAEGER_TIERFETT = 69;

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
        public void Ein_BHKW_ohne_Geraet_bekommt_gasfoermig_fluessig_und_tierische_Fette()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen =
                EnergietraegerZulaessigkeit.ZulaessigeGruppen(DbWerte.ERZEUGER_BHKW);

            Assert.NotNull(gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_ERDGAS), gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_HEIZOEL), gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_TIERFETT), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_STROM), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_FERNWAERME), gruppen);
        }

        /// <summary>
        /// „Tierische Fette" führt eine EIGENE Gruppe und einen eigenen Kategoriecode —
        /// der Träger fiele deshalb durch, wenn nur gasförmig und flüssig zugelassen
        /// wären. Die Gegenprobe steht im selben Fall: Der Kessel OHNE Gerät nimmt ihn
        /// ohnehin (keine Einengung), ein Öl-BHKW dagegen nicht.
        /// </summary>
        [Fact]
        public void Tierische_Fette_sind_am_BHKW_ohne_Geraet_zulaessig()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Träger trägt wirklich den eigenen Code - sonst prüfte der Fall nichts.
            Assert.Equal(EnergietraegerZulaessigkeit.CODE_TIERFETT,
                         PricingModelDesTraegers(TRAEGER_TIERFETT));

            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_BHKW, TRAEGER_TIERFETT));

            // Mit Gerät zählt allein die Kategorie des Geräts.
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_BHKW, BHKW_OEL, TRAEGER_TIERFETT));

            // Die elektrische Welt bleibt unberührt.
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(
                DbWerte.ERZEUGER_WAERMEPUMPE, TRAEGER_TIERFETT));
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
        // ET-E-3: die Vereinigung ueber ALLE Anlagen des Projekts
        // =================================================================

        /// <summary>Projekt der Testdatenbank ohne jede Anlagenzeile.</summary>
        private const int PROJEKT_OHNE_ANLAGEN = 19;

        /// <summary>Projekt 1024: Elektrokessel, Öl-BHKW, Wärmepumpe mit Heizstab, zwei Puffer.</summary>
        private const int PROJEKT_STROM_UND_OEL = 1024;

        /// <summary>Projekt 1009: drei Wärmepumpen und zwei Gaskessel — jede Gruppe einmal.</summary>
        private const int PROJEKT_ZWEI_WAERMEPUMPEN = 1009;

        /// <summary>Ein Gaskessel des Projekts 1009 (Brennstoff 1 = Stadtgas, Kategorie 1 = Gas).</summary>
        private const int KESSEL_DES_PROJEKTS_1009 = 1009230;

        /// <summary>Projekt 1027: Gaskessel 1018324 und eine Wärmepumpe.</summary>
        private const int PROJEKT_KESSEL_UND_WP = 1027;

        /// <summary>Der Gaskessel des Projekts 1027.</summary>
        private const int KESSEL_DES_PROJEKTS_1027 = 1018324;

        [Fact]
        public void Ein_Projekt_ohne_Anlagen_engt_nicht_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT_OHNE_ANLAGEN));
            // Ohne Projektkontext ebenso.
            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(0));
        }

        [Fact]
        public void Waermepumpe_und_Oelbrenner_vereinigen_Strom_und_Oel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen =
                EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT_STROM_UND_OEL);

            Assert.NotNull(gruppen);
            // Die Gruppennamen stehen in der Datenbank, nicht in diesem Test.
            Assert.Contains(GruppeDesTraegers(TRAEGER_STROM), gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_HEIZOEL), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_ERDGAS), gruppen);
            Assert.DoesNotContain(GruppeDesTraegers(TRAEGER_FERNWAERME), gruppen);
            Assert.Equal(2, gruppen.Count);
        }

        [Fact]
        public void Mehrere_gleichartige_Anlagen_nennen_ihre_Gruppe_nur_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit
                .ZulaessigeGruppenFuerProjekt(PROJEKT_ZWEI_WAERMEPUMPEN);

            Assert.NotNull(gruppen);

            // Drei Wärmepumpen und zwei Gaskessel — die Vereinigung ist genau die der
            // beiden Einzelfälle, jede Gruppe genau einmal.
            var erwartet = new List<string>(
                EnergietraegerZulaessigkeit.ZulaessigeGruppen(DbWerte.ERZEUGER_WAERMEPUMPE));
            foreach (string g in EnergietraegerZulaessigkeit.ZulaessigeGruppen(
                         DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_DES_PROJEKTS_1009))
                if (!erwartet.Contains(g)) erwartet.Add(g);
            erwartet.Sort(StringComparer.CurrentCultureIgnoreCase);

            Assert.Equal(erwartet, new List<string>(gruppen));
            Assert.Contains(GruppeDesTraegers(TRAEGER_STROM), gruppen);
            Assert.Contains(GruppeDesTraegers(TRAEGER_ERDGAS), gruppen);
            Assert.Equal(gruppen.Count, new List<string>(gruppen).Distinct().Count());
        }

        [Fact]
        public void Ein_Kessel_ohne_auswertbares_Geraet_oeffnet_das_ganze_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Ausgangslage: Gaskessel und Wärmepumpe engen auf zwei Gruppen ein.
            IReadOnlyList<string> vorher =
                EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT_KESSEL_UND_WP);
            Assert.NotNull(vorher);
            Assert.Contains(GruppeDesTraegers(TRAEGER_STROM), vorher);
            Assert.Contains(GruppeDesTraegers(TRAEGER_ERDGAS), vorher);

            // Ein Kessel ohne Brennstoff ist ein Kessel ohne Aussage — er darf alles
            // verbrennen, und damit ist das ganze Projekt offen.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Heizkessel SET Brennstoff = 0 WHERE ID = ?",
                new DbParam("@id", KESSEL_DES_PROJEKTS_1027));

            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT_KESSEL_UND_WP));
        }

        [Fact]
        public void Ein_Projekt_nur_mit_Solarthermie_engt_nicht_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Solarkollektoren");
            Assert.True(o != null && o != DBNull.Value);
            int kollektor = Convert.ToInt32(o);

            // Der Stand wird in der ARBEITSKOPIE aufgebaut: die Testdatenbank führt kein
            // Projekt, das allein Solarthermie betreibt.
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Solar) VALUES (?, ?, ?)",
                new DbParam("@p", PROJEKT_OHNE_ANLAGEN),
                new DbParam("@b", "Solarthermie (Prüfstand)"),
                new DbParam("@s", kollektor));

            // Solarthermie bezieht keine Energie — sie trägt nichts bei, und ein Projekt
            // ohne Anlage MIT Träger wird nicht eingeengt.
            Assert.Null(EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT_OHNE_ANLAGEN));
        }

        // =================================================================
        // Hilfe
        // =================================================================

        /// <summary>Der Gruppenname eines Trägers — aus der Datenbank, nicht aus dem Test.</summary>
        /// <summary><c>energy_carrier.pricing_model</c> — der KATEGORIECODE des Trägers.</summary>
        private static string PricingModelDesTraegers(int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT pricing_model FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        private static string GruppeDesTraegers(int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT group_code FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }
    }
}
