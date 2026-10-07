using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Proben des Iterationsrahmens AK3</b> (AK3-W3b; Entwurf AK3 2.3, 2.4): der Kreis ohne Datenbank am
    /// gekoppelten Probegebäude (Radiator, Heizkurve).
    /// <list type="bullet">
    /// <item><b>Gate „ohne Grenzen bitgleich zu AK1“</b>: ein Erzeuger ohne wirksame Grenze — jede Stunde ein
    /// Durchlauf, die Gebäudereihen Bit für Bit die des Jahreslaufs.</item>
    /// <item><b>Sperre mit Speicher</b>: Der Speicher überbrückt die Sperre (wieder bitgleich); ohne Speicher liefert
    /// die Sperrstunde nichts, und der Raum kühlt aus.</item>
    /// <item><b>Knappe Schranke</b>: Die Heizleistung hält die Schranke, höchstens drei Durchläufe.</item>
    /// </list>
    /// </summary>
    public class AnlagenkopplungKreisTests
    {
        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>Das gekoppelte Probegebäude (wie <c>GebaeudeStepperTests</c>), ohne Fahrplan.</summary>
        private static GebaeudeModellEingang Gekoppelt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Nachtabsenkung = g.Raumsolltemperatur_Tag;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            return GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
        }

        private static bool[] Sperre()
        {
            var m = new bool[8760];
            for (int h = 0; h < 8760; h++) m[h] = h < 59 * 24 && h % 24 >= 8 && h % 24 < 16;
            return m;
        }

        private sealed class FesterSpeicher : ISpeicherangebot
        {
            private readonly double _kwh;
            internal FesterSpeicher(double kwh) { _kwh = kwh; }
            public bool Vorhanden => true;
            public double EntnehmbarKwh(int stunde) => _kwh;
        }

        private static IErzeugerkapazitaet Kessel(double kw, bool[] gesperrt = null)
            => new FesteKapazitaet(new Fahrplanerzeuger { Bezeichner = "Kessel", NennleistungKw = kw, Gesperrt = gesperrt });

        /// <summary>Das Jahr im Kreis; liefert das unskalierte Ergebnis und den Kreis.</summary>
        private static GebaeudeModellErgebnis ImKreis(IErzeugerkapazitaet erzeuger, ISpeicherangebot speicher,
                                                      out Anlagenkopplung kreis, double faktor = 1.0)
        {
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(Gekoppelt()));
            s.Beginnen();
            kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, faktor) }, new[] { erzeuger }, speicher);
            for (int h = 0; h < 8760; h++)
            {
                kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
            }
            return s.Abschluss(0, 1)[0];
        }

        private static void Bitgleich(GebaeudeModellErgebnis soll, GebaeudeModellErgebnis ist)
        {
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(Bits(soll.HeizlastW[h]) == Bits(ist.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(Bits(soll.Raumtemperatur[h]) == Bits(ist.Raumtemperatur[h]), "Raumtemperatur, Stunde " + h);
                Assert.True(Bits(soll.Heizkreis.VorlaufC[h]) == Bits(ist.Heizkreis.VorlaufC[h]), "Vorlauf, Stunde " + h);
                Assert.True(Bits(soll.Heizkreis.RuecklaufC[h]) == Bits(ist.Heizkreis.RuecklaufC[h]), "Rücklauf, Stunde " + h);
            }
            Assert.Equal(Bits(soll.JahresheizwaermeMwh), Bits(ist.JahresheizwaermeMwh));
        }

        [Fact]
        public void Gate_ohne_Grenzen_ist_der_Kreis_bitgleich_zum_Jahreslauf_AK1()
        {
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1);
            GebaeudeModellErgebnis ist = ImKreis(Kessel(1.0e6), null, out Anlagenkopplung kreis);
            Bitgleich(soll, ist);
            Assert.Equal(8760, kreis.DurchlaeufeVerteilung[1]);
            Assert.Equal(0, kreis.StundenAnDerSchranke);
            Assert.Equal(1.0, kreis.DurchlaeufeMittel);
        }

        [Fact]
        public void Sperre_mit_Speicher_ueberbrueckt_die_Sperre_ohne_Speicher_kuehlt_der_Raum_aus()
        {
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1);
            Bitgleich(soll, ImKreis(Kessel(1.0e6, Sperre()), new FesterSpeicher(1.0e6), out Anlagenkopplung mit));
            Assert.Equal(0, mit.StundenAnDerSchranke);

            GebaeudeModellErgebnis ohne = ImKreis(Kessel(1.0e6, Sperre()), null, out Anlagenkopplung kreis);
            bool[] sperre = Sperre();
            int gedrosselt = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (!sperre[h]) continue;
                Assert.True(ohne.HeizlastW[h] <= 1e-9, "Sperrstunde " + h + " liefert " + ohne.HeizlastW[h] + " W");
                if (soll.HeizlastW[h] > 1.0) gedrosselt++;
            }
            Assert.True(gedrosselt > 0);
            Assert.True(kreis.StundenAnDerSchranke >= gedrosselt);
            Assert.True(ohne.Raumtemperatur.Where((t, h) => sperre[h]).Min() < soll.Raumtemperatur.Where((t, h) => sperre[h]).Min());
            Assert.True(kreis.DurchlaeufeMax <= 3, "Durchläufe höchstens " + kreis.DurchlaeufeMax);
        }

        [Fact]
        public void Eine_knappe_Schranke_haelt_die_Heizleistung_im_Massstab_des_Gebaeudes()
        {
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1);
            double spitzeKw = soll.HeizlastW.Max() / 1000.0;
            const double faktor = 2.0;
            double schrankeKw = 0.6 * spitzeKw * faktor;
            GebaeudeModellErgebnis ist = ImKreis(Kessel(schrankeKw), null, out Anlagenkopplung kreis, faktor);
            for (int h = 0; h < 8760; h++)
                Assert.True(ist.HeizlastW[h] * faktor <= schrankeKw * 1000.0 + 1e-6, "Stunde " + h);
            Assert.True(kreis.StundenAnDerSchranke > 0);
            Assert.True(kreis.DurchlaeufeMax <= 3, "Durchläufe höchstens " + kreis.DurchlaeufeMax);
        }

        [Fact]
        public void Ohne_Erzeuger_liefert_der_Kreis_nichts_und_bleibt_bei_zwei_Durchlaeufen()
        {
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(Gekoppelt()));
            s.Beginnen();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            Array.Empty<IErzeugerkapazitaet>(), new FesterSpeicher(5.0));
            Kopplungsstunde k = kreis.Stunde(0, double.NaN, default);
            Assert.Equal(Verfuegbarkeitsgrund.KeinErzeuger, k.Angebot.Grund);
            Assert.True(k.HeizlastW[0] <= 1e-9);
            Assert.Equal(2, k.Durchlaeufe);
        }
    }

    /// <summary>
    /// <b>Der AK3-Weg aus der Datenbank</b> (AK3-W3b): Projekt 1047 bzw. 1056 mit Stufe AK3 im Kern
    /// (<see cref="Ak3Kernmodus.AlleGekoppelten"/>) — ein Lauf ohne Fehler, der Kreis rechnet jede Stunde, die Kaskade
    /// lässt keinen Restbedarf (Festlegung 15), und ohne Kernstufe bleibt der Lauf der heutige. Die Messung (Durchläufe,
    /// Laufzeit, Heizwärme, Unterdeckung, WP-Strom gegen AK1) schreibt sie nach <c>AK3_MESSUNG</c>, wenn gesetzt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungKreisDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private sealed class Lauf
        {
            internal double Sekunden;
            internal SimulationControl Sim;
        }

        private static Lauf Rechnen(int projekt, Ak3Kernmodus modus)
        {
            using (Ak3Kernstufe.Schalten(modus))
            {
                SimulationProtokoll.NeuStarten();
                var r = new SimulationRunner();
                var uhr = Stopwatch.StartNew();
                bool ok = r.Simuliere(projekt, out string fehler);
                uhr.Stop();
                Assert.True(ok, "Lauf " + projekt + " (" + modus + ") gescheitert: " + fehler);
                return new Lauf { Sekunden = uhr.Elapsed.TotalSeconds, Sim = r.sim };
            }
        }

        [Theory]
        [InlineData(1047)]
        [InlineData(1056)]
        public void Projekt_rechnet_mit_Stufe_AK3_im_Kreis_ohne_Fehler(int projekt)
        {
            if (!_db.Vorhanden) return;
            Lauf ak1 = Rechnen(projekt, Ak3Kernmodus.Aus);
            Assert.Null(ak1.Sim.simulation_Waermebedarf.Ak3);
            Lauf ak3 = Rechnen(projekt, Ak3Kernmodus.AlleGekoppelten);
            Ak3Weg weg = ak3.Sim.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.NotNull(weg.Kreis);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.True(ak3.Sim.KesselInSchleife || ak3.Sim.BhkwInSchleife, "Vektorstufen nicht im Kreis");

            // Restbedarf 0 (Festlegung 15): Was der Kreis anbietet, deckt die Kaskade.
            Assert.True(ak3.Sim.RestwaermeMwh <= ak1.Sim.RestwaermeMwh + 1e-9,
                        "Restwärme AK3 " + ak3.Sim.RestwaermeMwh + " MWh gegen AK1 " + ak1.Sim.RestwaermeMwh);

            string ziel = Environment.GetEnvironmentVariable("AK3_MESSUNG");
            if (string.IsNullOrEmpty(ziel)) return;
            Anlagenkopplung k = weg.Kreis;
            CultureInfo c = CultureInfo.InvariantCulture;
            string verteilung = string.Join(" ", Enumerable.Range(1, k.DurchlaeufeVerteilung.Length - 1)
                .Where(i => k.DurchlaeufeVerteilung[i] > 0).Select(i => i + ":" + k.DurchlaeufeVerteilung[i]));
            File.AppendAllText(ziel, string.Format(c,
                "{0}|AK1 {1:0.000} s|AK3 {2:0.000} s|Faktor {3:0.00}|Durchläufe {4} (Mittel {5:0.000}, max {6})|Schranke {7} h|" +
                "Stützstellenwechsel {8}|Fallwechsel {9}|Heizwärme Gebäude {10:0.000}/{11:0.000} MWh|Unterdeckung {12:0.000}/{13:0.000} MWh|" +
                "WP-Strom {14:0.000}/{15:0.000} MWh|Abweichungsstunden {16}\n",
                projekt, ak1.Sekunden, ak3.Sekunden, ak3.Sekunden / ak1.Sekunden, verteilung, k.DurchlaeufeMittel, k.DurchlaeufeMax,
                k.StundenAnDerSchranke, k.StuetzstellenWechsel, k.FallWechsel,
                ak1.Sim.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt, ak3.Sim.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt,
                ak1.Sim.RestwaermeMwh, ak3.Sim.RestwaermeMwh,
                ak1.Sim.simulation_wp.WpStrombedarfGesamtKwh / 1000.0, ak3.Sim.simulation_wp.WpStrombedarfGesamtKwh / 1000.0,
                weg.DeltaKw.Count(d => d != 0.0)));
        }
    }
}
