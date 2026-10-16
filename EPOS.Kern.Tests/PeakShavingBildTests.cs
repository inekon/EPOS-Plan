using System;
using System.Globalization;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das Vorher/Nachher-Bild der Lastspitzenkappung
    /// (<see cref="PeakShavingBild"/>, iU9-W12.6 / Vorarbeit W12.0h).
    ///
    /// <para><b>Kein neuer Renderer.</b> Geprueft wird, dass das Bild ueber
    /// <see cref="ChartRenderer.ErzeugerStapel"/> entsteht — mit Sekundaerachse fuer
    /// den Ladezustand, im Viertelstundenraster ohne Kappung auf 8 760 Punkte, und
    /// deterministisch (dieselbe Reihe, dasselbe Bild). Die Zahl der ChartProben
    /// bleibt damit bei 30.</para>
    ///
    /// <para><b>Die Kultur ist seit #232b auf de-DE gepinnt</b> — THREADGEBUNDEN, im
    /// Konstruktor und mit Rückstellung in <see cref="Dispose"/>: Diese Klasse pinnte bis
    /// dahin gar keine Kultur und fiel deshalb gelegentlich unter
    /// <c>LANG=en_US.UTF-8</c> mit paralleler Sammlungsausführung, wenn der prozessweite
    /// Vorgabewert gerade auf en-US stand (Messbefund #232).</para>
    /// </summary>
    public class PeakShavingBildTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public PeakShavingBildTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        private static double[] Lastgang()
        {
            double[] w = new double[35040];
            for (int i = 0; i < w.Length; i++)
                w[i] = 100.0 + 40.0 * Math.Sin(2.0 * Math.PI * i / 96.0);
            for (int i = 1000; i < 1040; i++) w[i] = 400.0;
            return w;
        }

        private static PeakShavingErgebnis Ergebnis()
        {
            SpeicherParameter p = new SpeicherParameter
            {
                CNomKwh = 200.0,
                PKw = 100.0,
                SoCMinKwh = 20.0,
                SoCMaxKwh = 180.0,
                RoundTripWirkungsgrad = 0.9,
                StartSoCKwh = 20.0,
                DtH = 0.25,
                CCapEurProKwh = 400.0,
                CPowEurProKw = 200.0,
                IFixEur = 1000.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 15.0,
                DegradationProA = 0.0
            };
            PeakShavingParameter ps = new PeakShavingParameter
            {
                PZielKw = 300.0,
                Adaptiv = false,
                LeistungspreisEurProKwA = 120.0,
                BezugspreisMittelCtKwh = 25.0
            };
            return new PeakShaving(ps, SpeicherModus.Energetisch)
                       .BerechnePeakShaving(Lastgang(), p);
        }

        [Fact]
        public void Ohne_Ergebnis_entsteht_kein_Bild()
        {
            Assert.Null(PeakShavingBild.Lastgang(null, true));
        }

        /// <summary>Ein PNG, und zwar ein richtiges: die vier Kennbytes stehen vorn.</summary>
        [Fact]
        public void Das_Bild_ist_ein_PNG()
        {
            byte[] png = PeakShavingBild.Lastgang(Ergebnis(), false);

            Assert.NotNull(png);
            Assert.True(png.Length > 1000, "Das Bild ist verdaechtig klein.");
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }

        /// <summary>
        /// 1 240 x 560 — dasselbe Mass wie <c>GanglinieNormiert</c> und
        /// <c>Temperaturverlauf</c>. Die Groesse steht in den Bytes 16 bis 23 des
        /// IHDR-Blocks, jeweils als 32-Bit-Zahl mit hoechstwertigem Byte zuerst.
        /// </summary>
        [Fact]
        public void Das_Bild_misst_1240_auf_560()
        {
            byte[] png = PeakShavingBild.Lastgang(Ergebnis(), true);

            int breite = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int hoehe = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

            Assert.Equal(1240, breite);
            Assert.Equal(560, hoehe);
        }

        /// <summary>
        /// Der Schalter „Ladezustand im Diagramm zeigen" aendert das Bild — sonst
        /// waere die Sekundaerachse wirkungslos.
        /// </summary>
        [Fact]
        public void Der_Ladezustand_aendert_das_Bild()
        {
            PeakShavingErgebnis r = Ergebnis();

            byte[] ohne = PeakShavingBild.Lastgang(r, false);
            byte[] mit = PeakShavingBild.Lastgang(r, true);

            Assert.NotEqual(ohne.Length, mit.Length);
        }

        /// <summary>
        /// Welle RZ: Ohne die zwei zuschaltbaren Reihen ist das Modell dasselbe wie bisher — Bit fuer Bit im PNG,
        /// mit und ohne Ladezustand.
        /// </summary>
        [Fact]
        public void Ohne_Zusatzreihen_bleibt_das_Bild_byte_gleich()
        {
            PeakShavingErgebnis r = Ergebnis();
            foreach (bool soc in new[] { false, true })
                Assert.Equal(PeakShavingBild.Lastgang(r, soc),
                             WindowsFormsApplication1.Zeichnung.SkiaMaler.Png(PeakShavingBild.Modell(r, soc, false, false)));
        }

        /// <summary>
        /// Welle RZ: Lade- und Entladeleistung stehen als zwei Linien im Bild, die Schwelle als eine — je nur
        /// gewählt. Die Leistung ist Energie je Intervall durch die Intervalldauer (0,25 h).
        /// </summary>
        [Fact]
        public void Leistung_und_Schwelle_stehen_nur_gewaehlt_im_Bild()
        {
            PeakShavingErgebnis r = Ergebnis();
            string[] Namen(WindowsFormsApplication1.Zeichnung.Zeichenmodell m)
                => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(m.Reihen, x => x.Name));

            string[] ohne = Namen(PeakShavingBild.Modell(r, false, false, false));
            string[] leistung = Namen(PeakShavingBild.Modell(r, false, true, false));
            string[] schwelle = Namen(PeakShavingBild.Modell(r, false, false, true));

            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_LADELEISTUNG, ohne);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_SCHWELLE, ohne);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_LADELEISTUNG, leistung);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_ENTLADELEISTUNG, leistung);
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_SCHWELLE, leistung);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.PEAK_SERIE_SCHWELLE, schwelle);
            Assert.Equal(ohne.Length + 2, leistung.Length);
            Assert.Equal(ohne.Length + 1, schwelle.Length);

            double[] lade = PeakShavingBild.LadeleistungKw(r);
            double[] entlade = PeakShavingBild.EntladeleistungKw(r);
            Assert.Equal(r.LadungAcKwh.Length, lade.Length);
            for (int i = 0; i < lade.Length; i++)
            {
                Assert.Equal(r.LadungAcKwh[i] / 0.25, lade[i], 9);
                Assert.Equal(r.EntladungAcKwh[i] / 0.25, entlade[i], 9);
            }
            Assert.True(System.Linq.Enumerable.Max(entlade) > 0.0, "Der Speicher kappt die Spitze.");
            Assert.All(PeakShavingBild.SchwelleKw(r), w => Assert.Equal(r.ErreichteSchwelleKw, w));
        }

        /// <summary>
        /// Welle RZ: Die CSV der Huelle haengt die gewaehlten Reihen hinter die fuenf Spalten des Vorlaeufers;
        /// ohne Wahl bleiben es genau die fuenf.
        /// </summary>
        [Fact]
        public void Die_CSV_Spalten_folgen_der_Wahl()
        {
            PeakShavingErgebnis r = Ergebnis();
            string[] Kopf(EPOS.UI.Dialoge.Strom.PeakShavingBildwahl w)
                => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(PeakShavingHuelle.CsvSpalten(r, w), s => s.Name));

            string[] fuenf = Kopf(new EPOS.UI.Dialoge.Strom.PeakShavingBildwahl(true));
            Assert.Equal(5, fuenf.Length);
            Assert.Equal(fuenf, Kopf(null));
            Assert.Equal(7, Kopf(new EPOS.UI.Dialoge.Strom.PeakShavingBildwahl(false, true)).Length);
            Assert.Equal(6, Kopf(new EPOS.UI.Dialoge.Strom.PeakShavingBildwahl(false, false, true)).Length);
            string[] alle = Kopf(new EPOS.UI.Dialoge.Strom.PeakShavingBildwahl(false, true, true));
            Assert.Equal(new[]
            {
                WindowsFormsApplication1.MyResource.Resource.PEAK_CSV_LADELEISTUNG,
                WindowsFormsApplication1.MyResource.Resource.PEAK_CSV_ENTLADELEISTUNG,
                WindowsFormsApplication1.MyResource.Resource.PEAK_CSV_SCHWELLE
            }, alle[5..]);
        }

        /// <summary>Dieselbe Reihe, dasselbe Bild — Bit fuer Bit.</summary>
        [Fact]
        public void Dasselbe_Ergebnis_liefert_dasselbe_Bild()
        {
            PeakShavingErgebnis r = Ergebnis();

            Assert.Equal(PeakShavingBild.Lastgang(r, true), PeakShavingBild.Lastgang(r, true));
        }

        /// <summary>
        /// Die drei Reihen nennen ihre FARBROLLE (DG-E5), und deren Hausfarbe ist die
        /// des Bildes. Damit trägt jeder Legendeneintrag ein Farbfeld — und ein
        /// Anwender, der den Ladezustand umfärbt, färbt ihn in jedem Bild um.
        /// </summary>
        [Fact]
        public void Die_drei_Reihen_tragen_ihre_Farbrolle()
        {
            Assert.Equal(WindowsFormsApplication1.Zeichnung.Farbrolle.NETZ_OHNE_SPEICHER,
                         PeakShavingBild.RolleAlt);
            Assert.Equal(WindowsFormsApplication1.Zeichnung.Farbrolle.NETZ_MIT_SPEICHER,
                         PeakShavingBild.RolleNeu);
            Assert.Equal(WindowsFormsApplication1.Zeichnung.Farbrolle.SPEICHERFUELLSTAND,
                         PeakShavingBild.RolleSoC);
            Assert.Equal(PeakShavingBild.RolleSoC, SpeicherBetriebsbild.RolleSoC);

            var vorgabe = WindowsFormsApplication1.Zeichnung.Farbpalette.Vorgabe;
            Assert.Equal(new WindowsFormsApplication1.Zeichnung.Farbe(190, 90, 90),
                         vorgabe[PeakShavingBild.RolleAlt]);
            Assert.Equal(new WindowsFormsApplication1.Zeichnung.Farbe(40, 110, 180),
                         vorgabe[PeakShavingBild.RolleNeu]);
            Assert.Equal(new WindowsFormsApplication1.Zeichnung.Farbe(120, 130, 140),
                         vorgabe[PeakShavingBild.RolleSoC]);
        }
    }
}
