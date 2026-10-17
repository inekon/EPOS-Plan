using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bilder der Kalenderkarte</b> (Entwurf KP2, Welle K4, Festlegung 8; Befund B12): die
    /// Woche mit Lücke im Stundenprofil und das Teppichbild aus <see cref="Kalenderteppich"/>.
    ///
    /// <para><b>Die rote Probe von B12 im Bild:</b> Das Stundenprofil setzte „aus" (NaN) über
    /// <c>Math.Max(0, NaN)</c> als NaN-Bildpunkt ab, und die Fläche des SVG trug „NaN" im Pfad —
    /// die Stunde verschwand oder machte den Pfad ungültig. Mit Lücke bricht die Linie jetzt ab,
    /// ohne Lücke bleibt das Bild bitgleich (ChartProben-Messlatte).</para>
    ///
    /// <para>Deutsche Texte und Zahlformate: die Kultur ist auf de-DE gepinnt.</para>
    /// </summary>
    public class KalenderteppichBildTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Lüftungswoche der Probe: Mo–Fr 7–18 Uhr 2,0 1/h, nachts 0,5 1/h, Sa und So „aus".</summary>
        private static double[] Lueftungswoche()
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt >= 5 ? double.NaN : s >= 7 && s < 18 ? 2.0 : 0.5;
            return w;
        }

        /// <summary>Alle Punkte der Pfade eines Modells, auch in Gruppen.</summary>
        private static IEnumerable<Punkt> Punkte(IEnumerable<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is Pfad p) foreach (Punkt q in p.Punkte) yield return q;
                else if (b is Gruppe g) foreach (Punkt q in Punkte(g.Befehle)) yield return q;
            }
        }

        // =============================================================================
        //  Die Woche mit Lücke
        // =============================================================================

        /// <summary>
        /// <b>B12 im Stundenprofil:</b> Die „aus"-Stunden des Wochenendes sind eine Lücke — kein
        /// Bildpunkt ist NaN, die Reihe zerfällt in ein Stück (Mo–Fr), und der SVG-Pfad der Fläche
        /// trägt kein „NaN".
        /// </summary>
        [Fact]
        public void Die_Woche_mit_aus_bricht_die_Linie_statt_NaN_zu_zeichnen()
        {
            Zeichenmodell m = ChartRenderer.StundenprofilModell("Lüftung", Lueftungswoche(), 24,
                                                                "Wochenstunde", "Lüftung [1/h]");
            foreach (Punkt p in Punkte(m.Befehle))
                Assert.False(float.IsNaN(p.X) || float.IsNaN(p.Y), "NaN-Bildpunkt im Stundenprofil");

            List<Pfad> reihe = m.Befehle.OfType<Pfad>().Where(b => b.Marke == "reihe:Lüftung [1/h]").ToList();
            Assert.Equal(2, reihe.Count);                          // eine Fläche und eine Linie: Mo–Fr
            Assert.True(reihe[0].Geschlossen);
            Assert.False(reihe[1].Geschlossen);
            Assert.Equal(120, reihe[1].Punkte.Count);             // 5 × 24 Stunden, das Wochenende fehlt

            string d = SvgSchreiber.Reihenpfad(m.Reihen[0], m.Flaeche, true);
            Assert.DoesNotContain("NaN", d, StringComparison.Ordinal);
            Assert.Equal(1, d.Split('M').Length - 1);
            Assert.EndsWith("Z", d, StringComparison.Ordinal);
        }

        /// <summary>
        /// Eine Lücke mitten in der Woche teilt die Reihe in zwei Stücke — im Bild zwei Flächen und
        /// zwei Linien, im SVG zwei geschlossene Teilpfade, roh wie gebündelt.
        /// </summary>
        [Fact]
        public void Eine_Luecke_in_der_Wochenmitte_ergibt_zwei_Stuecke()
        {
            double[] w = Lueftungswoche();
            for (int i = 0; i < 168; i++) if (double.IsNaN(w[i])) w[i] = 1.0;
            for (int s = 0; s < 24; s++) w[2 * 24 + s] = double.NaN;         // Mittwoch „aus"

            Zeichenmodell m = ChartRenderer.StundenprofilModell("Lüftung", w, 24, "Wochenstunde", "Lüftung [1/h]");
            List<Pfad> reihe = m.Befehle.OfType<Pfad>().Where(b => b.Marke == "reihe:Lüftung [1/h]").ToList();
            Assert.Equal(4, reihe.Count);
            Assert.Equal(48, reihe[1].Punkte.Count);
            Assert.Equal(96, reihe[3].Punkte.Count);

            // Die zweite Fläche beginnt am linken Rand des Donnerstags (Wert 72 steht bei 73/168).
            Rahmen bild = m.Flaeche.Bild;
            Assert.Equal(bild.X + 72f / 168f * bild.Breite, reihe[2].Punkte[0].X, 3);

            foreach (bool roh in new[] { true, false })
            {
                string d = SvgSchreiber.Reihenpfad(m.Reihen[0], m.Flaeche, roh);
                Assert.DoesNotContain("NaN", d, StringComparison.Ordinal);
                Assert.Equal(2, d.Split('M').Length - 1);
                Assert.Equal(2, d.Split('Z').Length - 1);
            }
        }

        /// <summary>
        /// <b>Ohne NaN bitgleich zum Bestand:</b> Dieselbe Woche mit und ohne die neue Einheit der
        /// Reihe malt dasselbe PNG, und der SVG-Pfad bleibt ein einziger geschlossener Zug.
        /// </summary>
        [Fact]
        public void Ohne_Luecke_bleibt_das_Stundenprofil_wie_es_war()
        {
            double[] w = Lueftungswoche();
            for (int i = 0; i < 168; i++) if (double.IsNaN(w[i])) w[i] = 0.0;

            Zeichenmodell bestand = ChartRenderer.StundenprofilModell("S", w, 24, "Wochenstunde", "Lüftung [1/h]");
            List<Pfad> reihe = bestand.Befehle.OfType<Pfad>().Where(b => b.Marke == "reihe:Lüftung [1/h]").ToList();
            Assert.Equal(2, reihe.Count);
            Assert.Equal(168, reihe[1].Punkte.Count);
            Assert.Equal(171, reihe[0].Punkte.Count);
            Assert.Equal(bestand.Flaeche.Bild.X, reihe[0].Punkte[0].X);
            string d = SvgSchreiber.Reihenpfad(bestand.Reihen[0], bestand.Flaeche, true);
            Assert.Equal(1, d.Split('M').Length - 1);
        }

        /// <summary>Die Woche einer Größe nennt ihre Einheit an der Achse und an der Reihe.</summary>
        [Fact]
        public void Die_Woche_einer_Groesse_traegt_ihre_Einheit()
        {
            Zeichenmodell m = ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung, Lueftungswoche());
            Datenreihe r = Assert.Single(m.Reihen);
            Assert.Equal("Lüftung [1/h]", r.Name);
            Assert.Equal("1/h", r.Einheit);
            Assert.Contains(m.Befehle.OfType<Text>(), x => x.Inhalt == "Lüftung [1/h]");

            Zeichenmodell p = ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Personen, new double[168]);
            Assert.Equal("%", p.Reihen[0].Einheit);
            Assert.Equal("Personen [%]", p.Reihen[0].Name);
        }

        // =============================================================================
        //  Das Teppichbild
        // =============================================================================

        private static int Tag(int monat, int tagImMonat) => Feiertage.Gemeinjahrestag(monat, tagImMonat) - 1;

        /// <summary>Heizen „Büro" wie in <see cref="KalenderteppichTests"/>: 20/16 °C, Neujahr, Herbstferien, Heizperiode.</summary>
        private static Kalenderteppich BueroHeizen()
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt < 5 && s >= 7 && s < 18 ? 20.0 : 16.0;
            var perioden = new List<Kalenderregel>
            {
                Kalenderregel.Feiertag(100, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(200, DbWerte.KOND_ART_FERIEN, "Herbstferien",
                                       Tag(10, 20) + 1, Tag(10, 31) + 1, Kalenderangabe.AusWert(16.0)),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE,
                                       Standardfahrplan.BEZEICHNER_SAISON, Tag(5, 1) + 1, Tag(9, 30) + 1,
                                       Kalenderangabe.Abgeschaltet),
            };
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(w), null, perioden);
            return Kalenderteppich.Bilden(k, 2025);
        }

        private static IEnumerable<Zeichenbefehl> Alle(IEnumerable<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                yield return b;
                if (b is Gruppe g) foreach (Zeichenbefehl c in Alle(g.Befehle)) yield return c;
            }
        }

        /// <summary>
        /// <b>Büro-Heizen:</b> Die Felder fassen Stunden und Folgetage zusammen — weit unter 2 000
        /// Elementen —, jedes nennt Zeitraum, Stunden, Wert und Quelle; „aus" ist die eigene Rolle
        /// <c>RASTER_LOCH</c> mit Schraffur, das Bezugsjahr steht im Titel.
        /// </summary>
        [Fact]
        public void Buero_Heizen_fasst_zusammen_und_nennt_die_Quelle()
        {
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(BueroHeizen());
            int n = ChartRenderer.Elementzahl(m);
            Assert.InRange(n, 100, 600);
            Assert.Contains(m.Befehle.OfType<Text>(), t => t.Marke == "titel" && t.Inhalt == "Heizen · Bezugsjahr 2025");

            List<Rechteck> felder = m.Befehle.OfType<Rechteck>().Where(r => r.Marke == "reihe:Heizen").ToList();
            Assert.Contains(felder, r => r.Wert == "Mo–Fr 06.–10.01., 7–18 Uhr: 20 °C · Standardwoche");
            Assert.Contains(felder, r => r.Wert == "Mo–Fr 06.–10.01., 0–7 Uhr: 16 °C · Standardwoche");
            Assert.Contains(felder, r => r.Wert == "Mi 01.01., 0–24 Uhr: 16 °C · Neujahr (Feiertag)");
            Assert.Contains(felder, r => r.Wert == "20.–31.10., 0–24 Uhr: 16 °C · Herbstferien (Ferien)");
            Assert.Contains(felder, r => r.Wert == "Sa–So 04.–05.01., 0–24 Uhr: 16 °C · Standardwoche");
            Assert.All(felder, r => Assert.Equal(Farbrolle.HEIZWAERME, r.Fuellung.Ton.Rolle));

            // „aus": die Saison außerhalb der Heizperiode - EIN Feld über fünf Monate, eigene Rolle, schraffiert.
            List<Zeichenbefehl> aus = m.Befehle.Where(b => b.Marke == "reihe:aus").ToList();
            Rechteck saison = Assert.Single(aus.OfType<Rechteck>());
            Assert.Equal("01.05.–30.09., 0–24 Uhr: aus · außerhalb der Saison", saison.Wert);
            Assert.Equal(Farbrolle.RASTER_LOCH, saison.Fuellung.Ton.Rolle);
            Pfad schraffur = Assert.Single(aus.OfType<Pfad>());
            Assert.Equal(Farbrolle.RAHMEN, schraffur.Rand.Ton.Rolle);
            Assert.Equal(saison.Wert, schraffur.Wert);
            foreach (Punkt p in schraffur.Punkte)
            {
                Assert.InRange(p.X, saison.X - 0.01f, saison.X + saison.Breite + 0.01f);
                Assert.InRange(p.Y, saison.Y - 0.01f, saison.Y + saison.Hoehe + 0.01f);
            }

            // Die Legende: zwei Stufen (16 und 20 °C) und „aus"; keine Fußzeile der Vergröberung.
            Assert.Equal(new[] { "20 °C", "16 °C" },
                         m.Befehle.OfType<Text>().Where(t => t.Marke == "skala" && t.Inhalt != "°C").Select(t => t.Inhalt));
            Assert.Contains(m.Befehle, b => b.Marke == "legende:aus");
            Assert.DoesNotContain(Alle(m.Befehle).OfType<Text>(), t => t.Inhalt.StartsWith("Vereinfacht", StringComparison.Ordinal));

            // Die Felder decken jede Stunde des Jahres genau einmal.
            float flaeche = m.Befehle.OfType<Rechteck>().Where(r => r.Marke != null && r.Marke.StartsWith("reihe:", StringComparison.Ordinal))
                                     .Sum(r => r.Breite * r.Hoehe);
            Assert.Equal(949f * 288f, flaeche, 0);
        }

        /// <summary>
        /// Die Lüftung mit „aus" am Wochenende: Das Wochenende ist „aus", nicht 0 1/h — und die
        /// Nullzelle der Nacht bleibt eine Farbe der Skala (B12 im Teppich).
        /// </summary>
        [Fact]
        public void Lueftung_mit_aus_trennt_aus_von_null()
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt >= 5 ? double.NaN : s >= 7 && s < 18 ? 2.0 : 0.0;
            var k = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.AusWoche(w), null, null);
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(Kalenderteppich.Bilden(k, 2025));

            Assert.Contains(m.Befehle.OfType<Rechteck>(), r => r.Marke == "reihe:aus" &&
                            r.Wert == "Sa–So 04.–05.01., 0–24 Uhr: aus · Standardwoche");
            Assert.Contains(m.Befehle.OfType<Rechteck>(), r => r.Marke == "reihe:Lüftung" &&
                            r.Wert == "Mo–Fr 06.–10.01., 0–7 Uhr: 0 1/h · Standardwoche");
            Assert.Contains(m.Befehle.OfType<Rechteck>(), r => r.Marke == "reihe:Lüftung" &&
                            r.Wert == "Mo–Fr 06.–10.01., 7–18 Uhr: 2 1/h · Standardwoche");
            Assert.Equal(m.Befehle.OfType<Rechteck>().Count(r => r.Marke == "reihe:aus"),
                         m.Befehle.OfType<Pfad>().Count(p => p.Marke == "reihe:aus"));
            Assert.True(ChartRenderer.Elementzahl(m) <= ChartRenderer.KALENDERTEPPICH_ELEMENTE_MAX);
        }

        /// <summary>
        /// <b>Die Obergrenze hält</b>: Eine Woche, die jede Stunde zwischen „aus" und 84 verschiedenen
        /// Werten wechselt, trüge ungebündelt rund 13 000 Elemente. Das Bild zeichnet benannt gröber
        /// und bleibt unter 2 000.
        /// </summary>
        [Fact]
        public void Ueber_der_Grenze_zeichnet_das_Bild_benannt_groeber()
        {
            var w = new double[168];
            for (int i = 0; i < 168; i++) w[i] = i % 2 == 0 ? double.NaN : i * 0.05;
            var k = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.AusWoche(w), null, null);
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(Kalenderteppich.Bilden(k, 2025));

            Assert.InRange(ChartRenderer.Elementzahl(m), 1, ChartRenderer.KALENDERTEPPICH_ELEMENTE_MAX);
            Assert.Contains(Alle(m.Befehle).OfType<Text>(), t => t.Inhalt.StartsWith("Vereinfacht auf 3 Farbstufen und Blöcke zu 14 Tagen",
                                                                                      StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_Teppich_steht_der_Leerhinweis()
        {
            Zeichenmodell m = ChartRenderer.KalenderteppichModell(null);
            Assert.Contains(m.Befehle.OfType<Text>(), t => t.Marke == "leerhinweis" && t.Inhalt == "Kein Kalender vorhanden.");
            Assert.NotEmpty(ChartRenderer.KalenderteppichBild(null));
        }

        /// <summary>
        /// Die Texte aus den Ressourcen: unter de-DE wörtlich die Vorgabe des Renderers, unter en-US
        /// die Glossarübersetzung (carpet plot, off, reference year).
        /// </summary>
        [Fact]
        public void Die_Texte_der_Ressourcen_gleichen_unter_de_DE_der_Vorgabe_und_sind_englisch_uebersetzt()
        {
            var vorgabe = new ChartRenderer.KalenderteppichTexte();
            ChartRenderer.KalenderteppichTexte de = ChartRenderer.KalenderteppichTexte.AusRessourcen();
            foreach (var e in typeof(ChartRenderer.KalenderteppichTexte).GetProperties())
                Assert.Equal(e.GetValue(vorgabe), e.GetValue(de));

            using (new Kulturvorrichtung("en-US"))
            {
                ChartRenderer.KalenderteppichTexte en = ChartRenderer.KalenderteppichTexte.AusRessourcen();
                Assert.Equal("off", en.Aus);
                Assert.Equal("{0} · reference year {1}", en.Titel);
                Assert.Equal("Heating", en.Heizen);
                Assert.Equal("standard week", en.QuelleStandardwoche);
                Assert.Equal(12, en.Monate.Split(';').Length);
                Assert.Equal(7, en.Wochentage.Split(';').Length);
                foreach (var e in typeof(ChartRenderer.KalenderteppichTexte).GetProperties())
                {
                    string s = (string)e.GetValue(en);
                    Assert.False(string.IsNullOrWhiteSpace(s), e.Name);
                    Assert.DoesNotContain("ä", s); Assert.DoesNotContain("ö", s); Assert.DoesNotContain("ü", s);
                }
            }
        }
    }
}
