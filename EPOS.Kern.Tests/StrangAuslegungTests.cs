using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                                                  double? pDcMax = null, int? straengeJeMppt = null,
                                                  double? iScMax = null)
        {
            return new WechselrichterModel
            {
                m_ID = 7, m_szName = "Muster 2500TL", m_P_AC_Nenn = pAc, m_U_Mpp_Min = uMppMin,
                m_U_Mpp_Max = uMppMax, m_U_Dc_Max = uDcMax, m_I_Dc_Max = iDcMax,
                m_Anzahl_Mppt = mppt, m_P_DC_Max = pDcMax, m_Straenge_Je_Mppt = straengeJeMppt,
                m_I_Sc_Max = iScMax
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

        /// <summary>
        /// <b>W6‑B‑8</b> (Anwenderwunsch 08.09.2026): Der Vorschlag wird zur TABELLE —
        /// je Gerät und belegtem Tracker eine Zeile. Zwanzig Module an einem
        /// Ein-Tracker-Gerät sind zwei Geräte zu je einem Strang; die Tabelle nennt sie
        /// als Gerät 1/MPPT 1 und Gerät 2/MPPT 1.
        /// </summary>
        [Fact]
        public void Die_Aufteilung_gibt_je_Geraet_und_Tracker_eine_Zeile()
        {
            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(Modul(), Geraet(), 20);
            List<StrangAuslegung.Strangvorgabe> zeilen = StrangAuslegung.Aufteilen(v, 1);

            Assert.Equal(2, zeilen.Count);
            Assert.Equal(new[] { 1, 2 }, zeilen.Select(z => z.Geraetenummer).ToArray());
            Assert.Equal(new[] { 1, 1 }, zeilen.Select(z => z.Mppt).ToArray());
            Assert.All(zeilen, z => Assert.Equal(10, z.ModuleReihe));
            Assert.All(zeilen, z => Assert.Equal(1, z.StraengeParallel));

            // Ohne Vorschlag gibt es keine Tabelle - die bestehende bleibt stehen.
            Assert.Empty(StrangAuslegung.Aufteilen(
                StrangAuslegung.Vorschlagen(Modul(), Geraet(uMppMin: 590.0), 20), 1));
            Assert.Empty(StrangAuslegung.Aufteilen(null, 1));
        }

        /// <summary>
        /// <b>W6‑B‑8:</b> Zwei Stränge an EINEM Gerät mit zwei MPP-Trackern bekommen
        /// Tracker 1 und 2 — die Aufteilung verteilt so gleichmäßig wie möglich und
        /// hält damit die Grenze aus <c>ParallelJeMppt</c> von selbst ein. Geht die Zahl
        /// nicht auf, bekommen die VORDEREN Tracker den Strang mehr.
        /// </summary>
        [Fact]
        public void Zwei_Tracker_teilen_sich_die_Straenge_eines_Geraets()
        {
            // 20 Module, 5 kW, zwei Tracker, 30 A: ein Geraet mit zwei Straengen zu zehn.
            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(
                Modul(), Geraet(pAc: 5.0, iDcMax: 30.0, mppt: 2), 20);
            Assert.True(v.Moeglich, v.Grund);
            Assert.Equal(1, v.Geraete);
            Assert.Equal(2, v.Parallel);

            List<StrangAuslegung.Strangvorgabe> zeilen = StrangAuslegung.Aufteilen(v, 2);
            Assert.Equal(2, zeilen.Count);
            Assert.All(zeilen, z => Assert.Equal(1, z.Geraetenummer));
            Assert.Equal(new[] { 1, 2 }, zeilen.Select(z => z.Mppt).ToArray());
            Assert.All(zeilen, z => Assert.Equal(1, z.StraengeParallel));
            Assert.All(zeilen, z => Assert.Equal(10, z.ModuleReihe));

            // Drei Straenge auf zwei Tracker: 2 und 1, nicht 3 und 0.
            var ungerade = new StrangAuslegung.Vorschlag
            {
                Moeglich = true, Reihe = 10, Parallel = 3, Geraete = 1, Straenge = 3
            };
            Assert.Equal(new[] { 2, 1 },
                         StrangAuslegung.Aufteilen(ungerade, 2).Select(z => z.StraengeParallel).ToArray());

            // Mehr Tracker als Straenge lassen die ueberzaehligen unbelegt.
            Assert.Single(StrangAuslegung.Aufteilen(
                new StrangAuslegung.Vorschlag
                {
                    Moeglich = true, Reihe = 10, Parallel = 1, Geraete = 1, Straenge = 1
                }, 4));
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
            CultureInfo altUi = Thread.CurrentThread.CurrentUICulture;
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
                Thread.CurrentThread.CurrentUICulture = altUi;
            }
        }

        // =================================================================================
        // W6-B-9 / W6-B-10 / W6-B-11 (Anwenderentscheide 09.09.2026)
        // =================================================================================

        /// <summary>
        /// <b>Die Hilfe kennt denselben Rückfall wie die Ampel</b> (<b>W6‑B‑9</b>): Ohne
        /// <c>beta_OC</c> ist die Obergrenze aus P1 <c>⌊600 / (38,4 · 1,15)⌋ = ⌊13,59⌋
        /// = 13</c> — vorher entfiel sie ganz, und die Hilfe schickte den Anwender in
        /// genau das Rot, das sie vermeiden soll.
        /// </summary>
        [Fact]
        public void W6B9_Ohne_beta_OC_rechnet_die_Obergrenze_mit_dem_Faktor()
        {
            PhotovoltaikModel modul = Modul();
            modul.m_beta_OC = 0;

            StrangAuslegung.Reihenbereich r = StrangAuslegung.Reihe(modul, Geraet());

            Assert.Equal(13, r.MaxUoc);
            Assert.Null(r.MinMpp);      // P2 braucht den Koeffizienten wirklich
            Assert.Null(r.MaxMpp);
            Assert.True(r.Pruefbar);
            Assert.Equal(13, r.Max);
        }

        /// <summary>
        /// <b>Die Grenze der Strangzahl ist der KURZSCHLUSSstrom, wenn er gepflegt
        /// ist</b> (<b>W6‑B‑10</b>): <c>⌊25,0 / 9,5515⌋ = 2</c> statt
        /// <c>⌊12,0 / 9,5515⌋ = 1</c>. Ohne ihn bleibt es bei <c>I_Dc_Max</c>.
        /// </summary>
        [Fact]
        public void W6B10_ParallelJeMppt_nimmt_den_Kurzschlussstrom_als_Grenze()
        {
            Assert.Equal(1, StrangAuslegung.ParallelJeMppt(Modul(), Geraet()));
            Assert.Equal(2, StrangAuslegung.ParallelJeMppt(Modul(), Geraet(iScMax: 25.0)));

            // Der Deckel aus P5 gilt zusaetzlich - er ist die haertere Grenze.
            Assert.Equal(1, StrangAuslegung.ParallelJeMppt(
                                Modul(), Geraet(iScMax: 25.0, straengeJeMppt: 1)));
        }

        /// <summary>
        /// <b>Die Auslegungstemperaturen des Projekts gehen mit</b> (<b>W6‑B‑11</b>) —
        /// die Hilfe rät auf derselben Grundlage, auf der die Ampel prüft. Bei −20 °C
        /// ist <c>U_oc = 38,4 + (−0,118)·(−45) = 43,71 V</c> und die Obergrenze aus P1
        /// damit <c>⌊600 / 43,71⌋ = 13</c> statt 14.
        /// </summary>
        [Fact]
        public void W6B11_Die_Auslegungstemperaturen_verschieben_den_Reihenbereich()
        {
            StrangAuslegung.Reihenbereich kalt =
                StrangAuslegung.Reihe(Modul(), Geraet(), -20.0, StrangPlausibilitaet.T_HEISS);
            Assert.Equal(13, kalt.MaxUoc);

            // Ein heisserer Fall hebt die UNTERgrenze aus P2. An einem Geraet mit
            // MPP-Fenster ab 110 V: U_mpp(70 °C) = 26,09 V -> 110/26,09 = 4,22 -> 5;
            // U_mpp(110 °C) = 21,37 V -> 110/21,37 = 5,15 -> 6.
            Assert.Equal(5, StrangAuslegung.Reihe(Modul(), Geraet(uMppMin: 110.0)).MinMpp);
            Assert.Equal(6, StrangAuslegung.Reihe(Modul(), Geraet(uMppMin: 110.0),
                                                  StrangPlausibilitaet.T_KALT, 110.0).MinMpp);
        }

        /// <summary>
        /// <b>Ohne Temperaturangabe rechnet die Hilfe wie bisher</b> — die Zusicherung,
        /// an der die Ergebnisneutralität hängt.
        /// </summary>
        [Fact]
        public void W6B11_Ohne_Temperaturangabe_bleibt_der_Bereich_vier_bis_vierzehn()
        {
            StrangAuslegung.Reihenbereich a = StrangAuslegung.Reihe(Modul(), Geraet());
            StrangAuslegung.Reihenbereich b = StrangAuslegung.Reihe(
                Modul(), Geraet(), StrangPlausibilitaet.T_KALT, StrangPlausibilitaet.T_HEISS);

            Assert.Equal(a.Min, b.Min);
            Assert.Equal(a.Max, b.Max);
            Assert.Equal(4, b.Min);
            Assert.Equal(14, b.Max);
        }
    }

    /// <summary>
    /// <b>Der Vorschlag für die zwei Auslegungstemperaturen</b>
    /// (<c>AuslegungstemperaturVorschlag</c>, <b>W6‑B‑11</b>, Anwenderentscheid vom
    /// 09.09.2026).
    ///
    /// <para>Geprüft wird der DATENBANKFREIE Kern der Rechnung
    /// (<see cref="AuslegungstemperaturVorschlag.Aus"/>): die Zelltemperaturformel und
    /// der NOCT-Rückfall. Das Lesen der Klimareihe hat keinen eigenen Fall — es ist
    /// eine Abfrage, keine Regel.</para>
    /// </summary>
    public class AuslegungstemperaturVorschlagTests
    {
        /// <summary>
        /// <b>Die Zelltemperatur bei Volleinstrahlung</b>:
        /// <c>T_amb,max + (T_NOCT − 20) · 1000/800</c>. Mit 32,5 °C und T_NOCT 45 °C
        /// sind das <c>32,5 + 25 · 1,25 = 63,75 °C</c>. Der kalte Fall ist das
        /// Jahresminimum, unverändert.
        /// </summary>
        [Fact]
        public void Der_heisse_Fall_ist_die_Zelltemperatur_bei_1000_W()
        {
            AuslegungstemperaturVorschlag.Vorschlag v =
                AuslegungstemperaturVorschlag.Aus(-12.3, 32.5, 45.0);

            Assert.True(v.Moeglich);
            Assert.Equal(-12.3, v.Kalt, 6);
            Assert.Equal(63.75, v.Heiss, 6);
        }

        /// <summary>
        /// <b>Ein NOCT ausserhalb des Fensters 20…60 °C ergibt den Rückfall 45 °C</b> —
        /// dieselbe Regel wie im Rechenweg (<c>SimulationPV.NoctDesModuls</c>). Der
        /// Modulbestand führt in <c>T_NOCT</c> nachweislich den Kurzschlussstrom
        /// (Paket‑A‑Befund A1); ein Vorschlag, der daraus 9 °C Zelltemperatur
        /// errechnet, wäre schlimmer als keiner.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(0.0)]
        [InlineData(9.014)]
        [InlineData(75.0)]
        public void Ein_unplausibler_NOCT_faellt_auf_45_Grad_zurueck(double? noct)
        {
            Assert.Equal(45.0, AuslegungstemperaturVorschlag.NoctOderRueckfall(noct), 6);

            AuslegungstemperaturVorschlag.Vorschlag v =
                AuslegungstemperaturVorschlag.Aus(-10.0, 30.0, noct);
            Assert.Equal(61.25, v.Heiss, 6);      // 30 + 25 · 1,25
        }

        /// <summary>
        /// <b>Ein gepflegter NOCT wird genommen</b>: 48 °C ergeben
        /// <c>30 + 28 · 1,25 = 65,00 °C</c>.
        /// </summary>
        [Fact]
        public void Ein_plausibler_NOCT_geht_in_die_Rechnung()
        {
            Assert.Equal(48.0, AuslegungstemperaturVorschlag.NoctOderRueckfall(48.0), 6);
            Assert.Equal(65.0, AuslegungstemperaturVorschlag.Aus(-10.0, 30.0, 48.0).Heiss, 6);
        }

        /// <summary>
        /// <b>Der Rückfall ist derselbe wie im Rechenweg</b> — zwei Zahlen, die
        /// auseinanderlaufen dürfen, laufen auseinander.
        /// </summary>
        [Fact]
        public void Der_NOCT_Rueckfall_deckt_sich_mit_dem_Rechenweg()
        {
            Assert.Equal(SimulationPV.NOCT_RUECKFALL, AuslegungstemperaturVorschlag.NOCT_RUECKFALL);
            Assert.Equal(SimulationPV.NOCT_MIN, AuslegungstemperaturVorschlag.NOCT_MIN);
            Assert.Equal(SimulationPV.NOCT_MAX, AuslegungstemperaturVorschlag.NOCT_MAX);
        }

        /// <summary>
        /// <b>Die Herleitung nennt alle vier Zahlen</b> — sie ist das, was der Anwender
        /// liest, bevor er übernimmt.
        /// </summary>
        [Fact]
        public void Die_Herleitung_nennt_die_vier_Zahlen()
        {
            CultureInfo alt = Thread.CurrentThread.CurrentCulture;
            CultureInfo altUi = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            try
            {
                string satz = AuslegungstemperaturVorschlag.Aus(-12.3, 32.5, 45.0).Satz;

                Assert.Contains("-12,3", satz, System.StringComparison.Ordinal);
                Assert.Contains("32,5", satz, System.StringComparison.Ordinal);
                Assert.Contains("45,0", satz, System.StringComparison.Ordinal);
                Assert.Contains("63,8", satz, System.StringComparison.Ordinal);   // N1 rundet
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = alt;
                Thread.CurrentThread.CurrentUICulture = altUi;
            }
        }

        /// <summary>
        /// <b><c>null</c> heisst „die Vorgabe"</b> — in beiden Richtungen.
        /// </summary>
        [Fact]
        public void Ohne_gepflegten_Wert_gilt_die_Vorgabe()
        {
            Assert.Equal(StrangPlausibilitaet.T_KALT, Auslegungstemperaturen.Vorgabe.KaltOderVorgabe);
            Assert.Equal(StrangPlausibilitaet.T_HEISS, Auslegungstemperaturen.Vorgabe.HeissOderVorgabe);

            var eigen = new Auslegungstemperaturen(-20.0, 80.0);
            Assert.Equal(-20.0, eigen.KaltOderVorgabe);
            Assert.Equal(80.0, eigen.HeissOderVorgabe);
        }
    }
}
