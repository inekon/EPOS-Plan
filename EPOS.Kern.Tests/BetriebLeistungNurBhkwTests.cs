using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>29c (Anwenderentscheid 16.10.2026): Betriebskosten je kW und Jahr nur beim
    /// BHKW, und dort je kW ELEKTRISCH.</b> Im Betriebsraster führt die Wärmepumpe keine
    /// Leistungsart, das BHKW allein „je kW elektrisch" auf <c>Pel</c>. Die
    /// Investitionsseite bleibt unverändert; eine Bestandszeile bleibt wählbar und
    /// rechnet weiter.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebLeistungNurBhkwTests : IDisposable
    {
        private const int K_WAERMEPUMPE = 1;
        private const int K_BHKW = 7;

        private const int PROJEKT = 293001;
        private const int STAMM_ID = 293900;
        private const int A_BHKW = 293101;
        private const int G_BHKW = 293201;
        private const int P_WARTUNG = 293000001;

        private const double BHKW_PEL = 50.0;      // kW el
        private const double BHKW_PTHERM = 100.0;  // kW th
        private const double SATZ = 12.0;          // €/kW·a

        private static readonly string[] LEISTUNGSARTEN =
        {
            DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
            DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
            DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
        };

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() { _kultur.Dispose(); }

        // =====================================================================
        //  Die Auswahl
        // =====================================================================

        [Fact]
        public void Die_Waermepumpe_bietet_im_Betriebsraster_keine_Leistungsart()
        {
            List<string> betrieb = Werte(K_WAERMEPUMPE, false, null);
            foreach (string art in LEISTUNGSARTEN)
            {
                Assert.DoesNotContain(art, betrieb);
                Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                             WirtschaftlichkeitCtrl.BasisGrund(art, K_WAERMEPUMPE, false, false));
            }

            // Investitionsseite unberührt: alle drei, „je kW elektrisch" nach E20.
            List<string> invest = Werte(K_WAERMEPUMPE, true, null);
            foreach (string art in LEISTUNGSARTEN) Assert.Contains(art, invest);
        }

        [Fact]
        public void Das_Bhkw_bietet_im_Betriebsraster_allein_je_kW_elektrisch()
        {
            List<string> betrieb = Werte(K_BHKW, false, null);
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, betrieb);
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, betrieb);
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, betrieb);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET,
                         WirtschaftlichkeitCtrl.BasisGrund(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                                                           K_BHKW, false, false));

            List<string> invest = Werte(K_BHKW, true, null);
            foreach (string art in LEISTUNGSARTEN) Assert.Contains(art, invest);
        }

        [Fact]
        public void Eine_Bestandszeile_bleibt_in_der_Liste()
        {
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                Werte(K_WAERMEPUMPE, false, new[] { DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG }));
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                Werte(K_BHKW, false, new[] { DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG }));
        }

        [Fact]
        public void Name_und_Jahreseinheit_in_beiden_Sprachen()
        {
            Assert.Equal("je kW elektrisch",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, K_BHKW));
            Assert.Equal("€/kW·a",
                BemessungKatalog.Einheit(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, K_BHKW, true));

            using var en = new Kulturvorrichtung("en-US");
            Assert.Equal("per kW electric",
                BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, K_BHKW));
        }

        // =====================================================================
        //  Die Rechnung: Satz × Pel
        // =====================================================================

        /// <summary>
        /// Handrechnung: Wartung 12,00 €/kW·a × 50 kW el = 600,00 €/a — die elektrische
        /// Leistung, nicht die thermische (100 kW → 1.200,00 €/a) und nicht die Summe.
        /// </summary>
        [Fact]
        public void Die_Betriebszeile_rechnet_Satz_mal_elektrische_Leistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            Assert.Equal(BHKW_PEL, TechnikPlanwertCtrl.BaugroesseSumme(
                PROJEKT, K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_BHKW, false).Value, 6);

            KostenPositionNachweis zeile = null;
            foreach (KostenPositionNachweis n in
                     WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                         PROJEKT, WirtschaftlichkeitSzenario.ERWARTET))
                if (n.Id == P_WARTUNG) zeile = n;

            Assert.NotNull(zeile);
            Assert.Equal(BHKW_PEL, zeile.Menge.Value, 6);
            Assert.Equal(600.0, zeile.BetragJahr, 6);
        }

        // =====================================================================
        //  Aufbau
        // =====================================================================

        private static List<string> Werte(int komponente, bool invest, ICollection<string> benutzt)
        {
            var werte = new List<string>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Auswahl(komponente, invest, benutzt))
                werte.Add(i.Persistenz);
            return werte;
        }

        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void BeispielAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT + ", 'Bemessung 29c')");
            Sql("INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (" + STAMM_ID + ", 'Wartung', 0)");
            Sql("INSERT INTO Tab_BHKW (ID, ID_Projekt, Bezeichner, Pel, Ptherm) VALUES (" +
                G_BHKW + ", " + PROJEKT + ", 'BHKW 50/100', " + Z(BHKW_PEL) + ", " + Z(BHKW_PTHERM) + ")");

            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = A_BHKW + ", " + PROJEKT + ", 'BHKW', " + WizardItemClass.BHKW_TYP;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (s == "ID_BHKW" ? G_BHKW.ToString() : "NULL");
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");

            Sql("INSERT INTO Tab_ProjektWerte " +
                "(ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                " Nutzungsdauer, Kostenart, Bemessung, Einheitpreis, ID_Anlage) VALUES (" +
                P_WARTUNG + ", " + PROJEKT + ", " + STAMM_ID + ", " + K_BHKW + ", " +
                DbWerte.KOSTEN_KATEGORIE_BETRIEB + ", 0.0, 1, '" +
                DbWerte.KOSTENART_BETRIEBSGEBUNDEN + "', '" + DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH +
                "', " + Z(SATZ) + ", " + A_BHKW + ")");
        }

        private static string Z(double wert) { return wert.ToString(CultureInfo.InvariantCulture); }

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
