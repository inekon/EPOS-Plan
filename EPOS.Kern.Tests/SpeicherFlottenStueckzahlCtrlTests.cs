using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #247 (Anwenderentscheid SD‑E‑10 / SD‑Q17): die ERGEBNISSICHT der Methode
    /// „Stückzahl suchen" — bei EINER variierten Einheit die Kurve „Kapitalwert über
    /// Stückzahl", bei ZWEI die Rasterkarte n₁ × n₂, darüber hinaus nur die
    /// Kandidatentabelle.
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche.</b> Geprüft wird die Rechnung, die aus
    /// der Kandidatenliste Achsen macht: welche Stückzahlen eine Achse bilden, welcher von
    /// zwei Kandidaten auf derselben Stelle sie besetzt (erst Zulässigkeit, dann
    /// Kapitalwert — Spezifikation 9.4) und wo die Marke des Optimums steht.</para>
    /// </summary>
    public class SpeicherFlottenStueckzahlCtrlTests
    {
        // =====================================================================
        //  Wie viele Einheiten variiert der Lauf?
        // =====================================================================

        /// <summary>
        /// Die Zahl der variierten Einheiten kommt aus
        /// <see cref="FlottenKandidatZusammenfassung.Stueckzahlen"/> — sie entscheidet,
        /// welche Sicht die Ansicht zeigt.
        /// </summary>
        [Fact]
        public void Die_Zahl_der_variierten_Einheiten_kommt_aus_den_Kandidaten()
        {
            Assert.Equal(0, SpeicherFlottenAnzeigeCtrl.VariierteEinheiten(null));
            Assert.Equal(0, SpeicherFlottenAnzeigeCtrl.VariierteEinheiten(new FlottenAuslegungErgebnis()));
            Assert.Equal(1, SpeicherFlottenAnzeigeCtrl.VariierteEinheiten(EineEinheit()));
            Assert.Equal(2, SpeicherFlottenAnzeigeCtrl.VariierteEinheiten(ZweiEinheiten()));
        }

        // =====================================================================
        //  Die Kurve
        // =====================================================================

        /// <summary>
        /// Die Kurve führt jede geprüfte Stückzahl genau einmal, aufsteigend, mit ihrem
        /// Kapitalwert — und die Marke steht auf dem besten Kandidaten des Laufs.
        /// </summary>
        [Fact]
        public void Die_Kurve_fuehrt_jede_Stueckzahl_einmal_und_markiert_das_Optimum()
        {
            FlottenAuslegungErgebnis e = EineEinheit();

            FlottenStueckzahlkurve kurve = SpeicherFlottenAnzeigeCtrl.Stueckzahlkurve(e);

            Assert.False(kurve.IstLeer);
            Assert.Equal(new[] { 1, 2, 3 }, kurve.Stueckzahlen);
            Assert.Equal(new[] { 1000.0, 4000.0, 2000.0 }, kurve.Werte);
            Assert.Equal(new[] { false, false, true }, kurve.Unzulaessig);
            Assert.Equal(1, kurve.BesteStelle);                      // die 2 Stück
        }

        /// <summary>
        /// <b>Zwei Kandidaten auf derselben Stückzahl</b> — dieselbe Bestückung mit zwei
        /// Betriebszielen —: Der BESSERE besetzt die Stelle, und ein zulässiger schlägt
        /// einen unzulässigen auch mit kleinerem Kapitalwert.
        /// </summary>
        [Fact]
        public void Auf_derselben_Stueckzahl_gewinnt_der_bessere_Kandidat()
        {
            var e = new FlottenAuslegungErgebnis();
            e.Kandidaten.Add(Kandidat(stueck: 1, wert: 500, zulaessig: true));
            e.Kandidaten.Add(Kandidat(stueck: 1, wert: 900, zulaessig: false));
            e.Kandidaten.Add(Kandidat(stueck: 2, wert: 100, zulaessig: true));
            e.Kandidaten.Add(Kandidat(stueck: 2, wert: 800, zulaessig: true));

            FlottenStueckzahlkurve kurve = SpeicherFlottenAnzeigeCtrl.Stueckzahlkurve(e);

            Assert.Equal(new[] { 1, 2 }, kurve.Stueckzahlen);
            Assert.Equal(new[] { 500.0, 800.0 }, kurve.Werte);
            Assert.All(kurve.Unzulaessig, x => Assert.False(x));
        }

        /// <summary>Ein Lauf ohne Stückzahlachsen hat keine Kurve.</summary>
        [Fact]
        public void Ohne_variierte_Einheit_gibt_es_keine_Kurve()
        {
            Assert.True(SpeicherFlottenAnzeigeCtrl.Stueckzahlkurve(null).IstLeer);
            Assert.True(SpeicherFlottenAnzeigeCtrl.Stueckzahlkurve(ZweiEinheiten()).IstLeer);
        }

        /// <summary>Der Rückweg von einer Säule zum Kandidaten („Kandidat übernehmen").</summary>
        [Fact]
        public void Zu_jeder_Stueckzahl_gibt_es_den_Kandidaten_zurueck()
        {
            FlottenAuslegungErgebnis e = EineEinheit();

            FlottenKandidatZusammenfassung k = SpeicherFlottenAnzeigeCtrl.KandidatZuStueckzahl(e, 2);
            Assert.NotNull(k);
            Assert.Equal(4000.0, k.KapitalwertEuro, 6);
            Assert.Null(SpeicherFlottenAnzeigeCtrl.KandidatZuStueckzahl(e, 9));
        }

        // =====================================================================
        //  Die Karte n1 × n2
        // =====================================================================

        /// <summary>
        /// Zwei variierte Einheiten spannen eine ganzzahlige Karte auf; eine nicht
        /// gerechnete Stelle bleibt ein LOCH (<c>NaN</c>) und nicht der schlechteste Wert
        /// (Regel #226).
        /// </summary>
        [Fact]
        public void Zwei_variierte_Einheiten_ergeben_die_ganzzahlige_Karte()
        {
            FlottenAuslegungErgebnis e = ZweiEinheiten();

            FlottenStueckzahlraster raster = SpeicherFlottenAnzeigeCtrl.Stueckzahlraster(e);

            Assert.False(raster.IstLeer);
            Assert.Equal(new[] { 1, 2 }, raster.Zeilenzahlen);
            Assert.Equal(new[] { 0, 1 }, raster.Spaltenzahlen);
            Assert.Equal(100.0, raster.Werte[0][0], 6);
            Assert.Equal(700.0, raster.Werte[0][1], 6);
            Assert.Equal(300.0, raster.Werte[1][0], 6);
            Assert.True(double.IsNaN(raster.Werte[1][1]));            // nie gerechnet
            Assert.Equal(0, raster.BesteZeile);
            Assert.Equal(1, raster.BesteSpalte);
        }

        /// <summary>Mit einer variierten Einheit gibt es keine Karte — dann steht die Kurve.</summary>
        [Fact]
        public void Mit_einer_variierten_Einheit_gibt_es_keine_Karte()
            => Assert.True(SpeicherFlottenAnzeigeCtrl.Stueckzahlraster(EineEinheit()).IstLeer);

        // =====================================================================
        //  Die zwei Bilder
        // =====================================================================

        /// <summary>
        /// Beide Bilder entstehen aus denselben Daten — und ohne Daten entsteht keines
        /// (<c>null</c> statt eines leeren Rahmens).
        /// </summary>
        [Fact]
        public void Die_zwei_Bilder_entstehen_nur_mit_Daten()
        {
            Assert.NotNull(SpeicherFlottenAnzeigeCtrl.Stueckzahlbild(EineEinheit(), "Growatt"));
            Assert.Null(SpeicherFlottenAnzeigeCtrl.Stueckzahlbild(ZweiEinheiten()));

            Assert.NotNull(SpeicherFlottenAnzeigeCtrl.Stueckzahlrasterbild(ZweiEinheiten(), "A", "B"));
            Assert.Null(SpeicherFlottenAnzeigeCtrl.Stueckzahlrasterbild(EineEinheit()));
        }

        // =====================================================================
        //  Prüfstand
        // =====================================================================

        private static FlottenAuslegungErgebnis EineEinheit()
        {
            var e = new FlottenAuslegungErgebnis();
            e.Kandidaten.Add(Kandidat(1, 1000, true));
            e.Kandidaten.Add(Kandidat(2, 4000, true));
            e.Kandidaten.Add(Kandidat(3, 2000, false));
            e.BesterKandidat = e.Kandidaten[1];
            return e;
        }

        private static FlottenAuslegungErgebnis ZweiEinheiten()
        {
            var e = new FlottenAuslegungErgebnis();
            e.Kandidaten.Add(Paar(1, 0, 100));
            e.Kandidaten.Add(Paar(1, 1, 700));
            e.Kandidaten.Add(Paar(2, 0, 300));
            e.BesterKandidat = e.Kandidaten[1];
            return e;
        }

        private static FlottenKandidatZusammenfassung Kandidat(int stueck, double wert, bool zulaessig)
            => new()
            {
                KandidatId = "n=" + stueck,
                Zulaessig = zulaessig,
                KapitalwertEuro = wert,
                KapazitaetKWh = 20.0 * stueck,
                EntladeleistungKw = 10.0 * stueck,
                Stueckzahlen = new List<int> { stueck },
                Einheiten = Enumerable.Range(0, stueck).Select(i => new FlottenKandidatEinheit
                {
                    Id = "e" + i, KapazitaetKWh = 20.0, LadeleistungKw = 10.0, EntladeleistungKw = 10.0
                }).ToList()
            };

        private static FlottenKandidatZusammenfassung Paar(int n1, int n2, double wert)
            => new()
            {
                KandidatId = n1 + "/" + n2,
                Zulaessig = true,
                KapitalwertEuro = wert,
                KapazitaetKWh = 20.0 * (n1 + n2),
                Stueckzahlen = new List<int> { n1, n2 },
                Einheiten = Enumerable.Range(0, n1 + n2).Select(i => new FlottenKandidatEinheit
                {
                    Id = "e" + i, KapazitaetKWh = 20.0, LadeleistungKw = 10.0, EntladeleistungKw = 10.0
                }).ToList()
            };
    }
}
