using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// PV-Katalog-Koeffizienten (Merge 5, aus Form_CECImport nachgezogen): Ein PAN-Modul
    /// bringt alpha_Isc und beta_Voc in A/K bzw. V/K mit (PVsyst fuehrt sie in mA/GradC und
    /// mV/GradC), und die Zelltechnologie kommt aus der Quelle. Fuehrt die .pan einen
    /// Koeffizienten nicht (0), bleibt er NULL - die Anzeige zeigt den Strich.
    /// </summary>
    public class PvKoeffizientenTests
    {
        private static UnifiedModule Pan(double muIsc, double muVoc, string technol)
        {
            var pan = new PanModule
            {
                Manufacturer = "Trina Solar",
                Model = "TSM-650DEG21C.20",
                Technol = technol,
                PNom = 650,
                Isc = 18.35,
                Voc = 45.5,
                Imp = 17.27,
                Vmp = 37.7,
                muPmpReq = -0.34,
                muISC = muIsc,
                muVocSpec = muVoc,
                Width = 1.303,
                Height = 2.384,
                YearBegin = 2020
            };
            var svc = new PanDataService();
            svc.Aufnehmen(pan);
            return UnifiedModule.FromPanCec(svc.AllModules[0]);
        }

        [Fact]
        public void Pan_Koeffizienten_kommen_in_A_je_K_und_V_je_K_ins_Modell()
        {
            UnifiedModule m = Pan(muIsc: 4.5, muVoc: -120.0, technol: "mtSiMono");

            Assert.Equal(0.0045, m.AlphaSc ?? 0, 9);
            Assert.Equal(-0.12, m.BetaOc ?? 0, 9);

            PhotovoltaikModel modell = m.NachModell();
            Assert.Equal(0.0045, modell.m_alpha_SC, 9);
            Assert.Equal(-0.12, modell.m_beta_OC, 9);
            Assert.Equal(DbWerte.PV_TECHNOLOGIE_C_SI, modell.m_Technologie);
        }

        [Fact]
        public void Ohne_Koeffizienten_in_der_pan_bleibt_es_beim_Strich()
        {
            UnifiedModule m = Pan(muIsc: 0.0, muVoc: 0.0, technol: "");

            Assert.Null(m.AlphaSc);
            Assert.Null(m.BetaOc);
            Assert.Null(m.TNoct);
            Assert.Null(m.NachModell().m_Technologie);
        }

        [Fact]
        public void Die_Technologie_wird_aus_dem_PAN_Kuerzel_und_dem_CEC_Text_gelesen()
        {
            Assert.Equal(DbWerte.PV_TECHNOLOGIE_CDTE, PvErweitertesModell.TechnologieAusPan("mtCdTe"));
            Assert.Equal(DbWerte.PV_TECHNOLOGIE_C_SI, PvErweitertesModell.TechnologieAusCec("Mono-c-Si"));
            Assert.Null(PvErweitertesModell.TechnologieAusCec(null));
        }
    }
}
