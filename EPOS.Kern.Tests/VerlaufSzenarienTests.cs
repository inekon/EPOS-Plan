using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Seiten.Simulation;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E6 — <b>der Verlauf mit drei Szenarien</b> (Konzept Wirtschaftlichkeit
    /// § 2.13 (5), Analysepapier § 3.3 A3, Mockup Kategorie 8, Anhangzeilen U3 und U13).
    ///
    /// <para><b>Was diese Fälle festhalten.</b></para>
    /// <list type="bullet">
    ///   <item><description>die dritte Strichart: die Aufzählung ist wertgleich zum alten
    ///   Schalter, die Vorgabe zeichnet byte-gleich;</description></item>
    ///   <item><description>die Dreierreihe: drei Einzelläufe, Zahl für Zahl, ohne Schreiben in
    ///   die Datenbank, frei wählbarer Horizont, Sicht 2 allein B gegen A;</description></item>
    ///   <item><description>der Nulldurchgang: dieselbe Regel wie die Amortisationskennzahl;</description></item>
    ///   <item><description>die Reihenbildung: Farbe = Stand, Strichart = Szenario, Name
    ///   „Stand · Szenario", zweigeteilte Legende, benannte Ablehnung über acht Ständen;</description></item>
    ///   <item><description>das Bild: festes Maß bei zwei Legendenzeilen, länger bei mehr, die
    ///   Marken am Element;</description></item>
    ///   <item><description>das Excel-Blatt „Verlauf": je Jahr eine Zeile, je Stand und
    ///   Szenario eine Spalte in der Spaltengruppe des Szenarios, und die Hülle schreibt es
    ///   über den Dateidienst.</description></item>
    /// </list>
    ///
    /// <para><b>Die Prüfgruppen.</b> Synthetisch ohne Datenbank (ein Sammelmodell aus
    /// Probelinien) und — für die Läufe — die Gruppe der Blattstruktur-Wache (Stamm 9001,
    /// Variante A 9002) sowie 1040–1042 mit gebuchtem Stand. Wer die Testdatenbank oder ein
    /// <c>Dienste.*</c> braucht, steht in der seriellen Sammlung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VerlaufSzenarienTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 9001;
        private const int VARIANTE_A = 9002;

        private static readonly string WORST = WirtschaftlichkeitSzenario.WORST;
        private static readonly string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private static readonly string BEST = WirtschaftlichkeitSzenario.BEST;

        // =====================================================================
        //  (1) Die dritte Strichart
        // =====================================================================

        /// <summary>
        /// Die Vorgabe ist DURCHGEZOGEN — an Reihe und Legendeneintrag —, und die
        /// Strichfolgen sind die des Bestands: gestrichelt 8/5 wie der alte Schalter,
        /// gepunktet 2,5/3,5, durchgezogen keine.
        /// </summary>
        [Fact]
        public void Die_Strichart_ist_vorgabemaessig_durchgezogen_und_wertgleich_zum_Schalter()
        {
            Assert.Equal(ChartRenderer.Strichart.Durchgezogen,
                         new ChartRenderer.Reihe("R", new[] { 1.0, 2.0 }, SKColors.Red).Strichart);
            Assert.Equal(ChartRenderer.Strichart.Durchgezogen,
                         new ChartRenderer.Reihe("R", new[] { 1.0 }, SKColors.Red, ChartRenderer.Stapelart.Keine).Strichart);
            Assert.Equal(ChartRenderer.Strichart.Durchgezogen,
                         new ChartRenderer.Segment("S", 1.0, SKColors.Red).Strichart);
            Assert.Equal(0, (int)ChartRenderer.Strichart.Durchgezogen);

            Assert.Null(ChartRenderer.Strichfolge(ChartRenderer.Strichart.Durchgezogen));
            Assert.Equal(new Strichmuster(8f, 5f), ChartRenderer.Strichfolge(ChartRenderer.Strichart.Gestrichelt));
            Assert.Equal(new Strichmuster(ChartRenderer.PUNKT_STRICH, ChartRenderer.PUNKT_LUECKE),
                         ChartRenderer.Strichfolge(ChartRenderer.Strichart.Gepunktet));
            Assert.NotEqual(ChartRenderer.Strichfolge(ChartRenderer.Strichart.Gestrichelt),
                            ChartRenderer.Strichfolge(ChartRenderer.Strichart.Gepunktet));
        }

        /// <summary>
        /// Der alte Schalter der Stammlinie (U18) heißt jetzt „gestrichelt": dieselbe Reihe,
        /// dieselbe Strichfolge in der Datenreihe und im Legendenfeld — die übrigen Linien
        /// bleiben durchgezogen.
        /// </summary>
        [Fact]
        public void Die_gestrichelte_Stammlinie_ist_der_alte_Schalter()
        {
            List<ChartRenderer.Reihe> mit = ChartRenderer.VerlaufsReihen(Absolutserien(), true, true);
            List<ChartRenderer.Reihe> ohne = ChartRenderer.VerlaufsReihen(Absolutserien(), true);

            Assert.Equal(ChartRenderer.Strichart.Gestrichelt, mit[0].Strichart);
            Assert.All(mit.Skip(1), r => Assert.Equal(ChartRenderer.Strichart.Durchgezogen, r.Strichart));
            Assert.All(ohne, r => Assert.Equal(ChartRenderer.Strichart.Durchgezogen, r.Strichart));

            Zeichenmodell m = ChartRenderer.KapitalwertVerlaufModell("K", mit, null);
            Assert.Equal(new Strichmuster(8f, 5f), m.Reihen[0].Muster);
            Assert.Null(m.Reihen[1].Muster);
            Assert.Equal(new Strichmuster(8f, 5f), Legendenrand(m, "Stamm"));
        }

        /// <summary>
        /// Die DRITTE Strichart kommt an: in der Datenreihe, im Legendenfeld und im Bild — ein
        /// gepunktetes Bild ist ein anderes als ein gestricheltes.
        /// </summary>
        [Fact]
        public void Die_dritte_Strichart_wird_gezeichnet()
        {
            List<ChartRenderer.Reihe> punkte = ChartRenderer.VerlaufsReihen(Absolutserien(), true);
            punkte[0].Strichart = ChartRenderer.Strichart.Gepunktet;
            List<ChartRenderer.Reihe> striche = ChartRenderer.VerlaufsReihen(Absolutserien(), true);
            striche[0].Strichart = ChartRenderer.Strichart.Gestrichelt;

            Zeichenmodell m = ChartRenderer.KapitalwertVerlaufModell("K", punkte, null);
            Assert.Equal(new Strichmuster(ChartRenderer.PUNKT_STRICH, ChartRenderer.PUNKT_LUECKE), m.Reihen[0].Muster);
            Assert.Equal(new Strichmuster(ChartRenderer.PUNKT_STRICH, ChartRenderer.PUNKT_LUECKE),
                         Legendenrand(m, "Stamm"));

            Assert.False(ChartRenderer.KapitalwertVerlauf("K", punkte, null)
                         .SequenceEqual(ChartRenderer.KapitalwertVerlauf("K", striche, null)));
        }

        /// <summary>
        /// „Vorgabe byte-gleich": Eine Reihe, die die Strichart ausdrücklich auf
        /// „durchgezogen" setzt, zeichnet Byte für Byte wie eine, die sie nicht nennt.
        /// </summary>
        [Fact]
        public void Ohne_Strichart_bleibt_das_Bild_byte_gleich()
        {
            List<ChartRenderer.Reihe> ausdruecklich = ChartRenderer.VerlaufsReihen(Absolutserien(), true);
            foreach (ChartRenderer.Reihe r in ausdruecklich) r.Strichart = ChartRenderer.Strichart.Durchgezogen;

            Assert.Equal(ChartRenderer.KapitalwertVerlauf("K", ChartRenderer.VerlaufsReihen(Absolutserien(), true), "F"),
                         ChartRenderer.KapitalwertVerlauf("K", ausdruecklich, "F"));
        }

        // =====================================================================
        //  (2) Die Dreierreihe — drei Einzelläufe
        // =====================================================================

        /// <summary>
        /// <b>Die Dreierreihe IST drei Einzelläufe</b>: je Szenario dieselben Linien — absolut
        /// und als Differenz — wie der Einzellauf von <c>BerechneVerlauf</c>, Zahl für Zahl,
        /// dazu dieselben Restwerte. Die Reihenfolge der Läufe ist die des Bildes.
        /// </summary>
        [Fact]
        public void Die_Dreierreihe_ist_drei_Einzellaeufe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitVerlaufSzenarien drei =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppendaten(), Parametersatz(), 20);

            Assert.Equal(20, drei.Jahre);
            Assert.Equal(new[] { WORST, ERWARTET, BEST }, WirtschaftlichkeitVerlaufSzenarien.Reihenfolge.ToArray());
            Assert.Equal(3, drei.Laeufe.Count);

            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                WirtschaftlichkeitVerlauf einzeln =
                    new WirtschaftlichkeitCtrl().BerechneVerlauf(Gruppendaten(), Parametersatz(), 20, s);
                WirtschaftlichkeitVerlauf lauf = drei.Lauf(s);
                Assert.NotNull(lauf);
                Assert.Equal(s, lauf.Szenario);
                Gleich(einzeln.Absolut, lauf.Absolut);
                Gleich(einzeln.Differenz, lauf.Differenz);
            }

            // Die drei Szenarien sind drei verschiedene Läufe - nicht dreimal Erwartet.
            Assert.NotEqual(drei.Differenz(VARIANTE_A, WORST).Kumuliert[20],
                            drei.Differenz(VARIANTE_A, BEST).Kumuliert[20]);
            Assert.Equal(new[] { VARIANTE_A }, drei.Versionen().Select(v => v.Key).ToArray());
        }

        /// <summary>Der Horizont darf vom Betrachtungszeitraum abweichen — die Linien folgen ihm.</summary>
        [Fact]
        public void Der_Horizont_darf_vom_Betrachtungszeitraum_abweichen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitVerlaufSzenarien drei =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppendaten(), Parametersatz(), 25);

            Assert.Equal(25, drei.Jahre);
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                Assert.Equal(26, drei.Differenz(VARIANTE_A, s).Kumuliert.Length);
        }

        /// <summary>
        /// <b>Ohne Schreiben in die Datenbank</b> (Muster BerechneBandbreite): Die gebuchten
        /// Ergebniszeilen der Gruppe bleiben, wie sie sind.
        /// </summary>
        [Fact]
        public void Die_Dreierreihe_schreibt_nicht_in_die_Datenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Fingerabdruck(1040, 1041, 1042);
            WirtschaftlichkeitVerlaufSzenarien drei =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppe1040(), Parametersatz1040(), 20);

            Assert.Equal(3, drei.Laeufe.Count);
            Assert.Equal(vorher, Fingerabdruck(1040, 1041, 1042));
        }

        /// <summary>
        /// Sicht 2 (Konzept § 2.15, VG‑Q5): Gerechnet wird gegen A, gezeichnet allein B — die
        /// eine Linie B − A in drei Stricharten.
        /// </summary>
        [Fact]
        public void In_Sicht_2_steht_allein_B_gegen_A()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppe1040();
            daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = 1041, IdB = 1042 };
            WirtschaftlichkeitVerlaufSzenarien drei =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(daten, Parametersatz1040(), 20);

            Assert.Equal(new[] { 1042 }, drei.Versionen().Select(v => v.Key).ToArray());
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                WirtschaftlichkeitVerlauf lauf = drei.Lauf(s);
                VerlaufSerie a = lauf.Absolut.Single(x => x.IdProjekt == 1041);
                VerlaufSerie b = lauf.Absolut.Single(x => x.IdProjekt == 1042);
                VerlaufSerie d = drei.Differenz(1042, s);
                if (a.Kumuliert == null || b.Kumuliert == null) { Assert.Null(d); continue; }
                for (int t = 0; t <= 20; t++)
                    Assert.Equal(b.Kumuliert[t] - a.Kumuliert[t], d.Kumuliert[t], 9);
            }
        }

        // =====================================================================
        //  (3) Der Nulldurchgang
        // =====================================================================

        /// <summary>
        /// Die Regel auf der kumulierten Reihe: linear im Jahr des Durchgangs; ohne
        /// Mehrinvestition 0, wenn auch das Ende nicht negativ ist, sonst keiner; ohne
        /// Durchgang und bei nicht endlichen Werten keiner.
        /// </summary>
        [Theory]
        [InlineData(new[] { -100.0, -50.0, 0.0, 50.0 }, 2.0)]
        [InlineData(new[] { -100.0, -40.0, 20.0 }, 1.0 + 40.0 / 60.0)]
        [InlineData(new[] { -1000.0, -700.0, -400.0, -100.0, 200.0 }, 3.0 + 100.0 / 300.0)]
        [InlineData(new[] { 10.0, 20.0 }, 0.0)]
        [InlineData(new[] { 10.0, -5.0 }, double.NaN)]
        [InlineData(new[] { -100.0, -90.0, -80.0 }, double.NaN)]
        [InlineData(new[] { -100.0, double.NaN, 50.0 }, double.NaN)]
        [InlineData(new double[0], double.NaN)]
        public void Der_Nulldurchgang_folgt_der_Regel(double[] kumuliert, double erwartet)
        {
            double? jahr = KapitalwertRechner.Nulldurchgang(kumuliert);
            if (double.IsNaN(erwartet)) Assert.Null(jahr);
            else Assert.Equal(erwartet, jahr.Value, 12);
        }

        /// <summary>
        /// <b>Die Marke im Bild und die Zelle der Kennzahltafel können nicht auseinanderlaufen</b>:
        /// Auf der kumulierten Differenz zweier Zahlungsbilder liefert der Nulldurchgang
        /// dieselbe Zahl wie <c>AmortisationDifferenz</c> — mit Durchgang, ohne, und ohne
        /// Mehrinvestition.
        /// </summary>
        [Theory]
        [InlineData(new[] { -1000.0, 300.0, 300.0, 300.0, 300.0 }, new[] { 0.0, 0.0, 0.0, 0.0, 0.0 })]
        [InlineData(new[] { -5000.0, 400.0, 380.0, 360.0, 340.0 }, new[] { -1000.0, 10.0, 10.0, 10.0, 10.0 })]
        [InlineData(new[] { -100.0, 50.0, 50.0 }, new[] { -300.0, 10.0, 10.0 })]
        [InlineData(new[] { -100.0, -10.0, -10.0 }, new[] { -300.0, 60.0, 60.0 })]
        public void Der_Nulldurchgang_ist_die_Amortisationskennzahl(double[] variante, double[] referenz)
        {
            var kum = new double[variante.Length];
            double summe = 0;
            for (int t = 0; t < variante.Length; t++)
            {
                summe += variante[t] - referenz[t];
                kum[t] = summe;
            }

            double? amortisation = KapitalwertRechner.AmortisationDifferenz(
                new KapitalwertRechner.Zahlungsbild { BarwertReihe = variante },
                new KapitalwertRechner.Zahlungsbild { BarwertReihe = referenz });
            double? nulldurchgang = KapitalwertRechner.Nulldurchgang(kum);

            Assert.Equal(amortisation.HasValue, nulldurchgang.HasValue);
            if (amortisation.HasValue) Assert.Equal(amortisation.Value, nulldurchgang.Value, 9);
        }

        /// <summary>
        /// Im gerechneten Verlauf trägt der Erwartungsfall über T dieselbe dynamische
        /// Amortisation wie der gebuchte Lauf der Kennzahltafel.
        /// </summary>
        [Fact]
        public void Im_Erwartungsfall_ist_der_Nulldurchgang_die_Kennzahl_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitVerlaufSzenarien drei =
                new WirtschaftlichkeitCtrl().BerechneVerlaufSzenarien(Gruppe1040(), Parametersatz1040(), 20);
            List<WirtschaftlichkeitErgebnis> lauf =
                new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040(), 0, false);

            foreach (int id in new[] { 1041, 1042 })
            {
                WirtschaftlichkeitErgebnis e = lauf.Single(x => x.IdProjekt == id && x.Szenario == ERWARTET);
                double? marke = drei.Nulldurchgang(id, ERWARTET);
                Assert.Equal(e.AmortisationJahre.HasValue, marke.HasValue);
                if (marke.HasValue) Assert.Equal(e.AmortisationJahre.Value, marke.Value, 6);
            }
        }

        // =====================================================================
        //  (4) Die Reihenbildung
        // =====================================================================

        /// <summary>
        /// <b>Farbe = Stand, Strichart = Szenario</b>, Name „Stand · Szenario": Die drei
        /// Linien eines Standes teilen EINE Farbrolle, verschiedene Stände haben verschiedene;
        /// Ungünstig ist gestrichelt, Erwartet durchgezogen und kräftiger, Günstig gepunktet.
        /// </summary>
        [Fact]
        public void Farbe_ist_der_Stand_und_Strichart_das_Szenario()
        {
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(3), new ChartRenderer.VerlaufSzenarienTexte());

            Assert.Null(r.Ablehnung);
            Assert.Equal(9, r.Reihen.Count);
            Assert.Equal("Variante 1 · Ungünstig", r.Reihen[0].Name);
            Assert.Equal("Variante 1 · Erwartet", r.Reihen[1].Name);
            Assert.Equal("Variante 1 · Günstig", r.Reihen[2].Name);
            Assert.Equal("Variante 3 · Günstig", r.Reihen[8].Name);

            for (int stand = 0; stand < 3; stand++)
            {
                IEnumerable<ChartRenderer.Reihe> drei = r.Reihen.Skip(3 * stand).Take(3);
                Assert.All(drei, x => Assert.Equal(Serienrolle(stand), x.Rolle));
            }
            Assert.Equal(ChartRenderer.Strichart.Gestrichelt, r.Reihen[0].Strichart);
            Assert.Equal(ChartRenderer.Strichart.Durchgezogen, r.Reihen[1].Strichart);
            Assert.Equal(ChartRenderer.Strichart.Gepunktet, r.Reihen[2].Strichart);
            Assert.True(r.Reihen[1].Breite > r.Reihen[0].Breite, "Erwartet trägt die Aussage und steht kräftiger.");
        }

        /// <summary>
        /// Die Legende ist ZWEIGETEILT: so viele Einträge wie Stände, dazu drei — nicht Stände
        /// mal drei.
        /// </summary>
        [Fact]
        public void Die_Legende_ist_zweigeteilt()
        {
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(3), new ChartRenderer.VerlaufSzenarienTexte());

            Assert.Equal(new[] { "Variante 1", "Variante 2", "Variante 3" }, r.Varianten.Select(v => v.Name).ToArray());
            Assert.Equal(new[] { "Ungünstig", "Erwartet", "Günstig" }, r.Szenarien.Select(s => s.Name).ToArray());
            Assert.Equal(new[] { ChartRenderer.Strichart.Gestrichelt, ChartRenderer.Strichart.Durchgezogen,
                                 ChartRenderer.Strichart.Gepunktet },
                         r.Szenarien.Select(s => s.Strichart).ToArray());
        }

        /// <summary>
        /// Die Farbe hängt am PLATZ in der Gruppe: Ein abgewählter Stand färbt die übrigen
        /// nicht um.
        /// </summary>
        [Fact]
        public void Ein_abgewaehlter_Stand_faerbt_die_uebrigen_nicht_um()
        {
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(3), new ChartRenderer.VerlaufSzenarienTexte(), new[] { 902, 903 });

            Assert.Equal(6, r.Reihen.Count);
            Assert.All(r.Reihen.Take(3), x => Assert.Equal(Serienrolle(1), x.Rolle));
            Assert.All(r.Reihen.Skip(3), x => Assert.Equal(Serienrolle(2), x.Rolle));
        }

        /// <summary>
        /// Nur die gewählten Szenarien — die Legende nennt nur sie.
        /// </summary>
        [Fact]
        public void Nur_die_gewaehlten_Szenarien()
        {
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(2), new ChartRenderer.VerlaufSzenarienTexte(), null, new[] { ERWARTET });

            Assert.Equal(new[] { "Variante 1 · Erwartet", "Variante 2 · Erwartet" }, r.Reihen.Select(x => x.Name).ToArray());
            Assert.Equal(new[] { "Erwartet" }, r.Szenarien.Select(s => s.Name).ToArray());
        }

        /// <summary>
        /// Die Palette kennt ACHT Farben: acht Stände werden gezeichnet, neun lehnt die
        /// Reihenbildung BENANNT ab — statt eine Farbe doppelt zu vergeben.
        /// </summary>
        [Fact]
        public void Ueber_acht_Staenden_lehnt_die_Reihenbildung_benannt_ab()
        {
            Assert.Equal(8, ChartRenderer.HoechstensStaende);

            ChartRenderer.Szenarienreihen acht = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(8), new ChartRenderer.VerlaufSzenarienTexte());
            Assert.Null(acht.Ablehnung);
            Assert.Equal(24, acht.Reihen.Count);
            Assert.Equal(8, acht.Reihen.Select(x => x.Rolle).Distinct().Count());

            ChartRenderer.Szenarienreihen neun = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(9), new ChartRenderer.VerlaufSzenarienTexte());
            Assert.NotNull(neun.Ablehnung);
            Assert.Contains("8", neun.Ablehnung);
            Assert.Empty(neun.Reihen);

            // Acht von neun gewählt: wieder ein Bild, die Farben nach dem Platz unter den gewählten.
            ChartRenderer.Szenarienreihen gewaehlt = ChartRenderer.VerlaufsReihenSzenarien(
                Probemodell(9), new ChartRenderer.VerlaufSzenarienTexte(), Enumerable.Range(902, 8).ToList());
            Assert.Null(gewaehlt.Ablehnung);
            Assert.Equal(Serienrolle(0), gewaehlt.Reihen[0].Rolle);

            // Die Ablehnung aus den Ressourcen nennt dieselbe Zahl.
            Assert.Contains("8", string.Format(ChartRenderer.VerlaufSzenarienTexte.AusRessourcen().ZuVieleVarianten, 8));
        }

        /// <summary>
        /// Je Linie mit Durchgang EINE Marke, mit dem Jahr nach der Regel des Kerns.
        /// </summary>
        [Fact]
        public void Je_Linie_ein_Nulldurchgang_nach_der_Regel_des_Kerns()
        {
            WirtschaftlichkeitVerlaufSzenarien modell = Probemodell(3);
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(
                modell, new ChartRenderer.VerlaufSzenarienTexte());

            Assert.Equal(9, r.Marken.Count);
            foreach (ChartRenderer.Nulldurchgangsmarke m in r.Marken)
            {
                ChartRenderer.Reihe linie = r.Reihen.Single(x => x.Name == m.Reihe);
                Assert.Equal(KapitalwertRechner.Nulldurchgang(linie.Werte).Value, m.Jahr, 12);
                Assert.StartsWith(m.Stand + ChartRenderer.REIHENTRENNER, m.Reihe);
                Assert.EndsWith(m.Szenario, m.Reihe);
            }

            // Ein Stand ohne Durchgang bekommt keine Marke.
            WirtschaftlichkeitVerlaufSzenarien ohne = Probemodell(1, nutzen: 1.0);
            Assert.Empty(ChartRenderer.VerlaufsReihenSzenarien(ohne, new ChartRenderer.VerlaufSzenarienTexte()).Marken);
        }

        // =====================================================================
        //  (5) Das Bild
        // =====================================================================

        /// <summary>
        /// Zwei Legendenzeilen tragen das feste Maß 1240 × 620; die Zeichenfläche ist die des
        /// Bildes je Version, jede Linie steht als Datenreihe darin, und die Legende nennt
        /// jeden Stand, jedes Szenario und die Marke je EINMAL.
        /// </summary>
        [Fact]
        public void Das_Bild_traegt_das_feste_Mass_und_die_zweigeteilte_Legende()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte);
            Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, "Fuß");

            Assert.Equal(1240, m.Breite);
            Assert.Equal(620, m.Hoehe);
            Assert.NotNull(m.Flaeche);
            Assert.Equal(400f, m.Flaeche.Bild.Hoehe);
            Assert.Equal(9, m.Reihen.Count);

            var legende = m.Befehle.Select(b => b.Marke).Where(x => x != null && x.StartsWith("legende:"))
                           .Distinct().ToList();
            Assert.Equal(3 + 3 + 1, legende.Count);
            foreach (string name in new[] { "Variante 1", "Variante 2", "Variante 3", "Ungünstig", "Erwartet",
                                            "Günstig", texte.Nulldurchgang })
                Assert.Contains("legende:" + name, legende);

            // Die Marken der Nulldurchgänge tragen ihren Wert am Element.
            var marken = m.Befehle.Where(b => b.Marke == "nulldurchgang").ToList();
            Assert.Equal(r.Marken.Count, marken.Where(b => b is Kreis).Count());
            Assert.All(marken, b => Assert.False(string.IsNullOrEmpty(b.Wert)));
        }

        /// <summary>
        /// Mehr als zwei Legendenzeilen passen nicht ins feste Maß — das Bild wird um ganze
        /// Legendenzeilen LÄNGER, die Zeichenfläche bleibt.
        /// </summary>
        [Fact]
        public void Viele_Staende_verlaengern_das_Bild_um_ganze_Legendenzeilen()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell("Verlauf",
                ChartRenderer.VerlaufsReihenSzenarien(Probemodell(8, lang: true), texte), texte, null);

            Assert.True(m.Hoehe > 620, "Acht lange Namen brauchen mehr als zwei Legendenzeilen.");
            Assert.Equal(0, (m.Hoehe - 620) % (int)ChartRenderer.LEGENDE_ZEILE);
            Assert.Equal(400f, m.Flaeche.Bild.Hoehe);
        }

        /// <summary>Die Fußnote des Berichts — lang genug für mehr als eine Zeile.</summary>
        private const string LANGE_FUSSNOTE =
            "Kumulierter Barwert der Differenz zur Referenz je Jahr, ohne Restwert; Farbe = Variante, " +
            "Strichart = Szenario; der Nulldurchgang ist die dynamische Amortisation. Dieser Satz " +
            "verlängert die Fußnote, damit sie auf jeder Schrift der Ersatzkette umbricht.";

        /// <summary>Die Zeilen der kursiven Fußnote (14 pt) eines Modells in Zeichenreihenfolge.</summary>
        private static List<Text> Fusszeilen(Zeichenmodell m)
            => m.Befehle.OfType<Text>().Where(t => t.Schrift.Kursiv && t.Schrift.Punkt == 14f).ToList();

        /// <summary>Die gemessene Breite eines Textbefehls [px] — dieselbe Schrift wie der Maler.</summary>
        private static float Breite(Text t)
        {
            using (SKFont f = Schriftkette.Erzeuge(t.Schrift)) return f.MeasureText(t.Inhalt);
        }

        /// <summary>
        /// Anwenderbefund Word-Export: Die kursive Fußnote lief rechts aus dem Bild. Sie bricht
        /// jetzt an der Breite der Legende um, jede Zeile liegt innerhalb der Bildbreite, der
        /// Text geht vollständig durch, und das Bild wird um die zusätzlichen Zeilen länger —
        /// die Zeichenfläche bleibt 400 hoch. Im Zielmaß räumt die Zeichenfläche ihnen Platz.
        /// </summary>
        [Fact]
        public void Eine_lange_Fussnote_bricht_um_und_liegt_in_der_Bildbreite()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte);
            Zeichenmodell kurz = ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, "Fuß");
            Zeichenmodell lang = ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, LANGE_FUSSNOTE);

            List<Text> zeilen = Fusszeilen(lang);
            Assert.True(zeilen.Count >= 2, "Die lange Fußnote muss umbrechen.");
            Assert.Equal(LANGE_FUSSNOTE, string.Join(" ", zeilen.Select(t => t.Inhalt)));
            foreach (Text t in zeilen)
                Assert.True(t.X + Breite(t) <= lang.Breite - 30f + 0.5f,
                            "Fußzeile „" + t.Inhalt + "\" ragt rechts aus dem Bild.");
            Assert.True(lang.Hoehe > kurz.Hoehe, "Das Bild wächst um die zusätzlichen Fußzeilen.");
            Assert.Equal(400f, lang.Flaeche.Bild.Hoehe);
            Assert.True(zeilen.Last().Y < lang.Hoehe - 20f, "Die letzte Fußzeile steht im Bild.");
            for (int i = 1; i < zeilen.Count; i++) Assert.True(zeilen[i].Y > zeilen[i - 1].Y);

            // Zielmaß: Die Höhe bleibt, die Fläche wird niedriger.
            Zeichenmodell ziel = ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, LANGE_FUSSNOTE,
                                                                          new Bildmass(1240, 620));
            Assert.Equal(620, ziel.Hoehe);
            Assert.True(ziel.Flaeche.Bild.Hoehe < 400f);
            Assert.All(Fusszeilen(ziel), t => Assert.True(t.X + Breite(t) <= ziel.Breite && t.Y < ziel.Hoehe - 20f));

            // Das Bild je Version bricht nach derselben Regel um.
            List<ChartRenderer.Reihe> reihen = ChartRenderer.VerlaufsReihen(Absolutserien(), true);
            Zeichenmodell vk = ChartRenderer.KapitalwertVerlaufModell("K", reihen, "Fuß");
            Zeichenmodell vl = ChartRenderer.KapitalwertVerlaufModell("K", reihen, LANGE_FUSSNOTE);
            List<Text> vz = Fusszeilen(vl);
            Assert.True(vz.Count >= 2);
            Assert.Equal(LANGE_FUSSNOTE, string.Join(" ", vz.Select(t => t.Inhalt)));
            Assert.All(vz, t => Assert.True(t.X + Breite(t) <= vl.Breite - 30f + 0.5f));
            Assert.True(vl.Hoehe > vk.Hoehe);
            Assert.Equal(vk.Flaeche.Bild.Hoehe, vl.Flaeche.Bild.Hoehe);
            Assert.True(vz.Last().Y < vl.Hoehe - 20f);
        }

        /// <summary>
        /// Anwenderbefund Word-Export: Die x-Achseneinheit „Jahr" war am rechten Rand
        /// angeschnitten und stieß an die letzte Jahreszahl. Sie steht jetzt ganz im Bild und
        /// rechts neben der letzten Zahl — im Bild je Version, im Verlauf der drei Szenarien
        /// und im schmalen Zielmaß.
        /// </summary>
        [Fact]
        public void Die_Einheit_Jahr_liegt_ganz_im_Bild()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte);
            List<ChartRenderer.Reihe> reihen = ChartRenderer.VerlaufsReihen(Absolutserien(), true);
            var modelle = new[]
            {
                ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, null),
                ChartRenderer.KapitalwertSzenarienModell("Verlauf", r, texte, null, new Bildmass(760, 400)),
                ChartRenderer.KapitalwertVerlaufModell("K", reihen, null),
                ChartRenderer.KapitalwertVerlaufModell("K", reihen, null, new Bildmass(560, 400)),
            };
            string einheit = BerichtTexte.T("Jahr");
            foreach (Zeichenmodell m in modelle)
            {
                List<Text> achse = m.Befehle.OfType<Text>().Where(t => t.Marke == "xachse").ToList();
                Text jahr = achse.Single(t => t.Inhalt == einheit);
                Assert.True(jahr.X + Breite(jahr) <= m.Breite,
                            "„" + einheit + "\" endet bei " + (jahr.X + Breite(jahr)) + " > " + m.Breite);
                Text letzte = achse.Where(t => t.Inhalt != einheit).OrderBy(t => t.X).Last();
                Assert.True(jahr.X >= letzte.X + Breite(letzte),
                            "„" + einheit + "\" stößt an die letzte Jahreszahl " + letzte.Inhalt);
            }
        }

        /// <summary>
        /// Anwenderbefund Word-Export: Word zeigt das eingebettete SVG und übergeht
        /// <c>dominant-baseline</c> — der Titel war oben abgeschnitten. Im Druck-SVG trägt
        /// jeder Text seine Grundlinie selbst: Die Versalhöhe (hier großzügig 0,7 Geviert)
        /// liegt über der Grundlinie innerhalb des Bildes, die Unterlänge darunter auch.
        /// </summary>
        [Fact]
        public void Im_Druck_SVG_steht_jeder_Text_ohne_dominant_baseline_im_Bild()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            ChartRenderer.Szenarienreihen r = ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte);
            Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell(
                "Kumulierter Barwert der Differenz zur Referenz — drei Szenarien", r, texte, LANGE_FUSSNOTE);

            List<SvgKnoten> knoten = SvgSchreiber.Druckbaum(m, null, "d", Schriftkette.Aufstieg)
                .Alle().Where(k => k.Name == "text").ToList();
            Assert.NotEmpty(knoten);
            foreach (SvgKnoten k in knoten)
            {
                Assert.DoesNotContain(k.Attribute, a => a.Key == "dominant-baseline");
                float y = float.Parse(k.Attribute.Single(a => a.Key == "y").Value,
                                      System.Globalization.CultureInfo.InvariantCulture);
                float groesse = float.Parse(k.Attribute.Single(a => a.Key == "font-size").Value.Replace("px", ""),
                                            System.Globalization.CultureInfo.InvariantCulture);
                Assert.True(y - 0.7f * groesse >= 0f, "„" + k.Inhalt + "\" ragt oben aus dem Bild (y = " + y + ").");
                Assert.True(y + 0.25f * groesse <= m.Hoehe, "„" + k.Inhalt + "\" ragt unten aus dem Bild.");
            }
        }

        /// <summary>Die benannte Ablehnung steht an der Stelle des Bildes — ohne Zeichenfläche.</summary>
        [Fact]
        public void Die_Ablehnung_steht_an_der_Stelle_des_Bildes()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            Zeichenmodell m = ChartRenderer.KapitalwertSzenarienModell("Verlauf",
                ChartRenderer.VerlaufsReihenSzenarien(Probemodell(9), texte), texte, null);

            Assert.Null(m.Flaeche);
            Assert.Equal(620, m.Hoehe);
            Assert.Contains(m.Befehle, b => b.Marke == "leerhinweis");
        }

        /// <summary>Das Bild ist deterministisch — Modell und PNG.</summary>
        [Fact]
        public void Das_Bild_ist_deterministisch()
        {
            var texte = new ChartRenderer.VerlaufSzenarienTexte();
            Zeichenmodell a = ChartRenderer.KapitalwertSzenarienModell("V",
                ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte), texte, "F");
            Zeichenmodell b = ChartRenderer.KapitalwertSzenarienModell("V",
                ChartRenderer.VerlaufsReihenSzenarien(Probemodell(3), texte), texte, "F");

            Assert.True(a.Gleicht(b));
            Assert.Equal(SkiaMaler.Png(a), SkiaMaler.Png(b));
        }

        // =====================================================================
        //  (6) Das Excel-Blatt „Verlauf"
        // =====================================================================

        /// <summary>
        /// U13: je Jahr eine Zeile, je Stand und Szenario eine Spalte — in einer SPALTENGRUPPE
        /// je Szenario (Ungünstig · Erwartet · Günstig) —, darunter Nulldurchgang, Restwert und
        /// Kapitalwertdifferenz je Spalte. Die Werte sind die Linien des Bildes.
        /// </summary>
        [Fact]
        public void Das_Blatt_Verlauf_traegt_je_Jahr_eine_Zeile_und_je_Stand_und_Szenario_eine_Spalte()
        {
            WirtschaftlichkeitVerlaufSzenarien modell = Probemodell(2);
            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "verlauf.xlsx");
                VerlaufExcel.SchreibeMappe(pfad, modell, VerlaufBlattTexte.AusRessourcen(), "Referenz: Stamm");

                using var wb = new XLWorkbook(pfad);
                Assert.Single(wb.Worksheets);
                IXLWorksheet ws = wb.Worksheet("Verlauf");
                Assert.Equal("Kumulierter Barwert der Differenz zur Referenz je Jahr [€] — ohne Restwert",
                             ws.Cell(1, 1).GetString());
                Assert.Equal("Referenz: Stamm", ws.Cell(2, 1).GetString());

                // Die Spaltengruppen: je Szenario zwei Spalten, der Name steht über ihnen.
                Assert.Equal("Ungünstig", ws.Cell(VerlaufExcel.ZEILE_GRUPPEN, 2).GetString());
                Assert.Equal("Erwartet", ws.Cell(VerlaufExcel.ZEILE_GRUPPEN, 4).GetString());
                Assert.Equal("Günstig", ws.Cell(VerlaufExcel.ZEILE_GRUPPEN, 6).GetString());
                Assert.True(ws.Cell(VerlaufExcel.ZEILE_GRUPPEN, 2).IsMerged());

                Assert.Equal("Jahr", ws.Cell(VerlaufExcel.ZEILE_KOPF, 1).GetString());
                Assert.Equal(new[] { "Variante 1", "Variante 2", "Variante 1", "Variante 2", "Variante 1", "Variante 2" },
                             Enumerable.Range(2, 6).Select(c => ws.Cell(VerlaufExcel.ZEILE_KOPF, c).GetString()).ToArray());

                // Je Jahr eine Zeile, die Werte sind die Linien.
                string[] reihenfolge = WirtschaftlichkeitVerlaufSzenarien.Reihenfolge.ToArray();
                for (int t = 0; t <= 20; t++)
                {
                    Assert.Equal(t, ws.Cell(VerlaufExcel.ZEILE_JAHR0 + t, 1).GetDouble());
                    for (int g = 0; g < 3; g++)
                        for (int s = 0; s < 2; s++)
                            Assert.Equal(modell.Differenz(901 + s, reihenfolge[g]).Kumuliert[t],
                                         ws.Cell(VerlaufExcel.ZEILE_JAHR0 + t, 2 + 2 * g + s).GetDouble(), 6);
                }

                // Darunter je Spalte: Nulldurchgang, Restwert, Kapitalwertdifferenz.
                int z0 = VerlaufExcel.ZEILE_JAHR0 + 21;
                Assert.Equal("Nulldurchgang (dynamische Amortisation) [a]", ws.Cell(z0, 1).GetString());
                Assert.Equal(Math.Round(modell.Nulldurchgang(901, WORST).Value, 2), ws.Cell(z0, 2).GetDouble(), 6);
                VerlaufSerie d = modell.Differenz(902, BEST);
                Assert.Equal(d.RestwertBarwert, ws.Cell(z0 + 1, 7).GetDouble(), 6);
                Assert.Equal(d.Kumuliert[20] + d.RestwertBarwert, ws.Cell(z0 + 2, 7).GetDouble(), 6);
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Ohne Durchgang steht ein Strich — nie eine 0, die ein Jahr behauptete.</summary>
        [Fact]
        public void Ohne_Durchgang_steht_ein_Strich_und_keine_Null()
        {
            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "ohne.xlsx");
                VerlaufExcel.SchreibeMappe(pfad, Probemodell(1, nutzen: 1.0), new VerlaufBlattTexte(), null);

                using var wb = new XLWorkbook(pfad);
                IXLWorksheet ws = wb.Worksheet("Verlauf");
                int z0 = VerlaufExcel.ZEILE_JAHR0 + 21;
                for (int c = 2; c <= 4; c++) Assert.Equal("—", ws.Cell(z0, c).GetString());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Die Wahl des Abschnitts schränkt das Blatt ein — dieselben Linien wie das Bild.</summary>
        [Fact]
        public void Die_Wahl_des_Abschnitts_schraenkt_das_Blatt_ein()
        {
            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "wahl.xlsx");
                VerlaufExcel.SchreibeMappe(pfad, Probemodell(3), new VerlaufBlattTexte(), null,
                                           new[] { 902 }, new[] { ERWARTET });

                using var wb = new XLWorkbook(pfad);
                IXLWorksheet ws = wb.Worksheet("Verlauf");
                Assert.Equal("Erwartet", ws.Cell(VerlaufExcel.ZEILE_GRUPPEN, 2).GetString());
                Assert.Equal("Variante 2", ws.Cell(VerlaufExcel.ZEILE_KOPF, 2).GetString());
                Assert.True(ws.Cell(VerlaufExcel.ZEILE_KOPF, 3).IsEmpty());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// „Verlauf nach Excel…" (U13) geht über <c>Dienste.Datei</c>: Die Hülle fragt nach
        /// dem Ziel, schreibt das Blatt und meldet den Pfad; ein Abbruch im Dateiwähler bleibt
        /// still. Ohne gerechneten Verlauf sagt sie, was fehlt.
        /// </summary>
        [Fact]
        public async Task Verlauf_nach_Excel_schreibt_ueber_den_Dateidienst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IDateiDienst vorher = Dienste.Datei;
            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "knopf.xlsx");
                var datei = new MerkenderDateidienst { Antwort = ziel };
                Dienste.Datei = datei;

                var huelle = new KapitalwertVerlaufHuelle(1040, "Stammprojekt",
                    () => new VerlaufKontext { Gewaehlt = new List<int> { 1040, 1041, 1042 } });

                Rueckmeldung leer = await huelle.NachExcel(VerlaufWahl.Alle);
                Assert.False(leer.Erfolg);
                Assert.Equal(0, datei.Gefragt);

                huelle.DatenUebernehmen(Gruppe1040());
                Assert.True(huelle.Gerechnet);

                Rueckmeldung ok = await huelle.NachExcel(VerlaufWahl.Alle);
                Assert.True(ok.Erfolg, ok.Text);
                Assert.Equal(1, datei.Gefragt);
                Assert.EndsWith(".xlsx", datei.Vorschlag);
                Assert.Contains(ziel, ok.Text);
                using (var wb = new XLWorkbook(ziel))
                    Assert.True(wb.Worksheets.Contains("Verlauf"));

                datei.Antwort = "";
                Rueckmeldung still = await huelle.NachExcel(VerlaufWahl.Alle);
                Assert.Same(Rueckmeldung.Still, still);
            }
            finally
            {
                Dienste.Datei = vorher;
                Aufraeumen(ordner);
            }
        }

        /// <summary>
        /// Die Hülle des Abschnitts zeichnet aus den übernommenen Eingangsdaten: drei Szenarien
        /// als Nummern, die angehakten Stände, Bild, Nulldurchgangs- und Statuszeile.
        /// </summary>
        [Fact]
        public void Die_Huelle_zeichnet_aus_den_uebernommenen_Eingangsdaten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new KapitalwertVerlaufHuelle(1040, "Stammprojekt",
                () => new VerlaufKontext { Gewaehlt = new List<int> { 1040, 1041, 1042 } });
            huelle.DatenUebernehmen(Gruppe1040());

            VerlaufAnsicht alle = huelle.Zeichnen(VerlaufWahl.Alle);
            Assert.NotNull(alle.Modell);
            Assert.Equal(new[] { 0, 1, 2 }, alle.Szenarien.Select(s => s.Id).ToArray());
            Assert.Equal(new[] { "Ungünstig", "Erwartet", "Günstig" }, alle.Szenarien.Select(s => s.Text).ToArray());
            Assert.Equal(alle.Staende.Select(s => s.Id), alle.GewaehlteStaende);
            Assert.False(string.IsNullOrEmpty(alle.Statuszeile));

            VerlaufAnsicht eine = huelle.Zeichnen(new VerlaufWahl(new[] { alle.Staende[0].Id }, new[] { 1 }));
            Assert.Equal(new[] { alle.Staende[0].Id }, eine.GewaehlteStaende);
            Assert.Equal(new[] { 1 }, eine.GewaehlteSzenarien);
            Assert.NotSame(alle.Modell, eine.Modell);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Farbrolle Serienrolle(int i) => new[]
        {
            Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
            Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
        }[i];

        /// <summary>Drei Absolutreihen wie in den ChartProben (Stamm und zwei Varianten).</summary>
        private static List<VerlaufSerie> Absolutserien()
        {
            var liste = new List<VerlaufSerie>();
            foreach ((string name, bool stamm, double invest, double nutzen) in new[]
                     {
                         ("Stamm", true, -180000.0, 21000.0),
                         ("Variante A", false, -260000.0, 32000.0),
                         ("Variante B", false, -95000.0, 9000.0)
                     })
            {
                var k = new double[21];
                k[0] = invest;
                for (int t = 1; t < k.Length; t++) k[t] = k[t - 1] + nutzen * Math.Pow(0.97, t);
                liste.Add(new VerlaufSerie { Anzeige = name, IstStamm = stamm, Kumuliert = k });
            }
            return liste;
        }

        /// <summary>
        /// Ein synthetisches Sammelmodell: Stamm 900 als Referenz, Stände 901 … mit je einer
        /// Differenzlinie in allen drei Szenarien über 20 Jahre.
        /// </summary>
        private static WirtschaftlichkeitVerlaufSzenarien Probemodell(int staende, double nutzen = 160000.0,
                                                                     bool lang = false)
        {
            var modell = new WirtschaftlichkeitVerlaufSzenarien { Jahre = 20 };
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                double f = s == WORST ? 1.1 : s == BEST ? 0.9 : 1.0;
                double g = s == WORST ? 0.9 : s == BEST ? 1.1 : 1.0;
                var lauf = new WirtschaftlichkeitVerlauf { Jahre = 20, Szenario = s };
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 900, Anzeige = "Stamm", IstStamm = true,
                                                    Kumuliert = new double[21] });
                for (int i = 0; i < staende; i++)
                {
                    string name = lang ? "Variante " + (i + 1) + " mit Wärmepumpe und Speicher" : "Variante " + (i + 1);
                    var d = new double[21];
                    d[0] = -(400000.0 + 50000.0 * i) * f;
                    for (int t = 1; t <= 20; t++) d[t] = d[t - 1] + nutzen * g * Math.Pow(0.97, t);
                    lauf.Absolut.Add(new VerlaufSerie { IdProjekt = 901 + i, Anzeige = name, Kumuliert = d });
                    lauf.Differenz.Add(new VerlaufSerie { IdProjekt = 901 + i, Anzeige = name, Kumuliert = d,
                                                          RestwertBarwert = 1000.0 * (i + 1) });
                }
                modell.Laeufe[s] = lauf;
            }
            return modell;
        }

        /// <summary>Der Rand des Legendenfeldes eines Eintrags — seine Strichfolge.</summary>
        private static Strichmuster Legendenrand(Zeichenmodell m, string name)
        {
            foreach (Zeichenbefehl b in m.Befehle)
                if (b.Marke == "legende:" + name && b is Rechteck r && r.Rand != null && r.Rand.Muster != null)
                    return r.Rand.Muster;
            return null;
        }

        private static void Gleich(List<VerlaufSerie> erwartet, List<VerlaufSerie> ist)
        {
            Assert.Equal(erwartet.Count, ist.Count);
            for (int i = 0; i < erwartet.Count; i++)
            {
                Assert.Equal(erwartet[i].IdProjekt, ist[i].IdProjekt);
                Assert.Equal(erwartet[i].RestwertBarwert, ist[i].RestwertBarwert);
                if (erwartet[i].Kumuliert == null) { Assert.Null(ist[i].Kumuliert); continue; }
                Assert.Equal(erwartet[i].Kumuliert, ist[i].Kumuliert);
            }
        }

        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));
            return daten;
        }

        private static WirtschaftlichkeitParameter Parametersatz()
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = STAMM,
                IdReferenzprojekt = 0,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };
        }

        private static BerichtsDaten Gruppe1040()
        {
            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(1040, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(1041, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(1042, false, "Variante B", 7000.0));
            return daten;
        }

        private static WirtschaftlichkeitParameter Parametersatz1040()
        {
            WirtschaftlichkeitParameter p = Parametersatz();
            p.IdStamm = 1040;
            return p;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }

        /// <summary>Der gebuchte Stand einer Gruppe: je Ergebniszeile Projekt, Szenario und Zeitstempel.</summary>
        private static string Fingerabdruck(params int[] ids)
        {
            return string.Join(";", new WirtschaftlichkeitCtrl().LadeErgebnisse(ids.ToList())
                .OrderBy(e => e.IdProjekt).ThenBy(e => e.Szenario, StringComparer.Ordinal)
                .Select(e => e.IdProjekt + "|" + e.Szenario + "|" + e.Zeitstempel.Ticks));
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e6-verlauf-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }

        /// <summary>Ein Dateidienst, der die Frage mitzählt und einen festen Pfad nennt.</summary>
        private sealed class MerkenderDateidienst : IDateiDienst
        {
            public string Antwort = "";
            public string Vorschlag = "";
            public int Gefragt;

            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";

            public string DateiSpeichern(string titel, string filter, string vorschlag)
            {
                Gefragt++;
                Vorschlag = vorschlag ?? "";
                return Antwort;
            }

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad) => false;
        }
    }
}
