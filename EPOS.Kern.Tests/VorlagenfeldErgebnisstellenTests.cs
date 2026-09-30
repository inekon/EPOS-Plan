using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Katalog v10 — die Stellen des Simulationsergebnisses</b>: die zehn Ergebnisbilder je Stand
    /// (<see cref="Berichtsbilder.Ergebnisbilder"/>) als Bild und als Excel-Diagramm, ihre Schalter, die Kennwerte des
    /// Speicherlaufs und die solare Deckung — alle aus dem Wertesatz, ohne Datenbank.
    /// </summary>
    public class VorlagenfeldErgebnisstellenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Der synthetische Satz der Renderer-Proben, ergänzt um die Reihen der Reiter.</summary>
        private static ZeitreihenSatz Satz()
        {
            ZeitreihenSatz z = ChartRendererGruppeDTests.Satz();
            double[] bedarf = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            z.Reihen[ZeitreihenSatz.TEMPERATUR] = bedarf.Select((x, i) => 10.0 - 12.0 * Math.Cos(2 * Math.PI * i / 8760.0)).ToArray();
            z.Reihen[ZeitreihenSatz.HEIZSTAB] = bedarf.Select(x => x > 280 ? 5.0 : 0.0).ToArray();
            z.Reihen[ZeitreihenSatz.WP_STROM] = z.Hole(ZeitreihenSatz.WP_WAERME).Select(x => x / 3.5).ToArray();
            z.Reihen[ZeitreihenSatz.WAERMEREST] = bedarf.Select(x => Math.Max(0.0, x - 250.0)).ToArray();
            z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = z.Hole(ZeitreihenSatz.PV_GENUTZT).Select(x => x * 0.2).ToArray();
            z.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.HEIZUNG)] = bedarf.Select(x => x * 0.8).ToArray();
            z.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.BRAUCHWASSER)] = bedarf.Select(x => x * 0.2).ToArray();
            z.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG)] = bedarf.Select(x => Math.Max(0.0, 200.0 - x)).ToArray();
            return z;
        }

        [Fact]
        public void Jedes_Ergebnisbild_hat_mit_seinen_Reihen_ein_Modell_und_ohne_sie_keins()
        {
            ZeitreihenSatz z = Satz();
            Assert.Equal(10, Berichtsbilder.Ergebnisbilder.Count);
            foreach (string name in Berichtsbilder.Ergebnisbilder)
            {
                Zeichenmodell m = Berichtsbilder.Ergebnisbild(name, z);
                Assert.True(m != null, name + " ohne Modell");
                Assert.False(string.IsNullOrWhiteSpace(Berichtsbilder.Titel(m)), name + " ohne Titel");
                Assert.Null(Berichtsbilder.Ergebnisbild(name, new ZeitreihenSatz()));
                Assert.Null(Berichtsbilder.Ergebnisbild(name, null));
            }
            Assert.Null(Berichtsbilder.ErgebnisbildPlan("gibtsnicht", z));
            // Dieselben Titel wie die Reiter der Ergebnisseite.
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.CHART_TITEL_WAERMELAST_JAHRESGANGLINIE,
                         Berichtsbilder.ErgebnisbildPlan("heizkessel", z).Titel);
            Assert.Equal(Berichtsbilder.Ergebnisbildform.Streuwolke, Berichtsbilder.ErgebnisbildPlan("waermepumpe_streuwolke", z).Form);
            Assert.Equal(Berichtsbilder.Ergebnisbildform.Normiert, Berichtsbilder.ErgebnisbildPlan("bedarf_waerme", z).Form);
            Assert.Equal(3, Berichtsbilder.ErgebnisbildPlan("bedarf_waerme", z).Linien.Count);   // Summe, Heizung, Brauchwasser
        }

        [Fact]
        public void Die_Ergebnisbilder_stehen_in_Fassung_10_Word_und_Excel_die_Streuwolke_nur_Word()
        {
            foreach (string name in Berichtsbilder.Ergebnisbilder)
            {
                Vorlagenfeld bild = Vorlagenfeldkatalog.Finde("stand.bild." + name);
                Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde("hat.bild." + name);
                Assert.NotNull(bild);
                Assert.NotNull(schalter);
                Assert.Equal(10, bild.Seit);
                Assert.Equal(10, schalter.Seit);
                Assert.Equal(Vorlagenfeldart.Bild, bild.Art);
                Assert.Equal(Vorlagenfeldkontext.Stand, bild.Kontext);
                Assert.Equal(Vorlagenbedarf.Zeitreihen, bild.Bedarf);
                Assert.Equal(name == Berichtsbilder.ERGEBNISBILD_STREUWOLKE ? Vorlagenausgabe.Word : Vorlagenausgabe.Beide, bild.Ausgaben);
            }
            // Die Fassung 10 brachte Word-Schlüssel; spätere Fassungen heben die Word-Fassung weiter (v11: Kesseltafel).
            Assert.True(Vorlagenfeldkatalog.KatalogfassungWord >= 10);
        }

        [Fact]
        public void Die_Excel_Diagramme_der_Ergebnisbilder_tragen_je_Stunde_eine_Kategorie()
        {
            ZeitreihenSatz z = Satz();
            var v = new VariantenDaten { IdProjekt = 1, IstStamm = true, Projektname = "Probe", Zeitreihen = z };
            var daten = new BerichtsDaten { IdStamm = 1, Stammprojektname = "Probe" };
            daten.Varianten.Add(v);
            var k = new Diagrammkontext(daten, false);
            foreach (string name in Berichtsbilder.Ergebnisbilder)
            {
                string schluessel = "stand.bild." + name;
                Exceldiagramm d = Exceldiagrammquellen.Baue(schluessel, k, v);
                if (name == Berichtsbilder.ERGEBNISBILD_STREUWOLKE)
                {
                    Assert.False(Exceldiagrammquellen.Kennt(schluessel));
                    Assert.Null(d);
                    continue;
                }
                Assert.True(d != null, schluessel + " ohne Diagramm");
                Assert.Equal(ZeitreihenSatz.Stunden, d.Kategorien.Count);
                Assert.NotEmpty(d.Reihen);
                Assert.Equal(Berichtsbilder.Titel(Berichtsbilder.Ergebnisbild(name, z)), d.Titel);
            }
            // Normiert: der größte Wert ist 100 %.
            Exceldiagramm bedarf = Exceldiagrammquellen.Baue("stand.bild.bedarf_strom", k, v);
            Assert.Equal(100.0, bedarf.Reihen.SelectMany(r => r.Werte).Where(x => x.HasValue).Max(x => x.Value), 6);
            Assert.Null(Exceldiagrammquellen.Baue("stand.bild.heizkessel", k, new VariantenDaten { IdProjekt = 2 }));
        }

        [Fact]
        public void Bild_und_Schalter_loesen_am_Stand_auf()
        {
            var mit = new VariantenDaten { IdProjekt = 1, IstStamm = true, Projektname = "Probe", Zeitreihen = Satz() };
            var ohne = new VariantenDaten { IdProjekt = 2, Projektname = "Ohne" };
            var daten = new BerichtsDaten { IdStamm = 1, Stammprojektname = "Probe" };
            daten.Varianten.Add(mit);
            daten.Varianten.Add(ohne);
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);

            Vorlagenfeld bild = Vorlagenfeldkatalog.Finde("stand.bild.bhkw");
            Platzhalterwert am = Vorlagenfeldkatalog.Loese(bild, w.MitStand(mit), Array.Empty<Formatangabe>());
            Assert.False(am.IstLeer);
            Assert.NotNull(am.Diagramm);
            Assert.True(Vorlagenfeldkatalog.Loese(bild, w.MitStand(ohne), Array.Empty<Formatangabe>()).IstLeer);

            Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde("hat.bild.bhkw");
            Assert.True(Vorlagenfeldkatalog.Loese(schalter, w.MitStand(mit), Array.Empty<Formatangabe>()).Schalter);
            Assert.False(Vorlagenfeldkatalog.Loese(schalter, w.MitStand(ohne), Array.Empty<Formatangabe>()).Schalter);

            // Die Positionsform gilt auch für die Ergebnisbilder.
            Vorlagenfeld position = Vorlagenfeldkatalog.Finde("stand.1.bild.bhkw");
            Assert.NotNull(position);
            Assert.NotNull(Vorlagenfeldkatalog.Loese(position, w, Array.Empty<Formatangabe>()).Diagramm);
        }

        [Fact]
        public void Speicherlauf_und_solare_Deckung_kommen_aus_dem_gespeicherten_Ergebnis()
        {
            var ergebnis = new ErgebnisModel { Sim_Solarthermie = true, Solarthermie = new ErgebnisSolarthermieModel { Waermebedarfsdeckung = 12.345 } };
            ergebnis.Stromspeicher.Add(new ErgebnisStromspeicherModel
            {
                Betriebsart = "Eigenverbrauch", Berechnungsart = "Viertelstunde", Ertrag_Aequivalent = 1234.5,
                Jahresueberschuss = 456.78, Investition = 8000, Amortisation_Statisch = 9.26, Vollzyklen = 211.4,
                Eigenverbrauchsquote = 71.23, Autarkiegrad = 44.44,
            });
            var mit = new VariantenDaten { IdProjekt = 1, IstStamm = true, Projektname = "Probe", Ergebnis = ergebnis };
            var ohne = new VariantenDaten { IdProjekt = 2, Projektname = "Ohne", Ergebnis = new ErgebnisModel() };
            var daten = new BerichtsDaten { IdStamm = 1, Stammprojektname = "Probe" };
            daten.Varianten.Add(mit);
            daten.Varianten.Add(ohne);
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);

            string Text(string schluessel, VariantenDaten v)
                => Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(schluessel), w.MitStand(v), Array.Empty<Formatangabe>()).Text;

            Assert.Equal("Eigenverbrauch", Text("stand.speicher.betriebsart", mit));
            Assert.Equal("Viertelstunde", Text("stand.speicher.berechnungsart", mit));
            Assert.Equal("1.234,50 €/a", Text("stand.speicher.ertrag", mit));
            Assert.Equal("456,78 €/a", Text("stand.speicher.ueberschuss", mit));
            Assert.Equal("9,3 a", Text("stand.speicher.amortisation", mit));
            Assert.Equal("211,4", Text("stand.speicher.vollzyklen", mit));
            Assert.Equal("71,2 %", Text("stand.speicher.eigenverbrauch", mit));
            Assert.Equal("44,4 %", Text("stand.speicher.autarkie", mit));
            Assert.Equal("12,35 %", Text("stand.solarthermie.deckung", mit));

            Vorlagenfeld hat = Vorlagenfeldkatalog.Finde("stand.hat_speicherlauf");
            Assert.True(Vorlagenfeldkatalog.Loese(hat, w.MitStand(mit), Array.Empty<Formatangabe>()).Schalter);
            Assert.False(Vorlagenfeldkatalog.Loese(hat, w.MitStand(ohne), Array.Empty<Formatangabe>()).Schalter);
            Assert.True(Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.speicher.ertrag"), w.MitStand(ohne),
                                                  Array.Empty<Formatangabe>()).IstLeer);
            Assert.True(Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.solarthermie.deckung"), w.MitStand(ohne),
                                                  Array.Empty<Formatangabe>()).IstLeer);

            // Ohne Investition keine Amortisation.
            ergebnis.Stromspeicher[0].Investition = 0;
            Assert.True(Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.speicher.amortisation"), w.MitStand(mit),
                                                  Array.Empty<Formatangabe>()).IstLeer);
            // Die Positionsform: das Stammprojekt ist Stand 1.
            Assert.Equal("44,4 %", Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.1.speicher.autarkie"), w,
                                                           Array.Empty<Formatangabe>()).Text);
        }
    }
}
