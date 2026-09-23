using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zapfereignisgenerator</b> (Umsetzungskonzept Zapfprofilgenerator 4.4): gestutztes
    /// Mittel der gezogenen Verteilung (±1 %), Momente je Kategorie (±5 %), Erwartungswert gleich
    /// Tagesmenge, Zeitpunkte nach dem Tagesgang, Kappung, Determinismus je Seed, benannte
    /// Ablehnungen und die Einheiten je Bezugsart. Alle Kategorien und Werte sind ERFUNDEN — keine
    /// Jordan/Vajen-Zahl, keine Normzahl (Kapitel 6).
    /// </summary>
    public sealed class ZapfereignisgeneratorTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static double Cw => Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K;

        /// <summary>Vier erfundene Kategorien mit verschiedener Dauer (1, 2, 8, 4 min) — die Dauer erkennt die Kategorie.</summary>
        internal static Zapfkategorie[] Kategorien(int idArt = 1) => new[]
        {
            new Zapfkategorie(idArt, "Testkategorie A (fiktiv)", 2.0, 1.0, 1, 0.20, Fiktiv),
            new Zapfkategorie(idArt, "Testkategorie B (fiktiv)", 5.0, 2.0, 2, 0.30, Fiktiv),
            new Zapfkategorie(idArt, "Testkategorie C (fiktiv)", 12.0, 3.0, 8, 0.15, Fiktiv) { KappungLJeMin = 15.0 },
            new Zapfkategorie(idArt, "Testkategorie D (fiktiv)", 7.0, 2.0, 4, 0.35, Fiktiv),
        };

        /// <summary>Eine normierte Zeitstruktur: alle vier Tagtypen mit demselben Gang (Ruhetag wahlweise leer).</summary>
        internal static Zeitstruktur Struktur(double[] gang = null, bool ruhetagLeer = false)
        {
            gang ??= Gang((6, 0.25), (7, 0.25), (18, 0.25), (19, 0.25));
            var g = new double[4, 24];
            for (int t = 0; t < 4; t++)
                for (int h = 0; h < 24; h++) g[t, h] = ruhetagLeer && t == 3 ? 0.0 : gang[h];
            return new Zeitstruktur(Enumerable.Repeat(1.0, 12).ToArray(), Enumerable.Repeat(1.0 / 7, 7).ToArray(), null, g,
                                    new[] { false, false, false, ruhetagLeer });
        }

        // =================================================================================
        // Gestutztes Mittel
        // =================================================================================

        [Fact]
        public void Das_gestutzte_Mittel_ist_das_der_gezogenen_Verteilung()
        {
            // Ränder und Symmetrie des Stückpolynoms: g(c) − g(−c) = c.
            Assert.Equal(0.0, Zapfverteilung.PositivteilMittel(-6.0));
            Assert.Equal(0.0, Zapfverteilung.PositivteilMittel(-9.5));
            Assert.Equal(6.0, Zapfverteilung.PositivteilMittel(6.0));
            Assert.Equal(7.5, Zapfverteilung.PositivteilMittel(7.5));
            foreach (double c in new[] { 0.3, 1.0, 2.5, 5.0 })
                Assert.Equal(c, Zapfverteilung.PositivteilMittel(c) - Zapfverteilung.PositivteilMittel(-c), 12);
            // g(0) = E|z|/2 der Irwin-Hall-Summe (unabhängig in Python gerechnet).
            Assert.Equal(0.40063214948631615, Zapfverteilung.PositivteilMittel(0.0), 14);
            Assert.Throws<ArgumentOutOfRangeException>(() => Zapfverteilung.PositivteilMittel(double.NaN));

            // Ohne Streuung: der gekappte Wert selbst.
            Assert.Equal(4.0, Zapfverteilung.GestutztesMittel(4.0, 0.0, null));
            Assert.Equal(0.0, Zapfverteilung.GestutztesMittel(-1.0, 0.0, null));
            Assert.Equal(3.0, Zapfverteilung.GestutztesMittel(4.0, 0.0, 3.0));

            // Empirisch: das Mittel der gekappten Ziehung trifft das gestutzte Mittel auf ±1 % (4.4).
            var z = new ZapfZufall(21);
            foreach (var (mu, sigma, kappung) in new (double, double, double?)[]
                     { (2.0, 1.0, null), (1.0, 2.0, null), (12.0, 3.0, 15.0), (0.5, 2.0, 3.0), (0.0, 1.5, null) })
            {
                const int n = 400000;
                double s = 0.0;
                for (int i = 0; i < n; i++)
                {
                    double v = mu + sigma * z.Normal();
                    if (v < 0) v = 0;
                    if (kappung.HasValue && v > kappung.Value) v = kappung.Value;
                    s += v;
                }
                double soll = Zapfverteilung.GestutztesMittel(mu, sigma, kappung);
                Assert.InRange(s / n, soll * 0.99, soll * 1.01);
            }

            // Gegen die Normalformel μ·F(μ/σ) + σ·f(μ/σ) des Papiers: im Arbeitsbereich unter 1 %.
            foreach (double c in new[] { -1.0, -0.5, 0.0, 0.5, 1.0, 2.0, 3.0 })
            {
                double normal = c * 0.5 * (1.0 + Din4708Kennzahl.Erf(c / Math.Sqrt(2.0)))
                                + Math.Exp(-c * c / 2.0) / Math.Sqrt(2.0 * Math.PI);
                Assert.InRange(Zapfverteilung.PositivteilMittel(c), normal * 0.99, normal * 1.01);
            }
        }

        // =================================================================================
        // Momente und Erwartungswert
        // =================================================================================

        private const double TAG_KWH = 10.0;
        private const double SPREIZUNG_K = 40.0;
        private const int TAGE = 20000;

        private static List<Zapfereignis>[] Ziehen(ulong seed, Zapfkategoriensatz satz, Tageszeitdichte dichte, int tage = TAGE)
        {
            var z = new ZapfZufall(seed);
            var liste = new List<Zapfereignis>[tage];
            for (int d = 0; d < tage; d++)
            {
                liste[d] = new List<Zapfereignis>();
                Zapfereignisgenerator.Ziehen(z, satz, TAG_KWH, SPREIZUNG_K, dichte, liste[d]);
            }
            return liste;
        }

        [Fact]
        public void Die_Momente_je_Kategorie_treffen_die_Kalibrierung()
        {
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(Kategorien(), 1, "Zone A");
            Assert.Equal(1.0, satz.Werte.Sum(k => k.AnteilNormiert), 12);
            List<Zapfereignis>[] tage = Ziehen(31, satz, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag));
            double gesamt = tage.Sum(t => t.Sum(e => e.EnergieKwh));

            foreach (Zapfkategoriewert k in satz.Werte)
            {
                int dauer = k.Kategorie.DauerMin;
                double faktor = dauer * Cw * SPREIZUNG_K / 1000.0;
                Zapfereignis[] eigene = tage.SelectMany(t => t.Where(e => e.DauerMin == dauer)).ToArray();

                // Häufigkeit je Tag: die Rate der λ-Kalibrierung (±5 %).
                double rate = Zapfereignisgenerator.Rate(k, TAG_KWH, SPREIZUNG_K);
                Assert.InRange(eigene.Length / (double)TAGE, rate * 0.95, rate * 1.05);
                Assert.Equal(k.AnteilNormiert * TAG_KWH / (k.MittelLJeMin * faktor), rate, 10);

                // Volumenstrom: gestutztes Mittel (±1 %) und Streuung der gekappten Ziehung (±5 %).
                double[] v = eigene.Select(e => e.EnergieKwh / faktor).ToArray();
                double mittel = v.Average();
                Assert.InRange(mittel, k.MittelLJeMin * 0.99, k.MittelLJeMin * 1.01);
                double streuung = Math.Sqrt(v.Sum(x => (x - mittel) * (x - mittel)) / (v.Length - 1));
                Assert.InRange(streuung, 0.0, k.Kategorie.StreuungLJeMin * 1.05);
                Assert.InRange(streuung, k.Kategorie.StreuungLJeMin * 0.75, double.MaxValue);   // die Kappung staucht nur wenig

                // Anteil an der Energie (±5 %).
                double anteil = eigene.Sum(e => e.EnergieKwh) / gesamt;
                Assert.InRange(anteil, k.AnteilNormiert * 0.95, k.AnteilNormiert * 1.05);
            }
        }

        [Fact]
        public void Der_Erwartungswert_trifft_die_Tagesmenge()
        {
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(Kategorien(), 1, "Zone A");
            List<Zapfereignis>[] tage = Ziehen(32, satz, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag));
            double mittel = tage.Average(t => t.Sum(e => e.EnergieKwh));
            Assert.InRange(mittel, TAG_KWH * 0.99, TAG_KWH * 1.01);
            // Jedes Ereignis liegt im Tag, dauert wie seine Kategorie und trägt keine negative Energie.
            Assert.All(tage.SelectMany(t => t), e =>
            {
                Assert.InRange(e.MinuteBeginn, 0, Bedarfstag.MINUTEN - 1);
                Assert.Contains(e.DauerMin, new[] { 1, 2, 8, 4 });
                Assert.True(e.EnergieKwh >= 0);
            });
        }

        [Fact]
        public void Die_Zeitpunkte_folgen_dem_Tagesgang()
        {
            double[] gang = Gang((5, 0.1), (7, 0.4), (12, 0.2), (21, 0.3));
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(Kategorien(), 1, "Zone A");
            List<Zapfereignis>[] tage = Ziehen(33, satz, Tageszeitdichte.Aus(Struktur(gang), ZapfTagtyp.Samstag), 5000);
            Zapfereignis[] alle = tage.SelectMany(t => t).ToArray();
            var stunden = new int[24];
            var minuten = new int[60];
            foreach (Zapfereignis e in alle)
            {
                stunden[e.MinuteBeginn / 60]++;
                minuten[e.MinuteBeginn % 60]++;
            }
            for (int h = 0; h < 24; h++)
            {
                if (gang[h] == 0) Assert.Equal(0, stunden[h]);
                else Assert.InRange(stunden[h] / (double)alle.Length, gang[h] * 0.95, gang[h] * 1.05);
            }
            // Die Minute in der Stunde ist gleichverteilt (±15 % je Minute bei rund 150 000 Ereignissen).
            Assert.All(minuten, m => Assert.InRange(m, alle.Length / 60.0 * 0.85, alle.Length / 60.0 * 1.15));
        }

        [Fact]
        public void Die_Kappung_begrenzt_den_Volumenstrom_nach_oben_und_unten()
        {
            var kat = new[] { new Zapfkategorie(1, "Testkategorie K (fiktiv)", 3.0, 4.0, 2, 1.0, Fiktiv) { KappungLJeMin = 6.0 } };
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(kat, 1, "Zone A");
            List<Zapfereignis>[] tage = Ziehen(34, satz, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag), 4000);
            double faktor = 2 * Cw * SPREIZUNG_K / 1000.0;
            double[] v = tage.SelectMany(t => t).Select(e => e.EnergieKwh / faktor).ToArray();
            Assert.All(v, x => Assert.InRange(x, 0.0, 6.0 + 1e-12));
            Assert.Contains(v, x => x == 0.0);
            Assert.Contains(v, x => Math.Abs(x - 6.0) < 1e-12);
            Assert.InRange(v.Average(), satz.Werte[0].MittelLJeMin * 0.99, satz.Werte[0].MittelLJeMin * 1.01);
            Assert.InRange(tage.Average(t => t.Sum(e => e.EnergieKwh)), TAG_KWH * 0.99, TAG_KWH * 1.01);
        }

        // =================================================================================
        // Determinismus und Randfälle
        // =================================================================================

        [Fact]
        public void Derselbe_Seed_zieht_dieselben_Ereignisse()
        {
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(Kategorien(), 1, "Zone A");
            Tageszeitdichte dichte = Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag);
            List<Zapfereignis>[] a = Ziehen(35, satz, dichte, 200);
            List<Zapfereignis>[] b = Ziehen(35, satz, dichte, 200);
            List<Zapfereignis>[] c = Ziehen(36, satz, dichte, 200);
            Assert.Equal(a.SelectMany(t => t), b.SelectMany(t => t));
            Assert.NotEqual(a.SelectMany(t => t), c.SelectMany(t => t));
        }

        [Fact]
        public void Ohne_Menge_oder_mit_leerem_Tagesgang_wird_nicht_gezogen()
        {
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(Kategorien(), 1, "Zone A");
            var z = new ZapfZufall(37);
            var frisch = new ZapfZufall(37);
            var liste = new List<Zapfereignis>();
            Zapfereignisgenerator.Ziehen(z, satz, 0.0, SPREIZUNG_K, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag), liste);
            Tageszeitdichte leer = Tageszeitdichte.Aus(Struktur(ruhetagLeer: true), ZapfTagtyp.Ruhetag);
            Assert.True(leer.Leer);
            Zapfereignisgenerator.Ziehen(z, satz, TAG_KWH, SPREIZUNG_K, leer, liste);
            Assert.Empty(liste);
            Assert.Equal(frisch.Naechste(), z.Naechste());
            Assert.Throws<InvalidOperationException>(() => leer.Minute(z));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Zapfereignisgenerator.Ziehen(z, satz, -1.0, SPREIZUNG_K, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag), liste));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Zapfereignisgenerator.Ziehen(z, satz, TAG_KWH, 0.0, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag), liste));
        }

        [Fact]
        public void Ungueltige_Kategorien_werden_benannt_abgelehnt()
        {
            void Abgelehnt(IReadOnlyList<Zapfkategorie> k, string teil)
            {
                var ex = Assert.Throws<ZapfprofilEingabeException>(() => Zapfkategoriensatz.Aus(k, 1, "Zone Nord"));
                Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ex.Fehler);
                Assert.Equal("Zone Nord", ex.Zone);
                Assert.Contains(teil, ex.Message);
            }
            Zapfkategorie basis = Kategorien()[0];
            Abgelehnt(new Zapfkategorie[0], "keine Zapfkategorien");
            Abgelehnt(Kategorien(idArt: 2), "keine Zapfkategorien");
            Abgelehnt(new[] { basis with { VolumenstromLJeMin = -1 } }, "Volumenstrom");
            Abgelehnt(new[] { basis with { StreuungLJeMin = double.NaN } }, "Streuung");
            Abgelehnt(new[] { basis with { DauerMin = 0 } }, "Dauer");
            Abgelehnt(new[] { basis with { DauerMin = 1441 } }, "Dauer");
            Abgelehnt(new[] { basis with { Anteil = -0.1 } }, "Anteil");
            Abgelehnt(new[] { basis with { Anteil = 0.0 } }, "summieren zu 0");
            Abgelehnt(new[] { basis with { KappungLJeMin = 0.0 } }, "Kappung");
            Abgelehnt(new[] { basis with { VolumenstromLJeMin = 0.0, StreuungLJeMin = 0.0 } }, "gestutztes Mittel");

            // Eine Kategorie ohne Anteil neben einer mit Anteil ist erlaubt und zieht nie.
            Zapfkategoriensatz s = Zapfkategoriensatz.Aus(new[] { basis, basis with { Name = "Null", Anteil = 0.0, DauerMin = 3 } }, 1, "Z");
            Assert.Equal(0.0, Zapfereignisgenerator.Rate(s.Werte[1], TAG_KWH, SPREIZUNG_K));
            List<Zapfereignis>[] tage = Ziehen(38, s, Tageszeitdichte.Aus(Struktur(), ZapfTagtyp.Werktag), 500);
            Assert.DoesNotContain(tage.SelectMany(t => t), e => e.DauerMin == 3);
        }

        // =================================================================================
        // Einheiten je Bezugsart
        // =================================================================================

        [Fact]
        public void Die_Einheiten_folgen_der_Bezugsart()
        {
            Parametersatz ps = Parameter();
            ZonenStand z = Zone("Zone A");
            Assert.Equal(12, Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Wohneinheiten), 12.0, ps));
            Assert.Equal(30, Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Personen), 30.0, ps));
            Assert.Equal(15, Zapfeinheiten.Anzahl(z with { PersonenJeWe = 2.0 }, Art(bezug: ZapfBezugsart.Personen), 30.0, ps));
            Assert.Equal(8, Zapfeinheiten.Anzahl(z with { PersonenJeWe = 2.0 }, Art(bezug: ZapfBezugsart.Personen), 15.0, ps)); // 7,5 → 8
            // Fläche: Wohnfläche je WE der Zone, sonst der Parameter (80 m², erfunden).
            Assert.Equal(5, Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Flaeche), 400.0, ps));
            Assert.Equal(4, Zapfeinheiten.Anzahl(z with { WohnflaecheJeWeM2 = 100.0 }, Art(bezug: ZapfBezugsart.Flaeche), 400.0, ps));
            Assert.Throws<ParametersatzException>(() => Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Flaeche), 400.0,
                Parameter(null, ZapfParameter.WOHNEN_FLAECHE_JE_WE)));
            Assert.Equal(40, Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Betten), 40.0, ps));
            Assert.Equal(1, Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Duschplaetze), 0.2, ps));
            // Die Wohnungstabelle geht vor: Σ Anzahl.
            ZonenStand mitTabelle = z with
            {
                Wohnungen = new[] { new WohnungstypStand { Anzahl = 3 }, new WohnungstypStand { Anzahl = 4 } }
            };
            Assert.Equal(7, Zapfeinheiten.Anzahl(mitTabelle, Art(bezug: ZapfBezugsart.Personen), 20.0, ps));
            var ex = Assert.Throws<ZapfprofilEingabeException>(() =>
                Zapfeinheiten.Anzahl(z, Art(bezug: ZapfBezugsart.Betten), Zapfeinheiten.HOECHSTENS + 1.0, ps));
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, ex.Fehler);
        }
    }
}
