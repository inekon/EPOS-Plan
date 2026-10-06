using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Auslegungsheizlast ohne Anlagenkopplung</b> (E97, Befund O2-B1; Entwurf KP3, Festlegung 41): Φ_HL hat EINE
    /// Quelle, <see cref="GebaeudeModellEingang.Auslegungslasten"/>. Mit Kopplung die Zahl des Kopplungswegs, ohne sie
    /// derselbe Ausdruck — Einzone und Zonen Bit für Bit gleich dem gekoppelten Weg, wenn Auslegungspunkt und
    /// Strahlungsanteil gleich sind; ein Feld außerhalb seiner Grenzen gibt ohne Kopplung keine Zahl statt eines Fehlers.
    /// </summary>
    public sealed class AufheizAuslegungslastTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>Das Probegebäude mit Radiator und festem Strahlungsanteil (H12 ändert ihn dann nicht).</summary>
        private static ProjektGebaeudeModel MitUebergabe(double? aussenFeldC = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizung_Strahlungsanteil = 0.3;
            g.Auslegung_Aussentemperatur = aussenFeldC;
            return g;
        }

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, string stufe)
            => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, stufe, double.NaN, 1.0);

        private static double Einzone(GebaeudeModellEingang e)
            => GebaeudeModellEingang.Auslegungslasten(new[] { ZonenEingang.Einzeln(e) })[0];

        private static void Bit(double soll, double ist, string wo)
            => Assert.True(BitConverter.DoubleToInt64Bits(soll) == BitConverter.DoubleToInt64Bits(ist),
                           wo + ": " + soll.ToString("R", CultureInfo.InvariantCulture) + " gegen " +
                           ist.ToString("R", CultureInfo.InvariantCulture));

        [Theory]
        [InlineData(null)]
        [InlineData(-14.0)]
        public void Einzone_ohne_Kopplung_ist_die_Zahl_des_Kopplungswegs(double? aussenFeldC)
        {
            GebaeudeModellEingang gekoppelt = Eingang(MitUebergabe(aussenFeldC), DbWerte.ANLAGENKOPPLUNG_AK1);
            GebaeudeModellEingang frei = Eingang(MitUebergabe(aussenFeldC), null);
            Assert.True(gekoppelt.KopplungWirksam);
            Assert.False(frei.KopplungWirksam);
            Assert.True(gekoppelt.AuslegungsheizlastW > 0.0);
            Assert.True(double.IsNaN(frei.AuslegungsheizlastW));   // der Eingang selbst bleibt ohne Kopplung unverändert

            Bit(gekoppelt.AuslegungsheizlastW, Einzone(gekoppelt), "gekoppelt");
            Bit(gekoppelt.AuslegungsheizlastW, Einzone(frei), "ohne Kopplung");
            Bit(Einzone(frei), Aufheizzone.Aus(ZonenEingang.Einzeln(frei)).AuslegungsheizlastW, "Aufheizzone");
        }

        [Fact]
        public void Ohne_Kopplung_gibt_ein_Feld_ausserhalb_der_Grenzen_keine_Zahl()
        {
            double hergeleitet = Einzone(Eingang(MitUebergabe(), null));
            double kalt = Einzone(Eingang(MitUebergabe(-14.0), null));
            Assert.True(kalt > hergeleitet, "−14 °C trägt mehr Last als das hergeleitete Tagesmittel");
            Assert.True(double.IsNaN(Einzone(Eingang(MitUebergabe(GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN - 10.0), null))));
            // Gekoppelt bleibt es der benannte Fehler.
            Assert.Throws<GebaeudeModellException>(
                () => Eingang(MitUebergabe(GebaeudeFestwerte.AUSLEGUNG_AUSSEN_MIN - 10.0), DbWerte.ANLAGENKOPPLUNG_AK1));
        }

        [Fact]
        public void Zonen_ohne_Kopplung_sind_die_Zahlen_des_Kopplungswegs()
        {
            ProjektGebaeudeModel G()
            {
                ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
                g.Heizkreis_Aktiv = true;
                g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                g.Heizung_Strahlungsanteil = 0.3;
                return g;
            }
            GebaeudeKlima klima = AufheizMehrzonenTests.Klima();
            IReadOnlyList<ZonenEingang> gekoppelt = ZonenEingang.Bauen(G(), klima, anlagenkopplung: DbWerte.ANLAGENKOPPLUNG_AK1);
            IReadOnlyList<ZonenEingang> frei = ZonenEingang.Bauen(G(), klima);
            double[] lastG = GebaeudeModellEingang.Auslegungslasten(gekoppelt);
            double[] lastF = GebaeudeModellEingang.Auslegungslasten(frei);
            Aufheizzone[] az = Aufheizzone.AusZonen(frei);
            int beheizt = 0;
            foreach (int id in new[] { AufheizMehrzonenTests.WOHNUNG_1, AufheizMehrzonenTests.WOHNUNG_2 })
            {
                int g = AufheizMehrzonenTests.Stelle(gekoppelt, id), f = AufheizMehrzonenTests.Stelle(frei, id);
                Assert.True(gekoppelt[g].Eingang.KopplungWirksam);
                Assert.False(frei[f].Eingang.KopplungWirksam);
                Assert.True(lastG[g] > 0.0);
                Bit(gekoppelt[g].Eingang.AuslegungsheizlastW, lastG[g], "Zone " + id + ", gekoppelt");
                Bit(lastG[g], lastF[f], "Zone " + id + ", ohne Kopplung");
                Bit(lastF[f], az[f].AuslegungsheizlastW, "Zone " + id + ", Aufheizzone");
                beheizt++;
            }
            Assert.Equal(2, beheizt);
            Assert.True(double.IsNaN(lastF[AufheizMehrzonenTests.Stelle(frei, AufheizMehrzonenTests.KELLER)]));
        }
    }
}
