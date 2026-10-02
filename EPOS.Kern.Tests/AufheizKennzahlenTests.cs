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
    /// <b>Die Aufheizwerte in der Ergebniszeile</b> (Entwurf KP3, Welle D2; Abschnitt 4, Grundsatz 4,
    /// Festlegungen 25 und 26) — <see cref="GebaeudeKennzahlen"/> übernimmt
    /// <see cref="GebaeudeModellErgebnis.Aufheizung"/> in die Zeile des Gebäudes (<c>Tab_ErgebnisGebaeude</c>)
    /// und je Zone (<c>Tab_ErgebnisZone</c>), und <see cref="ErgebnisCtrl"/> schreibt und liest jede Spalte.
    ///
    /// <para><b>Geprüft wird:</b> die NULL-Regeln an einem Datensatz ohne Datenbank (Schalter aus,
    /// UNERREICHBAR ohne t_auf,max, GEKOPPELT nur mit Kappungsstunden, P_auf = +∞ der Testnaht und P_auf = 0
    /// als NULL, eine gekoppelte Zone ohne Aufheizwerte, die Bemessung nur am Gebäude); das Füllen aus einem
    /// Lauf mit Schalter an — einzonig über den Projektschalter in der Datenbank und in der Mehrzonenfassung
    /// (zwei beheizte Hälften, ein unbeheizter Nebenraum) mit Sommerlüftung — samt Rundreise über Speichern
    /// und Lesen; die Sommerlüftungsstunden sind NULL ohne Sommerlüftung und die Zahl des Modells mit ihr
    /// (Festlegung 26, B17).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizKennzahlenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public AufheizKennzahlenTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1018 mit dem Gebäude 10632 (Hotel): die Rampe greift an rund 20 Tagen (R2).</summary>
        private const int PROJEKT = 1018;
        private const int GEBAEUDE = 10632;

        private static readonly Aufheizvorgabe AN = new Aufheizvorgabe(true, null, null, null, null);

        private static Aufheizergebnis Bemessen() => new Aufheizergebnis
        {
            AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG,
            AufheizzeitMaxH = 4, AufheizAussenC = -16.25, AufheizLeistungKw = 41.5,
            AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL, Aufheiztage = 37, AufheiztageBegrenzt = 2,
            AufheiztageUnerreichbar = 1, AufheiztageNachweisband = 3, AufheizstundenH = 71, AufheizzeitLaengsteH = 4,
            AufheizspruengeAus = 5, HeizleistungMaxStundenH = 0.75,
        };

        // =============================================================================
        //  Teil 1 - die NULL-Regeln der Festlegung 25 (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// Schalter aus heißt keine Zeile; BEMESSEN überträgt jeden Wert, die Bemessung nur am Gebäude;
        /// UNERREICHBAR hat kein t_auf,max; GEKOPPELT trägt am Gebäude nur Zustand und Kappungsstunden, an der
        /// Zone gar nichts (die Zonenspalte kennt den Zustand nicht); P_auf = +∞ (Testnaht) und P_auf = 0 bleiben
        /// NULL, ebenso nicht endliche Kappungsstunden; GEMISCHT gibt es an der Zone nicht.
        /// </summary>
        [Fact]
        public void Die_NULL_Regeln_der_Festlegung_25()
        {
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(null, zone: false));
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(new Aufheizergebnis(), zone: false));

            Aufheizergebnis b = Bemessen();
            Aufheizkennzahlen g = GebaeudeKennzahlen.Aufheizwerte(b, zone: false);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, g.AufheizZustand);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, g.AufheizBemessung);
            Assert.Equal(4, g.AufheizzeitMaxH);
            Assert.Equal(-16.25, g.AufheizAussenC);
            Assert.Equal(41.5, g.AufheizLeistungKw);
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, g.AufheizLeistungsquelle);
            Assert.Equal(new int?[] { 37, 2, 1, 3, 71, 4, 5 },
                         new[] { g.Aufheiztage, g.AufheiztageBegrenzt, g.AufheiztageUnerreichbar, g.AufheiztageNachweisband,
                                 g.AufheizstundenH, g.AufheizzeitLaengsteH, g.AufheizspruengeAus });
            Assert.Equal(0.75, g.HeizleistungMaxStundenH);
            Assert.Equal(new[] { "AufheizzeitMaxH", "AufheizAussenC", "AufheizLeistungKw", "Aufheiztage", "AufheiztageBegrenzt",
                                 "AufheiztageUnerreichbar", "AufheiztageNachweisband", "AufheizstundenH", "AufheizzeitLaengsteH",
                                 "AufheizspruengeAus", "HeizleistungMaxStundenH" },
                         g.Zahlen().Select(p => p.Key));

            Aufheizkennzahlen z = GebaeudeKennzahlen.Aufheizwerte(b, zone: true);
            Assert.Null(z.AufheizBemessung);
            Assert.Equal(g with { AufheizBemessung = null }, z);

            Aufheizkennzahlen u = GebaeudeKennzahlen.Aufheizwerte(
                b with { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR }, zone: false);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, u.AufheizZustand);
            Assert.Null(u.AufheizzeitMaxH);
            Assert.Equal(1, u.AufheiztageUnerreichbar);
            Assert.DoesNotContain("AufheizzeitMaxH", u.Zahlen().Select(p => p.Key));

            var gekoppelt = new Aufheizergebnis { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, HeizleistungMaxStundenH = 3.5 };
            Aufheizkennzahlen k = GebaeudeKennzahlen.Aufheizwerte(gekoppelt, zone: false);
            Assert.Equal(new Aufheizkennzahlen { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, HeizleistungMaxStundenH = 3.5 }, k);
            Assert.Equal(new[] { "HeizleistungMaxStundenH" }, k.Zahlen().Select(p => p.Key));
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(gekoppelt, zone: true));

            var unbeheizt = new Aufheizergebnis { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, HeizleistungMaxStundenH = 0.0 };
            Assert.Equal(new Aufheizkennzahlen { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, HeizleistungMaxStundenH = 0.0 },
                         GebaeudeKennzahlen.Aufheizwerte(unbeheizt, zone: true));
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(unbeheizt, zone: false));

            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(b with { AufheizLeistungKw = double.PositiveInfinity }, false).AufheizLeistungKw);
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(b with { AufheizLeistungKw = 0.0 }, false).AufheizLeistungKw);
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(b with { AufheizLeistungKw = null }, false).AufheizLeistungKw);
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(b with { HeizleistungMaxStundenH = double.NaN }, false).HeizleistungMaxStundenH);
            Aufheizergebnis gemischt = b with { AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_GEMISCHT };
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GEMISCHT, GebaeudeKennzahlen.Aufheizwerte(gemischt, false).AufheizLeistungsquelle);
            Assert.Null(GebaeudeKennzahlen.Aufheizwerte(gemischt, true).AufheizLeistungsquelle);
        }

        // =============================================================================
        //  Teil 2 - Füllen aus dem Lauf und Rundreise
        // =============================================================================

        /// <summary>
        /// <b>Einzonig über den Projektschalter:</b> Der Lauf mit eingeschalteter Aufheizoptimierung schreibt die
        /// Aufheizspalten des Gebäudes — dieselben Werte, die <see cref="GebaeudeKennzahlen"/> aus den Aufheizwerten
        /// des Ergebnisträgers (skaliert) bildet, P_auf mit dem Faktor der Spitzen —, liest sie gleich zurück, und
        /// die Sommerlüftungsstunden bleiben ohne Sommerlüftung NULL. Ohne Schalter sind alle Spalten NULL.
        /// </summary>
        [Fact]
        public void Der_Lauf_mit_Schalter_schreibt_die_Aufheizspalten_und_ohne_Sommerlueftung_NULL()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(PROJEKT, AN));

            var laeufer = new SimulationRunner();
            Assert.True(laeufer.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            SimulationWaermebedarf wb = laeufer.simulation_Waermebedarf;
            ErgebnisGebaeudeModel zeile = wb.GebaeudeKennzahlenListe.Single();
            GebaeudeModellErgebnis vdi = wb.GebaeudeErgebnisse.Ergebnis(0);
            Aufheizergebnis a = vdi.Aufheizung;
            Assert.NotNull(a);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, zeile.AufheizZustand);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, zeile.AufheizBemessung);
            Assert.Equal(a.AufheizzeitMaxH, zeile.AufheizzeitMaxH);
            Assert.Equal(a.AufheizAussenC, zeile.AufheizAussenC);
            Assert.Equal(a.AufheizLeistungKw, zeile.AufheizLeistungKw);
            Assert.Equal(vdi.Skalierungsfaktor, a.Skalierungsfaktor);
            Assert.True(zeile.Aufheiztage > 0, "keine Rampe");
            Assert.Equal(a.HeizleistungMaxStundenH, zeile.HeizleistungMaxStundenH);
            Assert.Null(zeile.SommerlueftungsstundenH);
            Assert.False(vdi.SommerlueftungGesetzt);

            ErgebnisGebaeudeModel geladen = new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single();
            Assert.Equal(Abdruck(zeile), Abdruck(geladen));
            Assert.Null(geladen.SommerlueftungsstundenH);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ErgebnisGebaeude g INNER JOIN Tab_Ergebnis e ON e.ID = g.ID_Ergebnis " +
                "WHERE e.ID_Projekt = ? AND g.Sommerlueftungsstunden_H IS NOT NULL", new DbParam("@p", PROJEKT))));
            _aus.WriteLine("Projekt 1018 mit Schalter: " + Abdruck(geladen));

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(PROJEKT, Aufheizvorgabe.Aus));
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out fehler) > 0, fehler);
            Assert.Equal(string.Join("|", Enumerable.Repeat("~", 14)), Abdruck(new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single()));
        }

        /// <summary>
        /// <b>Mehrzonenfassung mit Sommerlüftung:</b> Gebäudezeile und je Zone die Zeile aus
        /// <see cref="GebaeudeKennzahlen.Bilden"/> tragen die Aufheizwerte des Laufs (die beheizten Hälften
        /// BEMESSEN, der Nebenraum UNBEHEIZT nur mit Kappungsstunden, das Gebäude ohne Quelle GEMISCHT, weil beide
        /// Hälften dieselbe Quelle haben) und die Sommerlüftungsstunden des Modells; über Speichern und Lesen
        /// kommt jede Spalte je Gebäude und Zone gleich zurück.
        /// </summary>
        [Fact]
        public void Die_Mehrzonenfassung_fuellt_Gebaeude_und_Zonen_und_die_Rundreise_haelt_jede_Spalte()
        {
            if (!_db.Vorhanden) return;
            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(PROJEKT, AN, double.NaN, x => x.ID_Gebaeude == GEBAEUDE,
                x => { x.Sommerlueftung = true; AufheizLauf.Mehrzonenfassung(x); }).Single();
            Assert.True(l.Gerechnet);
            Assert.NotNull(l.Mehrzonen);
            GebaeudeModellErgebnis vdi = l.Ergebnis;
            Assert.True(vdi.SommerlueftungGesetzt);

            double[] kw = (double[])l.Ziel.Clone();
            WPPlan.Core.BhkwPlan.WattToKw(kw);
            ErgebnisGebaeudeModel zeile = GebaeudeKennzahlen.Bilden(0, GEBAEUDE, "Hotel", DbWerte.GEBAEUDE_MODELL_VDI6007, kw, vdi);

            Assert.Equal(vdi.StundenMitSommerlueftung, zeile.SommerlueftungsstundenH);
            Pruefen(vdi.Aufheizung, zeile.AufheizZustand, zeile.AufheizzeitMaxH, zeile.AufheizAussenC, zeile.AufheizLeistungKw,
                    zeile.AufheizLeistungsquelle, zeile.Aufheiztage, zeile.AufheizstundenH, zeile.HeizleistungMaxStundenH);
            Assert.Equal(vdi.Aufheizung.AufheizBemessung, zeile.AufheizBemessung);
            Assert.Equal(3, zeile.Zonen.Count);
            for (int k = 0; k < 3; k++)
            {
                GebaeudeModellErgebnis r = vdi.Zonen[k].Ergebnis;
                ErgebnisZoneModel z = zeile.Zonen[k];
                Assert.True(r.SommerlueftungGesetzt);
                Assert.Equal(r.StundenMitSommerlueftung, z.SommerlueftungsstundenH);
                Pruefen(r.Aufheizung, z.AufheizZustand, z.AufheizzeitMaxH, z.AufheizAussenC, z.AufheizLeistungKw,
                        z.AufheizLeistungsquelle, z.Aufheiztage, z.AufheizstundenH, z.HeizleistungMaxStundenH);
            }
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, zeile.Zonen[0].AufheizZustand);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, zeile.Zonen[2].AufheizZustand);
            Assert.Null(zeile.Zonen[2].AufheizLeistungKw);
            Assert.NotNull(zeile.Zonen[2].HeizleistungMaxStundenH);
            Assert.True(zeile.Aufheiztage > 0, "keine Rampe");
            Assert.True(zeile.SommerlueftungsstundenH > 0, "keine Sommerlüftungsstunde");

            // Rundreise: die gebildete Zeile an die Stelle der gespeicherten.
            Assert.True(new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            zeile.Merkplatz = m.Gebaeude[0].Merkplatz;
            // Die Zonen der Fassung stehen nicht in Tab_Zone - ohne Verweis gespeichert (Muster ErgebnisAufheizTests).
            foreach (ErgebnisZoneModel z in zeile.Zonen) z.ID_Zone = null;
            m.Gebaeude[0] = zeile;
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            ErgebnisGebaeudeModel geladen = new ErgebnisCtrl().Load(PROJEKT).Gebaeude.Single();
            Assert.Equal(Abdruck(zeile), Abdruck(geladen));
            Assert.Equal(zeile.SommerlueftungsstundenH, geladen.SommerlueftungsstundenH);
            Assert.Equal(3, geladen.Zonen.Count);
            for (int k = 0; k < 3; k++) Assert.Equal(Abdruck(zeile.Zonen[k]), Abdruck(geladen.Zonen[k]));
            _aus.WriteLine("Mehrzonenfassung 1018: " + Abdruck(geladen) + "\n" +
                           string.Join("\n", geladen.Zonen.Select(Abdruck)));
        }

        private static void Pruefen(Aufheizergebnis a, string zustand, int? tMax, double? ta, double? p, string quelle,
                                    int? tage, int? stunden, double? kappung)
        {
            Assert.NotNull(a);
            Assert.Equal(a.AufheizZustand, zustand);
            Assert.Equal(a.AufheizzeitMaxH, tMax);
            Assert.Equal(a.AufheizAussenC, ta);
            Assert.Equal(a.AufheizLeistungKw, p);
            Assert.Equal(a.AufheizLeistungsquelle, quelle);
            Assert.Equal(a.Aufheiztage, tage);
            Assert.Equal(a.AufheizstundenH, stunden);
            Assert.Equal(a.HeizleistungMaxStundenH, kappung);
        }

        /// <summary>Die vierzehn Aufheizwerte eines Gebäudes als Text — <c>~</c> für null, Zahlen rundlaufsicher.</summary>
        internal static string Abdruck(ErgebnisGebaeudeModel g) => string.Join("|",
            T(g.AufheizZustand), T(g.AufheizBemessung), Z(g.AufheizzeitMaxH), Z(g.AufheizAussenC), Z(g.AufheizLeistungKw),
            T(g.AufheizLeistungsquelle), Z(g.Aufheiztage), Z(g.AufheiztageBegrenzt), Z(g.AufheiztageUnerreichbar),
            Z(g.AufheiztageNachweisband), Z(g.AufheizstundenH), Z(g.AufheizzeitLaengsteH), Z(g.AufheizspruengeAus),
            Z(g.HeizleistungMaxStundenH));

        /// <summary>Die dreizehn Aufheizwerte einer Zone und ihre Sommerlüftungsstunden als Text.</summary>
        internal static string Abdruck(ErgebnisZoneModel z) => string.Join("|",
            T(z.AufheizZustand), Z(z.AufheizzeitMaxH), Z(z.AufheizAussenC), Z(z.AufheizLeistungKw),
            T(z.AufheizLeistungsquelle), Z(z.Aufheiztage), Z(z.AufheiztageBegrenzt), Z(z.AufheiztageUnerreichbar),
            Z(z.AufheiztageNachweisband), Z(z.AufheizstundenH), Z(z.AufheizzeitLaengsteH), Z(z.AufheizspruengeAus),
            Z(z.HeizleistungMaxStundenH), Z(z.SommerlueftungsstundenH));

        private static string T(string s) => s ?? "~";

        private static string Z(int? w) => w.HasValue ? w.Value.ToString(CultureInfo.InvariantCulture) : "~";

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("R", CultureInfo.InvariantCulture) : "~";
    }
}
