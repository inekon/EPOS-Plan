using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Schritte der Karte im Einzelnen</b> (Stufe KP2, Welle U3; Teilkonzept Konditionierungsprofile
    /// 3.2, 3.5, 7.5; Entwurf KP2 Festlegung 15) — rein, ohne Datenbank: Grundangabe und Standardwoche
    /// (<see cref="Kalenderwerkzeuge.Grundangabe"/>, <see cref="Kalenderwerkzeuge.Standardwoche"/>) und ihr
    /// Schritt am Arbeitsstand, der die Herkunft einer direkten Eingabe stehen lässt.
    /// </summary>
    /// <remarks>Die Fälle vergleichen Zahlen, Ränge und Kennwörter, keine Ressourcentexte.</remarks>
    public class KalenderkarteSchritteTests
    {
        /// <summary>Ein Heizkalender mit konstanten 20 °C und einer eigenen Periode.</summary>
        private static Konditionierungskalender Konstant(double wert = 20.0)
            => new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(wert), null,
                                            new[] { Kalenderregel.Zeitraum(310, DbWerte.KOND_ART_ZEITRAUM, "Messe", 60, 62,
                                                                           Kalenderangabe.AusWert(22.0)) });

        private static double[] Woche(double tag, double nacht)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                    w[Kalenderwoche.Stelle(t, s)] = s >= 6 && s < 22 ? tag : nacht;
            return w;
        }

        // =============================================================================
        //  Grundangabe
        // =============================================================================

        [Fact]
        public void Die_Grundangabe_setzt_Wert_oder_aus_und_laesst_Perioden_und_Nennwert()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Grundangabe(Konstant(), 18.5);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Wert, b.Kalender.Grundangabe.Art);
            Assert.Equal(18.5, b.Kalender.Grundangabe.Wert);
            Assert.Equal("", b.Vermerk);
            Assert.Single(b.Kalender.Perioden);
            Assert.Equal(310, b.Kalender.Perioden[0].Rang);

            b = Kalenderwerkzeuge.Grundangabe(Konstant(), null);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Aus, b.Kalender.Grundangabe.Art);
        }

        [Fact]
        public void Die_Grundangabe_ersetzt_eine_Standardwoche()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(Woche(20, 16)), null, null);
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Grundangabe(k, 19.0);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Wert, b.Kalender.Grundangabe.Art);
        }

        [Fact]
        public void Eine_Grundangabe_ausserhalb_der_Grenzen_oder_ohne_Rundlauf_wird_benannt_abgelehnt()
        {
            var lueftung = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.AusWert(0.5), null, null);
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Grundangabe(lueftung, 55.0);
            Assert.False(b.Ok);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
            Assert.Null(b.Kalender);

            b = Kalenderwerkzeuge.Grundangabe(Konstant(), 20.123456);
            Assert.False(b.Ok);
        }

        // =============================================================================
        //  Standardwoche
        // =============================================================================

        [Fact]
        public void Die_Standardwoche_nimmt_168_Zellen_samt_aus_und_ersetzt_die_Grundangabe()
        {
            double[] w = Woche(21, 17);
            w[Kalenderwoche.Stelle(6, 3)] = double.NaN;
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Standardwoche(Konstant(), w);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Woche, b.Kalender.Grundangabe.Art);
            Assert.True(double.IsNaN(b.Kalender.Standardwoche[Kalenderwoche.Stelle(6, 3)]));
            Assert.Equal(21.0, b.Kalender.Standardwoche[Kalenderwoche.Stelle(0, 7)]);
            Assert.Single(b.Kalender.Perioden);
        }

        [Fact]
        public void Eine_Woche_mit_falscher_Laenge_oder_einem_Wert_ausserhalb_wird_benannt_abgelehnt()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Standardwoche(Konstant(), new double[24]);
            Assert.False(b.Ok);
            Assert.Contains("168", b.Meldung);

            var anteil = new Konditionierungskalender(Konditionierungsgroesse.Personen, Kalenderangabe.AusWert(1.0), null, null);
            double[] w = Enumerable.Repeat(0.5, Kalenderwoche.WOCHENWERTE).ToArray();
            w[100] = 1.5;
            b = Kalenderwerkzeuge.Standardwoche(anteil, w);
            Assert.False(b.Ok);
        }

        [Fact]
        public void Standardwoche_verwerfen_setzt_den_haeufigsten_Wert_als_Grundangabe()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(Woche(20, 16)), null, null);
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.Standardwoche(k, null);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Wert, b.Kalender.Grundangabe.Art);
            Assert.Equal(20.0, b.Kalender.Grundangabe.Wert);

            // Nur „aus" in der Woche: die Grundangabe „aus".
            var aus = new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll,
                Kalenderangabe.AusWoche(Enumerable.Repeat(double.NaN, Kalenderwoche.WOCHENWERTE).ToArray()), null, null);
            b = Kalenderwerkzeuge.Standardwoche(aus, null);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(Angabeart.Aus, b.Kalender.Grundangabe.Art);
        }

        [Fact]
        public void Der_haeufigste_Wert_nimmt_bei_Gleichstand_den_hoeheren_und_uebergeht_aus()
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int i = 0; i < w.Length; i++) w[i] = i < 60 ? 16.0 : i < 120 ? 20.0 : double.NaN;
            Assert.Equal(20.0, Kalenderwerkzeuge.HaeufigsterWert(w));
            Assert.Null(Kalenderwerkzeuge.HaeufigsterWert(Enumerable.Repeat(double.NaN, 168).ToArray()));
        }

        // =============================================================================
        //  Der Schritt am Arbeitsstand: eine direkte Eingabe lässt die Herkunft stehen
        // =============================================================================

        /// <summary>Ein Katalogbau mit angelegtem Heizkalender aus der Vorlage „Büro" und dem Vermerk eines Werkzeugs.</summary>
        internal static Konditionierungsarbeitsstand MitHeizkalender()
        {
            Konditionierungsstand e = Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, null)
                .MitKalender(Konditionierungsgroesse.Heizsoll, Konstant(), new Kalenderherkunft("Büro", "Zeitfenster"));
            return new Konditionierungsarbeitsstand(e, null);
        }

        [Fact]
        public void Grundangabe_und_Woche_am_Arbeitsstand_lassen_Vorlage_und_Vermerk_stehen()
        {
            var ort = new Konditionierungsort(Konditionierungsgroesse.Heizsoll);
            Konditionierungsschritt s = Konditionierungsarbeit.Grundangabe(MitHeizkalender(), ort, 18.0);
            Assert.True(s.Ok, s.Meldung);
            Assert.Equal(18.0, s.Stand.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Grundangabe.Wert);
            Assert.Equal(new Kalenderherkunft("Büro", "Zeitfenster"), s.Stand.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll));

            s = Konditionierungsarbeit.Standardwoche(MitHeizkalender(), ort, Woche(21, 17));
            Assert.True(s.Ok, s.Meldung);
            Assert.Equal(Angabeart.Woche, s.Stand.Gebaeude.Kalender(Konditionierungsgroesse.Heizsoll).Grundangabe.Art);
            Assert.Equal(new Kalenderherkunft("Büro", "Zeitfenster"), s.Stand.Gebaeude.Herkunft(Konditionierungsgroesse.Heizsoll));
        }

        // =============================================================================
        //  Die Periodenliste (Festlegung 15)
        // =============================================================================

        /// <summary>Ferien (Rang 202, Matrixbereich), eine Feiertagsregel (100), zwei eigene Perioden (310, 311).</summary>
        private static Konditionierungskalender MitPerioden()
            => new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(Woche(20, 16)), null, new[]
            {
                Kalenderregel.Zeitraum(202, DbWerte.KOND_ART_FERIEN, "Sommerferien", 205, 246, Kalenderangabe.AusWert(16.0)),
                Kalenderregel.Feiertag(100, "Neujahr", DbWerte.KOND_FEIERTAGE[0], Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(310, DbWerte.KOND_ART_ZEITRAUM, "Messe", 60, 62, Kalenderangabe.AusWert(22.0)),
                Kalenderregel.Zeitraum(311, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 100, 120, Kalenderangabe.Abgeschaltet),
            });

        [Fact]
        public void Eine_neue_Periode_steht_ueber_der_ranghoechsten_eigenen_im_Eigenband()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_ZEITRAUM,
                " Betriebsausflug ", 150, 150, null, Kalenderangabe.Abgeschaltet);
            Assert.True(b.Ok, b.Meldung);
            Kalenderregel neu = b.Kalender.Perioden.Single(p => p.Bezeichner == "Betriebsausflug");
            Assert.Equal(312, neu.Rang);
            Assert.Equal(Angabeart.Aus, neu.Angabe.Art);
            Assert.Equal(5, b.Kalender.Perioden.Count);

            // Ein Feiertag der Art FEIERTAG steht ebenso im Eigenband — über den Ferien.
            b = Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_FEIERTAG, "Neujahr aus", 0, 0,
                                                DbWerte.KOND_FEIERTAGE[0], Kalenderangabe.Abgeschaltet);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(312, b.Kalender.Perioden.Single(p => p.Bezeichner == "Neujahr aus").Rang);
            Assert.True(b.Kalender.Perioden.Single(p => p.Bezeichner == "Neujahr aus").IstFeiertag);
        }

        [Fact]
        public void Ersetzen_haelt_den_Rang_und_der_Matrixbereich_ist_nur_lesbar()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), 310, DbWerte.KOND_ART_ZEITRAUM,
                "Messe", 61, 64, null, Kalenderangabe.AusWert(21.0));
            Assert.True(b.Ok, b.Meldung);
            Kalenderregel r = b.Kalender.Perioden.Single(p => p.Rang == 310);
            Assert.Equal((61, 64, 21.0), (r.Beginn, r.Ende, r.Angabe.Wert));

            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), 202, DbWerte.KOND_ART_ZEITRAUM, "x", 1, 2, null,
                                                         Kalenderangabe.Abgeschaltet).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeLoeschen(MitPerioden(), 202).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), 999, DbWerte.KOND_ART_ZEITRAUM, "x", 1, 2, null,
                                                         Kalenderangabe.Abgeschaltet).Ok);
        }

        [Fact]
        public void Neue_Perioden_sind_Zeitraum_oder_Feiertag_mit_Name_Tagen_und_gueltiger_Angabe()
        {
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_FERIEN, "Herbst", 280, 290, null,
                                                         Kalenderangabe.AusWert(16.0)).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_BETRIEBSPAUSE, "Pause", 1, 2, null,
                                                         Kalenderangabe.Abgeschaltet).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_ZEITRAUM, "  ", 1, 2, null,
                                                         Kalenderangabe.Abgeschaltet).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_ZEITRAUM, "x", 0, 366, null,
                                                         Kalenderangabe.Abgeschaltet).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_FEIERTAG, "x", 0, 0, "OSTERSONNTAG",
                                                         Kalenderangabe.Abgeschaltet).Ok);
            Assert.False(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_ZEITRAUM, "x", 1, 2, null,
                                                         Kalenderangabe.AusWert(20.12345)).Ok);
            // Über den Jahreswechsel ist erlaubt.
            Assert.True(Kalenderwerkzeuge.PeriodeSetzen(MitPerioden(), null, DbWerte.KOND_ART_ZEITRAUM, "Jahreswechsel", 358, 5, null,
                                                        Kalenderangabe.AlsWochentag(7)).Ok);
        }

        [Fact]
        public void Rang_hoeher_und_niedriger_tauschen_im_Eigenband_und_an_den_Grenzen_wird_benannt_abgelehnt()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 310, hoeher: true);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(311, b.Kalender.Perioden.Single(p => p.Bezeichner == "Messe").Rang);
            Assert.Equal(310, b.Kalender.Perioden.Single(p => p.Bezeichner == "Umbau").Rang);

            b = Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 311, hoeher: false);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(310, b.Kalender.Perioden.Single(p => p.Bezeichner == "Umbau").Rang);

            Assert.False(Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 311, hoeher: true).Ok);   // oben
            Assert.False(Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 310, hoeher: false).Ok);  // unten
            Assert.False(Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 100, hoeher: true).Ok);   // Feiertagsband
            Assert.False(Kalenderwerkzeuge.RangVerschieben(MitPerioden(), 202, hoeher: true).Ok);   // Ferien
            Assert.Null(Kalenderwerkzeuge.Rangpruefung(b.Kalender.Perioden));
        }

        [Fact]
        public void Loeschen_nimmt_eine_eigene_Periode_und_eine_Feiertagsregel_und_laesst_den_Rest()
        {
            Kalenderwerkzeuge.Werkzeugbefund b = Kalenderwerkzeuge.PeriodeLoeschen(MitPerioden(), 310);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(new[] { 100, 202, 311 }, b.Kalender.Perioden.Select(p => p.Rang).OrderBy(r => r).ToArray());
            b = Kalenderwerkzeuge.PeriodeLoeschen(MitPerioden(), 100);
            Assert.True(b.Ok, b.Meldung);
            Assert.DoesNotContain(b.Kalender.Perioden, p => p.IstFeiertag);
        }

        [Fact]
        public void Ohne_angelegten_Kalender_lehnt_der_Schritt_benannt_ab_statt_einen_anzulegen()
        {
            var leer = new Konditionierungsarbeitsstand(Konditionierungsstand.Leer(Kalendereigentuemer.Katalogbau, null), null);
            Konditionierungsschritt s = Konditionierungsarbeit.Grundangabe(leer, new Konditionierungsort(Konditionierungsgroesse.Heizsoll), 20.0);
            Assert.False(s.Ok);
            Assert.False(string.IsNullOrEmpty(s.Meldung));
        }
    }
}
