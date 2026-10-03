using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Laufhinweise der Aufheizoptimierung</b> (Entwurf KP3, Welle R4; Festlegung 21, Teilkonzept 4.8):
    /// <c>SIMENG_AUFH_W1</c> … <c>W5</c> und der Bemessungshinweis <c>SIMENG_AUFH_W2_BEMESSUNG</c> — je W ein
    /// Fall, beide Kulturen (Text der Sprache, Zahl der Kultur), einmal je Gebäude; im Lauf über die Fassade
    /// (Einzone, Mehrzonenfassung, gekoppelt) einmal je Gebäude und ohne Schalter nie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizHinweisTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizHinweisTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static Aufheizergebnis Geplant(Func<Aufheizergebnis, Aufheizergebnis> setzen)
            => setzen(new Aufheizergebnis
            {
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE,
                AufheizzeitMaxH = 3, AufheizAussenC = -12.0, AufheizLeistungKw = 30.0,
                AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL, Aufheiztage = 40, AufheiztageBegrenzt = 0,
                AufheiztageUnerreichbar = 0, AufheiztageNachweisband = 0, AufheizstundenH = 80, AufheizzeitLaengsteH = 3,
                AufheizspruengeAus = 0, HeizleistungMaxStundenH = 0.0, TageUnterStationaer = 0, SpruengeAusHeizperiode = 0,
                KuerzesteAbsenkdauerH = 13,
            });

        /// <summary>Die Fälle: Schlüssel, Aufheizwerte, erwartete Argumente des Textes.</summary>
        public static IEnumerable<object[]> Faelle()
        {
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                yield return new object[] { kultur, "W1" };
                yield return new object[] { kultur, "W1 Bemessung" };
                yield return new object[] { kultur, "W2" };
                yield return new object[] { kultur, "W2 Bemessung" };
                yield return new object[] { kultur, "W3" };
                yield return new object[] { kultur, "W4" };
                yield return new object[] { kultur, "W5" };
            }
        }

        private static (Aufheizergebnis A, string Schluessel, Func<CultureInfo, string> Text) Fall(string fall)
        {
            switch (fall)
            {
                case "W1":
                    return (Geplant(a => a with { AufheiztageUnerreichbar = 12, TageUnterStationaer = 4 }), "SIMENG_AUFH_W1",
                            k => string.Format(k, R.SIMENG_AUFH_W1, 12.ToString(k), 4.ToString(k)));
                case "W1 Bemessung":
                    return (Geplant(a => a with { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, AufheizzeitMaxH = null }),
                            "SIMENG_AUFH_W1", k => string.Format(k, R.SIMENG_AUFH_W1, 0.ToString(k), 0.ToString(k)));
                case "W2":
                    return (Geplant(a => a with { AufheiztageBegrenzt = 7 }), "SIMENG_AUFH_W2",
                            k => string.Format(k, R.SIMENG_AUFH_W2, 7.ToString(k)));
                case "W2 Bemessung":
                    return (Geplant(a => a with { AufheizzeitMaxH = 13, KuerzesteAbsenkdauerH = 13 }), "SIMENG_AUFH_W2_BEMESSUNG",
                            k => string.Format(k, R.SIMENG_AUFH_W2_BEMESSUNG, 13.ToString(k), 13.ToString(k)));
                case "W3":
                    return (Geplant(a => a with { AufheiztageNachweisband = 5 }), "SIMENG_AUFH_W3",
                            k => string.Format(k, R.SIMENG_AUFH_W3, 5.ToString(k)));
                case "W4":
                    return (Geplant(a => a with { AufheizspruengeAus = 2, SpruengeAusHeizperiode = 1 }), "SIMENG_AUFH_W4",
                            k => string.Format(k, R.SIMENG_AUFH_W4, 2.ToString(k), 1.ToString(k)));
                case "W5":
                    return (new Aufheizergebnis { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, HeizleistungMaxStundenH = 3.5 },
                            "SIMENG_AUFH_W5", k => R.SIMENG_AUFH_W5);
                default:
                    throw new ArgumentOutOfRangeException(nameof(fall), fall, null);
            }
        }

        /// <summary>
        /// <b>Je W ein Fall, beide Kulturen, einmal je Gebäude:</b> genau ein Hinweis mit dem Text der Sprache
        /// und den Zahlen in der Kultur; ein zweiter Aufruf für dasselbe Gebäude fügt nichts hinzu, ein anderes
        /// Gebäude bekommt seinen eigenen. Ein Ergebnis ohne W meldet nichts, ohne Aufheizwerte auch nicht.
        /// </summary>
        [Theory]
        [MemberData(nameof(Faelle))]
        public void Je_W_ein_Hinweis_in_der_Kultur_einmal_je_Gebaeude(string kultur, string fall)
        {
            using var k = new Kulturvorrichtung(kultur);
            (Aufheizergebnis a, string schluessel, Func<CultureInfo, string> text) = Fall(fall);
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            const string wer = "Hinweisprobe (1)";

            Vdi6007Rechenweg.HinweisAufheizung(a, wer);
            Vdi6007Rechenweg.HinweisAufheizung(a, wer);
            string erwartet = "Gebäudemodell VDI 6007: " + wer + " — " + text(CultureInfo.CurrentCulture);
            Assert.Equal(new[] { erwartet }, p.Hinweise.ToArray());
            string kennung = "(" + fall.Split(' ')[0] + ")";
            Assert.Contains(kennung, erwartet, StringComparison.Ordinal);
            Assert.Contains(kultur == "de-DE" ? "Aufheizoptimierung" : "Preheat optimisation", erwartet, StringComparison.Ordinal);
            Assert.False(string.IsNullOrEmpty(R.ResourceManager.GetString(schluessel, CultureInfo.CurrentUICulture)));

            Vdi6007Rechenweg.HinweisAufheizung(a, "Hinweisprobe (2)");
            Assert.Equal(2, p.Hinweise.Count);

            SimulationProtokoll leer = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisAufheizung(Geplant(x => x), wer);
            Vdi6007Rechenweg.HinweisAufheizung(null, wer);
            Vdi6007Rechenweg.HinweisAufheizung(new Aufheizergebnis { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT }, wer);
            Assert.Empty(leer.Hinweise);
        }

        /// <summary>
        /// <b>Die Texte beider Sprachen</b> tragen dieselben Platzhalter; die deutsche Fassung spricht von der
        /// Aufheizoptimierung, die englische von „preheat optimisation".
        /// </summary>
        [Fact]
        public void Beide_Sprachen_tragen_dieselben_Platzhalter()
        {
            string[] schluessel = { "SIMENG_AUFH_W1", "SIMENG_AUFH_W2", "SIMENG_AUFH_W2_BEMESSUNG", "SIMENG_AUFH_W3", "SIMENG_AUFH_W4", "SIMENG_AUFH_W5" };
            foreach (string s in schluessel)
            {
                string de = R.ResourceManager.GetString(s, CultureInfo.GetCultureInfo("de-DE"));
                string en = R.ResourceManager.GetString(s, CultureInfo.GetCultureInfo("en-US"));
                Assert.False(string.IsNullOrEmpty(de) || string.IsNullOrEmpty(en), s);
                Assert.NotEqual(de, en);
                Assert.StartsWith("Aufheizoptimierung (W", de, StringComparison.Ordinal);
                Assert.StartsWith("Preheat optimisation (W", en, StringComparison.Ordinal);
                for (int i = 0; i < 4; i++)
                    Assert.Equal(de.Contains("{" + i + "}", StringComparison.Ordinal), en.Contains("{" + i + "}", StringComparison.Ordinal));
            }
        }

        // =====================================================================
        //  Im Lauf über die Fassade
        // =====================================================================

        private static List<string> Aufheizhinweise(SimulationProtokoll p)
            => p.Hinweise.Where(z => z.Contains("Aufheizoptimierung (W", StringComparison.Ordinal)
                                     || z.Contains("Preheat optimisation (W", StringComparison.Ordinal)).ToList();

        /// <summary>
        /// <b>Im Lauf</b> (Projekt 1018, Gebäude 10632, <c>Heizleistung_Max</c> auf 80 % der Zielleistung des Katalogbaus gesetzt):
        /// Quelle Grenze, W1 an kalten Tagen — der Hinweis steht einmal je Gebäude, auch nach einem zweiten Lauf
        /// im selben Protokoll, mit der Zahl des Ergebnisses; deutsch und englisch; in der Mehrzonenfassung
        /// einmal am Gebäude. Ohne Schalter kein Aufheizhinweis. Das gekoppelte Referenzprojekt 1047 meldet W5.
        /// </summary>
        [Fact]
        public void Im_Lauf_einmal_je_Gebaeude_und_ohne_Schalter_nie()
        {
            if (!_db.Vorhanden) return;
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;
            AufheizLauf.Gebaeudelauf ziel = AufheizLauf.Projekt(1018, an, double.NaN, wahl).Single();
            // Heizleistung_Max gilt im Klassenweg dem Katalogbau, ungeskaliert (B11) — also im Rahmen der Bemessung.
            double grenzeKw = 0.8 * ziel.Plan.Bemessung.AufheizleistungW / 1000.0;
            Action<ProjektGebaeudeModel> knapp = x => x.Heizleistung_Max = grenzeKw;

            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                using var k = new Kulturvorrichtung(kultur);
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                AufheizLauf.Gebaeudelauf eins = AufheizLauf.Projekt(1018, an, double.NaN, wahl, knapp).Single();
                List<string> nachEins = Aufheizhinweise(p);
                AufheizLauf.Projekt(1018, an, double.NaN, wahl, knapp).Single();
                List<string> nachZwei = Aufheizhinweise(p);
                Aufheizergebnis a = eins.Ergebnis.Aufheizung;
                Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GRENZE, a.AufheizLeistungsquelle);
                Assert.True(a.AufheiztageUnerreichbar > 0, "kein W1-Tag");
                Assert.Equal(nachEins, nachZwei);
                string w1 = Assert.Single(nachEins, z => z.Contains("(W1)", StringComparison.Ordinal));
                Assert.Contains(string.Format(CultureInfo.CurrentCulture, R.SIMENG_AUFH_W1,
                                              a.AufheiztageUnerreichbar.Value.ToString(CultureInfo.CurrentCulture),
                                              a.TageUnterStationaer.Value.ToString(CultureInfo.CurrentCulture)), w1, StringComparison.Ordinal);
                foreach (string z in nachEins) Assert.Single(nachEins, x => x == z);
                _aus.WriteLine(kultur + ": " + string.Join(" | ", nachEins));

                // Mehrzonenfassung: einmal am Gebäude.
                SimulationProtokoll pz = SimulationProtokoll.NeuStarten();
                AufheizLauf.Gebaeudelauf mz = AufheizLauf.Projekt(1018, an, double.NaN, wahl,
                    x => { knapp(x); AufheizLauf.Mehrzonenfassung(x); }).Single();
                Assert.NotNull(mz.Mehrzonen);
                List<string> zonen = Aufheizhinweise(pz);
                Assert.NotEmpty(zonen);
                Assert.Equal(zonen.Count, zonen.Distinct().Count());
                Assert.All(zonen, z => Assert.DoesNotContain("Hälfte", z, StringComparison.Ordinal));
            }

            SimulationProtokoll pa = SimulationProtokoll.NeuStarten();
            AufheizLauf.Projekt(1018, Aufheizvorgabe.Aus, double.NaN, wahl, knapp);
            Assert.Empty(Aufheizhinweise(pa));

            SimulationProtokoll pk = SimulationProtokoll.NeuStarten();
            List<AufheizLauf.Gebaeudelauf> gekoppelt = AufheizLauf.Projekt(1047, an);
            Assert.Contains(gekoppelt, l => l.Plan != null && l.Plan.Gekoppelt);
            Assert.Contains(Aufheizhinweise(pk), z => z.Contains("(W5)", StringComparison.Ordinal));
            Assert.All(gekoppelt.Where(l => l.Plan != null && l.Plan.Gekoppelt), l =>
            {
                Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, l.Ergebnis.Aufheizung.AufheizZustand);
                Assert.Null(l.Ergebnis.Aufheizung.AufheizLeistungKw);
                // Mit Kopplung ist der Kappungsanteil der des Heizkreises (B22), Bit für Bit.
                Assert.Equal(BitConverter.DoubleToInt64Bits(l.Ergebnis.Heizkreis.HeizleistungMaxStundenH),
                             BitConverter.DoubleToInt64Bits(l.Ergebnis.Aufheizung.HeizleistungMaxStundenH));
            });
        }
        // =====================================================================
        //  Verbrauchsangabe (Welle D2; B11, Befund R4)
        // =====================================================================

        private static List<string> Verbrauchshinweise(SimulationProtokoll p)
            => p.Hinweise.Where(z => z.Contains("Aufheizoptimierung (Verbrauchsangabe)", StringComparison.Ordinal)
                                     || z.Contains("Preheat optimisation (consumption entry)", StringComparison.Ordinal)).ToList();

        /// <summary>
        /// <b>Hinweis bei Verbrauchsangabe:</b> Wirkt die Rampe am Hotel (1018/10632) mit Verbrauchsangabe (80 MWh/a),
        /// steht einmal je Gebäude der Hinweis, dass die Rückrechnung die Mehrwärme der Rampen in den Faktor aufnimmt
        /// — mit der Zahl der Rampentage, deutsch und englisch, auch nach einem zweiten Lauf im selben Protokoll
        /// einmal. Mit Flächenangabe, ohne Schalter und ohne Rampentag (Testnaht P_auf = +∞) schweigt er.
        /// </summary>
        [Fact]
        public void Verbrauchsangabe_mit_wirkender_Rampe_nennt_den_Hinweis_einmal_je_Gebaeude()
        {
            if (!_db.Vorhanden) return;
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;
            Action<ProjektGebaeudeModel> verbrauch = x =>
            {
                x.Einheit = "Verbrauch  [MWh/a]";
                x.Z_AuswahlWohnflaeche = 80.0;
            };
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                using var k = new Kulturvorrichtung(kultur);
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1018, an, double.NaN, wahl, verbrauch).Single();
                AufheizLauf.Projekt(1018, an, double.NaN, wahl, verbrauch).Single();
                int tage = l.Ergebnis.Aufheizung.Aufheiztage.Value;
                Assert.True(tage > 0);
                string h = Assert.Single(Verbrauchshinweise(p));
                Assert.Equal("Gebäudemodell VDI 6007: Hotel-G-136 (10632) — " +
                             string.Format(CultureInfo.CurrentCulture, R.SIMENG_AUFH_VERBRAUCH, tage.ToString(CultureInfo.CurrentCulture)), h);
                _aus.WriteLine(kultur + ": " + h);
            }

            SimulationProtokoll flaeche = SimulationProtokoll.NeuStarten();
            AufheizLauf.Projekt(1018, an, double.NaN, wahl);
            Assert.Empty(Verbrauchshinweise(flaeche));

            SimulationProtokoll aus = SimulationProtokoll.NeuStarten();
            AufheizLauf.Projekt(1018, Aufheizvorgabe.Aus, double.NaN, wahl, verbrauch);
            Assert.Empty(Verbrauchshinweise(aus));

            SimulationProtokoll naht = SimulationProtokoll.NeuStarten();
            AufheizLauf.Projekt(1018, an, double.PositiveInfinity, wahl, verbrauch);
            Assert.Empty(Verbrauchshinweise(naht));

            string de = R.ResourceManager.GetString("SIMENG_AUFH_VERBRAUCH", CultureInfo.GetCultureInfo("de-DE"));
            string en = R.ResourceManager.GetString("SIMENG_AUFH_VERBRAUCH", CultureInfo.GetCultureInfo("en-US"));
            Assert.NotEqual(de, en);
            Assert.Contains("{0}", de, StringComparison.Ordinal);
            Assert.Contains("{0}", en, StringComparison.Ordinal);
            Assert.DoesNotContain("{1}", de + en, StringComparison.Ordinal);
        }
    }
}
