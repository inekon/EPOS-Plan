using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>N-AH12 — die manuelle Aufheizzeit je Gebäude</b> (Entscheid E59 (1); Entwurf KP3 Abschnitt 7,
    /// Festlegungen 37–39; Lesart t in Stunden, n = t + 1): Ein Gebäude mit <c>Aufheizzeit_Manuell_H</c> rampt an
    /// jedem Sprung mit n = min(t + 1, D + 1, 48), ohne Aufschlag; seine Zonen erben t, unbeheizte bleiben ohne
    /// Rampe; die Bemessung läuft weiter (Herleitung) und entscheidet nicht; die Ergebniszeile trägt
    /// <c>Aufheiz_Art</c> = MANUELL, <c>Aufheizzeit_Max_H</c> = t, Zustand BEMESSEN; der Export
    /// <c>Geb[n].Aufheizart</c> und <c>Geb[n].Aufheizzeit_Manuell</c> nur, wenn gesetzt; Katalogkopie → NULL,
    /// Duplikat trägt t; AK1 bleibt W5; Schalter aus bitgleich „aus". Dazu die Zone GEKOPPELT in der Ergebniszeile
    /// (Befund D2) und der Aufheizzuschlag nach E60 (Festlegung 41).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizManuellTests : IDisposable
    {
        private const int STUNDEN = 8760;
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public AufheizManuellTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private static readonly Aufheizvorgabe AN = new Aufheizvorgabe(true, null, null, null, null);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>Tag/Nacht 20/17 °C, Nacht 22–6 Uhr (D = 8), die Grenze für n = 4 am Bemessungsfall.</summary>
        private static Aufheizzone Standard(int? manuell, double heizMaxW = double.NaN)
            => AufheizRaenderTests.Zone(AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22),
                                        heizMaxW: double.IsNaN(heizMaxW) ? AufheizRaenderTests.Grenze(20.0, 3.0, -12.0, 4) : heizMaxW)
               with { ManuellH = manuell };

        // =====================================================================
        //  Einzone
        // =====================================================================

        /// <summary>
        /// <b>Jeder Sprung n = t + 1</b> (≤ D + 1, ≤ 48), mit und ohne Aufschlag, täglich und fest: t = 1 → 2,
        /// t = 5 → 6, t = 47 → 9 (W2 an jedem Tag); Zustand BEMESSEN, Art MANUELL, t in der Ergebniszeile, W1 entfällt;
        /// die Bemessung ist dieselbe wie ohne manuellen Wert; die Rampe folgt Festlegung 8.
        /// </summary>
        [Theory]
        [InlineData(1, false, false, 2)]
        [InlineData(5, false, false, 6)]
        [InlineData(47, false, false, 9)]
        [InlineData(5, true, false, 6)]
        [InlineData(1, true, true, 2)]
        [InlineData(47, true, true, 9)]
        public void N_AH12_Jeder_Sprung_rampt_mit_t_plus_1(int t, bool aufschlag, bool fest, int erwartet)
        {
            Aufheizvorgabe v = new Aufheizvorgabe(true, null, null, null, fest ? DbWerte.AUFHEIZ_ART_FEST : null,
                                                  aufschlag ? 24 : (int?)null, aufschlag ? 100.0 : (double?)null);
            Aufheizzone z = Standard(t);
            Aufheizplan ohne = Aufheizoptimierung.Planen(Standard(null), v);
            Aufheizplan p = Aufheizoptimierung.Planen(z, v);

            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, p.Art);
            Assert.Equal(t, p.ManuellH);
            Assert.Equal(t, p.AufheizzeitMaxH);
            Assert.Equal(ohne.Bemessung, p.Bemessung);
            Assert.Equal(3, p.Bemessung.Wirksam.AufheizzeitMaxH);
            Assert.Equal(365, p.Spruenge.Count);
            Assert.All(p.Spruenge, sp =>
            {
                Assert.Equal(erwartet, sp.N);
                Assert.Equal(t + 1 > 9, sp.Begrenzt);
                Assert.False(sp.Unerreichbar);
            });
            Assert.Equal(0, p.TageUnerreichbar);
            Assert.Equal(t + 1 > 9 ? 365 : 0, p.TageBegrenzt);
            Assert.Equal(365, p.Aufheiztage);
            Assert.Equal(erwartet - 1, p.LaengsteRampeH);
            AufheizRaenderTests.Vorschrift(z, p);
        }

        /// <summary>
        /// <b>Die Bemessung entscheidet nicht:</b> Ist sie unerreichbar (P_auf = 0,95·Φ_stat), rampt das Gebäude
        /// trotzdem mit t + 1, Zustand BEMESSEN, kein W1-Tag; die Bemessung selbst bleibt UNERREICHBAR (Herleitung),
        /// der Aufheizzuschlag 0 (Festlegung 41).
        /// </summary>
        [Fact]
        public void N_AH12_Unerreichbare_Bemessung_entscheidet_nicht()
        {
            double grenze = 0.95 * AufheizRaenderTests.PhiStat(20.0, -12.0);
            Aufheizplan p = Aufheizoptimierung.Planen(Standard(5, grenze), AN);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, p.Bemessung.Zustand);
            Assert.Null(p.Bemessung.Wirksam.AufheizzeitMaxH);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
            Assert.Equal(5, p.AufheizzeitMaxH);
            Assert.Equal(0, p.TageUnerreichbar);
            Assert.Equal(0, p.TageUnterStationaer);
            Assert.All(p.Spruenge, sp => Assert.Equal(6, sp.N));
            Assert.Equal(0.0, p.AufheizzuschlagW);

            Aufheizplan ohne = Aufheizoptimierung.Planen(Standard(null, grenze), AN);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, ohne.Zustand);
            Assert.Equal(365, ohne.TageUnerreichbar);
        }

        /// <summary><b>Der Deckel</b> am Montag nach dem Büro-Wochenende (D = 61): t = 47 rampt 48 Stufen, Werktage D + 1 = 14 (W2).</summary>
        [Fact]
        public void N_AH12_Deckel_am_Montag()
        {
            double[] soll = AufheizRaenderTests.Buero(20.0, 16.0, out bool[] we);
            Aufheizzone z = AufheizRaenderTests.Zone(soll, heizMaxW: AufheizRaenderTests.Grenze(20.0, 4.0, -12.0, 4)) with { ManuellH = 47 };
            Aufheizplan p = Aufheizoptimierung.Planen(z, AN);
            foreach (Aufheizsprung sp in p.Spruenge)
                if (AufheizRaenderTests.IstMontag(we, sp.Sprungstunde / 24))
                {
                    Assert.Equal(48, sp.N);
                    Assert.False(sp.Begrenzt);
                }
                else
                {
                    Assert.Equal(14, sp.N);
                    Assert.True(sp.Begrenzt);
                }
            Assert.Equal(47, p.LaengsteRampeH);
            AufheizRaenderTests.Vorschrift(z, p);
        }

        /// <summary><b>AK1 bleibt W5:</b> ein gekoppeltes Gebäude mit manuellem Wert bleibt GEKOPPELT, die Reihe dieselbe Instanz.</summary>
        [Fact]
        public void N_AH12_Gekoppelt_bleibt_W5()
        {
            Aufheizzone z = Standard(5) with { Gekoppelt = true };
            Aufheizplan p = Aufheizoptimierung.Planen(z, AN);
            Assert.True(p.Gekoppelt);
            Assert.Null(p.Art);
            Assert.Same(z.Soll, p.Reihe);
            Assert.False(p.Geaendert);
        }

        /// <summary>
        /// <b>Der Aufheizzuschlag</b> (E60, P17 (b), Festlegung 41): mit Zielleistung ρ·Φ_stat (Variante (a)), mit Grenze
        /// <c>Heizleistung_Max</c> − Φ_stat, nie unter 0; die Bemessung mit manuellem Wert liefert denselben Zuschlag.
        /// </summary>
        [Fact]
        public void Der_Aufheizzuschlag_nach_Festlegung_41()
        {
            Aufheizplan ziel = Aufheizoptimierung.Planen(
                AufheizRaenderTests.Zone(AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22)), AN);
            Aufheizbemessung b = ziel.Bemessung;
            Assert.False(b.QuelleGrenze);
            double phi = AufheizRaenderTests.PhiStat(20.0, -12.0);
            Assert.Equal(phi, b.PhiStatAuslegungW, 9);
            Assert.True(Math.Abs(b.AufheizzuschlagW - 0.2 * phi) <= 1e-9 * phi, "Ziel: Φ_RH = ρ·Φ_stat");

            double grenze = AufheizRaenderTests.Grenze(20.0, 3.0, -12.0, 4);
            Aufheizplan g = Aufheizoptimierung.Planen(Standard(null), AN);
            Assert.True(g.Bemessung.QuelleGrenze);
            Assert.Equal(Bits(grenze - g.Bemessung.PhiStatAuslegungW), Bits(g.AufheizzuschlagW));
            Assert.Equal(Bits(g.AufheizzuschlagW), Bits(Aufheizoptimierung.Planen(Standard(7), AN).AufheizzuschlagW));
        }

        // =====================================================================
        //  Zonen
        // =====================================================================

        /// <summary>
        /// <b>Zonen erben:</b> Im Dreizonengebäude mit t = 5 rampt jede beheizte Zone an jedem Sprung mit min(6, D + 1),
        /// der Keller bleibt UNBEHEIZT; der Aufschlag wirkt nicht; je Zone die Bemessung wie ohne Wert; Gebäude und
        /// Zonen tragen Art MANUELL und t; die Gebäudewerte nennen die bemessene Zeit getrennt.
        /// </summary>
        [Fact]
        public void N_AH12_Zonen_erben_den_Wert_des_Gebaeudes()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            Mehrzonenergebnis ohne = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                                                           aufheizvorgabe: AufheizMehrzonenTests.An());
            g.Aufheizzeit_Manuell_H = 5;
            Mehrzonenergebnis mit = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                                                          aufheizvorgabe: AufheizMehrzonenTests.An());
            Mehrzonenergebnis mitAufschlag = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                aufheizvorgabe: new Aufheizvorgabe(true, null, null, null, null, 24, 100.0));
            int spruenge = 0;
            foreach (int id in new[] { AufheizMehrzonenTests.WOHNUNG_1, AufheizMehrzonenTests.WOHNUNG_2 })
            {
                Aufheizplan a = ohne.Eingaenge[AufheizMehrzonenTests.Stelle(ohne.Eingaenge, id)].Aufheizplan;
                Aufheizplan p = mit.Eingaenge[AufheizMehrzonenTests.Stelle(mit.Eingaenge, id)].Aufheizplan;
                Aufheizplan q = mitAufschlag.Eingaenge[AufheizMehrzonenTests.Stelle(mitAufschlag.Eingaenge, id)].Aufheizplan;
                Assert.Equal(5, p.ManuellH);
                Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, p.Art);
                Assert.Equal(a.Bemessung, p.Bemessung);
                Assert.All(p.Spruenge, sp => Assert.Equal(Math.Min(6, sp.AbsenkdauerH + 1), sp.N));
                Assert.Equal(p.Spruenge, q.Spruenge);
                spruenge += p.Spruenge.Count;
                for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(p.Reihe[h]), Bits(q.Reihe[h]));
            }
            Assert.True(spruenge > 0);
            Aufheizplan keller = mit.Eingaenge[AufheizMehrzonenTests.Stelle(mit.Eingaenge, AufheizMehrzonenTests.KELLER)].Aufheizplan;
            Assert.True(keller.Unbeheizt);
            Assert.False(keller.Geaendert);

            Aufheizgebaeude geb = mit.Aufheizgebaeude;
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, geb.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, geb.Art);
            Assert.Equal(5, geb.AufheizzeitMaxH);
            Assert.Equal(5, geb.ManuellH);
            Assert.Equal(ohne.Aufheizgebaeude.AufheizzeitMaxH, geb.AufheizzeitBemessenH);
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, mit.Gebaeude.Aufheizung.AufheizArt);
            Assert.Equal(5, mit.Gebaeude.Aufheizung.AufheizzeitManuellH);
            foreach (GebaeudeModellErgebnis z in mit.Zonen)
                if (z.Aufheizung != null && z.Aufheizung.Geplant)
                    Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, z.Aufheizung.AufheizArt);
            _aus.WriteLine("Zonen mit t = 5: {0} Sprünge, Rampenstunden {1} h (ohne Wert {2} h), bemessen {3} h",
                           spruenge, geb.AufheizstundenH, ohne.Aufheizgebaeude.AufheizstundenH, geb.AufheizzeitBemessenH);
        }

        // =====================================================================
        //  Lauf, Ergebniszeile, Export, Auskunft, Kopierwege (Testdatenbank)
        // =====================================================================

        /// <summary>
        /// <b>Am Testdatenbankgebäude (1018):</b> Mit t = 5 tragen Kennzahlzeile und Export Art MANUELL, t = 5,
        /// BEMESSEN und die Variante; die Auskunft nennt den manuellen und den bemessenen Wert nebeneinander samt τ₂;
        /// ohne Wert ist die Art TAEGLICH und es gibt keinen Schlüssel <c>Aufheizzeit_Manuell</c>; Schalter aus mit Wert
        /// rechnet Bit für Bit wie ohne Wert.
        /// </summary>
        [Fact]
        public void N_AH12_Lauf_Ergebniszeile_Export_und_Auskunft()
        {
            if (!_db.Vorhanden) return;
            int idGebaeude = GebaeudeVon(1018);

            // Schalter aus: der Wert wirkt nicht.
            double[] ausOhne = (double[])Bedarf(1018).GebaeudeErgebnisse.Ergebnis(0).HeizlastW.Clone();
            Manuell(idGebaeude, 5);
            SimulationWaermebedarf ausMit = Bedarf(1018);
            Assert.Null(ausMit.GebaeudeErgebnisse.Ergebnis(0).Aufheizung);
            Gleich(ausOhne, ausMit.GebaeudeErgebnisse.Ergebnis(0).HeizlastW);
            Assert.Empty(GebaeudeErgebnisexport.Saetze(ausMit).Single().Texte);

            // Schalter an ohne Wert: TAEGLICH.
            Manuell(idGebaeude, null);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, AN));
            SimulationWaermebedarf ohne = Bedarf(1018);
            ErgebnisGebaeudeModel zeileOhne = ohne.GebaeudeKennzahlenListe.Single();
            Assert.Equal(DbWerte.AUFHEIZ_ART_TAEGLICH, zeileOhne.AufheizArt);
            GebaeudeExportsatz satzOhne = GebaeudeErgebnisexport.Saetze(ohne).Single();
            Assert.Contains(new KeyValuePair<string, string>("Geb[0].Aufheizart", DbWerte.AUFHEIZ_ART_TAEGLICH), satzOhne.Texte);
            Assert.DoesNotContain(satzOhne.Skalare, p => p.Key == "Geb[0].Aufheizzeit_Manuell");

            // Schalter an mit t = 5.
            Manuell(idGebaeude, 5);
            SimulationWaermebedarf mit = Bedarf(1018);
            ErgebnisGebaeudeModel zeile = mit.GebaeudeKennzahlenListe.Single();
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, zeile.AufheizArt);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, zeile.AufheizZustand);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, zeile.AufheizBemessung);
            Assert.Equal(5, zeile.AufheizzeitMaxH);
            Assert.Equal(0, zeile.AufheiztageUnerreichbar);
            Assert.Equal(zeileOhne.AufheizLeistungKw, zeile.AufheizLeistungKw);
            Assert.Equal(zeileOhne.AufheizAussenC, zeile.AufheizAussenC);
            Assert.Equal(zeileOhne.AufheizzuschlagKw, zeile.AufheizzuschlagKw);
            Assert.True(zeile.AufheizzuschlagKw >= 0.0);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Saetze(mit).Single();
            Assert.Contains(new KeyValuePair<string, string>("Geb[0].Aufheizart", DbWerte.AUFHEIZ_ART_MANUELL), satz.Texte);
            Assert.Equal(5.0, satz.Skalare.Single(p => p.Key == "Geb[0].Aufheizzeit_Manuell").Value);
            Assert.Equal(5.0, satz.Skalare.Single(p => p.Key == "Geb[0].AufheizzeitMaxH").Value);

            Aufheizauskunft a = GebaeudeBedarfCtrl.Aufheizbemessung(1018, Klimaregion(1018)).Single();
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, a.Art);
            Assert.Equal(5, a.AufheizzeitManuellH);
            Assert.Equal(zeileOhne.AufheizzeitMaxH, a.AufheizzeitMaxH);
            Assert.Equal(zeileOhne.AufheizZustand, a.Zustand);
            Assert.True(a.Tau2H > 0.0);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1018: ohne Wert t_auf,max {0} h, Rampentage {1}, Rampenstunden {2} h; t = 5: Rampentage {3}, Rampenstunden {4} h, " +
                "laengste {5} h; tau2 {6:F2} h; P_auf {7:F2} kW, Zuschlag {8:F2} kW, Phi_HL {9}",
                zeileOhne.AufheizzeitMaxH, zeileOhne.Aufheiztage, zeileOhne.AufheizstundenH, zeile.Aufheiztage,
                zeile.AufheizstundenH, zeile.AufheizzeitLaengsteH, a.Tau2H, zeile.AufheizLeistungKw, zeile.AufheizzuschlagKw,
                zeile.AuslegungsheizlastKw?.ToString("F2", CultureInfo.InvariantCulture) ?? "NULL"));
        }

        /// <summary>
        /// <b>Die Ergebniszeile in der Datenbank:</b> Ein Lauf mit t = 5 schreibt <c>Aufheiz_Art</c> = MANUELL und
        /// <c>Aufheizzeit_Max_H</c> = 5; eine Zonenzeile mit GEKOPPELT und Art geht hin und zurück (Befund D2).
        /// </summary>
        [Fact]
        public void N_AH12_Die_Ergebniszeile_traegt_MANUELL_und_die_Zone_GEKOPPELT()
        {
            if (!_db.Vorhanden) return;
            Manuell(GebaeudeVon(1018), 5);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, AN));
            int kopf = new SimulationRunner().SimuliereUndSpeichere(1018, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            DataRow r = DataRepository.GetDataTable(
                "SELECT Aufheiz_Art, Aufheizzeit_Max_H, Aufheiz_Zustand, Aufheiz_Bemessung, Aufheizzuschlag_Kw " +
                "FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?", new DbParam("?", kopf)).Rows.Cast<DataRow>().Single();
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, Convert.ToString(r[0], CultureInfo.InvariantCulture));
            Assert.Equal(5L, Convert.ToInt64(r[1], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, Convert.ToString(r[2], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, Convert.ToString(r[3], CultureInfo.InvariantCulture));
            Assert.NotEqual(DBNull.Value, r[4]);

            ErgebnisModel m = new ErgebnisCtrl().Load(1018);
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, m.Gebaeude.Single().AufheizArt);
            m.Gebaeude[0].Zonen.Add(new ErgebnisZoneModel
            {
                Rang = 1, Bezeichner = "Halle", IstBeheizt = true, HeizwaermeMwh = 1.5,
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, AufheizArt = DbWerte.AUFHEIZ_ART_MANUELL, HeizleistungMaxStundenH = 2.5,
            });
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            ErgebnisZoneModel z = new ErgebnisCtrl().Load(1018).Gebaeude.Single().Zonen.Single();
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, z.AufheizZustand);
            Assert.Equal(DbWerte.AUFHEIZ_ART_MANUELL, z.AufheizArt);
            Assert.Equal(2.5, z.HeizleistungMaxStundenH);
        }

        /// <summary>
        /// <b>Die Kopierwege</b> (Festlegung 38): Katalog → Projekt lässt die manuelle Aufheizzeit NULL (der Katalog führt
        /// sie nicht), das Projektduplikat und die Variante tragen sie, das Speichern des Gebäudes lässt sie stehen.
        /// </summary>
        [Fact]
        public void N_AH12_Katalogkopie_NULL_Duplikat_und_Variante_tragen_den_Wert()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Laurentiuskirche";
            int quelle = GebaeudeVon(1007);
            Manuell(quelle, 7);

            DataRow stamm = DataRepository.GetDataTable("SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            DataRow zuordnung = DataRepository.GetDataTable("SELECT ID, ID_Projekt FROM Z_ProjektGebaeude WHERE ID_Projekt = 1007 ORDER BY ID LIMIT 1").Rows[0];
            int idNeu = new GebaeudeStammCtrl().CopyFromStamm(Convert.ToString(stamm["Bezeichner"], CultureInfo.InvariantCulture), 1007,
                                                              Convert.ToInt32(zuordnung["ID"], CultureInfo.InvariantCulture));
            Assert.True(idNeu > 0);
            object kopie = DataRepository.ExecuteScalar("SELECT Aufheizzeit_Manuell_H FROM Tab_Gebaeude WHERE ID = ?", new DbParam("?", idNeu));
            Assert.True(kopie == null || kopie == DBNull.Value, "Die Katalogkopie trägt eine manuelle Aufheizzeit.");
            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ?",
                                                                          new DbParam("?", idNeu)), CultureInfo.InvariantCulture));
            Assert.Equal(7L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT Aufheizzeit_Manuell_H FROM Tab_Gebaeude WHERE ID = ?",
                                                                          new DbParam("?", quelle)), CultureInfo.InvariantCulture));

            int neu = new ProjektDuplizierenCtrl().Duplizieren(NAME, NAME + " manuell");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Contains(7L, Werte(neu));
            int variante = new VariantenCtrl().AnlegenAusStamm(1007, NAME, "manuell", out string fehler);
            Assert.True(variante > 0, "Variante: " + fehler);
            Assert.Contains(7L, Werte(variante));

            // Der Sichtleser liefert den Wert; das Speichern des Gebäudes (Spaltenliste ohne das Feld) lässt ihn stehen.
            var leser = new ProjektGebaeudeCtrl();
            leser.ReadAll(1007);
            ProjektGebaeudeModel gelesen = leser.items.Single(x => x.ID_Gebaeude == quelle);
            Assert.Equal(7, gelesen.Aufheizzeit_Manuell_H);
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static int GebaeudeVon(int projekt)
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("?", projekt)),
                               CultureInfo.InvariantCulture);

        private static void Manuell(int idGebaeude, int? wert)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Aufheizzeit_Manuell_H = ? WHERE ID = ?",
                                                     new DbParam("?", wert.HasValue ? (object)wert.Value : DBNull.Value),
                                                     new DbParam("?", idGebaeude)));

        private static List<long> Werte(int projekt)
        {
            var liste = new List<long>();
            foreach (DataRow r in DataRepository.GetDataTable("SELECT Aufheizzeit_Manuell_H FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                                                              new DbParam("?", projekt)).Rows)
                if (r[0] != DBNull.Value) liste.Add(Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private static int Klimaregion(int projekt)
        {
            var p = new ProjektCtrl();
            p.ReadSingle(projekt);
            return p.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf Bedarf(int projekt)
        {
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(projekt, Klimaregion(projekt));
            return sim;
        }

        private static void Gleich(double[] a, double[] b)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                Assert.True(Bits(a[h]) == Bits(b[h]), "Stunde " + h.ToString(CultureInfo.InvariantCulture));
        }
    }
}
