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
    /// <b>Der Lauf mit Aufheizoptimierung an einem Testdatenbankgebäude</b> (Entwurf KP3, Welle R2;
    /// Vorgriff auf N-AH7): Projekt 1018 (Gebäude 10632, Klasse-H/I-Bau, Strahlungsanteil 0,3) mit
    /// eingeschaltetem Schalter und den Vorgaben (a), ρ = 20 %, täglich — über die Fassade wie im Lauf.
    /// Die Rampe greift an den kalten Tagen, die Zähler sind plausibel, der Lauf trägt die Reihe mit
    /// Rampe als Heizsollwert, und zwei Läufe sind bitgleich.
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizLaufTests : IClassFixture<TestDatenbank>
    {
        private const int PROJEKT = 1018;
        private const int GEBAEUDE = 10632;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizLaufTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static AufheizLauf.Gebaeudelauf Lauf(Aufheizvorgabe vorgabe)
            => AufheizLauf.Projekt(PROJEKT, vorgabe, double.NaN, g => g.ID_Gebaeude == GEBAEUDE).Single();

        [Fact]
        public void Die_Rampe_greift_an_kalten_Tagen_und_zwei_Laeufe_sind_bitgleich()
        {
            if (!_db.Vorhanden) return;

            var an = new Aufheizvorgabe(true, null, null, null, null);
            AufheizLauf.Gebaeudelauf aus = Lauf(Aufheizvorgabe.Aus);
            AufheizLauf.Gebaeudelauf eins = Lauf(an);
            AufheizLauf.Gebaeudelauf zwei = Lauf(an);
            Assert.True(aus.Gerechnet && eins.Gerechnet && zwei.Gerechnet);
            Assert.Null(aus.Plan);
            Aufheizplan p = eins.Plan;
            Assert.NotNull(p);

            // Bemessung und Zähler.
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, p.Bemessung.Quelle);
            Assert.Equal(Aufheizform.Stundenmittel, p.Bemessung.Form);
            int tMax = p.Bemessung.VarianteA.AufheizzeitMaxH.Value;
            Assert.InRange(tMax, 1, 47);
            Assert.True(p.Bemessung.VarianteB.AufheizzeitMaxH >= tMax, "(b) < (a)");
            Assert.Equal(p.Bemessung.AussenMinC, p.Bemessung.VarianteA.AussenC);
            Assert.True(p.Aufheiztage >= 10, "Rampentage: " + p.Aufheiztage);
            Assert.InRange(p.LaengsteRampeH, 2, tMax);
            Assert.Equal(0, p.TageUnerreichbar);
            Assert.Equal(0, p.TageBegrenzt);
            Assert.Equal(0, p.SpruengeAus);
            Assert.Equal(0, p.TageBemessungBegrenzt);
            Assert.Equal(0, p.KuehlgekappteStundenH);
            Assert.InRange(p.MaskenstundenH, p.Aufheiztage, p.AufheizstundenH);
            Assert.All(p.Spruenge, sp => Assert.InRange(sp.N, 1, Math.Min(tMax + 1, sp.AbsenkdauerH + 1)));

            // Die Rampe greift an den kalten Tagen.
            List<Aufheizsprung> gerampt = p.Spruenge.Where(x => x.N > 1).ToList();
            double mittelRampe = gerampt.Average(x => x.AussenC);
            double mittelAlle = p.Spruenge.Average(x => x.AussenC);
            double kaeltesteOhne = p.Spruenge.Where(x => x.N == 1).Min(x => x.AussenC);
            Assert.True(mittelRampe < mittelAlle - 5.0,
                string.Format(CultureInfo.InvariantCulture, "T_a der Rampentage {0:0.0} °C, aller Tage {1:0.0} °C", mittelRampe, mittelAlle));

            // Der Lauf trägt die Reihe mit Rampe als Heizsollwert, und die Rampe wirkt.
            AufheizGrenzfallTests.Bitgleich(p.Reihe, eins.Ergebnis.Heizsollwert, "Heizsollwert = Reihe mit Rampe");
            Assert.True(p.Geaendert);
            int anders = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (p.Rampenmaske[h]) Assert.True(eins.Ergebnis.Heizsollwert[h] > aus.Ergebnis.Heizsollwert[h]);
                else Assert.Equal(aus.Ergebnis.Heizsollwert[h], eins.Ergebnis.Heizsollwert[h]);
                if (eins.Ziel[h] != aus.Ziel[h]) anders++;
            }
            Assert.True(anders > 0);

            // Determinismus (Vorgriff N-AH7): zwei Läufe bitgleich, Plan wie Ergebnis.
            AufheizGrenzfallTests.Bitgleich(eins.Ziel, zwei.Ziel, "zweiter Lauf, Heizreihe");
            AufheizGrenzfallTests.Bitgleich(eins.Ergebnis, zwei.Ergebnis, "zweiter Lauf");
            AufheizGrenzfallTests.Bitgleich(eins.Plan.Reihe, zwei.Plan.Reihe, "zweiter Lauf, Plan");
            Assert.Equal(eins.Plan.Spruenge, zwei.Plan.Spruenge);
            Assert.Equal(eins.Plan.Aufheiztage, zwei.Plan.Aufheiztage);
            Assert.Equal(eins.Plan.Bemessung.AufheizleistungW, zwei.Plan.Bemessung.AufheizleistungW);

            // Befund für R4: die Stundenleistung im Fenster [h_s − n + 1, h_s + 2] gegen 1,01·P_auf (W3, Zielleistung).
            // P_auf gilt dem Katalogbau (E8, Festlegung 16): die Reihen des Ergebnisses sind skaliert.
            double pAuf = p.Bemessung.AufheizleistungW;
            double faktorMit = eins.Ergebnis.Skalierungsfaktor, faktorOhne = aus.Ergebnis.Skalierungsfaktor;
            int ueberBand = 0, ueberBandOhne = 0;
            double spitzeFenster = 0.0, spitzeFensterOhne = 0.0;
            foreach (Aufheizsprung sp in gerampt)
            {
                double maxMit = 0.0, maxOhne = 0.0;
                for (int h = sp.Sprungstunde - sp.N + 1; h <= sp.Sprungstunde + 2 && h < 8760; h++)
                {
                    int r = ((h % 8760) + 8760) % 8760;
                    maxMit = Math.Max(maxMit, eins.Ergebnis.HeizlastW[r] / faktorMit);
                    maxOhne = Math.Max(maxOhne, aus.Ergebnis.HeizlastW[r] / faktorOhne);
                }
                if (maxMit > 1.01 * pAuf) ueberBand++;
                if (maxOhne > 1.01 * pAuf) ueberBandOhne++;
                spitzeFenster = Math.Max(spitzeFenster, maxMit);
                spitzeFensterOhne = Math.Max(spitzeFensterOhne, maxOhne);
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Projekt {0}, Gebäude {1}: t_auf,max (a) {2} h, (b) {3} h, T_a,B {4:0.0} °C, P_auf {5:0} W ({6}), " +
                "Sprünge {7}, Rampentage {8}, Σ (n − 1) {9} h, Maskenstunden {10}, längste Rampe {11} h, " +
                "W1 {12}, W2 {13}, W4 {14}, T_a Rampentage {15:0.0} °C, alle {16:0.0} °C, kälteste ohne Rampe {17:0.0} °C",
                PROJEKT, GEBAEUDE, tMax, p.Bemessung.VarianteB.AufheizzeitMaxH, p.Bemessung.VarianteA.AussenC, pAuf,
                p.Bemessung.Quelle, p.Spruenge.Count, p.Aufheiztage, p.AufheizstundenH, p.MaskenstundenH, p.LaengsteRampeH,
                p.TageUnerreichbar, p.TageBegrenzt, p.SpruengeAus, mittelRampe, mittelAlle, kaeltesteOhne));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Jahresheizwärme {0:0.000} → {1:0.000} MWh, Spitze {2:0.00} → {3:0.00} kW; Fenster der Rampentage über 1,01·P_auf: " +
                "ohne Rampe {4}, mit Rampe {5} von {8}; Spitze im Fenster {6:0} → {7:0} W (Katalogbau, Faktor {9:0.###})",
                aus.Ergebnis.JahresheizwaermeMwh, eins.Ergebnis.JahresheizwaermeMwh, aus.Ergebnis.SpitzeKw, eins.Ergebnis.SpitzeKw,
                ueberBandOhne, ueberBand, spitzeFensterOhne, spitzeFenster, gerampt.Count, faktorMit));
        }
    }
}
