using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Auslegungsoptimierung <see cref="SpeicherOptimierer"/>
    /// (AP8, Fachkonzept 6.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wogegen hier geprueft wird.</b> Fuer die Rastersuche gibt es keinen
    /// Excel-Verifikationsanker: Die gespeicherte V7-Heatmap zeigt eine reine
    /// Randloesung bei 5.000 kWh, ihre Zielgroesse war uneindeutig (drei Kandidaten im
    /// Umlauf), und die Zahlen sind mit keinem Datenstand reproduzierbar. Verifiziert
    /// wird deshalb gegen (1) eine vollstaendige <b>Handrechnung</b> auf einer
    /// 4-Intervall-Reihe, (2) die <b>Bereichslogik</b> der Vorlage
    /// <c>speicher_sim.py:optimiere_speicher</c> und (3) <b>Struktureigenschaften</b>,
    /// die unabhaengig von den Zahlen gelten muessen - Determinismus, Unabhaengigkeit
    /// von der Parallelitaet, sauberer Abbruch.
    /// </para>
    /// <para>
    /// <b>Der Handrechnungsfall.</b> 4 Viertelstunden, eta_RT = 1 (verlustfrei, damit
    /// AC- und DC-Seite zusammenfallen), Zins 0 und Degradation 0. Dann gilt
    /// <c>a = 1/N</c> und <c>RBF_deg = N</c>, also <c>E_a,aeq = E_a,1 * N * (1/N) =
    /// E_a,1</c> - die Zielfunktion reduziert sich auf
    /// <c>dJ = Summe(F) - I/N</c> und ist von Hand nachrechenbar. Die Reihe wechselt
    /// zwei Lade- und zwei Entladeintervalle; die Begrenzung liegt bei der kleinen
    /// Auslegung an der Leistung, bei der grossen am Ueberschuss.
    /// </para>
    /// </remarks>
    public sealed class SpeicherOptimiererTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public SpeicherOptimiererTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        private const double Dt = 0.25;
        private const double PreisCtKwh = 20.0;
        private const double VerguetungCtKwh = 5.0;

        // ==================================================================
        // Handrechnungsfall
        // ==================================================================

        /// <summary>
        /// 4 Intervalle: zwei mit 40 kW Ueberschuss, zwei mit 40 kW Defizit -
        /// je 10 kWh Energie im Viertelstundenraster.
        /// </summary>
        private static SpeicherEingang MiniEingang()
        {
            double[] last = { 0.0, 0.0, 40.0, 40.0 };
            double[] pv = { 40.0, 40.0, 0.0, 0.0 };
            double[] preis = { PreisCtKwh, PreisCtKwh, PreisCtKwh, PreisCtKwh };
            return new SpeicherEingang(last, pv, preis);
        }

        /// <summary>
        /// Basisauslegung 10 kWh / 10 kW, Band 0 … 10 kWh, verlustfrei, Zins 0,
        /// N = 10 a. c_cap = 100 EUR/kWh, c_pow = 50 EUR/kW, I_fix = 1.000 EUR.
        /// </summary>
        private static SpeicherParameter MiniBasis() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 1.0,
            DtH = Dt,
            VerguetungCtKwh = VerguetungCtKwh,
            CCapEurProKwh = 100.0,
            CPowEurProKw = 50.0,
            IFixEur = 1000.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 10.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.025
        };

        /// <summary>Zwei Kapazitaeten (10 und 20 kWh), genau eine C-Rate (1,0), kein Feinraster.</summary>
        private static OptimiererOptionen MiniOptionen() => new OptimiererOptionen
        {
            CMinKwh = 10.0,
            CMaxKwh = 20.0,
            Stuetzstellen = 2,
            RMin = 1.0,
            RMax = 1.0,
            RSchritt = 1.0,
            Feinraster = false
        };

        [Fact]
        public void Zielfunktion_Stimmt_Mit_Der_Handrechnung()
        {
            // C = 10 kWh, r = 1 -> P = 10 kW, P*dt = 2,5 kWh je Intervall.
            //   Laden   : 2 * min(10; 2,5; Bandrest) = 2 * 2,5 =  5,0 kWh -> -5,0 * 0,05 = -0,25 EUR
            //   Entladen: 2 * min(10; 2,5; SoC)      = 2 * 2,5 =  5,0 kWh -> +5,0 * 0,20 = +1,00 EUR
            //   Summe(F) = 0,75 EUR ;  I = 100*10 + 50*10 + 1000 = 2.500 EUR ;  A = I/N = 250 EUR/a
            //   dJ = 0,75 - 250 = -249,25 EUR/a
            //
            // C = 20 kWh, r = 1 -> P = 20 kW, P*dt = 5 kWh; das Band skaliert auf 0 … 20 kWh.
            //   Laden 2 * 5 = 10 kWh -> -0,50 ; Entladen 2 * 5 = 10 kWh -> +2,00 ; Summe(F) = 1,50
            //   I = 100*20 + 50*20 + 1000 = 4.000 EUR ; A = 400 EUR/a ; dJ = 1,50 - 400 = -398,50
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            OptimiererPunkt klein = erg.Grobraster.Punkte[0][0];
            OptimiererPunkt gross = erg.Grobraster.Punkte[1][0];

            Assert.Equal(10.0, klein.CNomKwh, 12);
            Assert.Equal(10.0, klein.PKw, 12);
            Assert.Equal(0.75, klein.ErtragReferenzjahrEur, 12);
            Assert.Equal(2500.0, klein.InvestitionEur, 12);
            Assert.Equal(250.0, klein.AnnuitaetEur, 12);
            Assert.Equal(-249.25, klein.ZielfunktionEur, 12);

            Assert.Equal(20.0, gross.CNomKwh, 12);
            Assert.Equal(20.0, gross.PKw, 12);
            Assert.Equal(1.50, gross.ErtragReferenzjahrEur, 12);
            Assert.Equal(4000.0, gross.InvestitionEur, 12);
            Assert.Equal(-398.50, gross.ZielfunktionEur, 12);

            // Maximiert wird dJ - die kleinere Auslegung gewinnt.
            Assert.Equal(10.0, erg.BestPunkt.CNomKwh, 12);
            Assert.Equal(1.0, erg.BestPunkt.CRate, 12);
        }

        [Fact]
        public void Zielfunktion_Ohne_Verschleissoption_Ist_Der_Jahresueberschuss()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            Assert.False(erg.KVerInZielfunktion);
            foreach (OptimiererPunkt[] zeile in erg.Grobraster.Punkte)
                foreach (OptimiererPunkt p in zeile)
                {
                    Assert.Equal(p.JahresueberschussEur, p.ZielfunktionEur, 12);
                    // K_ver wird trotzdem immer ausgewiesen (Fachkonzept 5.4, Verwendung 2).
                    Assert.True(p.VerschleisskostenEurProA > 0.0);
                }
        }

        [Fact]
        public void Verschleissoption_Zieht_K_Ver_Von_Der_Zielfunktion_Ab()
        {
            // n_zyk = E_dc / C_nutz. Bei eta = 1 ist E_dc = E_ac.
            //   C = 10: 5 kWh / 10 kWh = 0,5 -> K_ver = 0,5 * 10 * 0,025 = 0,125 EUR/a
            //   C = 20: 10 kWh / 20 kWh = 0,5 -> K_ver = 0,5 * 20 * 0,025 = 0,250 EUR/a
            OptimiererOptionen mitVerschleiss = MiniOptionen() with { KVerInZielfunktion = true };
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), mitVerschleiss);

            OptimiererPunkt klein = erg.Grobraster.Punkte[0][0];
            OptimiererPunkt gross = erg.Grobraster.Punkte[1][0];

            Assert.True(erg.KVerInZielfunktion);
            Assert.Equal(0.5, klein.AequivalenteVollzyklen, 12);
            Assert.Equal(0.125, klein.VerschleisskostenEurProA, 12);
            Assert.Equal(-249.375, klein.ZielfunktionEur, 12);
            Assert.Equal(-249.25, klein.JahresueberschussEur, 12);

            Assert.Equal(0.250, gross.VerschleisskostenEurProA, 12);
            Assert.Equal(-398.75, gross.ZielfunktionEur, 12);
        }

        [Fact]
        public void Sekundaerkennzahlen_Sind_Belegt()
        {
            // Zyklenhochrechnung n_zyk * N = 0,5 * 10 = 5 Vollzyklen; mit N_zyk = 4
            // ist das Budget gerissen, mit N_zyk = 6 nicht.
            OptimiererErgebnis eng = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen() with { ZyklenZugesichert = 4.0 });
            OptimiererErgebnis weit = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen() with { ZyklenZugesichert = 6.0 });
            OptimiererErgebnis ohne = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            OptimiererPunkt a = eng.Grobraster.Punkte[0][0];
            Assert.Equal(5.0, a.ZyklenNutzungsdauer, 12);
            Assert.True(a.ZyklenbudgetUeberschritten);
            Assert.False(weit.Grobraster.Punkte[0][0].ZyklenbudgetUeberschritten);
            // Ohne gepflegtes N_zyk unterbleibt die Bewertung.
            Assert.False(ohne.Grobraster.Punkte[0][0].ZyklenbudgetUeberschritten);

            // Amortisation ist Sekundaerkennzahl, nie Zielgroesse - aber gefuellt.
            // E_a,aeq = 0,75 EUR/a bei I = 2.500 EUR -> 3.333,33 a; bei i_z = 0 faellt
            // die dynamische Rechnung auf dieselbe Formel zurueck.
            Assert.Equal(AmortisationStatus.Amortisierbar, a.StatischeAmortisation.Status);
            Assert.Equal(2500.0 / 0.75, a.StatischeAmortisation.Jahre, 9);
            Assert.Equal(AmortisationStatus.Amortisierbar, a.DynamischeAmortisation.Status);
            Assert.Equal(2500.0 / 0.75, a.DynamischeAmortisation.Jahre, 9);

            // Energiekennzahlen: 5 kWh geladen, 5 kWh entladen, verlustfrei.
            Assert.Equal(5.0, a.LadeenergieKwh, 12);
            Assert.Equal(5.0, a.EntladeenergieKwh, 12);
            Assert.Equal(0.0, a.SpeicherverlusteKwh, 12);
            Assert.True(a.AutarkiegradMitSpeicher > 0.0);
            Assert.True(a.EigenverbrauchsquoteMitSpeicher > 0.0);
        }

        // ==================================================================
        // SoC-Band-Skalierung
        // ==================================================================

        [Fact]
        public void Rasterpunkt_Skaliert_Das_SoC_Band_Anteilig()
        {
            SpeicherParameter basis = MiniBasis() with
            {
                CNomKwh = 1000.0,
                SoCMinKwh = 100.0,   // 10 %
                SoCMaxKwh = 900.0,   // 90 %
                StartSoCKwh = 250.0  // 25 %
            };

            SpeicherParameter p = SpeicherOptimierer.Rasterpunkt(basis, 4000.0, 1.5);

            Assert.Equal(4000.0, p.CNomKwh, 12);
            Assert.Equal(6000.0, p.PKw, 12);          // r * C
            Assert.Equal(400.0, p.SoCMinKwh, 12);     // weiterhin 10 %
            Assert.Equal(3600.0, p.SoCMaxKwh, 12);    // weiterhin 90 %
            Assert.Equal(1000.0, p.StartSoCKwh!.Value, 12);   // weiterhin 25 %

            // Prozentsaetze bleiben erhalten - die eigentliche Zusage.
            Assert.Equal(basis.SoCMinKwh / basis.CNomKwh, p.SoCMinKwh / p.CNomKwh, 12);
            Assert.Equal(basis.SoCMaxKwh / basis.CNomKwh, p.SoCMaxKwh / p.CNomKwh, 12);

            // Alles Uebrige unveraendert.
            Assert.Equal(basis.RoundTripWirkungsgrad, p.RoundTripWirkungsgrad, 12);
            Assert.Equal(basis.CCapEurProKwh, p.CCapEurProKwh, 12);
            Assert.Equal(basis.CPowEurProKw, p.CPowEurProKw, 12);
            Assert.Equal(basis.Kapitalzins, p.Kapitalzins, 12);
        }

        [Fact]
        public void Rasterpunkt_Behaelt_Den_Offenen_Start_Ladezustand()
        {
            SpeicherParameter basis = MiniBasis();   // StartSoCKwh = null
            SpeicherParameter p = SpeicherOptimierer.Rasterpunkt(basis, 50.0, 2.0);

            Assert.Null(p.StartSoCKwh);
            Assert.Equal(p.SoCMinKwh, p.StartSoCEffektivKwh, 12);
        }

        // ==================================================================
        // Feinraster-Bereichslogik (Vorlage speicher_sim.py)
        // ==================================================================

        [Fact]
        public void Feinrasterbereich_Ist_Ein_Groessenschritt_Um_Das_Optimum()
        {
            // Vorschlagsraum des Fachkonzepts: 500 … 5.000 kWh, 10 Stuetzstellen
            // -> Schritt = 4500 / 9 = 500 kWh.
            OptimiererOptionen opt = new OptimiererOptionen();
            double unten, oben;

            SpeicherOptimierer.FeinrasterBereich(opt, 2000.0, out unten, out oben);
            Assert.Equal(1500.0, unten, 12);
            Assert.Equal(2500.0, oben, 12);
        }

        [Fact]
        public void Feinrasterbereich_Wird_Auf_Den_Suchraum_Geklemmt()
        {
            OptimiererOptionen opt = new OptimiererOptionen();
            double unten, oben;

            // Optimum auf der unteren Kante: kein Ausflug unter C_min.
            SpeicherOptimierer.FeinrasterBereich(opt, 500.0, out unten, out oben);
            Assert.Equal(500.0, unten, 12);
            Assert.Equal(1000.0, oben, 12);

            // Optimum auf der oberen Kante: kein Ausflug ueber C_max.
            SpeicherOptimierer.FeinrasterBereich(opt, 5000.0, out unten, out oben);
            Assert.Equal(4500.0, unten, 12);
            Assert.Equal(5000.0, oben, 12);
        }

        [Fact]
        public void Feinrasterbereich_Haelt_Die_Mindestbreite_Von_Einer_Kilowattstunde()
        {
            // Schritt = 0,4 kWh -> Bereich waere 0,4 kWh breit; die Vorlage weitet ihn
            // auf 1 kWh auf ("if s2_max - s2_min < 1: s2_max = s2_min + 1").
            OptimiererOptionen opt = new OptimiererOptionen
            {
                CMinKwh = 100.0,
                CMaxKwh = 100.4,
                Stuetzstellen = 2
            };

            double unten, oben;
            SpeicherOptimierer.FeinrasterBereich(opt, 100.0, out unten, out oben);

            Assert.Equal(100.0, unten, 12);
            Assert.Equal(101.0, oben, 12);
        }

        [Fact]
        public void Feinraster_Rechnet_Im_Ermittelten_Bereich()
        {
            OptimiererOptionen opt = MiniOptionen() with { Feinraster = true };
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), opt);

            // Grob-Optimum liegt bei C_min = 10 kWh, Schritt = (20-10)/(2-1) = 10
            // -> Feinbereich [max(10; 0) ; min(20; 20)] = [10 ; 20].
            Assert.NotNull(erg.Feinraster);
            Assert.Equal(10.0, erg.Feinraster!.CMinKwh, 12);
            Assert.Equal(20.0, erg.Feinraster.CMaxKwh, 12);
            Assert.False(erg.Grobraster.IstFeinraster);
            Assert.True(erg.Feinraster.IstFeinraster);

            // Gleichstand zwischen den Phasen: das Grobraster bleibt massgeblich
            // (Vorlage: "best2 if best2 > best1 else best1").
            Assert.Same(erg.Grobraster.BestPunkt, erg.BestPunkt);
            Assert.Same(erg.Grobraster, erg.BestRaster);
        }

        [Fact]
        public void Ohne_Feinraster_Bleibt_Die_Zweite_Stufe_Leer()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            Assert.Null(erg.Feinraster);
            Assert.Same(erg.Grobraster, erg.BestRaster);
            Assert.Equal(2, erg.PunkteGerechnet);
        }

        // ==================================================================
        // Randloesungserkennung (Fachkonzept 6.3)
        // ==================================================================

        [Fact]
        public void Randwarnung_Bei_Optimum_Auf_Der_Unteren_Kapazitaetskante()
        {
            // Der Handrechnungsfall: dJ faellt mit der Kapazitaet, das Optimum liegt
            // auf C_min.
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            Assert.Equal(10.0, erg.BestPunkt.CNomKwh, 12);
            Assert.True(erg.Randlage.KapazitaetUnten);
            Assert.False(erg.Randlage.KapazitaetOben);
            Assert.True(erg.Randlage.Vorhanden);
        }

        [Fact]
        public void Randwarnung_Bei_Optimum_Auf_Der_Oberen_Kapazitaetskante()
        {
            // Ohne Investitionskosten waechst dJ streng mit der Kapazitaet, solange
            // ueberhaupt noch Ueberschuss zu speichern ist - das Optimum landet
            // zwangslaeufig auf C_max. Genau diese Konstellation zeigte die
            // gespeicherte V7-Heatmap (Randloesung bei 5.000 kWh).
            SpeicherParameter kostenfrei = MiniBasis() with
            {
                CCapEurProKwh = 0.0,
                CPowEurProKw = 0.0,
                IFixEur = 0.0
            };

            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), kostenfrei, MiniOptionen());

            Assert.Equal(20.0, erg.BestPunkt.CNomKwh, 12);
            Assert.True(erg.Randlage.KapazitaetOben);
            Assert.False(erg.Randlage.KapazitaetUnten);
            Assert.True(erg.Randlage.Vorhanden);
        }

        [Fact]
        public void Inneres_Optimum_Loest_Keine_Randwarnung_Der_Kapazitaetsachse_Aus()
        {
            // Ueberschuss und Defizit sind auf 5 kWh je Intervall begrenzt; ab 20 kWh
            // bringt mehr Kapazitaet keinen Ertrag mehr, kostet aber weiter.
            //   dJ(10) = 0,75 - 0,1*0,5*10 = 0,25
            //   dJ(20) = 1,50 - 0,1*0,5*20 = 0,50   <- Optimum, innen
            //   dJ(30) = 1,50 - 0,1*0,5*30 = 0,00
            double[] last = { 0.0, 0.0, 20.0, 20.0 };
            double[] pv = { 20.0, 20.0, 0.0, 0.0 };
            SpeicherEingang eingang = new SpeicherEingang(
                last, pv, SpeicherEingang.KonstanteReihe(PreisCtKwh, 4));

            SpeicherParameter basis = MiniBasis() with
            {
                CCapEurProKwh = 0.5,
                CPowEurProKw = 0.0,
                IFixEur = 0.0
            };

            OptimiererOptionen opt = MiniOptionen() with { CMinKwh = 10.0, CMaxKwh = 30.0, Stuetzstellen = 3 };
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(eingang, basis, opt);

            Assert.Equal(0.25, erg.Grobraster.Punkte[0][0].ZielfunktionEur, 12);
            Assert.Equal(0.50, erg.Grobraster.Punkte[1][0].ZielfunktionEur, 12);
            Assert.Equal(0.00, erg.Grobraster.Punkte[2][0].ZielfunktionEur, 12);

            Assert.Equal(20.0, erg.BestPunkt.CNomKwh, 12);
            Assert.False(erg.Randlage.KapazitaetUnten);
            Assert.False(erg.Randlage.KapazitaetOben);

            // Die C-Raten-Achse hat hier nur ein Glied und ist damit zwangslaeufig
            // gleichzeitig untere und obere Kante - deshalb wird sie hier nicht
            // geprueft, sondern in Randwarnung_Auf_Der_C_Raten_Achse.
        }

        [Fact]
        public void Randwarnung_Auf_Der_C_Raten_Achse()
        {
            // c_pow = 0: die C-Rate ist kostenneutral, mehr Leistung ist nie schlechter
            // - das Optimum liegt auf r_max.
            SpeicherParameter basis = MiniBasis() with { CPowEurProKw = 0.0 };
            OptimiererOptionen opt = MiniOptionen() with { RMin = 0.5, RMax = 1.5, RSchritt = 0.5 };

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(MiniEingang(), basis, opt);

            Assert.Equal(3, erg.Grobraster.Spalten);
            Assert.Equal(1.5, erg.BestPunkt.CRate, 12);
            Assert.True(erg.Randlage.CRateOben);
            Assert.False(erg.Randlage.CRateUnten);
        }

        // ==================================================================
        // Hinweis c_pow = 0 (Fachkonzept 6.3)
        // ==================================================================

        [Fact]
        public void Hinweis_Wenn_Die_C_Raten_Achse_Kostenneutral_Ist()
        {
            SpeicherParameter ohneLeistungskosten = MiniBasis() with { CPowEurProKw = 0.0 };
            OptimiererErgebnis neutral = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), ohneLeistungskosten, MiniOptionen());

            Assert.True(neutral.CPowNeutral);
        }

        [Fact]
        public void Kein_Hinweis_Wenn_Leistungskosten_Gepflegt_Sind()
        {
            OptimiererErgebnis mitLeistungskosten = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            Assert.False(mitLeistungskosten.CPowNeutral);
        }

        // ==================================================================
        // Determinismus und Parallelitaet
        // ==================================================================

        [Fact]
        public void Zwei_Laeufe_Liefern_Bitgleiche_Ergebnisse()
        {
            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();
            OptimiererOptionen opt = new OptimiererOptionen();

            OptimiererErgebnis a = new SpeicherOptimierer().Optimiere(eingang, basis, opt);
            OptimiererErgebnis b = new SpeicherOptimierer().Optimiere(eingang, basis, opt);

            ErgebnisseSindBitgleich(a, b);
        }

        [Fact]
        public void Parallel_Und_Seriell_Sind_Gleich()
        {
            // Der Kernnachweis der Nebenlaeufigkeit: Ein erzwungen serieller Lauf
            // (MaxDegreeOfParallelism = 1) liefert BITGLEICH dasselbe wie der
            // parallele. Moeglich ist das, weil jeder Rasterpunkt nur in sein eigenes
            // Feld schreibt und der Bestpunkt erst danach in fester Reihenfolge
            // bestimmt wird.
            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();

            OptimiererErgebnis parallel = new SpeicherOptimierer()
                .Optimiere(eingang, basis, new OptimiererOptionen());
            OptimiererErgebnis seriell = new SpeicherOptimierer()
                .Optimiere(eingang, basis, new OptimiererOptionen { MaxParallel = 1 });

            ErgebnisseSindBitgleich(parallel, seriell);
        }

        [Fact]
        public void Parallel_Und_Seriell_Sind_Auch_Bei_Nachtnutzung_Gleich()
        {
            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();

            OptimiererErgebnis parallel = new SpeicherOptimierer().Optimiere(
                eingang, basis, new OptimiererOptionen { Strategie = OptimiererStrategie.Nachtnutzung });
            OptimiererErgebnis seriell = new SpeicherOptimierer().Optimiere(
                eingang, basis, new OptimiererOptionen { Strategie = OptimiererStrategie.Nachtnutzung, MaxParallel = 1 });

            ErgebnisseSindBitgleich(parallel, seriell);
        }

        [Fact]
        public void Nachtnutzung_Und_Dauernutzung_Liefern_Verschiedene_Raster()
        {
            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();
            OptimiererOptionen klein = new OptimiererOptionen { Stuetzstellen = 3, Feinraster = false };

            OptimiererErgebnis tag = new SpeicherOptimierer().Optimiere(eingang, basis, klein);
            OptimiererErgebnis nacht = new SpeicherOptimierer().Optimiere(
                eingang, basis, klein with { Strategie = OptimiererStrategie.Nachtnutzung });

            // Die Nachtnutzung haelt den Speicher tagsueber zurueck - auf einer Reihe
            // mit PV muss sich das im Ertrag niederschlagen.
            Assert.NotEqual(tag.BestPunkt.ErtragReferenzjahrEur, nacht.BestPunkt.ErtragReferenzjahrEur, 6);
        }

        /// <summary>Vergleicht zwei Laeufe Bit fuer Bit ueber alle Rasterpunkte.</summary>
        private static void ErgebnisseSindBitgleich(OptimiererErgebnis a, OptimiererErgebnis b)
        {
            RasterSindBitgleich(a.Grobraster, b.Grobraster);

            Assert.Equal(a.Feinraster == null, b.Feinraster == null);
            if (a.Feinraster != null) RasterSindBitgleich(a.Feinraster, b.Feinraster!);

            Assert.Equal(Bits(a.BestPunkt.CNomKwh), Bits(b.BestPunkt.CNomKwh));
            Assert.Equal(Bits(a.BestPunkt.CRate), Bits(b.BestPunkt.CRate));
            Assert.Equal(Bits(a.BestPunkt.ZielfunktionEur), Bits(b.BestPunkt.ZielfunktionEur));
            Assert.Equal(Bits(a.BestParameter.SoCMaxKwh), Bits(b.BestParameter.SoCMaxKwh));
            Assert.Equal(a.Randlage, b.Randlage);
            Assert.Equal(a.CPowNeutral, b.CPowNeutral);
            Assert.Equal(a.PunkteGerechnet, b.PunkteGerechnet);
        }

        private static void RasterSindBitgleich(OptimiererRaster a, OptimiererRaster b)
        {
            Assert.Equal(a.Zeilen, b.Zeilen);
            Assert.Equal(a.Spalten, b.Spalten);
            Assert.Equal(Bits(a.CMinKwh), Bits(b.CMinKwh));
            Assert.Equal(Bits(a.CMaxKwh), Bits(b.CMaxKwh));

            for (int i = 0; i < a.Zeilen; i++)
            {
                Assert.Equal(Bits(a.KapazitaetenKwh[i]), Bits(b.KapazitaetenKwh[i]));
                for (int k = 0; k < a.Spalten; k++)
                {
                    OptimiererPunkt pa = a.Punkte[i][k];
                    OptimiererPunkt pb = b.Punkte[i][k];
                    Assert.Equal(Bits(pa.CNomKwh), Bits(pb.CNomKwh));
                    Assert.Equal(Bits(pa.PKw), Bits(pb.PKw));
                    Assert.Equal(Bits(pa.ZielfunktionEur), Bits(pb.ZielfunktionEur));
                    Assert.Equal(Bits(pa.ErtragReferenzjahrEur), Bits(pb.ErtragReferenzjahrEur));
                    Assert.Equal(Bits(pa.AequivalenteVollzyklen), Bits(pb.AequivalenteVollzyklen));
                    Assert.Equal(Bits(pa.InvestitionEur), Bits(pb.InvestitionEur));
                }
            }
        }

        private static long Bits(double wert) => BitConverter.DoubleToInt64Bits(wert);

        // ==================================================================
        // Abbruch
        // ==================================================================

        [Fact]
        public void Abbruch_Vor_Dem_Start_Endet_Sofort()
        {
            using CancellationTokenSource quelle = new CancellationTokenSource();
            quelle.Cancel();

            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();

            Stopwatch uhr = Stopwatch.StartNew();
            Assert.ThrowsAny<OperationCanceledException>(() =>
                new SpeicherOptimierer().Optimiere(eingang, basis, new OptimiererOptionen(),
                                                   null, quelle.Token));
            uhr.Stop();

            // "Sauberes Ende" heisst hier: ohne einen einzigen Jahreslauf. Ein voller
            // 120-Punkte-Lauf dauert Groessenordnungen laenger als diese Schranke.
            Assert.True(uhr.ElapsedMilliseconds < 500,
                        "Der Abbruch vor dem Start dauerte " + uhr.ElapsedMilliseconds + " ms.");
        }

        [Fact]
        public void Abbruch_Waehrend_Des_Laufs_Bricht_Wirklich_Ab()
        {
            using CancellationTokenSource quelle = new CancellationTokenSource();
            int gemeldet = 0;

            IProgress<OptimiererFortschritt> fortschritt = new SynchronerFortschritt(f =>
            {
                Interlocked.Increment(ref gemeldet);
                if (f.Erledigt >= 5) quelle.Cancel();
            });

            Assert.ThrowsAny<OperationCanceledException>(() =>
                new SpeicherOptimierer().Optimiere(JahresEingang(), JahresBasis(),
                                                   new OptimiererOptionen(), fortschritt, quelle.Token));

            // Abgebrochen heisst: nicht alle 120 Punkte gerechnet.
            Assert.True(gemeldet < new OptimiererOptionen().PunkteGesamt,
                        "Es wurden " + gemeldet + " Punkte gemeldet - der Abbruch hat nicht gegriffen.");
        }

        // ==================================================================
        // Fortschritt
        // ==================================================================

        [Fact]
        public void Fortschritt_Meldet_Jeden_Punkt_Genau_Einmal()
        {
            List<OptimiererFortschritt> meldungen = new List<OptimiererFortschritt>();
            object schloss = new object();

            IProgress<OptimiererFortschritt> fortschritt = new SynchronerFortschritt(f =>
            {
                lock (schloss) meldungen.Add(f);
            });

            OptimiererOptionen opt = new OptimiererOptionen { Stuetzstellen = 3 };
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(JahresEingang(), JahresBasis(), opt, fortschritt);

            Assert.Equal(opt.PunkteGesamt, meldungen.Count);
            Assert.Equal(opt.PunkteGesamt, erg.PunkteGerechnet);

            // Jeder Stand kommt genau einmal vor - der Zaehler laeuft trotz
            // Parallelitaet luecken- und dublettenfrei von 1 bis Gesamt.
            HashSet<int> staende = new HashSet<int>();
            foreach (OptimiererFortschritt f in meldungen)
            {
                Assert.True(staende.Add(f.Erledigt), "Der Stand " + f.Erledigt + " wurde doppelt gemeldet.");
                Assert.Equal(opt.PunkteGesamt, f.Gesamt);
                Assert.InRange(f.Anteil, 0.0, 1.0);
            }
            Assert.Equal(1, staende.Count == 0 ? -1 : MinimumVon(staende));
            Assert.Equal(opt.PunkteGesamt, MaximumVon(staende));

            // Die zweite Stufe ist als solche gekennzeichnet.
            int feinPunkte = 0;
            foreach (OptimiererFortschritt f in meldungen) if (f.IstFeinraster) feinPunkte++;
            Assert.Equal(opt.PunkteJePhase, feinPunkte);
        }

        private static int MinimumVon(HashSet<int> werte)
        {
            int m = int.MaxValue;
            foreach (int w in werte) if (w < m) m = w;
            return m;
        }

        private static int MaximumVon(HashSet<int> werte)
        {
            int m = int.MinValue;
            foreach (int w in werte) if (w > m) m = w;
            return m;
        }

        /// <summary>
        /// <see cref="IProgress{T}"/> ohne <see cref="SynchronizationContext"/>: ruft
        /// direkt im meldenden Thread zurueck.
        /// </summary>
        /// <remarks>
        /// <c>Progress&lt;T&gt;</c> wuerde die Meldungen ueber den ThreadPool
        /// verschieben - der Test koennte dann weder zaehlen noch zeitnah abbrechen.
        /// In der Formularschicht ist genau dieses Verschieben erwuenscht, weil es die
        /// Meldung auf den UI-Thread bringt.
        /// </remarks>
        private sealed class SynchronerFortschritt : IProgress<OptimiererFortschritt>
        {
            private readonly Action<OptimiererFortschritt> _rueckruf;
            public SynchronerFortschritt(Action<OptimiererFortschritt> rueckruf) { _rueckruf = rueckruf; }
            public void Report(OptimiererFortschritt wert) { _rueckruf(wert); }
        }

        // ==================================================================
        // Optionen
        // ==================================================================

        [Fact]
        public void Vorbelegung_Entspricht_Dem_Fachkonzept()
        {
            OptimiererOptionen opt = new OptimiererOptionen();

            Assert.Equal(500.0, opt.CMinKwh, 12);
            Assert.Equal(5000.0, opt.CMaxKwh, 12);
            Assert.Equal(10, opt.Stuetzstellen);
            Assert.Equal(0.5, opt.RMin, 12);
            Assert.Equal(3.0, opt.RMax, 12);
            Assert.Equal(0.5, opt.RSchritt, 12);
            Assert.True(opt.Feinraster);
            Assert.False(opt.KVerInZielfunktion);     // Fachkonzept 5.4: Default AUS
            Assert.Equal(OptimiererStrategie.Dauernutzung, opt.Strategie);

            // 2 * 10 * 6 = 120 Jahreslaeufe.
            Assert.Equal(6, opt.CRatenAnzahl);
            Assert.Equal(60, opt.PunkteJePhase);
            Assert.Equal(120, opt.PunkteGesamt);

            double[] raten = opt.CRaten();
            Assert.Equal(new[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 }, raten);
        }

        [Theory]
        [InlineData(0.0, 5000.0, 10, 0.5, 3.0, 0.5)]      // C_min = 0
        [InlineData(5000.0, 500.0, 10, 0.5, 3.0, 0.5)]    // C_max < C_min
        [InlineData(500.0, 5000.0, 1, 0.5, 3.0, 0.5)]     // nur eine Stuetzstelle
        [InlineData(500.0, 5000.0, 10, 0.0, 3.0, 0.5)]    // r_min = 0
        [InlineData(500.0, 5000.0, 10, 3.0, 0.5, 0.5)]    // r_max < r_min
        [InlineData(500.0, 5000.0, 10, 0.5, 3.0, 0.0)]    // Schrittweite 0
        public void Unbrauchbare_Optionen_Werden_Abgelehnt(
            double cMin, double cMax, int n, double rMin, double rMax, double rSchritt)
        {
            OptimiererOptionen opt = new OptimiererOptionen
            {
                CMinKwh = cMin,
                CMaxKwh = cMax,
                Stuetzstellen = n,
                RMin = rMin,
                RMax = rMax,
                RSchritt = rSchritt
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => opt.Pruefe());
        }

        [Fact]
        public void Basisauslegung_Ohne_Kapazitaet_Wird_Abgelehnt()
        {
            // Ohne Basiskapazitaet ist der Prozentsatz des SoC-Bands nicht bestimmbar.
            SpeicherParameter ohne = MiniBasis() with { CNomKwh = 0.0, SoCMinKwh = 0.0, SoCMaxKwh = 0.0 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpeicherOptimierer().Optimiere(MiniEingang(), ohne, MiniOptionen()));
        }

        [Fact]
        public void Fehlende_Argumente_Werden_Abgelehnt()
        {
            SpeicherOptimierer o = new SpeicherOptimierer();
            Assert.Throws<ArgumentNullException>(() => o.Optimiere(null!, MiniBasis(), MiniOptionen()));
            Assert.Throws<ArgumentNullException>(() => o.Optimiere(MiniEingang(), null!, MiniOptionen()));
        }

        // ==================================================================
        // Rasterauswertung fuer die Anzeige
        // ==================================================================

        [Fact]
        public void Schnittkurve_Ist_Die_Spalte_Der_Besten_C_Rate()
        {
            OptimiererOptionen opt = MiniOptionen() with { RMin = 0.5, RMax = 1.5, RSchritt = 0.5 };
            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(MiniEingang(), MiniBasis(), opt);

            OptimiererRaster raster = erg.BestRaster;
            int spalte = raster.IndexCRate(erg.BestPunkt.CRate);
            double[] kurve = raster.Schnittkurve(spalte);

            Assert.Equal(raster.Zeilen, kurve.Length);
            for (int i = 0; i < raster.Zeilen; i++)
                Assert.Equal(raster.Punkte[i][spalte].ZielfunktionEur, kurve[i], 12);

            double min, max;
            raster.Wertebereich(out min, out max);
            Assert.True(min <= max);
            Assert.Equal(max, erg.BestRaster.BestPunkt.ZielfunktionEur, 12);
        }

        [Fact]
        public void BestParameter_Beschreibt_Den_Bestpunkt_Vollstaendig()
        {
            OptimiererErgebnis erg = new SpeicherOptimierer()
                .Optimiere(MiniEingang(), MiniBasis(), MiniOptionen());

            SpeicherParameter p = erg.BestParameter;
            Assert.Equal(erg.BestPunkt.CNomKwh, p.CNomKwh, 12);
            Assert.Equal(erg.BestPunkt.PKw, p.PKw, 12);
            Assert.Equal(erg.BestPunkt.InvestitionEur, p.InvestitionEur, 12);

            // Ein Einzellauf mit diesen Parametern reproduziert den Rasterpunkt.
            SpeicherErgebnis einzel = new Dauernutzung(SpeicherModus.Energetisch).Berechne(MiniEingang(), p);
            Assert.Equal(Bits(erg.BestPunkt.ErtragReferenzjahrEur),
                         Bits(einzel.Wirtschaftlichkeit.ErtragReferenzjahrEur));
            Assert.Equal(Bits(erg.BestPunkt.JahresueberschussEur),
                         Bits(einzel.Wirtschaftlichkeit.JahresueberschussEur));
        }

        // ==================================================================
        // Laufzeit (Abnahmekriterium AP8)
        // ==================================================================

        [Fact]
        public void Voller_Lauf_Bleibt_Deutlich_Unter_Zehn_Sekunden()
        {
            SpeicherEingang eingang = JahresEingang();
            SpeicherParameter basis = JahresBasis();
            OptimiererOptionen opt = new OptimiererOptionen();

            OptimiererErgebnis erg = new SpeicherOptimierer().Optimiere(eingang, basis, opt);

            _ausgabe.WriteLine("Rastersuche 120 Punkte x 35.040 Intervalle: "
                               + erg.Dauer.TotalMilliseconds.ToString("F0") + " ms"
                               + "  (Bestpunkt " + erg.BestPunkt.CNomKwh.ToString("F0") + " kWh / "
                               + erg.BestPunkt.CRate.ToString("F1") + " C, dJ = "
                               + erg.BestPunkt.ZielfunktionEur.ToString("F0") + " EUR/a)");

            Assert.Equal(120, erg.PunkteGerechnet);
            Assert.Equal(35040, eingang.Anzahl);
            Assert.True(erg.Dauer.TotalSeconds < 10.0,
                        "Die Rastersuche brauchte " + erg.Dauer.TotalSeconds.ToString("F2") + " s.");
        }

        // ==================================================================
        // Synthetische Jahresreihen
        // ==================================================================

        /// <summary>
        /// Deterministische Jahresreihen im Viertelstundenraster: Tagesgang der Last,
        /// PV-Tagesbogen mit Jahresgang, tagesperiodischer Bezugspreis.
        /// </summary>
        /// <remarks>
        /// Bewusst ohne Zufall und ohne Datei - die Reihen muessen ueber alle Laeufe
        /// bitgleich sein, sonst waere der Determinismustest wertlos.
        /// </remarks>
        private static SpeicherEingang JahresEingang(int n = 35040)
        {
            double[] last = new double[n];
            double[] pv = new double[n];
            double[] preis = new double[n];

            for (int i = 0; i < n; i++)
            {
                int imTag = i % 96;
                int tag = i / 96;
                double stunde = imTag * 0.25;
                double jahresgang = Math.Sin(2.0 * Math.PI * (tag - 80) / 365.0);

                double schicht = Math.Max(0.0, Math.Sin(Math.PI * (stunde - 6.0) / 14.0));
                last[i] = 400.0 + 260.0 * schicht;

                double tagbogen = Math.Max(0.0, Math.Sin(Math.PI * (stunde - 6.0) / 12.0));
                pv[i] = 1700.0 * tagbogen * (0.55 + 0.45 * jahresgang);

                preis[i] = 22.0 + 6.0 * Math.Cos(2.0 * Math.PI * stunde / 24.0);
            }

            return new SpeicherEingang(last, pv, preis)
                .MitVerguetungen(VerguetungCtKwh, 12.0);
        }

        /// <summary>Basisauslegung fuer die Jahreslaeufe: 2.000 kWh / 1 C, Band 10 … 90 %.</summary>
        private static SpeicherParameter JahresBasis() => new SpeicherParameter
        {
            CNomKwh = 2000.0,
            PKw = 2000.0,
            SoCMinKwh = 200.0,
            SoCMaxKwh = 1800.0,
            RoundTripWirkungsgrad = 0.90,
            DtH = Dt,
            VerguetungCtKwh = VerguetungCtKwh,
            CCapEurProKwh = 400.0,
            CPowEurProKw = 120.0,
            IFixEur = 20000.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.001,
            CVerEurProKwhZyklus = 0.025
        };
    }
}
