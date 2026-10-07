using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BA-1 — die Relevanzregel für Schichten</b> (Konzept Bauteilaufbau beim Import 5.1, E95-2): Schichten bis 5 mm
    /// mit je unter 2 % an R und C fallen aus dem vorgeschlagenen Aufbau, Folien und Abdichtungen unter 2 % von R
    /// immer; eine dünne, aber wirksame Dämmung bleibt. Weggelassene Schichten sind benannt.
    /// </summary>
    public sealed class SchichtrelevanzTests
    {
        // Innen → außen: Putz 15 mm, Kalksandstein 24 cm, Kleber 5 mm, Mineralwolle 16 cm, Außenputz 3 mm.
        private static readonly Schicht PUTZ = new Schicht(0.015, 0.7, 1400, 1000);
        private static readonly Schicht KS = new Schicht(0.24, 0.99, 1800, 1000);
        private static readonly Schicht KLEBER = new Schicht(0.005, 0.8, 1500, 1000);
        private static readonly Schicht MW = new Schicht(0.16, 0.035, 30, 1030);
        private static readonly Schicht AUSSENPUTZ = new Schicht(0.003, 0.7, 1400, 1000);
        private static readonly Schicht FOLIE = new Schicht(0.0002, 0.33, 920, 2200);

        private static int[] Stellen(IReadOnlyList<SchichtrelevanzBefund> b) => b.Select(x => x.Stelle).ToArray();

        [Fact]
        public void Folie_Kleber_und_duenner_Putz_fallen_weg_Daemmung_und_Mauerwerk_bleiben()
        {
            var folge = new[] { PUTZ, FOLIE, KS, KLEBER, MW, AUSSENPUTZ };
            IReadOnlyList<SchichtrelevanzBefund> weg = Schichtrelevanz.Unerheblich(folge);
            Assert.Equal(new[] { 1, 3, 5 }, Stellen(weg));
            Assert.All(weg, b => Assert.Equal(Schichtrelevanzgrund.Duenn, b.Grund));
            Assert.All(weg, b => Assert.True(b.AnteilR < 0.02 && b.AnteilC < 0.02));
        }

        [Fact]
        public void Eine_duenne_aber_wirksame_Daemmung_bleibt()
        {
            // Vakuumdämmung 4 mm, λ 0,007: R = 0,571 m²K/W — weit über 2 % von R.
            var vip = new Schicht(0.004, 0.007, 190, 800);
            IReadOnlyList<SchichtrelevanzBefund> weg = Schichtrelevanz.Unerheblich(new[] { PUTZ, KS, vip, AUSSENPUTZ });
            Assert.DoesNotContain(2, Stellen(weg));
            Assert.Equal(new[] { 3 }, Stellen(weg));
        }

        [Fact]
        public void Eine_duenne_speichernde_Schicht_ueber_2_Prozent_an_C_bleibt()
        {
            // Leichtbau: Gipskarton 12,5 mm innen, Mineralwolle, 5 mm Fliesenkleber mit 7 500 J/(m²K) — über 2 % von C.
            var gk = new Schicht(0.0125, 0.25, 900, 1000);
            var mw = new Schicht(0.2, 0.035, 30, 1030);
            IReadOnlyList<SchichtrelevanzBefund> weg = Schichtrelevanz.Unerheblich(new[] { KLEBER, gk, mw });
            Assert.Empty(weg);
        }

        [Fact]
        public void Grenzfaelle_an_5_mm_und_2_Prozent()
        {
            // Dicke genau 5 mm fällt, 5,1 mm bleibt (gleich kleine Anteile).
            Assert.Equal(new[] { 1 }, Stellen(Schichtrelevanz.Unerheblich(new[] { KS, new Schicht(0.005, 50, 100, 500), MW })));
            Assert.Empty(Schichtrelevanz.Unerheblich(new[] { KS, new Schicht(0.0051, 50, 100, 500), MW }));

            // Anteil an C knapp unter 2 % fällt, knapp darüber bleibt. ΣC ohne die Probe: KS 432 000 + MW 4 944.
            double cRest = 0.24 * 1800 * 1000 + 0.16 * 30 * 1030;
            double rhoUnter = 0.0199 / 0.9801 * cRest / (0.005 * 1000);
            double rhoUeber = 0.0201 / 0.9799 * cRest / (0.005 * 1000);
            Assert.Equal(new[] { 1 }, Stellen(Schichtrelevanz.Unerheblich(new[] { KS, new Schicht(0.005, 50, rhoUnter, 1000), MW })));
            Assert.Empty(Schichtrelevanz.Unerheblich(new[] { KS, new Schicht(0.005, 50, rhoUeber, 1000), MW }));

            // Anteil an R knapp über 2 % bleibt, auch bei 5 mm: R der Probe = 2,01 % von ΣR.
            double rRest = 0.24 / 0.99 + 0.16 / 0.035;
            double lamUeber = 0.005 / (0.0201 / 0.9799 * rRest);
            Assert.Empty(Schichtrelevanz.Unerheblich(new[] { KS, new Schicht(0.005, lamUeber, 100, 500), MW }));
        }

        [Fact]
        public void Abdichtung_faellt_unabhaengig_von_Dicke_und_Kapazitaet_solange_unter_2_Prozent_von_R()
        {
            // Bitumenbahn 10 mm: R 0,059 (1,2 % von R), C 12 000 (2,7 % von C) — als Sperre weg, sonst bliebe sie.
            var bahn = new Schicht(0.01, 0.17, 1200, 1000);
            var folge = new[] { PUTZ, KS, bahn, MW };
            Assert.Empty(Schichtrelevanz.Unerheblich(folge));
            SchichtrelevanzBefund b = Assert.Single(Schichtrelevanz.Unerheblich(folge, new[] { false, false, true, false }));
            Assert.Equal((2, Schichtrelevanzgrund.Sperre), (b.Stelle, b.Grund));
            Assert.True(b.AnteilC > 0.02);

            // Ohne Dämmung trägt dieselbe Bahn über 2 % von R und bleibt.
            Assert.Empty(Schichtrelevanz.Unerheblich(new[] { PUTZ, new Schicht(0.115, 0.99, 1800, 1000), bahn },
                                                     new[] { false, false, true }));
        }

        [Fact]
        public void Ruhende_Luftschicht_zaehlt_mit_ihrem_Widerstand_und_ohne_Masse()
        {
            // 5 mm ruhende Luft: R nach Tabelle 8 (0,11) gegenüber ΣR ≈ 4,8 → 2,3 % — bleibt.
            Assert.Empty(Schichtrelevanz.Unerheblich(new[] { KS, Schicht.RuhendeLuft(0.005), MW }));
        }

        [Fact]
        public void Im_Vorschlag_faellt_die_Folie_benannt_weg_und_der_Aufbau_rechnet_ohne_sie()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            // gbXML: erste Schicht außen — Außenputz, Dämmung, PE-Folie, Mauerwerk, Innenputz.
            var aufbau = new AbbildAufbau { Kennung = "kon-folie", Name = "Wand mit Folie", Status = Aufbaustatus.Vollstaendig };
            aufbau.Schichten.Add(new AbbildSchicht { Name = "Außenputz", DickeM = 0.003, LambdaWmK = 0.7, RhoKgM3 = 1400, CpJkgK = 1000 });
            aufbau.Schichten.Add(new AbbildSchicht { Name = "Mineralwolle", DickeM = 0.16, LambdaWmK = 0.035, RhoKgM3 = 30, CpJkgK = 1030 });
            aufbau.Schichten.Add(new AbbildSchicht { Name = "PE-Folie", DickeM = 0.0002, LambdaWmK = 0.33, RhoKgM3 = 920, CpJkgK = 2200 });
            aufbau.Schichten.Add(new AbbildSchicht { Name = "Kalksandstein", DickeM = 0.24, LambdaWmK = 0.99, RhoKgM3 = 1800, CpJkgK = 1000 });
            aufbau.Schichten.Add(new AbbildSchicht { Name = "Innenputz", DickeM = 0.015, LambdaWmK = 0.7, RhoKgM3 = 1400, CpJkgK = 1000 });
            AbbildBauteil wand = BauteilvorschlagProbe.Flaeche("wand", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 25, null, 90, 180, "R1");
            wand.Aufbau = aufbau;
            a.Gebaeude[0].Bauteile.Add(wand);

            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Select(m => m.ToString())));
            GebaeudeBauteilzeile z = BauteilvorschlagProbe.Zeile(v, "wand");
            GebaeudeAufbauzeile az = BauteilvorschlagProbe.AufbauZeile(v, z);
            // Innen → außen bleiben Innenputz, Kalksandstein, Mineralwolle.
            Assert.Equal(new[] { 0.015, 0.24, 0.16 }, az.Aufbau.Schichten.Select(s => s.Dicke));
            Assert.Equal(new[] { "Außenputz", "PE-Folie" }, az.Weggelassen.Select(w => w.Name).OrderBy(n => n, System.StringComparer.Ordinal));
            Assert.All(az.Weggelassen, w => Assert.Equal(Schichtrelevanzgrund.Duenn, w.Grund));
            BauteilvorschlagProbe.Nah(1.0 / (0.13 + 0.015 / 0.7 + 0.24 / 0.99 + 0.16 / 0.035 + 0.04), z.USchichten, 1e-12);

            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.SCHICHT_UNERHEBLICH);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal("2", m.Werte[0]);
            Assert.Contains("Wand mit Folie: PE-Folie (0.2 mm)", m.Werte[1]);
            Assert.Contains("Wand mit Folie: Außenputz (3 mm)", m.Werte[1]);
        }
    }
}
