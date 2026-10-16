using System.Collections.Generic;
using System.Globalization;
using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BP-3: das einheitliche Zahlformat der Jahreswerte in MWh</b> (Anwenderentscheid
    /// 16.10.2026). Jahressumme und Jahresarbeit in MWh stehen in Projektliste, Katalogliste,
    /// Summe im Listenkopf und Satzzeile als ganze Zahl mit Tausendertrennung der Kultur
    /// (<c>N0</c>): de „1.200“, en-US „1,200“. kWh behält sein Format, gerechnet und gefiltert
    /// wird mit der ungerundeten Zahl.
    /// </summary>
    [Collection("Testdatenbank")]
    public class JahreswertFormatTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public JahreswertFormatTests(TestDatenbank db)
        {
            _db = db;
        }

        [Theory]
        [InlineData("de-DE", 1200.4, "1.200")]
        [InlineData("de-DE", 40.0, "40")]
        [InlineData("de-DE", 0.4, "0")]
        [InlineData("de-DE", 0.6, "1")]
        [InlineData("en-US", 1200.4, "1,200")]
        [InlineData("en-US", 12345.6, "12,346")]
        public void Ein_Jahreswert_in_MWh_steht_ganzzahlig_mit_Tausendertrennung(string kultur, double wert, string erwartet)
        {
            using var k = new Kulturvorrichtung(kultur);
            Assert.Equal(erwartet, Energieeinheit.MWh.FormatiereJahreswert(wert));
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Ein_Jahreswert_in_kWh_behaelt_sein_Format(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);
            Assert.Equal(Energieeinheit.KWh.Formatiere(1200400.4), Energieeinheit.KWh.FormatiereJahreswert(1200400.4));
            Assert.Equal("1200400", Energieeinheit.KWh.FormatiereJahreswert(1200400.4));
        }

        [Fact]
        public void Das_Format_der_Einheit_selbst_bleibt_fuer_die_Eingabefelder_genau()
        {
            using var k = new Kulturvorrichtung("de-DE");
            Assert.Equal("1200,40", Energieeinheit.MWh.Formatiere(1200.4));
        }

        [Theory]
        [InlineData("de-DE", "1.200")]
        [InlineData("en-US", "1,200")]
        public void Die_Katalogzelle_eines_Jahreswerts_zeigt_N0_und_traegt_die_genaue_Zahl(string kultur, string erwartet)
        {
            using var k = new Kulturvorrichtung(kultur);
            Katalogfilterzeile z = new Katalogfilterzeile(1, "A")
                .MitZahl(Katalogfilterprofil.SpJahressummeMwh, 1200.25,
                         Energieeinheit.JAHRESWERT_MWH_NACHKOMMASTELLEN);
            Assert.Equal(erwartet, z.Text(Katalogfilterprofil.SpJahressummeMwh));
            Assert.Equal(1200.25, z.Zahl(Katalogfilterprofil.SpJahressummeMwh));
        }

        /// <summary>Der Bedarfskatalog (Kälte, Brauchwasser, Prozesswärme, Strom) zeigt die Jahressumme als N0.</summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Der_Bedarfskatalog_zeigt_die_Jahressumme_ganzzahlig(string kultur)
        {
            if (!_db.Vorhanden) return;
            using var k = new Kulturvorrichtung(kultur);
            int gesehen = 0;
            foreach (BedarfsArt art in new[] { BedarfsArt.Brauchwasser, BedarfsArt.Prozesswaerme,
                                               BedarfsArt.Stromverbraucher, BedarfsArt.Kaelte })
                foreach (Katalogfilterzeile z in BedarfStammCtrl.Katalogfilterzeilen(art))
                {
                    double? zahl = z.Zahl(Katalogfilterprofil.SpJahressummeMwh);
                    Assert.NotNull(zahl);
                    Assert.Equal(zahl.Value.ToString("N0", CultureInfo.CurrentCulture),
                                 z.Text(Katalogfilterprofil.SpJahressummeMwh));
                    gesehen++;
                }
            Assert.True(gesehen > 0);
        }

        /// <summary>Der Ganglinienkatalog (Wärme- und Kältebedarf extern, Strom, Solar, PV) zeigt die Jahresarbeit als N0.</summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Der_Ganglinienkatalog_zeigt_die_Jahresarbeit_ganzzahlig(string kultur)
        {
            if (!_db.Vorhanden) return;
            using var k = new Kulturvorrichtung(kultur);
            int gesehen = 0;
            foreach (Zeitreihenart art in new[] { Zeitreihenart.Waermebedarf, Zeitreihenart.Stromganglinie,
                                                  Zeitreihenart.Solarganglinie, Zeitreihenart.PvGanglinie })
                foreach (Katalogfilterzeile z in ZeitreihenKatalogCtrl.Katalogfilterzeilen(art))
                {
                    if (z.Zahl(Katalogfilterprofil.SpJahresarbeitMwh) is not double zahl) continue;
                    Assert.Equal(zahl.ToString("N0", CultureInfo.CurrentCulture),
                                 z.Text(Katalogfilterprofil.SpJahresarbeitMwh));
                    gesehen++;
                }
            Assert.True(gesehen > 0);
        }
    }
}
