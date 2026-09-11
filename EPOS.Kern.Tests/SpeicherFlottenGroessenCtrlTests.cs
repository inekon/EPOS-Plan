using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #193 (Paket P4 des Konzepts „Stromspeicher-Dialoge", Abschnitt 2.5): die
    /// RASTERDATEN der Flotte — Achsen, Wertematrix, Schraffurmatrix, Optimum-Stelle —
    /// und die zwei SCHNITTE daraus.
    ///
    /// <para><b>Warum das im Kern steht und hier geprüft wird.</b> Aus einer
    /// Kandidatenliste eine Karte zu machen ist eine Fachaufgabe: welche Werte eine
    /// Achse bilden, wann zwei Kandidaten auf derselben Stelle stehen, welcher von
    /// beiden sie besetzt und was ein Loch ist. Eine bunit-Probe sähe davon nur das
    /// fertige PNG.</para>
    ///
    /// <para><b>Der Prüfstand ist synthetisch</b>: ein 3 × 2-Raster aus
    /// <see cref="FlottenKandidatZusammenfassung"/>, von Hand gesetzt, ohne
    /// Rechenlauf und ohne Datenbank. Genau so kommen die Löcher, die doppelt
    /// besetzten Stellen und der unzulässige Kandidat gezielt ins Bild.</para>
    /// </summary>
    public sealed class SpeicherFlottenGroessenCtrlTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherFlottenGroessenCtrlTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        // =================================================================
        //  Achsen und Matrix
        // =================================================================

        /// <summary>
        /// Die Achsen sind die vorkommenden Werte, aufsteigend; die Matrix trägt den
        /// Kapitalwert je Stelle. Die NULLVARIANTE steht nicht darin — ohne Kapazität
        /// gibt es keine C-Rate und damit keinen Platz auf der Karte.
        /// </summary>
        [Fact]
        public void Die_Karte_traegt_die_Achsen_aus_den_Werten_und_nicht_die_Nullvariante()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(Ergebnis());

            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, raster.KapazitaetenKwh);
            Assert.Equal(new[] { 0.5, 1.0 }, raster.CRaten);
            Assert.Equal(1000.0, raster.Werte[0][0], 9);
            Assert.Equal(2000.0, raster.Werte[1][1], 9);
            Assert.False(raster.IstLeer);
        }

        /// <summary>
        /// Eine Stelle, an der KEIN Kandidat gerechnet wurde, ist <c>double.NaN</c> —
        /// nicht null. Der Renderer überspringt sie damit in der Farbskala, und eine
        /// Null wäre die Aussage „genauso gut wie die Nullvariante".
        /// </summary>
        [Fact]
        public void Ein_Loch_im_Raster_ist_NaN_und_keine_Null()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(Ergebnis());

            // 30 kWh bei 1,0 C ist im Prüfstand nicht gerechnet.
            Assert.True(double.IsNaN(raster.Werte[2][1]));
            Assert.False(raster.Unzulaessig[2][1]);
        }

        /// <summary>
        /// Ein unzulässiger Kandidat wird SCHRAFFIERT; sein Wert bleibt trotzdem in der
        /// Matrix — die Karte zeigt beides.
        /// </summary>
        [Fact]
        public void Ein_unzulaessiger_Kandidat_wird_schraffiert()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(Ergebnis());

            Assert.True(raster.Unzulaessig[2][0]);        // 30 kWh, 0,5 C
            Assert.Equal(4000.0, raster.Werte[2][0], 9);
            Assert.False(raster.Unzulaessig[0][0]);
        }

        /// <summary>
        /// Stehen zwei Kandidaten auf derselben Stelle — dieselbe Hardware mit zwei
        /// Betriebszielen —, besetzt der bessere sie: erst Zulässigkeit, dann
        /// Kapitalwert (Spezifikation 9.4).
        /// </summary>
        [Fact]
        public void Bei_zwei_Kandidaten_auf_einer_Stelle_gewinnt_der_bessere()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(Ergebnis());

            // 10 kWh / 1,0 C steht zweimal da: PeakShaving 1 500 (zulässig) und
            // PvGreedy 9 999 (UNzulässig). Der zulässige gewinnt trotz kleinerem Wert.
            Assert.Equal(1500.0, raster.Werte[0][1], 9);
            Assert.False(raster.Unzulaessig[0][1]);
        }

        /// <summary>Die Marke des Optimums steht auf der Stelle des besten Kandidaten.</summary>
        [Fact]
        public void Das_Optimum_steht_auf_seiner_Stelle()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis);

            Assert.Equal(1, raster.BesteZeile);
            Assert.Equal(1, raster.BesteSpalte);
        }

        /// <summary>Ohne besten Kandidaten (Nullvariante gewinnt) gibt es keine Marke.</summary>
        [Fact]
        public void Ohne_besten_Kandidaten_gibt_es_keine_Marke()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            ergebnis.BesterKandidat = null;
            ergebnis.NullvarianteGewonnen = true;

            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis);

            Assert.Equal(-1, raster.BesteZeile);
            Assert.Equal(-1, raster.BesteSpalte);
        }

        /// <summary>Ohne Ergebnis und ohne Kandidaten steht die leere Karte da — kein <c>null</c>.</summary>
        [Fact]
        public void Ohne_Kandidaten_ist_die_Karte_leer()
        {
            Assert.True(SpeicherFlottenAnzeigeCtrl.Rasterdaten(null).IstLeer);
            Assert.True(SpeicherFlottenAnzeigeCtrl.Rasterdaten(new FlottenAuslegungErgebnis()).IstLeer);
            Assert.Null(SpeicherFlottenAnzeigeCtrl.Rasterbild(null));
        }

        // =================================================================
        //  Summe der Flotte gegen die einzelne Einheit
        // =================================================================

        /// <summary>
        /// Mit <c>einheit = -1</c> bilden die SUMMEN die Achsen, mit <c>einheit = 0</c>
        /// die Größen der ERSTEN Einheit (Konzept 2.5: „je Einheit wählbar, bei Anzahl
        /// &gt; 1 die Summe"). Im Prüfstand hat die Flotte zwei gleich große Einheiten —
        /// die Achse halbiert sich.
        /// </summary>
        [Fact]
        public void Je_Einheit_halbiert_sich_die_Achse_der_Zweierflotte()
        {
            FlottenAuslegungErgebnis ergebnis = Zweierflotte();

            FlottenRasterdaten summe = SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis);
            FlottenRasterdaten erste = SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis, 0);
            FlottenRasterdaten zweite = SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis, 1);

            Assert.Equal(new[] { 20.0, 40.0 }, summe.KapazitaetenKwh);
            Assert.Equal(new[] { 10.0, 20.0 }, erste.KapazitaetenKwh);
            Assert.Equal(erste.KapazitaetenKwh, zweite.KapazitaetenKwh);

            // Die C-Rate ist eine VERHAELTNISgröße und bleibt deshalb dieselbe.
            Assert.Equal(summe.CRaten, erste.CRaten);
            Assert.Equal(summe.Werte[0][0], erste.Werte[0][0], 9);
        }

        /// <summary>
        /// Eine Einheit, die es bei diesem Kandidaten nicht gibt, lässt ihn aus der
        /// Karte fallen — statt eine erfundene Größe zu zeigen.
        /// </summary>
        [Fact]
        public void Eine_nicht_vorhandene_Einheit_faellt_aus_der_Karte()
        {
            Assert.True(SpeicherFlottenAnzeigeCtrl.Rasterdaten(Zweierflotte(), 2).IstLeer);
        }

        // =================================================================
        //  Die zwei Schnitte
        // =================================================================

        /// <summary>
        /// Der Schnitt bei fester C-Rate liest eine SPALTE der Matrix — über der
        /// Kapazität, samt der Marke auf dem besten endlichen Wert.
        /// </summary>
        [Fact]
        public void Der_Schnitt_bei_fester_CRate_liest_die_Spalte()
        {
            FlottenSchnittdaten schnitt = SpeicherFlottenAnzeigeCtrl.Schnittdaten(Ergebnis(), 0.5);

            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, schnitt.Achse);
            Assert.Equal(new[] { 1000.0, 500.0, 4000.0 }, schnitt.Werte);
            Assert.Equal(30.0, schnitt.OptimumAchse, 9);
            Assert.Equal(4000.0, schnitt.OptimumWert, 9);
            Assert.False(schnitt.IstLeer);
        }

        /// <summary>
        /// Der Schnitt über der LEISTUNG liest eine ZEILE und rechnet ihre Achse als
        /// <c>P = E · C</c> — dieselben Werte, andere Achse.
        /// </summary>
        [Fact]
        public void Der_Schnitt_ueber_der_Leistung_rechnet_die_Achse_aus_der_CRate()
        {
            FlottenSchnittdaten schnitt =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenLeistung(Ergebnis(), 20.0);

            Assert.Equal(new[] { 10.0, 20.0 }, schnitt.Achse);     // 20 kWh · 0,5 und · 1,0
            Assert.Equal(new[] { 500.0, 2000.0 }, schnitt.Werte);
            Assert.Equal(20.0, schnitt.OptimumAchse, 9);
            Assert.Equal(2000.0, schnitt.OptimumWert, 9);
        }

        /// <summary>
        /// Ein Wert, der nicht auf der Achse liegt, liefert den LEEREN Schnitt — und
        /// keine Kurve, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Ein_Wert_neben_der_Achse_liefert_keinen_Schnitt()
        {
            Assert.True(SpeicherFlottenAnzeigeCtrl.Schnittdaten(Ergebnis(), 0.7).IstLeer);
            Assert.True(SpeicherFlottenAnzeigeCtrl.SchnittdatenLeistung(Ergebnis(), 25.0).IstLeer);
            Assert.Null(SpeicherFlottenAnzeigeCtrl.Schnittbild(Ergebnis(), 0.7));
            Assert.Null(SpeicherFlottenAnzeigeCtrl.SchnittbildLeistung(Ergebnis(), 25.0));
        }

        /// <summary>
        /// Ein Loch in der Kurve bleibt <c>NaN</c> und zieht die Marke nicht auf sich —
        /// die Zeile 30 kWh hat bei 1,0 C keinen Kandidaten.
        /// </summary>
        [Fact]
        public void Ein_Loch_in_der_Kurve_bleibt_NaN()
        {
            FlottenSchnittdaten schnitt =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenLeistung(Ergebnis(), 30.0);

            Assert.True(double.IsNaN(schnitt.Werte[1]));
            Assert.Equal(15.0, schnitt.OptimumAchse, 9);     // 30 kWh · 0,5
            Assert.Equal(4000.0, schnitt.OptimumWert, 9);
        }

        // =================================================================
        //  Die drei Bilder
        // =================================================================

        /// <summary>
        /// Die drei Bilder entstehen als PNG und sind DETERMINISTISCH — zweimal
        /// zeichnen ergibt dieselben Bytes. Dieselbe Prüfung fahren die ChartProben.
        /// </summary>
        [Fact]
        public void Die_drei_Bilder_entstehen_und_sind_deterministisch()
        {
            FlottenAuslegungErgebnis e = Ergebnis();

            byte[] raster = SpeicherFlottenAnzeigeCtrl.Rasterbild(e);
            byte[] schnitt = SpeicherFlottenAnzeigeCtrl.Schnittbild(e, 0.5);
            byte[] leistung = SpeicherFlottenAnzeigeCtrl.SchnittbildLeistung(e, 20.0);

            Assert.NotNull(raster);
            Assert.NotNull(schnitt);
            Assert.NotNull(leistung);
            Assert.Equal(raster, SpeicherFlottenAnzeigeCtrl.Rasterbild(e));
            Assert.Equal(schnitt, SpeicherFlottenAnzeigeCtrl.Schnittbild(e, 0.5));
        }

        /// <summary>
        /// Die SCHRAFFUR kommt beim Renderer an: Dasselbe Raster ohne unzulässigen
        /// Kandidaten ergibt ein ANDERES Bild. Ohne diese Gegenprobe bestünde ein
        /// stillschweigend übergangener Parameter jede Maß- und Farbprüfung.
        /// </summary>
        [Fact]
        public void Die_Schraffur_veraendert_das_Bild()
        {
            FlottenAuslegungErgebnis mit = Ergebnis();
            FlottenAuslegungErgebnis ohne = Ergebnis();
            foreach (FlottenKandidatZusammenfassung k in ohne.Kandidaten) k.Zulaessig = true;

            Assert.NotEqual(SpeicherFlottenAnzeigeCtrl.Rasterbild(mit),
                            SpeicherFlottenAnzeigeCtrl.Rasterbild(ohne));
        }

        // =================================================================
        //  Die Kandidatentabelle
        // =================================================================

        /// <summary>
        /// Das Filterprofil trägt die zwölf Spalten der Kandidatentabelle; die
        /// Zahlenspalten stehen rechtsbündig und tragen einen Trichter, das Kennzeichen
        /// „zulässig" nur den Sortierpfeil (Konzept_Katalogfilter 5.6.2).
        /// </summary>
        [Fact]
        public void Das_Filterprofil_nennt_zwoelf_Spalten()
        {
            Katalogfilterprofil profil = SpeicherFlottenAnzeigeCtrl.Kandidatenprofil();

            Assert.Equal(12, profil.Spalten.Count);
            Assert.Equal("FLOTTE_KANDIDATEN", profil.Schluessel);

            Katalogspalte zulaessig = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_ZULAESSIG);
            Assert.Equal(Katalogspaltenart.JaNein, zulaessig.Art);
            Assert.True(zulaessig.Sortierbar);
            Assert.False(zulaessig.Filterbar);

            Katalogspalte spitze = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_SPITZE);
            Assert.Equal(Katalogspaltenart.Zahl, spitze.Art);
            Assert.True(spitze.Rechtsbuendig);
            Assert.True(spitze.Filterbar);
        }

        /// <summary>
        /// Die Zeilen tragen jeden Kandidaten in der Reihenfolge der Suche, der
        /// Schlüssel ist die Kandidatenkennung, und die Zahlen stehen als ZAHL daneben —
        /// sonst sortierte die Spalte „9" vor „10".
        /// </summary>
        [Fact]
        public void Die_Kandidatenzeilen_tragen_Schluessel_und_Zahlen()
        {
            FlottenAuslegungErgebnis e = Ergebnis();
            IReadOnlyList<Katalogfilterzeile> zeilen =
                SpeicherFlottenAnzeigeCtrl.Kandidatenzeilen(e);

            Assert.Equal(e.Kandidaten.Count, zeilen.Count);
            Assert.Equal(e.Kandidaten[0].KandidatId, zeilen[0].Schluessel);

            Katalogfilterzeile zeile = zeilen.Single(z => z.Schluessel == "K-20-1,0");
            Assert.Equal(20.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_KAPAZITAET));
            Assert.Equal(20.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_ENTLADEN));
            Assert.Equal(1.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_CRATE));
            Assert.Equal(2000.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_KAPITALWERT));
            Assert.Equal(11.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_VOLLZYKLEN));
            Assert.Equal(Resource.ALLG_BTN_JA,
                         zeile.Text(SpeicherFlottenAnzeigeCtrl.SP_ZULAESSIG));
        }

        /// <summary>
        /// Ein Kandidat, den die Rechnung nicht bewerten konnte, trägt
        /// <c>-∞</c> als Kapitalwert. In der Tabelle steht dafür der Leerwert und keine
        /// Zahl — sonst sortierte „unendlich schlecht" als Wert mit.
        /// </summary>
        [Fact]
        public void Ein_nicht_bewerteter_Kandidat_zeigt_keinen_Kapitalwert()
        {
            FlottenAuslegungErgebnis e = Ergebnis();
            e.Kandidaten.Add(new FlottenKandidatZusammenfassung
            {
                KandidatId = "K-Fehler",
                KapitalwertEuro = double.NegativeInfinity,
                KapazitaetKWh = 50,
                EntladeleistungKw = 25,
                Grund = "Prüfstand"
            });

            Katalogfilterzeile zeile = SpeicherFlottenAnzeigeCtrl.Kandidatenzeilen(e)
                .Single(z => z.Schluessel == "K-Fehler");

            Assert.Null(zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_KAPITALWERT));
            Assert.Equal("Prüfstand", zeile.Text(SpeicherFlottenAnzeigeCtrl.SP_GRUND));
        }

        /// <summary>
        /// Der Katalogfilter arbeitet auf diesen Zeilen wie auf jedem Katalog: Der
        /// Zahlenausdruck einer Spalte schränkt ein, die Sortierung dreht die Liste.
        /// Geprüft wird die NAHT, nicht der Filter — der ist in
        /// <c>KatalogspaltenfilterTests</c> geprüft.
        /// </summary>
        [Fact]
        public void Der_Katalogfilter_greift_auf_den_Kandidatenzeilen()
        {
            Katalogfilterprofil profil = SpeicherFlottenAnzeigeCtrl.Kandidatenprofil();
            IReadOnlyList<Katalogfilterzeile> alle =
                SpeicherFlottenAnzeigeCtrl.Kandidatenzeilen(Ergebnis());

            var stand = new Katalogfilterstand();
            stand.Setzen(SpeicherFlottenAnzeigeCtrl.SP_KAPAZITAET, ">15");
            IReadOnlyList<Katalogfilterzeile> gefiltert =
                Katalogfilter.Anwenden(profil, alle, stand);
            Assert.Equal(3, gefiltert.Count);
            Assert.All(gefiltert, z => Assert.True(
                z.Zahl(SpeicherFlottenAnzeigeCtrl.SP_KAPAZITAET) > 15.0));

            stand.Sortieren(SpeicherFlottenAnzeigeCtrl.SP_KAPITALWERT);
            stand.Sortieren(SpeicherFlottenAnzeigeCtrl.SP_KAPITALWERT);   // auf -> ab
            IReadOnlyList<Katalogfilterzeile> sortiert =
                Katalogfilter.Anwenden(profil, alle, stand);
            Assert.Equal(4000.0, sortiert[0].Zahl(SpeicherFlottenAnzeigeCtrl.SP_KAPITALWERT));
        }

        // ================================================================= Prüfstand

        /// <summary>
        /// Sechs Kandidaten auf einem 3 × 2-Raster (10/20/30 kWh × 0,5/1,0 C):
        /// die Nullvariante, vier belegte Stellen, eine doppelt besetzte, ein
        /// unzulässiger Kandidat und ein Loch bei 30 kWh / 1,0 C.
        /// </summary>
        private static FlottenAuslegungErgebnis Ergebnis()
        {
            var bester = Kandidat("K-20-1,0", 20, 20, 2000, true, 220);
            var ergebnis = new FlottenAuslegungErgebnis
            {
                Aussage = "Beste Variante im geprueften endlichen Raster",
                Kandidaten = new List<FlottenKandidatZusammenfassung>
                {
                    new() { KandidatId = "Nullvariante-ohne-Zusatzspeicher", Zulaessig = true },
                    Kandidat("K-10-0,5", 10, 5, 1000, true, 60),
                    Kandidat("K-10-1,0", 10, 10, 1500, true, 90),
                    // DIESELBE Stelle, anderes Betriebsziel - und unzulaessig.
                    Kandidat("K-10-1,0-PV", 10, 10, 9999, false, 0,
                             FlottenBetriebsziel.PvGreedy),
                    Kandidat("K-20-0,5", 20, 10, 500, true, 120),
                    bester,
                    Kandidat("K-30-0,5", 30, 15, 4000, false, 300)
                },
                BesterKandidat = bester
            };
            return ergebnis;
        }

        /// <summary>Vier Kandidaten mit je ZWEI gleich großen Einheiten.</summary>
        private static FlottenAuslegungErgebnis Zweierflotte()
        {
            var kandidaten = new List<FlottenKandidatZusammenfassung>();
            foreach (double kapazitaet in new[] { 10.0, 20.0 })
                foreach (double rate in new[] { 0.5, 1.0 })
                {
                    var k = Kandidat($"Z-{kapazitaet}-{rate}", 2 * kapazitaet,
                                     2 * kapazitaet * rate, 100 * kapazitaet * rate, true, 10);
                    k.Einheiten = new List<FlottenKandidatEinheit>
                    {
                        Teil("A", kapazitaet, rate),
                        Teil("B", kapazitaet, rate)
                    };
                    kandidaten.Add(k);
                }
            return new FlottenAuslegungErgebnis { Kandidaten = kandidaten };
        }

        private static FlottenKandidatEinheit Teil(string id, double kapazitaet, double rate) => new()
        {
            Id = id,
            KapazitaetKWh = kapazitaet,
            LadeleistungKw = kapazitaet * rate,
            EntladeleistungKw = kapazitaet * rate
        };

        private static FlottenKandidatZusammenfassung Kandidat(
            string id, double kapazitaet, double entladen, double kapitalwert,
            bool zulaessig, double durchsatz,
            FlottenBetriebsziel ziel = FlottenBetriebsziel.PeakShaving) => new()
        {
            KandidatId = id,
            Betriebsziel = ziel,
            Zulaessig = zulaessig,
            KapitalwertEuro = kapitalwert,
            KapazitaetKWh = kapazitaet,
            LadeleistungKw = entladen,
            EntladeleistungKw = entladen,
            DurchsatzKWh = durchsatz,
            Vollzyklen = kapazitaet > 0 ? durchsatz / kapazitaet : 0,
            BezugsspitzeKw = 16.74,
            ErsparnisEuroJahr = 180,
            Arbeitslos = durchsatz <= 0,
            Grund = zulaessig ? null : "Prüfstand: unzulässig",
            Einheiten = new List<FlottenKandidatEinheit>
            {
                new()
                {
                    Id = "A",
                    KapazitaetKWh = kapazitaet,
                    LadeleistungKw = entladen,
                    EntladeleistungKw = entladen
                }
            }
        };
    }
}
