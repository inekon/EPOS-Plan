using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Komfortkennzahlen ohne Datenbank</b> (Anlagenkopplung AK2-2b; 5.5, 7, 11.1): Schwelle, Nutzungszeit,
    /// Zonen- und Projektzusammenfassung, Kälteseite spiegelbildlich, NULL-Regel der Projektspalten und die Probe
    /// „Sperrzeit erzeugt Unterschreitung" am gekoppelten Probegebäude.
    /// </summary>
    public sealed class KomfortkennzahlenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KomfortkennzahlenTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static double[] Reihe(Func<int, double> f) => Enumerable.Range(0, 8760).Select(f).ToArray();

        private static bool[] Maske(Func<int, bool> f) => Enumerable.Range(0, 8760).Select(f).ToArray();

        [Fact]
        public void Komfortkennzahlen_Schwelle_Kelvinstunden_und_laengste_Strecke()
        {
            // Sollwert 20 °C; Raumluft wechselt in Blöcken zwischen 20, 19,5 und 17,5 °C.
            double[] soll = Reihe(h => 20.0);
            double[] luft = Reihe(h => (h % 50) < 5 ? 17.5 : (h % 50) < 20 ? 19.5 : 20.0);

            Komfortkennzahlen eins = Komfortkennzahlen.AusReihen(soll, luft, null, 1.0);
            Komfortkennzahlen null0 = Komfortkennzahlen.AusReihen(soll, luft, null, 0.0);

            // 175 volle Blöcke und ein Rest von 10 Stunden: je 5 Stunden mit 2,5 K und 15 Stunden mit 0,5 K.
            Assert.Equal(176 * 5, eins.Stunden);
            Assert.Equal(5, eins.LaengsteStrecke);
            Assert.Equal(eins.Stunden * 2.5, eins.Kelvinstunden, 9);
            Assert.Equal(176 * 5 + 175 * 15 + 5, null0.Stunden);
            Assert.True(null0.Stunden > eins.Stunden, "Schwelle 0 zählt mehr als 1 K");
            Assert.Equal(20, null0.LaengsteStrecke);
            foreach ((Komfortkennzahlen k, double schwelle) in new[] { (eins, 1.0), (null0, 0.0) })
            {
                Assert.True(k.Kelvinstunden >= k.Stunden * schwelle);
                Assert.True(k.LaengsteStrecke <= k.Stunden);
                Assert.Equal(k.Stunden, k.Maske.Count(b => b));
            }
        }

        [Fact]
        public void Unterschreitung_in_der_Nachtabsenkung_und_bei_Heizung_aus_zaehlt_nicht()
        {
            var nacht = Nachtzeit.Vorgabe;
            double[] soll = Reihe(h => nacht.Nutzungszeit(h) ? 20.0 : 16.0);
            // Nachts 14 °C (2 K unter dem abgesenkten Sollwert), tags 20 °C; die Stunde 8 jedes Tages „aus".
            double[] luft = Reihe(h => nacht.Nutzungszeit(h) ? 20.0 : 14.0);
            soll[8] = double.NaN;
            luft[8] = 10.0;
            bool[] nutzung = Maske(h => nacht.Nutzungszeit(h));

            Assert.Equal(0, Komfortkennzahlen.AusReihen(soll, luft, nutzung, 1.0).Stunden);
            // Gegenprobe: ohne Nutzungsmaske zählen die Nachtstunden.
            Komfortkennzahlen ohne = Komfortkennzahlen.AusReihen(soll, luft, null, 1.0);
            Assert.Equal(8760 - Enumerable.Range(0, 8760).Count(h => nacht.Nutzungszeit(h)), ohne.Stunden);
        }

        [Fact]
        public void Zonen_eine_reissende_Zone_zaehlt_die_Kelvinstunden_flaechengewichtet()
        {
            // Zone A (100 m²) reißt in den Stunden 0-9 um 2 K, Zone B (300 m²) in den Stunden 5-14 um 4 K.
            double[] a = Reihe(h => h < 10 ? 2.0 : 0.0);
            double[] b = Reihe(h => h >= 5 && h < 15 ? 4.0 : 0.0);
            Komfortkennzahlen k = Komfortkennzahlen.AusAbweichungen(new[] { a, b }, new[] { 100.0, 300.0 }, 1.0);
            Assert.Equal(15, k.Stunden);
            Assert.Equal(15, k.LaengsteStrecke);
            Assert.Equal(10 * 2.0 * 0.25 + 10 * 4.0 * 0.75, k.Kelvinstunden, 9);

            // Eine Zone: die Rechnung der Zone selbst.
            Komfortkennzahlen eine = Komfortkennzahlen.AusAbweichungen(new[] { a }, new[] { 100.0 }, 1.0);
            Assert.Equal(10, eine.Stunden);
            Assert.Equal(20.0, eine.Kelvinstunden, 9);
        }

        [Fact]
        public void Projekt_vereinigt_die_Stunden_summiert_Kelvinstunden_und_nimmt_die_laengste_Strecke()
        {
            Komfortkennzahlen g1 = Komfortkennzahlen.AusAbweichungen(new[] { Reihe(h => h < 10 ? 2.0 : 0.0) }, null, 1.0);
            Komfortkennzahlen g2 = Komfortkennzahlen.AusAbweichungen(new[] { Reihe(h => h >= 5 && h < 8 ? 3.0 : 0.0) }, null, 1.0);
            Komfortkennzahlen p = Komfortkennzahlen.Projekt(new[] { g1, g2, null });
            Assert.Equal(10, p.Stunden);
            Assert.Equal(20.0 + 9.0, p.Kelvinstunden, 9);
            Assert.Equal(10, p.LaengsteStrecke);
            Assert.Null(Komfortkennzahlen.Projekt(new Komfortkennzahlen[] { null }));
        }

        [Fact]
        public void Kaelteseite_ist_spiegelbildlich()
        {
            double[] luft = Reihe(h => 20.0 + 3.0 * Math.Sin(h * 0.3));
            double[] heizSoll = Reihe(h => 20.0);
            double[] kuehlSoll = Reihe(h => 20.0);
            double[] gespiegelt = luft.Select(t => 40.0 - t).ToArray();
            bool[] nutzung = Maske(h => h % 24 >= 6);
            Komfortkennzahlen kuehl = Komfortkennzahlen.AusReihen(kuehlSoll, luft, nutzung, 1.0, kuehlseite: true);
            Komfortkennzahlen heiz = Komfortkennzahlen.AusReihen(heizSoll, gespiegelt, nutzung, 1.0);
            Assert.True(kuehl.Stunden > 0);
            Assert.Equal(heiz.Stunden, kuehl.Stunden);
            Assert.Equal(heiz.Kelvinstunden, kuehl.Kelvinstunden, 9);
            Assert.Equal(heiz.LaengsteStrecke, kuehl.LaengsteStrecke);
        }

        [Fact]
        public void Projektspalten_bleiben_NULL_ohne_greifende_Schranke()
        {
            var k = new Komfortkennzahlen(3, 4.5, 2);
            var ohne = new ErgebnisEnergiebedarfModel();
            SimulationRunner.AnlagenfahrplanSpaltenSetzen(ohne, 0, k, k);
            Assert.Null(ohne.FahrplanBegrenztStundenH);
            Assert.Null(ohne.KomfortUnterschreitungsstundenH);
            Assert.Null(ohne.KomfortKelvinstundenKh);
            Assert.Null(ohne.KomfortLaengsteStreckeH);
            Assert.Null(ohne.KomfortUeberschreitungsstundenH);
            Assert.Null(ohne.KomfortKelvinstundenKuehlungKh);
            Assert.Null(ohne.KomfortUndRestbedarf);

            var mit = new ErgebnisEnergiebedarfModel { Waermerestbedarf = 1.25 };
            SimulationRunner.AnlagenfahrplanSpaltenSetzen(mit, 7, k, null);
            Assert.Equal(7, mit.FahrplanBegrenztStundenH);
            Assert.Equal(3, mit.KomfortUnterschreitungsstundenH);
            Assert.Equal(4.5, mit.KomfortKelvinstundenKh);
            Assert.Equal(2, mit.KomfortLaengsteStreckeH);
            Assert.Null(mit.KomfortUeberschreitungsstundenH);
            Assert.Null(mit.KomfortKelvinstundenKuehlungKh);
            Assert.Equal((3, 4.5, 2, 1.25), mit.KomfortUndRestbedarf);
        }

        // ---- Probe 11.1 „Sperrzeit erzeugt Unterschreitung" am gekoppelten Probegebäude ----

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>
        /// Das gekoppelte Probegebäude (Radiator, Heizkurve) <b>ohne Nachtabsenkung</b>: Mit Absenkung reißt schon die
        /// Aufheizspitze am Morgen an der Übergabe die Schwelle (AK1, ohne jede Sperre) — die Probe soll allein die
        /// Sperre messen.
        /// </summary>
        private static GebaeudeModellErgebnis Gekoppelt(Anlagenverfuegbarkeit[] verfuegbarkeit)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Nachtabsenkung = g.Raumsolltemperatur_Tag;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            e.Verfuegbarkeit = verfuegbarkeit;
            return Vdi6007Rechenweg.Laufen(e, 0, 1);
        }

        [Fact]
        public void Sperrzeit_erzeugt_Unterschreitung()
        {
            // Kein Speicher: eine achtstündige Sperre 8-16 Uhr an jedem Tag im Januar und Februar, sonst keine Schranke.
            var sperre = new Anlagenverfuegbarkeit[8760];
            for (int h = 0; h < 8760; h++)
            {
                bool gesperrt = h < 59 * 24 && h % 24 >= 8 && h % 24 < 16;
                sperre[h] = gesperrt
                    ? new Anlagenverfuegbarkeit(0.0, double.NaN, Verfuegbarkeitsgrund.Sperrzeit)
                    : new Anlagenverfuegbarkeit(double.NaN, double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung);
            }
            GebaeudeModellErgebnis ohne = Gekoppelt(null);
            GebaeudeModellErgebnis mit = Gekoppelt(sperre);
            Assert.True(GebaeudeKennzahlen.KomfortErhoben(ohne));
            Komfortkennzahlen kOhne = Komfortkennzahlen.Heizseite(ohne);
            Komfortkennzahlen kMit = Komfortkennzahlen.Heizseite(mit);
            _aus.WriteLine($"ohne Sperre: {kOhne.Stunden} h, {kOhne.Kelvinstunden:0.0} Kh, {kOhne.LaengsteStrecke} h");
            _aus.WriteLine($"mit Sperre:  {kMit.Stunden} h, {kMit.Kelvinstunden:0.0} Kh, {kMit.LaengsteStrecke} h");
            Assert.Equal(0, kOhne.Stunden);
            Assert.True(kMit.Stunden > 0, "Sperrzeit erzeugt Komfortstunden");
            Assert.True(kMit.Kelvinstunden >= kMit.Stunden * GebaeudeFestwerte.KOMFORT_SCHWELLE_K);
            Assert.True(kMit.LaengsteStrecke <= 16, "die längste Strecke bleibt in einer Nutzungszeit");
            // Die Unterschreitungen liegen in den Sperrmonaten (ein Tag Nachlauf erlaubt).
            Assert.True(Enumerable.Range(0, 8760).Where(h => kMit.Maske[h]).All(h => h < 60 * 24));

            // Die Kälteseite bleibt ohne wirksame Kühlung unerhoben.
            Assert.Null(Komfortkennzahlen.Kuehlseite(mit));
        }

        [Fact]
        public void Ungekoppeltes_Gebaeude_wird_nicht_erhoben()
        {
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima), 0, 1);
            Assert.False(GebaeudeKennzahlen.KomfortErhoben(bestand));
            ErgebnisGebaeudeModel zeile = GebaeudeKennzahlen.Bilden(0, 1, "Probe", DbWerte.GEBAEUDE_MODELL_VDI6007,
                                                                    new double[8760], bestand);
            Assert.Null(zeile.KomfortUnterschreitungsstundenH);
            Assert.Null(zeile.Bedarfsbegriff);
        }
    }
}
