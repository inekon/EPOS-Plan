using System;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Altweg;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wochenend- und Feriensollwert</b> (Auftrag #571): Beide sind ABSOLUTE Raumsolltemperaturen,
    /// gelten ganztägig — auch nachts — und 0 heißt „keine Absenkung", nie 0 °C. Das gilt für den
    /// VDI-Weg (Sollwertfahrplan über <see cref="Waermeuebergabevorgaben.Bestandswoche"/>) und für den
    /// Tagesbilanz-Weg (<see cref="TagesbilanzPhysik.TaeglHeizlastWG"/>); die Herleitungszeile des
    /// Gebäudedialogs sagt denselben Satz.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SollwertAbsenkungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // Wochenstunde: Montag 00:00 = 0; Samstag = Tag 5.
        private const int MO_03 = 3, MO_12 = 12, SA_03 = 5 * 24 + 3, SA_12 = 5 * 24 + 12;

        [Fact]
        public void Null_am_Wochenende_rechnet_wie_die_Werktage_nicht_mit_0_Grad()
        {
            double[] woche = Waermeuebergabevorgaben.Bestandswoche(20, 18, 0);
            Assert.Equal(20, woche[SA_12]);
            Assert.Equal(18, woche[SA_03]);
            Assert.False(Gebaeudemodellvorgaben.WochenendsollwertWirksam(0));
        }

        [Fact]
        public void Der_Wochenendwert_ist_absolut_und_gilt_ganztaegig_auch_nachts()
        {
            double[] woche = Waermeuebergabevorgaben.Bestandswoche(20, 18, 16);
            Assert.Equal(16, woche[SA_12]);
            Assert.Equal(16, woche[SA_03]);
            Assert.Equal(20, woche[MO_12]);
            Assert.Equal(18, woche[MO_03]);
        }

        [Fact]
        public void Die_Schwellen_sind_die_des_Laufs()
        {
            Assert.False(Gebaeudemodellvorgaben.WochenendsollwertWirksam(5));
            Assert.True(Gebaeudemodellvorgaben.WochenendsollwertWirksam(5.5));
            Assert.False(Gebaeudemodellvorgaben.FeriensollwertWirksam(0));
            Assert.False(Gebaeudemodellvorgaben.FeriensollwertWirksam(0.5));
            Assert.True(Gebaeudemodellvorgaben.FeriensollwertWirksam(1));
        }

        /// <summary>
        /// Tagesbilanz: Ein Wochenendtag mit 16 °C rechnet wie ein Werktag mit Tag- UND Nachtwert 16 °C —
        /// der Wert ist absolut und gilt alle 24 Stunden (gleicher Anfangszustand: Nachtwert 16 °C).
        /// </summary>
        [Fact]
        public void Tagesbilanz_Wochenendwert_ist_absolut_und_ganztaegig()
        {
            double we = Tag(weAbsenkung: 1, weTemp: 16, tag: 20, nacht: 16);
            double gleich = Tag(weAbsenkung: 0, weTemp: 0, tag: 16, nacht: 16);
            Assert.Equal(gleich, we, 9);
            Assert.NotEqual(Tag(weAbsenkung: 0, weTemp: 0, tag: 20, nacht: 16), we);
        }

        private static double Tag(int weAbsenkung, double weTemp, double tag, double nacht)
        {
            var z = new Tagesbilanzzustand();
            z.ResetState();
            TagesbilanzPhysik.TaeglHeizlastWG(z, 1, weAbsenkung, weTemp, 0, 0, tag, nacht, 200, 0, 150, 5000, -5, 26, 120, 120);
            return TagesbilanzPhysik.TaeglHeizlastWG(z, 2, weAbsenkung, weTemp, 0, 0, tag, nacht, 200, 0, 150, 5000, -5, 26, 120, 120);
        }

        [Fact]
        public void Die_Herleitungszeile_sagt_den_gerechneten_Fahrplan()
        {
            string ohne = Gebaeudemodellvorgaben.Sollwertzeile(20, 18, 0, 0, false, null, null, true);
            Assert.Contains("Werktags gilt 20 °C, nachts von 22 bis 6 Uhr 18 °C.", ohne);
            Assert.Contains("Das Wochenende rechnet wie die Werktage", ohne);
            Assert.Contains("Keine Ferienabsenkung", ohne);

            string mit = Gebaeudemodellvorgaben.Sollwertzeile(20, 18, 16, 12, true, 23, 5, true);
            Assert.Contains("nachts von 23 bis 5 Uhr 18 °C", mit);
            Assert.Contains("ganztägig 16 °C, auch nachts", mit);
            Assert.Contains("In den Ferienzeiträumen gilt ganztägig 12 °C", mit);

            // Ferienwert ohne Zeitraum: keine Ferienabsenkung.
            Assert.Contains("Keine Ferienabsenkung", Gebaeudemodellvorgaben.Sollwertzeile(20, 18, 16, 12, false, null, null, true));
        }

        [Fact]
        public void Tagesbilanz_rechnet_die_Nacht_fest_in_der_Vorgabe()
        {
            string z = Gebaeudemodellvorgaben.Sollwertzeile(20, 18, 0, 0, false, 23, 5, false);
            Assert.Contains("nachts von 22 bis 6 Uhr", z);
            Assert.Contains("Der Rechenweg Tagesbilanz rechnet die Nacht fest von 22 bis 6 Uhr.", z);
            Assert.DoesNotContain("fest von", Gebaeudemodellvorgaben.Sollwertzeile(20, 18, 0, 0, false, null, null, false));
        }
    }
}
