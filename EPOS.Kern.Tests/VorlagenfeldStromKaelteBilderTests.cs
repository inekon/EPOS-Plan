using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Katalog v12 — Stromlast des BHKW und Kälteproduktion</b>: die Bilder <c>stand.bild.bhkw_strom</c> (zweites Bild
    /// des Reiters „BHKW“) und <c>stand.bild.kaelte_produktion</c> (Blatt „Kälte“ des Reiters „Ergebnis“) samt Schaltern.
    ///
    /// <para>Die Reihen kommen aus dem Lauf über den Zeitreihensatz (<see cref="ZeitreihenExtraktor"/>): Strombedarf und
    /// Reststrombedarf am BHKW aus <see cref="SimulationErgebnisCtrl.BhkwStromStunden"/>, die Kältedeckung je Erzeuger und
    /// die ungedeckte Kälte aus <see cref="KaelteProduktionBild.AusLauf"/> — dieselben Quellen wie die Bilder der Seite.
    /// Rechnet das Projekt keine Kälte, bleibt das Kältebild leer.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorlagenfeldStromKaelteBilderTests : IDisposable
    {
        private const string BILD_BHKW_STROM = "stand.bild.bhkw_strom";
        private const string BILD_KAELTE = "stand.bild.kaelte_produktion";
        private const int PROJEKT_BHKW = 1018;
        private const int PROJEKT_KAELTE = 1017;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Platzhalterwert Loese(string schluessel, Berichtswerte w, VariantenDaten v)
            => Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(schluessel), w.MitStand(v), Array.Empty<Formatangabe>());

        private static (Berichtswerte W, VariantenDaten V) Sammle(int projekt)
        {
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(
                projekt, "Probe " + projekt, new List<int>(), true, null, CancellationToken.None);
            VariantenDaten v = Assert.Single(daten.Varianten);
            Assert.NotNull(v.Zeitreihen);
            return (Berichtswerte.Aus(daten, null, false, null), v);
        }

        private static bool IstPng(byte[] b)
            => b != null && b.Length > 8 && b[0] == 0x89 && b[1] == (byte)'P' && b[2] == (byte)'N' && b[3] == (byte)'G';

        [Fact]
        public void Beide_Bilder_stehen_in_Fassung_12_je_Stand_in_Word_und_Excel()
        {
            Assert.Equal(new[] { "bhkw_strom", "kaelte_produktion" }, Berichtsbilder.ErgebnisbilderFassung12);
            foreach (string schluessel in new[] { BILD_BHKW_STROM, BILD_KAELTE })
            {
                Vorlagenfeld bild = Vorlagenfeldkatalog.Finde(schluessel);
                Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde(schluessel.Replace("stand.bild.", "hat.bild."));
                Assert.NotNull(bild);
                Assert.NotNull(schalter);
                Assert.Equal(12, bild.Seit);
                Assert.Equal(12, schalter.Seit);
                Assert.Equal(Vorlagenfeldart.Bild, bild.Art);
                Assert.Equal(Vorlagenfeldkontext.Stand, bild.Kontext);
                Assert.Equal(Vorlagenbedarf.Zeitreihen, bild.Bedarf);
                Assert.Equal(Vorlagenausgabe.Beide, bild.Ausgaben);
                Assert.False(string.IsNullOrWhiteSpace(Vorlagenfeldkatalog.Beschreibung(bild, false)), schluessel + " ohne Text de");
                Assert.False(string.IsNullOrWhiteSpace(Vorlagenfeldkatalog.Beschreibung(bild, true)), schluessel + " ohne Text en");
            }
        }

        /// <summary>Die Pläne tragen die Reihen der Seite in ihrer Folge: Säule unten, Linien darüber.</summary>
        [Fact]
        public void Die_Plaene_tragen_die_Reihen_der_Seite()
        {
            var z = new ZeitreihenSatz();
            double[] produktion = Enumerable.Range(0, ZeitreihenSatz.Stunden).Select(h => h % 24 < 12 ? 50.0 : 0.0).ToArray();
            double[] bedarf = Enumerable.Repeat(40.0, ZeitreihenSatz.Stunden).ToArray();
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = produktion;
            z.Reihen[ZeitreihenSatz.BHKW_STROMBEDARF] = bedarf;
            z.Reihen[ZeitreihenSatz.BHKW_RESTSTROM] = bedarf.Select((b, i) => Math.Max(0.0, b - produktion[i])).ToArray();

            Berichtsbilder.Ergebnisbildplan strom = Berichtsbilder.ErgebnisbildPlan("bhkw_strom", z);
            Assert.NotNull(strom);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMDET_BHKW_TITEL_STROMLAST, strom.Titel);
            Assert.Single(strom.Stapel);
            Assert.Equal(3, strom.Linien.Count);                 // Einspeisung (hier 0), Reststrom, Strombedarf
            Assert.Equal(0.0, strom.Linien[0].Werte.Sum());
            Assert.Equal(bedarf.Sum(), strom.Linien[2].Werte.Sum(), 6);
            Assert.True(IstPng(SkiaMaler.Png(Berichtsbilder.Ergebnisbild("bhkw_strom", z))));

            // Ohne Kälte kein Kältebild — auch mit Kältebedarf allein nicht.
            z.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG)] = bedarf;
            Assert.Null(Berichtsbilder.ErgebnisbildPlan("kaelte_produktion", z));

            z.Reihen[ZeitreihenSatz.KAELTE_PRAEFIX + "1"] = bedarf.Select(x => 0.75 * x).ToArray();
            z.Beschriftungen[ZeitreihenSatz.KAELTE_PRAEFIX + "1"] = "";
            z.Kaeltereihen.Add(ZeitreihenSatz.KAELTE_PRAEFIX + "1");
            z.Reihen[ZeitreihenSatz.KAELTEREST] = bedarf.Select(x => 0.25 * x).ToArray();
            Berichtsbilder.Ergebnisbildplan kaelte = Berichtsbilder.ErgebnisbildPlan("kaelte_produktion", z);
            Assert.NotNull(kaelte);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.CHART_TITEL_KAELTEPRODUKTION_JAHRESGANGLINIE, kaelte.Titel);
            Assert.Equal(2, kaelte.Stapel.Count);                // Erzeuger, ungedeckte Kälte
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, kaelte.Stapel[0].Name);
            Assert.Single(kaelte.Linien);                        // Kältebedarf
            Assert.True(IstPng(SkiaMaler.Png(Berichtsbilder.Ergebnisbild("kaelte_produktion", z))));
        }

        /// <summary>
        /// Projekt 1018 rechnet ein BHKW und keine Kälte: Die Stromlast ist ein Bild, die Reihen gleichen denen des
        /// BHKW-Reiters (Reststrom = Strombedarf minus Produktion, nie unter 0); das Kältebild bleibt leer.
        /// </summary>
        [Fact]
        public void Projekt_1018_hat_die_Stromlast_des_BHKW_und_kein_Kaeltebild()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            (Berichtswerte w, VariantenDaten v) = Sammle(PROJEKT_BHKW);
            ZeitreihenSatz z = v.Zeitreihen;
            double[] produktion = z.Hole(ZeitreihenSatz.BHKW_STROM);
            double[] bedarf = z.Hole(ZeitreihenSatz.BHKW_STROMBEDARF);
            double[] rest = z.Hole(ZeitreihenSatz.BHKW_RESTSTROM);
            Assert.NotNull(produktion);
            Assert.NotNull(bedarf);
            Assert.NotNull(rest);
            Assert.True(produktion.Sum() > 0);
            for (int h = 0; h < rest.Length; h++)
                Assert.Equal(Math.Max(0.0, bedarf[h] - produktion[h]), rest[h], 9);

            Platzhalterwert bild = Loese(BILD_BHKW_STROM, w, v);
            Assert.False(bild.IstLeer);
            Assert.NotNull(bild.Diagramm);
            Assert.True(Loese("hat.bild.bhkw_strom", w, v).Schalter);
            Assert.True(IstPng(SkiaMaler.Png(Berichtsbilder.Ergebnisbild("bhkw_strom", z))));

            Assert.False(z.RechnetKaelte);
            Assert.Empty(z.Kaeltereihen);
            Assert.True(Loese(BILD_KAELTE, w, v).IstLeer);
            Assert.False(Loese("hat.bild.kaelte_produktion", w, v).Schalter);
        }

        /// <summary>
        /// Projekt 1017 rechnet Kälte und deckt sie mit einer Wärmepumpe im Kühlbetrieb: Das Kältebild trägt je Erzeuger
        /// eine Säule, gedeckte und ungedeckte Kälte ergeben zusammen den Kältebedarf.
        /// </summary>
        [Fact]
        public void Projekt_1017_hat_das_Bild_der_Kaelteproduktion()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            (Berichtswerte w, VariantenDaten v) = Sammle(PROJEKT_KAELTE);
            ZeitreihenSatz z = v.Zeitreihen;
            Assert.True(z.RechnetKaelte);
            Assert.NotEmpty(z.Kaeltereihen);
            double gedeckt = z.Kaeltereihen.Sum(k => z.Hole(k).Sum());
            double ungedeckt = z.Hole(ZeitreihenSatz.KAELTEREST).Sum();
            double bedarf = z.Hole(ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG)).Sum();
            Assert.True(gedeckt > 0);
            Assert.True(Math.Abs(gedeckt + ungedeckt - bedarf) <= 1e-6 * Math.Max(1.0, bedarf),
                        $"gedeckt {gedeckt} + ungedeckt {ungedeckt} gegen Bedarf {bedarf}");

            Platzhalterwert bild = Loese(BILD_KAELTE, w, v);
            Assert.False(bild.IstLeer);
            Assert.NotNull(bild.Diagramm);
            Assert.True(Loese("hat.bild.kaelte_produktion", w, v).Schalter);
            Assert.True(IstPng(SkiaMaler.Png(Berichtsbilder.Ergebnisbild("kaelte_produktion", z))));
        }
    }
}
