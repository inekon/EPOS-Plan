using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SkiaSharp;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die AUSLEGUNGSOPTIMIERUNG als Controller (W11b‑B‑5, Windows-Abnahme V2 vom
    /// 07.09.2026) — der Nachweis, dass der Umzug aus
    /// <c>Form_SpeicherOptimierung</c> (1 325 Zeilen, die letzte WinForms-Fachmaske)
    /// nichts an den Zahlen ändert.
    ///
    /// <para><b>Wogegen hier geprüft wird.</b> Für die Rastersuche gibt es keinen
    /// Referenzlauf — sie ist nicht Teil der Simulationskette. Gesichert wird sie
    /// deshalb gegen die <b>im Kopfkommentar der abgelösten Maske dokumentierte
    /// Formel</b>:</para>
    /// <code>
    /// max dJ(C, P) = E_a,aeq(C, P) − [ c_cap·C + c_pow·P + I_fix ] · a(i_z, N)  [ − K_ver ]
    /// </code>
    /// <para>Das ist zugleich der Zeuge dafür, dass der Controller nichts
    /// <b>rechnet</b>: Er prüft, ruft <see cref="SpeicherOptimierer"/>, formatiert und
    /// zeichnet. Die Rechnung selbst liegt unverändert in <c>SpeicherEngine</c> und
    /// hat dort ihre eigenen Tests (Handrechnung auf vier Intervallen).</para>
    ///
    /// <para><b>Ohne Datenbank.</b> Die Vorbereitung
    /// (<see cref="StromspeicherOptimierungVorbereitung"/>) ist die Nahtstelle zwischen
    /// Datenbank- und Rechenteil und lässt sich von Hand füllen — genau dafür ist sie
    /// getrennt worden.</para>
    ///
    /// <para><b>Die Kultur ist auf de-DE gepinnt.</b> Die Kennzahlen und die
    /// Statuszeile sind TEXT; ihre Dezimaltrennung hängt an der Kultur, und ein
    /// CI-Läufer steht auf en-US. Seit #232b pinnt der Konstruktor THREADGEBUNDEN auch
    /// <c>CurrentUICulture</c> (Ressourcentexte über <c>Resource.*</c> fallen sonst auf den
    /// prozessweiten Vorgabewert zurück, den über 50 andere Klassen laufend umschalten —
    /// Messbefund #232).</para>
    /// </summary>
    public sealed class SpeicherOptimierungCtrlTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherOptimierungCtrlTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        // =================================================================
        // Synthetischer Lauf — vier Viertelstunden, verlustfrei, Zins 0
        // =================================================================

        private const double Dt = 0.25;

        /// <summary>
        /// Zwei Viertelstunden mit 40 kW Überschuss, zwei mit 40 kW Defizit — dieselbe
        /// Reihe, an der <c>SpeicherOptimiererTests</c> seine Handrechnung prüft.
        /// </summary>
        private static SpeicherEingang Eingang()
        {
            double[] last = { 0.0, 0.0, 40.0, 40.0 };
            double[] pv = { 40.0, 40.0, 0.0, 0.0 };
            double[] preis = { 20.0, 20.0, 20.0, 20.0 };
            return new SpeicherEingang(last, pv, preis);
        }

        /// <summary>
        /// Basisauslegung 10 kWh / 10 kW, verlustfrei, Zins 0, N = 10 a.
        /// Mit Zins 0 gilt <c>a = 1/N</c> und <c>RBF_deg = N</c> — die Zielfunktion ist
        /// damit von Hand nachrechenbar.
        /// </summary>
        private static SpeicherParameter Basis() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = Dt,
            VerguetungCtKwh = 5.0,
            CCapEurProKwh = 100.0,
            CPowEurProKw = 50.0,
            IFixEur = 1000.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 10.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.025
        };

        private static StromspeicherOptimierungVorbereitung Vorbereitung()
            => new StromspeicherOptimierungVorbereitung
            {
                Eingang = Eingang(),
                Basis = Basis(),
                Kontext = null
            };

        /// <summary>Ein kleiner Suchraum: zwei Kapazitäten, eine C-Rate, kein Feinraster.</summary>
        private static SpeicherOptimierungEingaben Suchraum() => new SpeicherOptimierungEingaben
        {
            CMinKwh = 10.0,
            CMaxKwh = 20.0,
            Stuetzstellen = 2,
            RMin = 1.0,
            RMax = 1.0,
            RSchritt = 1.0,
            Feinraster = false,
            KVerInZielfunktion = false
        };

        private static SpeicherOptimierungErgebnis Lauf(SpeicherOptimierungEingaben e = null)
            => SpeicherOptimierungCtrl.Rechnen(Vorbereitung(), e ?? Suchraum(), null, CancellationToken.None);

        // =================================================================
        // Suchraum
        // =================================================================

        [Fact]
        public void Der_Suchraum_wird_in_die_Optionen_der_Engine_uebersetzt()
        {
            OptimiererOptionen o = SpeicherOptimierungCtrl.Optionen(new SpeicherOptimierungEingaben
            {
                CMinKwh = 500,
                CMaxKwh = 5000,
                Stuetzstellen = 10,
                RMin = 0.5,
                RMax = 3.0,
                RSchritt = 0.5,
                Feinraster = true,
                KVerInZielfunktion = true,
                Strategie = OptimiererStrategie.Nachtnutzung
            });

            Assert.Equal(500.0, o.CMinKwh);
            Assert.Equal(5000.0, o.CMaxKwh);
            Assert.Equal(10, o.Stuetzstellen);
            Assert.Equal(6, o.CRatenAnzahl);
            Assert.True(o.Feinraster);
            Assert.True(o.KVerInZielfunktion);
            Assert.Equal(OptimiererStrategie.Nachtnutzung, o.Strategie);
        }

        [Fact]
        public void Die_Punktzahl_ist_die_der_Vorgabe_des_Fachkonzepts()
        {
            // 2 Phasen x 10 Kapazitaeten x 6 C-Raten = 120 - die Zahl, die in der
            // Abnahme V2 auf dem Fortschrittsbalken stand.
            Assert.Equal(120, SpeicherOptimierungCtrl.Punktzahl(new SpeicherOptimierungEingaben()));
        }

        [Fact]
        public void Ohne_Feinraster_ist_es_die_haelfte()
        {
            var e = new SpeicherOptimierungEingaben { Feinraster = false };
            Assert.Equal(60, SpeicherOptimierungCtrl.Punktzahl(e));
        }

        [Fact]
        public void Eine_unbrauchbare_Eingabe_hat_keine_Punktzahl()
        {
            // Sie hat KEINE erfundene: 0 heisst "sag nichts", nicht "null Punkte".
            Assert.Equal(0, SpeicherOptimierungCtrl.Punktzahl(
                new SpeicherOptimierungEingaben { RSchritt = 0.0 }));
            Assert.Equal(0, SpeicherOptimierungCtrl.Punktzahl(
                new SpeicherOptimierungEingaben { Stuetzstellen = 1 }));
        }

        // =================================================================
        // Eingabeprüfung
        // =================================================================

        [Fact]
        public void Die_Vorgabe_des_Fachkonzepts_ist_brauchbar()
        {
            Assert.Empty(SpeicherOptimierungCtrl.Pruefe(new SpeicherOptimierungEingaben()));
        }

        [Theory]
        [InlineData(0.0, 5000.0, 10, 0.5, 3.0, 0.5)]      // C_min = 0
        [InlineData(5000.0, 500.0, 10, 0.5, 3.0, 0.5)]    // bis < von
        [InlineData(500.0, 5000.0, 1, 0.5, 3.0, 0.5)]     // eine Stuetzstelle
        [InlineData(500.0, 5000.0, 51, 0.5, 3.0, 0.5)]    // ueber der Schranke
        [InlineData(500.0, 5000.0, 10, 0.0, 3.0, 0.5)]    // C-Rate 0
        [InlineData(500.0, 5000.0, 10, 3.0, 0.5, 0.5)]    // r_max < r_min
        [InlineData(500.0, 5000.0, 10, 0.5, 3.0, 0.0)]    // Schrittweite 0
        public void Eine_unbrauchbare_Eingabe_wird_gemeldet(
            double cMin, double cMax, int stuetzstellen, double rMin, double rMax, double rSchritt)
        {
            var maengel = SpeicherOptimierungCtrl.Pruefe(new SpeicherOptimierungEingaben
            {
                CMinKwh = cMin,
                CMaxKwh = cMax,
                Stuetzstellen = stuetzstellen,
                RMin = rMin,
                RMax = rMax,
                RSchritt = rSchritt
            });

            Assert.NotEmpty(maengel);
            Assert.All(maengel, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        }

        /// <summary>
        /// Die Schranke, die die abgelöste Maske nicht kannte: Eine zu kleine
        /// Schrittweite machte aus einem Klick auf „Optimierung starten" hunderttausende
        /// Jahresläufe. Hier bleibt der Lauf stehen, bevor er beginnt.
        /// </summary>
        [Fact]
        public void Ein_zu_grosser_Suchraum_wird_gemeldet()
        {
            var e = new SpeicherOptimierungEingaben
            {
                CMinKwh = 100,
                CMaxKwh = 10000,
                Stuetzstellen = 50,
                RMin = 0.1,
                RMax = 5.0,
                RSchritt = 0.001
            };

            Assert.True(SpeicherOptimierungCtrl.Punktzahl(e) > SpeicherOptimierungCtrl.PUNKTE_MAX);
            Assert.NotEmpty(SpeicherOptimierungCtrl.Pruefe(e));
        }

        [Fact]
        public void Eine_unbrauchbare_Eingabe_startet_keinen_Lauf()
        {
            SpeicherOptimierungErgebnis erg = SpeicherOptimierungCtrl.Rechnen(
                Vorbereitung(),
                new SpeicherOptimierungEingaben { CMinKwh = 0.0 },
                null, CancellationToken.None);

            Assert.False(erg.Erfolg);
            Assert.False(erg.Abgebrochen);
            Assert.NotEmpty(erg.Meldung);
            Assert.Null(erg.RasterBild);
        }

        // =================================================================
        // Die Zielfunktion — der Kern des Nachweises
        // =================================================================

        /// <summary>
        /// <b>dJ = E_a,äq − I · a(i_z, N)</b>, wörtlich die Formel aus dem Kopfkommentar
        /// der abgelösten Maske. Geprüft an JEDEM Rasterpunkt, nicht nur am Bestpunkt.
        /// </summary>
        [Fact]
        public void Die_Zielfunktion_ist_der_Jahresueberschuss_nach_Kapitaldienst()
        {
            OptimiererErgebnis roh = StromspeicherSimCtrl.FuehreOptimierungAus(
                Vorbereitung(), SpeicherOptimierungCtrl.Optionen(Suchraum()), null, CancellationToken.None);

            foreach (OptimiererPunkt p in Alle(roh.Grobraster))
            {
                Assert.Equal(p.ErtragAequivalentEur - p.AnnuitaetEur, p.ZielfunktionEur, 9);
                Assert.Equal(p.JahresueberschussEur, p.ZielfunktionEur, 9);
            }
        }

        /// <summary>
        /// Mit der Option aus Fachkonzept 5.4 ist es <b>dJ − K_ver</b>. Der
        /// Jahresüberschuss OHNE Verschleißterm steht daneben — damit der
        /// Ergebnisblock zeigen kann, was die Option kostet.
        /// </summary>
        [Fact]
        public void Mit_der_Option_gehen_die_Verschleisskosten_ab()
        {
            var e = Suchraum();
            e.KVerInZielfunktion = true;

            OptimiererErgebnis roh = StromspeicherSimCtrl.FuehreOptimierungAus(
                Vorbereitung(), SpeicherOptimierungCtrl.Optionen(e), null, CancellationToken.None);

            foreach (OptimiererPunkt p in Alle(roh.Grobraster))
            {
                Assert.Equal(p.JahresueberschussEur - p.VerschleisskostenEurProA, p.ZielfunktionEur, 9);
                Assert.True(p.VerschleisskostenEurProA > 0.0);
            }
        }

        /// <summary>
        /// Die Investition ist <c>c_cap·C + c_pow·P + I_fix</c> und die Leistung
        /// <c>P = r·C</c> — die zwei Bausteine der Formel, je Rasterpunkt.
        /// </summary>
        [Fact]
        public void Investition_und_Leistung_folgen_der_Formel()
        {
            SpeicherParameter b = Basis();

            OptimiererErgebnis roh = StromspeicherSimCtrl.FuehreOptimierungAus(
                Vorbereitung(), SpeicherOptimierungCtrl.Optionen(Suchraum()), null, CancellationToken.None);

            foreach (OptimiererPunkt p in Alle(roh.Grobraster))
            {
                Assert.Equal(p.CRate * p.CNomKwh, p.PKw, 9);
                Assert.Equal(
                    b.CCapEurProKwh * p.CNomKwh + b.CPowEurProKw * p.PKw + b.IFixEur,
                    p.InvestitionEur, 9);

                // Zins 0 => a = 1/N.
                Assert.Equal(p.InvestitionEur / b.NutzungsdauerA, p.AnnuitaetEur, 9);
            }
        }

        /// <summary>
        /// Der Controller REICHT die Zahlen durch. Was in den Kennzahlen steht, ist der
        /// Bestpunkt der Engine — Wert für Wert, in der Kultur der Oberfläche.
        /// </summary>
        [Fact]
        public void Die_Kennzahlen_tragen_die_Zahlen_des_Bestpunkts()
        {
            OptimiererErgebnis roh = StromspeicherSimCtrl.FuehreOptimierungAus(
                Vorbereitung(), SpeicherOptimierungCtrl.Optionen(Suchraum()), null, CancellationToken.None);

            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Auswerten(roh);
            OptimiererPunkt best = roh.BestPunkt;

            Assert.True(dto.Erfolg);
            Assert.Equal(best.CNomKwh, dto.KapazitaetKwh);
            Assert.Equal(best.CRate, dto.CRate);
            Assert.Equal(best.PKw, dto.LeistungKw);
            Assert.Equal(best.ZielfunktionEur, dto.ZielfunktionEur);

            Assert.Equal(best.CNomKwh.ToString("0.#", CultureInfo.CurrentCulture),
                         Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_KAPAZITAET));
            Assert.Equal(best.ZielfunktionEur.ToString("0.00", CultureInfo.CurrentCulture),
                         Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_ZIELFUNKTION));
            Assert.Equal(best.InvestitionEur.ToString("0.00", CultureInfo.CurrentCulture),
                         Wert(dto, WindowsFormsApplication1.MyResource.Resource.OPT_KZ_INVEST));

            // Drei Gruppen, alle belegt.
            Assert.Contains(dto.Kennzahlen, z => z.Gruppe == SpeicherOptimierungCtrl.GRUPPE_AUSLEGUNG);
            Assert.Contains(dto.Kennzahlen, z => z.Gruppe == SpeicherOptimierungCtrl.GRUPPE_WIRTSCHAFT);
            Assert.Contains(dto.Kennzahlen, z => z.Gruppe == SpeicherOptimierungCtrl.GRUPPE_SPEICHER);
        }

        /// <summary>
        /// Ein negativer Wert meldet sich als solcher — das war in der Maske die
        /// Firebrick-Färbung der Zahlenspalte (<c>ZahlFaerben</c>-Muster).
        /// </summary>
        [Fact]
        public void Ein_negativer_Wert_ist_gekennzeichnet()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            // Der Mini-Fall traegt einen Kapitaldienst, den vier Viertelstunden nicht
            // erwirtschaften - die Zielfunktion ist negativ.
            Assert.True(dto.ZielfunktionEur < 0.0);
            Assert.True(dto.Kennzahlen.Single(
                z => z.Bezeichnung == WindowsFormsApplication1.MyResource.Resource.OPT_KZ_ZIELFUNKTION).Negativ);
        }

        // =================================================================
        // Randlage
        // =================================================================

        /// <summary>
        /// „Optimum am Rand — Suchbereich erweitern" (Fachkonzept 6.3). Im Mini-Fall
        /// liegt der Bestpunkt auf der KLEINSTEN Kapazität: Jede weitere kWh kostet
        /// Kapitaldienst, den vier Viertelstunden nicht einbringen.
        /// </summary>
        [Fact]
        public void Ein_Optimum_am_Rand_bekommt_seinen_Hinweis()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            Assert.True(dto.Randlage);
            Assert.Contains(dto.Hinweise, h => h.StartsWith(
                WindowsFormsApplication1.MyResource.Resource.OPT_WARN_RAND.Split('{')[0], StringComparison.Ordinal));
        }

        /// <summary>
        /// Der zweite Hinweis: <c>c_pow = 0</c> macht die C-Raten-Achse kostenneutral,
        /// das Optimum wandert dann zwangsläufig an ihre obere Grenze.
        /// </summary>
        [Fact]
        public void Eine_kostenneutrale_C_Raten_Achse_bekommt_ihren_Hinweis()
        {
            var vorbereitung = Vorbereitung();
            vorbereitung.Basis = Basis() with { CPowEurProKw = 0.0 };

            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                vorbereitung, Suchraum(), null, CancellationToken.None);

            Assert.True(dto.Erfolg);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_WARN_CPOW, dto.Hinweise);
        }

        // =================================================================
        // Abbruch
        // =================================================================

        [Fact]
        public void Eine_gesetzte_Abbruchmarke_liefert_kein_Raster()
        {
            using (var quelle = new CancellationTokenSource())
            {
                quelle.Cancel();

                SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                    Vorbereitung(), Suchraum(), null, quelle.Token);

                Assert.False(dto.Erfolg);
                Assert.True(dto.Abgebrochen);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_STATUS_ABGEBROCHEN, dto.Meldung);
                Assert.Null(dto.RasterBild);
                Assert.Empty(dto.Kennzahlen);
            }
        }

        /// <summary>
        /// Ein Abbruch MITTEN im Lauf: Nach <c>n</c> gemeldeten Punkten wird die Marke
        /// gesetzt. Ein halbes Raster wäre weder als Karte noch als Bestpunkt brauchbar
        /// — es kommt deshalb keines zurück.
        /// </summary>
        [Fact]
        public void Ein_Abbruch_nach_n_Punkten_liefert_kein_Teilergebnis()
        {
            var e = new SpeicherOptimierungEingaben
            {
                CMinKwh = 5.0,
                CMaxKwh = 50.0,
                Stuetzstellen = 20,
                RMin = 0.5,
                RMax = 3.0,
                RSchritt = 0.5,
                Feinraster = true
            };

            using (var quelle = new CancellationTokenSource())
            {
                int gemeldet = 0;
                var melder = new Melder(f =>
                {
                    gemeldet++;
                    quelle.Cancel();
                });

                SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                    Vorbereitung(), e, melder, quelle.Token);

                Assert.True(gemeldet > 0);
                Assert.False(dto.Erfolg);
                Assert.True(dto.Abgebrochen);
            }
        }

        // =================================================================
        // Die Drossel
        // =================================================================

        /// <summary>
        /// Die Engine meldet je Rasterpunkt aus ihrem <c>Parallel.For</c> heraus. Bei
        /// 120 Punkten in 0,3 s sind das 400 Meldungen je Sekunde, und jede einzelne
        /// kostete in der abgelösten Maske einen Zeichenlauf des Bedienfadens. Der
        /// Controller lässt höchstens jede zehnte durch.
        /// </summary>
        [Fact]
        public void Der_Fortschritt_kommt_gedrosselt_an()
        {
            var e = new SpeicherOptimierungEingaben
            {
                CMinKwh = 5.0,
                CMaxKwh = 50.0,
                Stuetzstellen = 20,
                RMin = 0.5,
                RMax = 3.0,
                RSchritt = 0.5,
                Feinraster = true
            };

            int punkte = SpeicherOptimierungCtrl.Punktzahl(e);   // 2 x 20 x 6 = 240
            var staende = new List<SpeicherOptimierungFortschritt>();

            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                Vorbereitung(), e, new Melder(f => { lock (staende) staende.Add(f); }),
                CancellationToken.None);

            Assert.True(dto.Erfolg);
            Assert.NotEmpty(staende);

            // Deutlich weniger Meldungen als Punkte - die genaue Zahl haengt an der
            // Uhr und darf es auch (100 ms ODER 10 Punkte).
            Assert.True(staende.Count < punkte,
                "Es kamen " + staende.Count + " Meldungen bei " + punkte + " Punkten an.");

            // Sie laufen VORWAERTS und nennen die Gesamtzahl - beides gilt auch bei
            // Meldungen aus mehreren Faeden.
            Assert.All(staende, f => Assert.Equal(punkte, f.Gesamt));
            for (int i = 1; i < staende.Count; i++)
                Assert.True(staende[i].Erledigt > staende[i - 1].Erledigt);
        }

        [Fact]
        public void Der_Fortschrittstext_nennt_Punkt_und_Gesamtzahl()
        {
            var stand = new SpeicherOptimierungFortschritt { Erledigt = 42, Gesamt = 120 };

            Assert.Equal(0.35, stand.Anteil!.Value, 9);
            Assert.Contains("42", stand.Text, StringComparison.Ordinal);
            Assert.Contains("120", stand.Text, StringComparison.Ordinal);

            var fein = new SpeicherOptimierungFortschritt { Erledigt = 90, Gesamt = 120, IstFeinraster = true };
            Assert.NotEqual(stand.Text, fein.Text);
        }

        // =================================================================
        // Bilder und CSV
        // =================================================================

        [Fact]
        public void Ein_Lauf_liefert_beide_Bilder_im_festgelegten_Mass()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            Assert.True(dto.Erfolg);
            Assert.Equal((860, 560), Mass(dto.RasterBild));
            Assert.Equal((720, 460), Mass(dto.SchnittBild));
        }

        [Fact]
        public void Die_Bilder_entstehen_deterministisch()
        {
            byte[] a = Lauf().RasterBild;
            byte[] b = Lauf().RasterBild;
            Assert.Equal(a, b);
        }

        /// <summary>
        /// Das Bild darf an einem nicht endlichen Wert NICHT scheitern. Genau daran
        /// starb die ScottPlot-Fassung, und zwar beim RENDERN im Anstrich des
        /// Steuerelements — also unfangbar („min must be a real number").
        /// </summary>
        [Fact]
        public void Ein_nicht_endlicher_Rasterwert_bringt_das_Bild_nicht_zu_Fall()
        {
            double[] cRaten = { 0.5, 1.0 };
            double[] kapazitaeten = { 100.0, 200.0 };
            double[][] werte =
            {
                new[] { 1.0, double.PositiveInfinity },
                new[] { double.NaN, -2.0 }
            };

            byte[] png = ChartRenderer.Optimierungsraster(
                "Raster", "C-Rate", "Kapazität", "ΔJ", cRaten, kapazitaeten, werte, 1, 1);

            Assert.Equal((860, 560), Mass(png));
        }

        [Fact]
        public void Ein_leeres_Raster_liefert_ein_Bild_mit_Hinweis()
        {
            byte[] png = ChartRenderer.Optimierungsraster(
                "Raster", "C-Rate", "Kapazität", "ΔJ",
                new double[0], new double[0], new double[0][], -1, -1);

            Assert.Equal((860, 560), Mass(png));
        }

        [Fact]
        public void Die_CSV_traegt_je_Rasterpunkt_eine_Zeile()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            string[] zeilen = dto.RasterCsv
                .Split('\n')
                .Select(z => z.TrimEnd('\r'))
                .Where(z => z.Length > 0)
                .ToArray();

            // Kopfzeile + 2 Kapazitaeten x 1 C-Rate (kein Feinraster).
            Assert.Equal(3, zeilen.Length);
            Assert.StartsWith(WindowsFormsApplication1.MyResource.Resource.OPT_CSV_PHASE, zeilen[0], StringComparison.Ordinal);
            // 21 Spalten wie bisher, dazu die fuenf der Lastspitzenkappung
            // (Anwenderentscheid W11b‑E‑3, 10.09.2026) — sie stehen IMMER in der Datei.
            Assert.All(zeilen, z => Assert.Equal(25, z.Count(c => c == ';')));

            // Dezimalkomma der Kultur - so oeffnet die Datei in deutschem Excel richtig.
            Assert.Contains(",", zeilen[1], StringComparison.Ordinal);
        }

        // =================================================================
        // Statuszeile
        // =================================================================

        [Fact]
        public void Die_Statuszeile_nennt_Punktzahl_und_Optimum()
        {
            SpeicherOptimierungErgebnis dto = Lauf();

            Assert.Equal(2, dto.PunkteGerechnet);
            Assert.Contains("2", dto.Statuszeile, StringComparison.Ordinal);
            Assert.Contains(dto.KapazitaetKwh.ToString("0.#", CultureInfo.CurrentCulture),
                            dto.Statuszeile, StringComparison.Ordinal);
            Assert.Contains(dto.CRate.ToString("0.##", CultureInfo.CurrentCulture),
                            dto.SchnittTitel, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Ergebnis_meldet_die_Auswertung_und_wirft_nicht()
        {
            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Auswerten(null);

            Assert.False(dto.Erfolg);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS, dto.Meldung);
        }

        [Fact]
        public void Ohne_Vorbereitung_meldet_der_Lauf_und_wirft_nicht()
        {
            SpeicherOptimierungErgebnis dto = SpeicherOptimierungCtrl.Rechnen(
                null, Suchraum(), null, CancellationToken.None);

            Assert.False(dto.Erfolg);
            Assert.NotEmpty(dto.Meldung);
        }

        [Fact]
        public void Die_drei_Strategien_stehen_in_der_Reihenfolge_der_Klappliste()
        {
            var namen = SpeicherOptimierungCtrl.Strategien();

            // Der dritte Eintrag kam mit dem Anwenderentscheid W11b‑E‑3 (10.09.2026)
            // dazu; die Reihenfolge ist der Zahlenwert von OptimiererStrategie.
            Assert.Equal(3, namen.Count);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG, namen[0]);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG, namen[1]);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_LASTSPITZENKAPPUNG, namen[2]);
        }

        // =================================================================
        // Hilfen
        // =================================================================

        private static IEnumerable<OptimiererPunkt> Alle(OptimiererRaster r)
        {
            for (int i = 0; i < r.Zeilen; i++)
                for (int s = 0; s < r.Spalten; s++) yield return r.Punkte[i][s];
        }

        private static string Wert(SpeicherOptimierungErgebnis dto, string bezeichnung)
            => dto.Kennzahlen.Single(z => z.Bezeichnung == bezeichnung).Wert;

        private static (int Breite, int Hoehe) Mass(byte[] png)
        {
            using (var bild = SKBitmap.Decode(png)) return (bild.Width, bild.Height);
        }

        /// <summary>Ein <see cref="IProgress{T}"/>, der einen Rückruf ausführt.</summary>
        private sealed class Melder : IProgress<SpeicherOptimierungFortschritt>
        {
            private readonly Action<SpeicherOptimierungFortschritt> _tun;

            internal Melder(Action<SpeicherOptimierungFortschritt> tun) { _tun = tun; }

            public void Report(SpeicherOptimierungFortschritt wert) => _tun(wert);
        }
    }
}
