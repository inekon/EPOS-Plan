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

            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, raster.Zeilenwerte);
            Assert.Equal(new[] { 0.5, 1.0 }, raster.Spaltenwerte);
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

            Assert.Equal(new[] { 20.0, 40.0 }, summe.Zeilenwerte);
            Assert.Equal(new[] { 10.0, 20.0 }, erste.Zeilenwerte);
            Assert.Equal(erste.Zeilenwerte, zweite.Zeilenwerte);

            // Die Spalte traegt die ENTLADELEISTUNG und halbiert sich mit: Beide Achsen
            // der Karte sind absolute Groessen, keine Verhaeltnisse.
            Assert.Equal(new[] { 10.0, 20.0, 40.0 }, summe.Spaltenwerte);
            Assert.Equal(new[] { 5.0, 10.0, 20.0 }, erste.Spaltenwerte);

            // Dieselbe Zelle, derselbe Kandidat: unten links steht der kleinste Speicher.
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
            FlottenSchnittdaten schnitt = SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiSpalte(Ergebnis(), 0.5);

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
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(Ergebnis(), 20.0);

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
            Assert.True(SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiSpalte(Ergebnis(), 0.7).IstLeer);
            Assert.True(SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(Ergebnis(), 25.0).IstLeer);
            Assert.Null(SpeicherFlottenAnzeigeCtrl.SchnittbildBeiSpalte(Ergebnis(), 0.7));
            Assert.Null(SpeicherFlottenAnzeigeCtrl.SchnittbildBeiZeile(Ergebnis(), 25.0));
        }

        /// <summary>
        /// Ein Loch in der Kurve bleibt <c>NaN</c> und zieht die Marke nicht auf sich —
        /// die Zeile 30 kWh hat bei 1,0 C keinen Kandidaten.
        /// </summary>
        [Fact]
        public void Ein_Loch_in_der_Kurve_bleibt_NaN()
        {
            FlottenSchnittdaten schnitt =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(Ergebnis(), 30.0);

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
            byte[] schnitt = SpeicherFlottenAnzeigeCtrl.SchnittbildBeiSpalte(e, 0.5);
            byte[] leistung = SpeicherFlottenAnzeigeCtrl.SchnittbildBeiZeile(e, 20.0);

            Assert.NotNull(raster);
            Assert.NotNull(schnitt);
            Assert.NotNull(leistung);
            Assert.Equal(raster, SpeicherFlottenAnzeigeCtrl.Rasterbild(e));
            Assert.Equal(schnitt, SpeicherFlottenAnzeigeCtrl.SchnittbildBeiSpalte(e, 0.5));
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
        //  Die Achsen folgen der Größenkopplung (Auftrag #226)
        // =================================================================

        /// <summary>
        /// Im Modus <c>KapazitaetUndLeistung</c> tragen die ZEILEN die Kapazität und die
        /// SPALTEN die Entladeleistung — beide Achsen genau das eingegebene Raster, und
        /// die Matrix ist LÜCKENLOS.
        /// </summary>
        /// <remarks>
        /// Das ist der Befund des Anwenders vom 11.09.2026 in Zahlen: 13 × 13 = 169
        /// Kandidaten auf einem Kapazität × Leistung-Gitter (20…500 in Schritten von 40).
        /// </remarks>
        [Fact]
        public void Im_Modus_Kapazitaet_und_Leistung_traegt_die_Spalte_die_Leistung()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(Gitter169());

            Assert.Equal(FlottenAuslegungsmodus.KapazitaetUndLeistung, raster.Modus);
            Assert.Equal(Flottenachsengroesse.Kapazitaet, raster.Zeilengroesse);
            Assert.Equal(Flottenachsengroesse.Leistung, raster.Spaltengroesse);

            Assert.Equal(Stuetzstellen(), raster.Zeilenwerte);
            Assert.Equal(Stuetzstellen(), raster.Spaltenwerte);

            // KEIN Loch: Jede der 169 Stellen trägt ihren Kandidaten.
            int loecher = raster.Werte.Sum(zeile => zeile.Count(double.IsNaN));
            Assert.Equal(0, loecher);

            // Das Optimum steht auf 220 kWh (Zeile 5) und 140 kW (Spalte 3).
            Assert.Equal(5, raster.BesteZeile);
            Assert.Equal(3, raster.BesteSpalte);
        }

        /// <summary>
        /// DIE GEGENPROBE zum Befund: Dieselben 169 Kandidaten als Kapazität × C-Rate
        /// gelesen zerfallen in eine krumme Spaltenachse mit überwiegend Löchern — genau
        /// das Bild, das der Anwender gemeldet hat.
        /// </summary>
        [Fact]
        public void Dieselben_Kandidaten_als_CRate_gelesen_zerfallen_in_Loecher()
        {
            FlottenAuslegungErgebnis falsch = Gitter169();
            falsch.Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndCRate;

            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(falsch);

            Assert.Equal(13, raster.Zeilenwerte.Count);
            Assert.True(raster.Spaltenwerte.Count > 13,
                "Die C-Raten der 169 Kandidaten sind kein Gitter.");

            int stellen = raster.Zeilenwerte.Count * raster.Spaltenwerte.Count;
            int loecher = raster.Werte.Sum(zeile => zeile.Count(double.IsNaN));
            Assert.True(loecher > stellen / 2,
                $"Erwartet überwiegend Löcher, gezählt {loecher} von {stellen}.");
        }

        /// <summary>
        /// Im Modus <c>LeistungUndCRate</c> tragen die ZEILEN die Entladeleistung und die
        /// SPALTEN die C-Rate; die Kapazität folgt als <c>E = P / C</c>.
        /// </summary>
        [Fact]
        public void Im_Modus_Leistung_und_CRate_traegt_die_Zeile_die_Leistung()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(LeistungUndCRate());

            Assert.Equal(Flottenachsengroesse.Leistung, raster.Zeilengroesse);
            Assert.Equal(Flottenachsengroesse.CRate, raster.Spaltengroesse);
            Assert.Equal(new[] { 5.0, 10.0 }, raster.Zeilenwerte);
            Assert.Equal(new[] { 0.5, 1.0 }, raster.Spaltenwerte);
            Assert.Equal(0, raster.Werte.Sum(zeile => zeile.Count(double.IsNaN)));
        }

        /// <summary>
        /// Der Modus <c>KapazitaetUndCRate</c> bleibt der Stand vor #226 — Zeilen
        /// Kapazität, Spalten C-Rate.
        /// </summary>
        [Fact]
        public void Der_CRaten_Modus_bleibt_wie_bisher()
        {
            FlottenAuslegungErgebnis e = Ergebnis();
            e.Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndCRate;
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(e);

            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, raster.Zeilenwerte);
            Assert.Equal(new[] { 0.5, 1.0 }, raster.Spaltenwerte);
            Assert.Equal(Flottenachsengroesse.Kapazitaet, raster.Zeilengroesse);
            Assert.Equal(Flottenachsengroesse.CRate, raster.Spaltengroesse);
        }

        /// <summary>
        /// Die ZWEI SCHNITTE folgen dem Modus: Im Kapazität × Leistung-Gitter läuft der
        /// erste über der Kapazität (eine Leistung festgehalten), der zweite über der
        /// Leistung (eine Kapazität festgehalten) — OHNE Umrechnung, die Spalten SIND
        /// die Leistungen.
        /// </summary>
        [Fact]
        public void Die_Schnitte_folgen_der_Kopplung_Kapazitaet_und_Leistung()
        {
            FlottenAuslegungErgebnis e = Gitter169();

            FlottenSchnittdaten ueberKapazitaet =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiSpalte(e, 140.0);
            Assert.Equal(Stuetzstellen(), ueberKapazitaet.Achse);
            Assert.Equal(220.0, ueberKapazitaet.OptimumAchse, 9);

            FlottenSchnittdaten ueberLeistung =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(e, 220.0);
            Assert.Equal(Stuetzstellen(), ueberLeistung.Achse);
            Assert.Equal(140.0, ueberLeistung.OptimumAchse, 9);
        }

        /// <summary>
        /// Im Modus <c>LeistungUndCRate</c> läuft der erste Schnitt über der Leistung,
        /// der zweite über der KAPAZITÄT — und deren Achse <c>E = P / C</c> fällt über
        /// der aufsteigenden C-Rate; sie wird deshalb aufsteigend gelegt.
        /// </summary>
        [Fact]
        public void Die_Schnitte_folgen_der_Kopplung_Leistung_und_CRate()
        {
            FlottenAuslegungErgebnis e = LeistungUndCRate();

            FlottenSchnittdaten ueberLeistung =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiSpalte(e, 0.5);
            Assert.Equal(new[] { 5.0, 10.0 }, ueberLeistung.Achse);
            Assert.Equal(new[] { 100.0, 300.0 }, ueberLeistung.Werte);

            // 10 kW bei 1,0 C sind 10 kWh, bei 0,5 C sind es 20 kWh - die Achse faellt
            // ueber der aufsteigenden C-Rate und wird deshalb umgelegt.
            FlottenSchnittdaten ueberKapazitaet =
                SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(e, 10.0);
            Assert.Equal(new[] { 10.0, 20.0 }, ueberKapazitaet.Achse);
            Assert.Equal(new[] { 400.0, 300.0 }, ueberKapazitaet.Werte);
            Assert.Equal(10.0, ueberKapazitaet.OptimumAchse, 9);
        }

        /// <summary>
        /// Die Bilder eines Modus sind ANDERE als die des anderen — ohne diese
        /// Gegenprobe bestünde ein stillschweigend übergangener Achsenmodus jede
        /// Maß- und Determinismusprüfung.
        /// </summary>
        [Fact]
        public void Ein_anderer_Achsenmodus_ergibt_ein_anderes_Bild()
        {
            FlottenAuslegungErgebnis nachLeistung = Gitter169();
            FlottenAuslegungErgebnis nachCRate = Gitter169();
            nachCRate.Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndCRate;

            Assert.NotEqual(SpeicherFlottenAnzeigeCtrl.Rasterbild(nachLeistung),
                            SpeicherFlottenAnzeigeCtrl.Rasterbild(nachCRate));
        }

        // =================================================================
        //  Grobraster in der Gesamtheit, Feinraster im Ausschnitt (Auftrag #255)
        // =================================================================

        /// <summary>
        /// DER ANWENDERENTSCHEID VOM 13.09.2026: Die Karte trägt NUR die Stützstellen des
        /// Grobrasters. Bis dahin bekam jeder Feinpunkt eine eigene Zeile, in der nur die
        /// eine Spalte des Feinrasters belegt war — im Modus Kapazität × Leistung stand
        /// damit die halbe Karte weiß da.
        /// </summary>
        [Fact]
        public void Die_Karte_traegt_nur_die_Stuetzstellen_des_Grobrasters()
        {
            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(MitFeinraster());

            Assert.Equal(new[] { 100.0, 200.0, 300.0 }, raster.Zeilenwerte);
            Assert.Equal(new[] { 100.0, 200.0 }, raster.Spaltenwerte);

            // Kein Loch: Jede Stelle des eingegebenen Rasters ist gerechnet.
            Assert.All(raster.Werte, zeile => Assert.All(zeile, w => Assert.True(double.IsFinite(w))));
        }

        /// <summary>
        /// Hat ein FEINPUNKT den Lauf gewonnen, markiert die Karte die Stelle des
        /// GROB-Optimums: Der Feinpunkt liegt zwischen den Stützstellen und hat im
        /// Grobgitter keine Stelle. Die Fußzeile des Bildes sagt, wo er steht.
        /// </summary>
        [Fact]
        public void Hat_ein_Feinpunkt_gewonnen_markiert_die_Karte_das_Grob_Optimum()
        {
            FlottenAuslegungErgebnis e = MitFeinraster();
            Assert.Equal(FlottenKandidatPhase.Fein, e.BesterKandidat.Phase);

            FlottenKandidatZusammenfassung grob = SpeicherFlottenAnzeigeCtrl.GrobOptimum(e);
            Assert.Equal("F-200-200", grob.KandidatId);

            FlottenRasterdaten raster = SpeicherFlottenAnzeigeCtrl.Rasterdaten(e);
            Assert.Equal(1, raster.BesteZeile);     // 200 kWh
            Assert.Equal(1, raster.BesteSpalte);    // 200 kW
        }

        /// <summary>
        /// Das GROB-OPTIMUM ist der beste ZULÄSSIGE Kandidat der ersten Phase — die
        /// Rangfolge des <c>FlottenOptimierer</c>. Die Nullvariante steht nicht im Raster
        /// und zählt hier nicht mit.
        /// </summary>
        [Fact]
        public void Das_Grob_Optimum_ist_der_beste_zulaessige_Grobpunkt()
        {
            Assert.Null(SpeicherFlottenAnzeigeCtrl.GrobOptimum(null));
            Assert.Null(SpeicherFlottenAnzeigeCtrl.GrobOptimum(new FlottenAuslegungErgebnis()));

            // Der Prüfstand ohne Feinraster: Bester zulässiger ist K-20-1,0 mit 2000 €;
            // K-30-0,5 trägt 4000 €, ist aber unzulässig.
            Assert.Equal("K-20-1,0", SpeicherFlottenAnzeigeCtrl.GrobOptimum(Ergebnis()).KandidatId);
        }

        /// <summary>
        /// Die zwei GESAMTSCHNITTE führen keine Feinpunkte mehr: Sie entstehen aus der
        /// Karte, und die trägt nur noch das Grobraster.
        /// </summary>
        [Fact]
        public void Die_Gesamtschnitte_fuehren_keine_Feinpunkte()
        {
            FlottenAuslegungErgebnis e = MitFeinraster();

            FlottenSchnittdaten spalte = SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiSpalte(e, 200.0);
            FlottenSchnittdaten zeile = SpeicherFlottenAnzeigeCtrl.SchnittdatenBeiZeile(e, 200.0);

            Assert.Equal(new[] { 100.0, 200.0, 300.0 }, spalte.Achse);
            Assert.Null(spalte.Feinpunkte);
            Assert.Equal(new[] { 100.0, 200.0 }, zeile.Achse);
            Assert.Null(zeile.Feinpunkte);
        }

        /// <summary>
        /// DER AUSSCHNITT: die Punkte des Feinrasters samt den Grobpunkten desselben
        /// festgehaltenen Wertes im Fenster — aufsteigend, ohne Doppelung, und die drei
        /// Stellen, an denen Grob und Fein zusammenfallen, bleiben GROB (das Feinraster
        /// gewinnt nur bei strikt besserem Kapitalwert).
        /// </summary>
        [Fact]
        public void Der_Ausschnitt_zeigt_das_Fenster_des_Feinrasters()
        {
            FlottenSchnittdaten a = SpeicherFlottenAnzeigeCtrl.Ausschnittdaten(MitFeinraster());

            Assert.False(a.IstLeer);
            Assert.Equal(19, a.Achse.Count);
            Assert.Equal(100.0, a.Achse[0], 6);
            Assert.Equal(300.0, a.Achse[18], 6);
            for (int i = 1; i < a.Achse.Count; i++) Assert.True(a.Achse[i] > a.Achse[i - 1]);

            Assert.NotNull(a.Feinpunkte);
            Assert.Equal(16, a.Feinpunkte.Count(x => x));
            Assert.False(a.Feinpunkte[0]);      // 100 kWh — Grobpunkt am Fensterrand
            Assert.False(a.Feinpunkte[9]);      // 200 kWh — das Grob-Optimum in der Mitte
            Assert.False(a.Feinpunkte[18]);     // 300 kWh — Grobpunkt am Fensterrand
        }

        /// <summary>
        /// Die MARKE des Ausschnitts steht auf dem besten Ergebnis DES LAUFS — dem
        /// Feinpunkt, der das Grob-Optimum geschlagen hat.
        /// </summary>
        [Fact]
        public void Der_Ausschnitt_markiert_das_beste_Ergebnis_des_Laufs()
        {
            FlottenAuslegungErgebnis e = MitFeinraster();
            FlottenSchnittdaten a = SpeicherFlottenAnzeigeCtrl.Ausschnittdaten(e);

            Assert.Equal(e.BesterKandidat.KapazitaetKWh, a.OptimumAchse, 6);
            Assert.Equal(e.BesterKandidat.KapitalwertEuro, a.OptimumWert, 6);

            // Die Marke liegt auf einem Feinpunkt, und der Wert steht auch in der Kurve.
            int stelle = a.Achse.ToList().FindIndex(x => Math.Abs(x - a.OptimumAchse) < 1e-9);
            Assert.True(a.Feinpunkte[stelle]);
            Assert.Equal(a.OptimumWert, a.Werte[stelle], 6);
        }

        /// <summary>
        /// OHNE ZWEITE PHASE gibt es keinen Ausschnitt: abgeschaltetes Feinraster, ein
        /// Lauf ohne Feinkandidaten, kein Ergebnis — überall der leere Schnitt und kein
        /// Bild. Unter der Suchmethode „Stückzahl" rechnet der Optimierer gar keines.
        /// </summary>
        [Fact]
        public void Ohne_Feinraster_gibt_es_keinen_Ausschnitt()
        {
            Assert.True(SpeicherFlottenAnzeigeCtrl.Ausschnittdaten(null).IstLeer);
            Assert.True(SpeicherFlottenAnzeigeCtrl.Ausschnittdaten(Ergebnis()).IstLeer);
            Assert.Null(SpeicherFlottenAnzeigeCtrl.Ausschnittbild(Ergebnis()));
            Assert.Equal("", SpeicherFlottenAnzeigeCtrl.Ausschnitttitel(Ergebnis()));
            Assert.Equal("", SpeicherFlottenAnzeigeCtrl.Ausschnittbeschreibung(Ergebnis()));

            // Die Marke allein macht keinen Ausschnitt: Ohne Feinkandidaten bleibt er leer
            // (so steht ein Stueckzahllauf da, der nie eine zweite Phase faehrt).
            FlottenAuslegungErgebnis behauptet = Ergebnis();
            behauptet.FeinrasterGerechnet = true;
            Assert.True(SpeicherFlottenAnzeigeCtrl.Ausschnittdaten(behauptet).IstLeer);
        }

        /// <summary>
        /// Titel und Begleitsatz nennen Achse, festgehaltenen Wert, Fenster, Schrittweite
        /// und das beste Ergebnis — die Zahlen, die ein Anwender aus der Kurve abliest.
        /// </summary>
        [Fact]
        public void Titel_und_Begleitsatz_des_Ausschnitts_nennen_ihre_Zahlen()
        {
            FlottenAuslegungErgebnis e = MitFeinraster();

            string titel = SpeicherFlottenAnzeigeCtrl.Ausschnitttitel(e);
            Assert.StartsWith("Ausschnitt um das Optimum", titel, StringComparison.Ordinal);
            Assert.Contains("Kapazität", titel, StringComparison.Ordinal);
            Assert.Contains("200 kW", titel, StringComparison.Ordinal);

            string satz = SpeicherFlottenAnzeigeCtrl.Ausschnittbeschreibung(e);
            Assert.Contains("19", satz, StringComparison.Ordinal);
            Assert.Contains("100 kWh", satz, StringComparison.Ordinal);
            Assert.Contains("300 kWh", satz, StringComparison.Ordinal);
            Assert.Contains("11,1 kWh", satz, StringComparison.Ordinal);   // ein Neuntel von 100
        }

        /// <summary>
        /// Das Bild entsteht, ist deterministisch und trägt die Marken der zweiten Phase:
        /// Dieselbe Kurve ohne Feinpunkte sähe anders aus.
        /// </summary>
        [Fact]
        public void Das_Ausschnittbild_entsteht_und_ist_deterministisch()
        {
            FlottenAuslegungErgebnis e = MitFeinraster();

            byte[] bild = SpeicherFlottenAnzeigeCtrl.Ausschnittbild(e);
            Assert.NotNull(bild);
            Assert.True(bild.Length > 1000);
            Assert.Equal(bild, SpeicherFlottenAnzeigeCtrl.Ausschnittbild(e));

            // Es ist ein ANDERES Bild als die Karte und als der Gesamtschnitt daneben.
            Assert.NotEqual(bild, SpeicherFlottenAnzeigeCtrl.SchnittbildBeiSpalte(e, 200.0));
        }

        /// <summary>
        /// Die FUSSZEILE der Karte sagt es, wenn das beste Ergebnis aus dem Feinraster
        /// stammt — sonst läse sich die markierte Stelle als das Beste des Laufs. Prüfbar
        /// ist das am Bild: Derselbe Lauf mit einem Grob-Sieger sieht anders aus.
        /// </summary>
        [Fact]
        public void Die_Fusszeile_der_Karte_nennt_den_Feinpunkt()
        {
            FlottenAuslegungErgebnis mitFein = MitFeinraster();
            FlottenAuslegungErgebnis mitGrob = MitFeinraster();
            mitGrob.BesterKandidat = SpeicherFlottenAnzeigeCtrl.GrobOptimum(mitGrob);

            Assert.NotEqual(SpeicherFlottenAnzeigeCtrl.Rasterbild(mitFein),
                            SpeicherFlottenAnzeigeCtrl.Rasterbild(mitGrob));
        }

        // =================================================================
        //  Die Texte je Achsengröße und Kopplung (Auftrag #226)
        // =================================================================

        /// <summary>
        /// Titel, Achsentitel, Schieberbeschriftung und Wertetext kommen je Größe und
        /// Kopplung aus den Ressourcen — die EINE Quelle für Bild und Markup.
        /// </summary>
        [Fact]
        public void Jede_Kopplung_traegt_ihre_eigenen_Texte()
        {
            Assert.Equal(Resource.FLOTTE_GROESSEN_CHART_RASTER,
                SpeicherFlottenAnzeigeCtrl.Rastertitel(FlottenAuslegungsmodus.KapazitaetUndCRate));
            Assert.Equal(Resource.FLOTTE_GROESSEN_CHART_RASTER_KW,
                SpeicherFlottenAnzeigeCtrl.Rastertitel(FlottenAuslegungsmodus.KapazitaetUndLeistung));
            Assert.Equal(Resource.FLOTTE_GROESSEN_CHART_RASTER_KW_C,
                SpeicherFlottenAnzeigeCtrl.Rastertitel(FlottenAuslegungsmodus.LeistungUndCRate));

            Assert.Equal(Resource.FLOTTE_GROESSEN_ACHSE_LEISTUNG,
                SpeicherFlottenAnzeigeCtrl.Achsentext(Flottenachsengroesse.Leistung));
            Assert.Equal(Resource.FLOTTE_GROESSEN_LBL_LEISTUNG,
                SpeicherFlottenAnzeigeCtrl.Schiebertext(Flottenachsengroesse.Leistung));
            Assert.Equal(Resource.FLOTTE_GROESSEN_LBL_KAPAZITAET,
                SpeicherFlottenAnzeigeCtrl.Schiebertext(Flottenachsengroesse.Kapazitaet));

            // Die Einheit steht am Wert, das Zahlenformat richtet sich nach der Größe.
            Assert.Equal("140 kW",
                SpeicherFlottenAnzeigeCtrl.Werttext(Flottenachsengroesse.Leistung, 140.0));
            Assert.Equal("220 kWh",
                SpeicherFlottenAnzeigeCtrl.Werttext(Flottenachsengroesse.Kapazitaet, 220.0));
            Assert.Equal("1,25 C",
                SpeicherFlottenAnzeigeCtrl.Werttext(Flottenachsengroesse.CRate, 1.25));
        }

        /// <summary>
        /// Die vier Schnitt-Überschriften: worüber die Kurve läuft und was festgehalten
        /// ist — je Paar ein eigener Text, auch für die Bildbeschreibung.
        /// </summary>
        [Fact]
        public void Jeder_Schnitt_nennt_seine_Achse_und_den_festgehaltenen_Wert()
        {
            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture,
                              Resource.FLOTTE_GROESSEN_CHART_SCHNITT_KAP_BEI_KW, "140"),
                SpeicherFlottenAnzeigeCtrl.Schnitttitel(
                    Flottenachsengroesse.Kapazitaet, Flottenachsengroesse.Leistung, 140.0));

            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture,
                              Resource.FLOTTE_GROESSEN_CHART_SCHNITT_LEI_BEI_C, "0,5"),
                SpeicherFlottenAnzeigeCtrl.Schnitttitel(
                    Flottenachsengroesse.Leistung, Flottenachsengroesse.CRate, 0.5));

            Assert.Equal(Resource.FLOTTE_GROESSEN_ALT_SCHNITT_KAP_BEI_KW,
                SpeicherFlottenAnzeigeCtrl.Schnittbeschreibung(
                    Flottenachsengroesse.Kapazitaet, Flottenachsengroesse.Leistung));
            Assert.Equal(Resource.FLOTTE_GROESSEN_ALT_SCHNITT_LEISTUNG,
                SpeicherFlottenAnzeigeCtrl.Schnittbeschreibung(
                    Flottenachsengroesse.Leistung, Flottenachsengroesse.Kapazitaet));
            Assert.Equal(Resource.FLOTTE_GROESSEN_ALT_RASTER_KW,
                SpeicherFlottenAnzeigeCtrl.Rasterbeschreibung(
                    FlottenAuslegungsmodus.KapazitaetUndLeistung));
        }

        // =================================================================
        //  Die Kandidatentabelle
        // =================================================================

        /// <summary>
        /// Das Filterprofil trägt die vierzehn Spalten der Kandidatentabelle; die
        /// Zahlenspalten stehen rechtsbündig und tragen einen Trichter, die Kennzeichen
        /// „zulässig" und „neutrale Kennwerte" nur den Sortierpfeil
        /// (Konzept_Katalogfilter 5.6.2).
        /// </summary>
        /// <remarks>
        /// <b>Die ABWEICHUNG ist die dreizehnte, die NEUTRALEN KENNWERTE die vierzehnte
        /// Spalte.</b> Die Größensuche rechnet Geräte; trifft keines den vorgegebenen
        /// Bereich, kommen die nächstliegenden — der Anwender muss sehen, wie weit ein
        /// Gerät von seiner Vorgabe entfernt ist, statt es für einen Treffer zu halten.
        /// </remarks>
        [Fact]
        public void Das_Filterprofil_nennt_vierzehn_Spalten()
        {
            Katalogfilterprofil profil = SpeicherFlottenAnzeigeCtrl.Kandidatenprofil();

            Assert.Equal(14, profil.Spalten.Count);
            Assert.Equal("FLOTTE_KANDIDATEN", profil.Schluessel);

            Katalogspalte zulaessig = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_ZULAESSIG);
            Assert.Equal(Katalogspaltenart.JaNein, zulaessig.Art);
            Assert.True(zulaessig.Sortierbar);
            Assert.False(zulaessig.Filterbar);

            Katalogspalte spitze = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_SPITZE);
            Assert.Equal(Katalogspaltenart.Zahl, spitze.Art);
            Assert.True(spitze.Rechtsbuendig);
            Assert.True(spitze.Filterbar);

            Katalogspalte abweichung = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_ABWEICHUNG);
            Assert.Equal(Katalogspaltenart.Zahl, abweichung.Art);
            Assert.True(abweichung.Rechtsbuendig);

            Katalogspalte neutral = profil.Spalte(SpeicherFlottenAnzeigeCtrl.SP_NEUTRAL);
            Assert.Equal(Katalogspaltenart.JaNein, neutral.Art);
            Assert.True(neutral.Sortierbar);
            Assert.False(neutral.Filterbar);
        }

        /// <summary>
        /// <b>Die Zeile nennt die Abweichung in PROZENT und die neutralen Kennwerte als
        /// Kennzeichen.</b> Eine Zahl zwischen 0 und 1 läse sich als Anteil; der Anwender
        /// vergleicht sie aber mit seiner eigenen Vorgabe.
        /// </summary>
        [Fact]
        public void Die_Kandidatenzeile_nennt_Abweichung_und_neutrale_Kennwerte()
        {
            var ergebnis = new FlottenAuslegungErgebnis
            {
                Kandidaten = new List<FlottenKandidatZusammenfassung>
                {
                    new()
                    {
                        KandidatId = "K", Zulaessig = true, KapazitaetKWh = 100,
                        EntladeleistungKw = 100, Abweichung = 0.125, NeutraleKennwerte = true,
                        Einheiten = new List<FlottenKandidatEinheit>
                        {
                            new() { Id = "A", KapazitaetKWh = 100, EntladeleistungKw = 100 }
                        }
                    }
                }
            };

            Katalogfilterzeile zeile =
                Assert.Single(SpeicherFlottenAnzeigeCtrl.Kandidatenzeilen(ergebnis));

            Assert.Equal(12.5, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_ABWEICHUNG).Value, 6);
            Assert.Equal(1.0, zeile.Zahl(SpeicherFlottenAnzeigeCtrl.SP_NEUTRAL).Value, 6);
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

        // =================================================================
        //  „Kandidat übernehmen" — die Rückabbildung (Auftrag #196)
        // =================================================================

        /// <summary>
        /// Für den BESTEN Kandidaten wird nichts gebaut: Der Optimierer hat dessen
        /// Konfiguration selbst gebildet und legt sie bei — sie wird genommen, damit die
        /// Optimum-Zeile denselben Stand liefert wie „Beste Flotte übernehmen".
        /// </summary>
        [Fact]
        public void Der_beste_Kandidat_liefert_die_Konfiguration_des_Optimierers()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            ergebnis.BesteKonfiguration = new FlottenStudieKonfiguration
            {
                Einheiten = { new FlottenEinheit { Id = "vom-Optimierer", KapazitaetKWh = 42 } }
            };

            FlottenStudieKonfiguration neu = SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(
                ergebnis, Arbeitsstand(), ergebnis.BesterKandidat);

            Assert.Equal("vom-Optimierer", Assert.Single(neu.Einheiten).Id);
            Assert.Equal(42.0, neu.Einheiten[0].KapazitaetKWh, 9);

            // Es ist eine KOPIE: Wer sie ändert, ändert das Laufergebnis nicht.
            neu.Einheiten[0].KapazitaetKWh = 1;
            Assert.Equal(42.0, ergebnis.BesteKonfiguration.Einheiten[0].KapazitaetKWh, 9);
        }

        /// <summary>
        /// Jeder ANDERE Kandidat wird zurückabgebildet: Größen und Betriebsziel kommen
        /// aus dem Kandidaten, alles Übrige — Wirkungsgrade, SoC-Band, Wirtschaftlichkeit —
        /// unverändert aus dem Arbeitsstand.
        /// </summary>
        [Fact]
        public void Ein_anderer_Kandidat_bekommt_seine_Groessen_und_den_Rest_des_Arbeitsstands()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenKandidatZusammenfassung kandidat =
                ergebnis.Kandidaten.Single(x => x.KandidatId == "K-30-0,5");

            FlottenStudieKonfiguration neu = SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(
                ergebnis, Arbeitsstand(), kandidat);

            FlottenEinheit einheit = Assert.Single(neu.Einheiten);
            Assert.Equal("A", einheit.Id);
            Assert.Equal(30.0, einheit.KapazitaetKWh, 9);
            Assert.Equal(15.0, einheit.EntladeleistungKw, 9);

            // Der Rest der Einheit stammt aus dem Arbeitsstand.
            Assert.Equal("Speicher A", einheit.Name);
            Assert.Equal(0.97, einheit.Ladewirkungsgrad, 9);
            Assert.Equal(0.15, einheit.SocMin, 9);

            // Und der Rest der Konfiguration ebenso.
            Assert.Equal(FlottenBetriebsziel.PeakShaving, neu.Optionen.Betriebsziel);
            Assert.Equal(7, neu.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        }

        /// <summary>
        /// Das BETRIEBSZIEL des Kandidaten schlägt das des Arbeitsstands — die
        /// Rastersuche fährt je Ziel eigene Kandidaten, und wer einen davon übernimmt,
        /// übernimmt sein Ziel mit.
        /// </summary>
        [Fact]
        public void Das_Betriebsziel_kommt_aus_dem_Kandidaten()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenKandidatZusammenfassung kandidat =
                ergebnis.Kandidaten.Single(x => x.KandidatId == "K-10-1,0-PV");

            FlottenStudieKonfiguration neu = SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(
                ergebnis, Arbeitsstand(), kandidat);

            Assert.Equal(FlottenBetriebsziel.PvGreedy, neu.Optionen.Betriebsziel);
        }

        /// <summary>
        /// Die Wirtschaftlichkeitsliste zieht mit: Sie trägt Investition, Betrieb, Ersatz
        /// und Restwert. Bliebe sie stehen, rechnete der nächste Lauf die Kosten der
        /// ALTEN Größen.
        /// </summary>
        [Fact]
        public void Die_Wirtschaftlichkeitsliste_traegt_die_neuen_Groessen()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenKandidatZusammenfassung kandidat =
                ergebnis.Kandidaten.Single(x => x.KandidatId == "K-30-0,5");

            FlottenStudieKonfiguration neu = SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(
                ergebnis, Arbeitsstand(), kandidat);

            Assert.Equal(30.0, Assert.Single(neu.Wirtschaftlichkeit.Einheiten).KapazitaetKWh, 9);
        }

        /// <summary>
        /// Eine Einheit, die der Arbeitsstand NICHT kennt (die Rastersuche erzeugt sie aus
        /// der Achsenvorlage), bekommt genau diese Vorlage — und ihre Kennung aus dem
        /// Kandidaten.
        /// </summary>
        [Fact]
        public void Eine_erzeugte_Einheit_nimmt_die_Vorlage_der_Suchachse()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenKandidatZusammenfassung kandidat =
                ergebnis.Kandidaten.Single(x => x.KandidatId == "K-20-0,5");
            kandidat.Einheiten[0].Id = "A-A1-N1";

            FlottenStudieKonfiguration stand = Arbeitsstand();
            stand.Auslegung.Achsen.Add(new FlottenAuslegungsAchse
            {
                Aktiv = true,
                Vorlage = new FlottenEinheit
                {
                    Id = "A", Name = "Vorlage", Ladewirkungsgrad = 0.5, SocMin = 0.25
                }
            });

            FlottenStudieKonfiguration neu =
                SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(ergebnis, stand, kandidat);

            FlottenEinheit einheit = Assert.Single(neu.Einheiten);
            Assert.Equal("A-A1-N1", einheit.Id);
            Assert.Equal("Vorlage", einheit.Name);
            Assert.Equal(0.5, einheit.Ladewirkungsgrad, 9);
            Assert.Equal(0.25, einheit.SocMin, 9);
            Assert.Equal(20.0, einheit.KapazitaetKWh, 9);
        }

        /// <summary>
        /// <b>Ein übernommener Kandidat bringt die Kennwerte SEINES Geräts mit.</b> Unter
        /// „Größe suchen" führt jeder Kandidat ein eigenes Gerät; käme die Vorlage aus der
        /// Achse, übernähme der Anwender ein Gerät mit den Kennwerten eines anderen —
        /// dieselben Zahlen im Bild, andere im nächsten Lauf.
        /// </summary>
        [Fact]
        public void Ein_uebernommener_Kandidat_traegt_die_Kennwerte_seines_Geraets()
        {
            FlottenAuslegungErgebnis ergebnis = Ergebnis();
            FlottenKandidatZusammenfassung kandidat =
                ergebnis.Kandidaten.Single(x => x.KandidatId == "K-30-0,5");
            kandidat.Quellkennung = "G30";
            kandidat.Einheiten[0].Id = "A-A1-N1";

            FlottenStudieKonfiguration stand = Arbeitsstand();
            stand.Auslegung.Achsen.Add(new FlottenAuslegungsAchse
            {
                Aktiv = true,
                Vorlage = new FlottenEinheit { Id = "A", Name = "Achsenvorlage",
                                               Ladewirkungsgrad = 0.5, SocMin = 0.25 },
                Geraete =
                {
                    new FlottenGeraetekandidat
                    {
                        Quellkennung = "G30",
                        Geraet = new FlottenEinheit
                        {
                            Id = "G30", Name = "Speicher 30",
                            KapazitaetKWh = 30, LadeleistungKw = 15, EntladeleistungKw = 15,
                            Ladewirkungsgrad = 0.88, Entladewirkungsgrad = 0.87,
                            SocMin = 0.05, SocMax = 0.95
                        }
                    }
                }
            });

            FlottenStudieKonfiguration neu =
                SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(ergebnis, stand, kandidat);

            FlottenEinheit einheit = Assert.Single(neu.Einheiten);
            Assert.Equal("Speicher 30", einheit.Name);
            Assert.Equal(0.88, einheit.Ladewirkungsgrad, 9);      // vom GERAET
            Assert.Equal(0.87, einheit.Entladewirkungsgrad, 9);
            Assert.Equal(0.05, einheit.SocMin, 9);
            Assert.Equal(30.0, einheit.KapazitaetKWh, 9);         // die Groesse des Kandidaten
        }

        /// <summary>Ohne Kandidat gibt es nichts zu übernehmen.</summary>
        [Fact]
        public void Ohne_Kandidat_kommt_keine_Konfiguration()
            => Assert.Null(SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(
                Ergebnis(), Arbeitsstand(), null));

        /// <summary>Der Arbeitsstand, den die Rückabbildung als Vorlage benutzt.</summary>
        private static FlottenStudieKonfiguration Arbeitsstand() => new()
        {
            Einheiten =
            {
                new FlottenEinheit
                {
                    Id = "A", Name = "Speicher A", KapazitaetKWh = 5,
                    LadeleistungKw = 2, EntladeleistungKw = 2,
                    Ladewirkungsgrad = 0.97, Entladewirkungsgrad = 0.96,
                    SocMin = 0.15, SocStart = 0.2, SocMax = 0.95
                }
            },
            Optionen = new FlottenSimulationOptionen { Betriebsziel = FlottenBetriebsziel.MultiUse },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            {
                ProjektjahreBeiWiederholung = 7
            }
        };

        // ================================================================= Prüfstand

        /// <summary>
        /// Die dreizehn Stützstellen des Anwenderfalls: 20…500 in Schritten von 40.
        /// </summary>
        private static double[] Stuetzstellen()
            => Enumerable.Range(0, 13).Select(i => 20.0 + 40.0 * i).ToArray();

        /// <summary>
        /// DER FALL DES ANWENDERS (11.09.2026): eine Einheit mit Größenkopplung
        /// „Kapazität und Leistung", beide Größen 20…500 in Schritten von 40 —
        /// 13 × 13 = 169 Kandidaten auf einem vollen Gitter, das Optimum bei
        /// 220 kWh / 140 kW.
        /// </summary>
        private static FlottenAuslegungErgebnis Gitter169()
        {
            double[] stellen = Stuetzstellen();
            var kandidaten = new List<FlottenKandidatZusammenfassung>();
            FlottenKandidatZusammenfassung bester = null;

            foreach (double kapazitaet in stellen)
                foreach (double leistung in stellen)
                {
                    double wert = 5000.0
                                - (kapazitaet - 220.0) * (kapazitaet - 220.0) / 100.0
                                - (leistung - 140.0) * (leistung - 140.0) / 50.0;
                    var k = Kandidat($"G-{kapazitaet}-{leistung}", kapazitaet, leistung,
                                     wert, true, 10 * kapazitaet);
                    kandidaten.Add(k);
                    if (Math.Abs(kapazitaet - 220.0) < 1e-9 && Math.Abs(leistung - 140.0) < 1e-9)
                        bester = k;
                }

            return new FlottenAuslegungErgebnis
            {
                Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                Kandidaten = kandidaten,
                BesterKandidat = bester
            };
        }

        /// <summary>
        /// Vier Kandidaten auf einem Leistung × C-Rate-Gitter (5/10 kW × 0,5/1,0 C); die
        /// Kapazität folgt als <c>E = P / C</c> und nimmt die Werte 5, 10 und 20 kWh an.
        /// </summary>
        private static FlottenAuslegungErgebnis LeistungUndCRate()
        {
            var kandidaten = new List<FlottenKandidatZusammenfassung>();
            double wert = 0.0;
            foreach (double leistung in new[] { 5.0, 10.0 })
                foreach (double rate in new[] { 0.5, 1.0 })
                {
                    wert += 100.0;
                    kandidaten.Add(Kandidat($"L-{leistung}-{rate}", leistung / rate, leistung,
                                            wert, true, 50));
                }

            return new FlottenAuslegungErgebnis
            {
                Achsenmodus = FlottenAuslegungsmodus.LeistungUndCRate,
                Kandidaten = kandidaten
            };
        }
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
                // DIE ALTE KOPPLUNGSMARKE, AUSGESCHRIEBEN: Ein Ergebnis, das sie noch
                // traegt, soll sich weiterhin zeichnen lassen - Zeilen Kapazitaet,
                // Spalten C-Rate. Die Vorbelegung eines frischen Ergebnisses ist
                // KapazitaetUndLeistung, die einzige Kopplung, die eine Suche erzeugt.
                Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndCRate,
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

        /// <summary>
        /// DER FALL DES ANWENDERS (13.09.2026): ein Grobraster 100/200/300 kWh ×
        /// 100/200 kW und darüber das Feinraster, das der <c>FlottenOptimierer</c> um das
        /// Grob-Optimum (200 kWh / 200 kW) legt — neunzehn Kapazitäten von 100 bis 300 kWh
        /// in Schritten von einem Neuntel der Grobschrittweite, die Leistung festgehalten.
        /// </summary>
        /// <remarks>
        /// Die drei Stellen 100, 200 und 300 kWh kommen in BEIDEN Phasen vor und tragen
        /// dort denselben Kapitalwert — genau so rechnet die Engine, und genau daran
        /// zeigt sich, dass das Feinraster nur bei strikt besserem Wert gewinnt. Das
        /// Optimum des Laufs liegt bei 233,3 kWh und damit ZWISCHEN zwei Stützstellen.
        /// </remarks>
        private static FlottenAuslegungErgebnis MitFeinraster()
        {
            var kandidaten = new List<FlottenKandidatZusammenfassung>
            {
                new() { KandidatId = "Nullvariante-ohne-Zusatzspeicher", Zulaessig = true }
            };

            foreach (double kapazitaet in new[] { 100.0, 200.0, 300.0 })
                foreach (double leistung in new[] { 100.0, 200.0 })
                    kandidaten.Add(Kandidat($"F-{kapazitaet}-{leistung}", kapazitaet, leistung,
                                            Muldenwert(kapazitaet, leistung), true, 10 * kapazitaet));

            FlottenKandidatZusammenfassung bester = null;
            for (int i = 0; i <= 18; i++)
            {
                double kapazitaet = Math.Round(100.0 + i * (100.0 / 9.0), 6);
                var k = Kandidat($"FF-{i}", kapazitaet, 200.0,
                                 Muldenwert(kapazitaet, 200.0), true, 10 * kapazitaet);
                k.Phase = FlottenKandidatPhase.Fein;
                kandidaten.Add(k);
                if (bester is null || k.KapitalwertEuro > bester.KapitalwertEuro) bester = k;
            }

            return new FlottenAuslegungErgebnis
            {
                Achsenmodus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                Kandidaten = kandidaten,
                BesterKandidat = bester,
                FeinrasterGerechnet = true
            };
        }

        /// <summary>Eine Mulde mit dem Scheitel bei 233 kWh / 200 kW — beide Phasen rechnen sie.</summary>
        private static double Muldenwert(double kapazitaet, double leistung)
            => 5000.0
             - (kapazitaet - 233.0) * (kapazitaet - 233.0) / 10.0
             - (leistung - 200.0) * (leistung - 200.0) / 20.0;

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
