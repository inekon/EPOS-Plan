using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Stundenschritt, ergebnisneutral erweitert (Stufe G6b, Welle W3)</b> — Fallfolge und
    /// Abschnittsdauern gibt <see cref="Zonenmodell2K.Schritt"/> jetzt für JEDE Stunde heraus, und
    /// <see cref="Zonenmodell2K.SchrittMitMuster"/> rechnet eine Stunde mit festem Muster nach
    /// (Auftrag G6b, W3; die Zonenschleife hält damit in W4 das Muster eines ersten Durchlaufs fest).
    /// Die Probe: Mit dem Muster, das <see cref="Zonenmodell2K.Schritt"/> gefunden hat, ist die
    /// nachgerechnete Stunde Bit für Bit dieselbe — über ein ganzes Jahr der Fälle des
    /// Einzonennetzes (W0), mit Heiz-, Kühl- und Grenzfällen und Umschaltstunden.
    /// </summary>
    public class SchrittMusterTests
    {
        [Theory]
        [InlineData(GebaeudeEinzonennetzTests.IDEAL)]
        [InlineData(GebaeudeEinzonennetzTests.KUEHLUNG_IDEAL)]
        [InlineData(GebaeudeEinzonennetzTests.LEISTUNGSGRENZE)]
        [InlineData(GebaeudeEinzonennetzTests.SOMMERLUEFTUNG)]
        public void Die_Stunde_mit_ihrem_eigenen_Muster_ist_bitgleich(string fall)
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(fall);
            var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            m.Zuruecksetzen(e.ThetaSoll[0]);
            int mehrere = 0, grenze = 0, kuehlen = 0;
            for (int h = 0; h < 8760; h++)
            {
                Stundenrand r = e.Rand(h, e.Sommerlueftung && h % 24 >= 12);
                double aw = m.ThetaMAw, iw = m.ThetaMIw;
                Stundenergebnis s = m.Schritt(in r);
                Stundenmuster muster = m.LetztesMuster;
                Assert.Equal(s.Abschnitte, muster.Anzahl);
                Assert.True(muster.Anzahl >= 1);
                for (int i = 0; i < muster.Anzahl; i++) Assert.True(muster.Dauer[i] > 0.0);
                if (muster.Anzahl > 1) mehrere++;
                foreach (Betriebsfall f in muster.Folge)
                {
                    if (f == Betriebsfall.Heizgrenze) grenze++;
                    if (f == Betriebsfall.KuehlenGeregelt) kuehlen++;
                }

                double awEnde = m.ThetaMAw, iwEnde = m.ThetaMIw;
                m.Zuruecksetzen(aw, iw);
                Stundenergebnis n = m.SchrittMitMuster(in r, muster);
                Gleich(s, n, h);
                Assert.Equal(Bits(awEnde), Bits(m.ThetaMAw));
                Assert.Equal(Bits(iwEnde), Bits(m.ThetaMIw));
                Assert.Equal(muster.Anzahl, m.LetzteFallfolge.Length);
            }

            // Jeder Fall tut, was sein Name verlangt: Umschaltstunden, die Grenze, die Kühlung.
            Assert.True(mehrere > 0, fall + ": keine Stunde mit mehreren Abschnitten.");
            if (fall == GebaeudeEinzonennetzTests.LEISTUNGSGRENZE) Assert.True(grenze > 0, "Die Leistungsgrenze greift nie.");
            if (fall == GebaeudeEinzonennetzTests.KUEHLUNG_IDEAL) Assert.True(kuehlen > 0, "Die Kühlung greift nie.");
        }

        [Fact]
        public void Auch_eine_ideale_Stunde_gibt_ihre_Fallfolge_heraus()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            Assert.Empty(m.LetzteFallfolge);
            m.Zuruecksetzen(e.ThetaSoll[0]);
            Stundenergebnis s = m.Schritt(e.Rand(0));
            Assert.Equal(s.Abschnitte, m.LetzteFallfolge.Length);
            Assert.Equal(s.Abschnitte, m.LetzteAbschnittsdauern.Length);
        }

        [Fact]
        public void Eine_Stunde_mit_Uebergabe_laesst_sich_nicht_nachrechnen()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.AK1_HEIZSEITE);
            Assert.True(e.KopplungWirksam);
            var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            m.Zuruecksetzen(e.ThetaSoll[0]);
            Stundenrand r = e.Rand(0);
            m.Schritt(in r);
            Stundenmuster muster = m.LetztesMuster;
            double aw = m.ThetaMAw, iw = m.ThetaMIw;
            Assert.Throws<ArgumentException>(() => m.SchrittMitMuster(in r, muster));
            Assert.Equal(Bits(aw), Bits(m.ThetaMAw));
            Assert.Equal(Bits(iw), Bits(m.ThetaMIw));
        }

        [Fact]
        public void Ein_Muster_muss_die_Stunde_genau_fuellen()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            m.Zuruecksetzen(e.ThetaSoll[0]);
            Stundenrand r = e.Rand(0);
            double aw = m.ThetaMAw, iw = m.ThetaMIw;
            Assert.Throws<ArgumentException>(() => m.SchrittMitMuster(in r,
                new Stundenmuster(new[] { Betriebsfall.Totband }, new[] { 1800.0 })));
            Assert.Throws<ArgumentException>(() => m.SchrittMitMuster(in r,
                new Stundenmuster(new[] { Betriebsfall.Totband, Betriebsfall.HeizenGeregelt }, new[] { 1800.0, 2400.0 })));
            Assert.Throws<ArgumentException>(() => m.SchrittMitMuster(in r,
                new Stundenmuster(new[] { Betriebsfall.UebergabeGesaettigt }, new[] { 3600.0 })));
            Assert.Throws<ArgumentException>(() => new Stundenmuster(new[] { Betriebsfall.Totband }, new double[0]));
            Assert.Equal(Bits(aw), Bits(m.ThetaMAw));
            Assert.Equal(Bits(iw), Bits(m.ThetaMIw));

            // Die ganze Stunde in einem Abschnitt: rechnet.
            Stundenergebnis s = m.SchrittMitMuster(in r, new Stundenmuster(new[] { Betriebsfall.Totband }, new[] { 3600.0 }));
            Assert.Equal(1, s.Abschnitte);
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static void Gleich(Stundenergebnis a, Stundenergebnis b, int h)
        {
            string w = "Stunde " + h;
            Assert.True(Bits(a.HeizleistungW) == Bits(b.HeizleistungW), w + ": Heizleistung");
            Assert.True(Bits(a.KuehlleistungW) == Bits(b.KuehlleistungW), w + ": Kühlleistung");
            Assert.True(Bits(a.ThetaAirMittel) == Bits(b.ThetaAirMittel), w + ": Raumluft");
            Assert.True(Bits(a.ThetaOpMittel) == Bits(b.ThetaOpMittel), w + ": operative Temperatur");
            Assert.True(Bits(a.ThetaSAwMittel) == Bits(b.ThetaSAwMittel), w + ": Oberfläche AW");
            Assert.True(Bits(a.ThetaSIwMittel) == Bits(b.ThetaSIwMittel), w + ": Oberfläche IW");
            Assert.True(Bits(a.ThetaMAwMittel) == Bits(b.ThetaMAwMittel), w + ": Masse AW Mittel");
            Assert.True(Bits(a.ThetaMIwMittel) == Bits(b.ThetaMIwMittel), w + ": Masse IW Mittel");
            Assert.True(Bits(a.ThetaMAwEnde) == Bits(b.ThetaMAwEnde), w + ": Masse AW Ende");
            Assert.True(Bits(a.ThetaMIwEnde) == Bits(b.ThetaMIwEnde), w + ": Masse IW Ende");
            Assert.Equal(a.Abschnitte, b.Abschnitte);
        }
    }
}
