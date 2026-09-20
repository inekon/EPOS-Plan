using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die sieben Bilder OHNE ZEITACHSE</b> (Konzept Diagramme, Etappe E3,
    /// Gruppe c): Kuchen, horizontale Balken, Strombilanz im Monatsverlauf,
    /// Monatssäulen, Monatsstapel, Ring und die Rasterkarte der
    /// Auslegungsoptimierung.
    ///
    /// <para><b>Was hier zugesichert wird.</b> Erstens das Wichtigste: Das PNG des
    /// Bestands ist byte-gleich zu <c>SkiaMaler.Png(…Modell(…))</c> — der Umbau darf
    /// kein Bild verschieben, und diese Zusage steht hier neben der der ChartProben,
    /// damit sie in JEDEM Kern-Lauf mitläuft. Zweitens der Entscheid DG-E3-7: Diese
    /// Bilder sind reine Pixelbilder — <c>Flaeche</c> bleibt <c>null</c>, es gibt
    /// keine <c>Datenreihe</c>, und es gibt deshalb auch keinen Zeitachsen-Zoom.
    /// Drittens der Entscheid DG-E3-6: Jedes Datenelement trägt neben seiner Marke
    /// den fertig formatierten <c>Wert</c>, den die Oberfläche beim Zeigen darauf
    /// anzeigt — im Wortlaut und in der Kultur des Renderers.</para>
    ///
    /// <para><b>Die Kultur ist gepinnt</b> (<see cref="Kulturvorrichtung"/>): Die
    /// Werte tragen deutsche Zahlen („48,0 %", „4.000 €"), und der Windows-Läufer
    /// steht auf en-US. Der Renderer formatiert zwar ausdrücklich gegen <c>de-DE</c>;
    /// die Leertexte kommen aber über <c>BerichtTexte</c> und damit über
    /// <c>CurrentUICulture</c>.</para>
    /// </summary>
    public sealed class ChartRendererGruppeCTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        // Die Gaben — dieselben Zahlen für jeden Fall dieser Klasse
        // =====================================================================

        private static List<ChartRenderer.Segment> Kuchensegmente() => new List<ChartRenderer.Segment>
        {
            new ChartRenderer.Segment("Solarthermie", 12.0, ChartRenderer.C_SOLAR),
            new ChartRenderer.Segment("Wärmepumpe", 48.0, ChartRenderer.C_WP),
            new ChartRenderer.Segment("BHKW", 26.0, ChartRenderer.C_BHKW),
            new ChartRenderer.Segment("Spitzenkessel", 14.0, ChartRenderer.C_KESSEL)
        };

        private static List<ChartRenderer.Balken> Balkenzeilen() => new List<ChartRenderer.Balken>
        {
            new ChartRenderer.Balken("Stamm", 412.0, true),
            new ChartRenderer.Balken("Variante A", 355.0, false)
        };

        private static List<ChartRenderer.Ringsegment> Ringsegmente() => new List<ChartRenderer.Ringsegment>
        {
            new ChartRenderer.Ringsegment("Wärmepumpe", 300, ChartRenderer.C_WP),
            new ChartRenderer.Ringsegment("Spitzenkessel", 100, ChartRenderer.C_KESSEL)
        };

        private static List<ChartRenderer.Reihe> Stapelreihen() => new List<ChartRenderer.Reihe>
        {
            new ChartRenderer.Reihe("Direkt", Monatswerte(10.0), SKColors.Gold),
            new ChartRenderer.Reihe("Speicher", Monatswerte(100.0), SKColors.LightGreen)
        };

        /// <summary>Zwölf Monatswerte <paramref name="ab"/> … <paramref name="ab"/> + 11.</summary>
        private static double[] Monatswerte(double ab)
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++) w[m] = ab + m;
            return w;
        }

        /// <summary>Zwölf gleiche Monatswerte — für eine Achse einer gewollten Größe.</summary>
        private static double[] Gleichwert(double wert)
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++) w[m] = wert;
            return w;
        }

        private static readonly double[] CRATEN = { 0.5, 1.0, 1.5 };
        private static readonly double[] KAPAZITAETEN = { 200.0, 400.0 };

        /// <summary>Zwei Zeilen, drei Spalten — das Beste steht auf (1, 2).</summary>
        private static double[][] Rasterwerte() => new[]
        {
            new[] { -500.0, 1000.0, 2000.0 },
            new[] { 1500.0, 3000.0, 4000.0 }
        };

        /// <summary>Ein Zeitreihensatz mit Strombedarf und zwei Deckungsreihen.</summary>
        private static ZeitreihenSatz Strombilanzsatz()
        {
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Stundenreihe(100.0);
            z.Reihen[ZeitreihenSatz.PV_GENUTZT] = Stundenreihe(30.0);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Stundenreihe(70.0);
            return z;
        }

        private static double[] Stundenreihe(double wert)
        {
            var r = new double[ZeitreihenSatz.Stunden];
            for (int i = 0; i < r.Length; i++) r[i] = wert;
            return r;
        }

        /// <summary>Die sieben Modelle unter ihrem Namen — der Reihe nach.</summary>
        private static List<KeyValuePair<string, Func<Zeichenmodell>>> Bilder()
            => new List<KeyValuePair<string, Func<Zeichenmodell>>>
            {
                Paar("Kuchen", () => ChartRenderer.KuchenModell("Wärmedeckung", Kuchensegmente())),
                Paar("BalkenHorizontal",
                     () => ChartRenderer.BalkenHorizontalModell("Brennstoff", "MWh/a", Balkenzeilen())),
                Paar("StrombilanzMonate",
                     () => ChartRenderer.StrombilanzMonateModell(Strombilanzsatz())),
                Paar("MonatsSaeulen",
                     () => ChartRenderer.MonatsSaeulenModell("Strombedarf", Monatswerte(10.0),
                                                             SKColors.YellowGreen, "MWh")),
                Paar("MonatsStapel",
                     () => ChartRenderer.MonatsStapelModell("Deckung", "kWh", Stapelreihen())),
                Paar("Ring", () => ChartRenderer.RingModell("Deckung", Ringsegmente(), 75.0, "%")),
                Paar("Optimierungsraster",
                     () => ChartRenderer.OptimierungsrasterModell("Jahresüberschuss",
                              "C-Rate [1/h]", "Kapazität [kWh]", "Kapitalwert [€]",
                              CRATEN, KAPAZITAETEN, Rasterwerte(), 1, 2))
            };

        private static KeyValuePair<string, Func<Zeichenmodell>> Paar(
            string name, Func<Zeichenmodell> bau)
            => new KeyValuePair<string, Func<Zeichenmodell>>(name, bau);

        // =====================================================================
        // 1 — Das PNG bleibt, was es war
        // =====================================================================

        /// <summary>
        /// <b>Die unverrückbare Bedingung der Etappe.</b> Jede <c>byte[]</c>-Methode
        /// gibt genau das Modell an den Maler, das ihre <c>…Modell</c>-Schwester
        /// liefert — Byte für Byte dasselbe Bild.
        ///
        /// <para>Die ChartProben messen dasselbe gegen eine eingefrorene Hashliste;
        /// dieser Fall läuft in JEDEM Kern-Lauf mit und nennt im Fehlerfall sofort
        /// das Bild.</para>
        /// </summary>
        [Fact]
        public void JedesBildIstByteGleichZumModellweg()
        {
            Gleich("Kuchen", ChartRenderer.Kuchen("Wärmedeckung", Kuchensegmente()),
                   ChartRenderer.KuchenModell("Wärmedeckung", Kuchensegmente()));

            Gleich("BalkenHorizontal",
                   ChartRenderer.BalkenHorizontal("Brennstoff", "MWh/a", Balkenzeilen()),
                   ChartRenderer.BalkenHorizontalModell("Brennstoff", "MWh/a", Balkenzeilen()));

            Gleich("StrombilanzMonate", ChartRenderer.StrombilanzMonate(Strombilanzsatz()),
                   ChartRenderer.StrombilanzMonateModell(Strombilanzsatz()));

            Gleich("MonatsSaeulen",
                   ChartRenderer.MonatsSaeulen("Strombedarf", Monatswerte(10.0),
                                               SKColors.YellowGreen, "MWh"),
                   ChartRenderer.MonatsSaeulenModell("Strombedarf", Monatswerte(10.0),
                                                     SKColors.YellowGreen, "MWh"));

            Gleich("MonatsStapel", ChartRenderer.MonatsStapel("Deckung", "kWh", Stapelreihen()),
                   ChartRenderer.MonatsStapelModell("Deckung", "kWh", Stapelreihen()));

            // BEIDE Ueberladungen des Rings gehen durch RingModell.
            Gleich("Ring", ChartRenderer.Ring("Deckung", Ringsegmente(), 75.0, "%"),
                   ChartRenderer.RingModell("Deckung", Ringsegmente(), 75.0, "%"));
            Gleich("Ring ohne Legende",
                   ChartRenderer.Ring("Deckung", Ringsegmente(), 75.0, "%", "gedeckt", false),
                   ChartRenderer.RingModell("Deckung", Ringsegmente(), 75.0, "%", "gedeckt", false));

            Gleich("Optimierungsraster",
                   ChartRenderer.Optimierungsraster("Jahresüberschuss", "C-Rate [1/h]",
                        "Kapazität [kWh]", "Kapitalwert [€]", CRATEN, KAPAZITAETEN,
                        Rasterwerte(), 1, 2),
                   ChartRenderer.OptimierungsrasterModell("Jahresüberschuss", "C-Rate [1/h]",
                        "Kapazität [kWh]", "Kapitalwert [€]", CRATEN, KAPAZITAETEN,
                        Rasterwerte(), 1, 2));
        }

        private static void Gleich(string name, byte[] bestand, Zeichenmodell modell)
        {
            Assert.True(bestand != null && bestand.Length > 0, name + ": kein Bild");
            Assert.True(bestand.SequenceEqual(SkiaMaler.Png(modell)),
                        name + ": das Modell malt ein anderes PNG als die Bestandsmethode.");
        }

        /// <summary>
        /// Zweimal erzeugt ergibt dasselbe Modell — die Voraussetzung jedes
        /// Determinismusnachweises.
        /// </summary>
        [Fact]
        public void ZweimalErzeugtIstDasselbeModell()
        {
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in Bilder())
                Assert.True(b.Value().Gleicht(b.Value()),
                            b.Key + ": zweimal erzeugt ergibt zwei verschiedene Modelle.");
        }

        // =====================================================================
        // 2 — DG-E3-7: reine Pixelbilder
        // =====================================================================

        /// <summary>
        /// <b>Keine Zeichenfläche, keine Datenreihe</b> (Entscheid DG-E3-7). Diese
        /// sieben Bilder tragen keine Zeitachse: Zwölf starre Monatsfächer, vier
        /// Varianten und ein Raster aus Kapazität × C-Rate sind nichts, worauf man
        /// zoomen könnte. Der <c>SvgSchreiber</c> baut deshalb kein inneres
        /// <c>&lt;svg&gt;</c> — und genau das muss so bleiben, sonst zeigte die
        /// Oberfläche einen Zoomgriff, hinter dem nichts steht.
        /// </summary>
        [Fact]
        public void KeinesDerSiebenBilderFuehrtFlaecheOderReihen()
        {
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in Bilder())
            {
                Zeichenmodell m = b.Value();
                Assert.True(m.Flaeche == null, b.Key + ": führt eine Zeichenfläche.");
                Assert.True(m.Reihen.Count == 0, b.Key + ": führt Datenreihen.");

                string svg = SvgSchreiber.Text(m, Farbpalette.Vorgabe);
                Assert.DoesNotContain(SvgSchreiber.KLASSE_FLAECHE, svg, StringComparison.Ordinal);
                Assert.DoesNotContain(SvgSchreiber.KLASSE_REIHE, svg, StringComparison.Ordinal);
            }
        }

        // =====================================================================
        // 3 — DG-E3-6: Marke UND Wert an jedem Datenelement
        // =====================================================================

        /// <summary>
        /// <b>Jedes Datenelement trägt beides.</b> Ein Befehl mit der Marke
        /// <c>reihe:…</c> hat einen nicht leeren <c>Wert</c>, und umgekehrt trägt kein
        /// Befehl einen Wert ohne Marke. Ohne diese Zusage gäbe es Säulen, auf die die
        /// Oberfläche zeigen kann, ohne etwas anzeigen zu können.
        /// </summary>
        [Fact]
        public void JedesDatenelementTraegtMarkeUndWert()
        {
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in Bilder())
            {
                List<Zeichenbefehl> alle = Alle(b.Value().Befehle).ToList();
                List<Zeichenbefehl> elemente = alle.Where(c => Istreihe(c.Marke)).ToList();

                Assert.True(elemente.Count > 0, b.Key + ": kein Element mit reihe:-Marke.");
                foreach (Zeichenbefehl c in elemente)
                    Assert.False(string.IsNullOrEmpty(c.Wert),
                                 b.Key + ": Element ohne Wert — " + c.Marke);

                foreach (Zeichenbefehl c in alle)
                    if (c.Wert != null)
                        Assert.True(c.Marke != null, b.Key + ": Wert ohne Marke — " + c.Wert);
            }
        }

        /// <summary>
        /// <b>Titel und Achsen sind markiert</b>, und ein Legendeneintrag schaltet
        /// Elemente, die es gibt: Zu jedem <c>legende:&lt;N&gt;</c> gehört mindestens
        /// ein <c>reihe:&lt;N&gt;</c>. Ohne denselben Schlüssel wäre die Legende kein
        /// Schalter, sondern nur ein Bild.
        /// </summary>
        [Fact]
        public void DieMarkenTragenTitelAchsenUndLegende()
        {
            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in Bilder())
            {
                List<Zeichenbefehl> alle = Alle(b.Value().Befehle).ToList();
                Assert.Contains(alle, c => c.Marke == "titel");

                var reihen = new HashSet<string>(
                    alle.Where(c => Istreihe(c.Marke)).Select(c => Schluessel(c.Marke, "reihe:")),
                    StringComparer.Ordinal);

                foreach (string legende in alle
                             .Where(c => c.Marke != null &&
                                         c.Marke.StartsWith("legende:", StringComparison.Ordinal))
                             .Select(c => Schluessel(c.Marke, "legende:"))
                             .Distinct(StringComparer.Ordinal))
                    Assert.True(reihen.Contains(legende),
                                b.Key + ": Legendeneintrag ohne Elemente — " + legende);
            }

            // Die y-Achse tragen die vier Bilder mit Wertachse, die x-Achse die drei
            // mit Faechern und das Raster.
            foreach (string name in new[] { "StrombilanzMonate", "MonatsSaeulen",
                                            "MonatsStapel", "Optimierungsraster" })
            {
                List<Zeichenbefehl> alle = Alle(Modell(name).Befehle).ToList();
                Assert.Contains(alle, c => c.Marke == "yachse");
                Assert.Contains(alle, c => c.Marke == "xachse");
            }

            // Kuchen, Ring und der horizontale Balken haben KEINE beschriftete Achse —
            // und sollen deshalb auch keine solche Marke vortaeuschen.
            foreach (string name in new[] { "Kuchen", "Ring", "BalkenHorizontal" })
            {
                List<Zeichenbefehl> alle = Alle(Modell(name).Befehle).ToList();
                Assert.DoesNotContain(alle, c => c.Marke == "xachse" || c.Marke == "yachse");
            }
        }

        // =====================================================================
        // 4 — Der Wortlaut des Werts, Bild für Bild
        // =====================================================================

        /// <summary>
        /// <b>Ein Kuchen- und ein Ringsegment nennen ihren ANTEIL</b> — in derselben
        /// Stufung, in der das Bild ihn beschriftet („N1"), und in deutscher
        /// Schreibweise. Der Satz aus dem Entscheid DG-E3-6 steht hier wörtlich.
        /// </summary>
        [Fact]
        public void KuchenUndRingNennenIhrenAnteil()
        {
            List<string> kuchen = Werte(Modell("Kuchen"));
            Assert.Equal(new[] { "Solarthermie: 12,0 %", "Wärmepumpe: 48,0 %",
                                 "BHKW: 26,0 %", "Spitzenkessel: 14,0 %" }, kuchen);

            // 300 von 400 sind 75 Prozent - dieselbe Zahl, die in der Mitte steht.
            List<string> ring = Werte(Modell("Ring"));
            Assert.Equal(new[] { "Wärmepumpe: 75,0 %", "Spitzenkessel: 25,0 %" }, ring);
        }

        /// <summary>
        /// <b>Eine Balkenzeile nennt ihre Variante, ihre Zahl und die Einheit</b> — im
        /// Format, in dem das Bild die Zahl hinter den Balken schreibt („N0"). Alle
        /// vier Befehle der Zeile (Beschriftung, Balken, Rahmen, Zahl) tragen
        /// denselben Wert: Die ZEILE ist das Element.
        /// </summary>
        [Fact]
        public void EineBalkenzeileNenntVarianteZahlUndEinheit()
        {
            Zeichenmodell m = Modell("BalkenHorizontal");
            Assert.Equal(new[] { "Stamm: 412 MWh/a", "Variante A: 355 MWh/a" }, Werte(m));

            Assert.Equal(4, Alle(m.Befehle).Count(c => c.Wert == "Stamm: 412 MWh/a"));

            // Ohne Einheit bleibt die Einheit weg - und das Bild bleibt eines.
            Assert.Equal(new[] { "Stamm: 412", "Variante A: 355" },
                         Werte(ChartRenderer.BalkenHorizontalModell("B", null, Balkenzeilen())));
        }

        /// <summary>
        /// <b>Eine Monatssäule nennt ihren Monat</b>, ein Stapelfeld zusätzlich seine
        /// Reihe — getrennt durch denselben Mittelpunkt, den der Entscheid nennt. Die
        /// Nachkommastellen sind die der EIGENEN y-Achse: Die Säulen rechnen über
        /// <c>Skala.Bedarf</c>, der Stapel über die N0/N1-Regel des <c>YRaster</c>.
        /// </summary>
        [Fact]
        public void MonatssaeuleUndStapelfeldNennenMonatUndReihe()
        {
            List<string> saeulen = Werte(Modell("MonatsSaeulen"));
            Assert.Equal(12, saeulen.Count);
            Assert.Equal("Jan: 10 MWh", saeulen[0]);
            Assert.Equal("Dez: 21 MWh", saeulen[11]);

            List<string> stapel = Werte(Modell("MonatsStapel"));
            Assert.Equal(24, stapel.Count);
            Assert.Equal("Jan · Direkt: 10 kWh", stapel[0]);
            Assert.Equal("Jan · Speicher: 100 kWh", stapel[1]);

            // Eine kleine Achse bekommt eine Nachkommastelle - Bild und Zeigetext
            // tragen dieselbe Stufung (YRaster: N0 ab 10, sonst N1).
            var klein = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("Klein", Gleichwert(0.1), SKColors.Gold) };
            Assert.Equal("Jan · Klein: 0,1 kWh",
                         Werte(ChartRenderer.MonatsStapelModell("K", "kWh", klein))[0]);
        }

        /// <summary>
        /// <b>Der Monatsbalken der Strombilanz</b> nennt Monat, Reihe und Einheit; der
        /// BEDARFSZUG dagegen nur seinen Namen — er ist EIN Element über zwölf Monate
        /// und zeigt keine einzelne Zahl.
        /// </summary>
        [Fact]
        public void DerBedarfszugNenntNurSeinenNamen()
        {
            Zeichenmodell m = Modell("StrombilanzMonate");
            List<string> werte = Werte(m);

            Assert.Contains("Jan · PV-Eigenverbrauch: 22 MWh/Monat", werte);
            Assert.Contains("Strombedarf", werte);

            Zeichenbefehl zug = Alle(m.Befehle)
                .Single(c => c.Marke == "reihe:Strombedarf" && c is Pfad);
            Assert.Equal("Strombedarf", zug.Wert);
        }

        /// <summary>
        /// <b>Eine Rasterzelle nennt BEIDE Stützstellen und ihren Zielwert</b> — jede
        /// in dem Format, in dem das Bild ihre Achse beschriftet, und die Zahl mit der
        /// Einheit der Farbskala. Die Achsenbeschriftung trägt ihre Einheit in eckigen
        /// Klammern; im Wert steht sie hinter der Zahl, wo sie hingehört.
        /// </summary>
        [Fact]
        public void EineRasterzelleNenntBeideAchsenUndDieEinheitDerSkala()
        {
            Zeichenmodell m = Modell("Optimierungsraster");
            List<string> werte = Werte(m);

            Assert.Equal(6, werte.Count);            // 2 Kapazitaeten x 3 C-Raten
            Assert.Equal("Kapazität 200 kWh · C-Rate 0,5 1/h: -500 €", werte[0]);
            Assert.Equal("Kapazität 400 kWh · C-Rate 1,5 1/h: 4.000 €", werte[5]);

            // Die Bestmarke nennt DIESELBE Zelle wie das Raster darunter.
            Zeichenbefehl beste = Alle(m.Befehle).Single(c => c.Marke == "marke");
            Assert.Equal("Kapazität 400 kWh · C-Rate 1,5 1/h: 4.000 €", beste.Wert);

            // Die Farbskala ist die Legende dieses Bildes und traegt ihre eigene Marke.
            Assert.Contains(Alle(m.Befehle), c => c.Marke == "skala");

            // Die Erlaeuterung hinter der Einheit gehoert zur ACHSE, nicht zur Zelle.
            Zeichenmodell mitSatz = ChartRenderer.OptimierungsrasterModell("J",
                "C-Rate [1/h] (Leistung = Kapazität × C-Rate)", "Kapazität [kWh]",
                "Kapitalwert [€]", CRATEN, KAPAZITAETEN, Rasterwerte(), -1, -1);
            Assert.Equal("Kapazität 200 kWh · C-Rate 0,5 1/h: -500 €", Werte(mitSatz)[0]);
        }

        /// <summary>
        /// <b>Ein LOCH nennt seinen Grund, eine SPERRE ihre Sperre.</b> Beides sagt
        /// das Bild schon — das eine durch die Lochfarbe (#226), das andere durch die
        /// Schraffur (#193) —, und der Zeigetext sagt es in Worten. Die Schraffur
        /// gehört dabei zur Zelle: Sie trägt denselben Wert.
        /// </summary>
        [Fact]
        public void LochUndSperreStehenImWert()
        {
            double[][] werte = Rasterwerte();
            werte[0][0] = double.NaN;
            var sperre = new[] { new[] { false, false, false }, new[] { false, false, true } };

            Zeichenmodell m = ChartRenderer.OptimierungsrasterModell("J", "C-Rate [1/h]",
                "Kapazität [kWh]", "Kapitalwert [€]", CRATEN, KAPAZITAETEN, werte, 1, 2, sperre);

            List<string> texte = Werte(m);
            Assert.Equal("Kapazität 200 kWh · C-Rate 0,5 1/h: nicht gerechnet", texte[0]);
            Assert.Equal("Kapazität 400 kWh · C-Rate 1,5 1/h: 4.000 € (unzulässig)", texte[5]);

            // Zelle UND Schraffur: zwei DATENELEMENTE mit demselben Wert.
            const string gesperrt = "Kapazität 400 kWh · C-Rate 1,5 1/h: 4.000 € (unzulässig)";
            Assert.Equal(2, Alle(m.Befehle).Count(c => Istreihe(c.Marke) && c.Wert == gesperrt));

            // Und die Bestmarke steht auf genau dieser Zelle - sie sagt deshalb
            // dasselbe, samt ihrer Sperre.
            Assert.Equal(gesperrt, Alle(m.Befehle).Single(c => c.Marke == "marke").Wert);
        }

        // =====================================================================
        // 5 — Die Leerfälle
        // =====================================================================

        /// <summary>
        /// <b>Ein Bild ohne Daten sagt das — und markiert es.</b> Der Hinweis trägt
        /// <c>leerhinweis</c>, damit die Oberfläche ihn erkennt, statt eine leere
        /// Zeichenfläche anzubieten; Datenelemente gibt es dann keine.
        /// </summary>
        [Fact]
        public void DieLeerfaelleTragenDenLeerhinweis()
        {
            var leer = new List<KeyValuePair<string, Zeichenmodell>>
            {
                new KeyValuePair<string, Zeichenmodell>("MonatsSaeulen ohne Werte",
                    ChartRenderer.MonatsSaeulenModell("M", null, SKColors.Red, "MWh")),
                new KeyValuePair<string, Zeichenmodell>("MonatsSaeulen zu kurz",
                    ChartRenderer.MonatsSaeulenModell("M", new double[3], SKColors.Red, "MWh")),
                new KeyValuePair<string, Zeichenmodell>("MonatsStapel ohne Reihen",
                    ChartRenderer.MonatsStapelModell("M", "kWh", new List<ChartRenderer.Reihe>())),
                new KeyValuePair<string, Zeichenmodell>("Ring ohne Segmente",
                    ChartRenderer.RingModell("R", null, 0, "%")),
                new KeyValuePair<string, Zeichenmodell>("Raster ohne Stützstellen",
                    ChartRenderer.OptimierungsrasterModell("J", "x", "y", "s",
                        new double[0], new double[0], null, -1, -1))
            };

            foreach (KeyValuePair<string, Zeichenmodell> l in leer)
            {
                List<Zeichenbefehl> alle = Alle(l.Value.Befehle).ToList();
                Assert.Contains(alle, c => c.Marke == "leerhinweis");
                Assert.DoesNotContain(alle, c => Istreihe(c.Marke));
                Assert.Null(l.Value.Flaeche);

                // Und das Bild entsteht trotzdem.
                Assert.True(SkiaMaler.Png(l.Value).Length > 0, l.Key + ": kein Bild");
            }

            // Die Strombilanz liefert in ihren Leerfaellen weiterhin GAR KEIN Modell -
            // und die Bestandsmethode deshalb weiterhin null.
            Assert.Null(ChartRenderer.StrombilanzMonateModell(new ZeitreihenSatz()));
            Assert.Null(ChartRenderer.StrombilanzMonate(new ZeitreihenSatz()));
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static Zeichenmodell Modell(string name)
            => Bilder().First(b => b.Key == name).Value();

        private static bool Istreihe(string marke)
            => marke != null && marke.StartsWith("reihe:", StringComparison.Ordinal);

        private static string Schluessel(string marke, string vorsatz)
            => marke.Substring(vorsatz.Length);

        /// <summary>Alle Befehle eines Modells, auch die in Gruppen — in Zeichenreihenfolge.</summary>
        private static IEnumerable<Zeichenbefehl> Alle(IReadOnlyList<Zeichenbefehl> befehle)
        {
            if (befehle == null) yield break;
            foreach (Zeichenbefehl b in befehle)
            {
                yield return b;
                if (b is Gruppe g)
                    foreach (Zeichenbefehl k in Alle(g.Befehle)) yield return k;
            }
        }

        /// <summary>
        /// Die Werte der Datenelemente in Zeichenreihenfolge, jeder EINMAL: Ein Element
        /// besteht aus einem bis vier Befehlen mit demselben Wert (die Balkenzeile aus
        /// vier, die gesperrte Rasterzelle aus zwei).
        /// </summary>
        private static List<string> Werte(Zeichenmodell m)
        {
            var werte = new List<string>();
            string letzter = null;
            foreach (Zeichenbefehl b in Alle(m.Befehle))
            {
                if (!Istreihe(b.Marke) || string.IsNullOrEmpty(b.Wert)) continue;
                if (string.Equals(b.Wert, letzter, StringComparison.Ordinal)) continue;
                werte.Add(b.Wert);
                letzter = b.Wert;
            }
            return werte;
        }
    }
}
