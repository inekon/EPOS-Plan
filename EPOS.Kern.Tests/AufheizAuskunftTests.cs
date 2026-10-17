using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Auskunft = Lauf</b> (Entwurf KP3, Welle D2; Grundsatz 3, Festlegungen 3 und 16, B11, B14; Abschnitt 7
    /// „Export, Auskunft") — die Aufheizbemessung ohne Jahreslauf
    /// (<see cref="SimulationWaermebedarf.AufheizbemessungEinesGebaeudes"/>,
    /// <see cref="GebaeudeBedarfCtrl.Aufheizbemessung"/>) liefert je Gebäude dieselben Zahlen wie die Aufheizwerte
    /// des Laufs — <b>bitgleich</b>: Zustand, Variante, t_auf,max, T_a,B, P_auf samt Quelle und Faktor.
    /// <list type="bullet">
    /// <item>alle Gebäude der sechzehn Referenzprojekte über die Projekteinstellung (1040 auf dem
    /// Tagesbilanz-Weg ohne Bemessung, 1047 gekoppelt);</item>
    /// <item>Faktor ≠ 1 (1018 mit Flächenangabe: Eingabe und Faktor), mit Grenze, mit Verbrauchsangabe (Faktor
    /// erst im Lauf), Variante (b), Testnaht P_auf = +∞;</item>
    /// <item>Mehrzonen (Mehrzonenfassung mit ausdrücklicher Zuordnung und mit Trennwand nach der 4-K-Regel);</item>
    /// <item>mit Konditionierungssatz (Heizkalender mit Heizperiode am Projektgebäude) — samt Gegenprobe: ohne
    /// den Satz gebaut (wie <see cref="SimulationWaermebedarf.UebergabeEingang"/>) entstünde eine andere
    /// Bemessung (B14).</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizAuskunftTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public AufheizAuskunftTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose() => _db.Dispose();

        private static readonly Aufheizvorgabe AN = new Aufheizvorgabe(true, null, null, null, null);

        private static int Klimaregion(int projekt)
        {
            var p = new ProjektCtrl();
            p.ReadSingle(projekt);
            return p.m_ID_Klimaregion;
        }

        /// <summary>Die Auskunft für die frisch gelesenen Gebäude des Projekts — Vorgabe und Testnaht wie <see cref="AufheizLauf"/>.</summary>
        private static List<Aufheizauskunft> Auskunft(int projekt, Aufheizvorgabe vorgabe, double testW = double.NaN,
                                                      Func<ProjektGebaeudeModel, bool> auswahl = null,
                                                      Action<ProjektGebaeudeModel> umbau = null)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt };
            sim.KlimakalenderLesen(Klimaregion(projekt));
            sim.AufheizvorgabeProjekt = vorgabe;
            sim.Vdi6007weg.AufheizleistungTestW = testW;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projekt);
            var liste = new List<Aufheizauskunft>();
            for (int i = 0; i < ctrl.rows; i++)
            {
                ProjektGebaeudeModel item = ctrl.items[i];
                if (!Gebaeuderechenweg.IstVdi6007(item.Gebaeude_Modell)) continue;
                if (auswahl != null && !auswahl(item)) continue;
                umbau?.Invoke(item);
                liste.Add(sim.AufheizbemessungEinesGebaeudes(item));
            }
            return liste;
        }

        /// <summary>Auskunft und Lauf eines Gebäudes bitgleich — gegen die Aufheizwerte des Ergebnisträgers (skaliert).</summary>
        private static void Gleich(Aufheizauskunft a, GebaeudeModellErgebnis lauf, string wo)
        {
            Aufheizergebnis l = lauf.Aufheizung;
            Assert.True(l != null, wo + ": der Lauf hat keine Aufheizwerte");
            Assert.True(a.Befund == null, wo + ": " + a.Befund);
            Assert.Equal(l.AufheizZustand, a.Zustand);
            Assert.Equal(l.AufheizBemessung, a.Bemessung);
            Assert.Equal(l.AufheizzeitMaxH, a.AufheizzeitMaxH);
            Assert.Equal(l.AufheizLeistungsquelle, a.Quelle);
            Bit(l.AufheizAussenC, a.AussenC, wo + ", T_a,B");
            if (a.Skalierungsfaktor.HasValue)
            {
                Bit(lauf.Skalierungsfaktor, a.Skalierungsfaktor, wo + ", Faktor");
                Bit(l.AufheizLeistungKw, a.LeistungKw, wo + ", P_auf");
            }
            else
            {
                // Verbrauchsangabe: P_auf am Katalogbau mal dem Faktor des Laufs ist die Zahl des Laufs.
                Assert.True(a.FaktorErstImLauf, wo);
                Assert.Null(a.LeistungKw);
                Bit(l.AufheizLeistungKw, a.LeistungUnskaliertKw * lauf.Skalierungsfaktor, wo + ", P_auf");
            }
        }

        private static void Bit(double? soll, double? ist, string wo)
        {
            Assert.True(soll.HasValue == ist.HasValue, wo + ": " + soll + " gegen " + ist);
            if (soll.HasValue)
                Assert.True(BitConverter.DoubleToInt64Bits(soll.Value) == BitConverter.DoubleToInt64Bits(ist.Value),
                            wo + ": " + soll.Value.ToString("R", CultureInfo.InvariantCulture) + " gegen " +
                            ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Die sechzehn Referenzprojekte über die Projekteinstellung
        // =============================================================================

        /// <summary>
        /// <b>Alle Gebäude der sechzehn Referenzprojekte</b> mit eingeschalteter Aufheizoptimierung in der
        /// Datenbank: <see cref="GebaeudeBedarfCtrl.Aufheizbemessung"/> nennt je Gebäude die Werte des Laufs
        /// (<see cref="SimulationWaermebedarf.Waermebedarf_berechnen"/>), bitgleich; der Tagesbilanz-Weg (1040)
        /// bemisst nicht, das gekoppelte 1047 ist GEKOPPELT. Mit ausgeschalteter Optimierung ist die Auskunft leer.
        /// </summary>
        [Fact]
        public void In_allen_sechzehn_Projekten_nennt_die_Auskunft_die_Zahlen_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            int gebaeude = 0, gekoppelt = 0, tagesbilanz = 0, faktor = 0;
            foreach (int projekt in AufheizLauf.Referenzprojekte)
            {
                Assert.Empty(GebaeudeBedarfCtrl.Aufheizbemessung(projekt, Klimaregion(projekt)));
                Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, AN));
                IReadOnlyList<Aufheizauskunft> auskunft = GebaeudeBedarfCtrl.Aufheizbemessung(projekt, Klimaregion(projekt));
                var lauf = new SimulationWaermebedarf();
                lauf.Waermebedarf_berechnen(projekt, Klimaregion(projekt));
                Assert.Equal(lauf.GebaeudeKennzahlenListe.Count, auskunft.Count);
                for (int i = 0; i < auskunft.Count; i++)
                {
                    Aufheizauskunft a = auskunft[i];
                    string wo = "Projekt " + projekt + ", Gebäude " + a.ID_Gebaeude;
                    Assert.Equal(lauf.GebaeudeKennzahlenListe[i].ID_Gebaeude, a.ID_Gebaeude);
                    GebaeudeModellErgebnis vdi = lauf.GebaeudeErgebnisse.Ergebnis(i);
                    if (vdi == null)
                    {
                        Assert.True(a.Tagesbilanz, wo);
                        Assert.Null(a.Zustand);
                        tagesbilanz++;
                        continue;
                    }
                    Gleich(a, vdi, wo);
                    // Dieselbe Zahl wie die Ergebniszeile (Tab_ErgebnisGebaeude).
                    ErgebnisGebaeudeModel zeile = lauf.GebaeudeKennzahlenListe[i];
                    Assert.Equal(zeile.AufheizZustand, a.Zustand);
                    Bit(zeile.AufheizLeistungKw, a.LeistungKw, wo + ", Zeile");
                    if (a.Zustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT) gekoppelt++;
                    if (a.Skalierungsfaktor is double f && f != 1.0) faktor++;
                    gebaeude++;
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Auskunft = Lauf: {0} VDI-Gebäude bitgleich (davon {1} GEKOPPELT, {2} mit Faktor ≠ 1), {3} auf dem Tagesbilanz-Weg",
                gebaeude, gekoppelt, faktor, tagesbilanz));
            Assert.Equal(17, gebaeude);
            Assert.Equal(1, tagesbilanz);
            Assert.True(gekoppelt >= 1);
            Assert.True(faktor >= 1);
        }

        // =============================================================================
        //  Faktor, Grenze, Verbrauchsangabe, Variante (b), Testnaht
        // =============================================================================

        public static IEnumerable<object[]> Faelle() => new[]
        {
            new object[] { "Ziel" }, new object[] { "Grenze" }, new object[] { "Verbrauch" },
            new object[] { "Verbrauch Grenze" }, new object[] { "Abzug" }, new object[] { "Testnaht" },
        };

        /// <summary>
        /// <b>Faktor ≠ 1</b> am Hotel (1018/10632, Flächenangabe, Faktor ≈ 0,25): die Zielleistung; die Grenze
        /// <c>Heizleistung_Max</c> (P_auf am Katalogbau = Eingabe, skaliert = Eingabe · Faktor, B11); die
        /// Verbrauchsangabe (80 MWh/a — der Faktor entsteht erst im Lauf; P_auf am Katalogbau mal dem Faktor des
        /// Laufs ist dessen Zahl), auch mit Grenze; Variante (b); die Testnaht P_auf = +∞. Jedes Mal bitgleich.
        /// </summary>
        [Theory]
        [MemberData(nameof(Faelle))]
        public void Faktor_Grenze_Verbrauch_Variante_und_Testnaht_wie_der_Lauf(string fall)
        {
            if (!_db.Vorhanden) return;
            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;
            Aufheizvorgabe vorgabe = fall == "Abzug" ? new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 3.0, 0.1, null) : AN;
            double testW = fall == "Testnaht" ? double.PositiveInfinity : double.NaN;
            double grenzeKw = 0.8 * AufheizLauf.Projekt(1018, AN, double.NaN, wahl).Single().Plan.Bemessung.AufheizleistungW / 1000.0;
            Action<ProjektGebaeudeModel> umbau = x =>
            {
                if (fall.Contains("Grenze", StringComparison.Ordinal)) x.Heizleistung_Max = grenzeKw;
                if (fall.StartsWith("Verbrauch", StringComparison.Ordinal))
                {
                    x.Einheit = "Verbrauch  [MWh/a]";
                    x.Z_AuswahlWohnflaeche = 80.0;
                }
            };

            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1018, vorgabe, testW, wahl, umbau).Single();
            Aufheizauskunft a = Auskunft(1018, vorgabe, testW, wahl, umbau).Single();
            Gleich(a, l.Ergebnis, fall);
            Assert.NotEqual(1.0, l.Ergebnis.Skalierungsfaktor);
            Assert.Equal(fall.StartsWith("Verbrauch", StringComparison.Ordinal), a.FaktorErstImLauf);
            if (fall.Contains("Grenze", StringComparison.Ordinal))
            {
                Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GRENZE, a.Quelle);
                Assert.Equal(1000.0 * grenzeKw / 1000.0, a.LeistungUnskaliertKw);
            }
            if (fall == "Abzug") Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, a.Bemessung);
            if (fall == "Testnaht") Assert.Equal(double.PositiveInfinity, a.LeistungKw);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: {1}, t_auf,max {2}, T_a,B {3}, P_auf {4} kW (Katalogbau {5} kW, Faktor {6}), Quelle {7}",
                fall, a.Zustand, a.AufheizzeitMaxH, a.AussenC, a.LeistungKw, a.LeistungUnskaliertKw,
                a.Skalierungsfaktor?.ToString("R", CultureInfo.InvariantCulture) ?? "erst im Lauf", a.Quelle));
        }

        // =============================================================================
        //  Mehrzonen
        // =============================================================================

        /// <summary>
        /// <b>Mehrzonen</b> (Festlegung 22): die Mehrzonenfassung des Hotels — mit ausdrücklich zugeordneter
        /// Trennwand und mit einer Trennwand nach der 4-K-Regel (adiabater Vorlauf) —, auch mit Grenze in beiden
        /// Hälften: Zustand, Variante, t_auf,max als Maximum, T_a,B als Minimum, P_auf als Summe, die Quelle; Faktor 1.
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Mehrzonen_wie_der_Lauf(bool vierKRegel, bool grenze)
        {
            if (!_db.Vorhanden) return;
            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;
            Action<ProjektGebaeudeModel> umbau = x =>
            {
                if (grenze) x.Heizleistung_Max = 30.0;
                AufheizLauf.Mehrzonenfassung(x);
                if (!vierKRegel) return;
                GebaeudeZonensatz h = x.Zonen[0];
                List<BauteilEingang> regel = h.Bauteile.Select(b => b.Rand == Bauteilrand.Zone && b.Bezeichnung == "Trennwand"
                                                                     ? b.MitZuordnung(Trennflaechenzuordnung.Regel) : b).ToList();
                x.Zonen = new[] { new GebaeudeZonensatz(h.ZonenId, h.Bezeichnung, regel, h.Nutzflaeche_M2, h.Eingaben, h.Rang),
                                  x.Zonen[1], x.Zonen[2] };
            };
            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1018, AN, double.NaN, wahl, umbau).Single();
            Assert.NotNull(l.Mehrzonen);
            Assert.Equal(vierKRegel ? 1 : 0, l.Mehrzonen.Paare.Count);
            Aufheizauskunft a = Auskunft(1018, AN, double.NaN, wahl, umbau).Single();
            Gleich(a, l.Ergebnis, "Mehrzonen" + (vierKRegel ? ", 4-K-Regel" : "") + (grenze ? ", Grenze" : ""));
            Assert.Equal(1.0, a.Skalierungsfaktor);
            Assert.Equal(l.Mehrzonen.Aufheizgebaeude.Zustand, a.Zustand);
            if (grenze) Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GRENZE, a.Quelle);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Mehrzonen (4-K {0}, Grenze {1}): {2}, t_auf,max {3}, T_a,B {4}, P_auf {5} kW, {6}",
                vierKRegel, grenze, a.Zustand, a.AufheizzeitMaxH, a.AussenC, a.LeistungKw, a.Quelle));
        }

        // =============================================================================
        //  Mit Konditionierungssatz (B14)
        // =============================================================================

        /// <summary>
        /// <b>Mit Konditionierungssatz</b> (B14): Das Hotel bekommt einen Heizkalender (Wochenende 16 °C, Heizperiode
        /// 1.10. bis 30.4., außerhalb „aus"). Die Auskunft baut mit dem Satz wie der Lauf und nennt seine Zahlen;
        /// ein Eingang ohne Satz — so baut <see cref="SimulationWaermebedarf.UebergabeEingang"/> — käme zu anderen
        /// Sprüngen (Gegenprobe mit demselben Eingangsbauer ohne Satz).
        /// </summary>
        [Fact]
        public void Mit_Konditionierungssatz_wie_der_Lauf_und_anders_als_ohne()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(10632);
            KonditionierungCtrl.Ergebnis r = new KonditionierungCtrl().Anlegen(eigner, Heizperiode(), Konditionierungsgroesse.Heizsoll);
            Assert.True(r.Ok, r.Meldung);

            Func<ProjektGebaeudeModel, bool> wahl = x => x.ID_Gebaeude == 10632;
            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1018, AN, double.NaN, wahl).Single();
            Aufheizauskunft a = Auskunft(1018, AN, double.NaN, wahl).Single();
            Gleich(a, l.Ergebnis, "Heizkalender");
            Assert.Contains(l.Ergebnis.Heizsollwert, double.IsNaN);
            Assert.True(l.Ergebnis.HeizkalenderWirksam);

            // Gegenprobe ohne Satz.
            var sim = new SimulationWaermebedarf { m_ID_Projekt = 1018 };
            sim.KlimakalenderLesen(Klimaregion(1018));
            sim.AufheizvorgabeProjekt = AN;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1018);
            ProjektGebaeudeModel item = ctrl.items.Take(ctrl.rows).Single(wahl);
            KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
            GebaeudeModellEingang ohne = GebaeudeModellEingang.Bauen(item, k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad,
                                                                    k.Breitengrad, sim.Vdi6007weg.Zeitbezug, sim.KuehlbetriebProjekt);
            Assert.False(ohne.HeizkalenderWirksam);
            Aufheizplan ohneSatz = Aufheizoptimierung.Planen(Aufheizzone.Aus(ZonenEingang.Einzeln(ohne)), AN);
            Assert.NotEqual(l.Plan.Spruenge.Count, ohneSatz.Spruenge.Count);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Heizkalender: Auskunft = Lauf ({0}, t_auf,max {1}, {2} Sprünge); ohne Satz {3} Sprünge, t_auf,max {4}",
                a.Zustand, a.AufheizzeitMaxH, l.Plan.Spruenge.Count, ohneSatz.Spruenge.Count,
                ohneSatz.Bemessung?.Wirksam.AufheizzeitMaxH));
        }

        // =============================================================================
        //  Die Naht der Oberfläche
        // =============================================================================

        /// <summary>
        /// <b>Die Herleitungszeile</b> (Naht <c>AufheizHerleitung</c> in <c>SimulationErgebnisHuelle.ParameterGaben</c>):
        /// Ohne Schalter keine Zeile; mit Schalter je Gebäude des Projekts eine Zeile aus derselben Auskunft — am Hotel
        /// (1018, Faktor ≠ 1) mit Katalogbau und Faktor —, deutsch und englisch, die Zahlen in der Kultur.
        /// </summary>
        [Fact]
        public void Die_Huelle_belegt_die_Herleitungszeile_aus_der_Auskunft()
        {
            if (!_db.Vorhanden) return;
            EPOS.UI.Seiten.Simulation.SimulationParameterDienste wege =
                SimulationErgebnisHuelle.Erzeugen(null, 1018, new BedarfsZustand()).ParameterGaben();
            Assert.Empty(wege.AufheizHerleitung());
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, AN));
            Aufheizauskunft a = GebaeudeBedarfCtrl.Aufheizbemessung(1018, Klimaregion(1018)).Single();
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                using var k = new Kulturvorrichtung(kultur);
                string zeile = Assert.Single(wege.AufheizHerleitung());
                Assert.Equal(AufheizHerleitungszeile.Zeile(AufheizHerleitungszeile.Aus(a), CultureInfo.CurrentCulture), zeile);
                Assert.StartsWith(a.Gebaeudename + ": ", zeile, StringComparison.Ordinal);
                Assert.Contains(a.LeistungKw.Value.ToString("0.0", CultureInfo.CurrentCulture) + " kW", zeile, StringComparison.Ordinal);
                Assert.Contains(a.Skalierungsfaktor.Value.ToString("0.###", CultureInfo.CurrentCulture), zeile, StringComparison.Ordinal);
                Assert.Contains(kultur == "de-DE" ? "Katalogbau" : "catalogue building", zeile, StringComparison.Ordinal);
                _aus.WriteLine(kultur + ": " + zeile);
            }
        }

        /// <summary>Die Matrix des Probegebäudes mit Wochenende 16 °C und Heizperiode 1.10. bis 30.4. (Muster KonditionierungCtrlTests).</summary>
        private static Vorgabematrix Heizperiode()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = 16.0;
            Matrixeingang b = Konditionierungseingang.Bestand(g, false, false);
            var vorgaben = new List<Vorgabezeile>
            {
                new Vorgabezeile { IdGebaeude = 1, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL, Zeile = DbWerte.KOND_ZEILE_SAISON, Von = 274, Bis = 120 },
            };
            return Vorgabematrix.Bilden(b, vorgaben);
        }
    }
}
