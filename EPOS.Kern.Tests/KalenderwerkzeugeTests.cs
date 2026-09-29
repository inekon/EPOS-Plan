using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Werkzeuge der Kalenderkarte</b> (Stufe KP1b, Konzept Konditionierungsprofile 3.5):
    /// das Zeitfenster „Tage, von, bis, Wert", die Feiertage als Regel (F11) und die Rangbänder
    /// (N1.61 Nr. 5).
    ///
    /// <para><b>Ohne Datenbank</b> — <see cref="Kalenderwerkzeuge"/> ist eine reine Funktion von
    /// Kalender und Eingaben. Die Fälle pinnen keine Kultur: Sie vergleichen Zahlen, Ränge und
    /// Kennwörter, keine Ressourcentexte.</para>
    /// </summary>
    public class KalenderwerkzeugeTests
    {
        private const int W0_MONTAG = 0;
        private const int REFERENZJAHR = 2021;

        /// <summary>Ein Heizkalender mit konstanten 20 °C und ohne Periode.</summary>
        private static Konditionierungskalender Konstant(double wert = 20.0)
            => new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                            Kalenderangabe.AusWert(wert), null, null);

        // =============================================================================
        //  Das Zeitfenster
        // =============================================================================

        [Fact]
        public void Das_Zeitfenster_ersetzt_genau_seine_Stunden_an_seinen_Tagen()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(
                Konstant(), new[] { 0, 1, 2, 3, 4 }, 7, 18, 20.0);
            Assert.True(b.Ok, b.Meldung);
            Assert.False(string.IsNullOrEmpty(b.Vermerk));

            // Der Ausgangskalender stand konstant auf 20; das Fenster setzt 20 ueber sich selbst.
            // Deshalb die Gegenprobe mit einem ANDEREN Wert:
            b = Kalenderwerkzeuge.Zeitfenster(Konstant(16.0), new[] { 0, 1, 2, 3, 4 }, 7, 18, 20.0);
            Assert.True(b.Ok, b.Meldung);
            IReadOnlyList<double> w = b.Kalender.Standardwoche;
            Assert.Equal(Kalenderwoche.WOCHENWERTE, w.Count);

            for (int tag = 0; tag < 7; tag++)
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    bool imFenster = tag <= 4 && stunde >= 7 && stunde < 18;
                    Assert.Equal(imFenster ? 20.0 : 16.0, w[Kalenderwoche.Stelle(tag, stunde)]);
                }
        }

        [Fact]
        public void Das_Zeitfenster_geht_ueber_Mitternacht()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(
                Konstant(20.0), null, 22, 6, 16.0);
            Assert.True(b.Ok, b.Meldung);
            IReadOnlyList<double> w = b.Kalender.Standardwoche;

            for (int tag = 0; tag < 7; tag++)
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    bool nacht = stunde >= 22 || stunde < 6;      // acht Stunden, wie Nachtzeit 22–6
                    Assert.Equal(nacht ? 16.0 : 20.0, w[Kalenderwoche.Stelle(tag, stunde)]);
                }
        }

        [Fact]
        public void Das_Zeitfenster_schreibt_aus_und_laesst_die_Perioden_stehen()
        {
            var perioden = new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Brücke",
                                       100, 110, Kalenderangabe.AusWert(18.0)),
            };
            var quelle = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                      Kalenderangabe.AusWert(20.0), null, perioden);

            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(
                quelle, new[] { 5, 6 }, 0, 24, null);
            Assert.True(b.Ok, b.Meldung);

            IReadOnlyList<double> w = b.Kalender.Standardwoche;
            for (int stunde = 0; stunde < 24; stunde++)
            {
                Assert.True(double.IsNaN(w[Kalenderwoche.Stelle(5, stunde)]));
                Assert.True(double.IsNaN(w[Kalenderwoche.Stelle(6, stunde)]));
                Assert.Equal(20.0, w[Kalenderwoche.Stelle(0, stunde)]);
            }
            Assert.Single(b.Kalender.Perioden);
            Assert.Equal(Standardfahrplan.RANG_EIGEN, b.Kalender.Perioden[0].Rang);
        }

        [Theory]
        [InlineData(7, 7)]          // leeres Fenster
        [InlineData(24, 6)]         // Beginn außerhalb
        [InlineData(7, 25)]         // Ende außerhalb
        public void Ein_leeres_oder_ungueltiges_Fenster_wird_benannt_abgelehnt(int von, int bis)
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(Konstant(), null, von, bis, 20.0);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
            Assert.Null(b.Kalender);
        }

        [Fact]
        public void Ein_Wert_ausserhalb_der_Grenzen_und_ein_unrunder_Wert_werden_abgelehnt()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(Konstant(), null, 7, 18, 99.0);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));

            // Fuenf Nachkommastellen: der Rundlauf ueber den Text der Wochenspalte scheitert.
            b = Kalenderwerkzeuge.Zeitfenster(Konstant(), null, 7, 18, 20.000005);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
        }

        [Fact]
        public void Ein_unbekannter_Wochentag_wird_benannt_abgelehnt()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Zeitfenster(
                Konstant(), new[] { 0, 7 }, 7, 18, 20.0);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
        }

        // =============================================================================
        //  Die Feiertage als Regel
        // =============================================================================

        [Fact]
        public void Die_Feiertage_legen_neun_Perioden_im_Band_100_bis_108_an()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Feiertagsregeln(Konstant());
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(DbWerte.KOND_FEIERTAGE.Count, b.Kalender.Perioden.Count);
            Assert.Equal(9, b.Kalender.Perioden.Count);

            var regeln = new List<string>();
            foreach (Kalenderregel r in b.Kalender.Perioden)
            {
                Assert.True(r.IstFeiertag);
                Assert.Equal(DbWerte.KOND_ART_FEIERTAG, r.Art);
                Assert.Equal(Angabeart.WieWochentag, r.Angabe.Art);
                Assert.Equal(7, r.Angabe.WieWochentag);
                Assert.InRange(r.Rang, Standardfahrplan.RANG_FEIERTAG, Standardfahrplan.RANG_FEIERTAG_LETZTER);
                Assert.False(string.IsNullOrEmpty(r.Bezeichner));
                regeln.Add(r.Feiertagsregel);
            }
            foreach (string regel in DbWerte.KOND_FEIERTAGE) Assert.Contains(regel, regeln);
        }

        [Fact]
        public void Die_Feiertage_legen_eine_vorhandene_Regel_nicht_doppelt_an()
        {
            Kalenderwerkzeuge.Werkzeugbefund erst = Kalenderwerkzeuge.Feiertagsregeln(Konstant());
            Assert.True(erst.Ok, erst.Meldung);

            Kalenderwerkzeuge.Werkzeugbefund zweit = Kalenderwerkzeuge.Feiertagsregeln(erst.Kalender);
            Assert.True(zweit.Ok, zweit.Meldung);
            Assert.Equal(9, zweit.Kalender.Perioden.Count);

            // Auch eine Regel mit FREMDEM Rang und fremder Angabe zaehlt als vorhanden.
            var eigene = new[]
            {
                Kalenderregel.Feiertag(Standardfahrplan.RANG_EIGEN, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                       Kalenderangabe.Abgeschaltet),
            };
            var mitEigener = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                          Kalenderangabe.AusWert(20.0), null, eigene);
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Feiertagsregeln(mitEigener);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(9, b.Kalender.Perioden.Count);
            int neujahr = 0;
            foreach (Kalenderregel r in b.Kalender.Perioden)
                if (string.Equals(r.Feiertagsregel, DbWerte.KOND_FEIERTAG_NEUJAHR, StringComparison.Ordinal))
                    neujahr++;
            Assert.Equal(1, neujahr);
        }

        [Fact]
        public void Ein_Feiertag_in_den_Ferien_behaelt_den_Ferienwert()
        {
            // Neujahr ist der 1. Januar; die Ferien laufen vom 1. bis zum 10. Januar auf „aus".
            var ferien = new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Ferien 1",
                                       1, 10, Kalenderangabe.AusWert(12.0)),
            };
            var quelle = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                      Kalenderangabe.AusWert(20.0), null, ferien);

            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Feiertagsregeln(quelle);
            Assert.True(b.Ok, b.Meldung);

            // Der Feiertag liegt UNTER den Ferien: am 1. Januar gilt der Ferienwert.
            Assert.Equal("Ferien 1", b.Kalender.Quelle(0, REFERENZJAHR));
            double[] reihe = b.Kalender.Auswerten(W0_MONTAG, REFERENZJAHR);
            Assert.Equal(12.0, reihe[0]);
            Assert.Equal(12.0, reihe[24 * 9]);          // 10. Januar, noch Ferien
            Assert.Equal(20.0, reihe[24 * 10]);         // 11. Januar, wieder Grundangabe

            // Gegenprobe: laege der Feiertag ueber den Ferien, gaelte „wie Sonntag" — hier ohne
            // Standardwoche, also greift die Angabe nicht und der Ferienwert bliebe ohnehin.
            Assert.True(Standardfahrplan.RANG_FEIERTAG_LETZTER < Standardfahrplan.RANG_FERIEN);
        }

        [Fact]
        public void Ein_belegter_Feiertagsrang_wird_benannt_abgelehnt()
        {
            var fremd = new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_FEIERTAG, DbWerte.KOND_ART_ZEITRAUM, "Fremd",
                                       50, 60, Kalenderangabe.AusWert(18.0)),
            };
            var quelle = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                      Kalenderangabe.AusWert(20.0), null, fremd);
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Feiertagsregeln(quelle);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
            Assert.Null(b.Kalender);
        }

        // =============================================================================
        //  Die Rangbänder
        // =============================================================================

        [Fact]
        public void Die_vier_Baender_liegen_in_der_Reihenfolge_der_Festlegung()
        {
            Assert.Equal(100, Standardfahrplan.RANG_FEIERTAG);
            Assert.Equal(108, Standardfahrplan.RANG_FEIERTAG_LETZTER);
            Assert.Equal(200, Standardfahrplan.RANG_FERIEN);
            Assert.Equal(310, Standardfahrplan.RANG_EIGEN);
            Assert.Equal(899, Standardfahrplan.RANG_EIGEN_LETZTER);
            Assert.Equal(900, Standardfahrplan.RANG_SAISON);

            Assert.True(Standardfahrplan.RANG_FEIERTAG_LETZTER < Standardfahrplan.RANG_FERIEN);
            Assert.True(Standardfahrplan.RANG_FERIEN + Matrixeingang.FERIENZEITRAEUME - 1
                        < Standardfahrplan.RANG_EIGEN);
            Assert.True(Standardfahrplan.RANG_EIGEN_LETZTER < Standardfahrplan.RANG_SAISON);
            Assert.True(Standardfahrplan.RANG_SAISON <= Kalenderregel.RANG_MAX);
        }

        [Fact]
        public void Das_Eigenband_vergibt_aufsteigend_und_ueberspringt_Belegtes()
        {
            var perioden = new[]
            {
                Kalenderregel.Zeitraum(1, DbWerte.KOND_ART_ZEITRAUM, "A", 10, 20, Kalenderangabe.AusWert(18.0)),
                Kalenderregel.Zeitraum(2, DbWerte.KOND_ART_ZEITRAUM, "B", 30, 40, Kalenderangabe.Abgeschaltet),
            };
            var belegt = new HashSet<int> { Standardfahrplan.RANG_EIGEN, Standardfahrplan.RANG_FERIEN };

            string fehler = Kalenderwerkzeuge.ImEigenband(perioden, belegt, out List<Kalenderregel> vergeben);
            Assert.Null(fehler);
            Assert.Equal(2, vergeben.Count);
            Assert.Equal(Standardfahrplan.RANG_EIGEN + 1, vergeben[0].Rang);
            Assert.Equal(Standardfahrplan.RANG_EIGEN + 2, vergeben[1].Rang);
            Assert.Equal("A", vergeben[0].Bezeichner);
            Assert.Equal(Kalenderangabe.Abgeschaltet.Art, vergeben[1].Angabe.Art);

            // Die belegten Raenge wachsen mit: ein zweiter Aufruf faengt darueber an.
            Assert.Contains(Standardfahrplan.RANG_EIGEN + 1, belegt);
            Assert.Contains(Standardfahrplan.RANG_EIGEN + 2, belegt);
        }

        [Fact]
        public void Ein_volles_Eigenband_wird_benannt_abgelehnt()
        {
            var belegt = new HashSet<int>();
            for (int r = Standardfahrplan.RANG_EIGEN; r <= Standardfahrplan.RANG_EIGEN_LETZTER; r++)
                belegt.Add(r);

            var perioden = new[]
            {
                Kalenderregel.Zeitraum(1, DbWerte.KOND_ART_ZEITRAUM, "A", 10, 20, Kalenderangabe.AusWert(18.0)),
            };
            string fehler = Kalenderwerkzeuge.ImEigenband(perioden, belegt, out List<Kalenderregel> vergeben);
            Assert.False(string.IsNullOrEmpty(fehler));
            Assert.Empty(vergeben);
        }

        [Fact]
        public void Ein_doppelter_Rang_wird_benannt_abgelehnt()
        {
            var perioden = new[]
            {
                Kalenderregel.Zeitraum(310, DbWerte.KOND_ART_ZEITRAUM, "A", 10, 20, Kalenderangabe.AusWert(18.0)),
                Kalenderregel.Zeitraum(310, DbWerte.KOND_ART_ZEITRAUM, "B", 30, 40, Kalenderangabe.AusWert(19.0)),
            };
            Assert.False(string.IsNullOrEmpty(Kalenderwerkzeuge.Rangpruefung(perioden)));
            Assert.Null(Kalenderwerkzeuge.Rangpruefung(new[] { perioden[0] }));
            Assert.Null(Kalenderwerkzeuge.Rangpruefung(null));
        }

        [Fact]
        public void MitRang_erhaelt_Zeitraum_und_Feiertagsregel()
        {
            Kalenderregel z = Kalenderregel.Zeitraum(5, DbWerte.KOND_ART_ZEITRAUM, "Z", 10, 20,
                                                     Kalenderangabe.AusWert(18.0));
            Kalenderregel neu = Kalenderwerkzeuge.MitRang(z, 400);
            Assert.Equal(400, neu.Rang);
            Assert.Equal(z.Art, neu.Art);
            Assert.Equal(z.Beginn, neu.Beginn);
            Assert.Equal(z.Ende, neu.Ende);
            Assert.False(neu.IstFeiertag);

            Kalenderregel f = Kalenderregel.Feiertag(5, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                                     Kalenderangabe.AlsWochentag(7));
            Kalenderregel neuF = Kalenderwerkzeuge.MitRang(f, 401);
            Assert.Equal(401, neuF.Rang);
            Assert.True(neuF.IstFeiertag);
            Assert.Equal(DbWerte.KOND_FEIERTAG_NEUJAHR, neuF.Feiertagsregel);
        }

        [Fact]
        public void Die_Woche_entsteht_aus_der_Grundangabe_wenn_der_Kalender_keine_fuehrt()
        {
            double[] ausWert = Kalenderwerkzeuge.WocheAus(Konstant(19.5));
            Assert.Equal(Kalenderwoche.WOCHENWERTE, ausWert.Length);
            foreach (double w in ausWert) Assert.Equal(19.5, w);

            var aus = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                   Kalenderangabe.Abgeschaltet, null, null);
            foreach (double w in Kalenderwerkzeuge.WocheAus(aus)) Assert.True(double.IsNaN(w));
        }
    }
}
