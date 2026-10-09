using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KM3‑E3‑b — Teillast und Takten der Kältemaschine im Bericht</b> (Fachkonzept Teillast und Takten 5.3, 5.4): die
    /// fünf Kennzahlen <c>kaelte.km.*</c>, die Tafel <c>stand.tabelle.km_teillast</c> und die Lesewerte; ohne Maschine mit
    /// Teillastweg bleiben Kennzahlen leer und die Tafel leer mit Grund.
    /// </summary>
    public sealed class KaeltemaschineTeillastBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        private static ErgebnisKaeltemaschineModel MitWeg() => new ErgebnisKaeltemaschineModel
        {
            Bezeichner = "KM 1", ID_Kaeltemaschine = 7, Kaelteproduktion_MWh = 30, Stromverbrauch_MWh = 10, Hilfsstrom_MWh = 2,
            FreieKuehlung_MWh = 6, Taktstunden = 40, Taktstrom_MWh = 0.5, Starts = 120, Teillaststunden = 300,
            Lastgrad_Mittel = 0.6, Stunden_Extrapoliert = 3
        };

        private static ErgebnisKaeltemaschineModel OhneWeg() => new ErgebnisKaeltemaschineModel
        {
            Bezeichner = "KM 2", Kaelteproduktion_MWh = 10, Stromverbrauch_MWh = 3, Hilfsstrom_MWh = 1, Taktstunden = 5
        };

        private static VariantenDaten Stand(Dictionary<int, int> verdichterstunden, params ErgebnisKaeltemaschineModel[] km)
        {
            var e = new ErgebnisModel();
            e.Kaeltemaschinen.AddRange(km);
            var v = new VariantenDaten { Ergebnis = e };
            if (verdichterstunden != null)
            {
                v.Zeitreihen = new ZeitreihenSatz();
                foreach (var kv in verdichterstunden) v.Zeitreihen.KaeltemaschineVerdichterstunden[kv.Key] = kv.Value;
            }
            v.KaeltemaschineTeillast[7] = new KaeltemaschineTeillastLesewerte
            {
                Herkunft = KaeltemaschinenKurvenherkunft.Vorgabekurve, Verdichterregelung = KaeltemaschineTeillastSchema.REGELUNG_STUFEN
            };
            return v;
        }

        private static double? Wert(string schluessel, VariantenDaten v)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(v);

        [Fact]
        public void Die_fuenf_Kennzahlen_rechnen_nur_die_Maschinen_mit_Weg()
        {
            // Die Maschine ohne Weg steht VOR der mit Weg: der Platz in der Ergebnisliste ist 1.
            VariantenDaten v = Stand(new Dictionary<int, int> { [1] = 1200 }, OhneWeg(), MitWeg());
            Assert.Equal(500.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_TAKTSTROM, v)!.Value, 9);
            Assert.Equal(120.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_STARTS, v));
            Assert.Equal(25.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_TEILLASTANTEIL, v)!.Value, 9);
            Assert.Equal(0.6, Wert(KennzahlenKatalog.SCHLUESSEL_KM_LASTGRAD, v)!.Value, 9);
            // (30 − 6) / (10 − 2) = 3,0
            Assert.Equal(3.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_JAZ_VERDICHTER, v)!.Value, 9);

            // Ohne Verdichterstunden des Laufs kein Teillastanteil; ohne Maschine mit Weg keine der fünf.
            Assert.Null(Wert(KennzahlenKatalog.SCHLUESSEL_KM_TEILLASTANTEIL, Stand(null, MitWeg())));
            VariantenDaten ohne = Stand(new Dictionary<int, int>(), OhneWeg());
            foreach (string s in new[] { KennzahlenKatalog.SCHLUESSEL_KM_TAKTSTROM, KennzahlenKatalog.SCHLUESSEL_KM_STARTS,
                                         KennzahlenKatalog.SCHLUESSEL_KM_TEILLASTANTEIL, KennzahlenKatalog.SCHLUESSEL_KM_LASTGRAD,
                                         KennzahlenKatalog.SCHLUESSEL_KM_JAZ_VERDICHTER })
                Assert.Null(Wert(s, ohne));
            Assert.All(KennzahlenKatalog.Alle().Where(k => k.Schluessel.StartsWith("kaelte.km.", StringComparison.Ordinal)
                                                         && k.Seit == 18), k => Assert.NotEqual(k.LabelDe, k.LabelEn));
            Assert.Equal(5, KennzahlenKatalog.Alle().Count(k => k.Schluessel.StartsWith("kaelte.km.", StringComparison.Ordinal) && k.Seit == 18));
        }

        [Fact]
        public void Die_Tafel_fuehrt_je_Maschine_mit_Weg_eine_Spalte_und_bleibt_ohne_Weg_leer_mit_Grund()
        {
            Berichtstabelle t = Berichtstabellen.KaeltemaschineTeillast(Stand(new Dictionary<int, int> { [1] = 1200 }, OhneWeg(), MitWeg()), DE);
            Assert.Equal(new[] { "Größe", "KM 1" }, t.Kopf.Zellen.Select(z => z.Text));
            Assert.Equal(11, t.Zeilen.Count);
            Assert.Equal("Kurve (Vorgabekurve)", t.Zeilen[0].Zellen[1].Text);
            Assert.Equal("gestuft", t.Zeilen[1].Zellen[1].Text);
            Assert.Equal("0,9 (Vorgabe)", t.Zeilen[2].Zellen[1].Text);
            Assert.Equal(new double?[] { 40, 120, 500, 300, 25, 0.6, 3, 3.0 },
                         t.Zeilen.Skip(3).Select(z => z.Zellen[1].Zahl).Select(x => x.HasValue ? Math.Round(x.Value, 6) : x));
            Assert.Single(t.Hinweise);

            Berichtstabelle leer = Berichtstabellen.KaeltemaschineTeillast(Stand(null, OhneWeg()), DE);
            Assert.Equal(0, leer.Zeilen.Count);
            Assert.Equal(Berichtstabellen.Grund(nameof(WindowsFormsApplication1.MyResource.Resource.BV_GRUND_KEINE_KM_TEILLAST), DE), leer.Leergrund);
        }

        [Fact]
        public void Tafel_und_Kennzahlen_stehen_in_Katalog_18_mit_Schalter()
        {
            Assert.Equal(18, Vorlagenfeldkatalog.KATALOGFASSUNG);
            Assert.Equal(18, Vorlagenfeldkatalog.Finde("stand.tabelle.km_teillast")?.Seit);
            Assert.NotNull(Vorlagenfeldkatalog.Finde("hat.tabelle.km_teillast"));
        }

        [Fact]
        public void Die_Lesewerte_nennen_Weg_Herkunft_Regelung_und_Cd()
        {
            var m = new KaeltemaschineModel { Teillast_Weg = KaeltemaschineTeillastSchema.WEG_LINEAR, Taktverlustfaktor_Cd = 0.85,
                                              Kennfeld_Randweg = KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD };
            KaeltemaschineTeillastLesewerte w = KaeltemaschineTeillastLesewerte.Aus(m);
            Assert.True(w.Gesetzt);
            Assert.Equal("linear", w.WegText(DE));
            Assert.Equal("0,85", w.CdText(DE));
            Assert.Equal("keine Angabe", w.RegelungText(DE));
            Assert.False(KaeltemaschineTeillastLesewerte.Aus(new KaeltemaschineModel()).Gesetzt);
            Assert.Equal("wie bisher", KaeltemaschineTeillastLesewerte.Aus(new KaeltemaschineModel()).WegText(DE));
            Assert.Equal("0,9 (Vorgabe)", KaeltemaschineTeillastLesewerte.Aus(new KaeltemaschineModel()).CdText(DE));
        }
    }

    /// <summary>
    /// KM3‑E3‑b: die Tafel am Referenzprojekt 1063 der Testdatenbank — Lauf, Ergebnis, Zeitreihensatz und Lesewerte wie
    /// im Sammler.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineTeillastTafelDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        [Fact]
        public void Die_Tafel_von_1063_weist_Takten_und_Teillast_aus()
        {
            const int projekt = 1063;
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(projekt, out string fehler), "Lauf gescheitert: " + fehler);
            var v = new VariantenDaten
            {
                IdProjekt = projekt,
                Ergebnis = SimulationRunner.BaueErgebnis(projekt, r.simulation_Waermebedarf, r.simulation_Strombedarf, r.sim),
                Zeitreihen = ZeitreihenExtraktor.AusLauf(r),
                KaeltemaschineTeillast = KaeltemaschineTeillastBerichtsquelle.Lade(projekt),
            };
            Berichtstabelle t = Berichtstabellen.KaeltemaschineTeillast(v, new CultureInfo("de-DE"));
            Assert.Null(t.Leergrund);
            Assert.Equal(2, t.Kopf.Zellen.Count);
            Assert.DoesNotContain(Tabellenzelle.STRICH, t.Zeilen.Take(3).Select(z => z.Zellen[1].Text));
            double? Z(int i) => t.Zeilen[i].Zellen[1].Zahl;
            Assert.Equal(218.0, Z(3));
            Assert.Equal(717.0, Z(4));
            Assert.Equal(17.27, Z(5)!.Value, 2);
            Assert.Equal(202.0, Z(6));
            Assert.InRange(Z(7)!.Value, 0.0, 100.0);
            Assert.Equal(0.70, Z(8)!.Value, 2);
            Assert.Equal(1.0, Z(9));
            Assert.True(Z(10) > 0);
        }
    }
}
