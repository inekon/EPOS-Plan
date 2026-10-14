using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorauswahl des Energieträgers</b> eines Erzeugers ohne Träger (Anwenderwunsch
    /// 08.10.2026, <see cref="EnergietraegerZulaessigkeit.Vorauswahl(string, int, int)"/>): je
    /// Anlagenart gemessen an der Testdatenbank, dazu die Regel ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerVorauswahlTests
    {
        /// <summary>Gasheizkessel des Projekts 1030 (Brennstoff 3 = Erdgas E).</summary>
        private const int KESSEL_GAS = 1018330;

        /// <summary>Elektrokessel des Projekts 1017 (Brennstoff 13).</summary>
        private const int KESSEL_STROM = 1017237;

        /// <summary>BHKW mit Brennstoff 8 = Heizöl L.</summary>
        private const int BHKW_OEL = 1018146;

        /// <summary>„Elektrische Energie" — der Auslieferungs-Stromträger des Katalogs.</summary>
        private const int TRAEGER_STROM = 60;

        /// <summary>„Erdgas E" (ID_Brennstoff 3).</summary>
        private const int TRAEGER_ERDGAS_E = 63;

        /// <summary>„Heizöl L" (ID_Brennstoff 8, der erste seiner Familie in Katalogreihenfolge).</summary>
        private const int TRAEGER_HEIZOEL_L = 62;

        [Theory]
        [InlineData(DbWerte.ERZEUGER_WAERMEPUMPE)]
        [InlineData(DbWerte.ERZEUGER_PHOTOVOLTAIK)]
        [InlineData(DbWerte.ERZEUGER_STROMSPEICHER)]
        [InlineData(EnergietraegerZulaessigkeit.ERZEUGER_HEIZSTAB)]
        public void Die_elektrische_Welt_bekommt_den_Standard_Stromtraeger(string erzeugerart)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int v = EnergietraegerZulaessigkeit.Vorauswahl(erzeugerart, 0, 0);

            Assert.Equal(TRAEGER_STROM, v);
            Assert.Equal(ProjektEnergietraegerCtrl.StandardStromTraeger(0), v);
            Assert.True(EnergietraegerZulaessigkeit.IstZulaessig(erzeugerart, v));
        }

        [Fact]
        public void Ein_Gaskessel_bekommt_den_Traeger_seines_Brennstoffs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(TRAEGER_ERDGAS_E,
                EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_GAS, 0));
        }

        [Fact]
        public void Ein_Elektrokessel_bekommt_den_Standard_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(TRAEGER_STROM,
                EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_STROM, 0));
        }

        [Fact]
        public void Ein_Kessel_ohne_Geraet_bleibt_ohne_Vorauswahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_HEIZKESSEL, 0, 0));
        }

        [Fact]
        public void Ein_Oel_BHKW_bekommt_den_Traeger_seines_Brennstoffs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(TRAEGER_HEIZOEL_L,
                EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_BHKW, BHKW_OEL, 0));
        }

        [Fact]
        public void Ein_BHKW_ohne_Geraet_bekommt_den_ersten_Gastraeger_des_Katalogs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int v = EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_BHKW, 0, 0);

            List<EnergyCarrier> katalog = EnergietraegerZulaessigkeit.ZulaessigerKatalog(DbWerte.ERZEUGER_BHKW);
            Assert.Equal(katalog[0].ID, v);
            Assert.Equal(katalog.First(c => c.ID == TRAEGER_ERDGAS_E).GroupCode, katalog[0].GroupCode);
        }

        [Theory]
        [InlineData(DbWerte.ERZEUGER_SOLARTHERMIE)]
        [InlineData(DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER)]
        [InlineData("")]
        public void Ohne_Traeger_oder_ohne_Einengung_keine_Vorauswahl(string erzeugerart)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, EnergietraegerZulaessigkeit.Vorauswahl(erzeugerart, 0, 0));
        }

        // =================================================================
        // Die Regel ohne Datenbank
        // =================================================================

        private static EnergyCarrier T(int id, string gruppe, int brennstoff = 0)
            => new EnergyCarrier { ID = id, GroupCode = gruppe, Name = "T" + id, ID_Brennstoff = brennstoff };

        [Fact]
        public void Die_Rangfolge_Standard_vor_Geraetebrennstoff_vor_Katalogreihenfolge()
        {
            var katalog = new List<EnergyCarrier> { T(1, "Gas", 14), T(2, "Gas", 3), T(3, "Strom", 13), T(4, "Strom", 13) };
            var gas = new[] { "Gas" };
            var strom = new[] { "Strom" };

            Assert.Equal(4, EnergietraegerZulaessigkeit.Vorauswahl(strom, katalog, 4, 0));
            Assert.Equal(2, EnergietraegerZulaessigkeit.Vorauswahl(gas, katalog, 0, 3));
            Assert.Equal(1, EnergietraegerZulaessigkeit.Vorauswahl(gas, katalog, 0, 0));
            // Ein Standard außerhalb der zulässigen Gruppen zählt nicht.
            Assert.Equal(1, EnergietraegerZulaessigkeit.Vorauswahl(gas, katalog, 4, 0));
            // Keine Einengung oder leere Gruppenliste: keine Vorauswahl.
            Assert.Equal(0, EnergietraegerZulaessigkeit.Vorauswahl(null, katalog, 4, 0));
            Assert.Equal(0, EnergietraegerZulaessigkeit.Vorauswahl(Array.Empty<string>(), katalog, 4, 0));
            Assert.Equal(0, EnergietraegerZulaessigkeit.Vorauswahl(new[] { "Holz" }, katalog, 0, 0));
        }
    }
}
