using System;
using System.Globalization;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die 14 Zahlen der Lastspitzenkappung samt ihren vier Pruefregeln und der
    /// Umrechnung in die Engine-Parameter (<see cref="PeakShavingEingaben"/>,
    /// iU9-W12.6).
    ///
    /// <para>Vorbild ist <c>Form_PeakShaving.ParameterLesen</c> (:419-480). Geprueft
    /// werden die vier Regeln in ihrer Reihenfolge, die vier Einheitenumrechnungen,
    /// die beiden festen Werte (<c>DtH = 0,25</c>, <c>Degradation = 0</c>) und die
    /// Uebernahme der Vorbelegung.</para>
    ///
    /// <para>Ohne Datenbank. Wo Texte geprueft werden, ist die Sprache festgelegt
    /// (Regel seit iU9-W8).</para>
    /// </summary>
    public class PeakShavingEingabenTests
    {
        /// <summary>Ein in jeder Hinsicht gueltiger Satz.</summary>
        private static PeakShavingEingaben Gut() => new PeakShavingEingaben
        {
            PKw = 100.0,
            KapazitaetKwh = 200.0,
            SoCMinProzent = 10.0,
            SoCMaxProzent = 90.0,
            StartSoCProzent = 10.0,
            WirkungsgradRt = 0.9,
            ZielschwelleKw = 300.0,
            LeistungspreisEurProKwA = 120.0,
            BezugspreisMittelCtKwh = 25.0,
            CCapEurProKwh = 400.0,
            CPowEurProKw = 200.0,
            IFixEur = 1000.0,
            KapitalzinsProzent = 3.0,
            NutzungsdauerA = 15.0,
            Adaptiv = false,
            Kompatibilitaetsmodus = false
        };

        // ==================================================================
        // Die vier Pruefregeln
        // ==================================================================

        [Fact]
        public void Ein_gueltiger_Satz_wird_nicht_beanstandet()
        {
            Assert.Equal("", Gut().Pruefe(out PeakShavingEingaben.Feld feld));
            Assert.Equal(PeakShavingEingaben.Feld.Keines, feld);
        }

        [Fact]
        public void Eine_Kapazitaet_von_null_beanstandet_das_Kapazitaetsfeld()
        {
            using var _ = new DeutscheOberflaeche();

            PeakShavingEingaben e = Gut();
            e.KapazitaetKwh = 0.0;

            Assert.Equal("Die Kapazität muss größer als 0 sein.",
                         e.Pruefe(out PeakShavingEingaben.Feld feld));
            Assert.Equal(PeakShavingEingaben.Feld.Kapazitaet, feld);
        }

        [Fact]
        public void Ein_umgedrehtes_Ladeband_beanstandet_die_obere_Grenze()
        {
            using var _ = new DeutscheOberflaeche();

            PeakShavingEingaben e = Gut();
            e.SoCMaxProzent = 10.0;      // gleich der unteren Grenze

            Assert.Equal("Der obere Ladezustand muss über dem unteren liegen.",
                         e.Pruefe(out PeakShavingEingaben.Feld feld));
            Assert.Equal(PeakShavingEingaben.Feld.SoCMax, feld);
        }

        /// <summary>
        /// Der Wirkungsgrad liegt im halboffenen Bereich (0 … 1]: 1,0 ist erlaubt,
        /// 0 und alles darueber nicht — woertlich <c>eta &lt;= 0 || eta &gt; 1</c>.
        /// </summary>
        [Fact]
        public void Der_Wirkungsgrad_darf_eins_sein_aber_nicht_null_und_nicht_mehr()
        {
            using var sprache = new DeutscheOberflaeche();

            PeakShavingEingaben e = Gut();

            e.WirkungsgradRt = 1.0;
            Assert.Equal("", e.Pruefe(out PeakShavingEingaben.Feld ohne));
            Assert.Equal(PeakShavingEingaben.Feld.Keines, ohne);

            e.WirkungsgradRt = 0.0;
            Assert.Equal("Der Wirkungsgrad muss im Bereich (0 … 1] liegen.",
                         e.Pruefe(out PeakShavingEingaben.Feld feld));
            Assert.Equal(PeakShavingEingaben.Feld.Wirkungsgrad, feld);

            e.WirkungsgradRt = 1.0001;
            Assert.Equal("Der Wirkungsgrad muss im Bereich (0 … 1] liegen.",
                         e.Pruefe(out PeakShavingEingaben.Feld ueber));
            Assert.Equal(PeakShavingEingaben.Feld.Wirkungsgrad, ueber);
        }

        [Fact]
        public void Eine_Nutzungsdauer_von_null_beanstandet_die_Nutzungsdauer()
        {
            using var _ = new DeutscheOberflaeche();

            PeakShavingEingaben e = Gut();
            e.NutzungsdauerA = 0.0;

            Assert.Equal("Die Nutzungsdauer muss größer als 0 sein.",
                         e.Pruefe(out PeakShavingEingaben.Feld feld));
            Assert.Equal(PeakShavingEingaben.Feld.Nutzungsdauer, feld);
        }

        /// <summary>
        /// Die Reihenfolge ist die des Vorlaeufers: Kapazitaet, Band, Wirkungsgrad,
        /// Nutzungsdauer. Stimmen mehrere nicht, meldet die ERSTE.
        /// </summary>
        [Fact]
        public void Bei_mehreren_Verstoessen_meldet_die_erste_Regel()
        {
            PeakShavingEingaben e = Gut();
            e.KapazitaetKwh = -1.0;
            e.SoCMaxProzent = 0.0;
            e.WirkungsgradRt = 5.0;
            e.NutzungsdauerA = -3.0;

            e.Pruefe(out PeakShavingEingaben.Feld feld);
            Assert.Equal(PeakShavingEingaben.Feld.Kapazitaet, feld);
        }

        // ==================================================================
        // Die Umrechnung
        // ==================================================================

        /// <summary>
        /// Das SoC-Band steht in Prozent an der Maske und in kWh in der Engine;
        /// der Kapitalzins geht als Faktor hinein.
        /// </summary>
        [Fact]
        public void Das_Ladeband_wird_von_Prozent_in_Kilowattstunden_gerechnet()
        {
            SpeicherParameter p = Gut().AlsSpeicherParameter();

            Assert.Equal(200.0, p.CNomKwh);
            Assert.Equal(100.0, p.PKw);
            Assert.Equal(20.0, p.SoCMinKwh);        // 200 * 10 %
            Assert.Equal(180.0, p.SoCMaxKwh);       // 200 * 90 %
            Assert.Equal(20.0, p.StartSoCKwh);      // 200 * 10 %
            Assert.Equal(0.03, p.Kapitalzins);      // 3 % als Faktor
        }

        /// <summary>
        /// <c>DtH = 0,25</c> ist fest — die Maske rechnet im Viertelstundenraster —,
        /// und die Degradation bleibt 0: Sie ist an dieser Maske bewusst kein Feld.
        /// </summary>
        [Fact]
        public void Zeitraster_und_Degradation_sind_fest()
        {
            SpeicherParameter p = Gut().AlsSpeicherParameter();

            Assert.Equal(0.25, p.DtH);
            Assert.Equal(PeakShavingEingaben.DtHStunden, p.DtH);
            Assert.Equal(0.0, p.DegradationProA);
        }

        /// <summary>
        /// Im adaptiven Betrieb geht KEINE Zielschwelle hinein — sie wird ja gerade
        /// nachgezogen.
        /// </summary>
        [Fact]
        public void Die_Zielschwelle_geht_nur_im_festen_Betrieb_hinein()
        {
            PeakShavingEingaben e = Gut();

            PeakShavingParameter fest = e.AlsPeakShavingParameter();
            Assert.False(fest.Adaptiv);
            Assert.Equal(300.0, fest.PZielKw);

            e.Adaptiv = true;
            PeakShavingParameter adaptiv = e.AlsPeakShavingParameter();
            Assert.True(adaptiv.Adaptiv);
            Assert.Equal(0.0, adaptiv.PZielKw);
            Assert.Equal(120.0, adaptiv.LeistungspreisEurProKwA);
            Assert.Equal(25.0, adaptiv.BezugspreisMittelCtKwh);
        }

        [Fact]
        public void Der_Kompatibilitaetsschalter_waehlt_den_Rechenmodus()
        {
            PeakShavingEingaben e = Gut();

            Assert.Equal(SpeicherModus.Energetisch, e.Modus);
            e.Kompatibilitaetsmodus = true;
            Assert.Equal(SpeicherModus.ExcelKompatibilitaet, e.Modus);
        }

        // ==================================================================
        // Die Vorbelegung
        // ==================================================================

        /// <summary>
        /// <c>Aus</c> uebernimmt die Vorbelegung und setzt „adaptiv" fest auf
        /// <c>true</c> — der Vorlaeufer setzte den Haken unabhaengig von der
        /// Variante (:250).
        /// </summary>
        [Fact]
        public void Die_Vorbelegung_kommt_durch_und_adaptiv_steht_immer_an()
        {
            PeakShavingVorbelegung v = new PeakShavingVorbelegung
            {
                AusProjekt = true,
                Bezeichner = "Speicher A",
                PKw = 55.0,
                KapazitaetKwh = 111.0,
                Kompatibilitaetsmodus = true
            };

            PeakShavingEingaben e = PeakShavingEingaben.Aus(v);

            Assert.Equal(55.0, e.PKw);
            Assert.Equal(111.0, e.KapazitaetKwh);
            Assert.True(e.Kompatibilitaetsmodus);
            Assert.True(e.Adaptiv);
            Assert.Equal(0.0, e.ZielschwelleKw);
        }

        /// <summary>Ohne Vorbelegung gelten die Vorgaben der Modelle.</summary>
        [Fact]
        public void Ohne_Vorbelegung_stehen_die_Vorgaben_der_Modelle()
        {
            PeakShavingEingaben e = PeakShavingEingaben.Aus(null);

            Assert.Equal(100.0, e.PKw);
            Assert.Equal(200.0, e.KapazitaetKwh);
            Assert.Equal(StromspeicherVarianteModel.SOC_MIN_VORGABE, e.SoCMinProzent);
            Assert.Equal(StromspeicherVarianteModel.SOC_MAX_VORGABE, e.SoCMaxProzent);
            Assert.Equal(StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE, e.WirkungsgradRt);
            Assert.Equal(StromspeicherVarianteModel.NUTZUNGSDAUER_VORGABE, e.NutzungsdauerA);
            Assert.Equal("", e.Pruefe(out _));
        }

        /// <summary>
        /// Die Meldungen kommen aus dem Ressourcenkatalog und folgen der
        /// Oberflaechensprache; auf einem englischen Laeufer stehen dort die
        /// englischen Texte (Regel seit iU9-W8).
        /// </summary>
        [Fact]
        public void Die_Meldung_folgt_der_Oberflaechensprache()
        {
            PeakShavingEingaben e = Gut();
            e.KapazitaetKwh = 0.0;

            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                string en = e.Pruefe(out _);
                Assert.NotEmpty(en);

                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                string de = e.Pruefe(out _);
                Assert.NotEqual(en, de);
            }
            finally { CultureInfo.CurrentUICulture = vorher; }
        }

        /// <summary>
        /// Stellt de-DE ein und beim Verlassen die vorherige Sprache wieder her — die
        /// Regel seit iU9-W8: Der Windows-Laeufer der CI laeuft mit en-US.
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }
    }
}
