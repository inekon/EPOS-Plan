using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Proben der Welle AK3-K-K1</b> (Entwurf AK3-K 1.1, Festlegungen 9, 16, 21) am Referenzprojekt 1058 (Stufe AK3,
    /// reversible Wärmepumpe mit Kühlbetrieb). Alle Proben schalten den <see cref="Ak3KKernschalter"/> ein; ohne Schalter
    /// rechnet 1058 wie in der Basis R42 (Gate: Referenzlauf byte-gleich).
    /// <list type="bullet">
    /// <item><b>Kühlkanal = Kreisreihe, Bedarfsprobe grün</b> (Fehler 1.1 (a)): Kühlkanal, <c>Kaeltebedarf</c>,
    /// <c>Kaeltebedarf_Gebaeude</c> und Jahressumme folgen der Kühlreihe des Steppers; die Kaskade deckt dieselbe Reihe.</item>
    /// <item><b>Heizsperre am Kühltag in der Wärmeschranke</b> (Fehler 1.1 (b), K8a): an einem Kühltag bietet die
    /// reversible Wärmepumpe keine Heizleistung an, Grund <see cref="Verfuegbarkeitsgrund.Umschaltung"/>.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3KaelteKreisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public Ak3KaelteKreisTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1058;

        private static SimulationRunner Rechnen(int projekt, bool schalter)
        {
            using (Ak3KKernschalter.Schalten(schalter))
            {
                SimulationProtokoll.NeuStarten();
                var r = new SimulationRunner();
                bool ok = r.SimuliereUndSpeichere(projekt, out string fehler) > 0;
                Assert.True(ok, "Lauf " + projekt + " gescheitert: " + fehler);
                Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
                return r;
            }
        }

        [Fact]
        public void Mit_Schalter_folgt_der_Kuehlkanal_der_Kreisreihe_und_die_Bedarfsprobe_bleibt_gruen()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT, true);
            SimulationWaermebedarf w = r.simulation_Waermebedarf;
            Assert.NotNull(w.Ak3);
            SimulationKaeltebedarf k = w.Kaelteseite;
            Assert.True(k.Gerechnet);
            GebaeudeModellErgebnis e = w.GebaeudeErgebnisse.Ergebnis(0);
            Assert.NotNull(e.KuehlbedarfKwh);

            int abweichend = 0;
            for (int h = 0; h < 8760; h++)
                if (Math.Abs(k.Kaeltebedarf_Gebaeude[h] - e.KuehlbedarfKwh[h]) > 1e-9) abweichend++;
            _aus.WriteLine("1058 mit Schalter: Gebäude {0:0.00000} MWh, Kaeltebedarf_Gesamt {1:0.00000} MWh, abweichende Stunden {2}",
                           e.KuehlenergieMwh, k.Kaeltebedarf_Gesamt, abweichend);
            Assert.Equal(0, abweichend);
            Assert.Equal(e.KuehlenergieMwh.Value, k.Kaeltebedarf_Gesamt, 9);
            Assert.Equal(e.KuehlenergieMwh.Value, k.Kaeltebedarf_Gebaeude_Gesamt, 9);
            Assert.Equal(0, k.Bedarfsprobe_Verletzungen);

            double max = 0.0;
            for (int h = 0; h < 8760; h++) max = Math.Max(max, k.Kaeltebedarf[h]);
            Assert.Equal(max, k.Kaeltebedarf_Max);

            // Die Kaskade deckt die Reihe des Kreises (Zeichen für Zeichen dieselbe Reihe).
            Assert.NotNull(k.Kaskade);
            for (int h = 0; h < 8760; h++)
                Assert.True(k.Kaskade.Bedarf_stuendlich[h].Equals(k.Kaeltebedarf[h] > 0 ? k.Kaeltebedarf[h] : 0.0), "Stunde " + h);
        }

        /// <summary>
        /// <b>Kältestunde bitgleich zum Jahreslauf</b> (Festlegung 16): Je Kälteprojekt ohne Schalter und für 1058 mit
        /// Schalter (Kältestunde im Kreis nach der Wärmestunde) rechnet die Kaskade des Laufs ein zweites Mal als Jahreslauf
        /// über den Kältebedarf des Laufs — Deckung, Rest, Strom, Speicher und Erzeugerreihen Zeichen für Zeichen gleich.
        /// </summary>
        [Theory]
        [InlineData(1017, false)]
        [InlineData(1047, false)]
        [InlineData(1055, false)]
        [InlineData(1056, false)]
        [InlineData(1058, false)]
        [InlineData(1058, true)]
        public void Kaeltestunde_bitgleich_zum_Jahreslauf(int projekt, bool schalter)
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(projekt, schalter);
            SimulationKaeltebedarf k = r.simulation_Waermebedarf.Kaelteseite;
            Kaeltekaskade kaskade = k.Kaskade;
            Assert.NotNull(kaskade);
            Assert.Equal(8760, kaskade.NaechsteStunde);
            Assert.Equal(schalter, kaskade.ImKreis);
            double[][] vorher =
            {
                (double[])kaskade.Bedarf_stuendlich.Clone(), (double[])kaskade.Deckung_stuendlich.Clone(),
                (double[])kaskade.Rest_stuendlich.Clone(), (double[])kaskade.Stromverbrauch_Kuehlung_stuendlich.Clone(),
                (double[])kaskade.Speicherentladung_stuendlich.Clone(), (double[])kaskade.Speicherladung_stuendlich.Clone(),
            };
            double[][] erzeuger = kaskade.Erzeuger.SelectMany(e => new[] { (double[])e.Kaelte_stuendlich.Clone(), (double[])e.Strom_stuendlich.Clone() }).ToArray();
            double deckung = kaskade.DeckungGesamtKwh, strom = kaskade.StromGesamtKwh;

            kaskade.Rechnen(k.Kaeltebedarf, r.sim.simulation_wp.Extrapolation_Erlaubt);

            double[][] nachher =
            {
                kaskade.Bedarf_stuendlich, kaskade.Deckung_stuendlich, kaskade.Rest_stuendlich,
                kaskade.Stromverbrauch_Kuehlung_stuendlich, kaskade.Speicherentladung_stuendlich, kaskade.Speicherladung_stuendlich,
            };
            for (int i = 0; i < vorher.Length; i++) Assert.Equal(vorher[i], nachher[i]);
            double[][] erzeugerNach = kaskade.Erzeuger.SelectMany(e => new[] { e.Kaelte_stuendlich, e.Strom_stuendlich }).ToArray();
            for (int i = 0; i < erzeuger.Length; i++) Assert.Equal(erzeuger[i], erzeugerNach[i]);
            Assert.Equal(deckung, kaskade.DeckungGesamtKwh);
            Assert.Equal(strom, kaskade.StromGesamtKwh);
            _aus.WriteLine("{0} Schalter {1}: Deckung {2:0.000} MWh, Strom {3:0.000} MWh, Rest {4:0.000} MWh — bitgleich",
                           projekt, schalter ? "ein" : "aus", deckung / 1000.0, strom / 1000.0, kaskade.RestGesamtKwh / 1000.0);
        }

        [Fact]
        public void Auf_AK3_entfaellt_der_Satz_gebaut_ist_AK1_und_der_Kreis_nennt_die_Kaelteseite()
        {
            if (!_db.Vorhanden) return;
            string nichtGebaut = string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_AK_STUFE_NICHT_GEBAUT,
                                               DbWerte.ANLAGENKOPPLUNG_AK3);
            Rechnen(PROJEKT, false);
            var ohne = SimulationProtokoll.Aktuell.Hinweise.ToList();
            Assert.DoesNotContain(nichtGebaut, ohne);
            Assert.Contains(ohne, t => t.StartsWith("Anlagenkopplung AK3 (Kernstufe)", StringComparison.Ordinal));
            Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK3K_KREIS_MIT_KAELTESEITE, ohne);

            Rechnen(PROJEKT, true);
            var mit = SimulationProtokoll.Aktuell.Hinweise.ToList();
            Assert.DoesNotContain(nichtGebaut, mit);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK3K_KREIS_MIT_KAELTESEITE, mit);
        }

        [Fact]
        public void Mit_Schalter_bietet_die_reversible_Waermepumpe_am_Kuehltag_keine_Heizleistung_an()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT, true);
            Anlagenkopplung kreis = r.simulation_Waermebedarf.Ak3.Kreis;
            Assert.NotNull(kreis);
            WaermepumpeKapazitaet wp = kreis.Erzeuger.OfType<WaermepumpeKapazitaet>().Single();
            SimulationWaermepumpe modul = r.sim.simulation_wp;
            Assert.NotNull(modul.Kuehltage);

            int kuehlstunden = 0, angeboten = 0, umgeschaltet = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (!modul.HeizkanalGesperrt(0, h)) continue;
                kuehlstunden++;
                Erzeugerangebot a = wp.Abfragen(h, double.NaN);
                if (a.VerfuegbarKw > 0.0) angeboten++;
                // Sperrzeit geht vor (sie bindet ohnehin); sonst nennt die Stunde die Umschaltung.
                if (a.Grund == Verfuegbarkeitsgrund.Umschaltung) umgeschaltet++;
                else Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, a.Grund);
            }
            _aus.WriteLine("1058: {0} Stunden an Kühltagen, davon {1} mit Heizangebot der Wärmepumpe, {2} mit Grund Umschaltung",
                           kuehlstunden, angeboten, umgeschaltet);
            Assert.True(kuehlstunden > 0);
            Assert.Equal(0, angeboten);
            Assert.True(umgeschaltet > 0);
        }
    }
}
