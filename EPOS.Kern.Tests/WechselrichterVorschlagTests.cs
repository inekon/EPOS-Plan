using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// „Wechselrichter vorschlagen" (<c>WechselrichterVorschlag</c>): Bewertung und Rangfolge
    /// je Kataloggerät, gerechnet am Modul der <c>StrangAuslegungTests</c> (275,19 W,
    /// U_oc 38,4 V, U_mpp 31,4 V, I_sc 9,34 A, beta_OC −0,118 V/K, alpha_SC 0,0047 A/K) und
    /// fünf synthetischen Geräten: passend, bedingt (DC/AC hoch), zu klein, zu groß und eines
    /// mit verletztem MPP-Fenster.
    /// </summary>
    public class WechselrichterVorschlagTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const double KALT = -10.0;
        private const double HEISS = 70.0;

        private static PhotovoltaikModel Modul()
        {
            return new PhotovoltaikModel
            {
                m_szName = "Modul 275", m_Leistung = 275.19, m_U_Leerlauf = 38.4,
                m_U_Mpp = 31.4, m_I_Kurzschluss = 9.34, m_beta_OC = -0.118, m_alpha_SC = 0.0047
            };
        }

        private static WechselrichterModel Geraet(int id, string name, double pAc, double uMppMin = 80.0,
                                                  double uMppMax = 500.0, double uDcMax = 600.0,
                                                  double iDcMax = 12.0, int? mppt = 2)
        {
            return new WechselrichterModel
            {
                m_ID = id, m_szName = name, m_szFirma = "Muster", m_P_AC_Nenn = pAc,
                m_U_Mpp_Min = uMppMin, m_U_Mpp_Max = uMppMax, m_U_Dc_Max = uDcMax,
                m_I_Dc_Max = iDcMax, m_Anzahl_Mppt = mppt
            };
        }

        // Passend: 4,6 kW, zwei Tracker -> 20 Module = 1 x (2 x 10), DC/AC 1,197.
        private static WechselrichterModel Passend() => Geraet(1, "Passend 4600", 4.6);
        // Bedingt: 3,8 kW -> DC/AC 1,448 (zwischen 1,3 und 1,5).
        private static WechselrichterModel Knapp() => Geraet(2, "Knapp 3800", 3.8);
        // Zu klein: 0,7 kW traegt hoechstens 3 Module (DC/AC 1,5), die kuerzeste Reihe sind 4.
        private static WechselrichterModel Klein() => Geraet(3, "Mikro 700", 0.7);
        // Zu gross: 20 kW brauchen mindestens 73 Module fuer DC/AC 1,0.
        private static WechselrichterModel Gross() => Geraet(4, "Gross 20000", 20.0);
        // MPP-Fenster 600...800 V: die Reihe braucht heiss >= 23 Module, kalt <= 22.
        private static WechselrichterModel Hochvolt() => Geraet(5, "Hochvolt 5000", 5.0, 600.0, 800.0, 1000.0);

        private static List<WechselrichterModel> Katalog()
            => new List<WechselrichterModel> { Gross(), Hochvolt(), Knapp(), Klein(), Passend() };

        [Fact]
        public void Die_Rangfolge_setzt_geeignet_vor_bedingt_vor_ungeeignet()
        {
            List<WechselrichterVorschlag.Kandidat> liste =
                WechselrichterVorschlag.Bewerten(Modul(), 20, KALT, HEISS, Katalog());

            Assert.Equal(5, liste.Count);
            Assert.Equal("Passend 4600", liste[0].Geraet.m_szName);
            Assert.Equal(WechselrichterVorschlag.Eignung.Geeignet, liste[0].Stufe);
            Assert.Equal("Knapp 3800", liste[1].Geraet.m_szName);
            Assert.Equal(WechselrichterVorschlag.Eignung.Bedingt, liste[1].Stufe);
            Assert.Equal(WechselrichterVorschlag.Grund.DcAcHoch, liste[1].Hauptgrund);

            // Die ungeeigneten stehen nach Namen (ordinal) am Ende.
            Assert.All(liste.Skip(2), k => Assert.Equal(WechselrichterVorschlag.Eignung.Ungeeignet, k.Stufe));
            Assert.Equal(new[] { "Gross 20000", "Hochvolt 5000", "Mikro 700" },
                         liste.Skip(2).Select(k => k.Geraet.m_szName).ToArray());
        }

        [Fact]
        public void Die_ungeeigneten_Geraete_nennen_ihren_Grund()
        {
            var m = Modul();
            Assert.Equal(WechselrichterVorschlag.Grund.GeraetZuKlein,
                         WechselrichterVorschlag.Bewerte(m, Klein(), 20, KALT, HEISS).Hauptgrund);
            Assert.Equal(WechselrichterVorschlag.Grund.GeraetZuGross,
                         WechselrichterVorschlag.Bewerte(m, Gross(), 20, KALT, HEISS).Hauptgrund);
            Assert.Equal(WechselrichterVorschlag.Grund.Spannungsfenster,
                         WechselrichterVorschlag.Bewerte(m, Hochvolt(), 20, KALT, HEISS).Hauptgrund);
            Assert.Equal(WechselrichterVorschlag.Grund.ModulFehlt,
                         WechselrichterVorschlag.Bewerte(null, Passend(), 20, KALT, HEISS).Hauptgrund);
            Assert.Equal(WechselrichterVorschlag.Grund.ModulFehlt,
                         WechselrichterVorschlag.Bewerte(m, Passend(), 0, KALT, HEISS).Hauptgrund);

            var ohneWerte = new WechselrichterModel { m_ID = 9, m_szName = "Leer", m_P_AC_Nenn = 5.0 };
            Assert.Equal(WechselrichterVorschlag.Grund.WerteFehlen,
                         WechselrichterVorschlag.Bewerte(m, ohneWerte, 20, KALT, HEISS).Hauptgrund);
        }

        [Fact]
        public void Das_passende_Geraet_traegt_seine_Kennzahlen()
        {
            WechselrichterVorschlag.Kandidat k =
                WechselrichterVorschlag.Bewerte(Modul(), Passend(), 20, KALT, HEISS);

            Assert.Equal(WechselrichterVorschlag.Eignung.Geeignet, k.Stufe);
            Assert.Empty(k.Gruende);
            Assert.Equal(1, k.Geraete);
            Assert.Equal(2, k.Vorschlag.Parallel);
            Assert.Equal(10, k.Vorschlag.Reihe);
            Assert.Equal(0, k.Restmodule);
            Assert.Equal(20 * 275.19 / 1000.0 / 4.6, k.DcAc!.Value, 9);

            // U_oc(-10 °C) = 10 · (38,4 + 0,118 · 35) = 425,3 V; MPP 70 °C = 260,9 V, -10 °C = 355,3 V.
            Assert.Equal(425.3, k.UocKalt!.Value, 6);
            Assert.Equal(260.9, k.MppHeiss!.Value, 6);
            Assert.Equal(355.3, k.MppKalt!.Value, 6);
            Assert.Equal(600.0, k.UDcMax);
            // Ein Strang je Tracker: 9,34 + 0,0047 · 45 = 9,5515 A gegen 12 A.
            Assert.Equal(9.5515, k.StromJeMppt!.Value, 6);
            Assert.Equal(12.0, k.IMaxJeMppt);

            Assert.Equal("1 × (2 × 10)", WechselrichterVorschlag.AufteilungText(k));
            Assert.Equal("1,20", WechselrichterVorschlag.DcAcText(k));
            Assert.Equal("425 ≤ 600 V", WechselrichterVorschlag.UocText(k));
            Assert.Equal("261…355 V in 80…500 V", WechselrichterVorschlag.MppText(k));
            Assert.Equal("geeignet", WechselrichterVorschlag.StufeText(k.Stufe));
            Assert.Equal("", WechselrichterVorschlag.GrundText(k));
        }

        [Fact]
        public void Restmodule_machen_ein_Geraet_bedingt()
        {
            // 21 Module gehen mit zwei Trackern nicht auf; 20 schon - ein Modul bleibt übrig.
            WechselrichterVorschlag.Kandidat k =
                WechselrichterVorschlag.Bewerte(Modul(), Passend(), 21, KALT, HEISS);

            Assert.Equal(WechselrichterVorschlag.Eignung.Bedingt, k.Stufe);
            Assert.Equal(WechselrichterVorschlag.Grund.Restmodule, k.Hauptgrund);
            Assert.Equal(1, k.Restmodule);
            Assert.Equal(21, k.Modulzahl);
            Assert.Equal("1 Module ohne Strang", WechselrichterVorschlag.GrundText(k));
        }

        [Fact]
        public void Innerhalb_einer_Stufe_zaehlt_die_Naehe_zum_Zielband()
        {
            // 5,0 kW: DC/AC 1,10 (im Zielband); 5,5 kW: DC/AC 1,00 (am Rand des Fensters).
            var nah = Geraet(11, "Z Nah 5000", 5.0);
            var fern = Geraet(12, "A Fern 5500", 5.5);
            var liste = WechselrichterVorschlag.Bewerten(Modul(), 20, KALT, HEISS,
                                                         new[] { fern, nah, Passend() });

            Assert.All(liste, k => Assert.Equal(WechselrichterVorschlag.Eignung.Geeignet, k.Stufe));
            // Im Band (Abstand 0) stehen Passend (1,197) und Nah (1,101) - nach Name; Fern (1,0006) zuletzt.
            Assert.Equal(new[] { "Passend 4600", "Z Nah 5000", "A Fern 5500" },
                         liste.Select(k => k.Geraet.m_szName).ToArray());
        }

        [Fact]
        public void Die_Rangfolge_haengt_nicht_an_der_Eingabereihenfolge()
        {
            var a = WechselrichterVorschlag.Bewerten(Modul(), 20, KALT, HEISS, Katalog())
                                           .Select(k => k.Geraet.m_ID).ToArray();
            var umgekehrt = Katalog();
            umgekehrt.Reverse();
            var b = WechselrichterVorschlag.Bewerten(Modul(), 20, KALT, HEISS, umgekehrt)
                                           .Select(k => k.Geraet.m_ID).ToArray();
            Assert.Equal(a, b);

            // Doppelte Ids und null zählen nicht.
            var mitDoppel = Katalog();
            mitDoppel.Add(Passend());
            mitDoppel.Add(null!);
            Assert.Equal(5, WechselrichterVorschlag.Bewerten(Modul(), 20, KALT, HEISS, mitDoppel).Count);
        }

        [Fact]
        public void Die_Auslegungstemperaturen_wirken_auf_die_Bewertung()
        {
            // Bei -30 °C steigt U_oc der Zehnerreihe auf 10 · (38,4 + 0,118 · 55) = 448,9 V.
            WechselrichterVorschlag.Kandidat k =
                WechselrichterVorschlag.Bewerte(Modul(), Passend(), 20, -30.0, HEISS);
            Assert.Equal(448.9, k.UocKalt!.Value, 6);

            // Ein Gerät mit U_max 440 V lässt bei -10 °C die Zehnerreihe zu (425,3 V), bei -30 °C nicht.
            var eng = Geraet(21, "Eng 4600", 4.6, uDcMax: 440.0);
            Assert.Equal(10, WechselrichterVorschlag.Bewerte(Modul(), eng, 20, KALT, HEISS).Vorschlag.Reihe);
            Assert.NotEqual(10, WechselrichterVorschlag.Bewerte(Modul(), eng, 20, -30.0, HEISS).Vorschlag?.Reihe ?? 0);
        }

        [Fact]
        public void Die_Aufteilung_der_Vorgabetemperaturen_bleibt_die_von_bisher()
        {
            StrangAuslegung.Vorschlag alt = StrangAuslegung.Vorschlagen(Modul(), Passend(), 20);
            StrangAuslegung.Vorschlag neu = StrangAuslegung.Vorschlagen(Modul(), Passend(), 20,
                StrangPlausibilitaet.T_KALT, StrangPlausibilitaet.T_HEISS);
            Assert.Equal(alt.Reihe, neu.Reihe);
            Assert.Equal(alt.Parallel, neu.Parallel);
            Assert.Equal(alt.Geraete, neu.Geraete);
            Assert.Equal(alt.DcAc, neu.DcAc);
        }

        /// <summary>
        /// Ein Strang, dessen Kurzschlussstrom schon allein über der Grenze je Tracker liegt,
        /// ist ungeeignet wegen des STROMS — nicht wegen der Teilbarkeit. Werte des Moduls
        /// „530 W" und des Geräts „Muster 2500TL" der Testdatenbank: I_sc(70 °C) =
        /// 13,6 + 0,00272 · 45 = 13,72 A gegen I_Dc_Max 12 A.
        /// </summary>
        [Fact]
        public void Ein_Strang_ueber_der_Stromgrenze_ist_ungeeignet_wegen_des_Stroms()
        {
            var modul = new PhotovoltaikModel
            {
                m_szName = "Modul 530", m_Leistung = 530.785, m_U_Leerlauf = 49.2, m_U_Mpp = 41.5,
                m_I_Kurzschluss = 13.6, m_alpha_SC = 0.00272, m_beta_OC = -0.128904
            };
            var geraet = Geraet(1, "Muster 2500TL", 2.5);
            geraet.m_P_DC_Max = 3.75;
            geraet.m_Straenge_Je_Mppt = 2;

            WechselrichterVorschlag.Kandidat k = WechselrichterVorschlag.Bewerte(modul, geraet, 10, KALT, HEISS);

            Assert.Equal(WechselrichterVorschlag.Eignung.Ungeeignet, k.Stufe);
            Assert.Equal(WechselrichterVorschlag.Grund.StromZuHoch, k.Hauptgrund);
            Assert.Equal("Strom eines Strangs 13,7 A über der Grenze je MPPT 12,0 A",
                         WechselrichterVorschlag.GrundText(k));

            // Mit gepflegtem Kurzschlussstrom je MPPT (15 A) passt es: 2 Geräte à 1 × 5 Module.
            geraet.m_I_Sc_Max = 15.0;
            k = WechselrichterVorschlag.Bewerte(modul, geraet, 10, KALT, HEISS);
            Assert.Equal(WechselrichterVorschlag.Eignung.Geeignet, k.Stufe);
            Assert.Equal("2 × (1 × 5)", WechselrichterVorschlag.AufteilungText(k));
            Assert.Equal("1,06", WechselrichterVorschlag.DcAcText(k));
        }

        [Fact]
        public void Die_Gruende_stehen_als_Satz_in_der_Kultur_des_Anwenders()
        {
            var m = Modul();
            Assert.Contains("zu klein",
                WechselrichterVorschlag.GrundText(WechselrichterVorschlag.Bewerte(m, Klein(), 20, KALT, HEISS)),
                StringComparison.Ordinal);
            Assert.Contains("alle 20 Module",
                WechselrichterVorschlag.GrundText(WechselrichterVorschlag.Bewerte(m, Gross(), 20, KALT, HEISS)),
                StringComparison.Ordinal);
            Assert.Equal("DC/AC 1,45 über 1,30",
                WechselrichterVorschlag.GrundText(WechselrichterVorschlag.Bewerte(m, Knapp(), 20, KALT, HEISS)));
            Assert.Equal("", WechselrichterVorschlag.AufteilungText(WechselrichterVorschlag.Bewerte(m, Klein(), 20, KALT, HEISS)));
        }
    }
}
