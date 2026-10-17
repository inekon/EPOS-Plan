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
    /// Erkennung „Quelle führt die Größe nicht“ (NULL oder durchgehend 0). Teil B: die Rohwert-Reihen
    /// aus der Rohreihe (Vorschau) und aus <see cref="KlimaExportDaten"/> (Jahresverlauf), die
    /// Sichtbarkeit aus dem Datenstand und der Export als Alias des Katalogs.
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
        public void Rechen_und_gerechnete_Spalten_immer_sichtbar_Rohwerte_nur_freigeschaltet()
        {
            // Teil B: Die Freischaltung kommt aus dem Datenstand (Schritt 238); der Katalog kennt nur die Regel.
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen)
            {
                Assert.Equal(r.Herkunft == KlimaReihenherkunft.Rohwert, r.IstRohwert);
                Assert.Equal(!r.IstRohwert, KlimaReihenkatalog.Sichtbar(r, false));
                Assert.True(KlimaReihenkatalog.Sichtbar(r, true));
            }
            Assert.False(KlimaReihenkatalog.Sichtbar(null, true));
            // Temperatur und Sonnenwinkel haben ihre eigenen Reiter.
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.R_TEMPERATUR).ImJahresverlauf);
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.F_SONNENWINKEL).ImJahresverlauf);
            Assert.False(KlimaReihenkatalog.Reihe(KlimaReihenkatalog.F_SUED).InVorschau);
        }

        [Fact]
        public void Fuenf_Rohwerte_mit_Bild_Spalten_der_Rohwerttabelle()
        {
            string[] erwartet =
            {
                "ROH_Wasserdampfgehalt_g_kg", "ROH_Direkt_Horizontal_W_m2", "ROH_Ausstrahlung_W_m2",
                "ROH_Luftdruck_hPa", "ROH_Windgeschwindigkeit_m_s"
            };
            // Windrichtung und Qualitätskennung haben kein Bild (KD-Q12) und stehen in keiner Wahl.
            var mitBild = KlimaReihenkatalog.Reihen.Where(r => r.IstRohwert && r.InVorschau).ToList();
            Assert.Equal(erwartet, mitBild.Select(r => r.Schluessel));
            Assert.All(mitBild, r => Assert.True(r.ImJahresverlauf));
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen.Where(r => r.IstRohwert))
            {
                Assert.Contains(r.Spalte, KlimaRohwerteSchema.ROHSPALTEN);
                Assert.Equal(KlimaReihenkatalog.ROH_PREFIX + r.Spalte, r.Schluessel);
            }
            Assert.Equal("", KlimaReihenkatalog.SperrgrundMonatsbild("ROH_Luftdruck_hPa", new[] { KlimaReihenkatalog.R_GLOBAL }));
        }

        [Fact]
        public void Export_nimmt_Schluessel_Spalte_und_Einheit_aus_dem_Katalog()
        {
            Assert.Equal(KlimaReihenkatalog.ROH_PREFIX, KlimaExportOptionen.ROH_PREFIX);
            Assert.Equal(KlimaReihenkatalog.R_GLOBAL, KlimaExportOptionen.R_GLOBAL);
            Assert.Equal(KlimaReihenkatalog.F_SONNENWINKEL, KlimaExportOptionen.F_SONNENWINKEL);
            foreach (KlimaReihe r in KlimaReihenkatalog.Reihen)
            {
                KlimaExportGroesse g = KlimaExportOptionen.Groesse(r.Schluessel);
                Assert.NotNull(g);
                Assert.Equal(r.Spalte, g.Spalte);
                Assert.Equal(r.Einheit, g.Einheit);
                Assert.Equal(r.IstRohwert, g.Art == KlimaWertart.Rohwert);
                Assert.Equal(r.Herkunft == KlimaReihenherkunft.Gerechnet, g.Gerechnet);
            }
        }

        [Fact]
        public void MonatVonStunde_folgt_dem_Gemeinjahr()
        {
            Assert.Equal(0, KlimaReihenkatalog.MonatVonStunde(1));
            Assert.Equal(0, KlimaReihenkatalog.MonatVonStunde(744));
            Assert.Equal(1, KlimaReihenkatalog.MonatVonStunde(745));
            Assert.Equal(2, KlimaReihenkatalog.MonatVonStunde(1417));
            Assert.Equal(11, KlimaReihenkatalog.MonatVonStunde(8017));
            Assert.Equal(11, KlimaReihenkatalog.MonatVonStunde(8760));
            Assert.Equal(-1, KlimaReihenkatalog.MonatVonStunde(0));
            Assert.Equal(-1, KlimaReihenkatalog.MonatVonStunde(8761));
            // Dieselben Grenzen wie der Zeitstempel der Rechenspalten: Stunde h liegt am 1.1. + (h − 1) h.
            var t = new DateTime(2021, 1, 1);
            for (int h = 1; h <= 8760; h += 97)
                Assert.Equal(t.AddHours(h - 1).Month - 1, KlimaReihenkatalog.MonatVonStunde(h));
        }

        [Fact]
        public void Monatsreihen_der_Rohwerte_aus_der_Rohreihe_Mittel_und_Summe()
        {
            // Stunden mit Temperatur, dazu die Rohreihe: Luftdruck 1000 + Monat (Mittel), Ausstrahlung 100 W/m² je
            // Stunde (Summe in kWh/m²), Wind nur im Januar (Teilfüllung), Wasserdampf nie (die Quelle führt ihn nicht).
            List<TmyHourlyData> stunden = Jahr((s, m) => s.Temperature = 5);
            var roh = new KlimaRohreihe { Zeitbezug = KlimaRohwerteSchema.ZEITBEZUG_MEZ };
            for (int h = 1; h <= 8760; h++)
                roh.Stunden.Add(new KlimaRohstunde
                {
                    Stunde = h, Luftdruck_hPa = 1000 + KlimaReihenkatalog.MonatVonStunde(h), Ausstrahlung = 100,
                    Windgeschwindigkeit_m_s = h <= 744 ? 2.0 : (double?)null
                });
            KlimaKennzahlen k = KlimaKennzahlen.Rechnen(stunden, null, roh);
            double[] druck = k.Monatsreihe("ROH_Luftdruck_hPa");
            Assert.NotNull(druck);
            for (int m = 0; m < 12; m++) Assert.Equal(1000 + m, druck[m], 9);
            double[] aus = k.Monatsreihe("ROH_Ausstrahlung_W_m2");
            Assert.Equal(TAGE[0] * 24 * 100 / 1000.0, aus[0], 9);
            Assert.Equal(TAGE[1] * 24 * 100 / 1000.0, aus[1], 9);
            Assert.Equal(8760, k.Reihenstunden["ROH_Ausstrahlung_W_m2"]);
            double[] wind = k.Monatsreihe("ROH_Windgeschwindigkeit_m_s");
            Assert.Equal(2.0, wind[0], 9);
            Assert.True(double.IsNaN(wind[1]));
            Assert.Equal(744, k.Reihenstunden["ROH_Windgeschwindigkeit_m_s"]);
            Assert.Null(k.Monatsreihe("ROH_Wasserdampfgehalt_g_kg"));
            Assert.Null(k.Monatsreihe("ROH_Direkt_Horizontal_W_m2"));
            // Die Rechenspalten bleiben, wie ohne Rohreihe; ohne Rohreihe gibt es keine Rohwert-Reihe.
            KlimaKennzahlen ohne = KlimaKennzahlen.Rechnen(stunden);
            Assert.Equal(ohne.Monatsreihe(KlimaReihenkatalog.R_TEMPERATUR), k.Monatsreihe(KlimaReihenkatalog.R_TEMPERATUR));
            Assert.DoesNotContain(ohne.Monatsreihen.Keys, s => s.StartsWith(KlimaReihenkatalog.ROH_PREFIX, StringComparison.Ordinal));
        }

        [Fact]
        public void AusRohwerten_Region_ohne_Rohdaten_oder_leere_Spalte_fuehrt_nicht()
        {
            var daten = new KlimaExportDaten { IdRegion = 1, Rohdaten = true };
            daten.Rohwerte[KlimaRohwerteSchema.SPALTE_LUFTDRUCK] = Enumerable.Range(0, 8760).Select(i => 1000.0 + i % 24).ToArray();
            daten.Rohwerte[KlimaRohwerteSchema.SPALTE_WASSERDAMPF] = Enumerable.Repeat(double.NaN, 8760).ToArray();
            IReadOnlyList<KlimaReihenwerte> reihen = KlimaReihenkatalog.AusRohwerten(daten);
            Assert.Equal(new[] { "ROH_Wasserdampfgehalt_g_kg", "ROH_Direkt_Horizontal_W_m2", "ROH_Ausstrahlung_W_m2",
                                 "ROH_Luftdruck_hPa", "ROH_Windgeschwindigkeit_m_s" },
                         reihen.Select(w => w.Reihe.Schluessel));
            Assert.All(reihen, w => Assert.Equal(8760, w.Werte.Length));
            Assert.All(reihen, w => Assert.True(w.Reihe.IstRohwert));
            KlimaReihenwerte druck = reihen.Single(w => w.Reihe.Schluessel == "ROH_Luftdruck_hPa");
            Assert.True(druck.Gefuehrt);
            Assert.Equal(1001.0, druck.Werte[1]);
            Assert.False(reihen.Single(w => w.Reihe.Schluessel == "ROH_Wasserdampfgehalt_g_kg").Gefuehrt);   // nur NULL
            Assert.False(reihen.Single(w => w.Reihe.Schluessel == "ROH_Ausstrahlung_W_m2").Gefuehrt);       // keine Spalte
            // Altbestand (KD-Q7): Rohdaten = 0 - keine Reihe führt, auch nicht mit Werten im Satz.
            daten.Rohdaten = false;
            Assert.All(KlimaReihenkatalog.AusRohwerten(daten), w => Assert.False(w.Gefuehrt));
            Assert.Empty(KlimaReihenkatalog.AusRohwerten(null));
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
