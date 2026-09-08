using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Auslegungshilfe (<c>StrangAuslegung</c>, Anwenderwunsch 08.09.2026) — gerechnet
    /// am Modul und Gerät des Anhangs A der <c>StrangPlausibilitaetTests</c>, damit beide
    /// Richtungen dieselben Zahlen tragen: Was die Prüfung rot oder gelb nennt, liegt hier
    /// außerhalb des genannten Bereichs.
    /// </summary>
    public class StrangAuslegungTests
    {
        // Ablytek 6MN6A275: 275,19 W, U_oc 38,4 V, U_mpp 31,4 V, I_sc 9,34 A,
        // beta_OC -0,118 V/K, alpha_SC 0,0047 A/K (Anhang A).
        private static PhotovoltaikModel Modul()
        {
            return new PhotovoltaikModel
            {
                m_szName = "Ablytek 6MN6A275", m_Leistung = 275.19, m_U_Leerlauf = 38.4,
                m_U_Mpp = 31.4, m_I_Kurzschluss = 9.34, m_beta_OC = -0.118, m_alpha_SC = 0.0047
            };
        }

        // Muster 2500TL: 2,5 kW, MPP 80…500 V, U_dc 600 V, 12 A, ein Tracker.
        private static WechselrichterModel Geraet(double pAc = 2.5, double uMppMin = 80.0,
                                                  double uMppMax = 500.0, double uDcMax = 600.0,
                                                  double iDcMax = 12.0, int? mppt = 1,
                                                  double? pDcMax = null, int? straengeJeMppt = null)
        {
            return new WechselrichterModel
            {
                m_ID = 7, m_szName = "Muster 2500TL", m_P_AC_Nenn = pAc, m_U_Mpp_Min = uMppMin,
                m_U_Mpp_Max = uMppMax, m_U_Dc_Max = uDcMax, m_I_Dc_Max = iDcMax,
                m_Anzahl_Mppt = mppt, m_P_DC_Max = pDcMax, m_Straenge_Je_Mppt = straengeJeMppt
            };
        }

        [Fact]
        public void Die_Reihe_liegt_zwischen_vier_und_vierzehn_Modulen()
        {
            // P1: 600 / 42,53 = 14,1 -> 14 · P2: 80 / 26,09 = 3,07 -> 4 · P3: 500 / 35,53 = 14,07 -> 14
            StrangAuslegung.Reihenbereich r = StrangAuslegung.Reihe(Modul(), Geraet());
            Assert.True(r.Pruefbar);
            Assert.True(r.Moeglich);
            Assert.Equal(14, r.MaxUoc);
            Assert.Equal(4, r.MinMpp);
            Assert.Equal(14, r.MaxMpp);
            Assert.Equal(4, r.Min);
            Assert.Equal(14, r.Max);
        }

        [Fact]
        public void Die_Reihe_stimmt_mit_der_Pruefung_ueberein()
        {
            // Anhang A: 15 Module sind rot über P1 - die Hilfe sagt hoechstens 14.
            StrangAuslegung.Reihenbereich r = StrangAuslegung.Reihe(Modul(), Geraet());
            Assert.True(15 > r.Max.Value);
            Assert.True(10 >= r.Min && 10 <= r.Max.Value);
        }

        [Fact]
        public void Ein_Tracker_traegt_einen_Strang_dieses_Moduls()
        {
            // 12 A / 9,5515 A = 1,26 -> 1
            Assert.Equal(1, StrangAuslegung.ParallelJeMppt(Modul(), Geraet()));
            Assert.Equal(3, StrangAuslegung.ParallelJeMppt(Modul(), Geraet(iDcMax: 30.0)));
            Assert.Equal(2, StrangAuslegung.ParallelJeMppt(Modul(), Geraet(iDcMax: 30.0, straengeJeMppt: 2)));
        }

        [Fact]
        public void Das_Geraet_traegt_zehn_bis_dreizehn_Module()
        {
            // 1,0 · 2500 / 275,19 = 9,08 -> 10 · 1,5 · 2500 / 275,19 = 13,6 -> 13
            StrangAuslegung.Modulbereich m = StrangAuslegung.ModuleJeGeraet(Modul(), Geraet());
            Assert.True(m.Pruefbar);
            Assert.Equal(10, m.Min);
            Assert.Equal(13, m.Max);
            // Die DC-Eingangsgrenze kann die Obergrenze weiter druecken: 3,0 kW / 275,19 W = 10,9 -> 10.
            Assert.Equal(10, StrangAuslegung.ModuleJeGeraet(Modul(), Geraet(pDcMax: 3.0)).Max);
        }

        [Fact]
        public void Der_Vorschlag_fuer_zehn_Module_ist_ein_Strang_an_einem_Geraet()
        {
            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(Modul(), Geraet(), 10);
            Assert.True(v.Moeglich, v.Grund);
            Assert.Equal(10, v.Reihe);
            Assert.Equal(1, v.Parallel);
            Assert.Equal(1, v.Geraete);
            Assert.Equal(1.10076, v.DcAc, 5);
        }

        [Fact]
        public void Der_Vorschlag_fuer_zwanzig_Module_braucht_zwei_Geraete()
        {
            // Ein Tracker, ein Strang je Tracker: zwei Straenge zu zehn -> zwei Geraete zu 1,10.
            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(Modul(), Geraet(), 20);
            Assert.True(v.Moeglich, v.Grund);
            Assert.Equal(10, v.Reihe);
            Assert.Equal(1, v.Parallel);
            Assert.Equal(2, v.Geraete);
            Assert.Equal(2, v.Straenge);
        }

        [Fact]
        public void Ohne_passende_Reihe_nennt_der_Vorschlag_den_Grund()
        {
            // MPP-Fenster ab 590 V: 590 / 26,09 = 22,6 -> mindestens 23, aber U_oc erlaubt hoechstens 14.
            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(Modul(), Geraet(uMppMin: 590.0), 20);
            Assert.False(v.Moeglich);
            Assert.Contains("Reihe", v.Grund);
        }

        [Fact]
        public void Die_Bewertung_stellt_das_passende_Geraet_nach_vorn()
        {
            var katalog = new List<WechselrichterModel>
            {
                Geraet(pAc: 100.0),                     // viel zu gross: DC/AC 0,03
                Geraet(uMppMin: 590.0),                 // Fenster unerreichbar
                Geraet()                                // passt: 1,10
            };
            katalog[0].m_szName = "Gross"; katalog[1].m_szName = "Hoch"; katalog[2].m_szName = "Passt";
            List<StrangAuslegung.Bewertung> b = StrangAuslegung.GeraeteBewerten(Modul(), 10, katalog);
            Assert.Equal("Passt", b[0].Geraet.m_szName);
            Assert.True(b[0].Vorschlag.Moeglich);
            Assert.False(b[2].Vorschlag.Moeglich);
        }

        [Fact]
        public void Die_Saetze_nennen_die_Bereiche()
        {
            CultureInfo alt = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            try
            {
                Assert.Equal("passend wären 4…14 Module in Reihe", StrangAuslegung.ReiheEmpfehlung(Modul(), Geraet()));
                Assert.Equal("passend wären 10…13 Module je Gerät", StrangAuslegung.GeraetEmpfehlung(Modul(), Geraet()));
                Assert.Equal("keine Reihe passt zu diesem Gerät, anderes Gerät wählen",
                             StrangAuslegung.ReiheEmpfehlung(Modul(), Geraet(uMppMin: 590.0)));
                Assert.Equal("", StrangAuslegung.ReiheEmpfehlung(new PhotovoltaikModel(), Geraet()));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = alt;
                Thread.CurrentThread.CurrentUICulture = alt;
            }
        }
    }
}
