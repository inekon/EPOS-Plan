using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Reihenkatalog des Klimadatenimports</b> (Auftrag KD-8 Teil A, Konzept Klimadatenimport
    /// § 4a): Katalogwache, Monatsreihen der Vorschau (Summe gegen Mittel, Monatsgrenzen,
    /// Teilfüllung, fehlende Größe), Achsenregel des Monatsbilds, Regel des Jahresverlaufs und die
    /// Erkennung „Quelle führt die Größe nicht“ (NULL oder durchgehend 0).
    /// </summary>
    public class KlimaReihenkatalogTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly int[] TAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Ein Gemeinjahr mit 8 760 Stunden; die Werte setzt <paramref name="setzen"/> je Monat.</summary>
        private static List<TmyHourlyData> Jahr(Action<TmyHourlyData, int> setzen)
        {
            var liste = new List<TmyHourlyData>();
            var t = new DateTime(2021, 1, 1);
            for (int h = 0; h < 8760; h++, t = t.AddHours(1))
            {
                var s = new TmyHourlyData { TimeString = t.ToString("yyyyMMdd:HHmm", CultureInfo.InvariantCulture) };
                setzen(s, t.Month - 1);
                liste.Add(s);
            }
            return liste;
        }

        // =====================================================================
        // Katalogwache
        // =====================================================================

        [Fact]
        public void Jeder_KlimaParameter_steht_im_Katalog_mit_Einheit_und_Regel()
        {
            foreach (KlimaParameter p in Enum.GetValues(typeof(KlimaParameter)))
            {
                KlimaReihe r = KlimaReihenkatalog.Reihe(p);
                Assert.True(r != null, p + " fehlt im Reihenkatalog");
                if (p != KlimaParameter.QualitaetStrahlung) Assert.False(string.IsNullOrEmpty(r.Einheit), p + " ohne Einheit");
                Assert.False(string.IsNullOrEmpty(r.Name()), p + " ohne Namen");
                Assert.False(string.IsNullOrEmpty(r.Kurzname()), p + " ohne Kurznamen");
            }
            Assert.Equal(KlimaReihenkatalog.Reihen.Count, KlimaReihenkatalog.Reihen.Select(r => r.Schluessel).Distinct().Count());
            Assert.Equal(KlimaReihenkatalog.Reihen.Count, KlimaReihenkatalog.Reihen.Select(r => r.Reihenfolge).Distinct().Count());
        }

        /// <summary>Schlüssel und Spalten sind die des Exportverzeichnisses (KD-5) — Grundlage der Vereinigung.</summary>
        [Theory]
        [InlineData("R_TEMPERATUR", "Temperatur", "°C")]
        [InlineData("R_LUFTFEUCHTE", "Luftfeuchte", "%")]
        [InlineData("R_GLOBAL", "Globalstrahlung", "W/m²")]
        [InlineData("R_DIREKT_NORMAL", "Direktstrahlung", "W/m²")]
        [InlineData("R_DIFFUS", "Diffusstrahlung", "W/m²")]
        [InlineData("R_GEGENSTRAHLUNG", "Gegenstrahlung", "W/m²")]
        [InlineData("R_BEDECKUNG", "Bedeckungsgrad", "Achtel")]
        [InlineData("F_NORD", "Sol_Nord", "W/m²")]
        [InlineData("F_OST", "Sol_Ost", "W/m²")]
        [InlineData("F_SUED", "Sol_Sued", "W/m²")]
        [InlineData("F_WEST", "Sol_West", "W/m²")]
        [InlineData("F_SONNENWINKEL", "Sonnenwinkel", "°")]
        [InlineData("ROH_Luftdruck_hPa", "Luftdruck_hPa", "hPa")]
        [InlineData("ROH_Windgeschwindigkeit_m_s", "Windgeschwindigkeit_m_s", "m/s")]
        [InlineData("ROH_Wasserdampfgehalt_g_kg", "Wasserdampfgehalt_g_kg", "g/kg")]
        [InlineData("ROH_Ausstrahlung_W_m2", "Ausstrahlung_W_m2", "W/m²")]
        [InlineData("ROH_Direkt_Horizontal_W_m2", "Direkt_Horizontal_W_m2", "W/m²")]
        public void Schluessel_Spalte_und_Einheit_wie_im_Export(string schluessel, string spalte, string einheit)
        {
            KlimaReihe r = KlimaReihenkatalog.Reihe(schluessel);
            Assert.NotNull(r);
            Assert.Equal(spalte, r.Spalte);
            Assert.Equal(einheit, r.Einheit);
        }

        [Fact]
        public void Strahlung_summiert_sonst_Mittel_Windrichtung_und_Kennung_ohne_Bild()
        {
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen)
            {
                if (r.Parameter is KlimaParameter.Windrichtung or KlimaParameter.QualitaetStrahlung)
                {
                    Assert.Equal(KlimaMonatsregel.KeinBild, r.Monatsregel);
                    Assert.False(r.InVorschau || r.ImJahresverlauf);
                    continue;
                }
                bool strahlung = r.Einheit == "W/m²";
                Assert.Equal(strahlung ? KlimaMonatsregel.Summe : KlimaMonatsregel.Mittel, r.Monatsregel);
                Assert.Equal(strahlung ? "kWh/m²" : r.Einheit, r.Monatseinheit);
            }
        }

        [Fact]
        public void Teil_A_schaltet_nur_die_Spalten_von_Tab_Solar_frei()
        {
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen)
                Assert.Equal(r.Herkunft != KlimaReihenherkunft.Rohwert, r.Freigeschaltet);
            // Temperatur und Sonnenwinkel haben ihre eigenen Reiter.
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.R_TEMPERATUR).ImJahresverlauf);
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.F_SONNENWINKEL).ImJahresverlauf);
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.F_SUED).InVorschau);
        }

        [Fact]
        public void Rohwerte_sind_bis_zur_Freischaltung_verborgen_Gegenprobe_frei()
        {
            string[] erwartet =
            {
                "ROH_Wasserdampfgehalt_g_kg", "ROH_Direkt_Horizontal_W_m2", "ROH_Ausstrahlung_W_m2",
                "ROH_Luftdruck_hPa", "ROH_Windgeschwindigkeit_m_s"
            };
            // Windrichtung und Qualitätskennung haben kein Bild (KD-Q12) und stehen in keiner Wahl.
            var mitBild = KlimaReihenkatalog.Reihen.Where(r => r.Herkunft == KlimaReihenherkunft.Rohwert && r.InVorschau).ToList();
            Assert.Equal(erwartet, mitBild.Select(r => r.Schluessel));
            Assert.All(mitBild, r => Assert.True(r.ImJahresverlauf));
            Assert.False(KlimaReihenkatalog.RohwerteFreigeschaltet);
            Assert.All(mitBild, r => Assert.False(r.Freigeschaltet));

            KlimaReihenkatalog.RohwerteFreigeschaltet = true;
            try
            {
                Assert.All(mitBild, r => Assert.True(r.Freigeschaltet));
                Assert.Equal("", KlimaReihenkatalog.SperrgrundMonatsbild("ROH_Luftdruck_hPa", new[] { KlimaReihenkatalog.R_GLOBAL }));
            }
            finally
            {
                KlimaReihenkatalog.RohwerteFreigeschaltet = false;
            }
        }

        // =====================================================================
        // Monatsreihen der Vorschau
        // =====================================================================

        [Fact]
        public void Monatsreihen_Summe_und_Mittel_an_den_Monatsgrenzen()
        {
            var stunden = Jahr((s, m) =>
            {
                s.Temperature = m;                         // Mittel = Monatsindex
                s.GlobalIrradiance = 100;                  // Summe = Stunden · 0,1 kWh/m²
                s.DiffuseIrradiance = 50;
                s.DirectIrradiance = 20;
                s.Humidity = 80;
            });
            KlimaKennzahlen k = KlimaKennzahlen.Rechnen(stunden);

            double[] g = k.Monatsreihe(KlimaReihenkatalog.R_GLOBAL);
            double[] d = k.Monatsreihe(KlimaReihenkatalog.R_DIFFUS);
            double[] t = k.Monatsreihe(KlimaReihenkatalog.R_TEMPERATUR);
            for (int m = 0; m < 12; m++)
            {
                Assert.Equal(TAGE[m] * 24 * 0.1, g[m], 9);
                Assert.Equal(TAGE[m] * 24 * 0.05, d[m], 9);
                Assert.Equal(m, t[m], 9);
                Assert.Equal(80, k.Monatsreihe(KlimaReihenkatalog.R_LUFTFEUCHTE)[m], 9);
            }
            Assert.Equal(k.MonatssummeGlobalstrahlungKwhM2, g);
            Assert.Equal(8760, k.Reihenstunden[KlimaReihenkatalog.R_LUFTFEUCHTE]);
        }

        [Fact]
        public void Fehlende_Groesse_hat_keine_Reihe_Teilfuellung_mittelt_ueber_vorhandene()
        {
            var stunden = Jahr((s, m) =>
            {
                s.Temperature = 5;
                s.Gegenstrahlung = m == 0 ? 300 : (double?)null;   // nur im Januar
                s.Bedeckungsgrad = null;                            // PVGIS führt ihn nicht
            });
            // Im Januar trägt nur jede zweite Stunde Feuchte.
            for (int i = 0; i < 744; i += 2) stunden[i].Humidity = 60;

            KlimaKennzahlen k = KlimaKennzahlen.Rechnen(stunden);

            Assert.Null(k.Monatsreihe(KlimaReihenkatalog.R_BEDECKUNG));
            Assert.False(k.Reihenstunden.ContainsKey(KlimaReihenkatalog.R_BEDECKUNG));
            Assert.Equal(372, k.Reihenstunden[KlimaReihenkatalog.R_LUFTFEUCHTE]);
            Assert.Equal(60, k.Monatsreihe(KlimaReihenkatalog.R_LUFTFEUCHTE)[0], 9);
            Assert.True(double.IsNaN(k.Monatsreihe(KlimaReihenkatalog.R_LUFTFEUCHTE)[1]));
            Assert.Equal(744 * 0.3, k.Monatsreihe(KlimaReihenkatalog.R_GEGENSTRAHLUNG)[0], 9);
            Assert.True(double.IsNaN(k.Monatsreihe(KlimaReihenkatalog.R_GEGENSTRAHLUNG)[5]));
        }

        [Fact]
        public void Direkt_normal_kommt_aus_der_umgerechneten_Kopie()
        {
            var stunden = Jahr((s, m) => { s.Temperature = 1; s.DirectIrradiance = 10; });
            var normal = stunden.Select(s => { var c = s.Kopie(); c.DirectIrradiance = 30; return c; }).ToList();

            KlimaKennzahlen k = KlimaKennzahlen.Rechnen(stunden, normal);

            Assert.Equal(744 * 0.03, k.Monatsreihe(KlimaReihenkatalog.R_DIREKT_NORMAL)[0], 9);
        }

        // =====================================================================
        // Achsenregel des Monatsbilds (KD-Q10)
        // =====================================================================

        [Fact]
        public void Strahlung_steht_links_sonst_die_zuerst_gewaehlte_Einheit()
        {
            Assert.Equal(new[] { "kWh/m²", "°C" },
                KlimaReihenkatalog.Monatseinheiten(new[] { KlimaReihenkatalog.R_TEMPERATUR, KlimaReihenkatalog.R_GLOBAL }));
            Assert.Equal(new[] { "%", "°C" },
                KlimaReihenkatalog.Monatseinheiten(new[] { KlimaReihenkatalog.R_LUFTFEUCHTE, KlimaReihenkatalog.R_TEMPERATUR }));
        }

        [Fact]
        public void Dritte_Einheit_ist_weich_gesperrt_letzte_Reihe_bleibt()
        {
            var wahl = new List<string> { KlimaReihenkatalog.R_GLOBAL, KlimaReihenkatalog.R_TEMPERATUR };

            string grund = KlimaReihenkatalog.SperrgrundMonatsbild(KlimaReihenkatalog.R_LUFTFEUCHTE, wahl);
            Assert.Contains("Höchstens zwei Einheiten", grund);
            Assert.Contains("kWh/m², °C", grund);
            Assert.Equal("", KlimaReihenkatalog.SperrgrundMonatsbild(KlimaReihenkatalog.R_DIFFUS, wahl));
            Assert.Equal("", KlimaReihenkatalog.SperrgrundMonatsbild(KlimaReihenkatalog.R_TEMPERATUR, wahl));
            Assert.Equal("Mindestens eine Reihe bleibt gewählt.",
                KlimaReihenkatalog.SperrgrundMonatsbild(KlimaReihenkatalog.R_GLOBAL, new[] { KlimaReihenkatalog.R_GLOBAL }));
        }

        private static KlimaKennzahlen Kennzahlen()
            => KlimaKennzahlen.Rechnen(Jahr((s, m) =>
            {
                s.Temperature = m; s.GlobalIrradiance = 100 + m; s.DiffuseIrradiance = 40; s.Humidity = 70;
            }));

        [Fact]
        public void Vorgabebild_gleich_dem_heutigen_Bild_auch_in_anderer_Reihenfolge()
        {
            KlimaKennzahlen k = Kennzahlen();
            Zeichenmodell heute = KlimaVorschaubild.Modell(k);
            Zeichenmodell umgekehrt = KlimaVorschaubild.Modell(k,
                new[] { KlimaReihenkatalog.R_TEMPERATUR, KlimaReihenkatalog.R_GLOBAL });

            Assert.Same(heute.GetType(), umgekehrt.GetType());
            Assert.Equal(heute.Reihen.Select(r => (r.Name, r.Achsenseite)), umgekehrt.Reihen.Select(r => (r.Name, r.Achsenseite)));
            Assert.Equal(heute.Befehle.Count, umgekehrt.Befehle.Count);
        }

        [Fact]
        public void Zwei_Einheiten_zwei_Achsen_dritte_Einheit_faellt_weg()
        {
            KlimaKennzahlen k = Kennzahlen();
            Zeichenmodell bild = KlimaVorschaubild.Modell(k, new[]
            {
                KlimaReihenkatalog.R_LUFTFEUCHTE, KlimaReihenkatalog.R_DIFFUS, KlimaReihenkatalog.R_GLOBAL,
                KlimaReihenkatalog.R_TEMPERATUR
            });

            var links = bild.Reihen.Where(r => r.Achsenseite == Achsenseite.Links).Select(r => r.Name).ToList();
            var rechts = bild.Reihen.Where(r => r.Achsenseite != Achsenseite.Links).Select(r => r.Name).ToList();
            Assert.Equal(2, links.Count);                     // Diffus und Global (kWh/m²) links
            Assert.Single(rechts);                            // Feuchte rechts, Temperatur fällt weg
            Assert.Equal("Relative Luftfeuchte [%]", rechts[0]);
        }

        // =====================================================================
        // Jahresverlauf (KD-Q11) und Reihenbestand
        // =====================================================================

        [Fact]
        public void Jahresverlauf_gleiche_Einheit_fuegt_hinzu_andere_ersetzt_hoechstens_drei()
        {
            IReadOnlyList<string> w = new[] { KlimaReihenkatalog.R_GLOBAL };
            w = KlimaReihenkatalog.JahresverlaufKlick(w, KlimaReihenkatalog.R_DIFFUS, out string g1);
            w = KlimaReihenkatalog.JahresverlaufKlick(w, KlimaReihenkatalog.F_SUED, out _);
            Assert.Equal(3, w.Count);
            Assert.Equal("", g1);

            var voll = KlimaReihenkatalog.JahresverlaufKlick(w, KlimaReihenkatalog.F_NORD, out string g2);
            Assert.Equal(w, voll);
            Assert.Contains("drei", g2);

            var allein = KlimaReihenkatalog.JahresverlaufKlick(w, KlimaReihenkatalog.R_LUFTFEUCHTE, out _);
            Assert.Equal(new[] { KlimaReihenkatalog.R_LUFTFEUCHTE }, allein);

            var bleibt = KlimaReihenkatalog.JahresverlaufKlick(allein, KlimaReihenkatalog.R_LUFTFEUCHTE, out string g3);
            Assert.Equal(allein, bleibt);
            Assert.Equal("Mindestens eine Reihe bleibt gewählt.", g3);
        }

        [Fact]
        public void Null_oder_durchgehend_0_heisst_Quelle_fuehrt_die_Groesse_nicht()
        {
            var zeilen = Enumerable.Range(0, 8760).Select(i => new SolardatenModel
            {
                Außen_Temp = 5,
                Globalstrahlung = i % 24 > 6 && i % 24 < 18 ? 200 : 0,
                Direktstrahlung = 0,                  // nicht nullbar, durchgehend 0
                Luftfeuchte = null,                   // nullbar, durchgehend NULL
                Gegenstrahlung = 300,
                Bedeckungsgrad = i == 5 ? 0 : null    // eine 0, sonst NULL → führt nicht
            }).ToList();

            var bestand = KlimaReihenkatalog.AusRechenspalten(zeilen).ToDictionary(w => w.Reihe.Schluessel);

            Assert.True(bestand[KlimaReihenkatalog.R_GLOBAL].Gefuehrt);
            Assert.True(bestand[KlimaReihenkatalog.R_GEGENSTRAHLUNG].Gefuehrt);
            Assert.False(bestand[KlimaReihenkatalog.R_DIREKT_NORMAL].Gefuehrt);
            Assert.False(bestand[KlimaReihenkatalog.R_LUFTFEUCHTE].Gefuehrt);
            Assert.False(bestand[KlimaReihenkatalog.R_BEDECKUNG].Gefuehrt);
            Assert.True(double.IsNaN(bestand[KlimaReihenkatalog.R_LUFTFEUCHTE].Werte[0]));
            Assert.False(bestand.ContainsKey(KlimaReihenkatalog.R_TEMPERATUR));
            Assert.False(bestand.ContainsKey(KlimaReihenkatalog.F_SONNENWINKEL));
            Assert.Equal(new[] { KlimaReihenkatalog.R_LUFTFEUCHTE, KlimaReihenkatalog.R_GLOBAL, KlimaReihenkatalog.R_DIREKT_NORMAL,
                                 KlimaReihenkatalog.R_DIFFUS, KlimaReihenkatalog.R_GEGENSTRAHLUNG, KlimaReihenkatalog.F_SUED,
                                 KlimaReihenkatalog.F_WEST, KlimaReihenkatalog.F_OST, KlimaReihenkatalog.F_NORD,
                                 KlimaReihenkatalog.R_BEDECKUNG },
                         KlimaReihenkatalog.AusRechenspalten(zeilen).Select(w => w.Reihe.Schluessel));
        }

        [Fact]
        public void Jahresverlauf_zeichnet_eine_Einheit_bis_drei_Reihen()
        {
            var zeilen = Enumerable.Range(0, 8760).Select(i => new SolardatenModel
            {
                Globalstrahlung = i % 300, Diffusstrahlung = i % 100, Sol_Sued = i % 200, Sol_Nord = 3, Luftfeuchte = 50
            }).ToList();
            var bestand = KlimaReihenkatalog.AusRechenspalten(zeilen).ToDictionary(w => w.Reihe.Schluessel);

            Zeichenmodell bild = KlimaVorschaubild.Jahresverlauf(new[]
            {
                bestand[KlimaReihenkatalog.R_GLOBAL], bestand[KlimaReihenkatalog.R_LUFTFEUCHTE],
                bestand[KlimaReihenkatalog.R_DIFFUS], bestand[KlimaReihenkatalog.F_SUED], bestand[KlimaReihenkatalog.F_NORD]
            });

            Assert.Equal(3, bild.Reihen.Count);
            Assert.DoesNotContain(bild.Reihen, r => r.Name.Contains("Luftfeuchte"));
            Assert.Null(KlimaVorschaubild.Jahresverlauf(Array.Empty<KlimaReihenwerte>()));
        }
    }
}
