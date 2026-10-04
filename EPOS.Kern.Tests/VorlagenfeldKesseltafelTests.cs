using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Katalog v11 — der Betrieb je Heizkessel</b> (Konzept Kesselkennlinie 5, Bericht): die Tabelle
    /// <c>stand.tabelle.heizkessel</c> mit Jahresnutzungsgrad η_eff, Anteil des Brennwertbetriebs nach Stunden und nach
    /// Wärme und Starts je Kessel, samt Schalter <c>hat.tabelle.heizkessel</c>.
    ///
    /// <para><b>Die Werte kommen aus dem Lauf</b> (<see cref="ZeitreihenSatz.Kessel"/>): Brennwertstunden, Brennwertwärme
    /// und Starts stehen nicht im gespeicherten Ergebnis. Der Sammler erhebt sie mit dem Zeitreihensatz — der Eintrag
    /// trägt deshalb den Bedarf <see cref="Vorlagenbedarf.Zeitreihen"/> —, über dieselbe Stelle wie der Kessel-Reiter
    /// (<see cref="SimulationErgebnisCtrl.Heizkessel"/>). Der Tabellenbau liest nur den Wertesatz.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorlagenfeldKesseltafelTests : IDisposable
    {
        private const string TAFEL = "stand.tabelle.heizkessel";
        private const string SCHALTER = "hat.tabelle.heizkessel";
        private const int PROJEKT_REFERENZ = 1050;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Platzhalterwert Loese(string schluessel, Berichtswerte w, VariantenDaten v)
            => Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(schluessel), w.MitStand(v), Array.Empty<Formatangabe>());

        private static (Berichtswerte W, VariantenDaten Mit, VariantenDaten OhneKessel, VariantenDaten OhneLauf) Staende()
        {
            var satz = new ZeitreihenSatz();
            satz.Kessel.Add(new Kesselbetrieb("Brennwertkessel", 96.43, true, 88.2, 91.6, 1234));
            satz.Kessel.Add(new Kesselbetrieb("Elektrokessel", 99.0, false, 0, 0, 17));
            var mit = new VariantenDaten { IdProjekt = 1, IstStamm = true, Projektname = "Probe", Zeitreihen = satz };
            var ohneKessel = new VariantenDaten { IdProjekt = 2, Projektname = "Ohne Kessel", Zeitreihen = new ZeitreihenSatz() };
            var ohneLauf = new VariantenDaten { IdProjekt = 3, Projektname = "Ohne Lauf" };
            var daten = new BerichtsDaten { IdStamm = 1, Stammprojektname = "Probe" };
            daten.Varianten.Add(mit);
            daten.Varianten.Add(ohneKessel);
            daten.Varianten.Add(ohneLauf);
            return (Berichtswerte.Aus(daten, null, false, null), mit, ohneKessel, ohneLauf);
        }

        [Fact]
        public void Die_Kesseltafel_steht_in_Fassung_11_je_Stand_in_Word_und_Excel()
        {
            Vorlagenfeld tafel = Vorlagenfeldkatalog.Finde(TAFEL);
            Assert.NotNull(tafel);
            Assert.Equal(11, tafel.Seit);
            Assert.Equal(Vorlagenfeldart.Tabelle, tafel.Art);
            Assert.Equal(Vorlagenfeldkontext.Stand, tafel.Kontext);
            Assert.Equal(Vorlagenbedarf.Zeitreihen, tafel.Bedarf);
            Assert.Equal(Vorlagenausgabe.Beide, tafel.Ausgaben);

            // Der Schalter ist so alt wie seine Tabelle - sonst stünde er in der ausgelieferten Liste der Fassung 4.
            Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde(SCHALTER);
            Assert.NotNull(schalter);
            Assert.Equal(11, schalter.Seit);
            Assert.Equal(Vorlagenfeldart.Schalter, schalter.Art);
            Assert.All(Vorlagenfeldkatalog.Alle.Where(f => f.Schluessel.StartsWith("hat.tabelle.", StringComparison.Ordinal)
                                                           && f.Schluessel != SCHALTER
                                                           // v12: der Schalter der Pufferauslegungstafel ist so alt wie sie.
                                                           && f.Schluessel != "hat.tabelle.pufferauslegung"),
                       f => Assert.Equal(4, f.Seit));
            // Die Fassung 11 brachte Word-Schlüssel; spätere Fassungen heben die Word-Fassung weiter (v12: Strom- und Kältebild, Pufferauslegungstafel).
            Assert.True(Vorlagenfeldkatalog.KatalogfassungWord >= 11);
        }

        [Fact]
        public void Die_Tafel_nennt_je_Kessel_Nutzungsgrad_Brennwertanteil_und_Starts()
        {
            (Berichtswerte w, VariantenDaten mit, _, _) = Staende();

            Platzhalterwert wert = Loese(TAFEL, w, mit);
            Assert.False(wert.IstLeer);
            Berichtstabelle t = wert.Tabelle;
            Assert.Equal(new[] { "Heizkessel", "Jahresnutzungsgrad [%]", "Brennwertbetrieb Stunden [%]",
                                 "Brennwertbetrieb Wärme [%]", "Starts [1/a]" },
                         t.Kopf.Zellen.Select(z => z.Text));
            Assert.Equal(2, t.Zeilen.Count);
            Assert.Equal(new[] { "Brennwertkessel", "96,4", "88", "92", "1.234" }, t.Zeilen[0].Zellen.Select(z => z.Text));
            // Ohne Brennwertkennlinie kein Brennwertbetrieb: Strich statt 0, wie im Kessel-Reiter.
            Assert.Equal(new[] { "Elektrokessel", "99,0", Tabellenzelle.STRICH, Tabellenzelle.STRICH, "17" },
                         t.Zeilen[1].Zellen.Select(z => z.Text));
            Assert.Equal(96.43, t.Zeilen[0].Zellen[1].Zahl);
            Assert.Null(t.Zeilen[1].Zellen[2].Zahl);
        }

        [Fact]
        public void Ohne_Kessel_oder_ohne_Lauf_bleibt_die_Tafel_mit_Grund_leer()
        {
            (Berichtswerte w, VariantenDaten mit, VariantenDaten ohneKessel, VariantenDaten ohneLauf) = Staende();

            // Eine leere Tabelle löst zum Leerwert samt Grund auf (Konzept Berichtsvorlagen 4.10).
            Platzhalterwert keinKessel = Loese(TAFEL, w, ohneKessel);
            Assert.True(keinKessel.IstLeer);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_KEIN_HEIZKESSEL, keinKessel.Grund);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_KEIN_HEIZKESSEL,
                         Berichtstabellen.Heizkessel(ohneKessel, false, w.Kultur).Leergrund);

            Platzhalterwert keinLauf = Loese(TAFEL, w, ohneLauf);
            Assert.True(keinLauf.IstLeer);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_KEINE_ZEITREIHEN, keinLauf.Grund);

            Assert.True(Loese(SCHALTER, w, mit).Schalter);
            Assert.False(Loese(SCHALTER, w, ohneKessel).Schalter);
            Assert.False(Loese(SCHALTER, w, ohneLauf).Schalter);
        }

        /// <summary>
        /// Der Sammler füllt die Tafel aus dem Lauf: Projekt 1050 rechnet mit Brennwertkennlinie am Rückfall-Rücklauf
        /// (50 °C unter dem Taupunkt des Gases, also jede Laufstunde im Brennwertbetrieb) und mit gepflegten Taktwerten.
        /// Der Jahresnutzungsgrad der Tafel ist der des gespeicherten Ergebnisses, die Starts sind die des Laufs.
        /// </summary>
        [Fact]
        public void Der_Sammler_fuellt_die_Tafel_aus_dem_Lauf_von_1050()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(
                PROJEKT_REFERENZ, "Probe 1050", new List<int>(), true, null, CancellationToken.None);
            VariantenDaten v = Assert.Single(daten.Varianten);
            Assert.NotNull(v.Zeitreihen);

            Kesselbetrieb k = Assert.Single(v.Zeitreihen.Kessel);
            ErgebnisHeizkesselModulModel gespeichert = Assert.Single(v.Ergebnis.Heizkessel.Module);
            Assert.Equal(gespeichert.Modul, k.Name);
            // Das Ergebnis speichert den Jahresnutzungsgrad auf zwei Stellen gerundet.
            Assert.True(Math.Abs(gespeichert.Jahresnutzungsgrad - k.JahresnutzungsgradProzent) <= 0.005 + 1e-9,
                        gespeichert.Jahresnutzungsgrad + " gegen " + k.JahresnutzungsgradProzent);
            Assert.True(k.MitBrennwertkennlinie);
            Assert.Equal(100.0, k.BrennwertStundenProzent, 9);
            Assert.Equal(100.0, k.BrennwertWaermeProzent, 9);
            Assert.True(k.Starts > 0);

            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            Berichtstabelle t = Loese(TAFEL, w, v).Tabelle;
            Assert.Single(t.Zeilen);
            Assert.Equal(k.Starts, t.Zeilen[0].Zellen[4].Zahl);
        }
    }
}
