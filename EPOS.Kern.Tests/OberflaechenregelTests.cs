using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Oberflächenregel</b> (Anwenderrückmeldung 13.10.2026: „Die Grafiken überdecken
    /// sich"): Bedarf als Linie über den Flächen, Erzeugung als Fläche (größere hinten,
    /// durchscheinend), Stapel bleibt Stapel, dichte Jahresreihen als Tagesband — und der
    /// Berichtsweg bleibt unberührt. Dazu das Tagesband aus 8 760 Werten und die Teilung der
    /// Zeitachse je Einheit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class OberflaechenregelTests
    {
        private static double[] Stunden(Func<int, double> f)
        {
            var w = new double[8760];
            for (int i = 0; i < 8760; i++) w[i] = f(i);
            return w;
        }

        /// <summary>Das PV-Bild der Simulation: drei Linien (Überschuss, Strombedarf, Photovoltaik) über Viertelstunden.</summary>
        private static Zeichenmodell PvModell()
        {
            var pv = new double[35040];
            var bedarf = new double[35040];
            var ueberschuss = new double[35040];
            for (int i = 0; i < 35040; i++)
            {
                double stunde = (i % 96) / 4.0;
                pv[i] = 8.0 * Math.Max(0.0, Math.Sin((stunde - 6.0) / 12.0 * Math.PI));
                bedarf[i] = 3.0 + (stunde > 6 && stunde < 22 ? 2.0 : 0.0);
                ueberschuss[i] = Math.Max(0.0, pv[i] - bedarf[i]);
            }
            var linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Überschuss", ueberschuss, Farbrolle.UEBERSCHUSS),
                new ChartRenderer.Reihe("Strombedarf", bedarf, Farbrolle.BEDARF),
                new ChartRenderer.Reihe("Photovoltaik", pv, Farbrolle.STROM_PV)
            };
            var zweite = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Speicherfüllstand", Enumerable.Repeat(50.0, 35040).ToArray(),
                                        Farbrolle.SPEICHERFUELLSTAND)
            };
            return ChartRenderer.ErzeugerStapelModell("PV", new List<ChartRenderer.Reihe>(), linien, null,
                                                      "Leistung [kW]", ChartRenderer.Achse.Monate, false,
                                                      zweite, "Speicher [kWh]");
        }

        // ---------------------------------------------------------------- Rollen

        [Theory]
        [InlineData("BEDARF", Reihenrolle.Bedarf)]
        [InlineData("REST", Reihenrolle.Bedarf)]
        [InlineData("STROM_PV", Reihenrolle.Erzeugung)]
        [InlineData("UEBERSCHUSS", Reihenrolle.Erzeugung)]
        [InlineData("WAERME_SOLAR", Reihenrolle.Erzeugung)]
        [InlineData("AUSSENTEMPERATUR", Reihenrolle.Unbestimmt)]
        [InlineData("SPEICHERFUELLSTAND", Reihenrolle.Unbestimmt)]
        public void Die_Rolle_folgt_aus_der_Farbrolle(string farbrolle, Reihenrolle erwartet)
            => Assert.Equal(erwartet, Oberflaechenregel.Aus(new Farbrolle(farbrolle)));

        [Fact]
        public void Eine_genannte_Rolle_schlaegt_die_Farbrolle()
        {
            var r = new Datenreihe("x", Farbton.Aus(Farbrolle.SERIE_1), 1f, null, Stunden(i => 1.0),
                                   Reihenrolle: Reihenrolle.Bedarf);
            Assert.Equal(Reihenrolle.Bedarf, Oberflaechenregel.Rolle(r));
            Assert.Equal(Reihenrolle.Unbestimmt, Oberflaechenregel.Rolle(r with { Reihenrolle = Reihenrolle.Unbestimmt }));
        }

        [Fact]
        public void Die_Huelle_reicht_die_Rolle_in_das_Modell()
        {
            var reihe = new ChartRenderer.Reihe("Last", Stunden(i => 2.0), Farbrolle.SERIE_2).MitRolle(Reihenrolle.Bedarf);
            Zeichenmodell m = ChartRenderer.ErzeugerStapelModell("x", new List<ChartRenderer.Reihe>(),
                                                                 new List<ChartRenderer.Reihe> { reihe }, null,
                                                                 "kW", ChartRenderer.Achse.Monate, false);
            Assert.Equal(Reihenrolle.Bedarf, m.Reihen.Single().Reihenrolle);
        }

        // ---------------------------------------------------------------- Die Regel

        [Fact]
        public void Bedarf_wird_Linie_zuletzt_Erzeugung_Flaeche_groessere_hinten()
        {
            Zeichenmodell bericht = PvModell();
            Zeichenmodell bild = Oberflaechenregel.Anwenden(bericht);

            Assert.Equal(Darstellung.Bericht, bericht.Darstellung);
            Assert.Equal(Darstellung.Interaktiv, bild.Darstellung);
            Assert.NotSame(bericht, bild);

            // Reihenfolge: Flaechen (PV vor Ueberschuss - die groessere hinten), dann die
            // Linie der zweiten Achse, zuletzt der Bedarf.
            Assert.Equal(new[] { "Photovoltaik", "Überschuss", "Speicherfüllstand", "Strombedarf" },
                         bild.Reihen.Select(r => r.Name).ToArray());

            Datenreihe pv = bild.Reihen[0], ueberschuss = bild.Reihen[1], bedarf = bild.Reihen[3];
            Assert.Equal(Reihenart.Flaeche, pv.Art);
            Assert.Equal(Reihenart.Flaeche, ueberschuss.Art);
            Assert.Equal(Reihenart.Linie, bedarf.Art);
            Assert.Equal(Reihenrolle.Bedarf, bedarf.Reihenrolle);
            Assert.Null(pv.Unten);
            Assert.Equal(Oberflaechenregel.DECKUNG_FLAECHE, pv.Ton.Deckung);
            Assert.Equal(Farbrolle.STROM_PV, pv.Randton.Rolle);
            Assert.True(pv.Tagesband && ueberschuss.Tagesband && bedarf.Tagesband);
            Assert.True(bild.Reihen[2].Tagesband);
        }

        [Fact]
        public void Der_Berichtsweg_bleibt_unberuehrt()
        {
            Zeichenmodell bericht = PvModell();
            Zeichenmodell vorher = PvModell();
            Zeichenmodell bild = Oberflaechenregel.Anwenden(bericht);

            // Die Gabe ist unveraendert, die Befehlsliste dieselbe, das Druck-SVG byte-gleich.
            Assert.True(bericht.Gleicht(vorher));
            Assert.Equal(bericht.Befehle.Count, bild.Befehle.Count);
            for (int i = 0; i < bericht.Befehle.Count; i++) Assert.Same(bericht.Befehle[i], bild.Befehle[i]);
            Assert.Equal(SvgSchreiber.Drucktext(vorher), SvgSchreiber.Drucktext(bild));
            Assert.All(bericht.Reihen, r => Assert.Equal(Reihenart.Linie, r.Art));
            Assert.All(bericht.Reihen, r => Assert.False(r.Tagesband));
            Assert.DoesNotContain("data-darstellung", SvgSchreiber.Text(bericht));
            Assert.Contains("data-darstellung=\"interaktiv\"", SvgSchreiber.Text(bild));

            // Zweimal angewandt: dasselbe Modell, einmal angewandt bleibt es.
            Assert.Same(bild, Oberflaechenregel.Anwenden(bild));
        }

        [Fact]
        public void Ein_Stapel_bleibt_Stapel_und_seine_Huelle_Linie()
        {
            var stapel = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Kessel", Stunden(i => 3.0), Farbrolle.WAERME_KESSEL, ChartRenderer.Stapelart.Flaeche),
                new ChartRenderer.Reihe("Rest", Stunden(i => 1.0), Farbrolle.REST, ChartRenderer.Stapelart.Flaeche)
            };
            var linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Wärmebedarf", Stunden(i => 4.0), Farbrolle.BEDARF)
            };
            Zeichenmodell bild = Oberflaechenregel.Anwenden(ChartRenderer.ErzeugerStapelModell(
                "Kessel", stapel, linien, null, "kW", ChartRenderer.Achse.Monate, false));

            Assert.Equal(new[] { "Kessel", "Rest", "Wärmebedarf" }, bild.Reihen.Select(r => r.Name).ToArray());
            Assert.True(Pfadregel.IstStapelschicht(bild.Reihen[0]));
            Assert.True(Pfadregel.IstStapelschicht(bild.Reihen[1]));
            Assert.Equal(Reihenart.Linie, bild.Reihen[2].Art);
            Assert.True(bild.Reihen[2].Huelle);
            Assert.All(bild.Reihen, r => Assert.False(r.Tagesband));   // Stufenregel, kein Band
        }

        [Fact]
        public void Die_Dauerlinie_bekommt_kein_Tagesband()
        {
            var linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Wärmebedarf", Stunden(i => i % 24), Farbrolle.BEDARF),
                new ChartRenderer.Reihe("Wärmeproduktion", Stunden(i => (i % 24) / 2.0), Farbrolle.WAERME_SOLAR)
            };
            Zeichenmodell bild = Oberflaechenregel.Anwenden(ChartRenderer.ErzeugerStapelModell(
                "Solar", new List<ChartRenderer.Reihe>(), linien, null, "kW", ChartRenderer.Achse.Jahresstunden, true));
            Assert.Equal(Achsenart.Index, bild.Flaeche.X);
            Assert.All(bild.Reihen, r => Assert.False(r.Tagesband));
            Assert.Equal(Reihenart.Flaeche, bild.Reihen.Single(r => r.Name == "Wärmeproduktion").Art);
        }

        [Fact]
        public void Ein_Bild_ohne_Flaeche_kommt_unveraendert_zurueck()
        {
            Zeichenmodell kuchen = ChartRenderer.KuchenModell("Kuchen",
                new List<ChartRenderer.Segment> { new ChartRenderer.Segment("a", 1.0, SkiaSharp.SKColors.Red) });
            Assert.Same(kuchen, Oberflaechenregel.Anwenden(kuchen));
            Assert.Null(Oberflaechenregel.Anwenden(null));
        }

        // ---------------------------------------------------------------- Tagesband

        [Fact]
        public void Das_Tagesband_hat_365_Tage_mit_Min_Max_Mittel()
        {
            double[] werte = Stunden(i => (i % 24) + (i / 24));   // Tag d: d … d+23
            IReadOnlyList<Pfadregel.Tagesstufe> band = Pfadregel.Tagesband(werte);

            Assert.Equal(365, band.Count);
            for (int d = 0; d < 365; d++)
            {
                Assert.Equal(d * 24, band[d].Von);
                Assert.Equal(d * 24 + 23, band[d].Bis);
                Assert.Equal(d, band[d].Min, 9);
                Assert.Equal(d + 23, band[d].Max, 9);
                Assert.Equal(d + 11.5, band[d].Mittel, 9);
                Assert.True(band[d].Belegt);
            }
        }

        [Fact]
        public void Das_Tagesband_eines_Ausschnitts_schneidet_die_Randtage_an()
        {
            double[] werte = Stunden(i => i);
            IReadOnlyList<Pfadregel.Tagesstufe> band = Pfadregel.Tagesband(werte, 30, 60);   // Stunden 30 … 89

            Assert.Equal(3, band.Count);
            Assert.Equal((30, 47), (band[0].Von, band[0].Bis));
            Assert.Equal((48, 71), (band[1].Von, band[1].Bis));
            Assert.Equal((72, 89), (band[2].Von, band[2].Bis));
            Assert.Equal(38.5, band[0].Mittel, 9);
        }

        [Fact]
        public void Ein_Tag_ohne_endlichen_Wert_ist_eine_Luecke()
        {
            double[] werte = Stunden(i => i / 24 == 10 ? double.NaN : 1.0);
            werte[5 * 24 + 3] = double.PositiveInfinity;   // ein einzelner Ausreisser zaehlt nicht
            IReadOnlyList<Pfadregel.Tagesstufe> band = Pfadregel.Tagesband(werte);
            Assert.False(band[10].Belegt);
            Assert.True(band[5].Belegt);
            Assert.Equal(1.0, band[5].Max, 9);
            Assert.Empty(Pfadregel.Tagesband(new double[100]));   // kein Jahresraster
        }

        [Fact]
        public void Das_Band_steht_im_Jahresbild_und_weicht_im_Ausschnitt_den_Stunden()
        {
            Zeichenmodell bild = Oberflaechenregel.Anwenden(PvModell());
            Datenreihe bedarf = bild.Reihen.Single(r => r.Name == "Strombedarf");
            int spalten = (int)Math.Round(bild.Flaeche.Bild.Breite);

            Assert.True(Pfadregel.AlsTagesband(bedarf, 35040, spalten));
            Assert.False(Pfadregel.AlsTagesband(bedarf, 96 * 20, spalten));   // 20 Tage: ein Tag > 4 Spalten

            string band = SvgSchreiber.Bandpfad(bedarf, bild.Flaeche);
            Assert.StartsWith("M ", band);
            Assert.EndsWith(" Z", band);
            Assert.Equal("", SvgSchreiber.Bandpfad(bedarf, bild.Flaeche, 1000, 1000 + 96 * 20));

            string kern = SvgSchreiber.Reihenpfad(bedarf, bild.Flaeche, true);
            Assert.Equal(365, kern.Count(c => c == ','));   // 365 Tagesmittel (ein Punkt je Tag), kein Z (Linie)
            Assert.DoesNotContain("Z", kern);
            string pv = SvgSchreiber.Reihenpfad(bild.Reihen.Single(r => r.Name == "Photovoltaik"), bild.Flaeche, true);
            Assert.EndsWith(" Z", pv);   // Flaeche: auf der Achsennull geschlossen

            string svg = SvgSchreiber.Text(bild);
            Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(svg, "class=\"epos-band\"").Count);
            Assert.Contains("data-band=\"Strombedarf\"", svg);
        }

        // ---------------------------------------------------------------- Die Achsteilung je Skala

        /// <summary>Kein Paar benachbarter Marken liegt enger als <paramref name="minPx"/>.</summary>
        private static void Abstand_haelt(IReadOnlyList<Zeitachse.Achsenmarke> marken, double von, double bis,
                                          double breite, double minPx)
        {
            for (int i = 1; i < marken.Count; i++)
                Assert.True((marken[i].Stunde - marken[i - 1].Stunde) / (bis - von) * breite >= minPx - 1e-9,
                            marken[i - 1].Text + " / " + marken[i].Text);
        }

        [Fact]
        public void Die_Datumsteilung_nennt_im_Jahr_die_Monatsanfaenge()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                IReadOnlyList<Zeitachse.Achsenmarke> marken = Zeitachse.Teilung(Zeitachseneinheit.Datum, 0, 8759, 1100);
                Assert.Equal(12, marken.Count);
                Assert.Equal(0.0, marken[0].Stunde);
                Assert.Equal("01.01.", marken[0].Text);
                Assert.Equal(31 * 24.0, marken[1].Stunde);
                Assert.Equal("01.02.", marken[1].Text);
                Assert.Equal("01.09.", marken[8].Text);
                Assert.All(marken, m => Assert.Equal("", m.Zweitzeile));
                Abstand_haelt(marken, 0, 8759, 1100, Zeitachse.MARKE_MIN_PX);
                Assert.Equal("Datum", Zeitachse.Achsentitel(Zeitachseneinheit.Datum));

                // Schmal: jeder zweite Monat oder weiter - nie enger als 72 px.
                IReadOnlyList<Zeitachse.Achsenmarke> schmal = Zeitachse.Teilung(Zeitachseneinheit.Datum, 0, 8759, 500);
                Assert.True(schmal.Count < 12);
                Abstand_haelt(schmal, 0, 8759, 500, Zeitachse.MARKE_MIN_PX);
            }
        }

        [Fact]
        public void Die_Datumsteilung_nennt_im_Ausschnitt_Tage()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                // Zwei Wochen ab dem 1. Februar auf 1 100 Bildpunkten: jeder Tag eine Marke.
                double von = 31 * 24, bis = von + 14 * 24;
                IReadOnlyList<Zeitachse.Achsenmarke> marken = Zeitachse.Teilung(Zeitachseneinheit.Datum, von, bis, 1100);
                Assert.Equal(15, marken.Count);
                Assert.Equal("01.02.", marken[0].Text);
                Assert.Equal("15.02.", marken[14].Text);
                Abstand_haelt(marken, von, bis, 1100, Zeitachse.MARKE_MIN_PX);

                // Ein Vierteljahr: Tagesschritte, keine Uhrzeit, kein Ueberlappen.
                IReadOnlyList<Zeitachse.Achsenmarke> quartal = Zeitachse.Teilung(Zeitachseneinheit.Datum, 0, 90 * 24, 1100);
                Assert.True(quartal.Count >= 5);
                Assert.DoesNotContain(quartal, m => m.Text.Contains(':'));
                Abstand_haelt(quartal, 0, 90 * 24, 1100, Zeitachse.MARKE_MIN_PX);
            }
        }

        [Fact]
        public void Die_Datumsteilung_nennt_im_Tag_die_Uhrzeit()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                // Ein Tag (6. Mai) auf 1 100 Bildpunkten: Stundenschritte mit Datum und Uhrzeit.
                double von = 125 * 24, bis = von + 24;
                IReadOnlyList<Zeitachse.Achsenmarke> marken = Zeitachse.Teilung(Zeitachseneinheit.Datum, von, bis, 1100);
                Assert.Equal("06.05. 00:00", marken[0].Text);
                Assert.Contains("06.05. 12:00", marken.Select(m => m.Text));
                Assert.All(marken, m => Assert.Matches(@"^\d\d\.\d\d\. \d\d:00$", m.Text));
                Abstand_haelt(marken, von, bis, 1100, Zeitachse.MARKE_UHRZEIT_MIN_PX);
            }
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("05/06", Zeitachse.DatumKurz(125 * 24 + 3));
        }

        /// <summary>
        /// Jede Zeitachse über das Jahr trägt die Wahl: Viertelstunden, Stunden und Tageswerte
        /// (Tag d bei Stunde 24·d); eine Indexachse, eine Woche oder ein Ausschnitt nicht.
        /// </summary>
        [Fact]
        public void Jede_Jahreszeitachse_kennt_ihre_Stunden_je_Stuetzstelle()
        {
            var bild = new Rahmen(0, 0, 1000, 400);
            double Je(int n, Achsenart art = Achsenart.Stunden, double von = 0)
                => Zeitachse.StundenJeStuetzstelle(new Zeichenflaeche(bild, new Datenfenster(von, von + n - 1, 0, 1), art));
            Assert.Equal(0.25, Je(35040));
            Assert.Equal(1.0, Je(8760));
            Assert.Equal(24.0, Je(365));
            Assert.Equal(0.0, Je(168));
            Assert.Equal(0.0, Je(12));
            Assert.Equal(0.0, Je(8760, Achsenart.Index));
            Assert.Equal(0.0, Je(8760, Achsenart.Stunden, 100));
        }

        /// <summary>Das Kostenprofil eines Jahres zählt Jahresstunden — die Oberfläche trägt dort die Wahl.</summary>
        [Fact]
        public void Das_Kostenprofil_eines_Jahres_zaehlt_Jahresstunden()
        {
            Zeichenmodell m = ChartRenderer.KostenprofilModell("Kostenprofil", Stunden(i => 10.0 + i % 24), "ct/kWh", "Monat");
            Assert.Equal(Achsenart.Stunden, m.Flaeche.X);
            Assert.Equal(1.0, Zeitachse.StundenJeStuetzstelle(m.Flaeche));
        }

        [Fact]
        public void Die_Teilung_Stunde_im_Jahr_geht_in_runden_Schritten()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                IReadOnlyList<Zeitachse.Achsenmarke> marken = Zeitachse.Teilung(Zeitachseneinheit.StundeImJahr, 3000, 3400, 1100);
                Assert.Equal(new[] { 3000.0, 3050.0, 3100.0, 3150.0, 3200.0, 3250.0, 3300.0, 3350.0, 3400.0 },
                             marken.Select(m => m.Stunde));
                Assert.Equal("3.000", marken[0].Text);
                Assert.All(marken, m => Assert.Equal("", m.Zweitzeile));
                Abstand_haelt(marken, 3000, 3400, 1100, Zeitachse.MARKE_MIN_PX);

                IReadOnlyList<Zeitachse.Achsenmarke> jahr = Zeitachse.Teilung(Zeitachseneinheit.StundeImJahr, 0, 8759, 1100);
                Assert.Equal(0.0, jahr[0].Stunde);
                Assert.Equal(1000.0, jahr[1].Stunde);
                Assert.Equal("Stunde im Jahr [h]", Zeitachse.Achsentitel(Zeitachseneinheit.StundeImJahr));
                Assert.Empty(Zeitachse.Teilung(Zeitachseneinheit.StundeImJahr, 100, 100, 1100));
            }
        }
    }
}
