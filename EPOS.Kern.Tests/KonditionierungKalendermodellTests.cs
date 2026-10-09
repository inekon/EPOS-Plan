using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Kalendermodell der Konditionierung</b> (Stufe KP1, Konzept Konditionierungsprofile 3.2,
    /// 3.6 und 5.2): der strenge Leser und Schreiber der Standardwoche, die Kompilierung zur
    /// 8760-Reihe samt Vorrang, Perioden, Jahreswechsel und Feiertagen, die Vererbung Gebäude → Zone
    /// je Zelle und der Zeilenleser mit seinen benannten Fehlerbildern.
    ///
    /// <para><b>Ohne Datenbank, ohne Uhr, ohne Zufall.</b> Die Fälle vergleichen Zahlen und
    /// Kennwörter, keine Ressourcentexte — sie brauchen deshalb keine Kulturpinnung; die
    /// <c>InvariantCulture</c> von Leser und Schreiber ist selbst Gegenstand der Prüfung.</para>
    /// </summary>
    public class KonditionierungKalendermodellTests
    {
        private const int JAHR = 2025;

        /// <summary>Eine Woche mit einem festen Wert in jeder Zelle.</summary>
        private static double[] Woche(double wert)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int i = 0; i < w.Length; i++) w[i] = wert;
            return w;
        }

        private static string Text(IReadOnlyList<double> w, Konditionierungsgroesse g)
            => Kalenderwoche.Schreiben(w, g);

        // =============================================================================
        //  Der strenge Leser und Schreiber der Woche (5.2)
        // =============================================================================

        [Fact]
        public void Der_Rundlauf_der_Woche_ist_bitgleich_auch_mit_aus_und_vier_Nachkommastellen()
        {
            double[] w = Woche(20.0);
            w[0] = double.NaN;                 // "aus"
            w[1] = 18.5;
            w[2] = 20.1234;                    // genau vier Nachkommastellen
            w[3] = 0.0;
            w[167] = 30.0;

            string t = Text(w, Konditionierungsgroesse.Heizsoll);
            Assert.StartsWith(DbWerte.KOND_WOCHE_AUS + ";18.5;20.1234;0;", t, StringComparison.Ordinal);
            Assert.DoesNotContain(",", t, StringComparison.Ordinal);       // InvariantCulture

            Wochenlesung l = Kalenderwoche.Lesen(t, Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Wochenbefund.Gelesen, l.Befund);
            for (int i = 0; i < w.Length; i++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(w[i]), BitConverter.DoubleToInt64Bits(l.Werte[i]));
        }

        [Fact]
        public void Der_Schreiber_setzt_kein_Minus_Null_und_kein_Komma()
        {
            Assert.Equal("0", Kalenderwoche.Text(-0.0));
            Assert.Equal("0", Kalenderwoche.Text(0.0));
            Assert.Equal("1.5", Kalenderwoche.Text(1.5));
            Assert.Equal(DbWerte.KOND_WOCHE_AUS, Kalenderwoche.Text(double.NaN));
        }

        [Fact]
        public void Der_Rundlauf_lehnt_eine_fuenfte_Nachkommastelle_ab()
        {
            Assert.True(Kalenderwoche.Rundlauf(20.0));
            Assert.True(Kalenderwoche.Rundlauf(20.1234));
            Assert.True(Kalenderwoche.Rundlauf(double.NaN));
            Assert.False(Kalenderwoche.Rundlauf(20.12345));
            Assert.False(Kalenderwoche.Rundlauf(1.0 / 3.0));
            Assert.False(Kalenderwoche.Rundlauf(double.PositiveInfinity));
        }

        [Theory]
        [InlineData(167)]
        [InlineData(169)]
        public void Nicht_genau_168_Zellen_sind_ein_benannter_Befund(int zahl)
        {
            var teile = new string[zahl];
            for (int i = 0; i < zahl; i++) teile[i] = "20";
            Wochenlesung l = Kalenderwoche.Lesen(string.Join(";", teile), Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Wochenbefund.FalscheWertzahl, l.Befund);
            Assert.Equal(zahl, l.Gefunden);
            Assert.Null(l.Werte);
        }

        [Fact]
        public void Eine_Zelle_die_keine_Zahl_und_kein_aus_ist_nennt_ihre_Stelle()
        {
            double[] w = Woche(20.0);
            string t = Text(w, Konditionierungsgroesse.Heizsoll);
            string[] teile = t.Split(';');
            teile[42] = "warm";
            Wochenlesung l = Kalenderwoche.Lesen(string.Join(";", teile), Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Wochenbefund.KeineZahl, l.Befund);
            Assert.Equal(43, l.Stelle);
        }

        [Fact]
        public void Eine_Zelle_ausserhalb_der_Grenzen_ihrer_Groesse_nennt_ihre_Stelle()
        {
            double[] w = Woche(0.4);
            string t = Text(w, Konditionierungsgroesse.Lueftung);
            string[] teile = t.Split(';');
            teile[7] = "25";                   // die Lüftung reicht bis 20 1/h
            Wochenlesung l = Kalenderwoche.Lesen(string.Join(";", teile), Konditionierungsgroesse.Lueftung);
            Assert.Equal(Wochenbefund.WertAusserhalb, l.Befund);
            Assert.Equal(8, l.Stelle);
        }

        [Fact]
        public void Ein_leerer_Text_heisst_keine_Woche_und_die_Grundangabe_gilt()
        {
            foreach (string t in new[] { null, "", "   " })
                Assert.Equal(Wochenbefund.KeineWoche,
                             Kalenderwoche.Lesen(t, Konditionierungsgroesse.Heizsoll).Befund);
        }

        [Fact]
        public void Der_Leser_nimmt_Leerraum_und_jede_Schreibweise_von_aus()
        {
            double[] w = Woche(20.0);
            string[] teile = Text(w, Konditionierungsgroesse.Heizsoll).Split(';');
            teile[0] = " aus ";
            teile[1] = "AUS";
            teile[2] = " 18.5 ";
            Wochenlesung l = Kalenderwoche.Lesen(string.Join(";", teile), Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Wochenbefund.Gelesen, l.Befund);
            Assert.True(double.IsNaN(l.Werte[0]));
            Assert.True(double.IsNaN(l.Werte[1]));
            Assert.Equal(18.5, l.Werte[2]);
        }

        [Fact]
        public void Der_Schreiber_lehnt_eine_falsche_Zellzahl_und_einen_Wert_ausserhalb_ab()
        {
            Assert.Throws<ArgumentNullException>(() => Kalenderwoche.Schreiben(null, Konditionierungsgroesse.Heizsoll));
            Assert.Throws<ArgumentException>(() =>
                Kalenderwoche.Schreiben(new double[167], Konditionierungsgroesse.Heizsoll));
            double[] w = Woche(20.0);
            w[3] = 99.0;
            Assert.Throws<ArgumentException>(() => Kalenderwoche.Schreiben(w, Konditionierungsgroesse.Heizsoll));
        }

        [Fact]
        public void Die_Woche_bleibt_unter_der_Zeichengrenze_der_Spalte()
        {
            double[] w = Woche(20.1234);
            Assert.True(Text(w, Konditionierungsgroesse.Heizsoll).Length <= KonditionierungSchema.WOCHE_MAX_ZEICHEN);
            // 168 x "20.1234" plus 167 Trennzeichen = 1343 Zeichen.
            Assert.Equal(168 * 7 + 167, Text(w, Konditionierungsgroesse.Heizsoll).Length);
        }

        // =============================================================================
        //  Die Kompilierung zur 8760-Reihe (3.2)
        // =============================================================================

        [Fact]
        public void Eine_Grundangabe_liefert_8760_gleiche_Werte_und_aus_den_Wert_der_Groesse()
        {
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                var wert = new Konditionierungskalender(g, Kalenderangabe.AusWert(
                    Konditionierungsgroessen.Min(g)), null, null);
                double[] r = wert.Auswerten(2, JAHR);
                Assert.Equal(8760, r.Length);
                foreach (double v in r) Assert.Equal(Konditionierungsgroessen.Min(g), v);

                var aus = new Konditionierungskalender(g, Kalenderangabe.Abgeschaltet, null, null);
                double[] ra = aus.Auswerten(2, JAHR);
                double erwartet = Konditionierungsgroessen.AusWert(g);
                foreach (double v in ra)
                    Assert.Equal(BitConverter.DoubleToInt64Bits(erwartet), BitConverter.DoubleToInt64Bits(v));
            }
        }

        [Fact]
        public void Der_Wochentag_folgt_w0_plus_Tag_modulo_sieben()
        {
            // Eine Woche, deren Zelle den Wochentag verrät: w * 24 + s als Wert (0 … 167).
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int i = 0; i < w.Length; i++) w[i] = i / 24.0;         // 0 … 6,958
            var k = new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                 Kalenderangabe.AusWoche(w), null, null);
            for (int w0 = 0; w0 < 7; w0++)
            {
                double[] r = k.Auswerten(w0, JAHR);
                for (int d = 0; d < 365; d++)
                    Assert.Equal((w0 + d) % 7, (int)Math.Floor(r[d * 24]));
            }
        }

        [Fact]
        public void Die_ranghoechste_Periode_gewinnt_und_die_Vorschau_nennt_sie()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                Kalenderangabe.AusWert(20.0), null, new[]
                {
                    Kalenderregel.Zeitraum(100, DbWerte.KOND_ART_ZEITRAUM, "schwach", 100, 200,
                                           Kalenderangabe.AusWert(18.0)),
                    Kalenderregel.Zeitraum(300, DbWerte.KOND_ART_FERIEN, "stark", 150, 160,
                                           Kalenderangabe.AusWert(16.0)),
                    Kalenderregel.Zeitraum(900, DbWerte.KOND_ART_BETRIEBSPAUSE, "Pause", 155, 156,
                                           Kalenderangabe.Abgeschaltet),
                });
            double[] r = k.Auswerten(0, JAHR);
            Assert.Equal(20.0, r[50 * 24]);                       // keine Periode
            Assert.Equal(18.0, r[110 * 24]);                      // Rang 100
            Assert.Equal(16.0, r[152 * 24]);                      // Rang 300 über Rang 100
            Assert.True(double.IsNaN(r[154 * 24]));               // Rang 900 über allem (Tag 155)
            Assert.Null(k.Quelle(50, JAHR));
            Assert.Equal("schwach", k.Quelle(110, JAHR));
            Assert.Equal("stark", k.Quelle(152, JAHR));
            Assert.Equal("Pause", k.Quelle(154, JAHR));
        }

        [Fact]
        public void Eine_Periode_ueber_den_Jahreswechsel_enthaelt_beide_Enden()
        {
            Kalenderregel r = Kalenderregel.Zeitraum(500, DbWerte.KOND_ART_ZEITRAUM, "Winter", 350, 10,
                                                     Kalenderangabe.AusWert(16.0));
            Assert.True(r.UeberJahreswechsel);
            Assert.True(r.Enthaelt(349, -1));       // Tag 350
            Assert.True(r.Enthaelt(364, -1));       // Tag 365
            Assert.True(r.Enthaelt(0, -1));         // Tag 1
            Assert.True(r.Enthaelt(9, -1));         // Tag 10
            Assert.False(r.Enthaelt(10, -1));       // Tag 11
            Assert.False(r.Enthaelt(348, -1));      // Tag 349
        }

        [Fact]
        public void Ein_Ausnahmetag_ist_eine_Periode_von_einem_Tag()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                Kalenderangabe.AusWert(20.0), null, new[]
                {
                    Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM, "Brückentag", 123, 123,
                                           Kalenderangabe.Abgeschaltet),
                });
            double[] r = k.Auswerten(0, JAHR);
            Assert.Equal(20.0, r[121 * 24]);
            for (int s = 0; s < 24; s++) Assert.True(double.IsNaN(r[122 * 24 + s]));
            Assert.Equal(20.0, r[123 * 24]);
        }

        // =============================================================================
        //  Feiertage als Regel (F11)
        // =============================================================================

        [Theory]
        [InlineData(2024, 3, 31)]
        [InlineData(2025, 4, 20)]
        [InlineData(2026, 4, 5)]
        [InlineData(2027, 3, 28)]
        [InlineData(2000, 4, 23)]
        public void Das_Osterdatum_ist_eine_Rechenvorschrift(int jahr, int monat, int tag)
        {
            DateTime o = Feiertage.Ostersonntag(jahr);
            Assert.Equal(monat, o.Month);
            Assert.Equal(tag, o.Day);
            Assert.Equal(DayOfWeek.Sunday, o.DayOfWeek);
        }

        [Fact]
        public void Die_neun_Regeln_loesen_sich_gegen_das_Referenzjahr_auf()
        {
            Assert.Equal(9, Feiertage.Regeln.Count);
            Assert.Equal(1, Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_NEUJAHR, Gemeinjahrkalender.Kalenderjahr(2025)));
            Assert.Equal(31 + 28 + 31 + 30 + 1, Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_ERSTER_MAI, Gemeinjahrkalender.Kalenderjahr(2025)));
            Assert.Equal(359, Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_WEIHNACHTEN_1, Gemeinjahrkalender.Kalenderjahr(2025)));
            Assert.Equal(360, Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_WEIHNACHTEN_2, Gemeinjahrkalender.Kalenderjahr(2025)));
            Assert.Equal(-1, Feiertage.Jahrestag("ROSENMONTAG", Gemeinjahrkalender.Kalenderjahr(2025)));

            // Karfreitag 2025 = 18.4. (Tag 108), 2024 = 29.3. (Tag 88) - das Jahr verschiebt ihn.
            Assert.Equal(Feiertage.Gemeinjahrestag(4, 18), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_KARFREITAG, Gemeinjahrkalender.Kalenderjahr(2025)));
            Assert.Equal(Feiertage.Gemeinjahrestag(3, 29), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_KARFREITAG, Gemeinjahrkalender.Kalenderjahr(2024)));
        }

        [Fact]
        public void Eine_Feiertagsregel_wirkt_in_der_Reihe_und_wandert_mit_dem_Jahr()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                Kalenderangabe.AusWert(20.0), null, new[]
                {
                    Kalenderregel.Feiertag(700, "Karfreitag", DbWerte.KOND_FEIERTAG_KARFREITAG,
                                           Kalenderangabe.AusWert(16.0)),
                });
            int t2025 = Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_KARFREITAG, Gemeinjahrkalender.Kalenderjahr(2025)) - 1;
            int t2024 = Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_KARFREITAG, Gemeinjahrkalender.Kalenderjahr(2024)) - 1;
            Assert.NotEqual(t2025, t2024);

            double[] r25 = k.Auswerten(2, 2025);
            Assert.Equal(16.0, r25[t2025 * 24]);
            Assert.Equal(20.0, r25[t2024 * 24]);

            double[] r24 = k.Auswerten(0, 2024);
            Assert.Equal(16.0, r24[t2024 * 24]);
            Assert.Equal(20.0, r24[t2025 * 24]);
        }

        [Fact]
        public void Wie_Wochentag_nimmt_die_Stunden_dieses_Tags_aus_der_Standardwoche()
        {
            // Montag bis Freitag 20 Grad, Samstag 17, Sonntag 15.
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[Kalenderwoche.Stelle(wt, s)] = wt == 6 ? 15.0 : wt == 5 ? 17.0 : 20.0;

            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                Kalenderangabe.AusWoche(w), null, new[]
                {
                    Kalenderregel.Feiertag(700, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                           Kalenderangabe.AlsWochentag(7)),      // wie Sonntag
                });
            double[] r = k.Auswerten(2, 2025);       // 1.1.2025 war ein Mittwoch (w0 = 2)
            Assert.Equal(15.0, r[0]);                // Neujahr rechnet wie Sonntag
            Assert.Equal(20.0, r[24]);               // 2. Januar: Donnerstag

            // OHNE Standardwoche greift die Angabe nicht - der Aufrufer faellt zurueck.
            var ohne = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                Kalenderangabe.AusWert(20.0), null, new[]
                {
                    Kalenderregel.Feiertag(700, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                           Kalenderangabe.AlsWochentag(7)),
                });
            Assert.Equal(20.0, ohne.Auswerten(2, 2025)[0]);
        }

        // =============================================================================
        //  Die Grenzen des Modells (3.6)
        // =============================================================================

        [Fact]
        public void Ein_doppelter_Rang_und_zu_viele_Perioden_sind_Ausnahmen()
        {
            Kalenderangabe a = Kalenderangabe.AusWert(18.0);
            Assert.Throws<ArgumentException>(() => new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, new[]
                {
                    Kalenderregel.Zeitraum(100, DbWerte.KOND_ART_ZEITRAUM, "a", 1, 2, a),
                    Kalenderregel.Zeitraum(100, DbWerte.KOND_ART_ZEITRAUM, "b", 3, 4, a),
                }));

            var viele = new List<Kalenderregel>();
            for (int i = 1; i <= Kalenderregel.PERIODEN_MAX + 1; i++)
                viele.Add(Kalenderregel.Zeitraum(i, DbWerte.KOND_ART_ZEITRAUM, "p" + i, i, i, a));
            Assert.Throws<ArgumentException>(() => new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, viele));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(366)]
        public void Ein_Tag_ausserhalb_1_bis_365_ist_eine_Ausnahme(int tag)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderregel.Zeitraum(
                100, DbWerte.KOND_ART_ZEITRAUM, "a", tag, 10, Kalenderangabe.AusWert(18.0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderregel.Zeitraum(
                100, DbWerte.KOND_ART_ZEITRAUM, "a", 10, tag, Kalenderangabe.AusWert(18.0)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1000)]
        public void Ein_Rang_ausserhalb_1_bis_999_ist_eine_Ausnahme(int rang)
            => Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderregel.Zeitraum(
                   rang, DbWerte.KOND_ART_ZEITRAUM, "a", 1, 2, Kalenderangabe.AusWert(18.0)));

        [Fact]
        public void Eine_unbekannte_Feiertagsregel_ist_eine_Ausnahme()
            => Assert.Throws<ArgumentException>(() => Kalenderregel.Feiertag(
                   100, "a", "ROSENMONTAG", Kalenderangabe.AusWert(18.0)));

        [Fact]
        public void Wie_Wochentag_ausserhalb_1_bis_7_ist_eine_Ausnahme()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderangabe.AlsWochentag(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderangabe.AlsWochentag(8));
        }

        [Fact]
        public void Wie_Wochentag_gibt_es_nur_an_einer_Periode_nicht_als_Grundangabe()
            => Assert.Throws<ArgumentException>(() => new Konditionierungskalender(
                   Konditionierungsgroesse.Heizsoll, Kalenderangabe.AlsWochentag(7), null, null));

        [Fact]
        public void Ein_Nennwert_steht_nur_an_Geraeten_und_Personen()
        {
            Assert.Throws<ArgumentException>(() => new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), 500.0, null));
            Assert.Equal(500.0, new Konditionierungskalender(Konditionierungsgroesse.Personen,
                Kalenderangabe.AusWert(1.0), 500.0, null).Nennwert);
        }

        // =============================================================================
        //  Der Zeilenleser und seine benannten Fehlerbilder (3.6)
        // =============================================================================

        private static Kalenderzeile Zeile(double? wert = null, bool aus = false, string woche = null,
                                          double? nennwert = null,
                                          string groesse = DbWerte.KOND_GROESSE_HEIZSOLL)
            => new Kalenderzeile { Id = 1, IdGebaeude = 7, Groesse = groesse, Wert = wert, Aus = aus,
                                   Woche = woche, Nennwert = nennwert };

        [Fact]
        public void Der_Zeilenleser_liest_die_drei_Grundangaben()
        {
            Assert.Equal(Angabeart.Wert, Kalenderleser.Lesen(Zeile(wert: 20.0), null).Kalender.Grundangabe.Art);
            Assert.Equal(Angabeart.Aus, Kalenderleser.Lesen(Zeile(aus: true), null).Kalender.Grundangabe.Art);
            Assert.Equal(Angabeart.Woche,
                         Kalenderleser.Lesen(Zeile(woche: Text(Woche(20.0), Konditionierungsgroesse.Heizsoll)), null)
                                      .Kalender.Grundangabe.Art);
        }

        [Theory]
        [InlineData(null, false, null)]                 // keine Angabe
        [InlineData(20.0, true, null)]                  // Wert und "aus"
        public void Nicht_genau_eine_Grundangabe_ist_ein_benannter_Befund(double? wert, bool aus, string woche)
            => Assert.Equal(Kalenderbefund.AngabeNichtEindeutig,
                            Kalenderleser.Lesen(Zeile(wert, aus, woche), null).Befund);

        [Fact]
        public void Eine_unbekannte_Groesse_und_ein_Wert_ausserhalb_sind_benannte_Befunde()
        {
            Assert.Equal(Kalenderbefund.GroesseUnbekannt,
                         Kalenderleser.Lesen(Zeile(wert: 20.0, groesse: "LICHT"), null).Befund);
            Assert.Equal(Kalenderbefund.WertAusserhalb,
                         Kalenderleser.Lesen(Zeile(wert: 99.0), null).Befund);
            Assert.Equal(Kalenderbefund.NennwertUngueltig,
                         Kalenderleser.Lesen(Zeile(wert: 20.0, nennwert: 500.0), null).Befund);
            Assert.Equal(Kalenderbefund.NennwertUngueltig,
                         Kalenderleser.Lesen(Zeile(wert: 1.0, nennwert: -1.0,
                                                   groesse: DbWerte.KOND_GROESSE_PERSONEN), null).Befund);
        }

        [Fact]
        public void Eine_ungueltige_Woche_nennt_Wochenbefund_und_Stelle()
        {
            Kalenderlesung l = Kalenderleser.Lesen(Zeile(woche: "20;20;20"), null);
            Assert.Equal(Kalenderbefund.WocheUngueltig, l.Befund);
            Assert.Equal(Wochenbefund.FalscheWertzahl, l.Wochenbefund);
            Assert.Contains("FalscheWertzahl", l.Fundstelle(), StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Zeilenleser_liest_Perioden_und_uebergeht_fremde_Kalender()
        {
            var perioden = new List<Periodenzeile>
            {
                new Periodenzeile { Id = 1, IdKalender = 1, Rang = 200, Art = DbWerte.KOND_ART_FERIEN,
                                    Bezeichner = "Sommer", Beginn = 180, Ende = 210, Wert = 16.0 },
                new Periodenzeile { Id = 2, IdKalender = 99, Rang = 300, Art = DbWerte.KOND_ART_ZEITRAUM,
                                    Bezeichner = "fremd", Beginn = 1, Ende = 2, Wert = 12.0 },
                new Periodenzeile { Id = 3, IdKalender = 1, Rang = 700, Art = DbWerte.KOND_ART_FEIERTAG,
                                    Bezeichner = "Neujahr", Feiertagsregel = DbWerte.KOND_FEIERTAG_NEUJAHR,
                                    WieWochentag = 7 },
            };
            Kalenderlesung l = Kalenderleser.Lesen(Zeile(wert: 20.0), perioden);
            Assert.Equal(Kalenderbefund.Gelesen, l.Befund);
            Assert.Equal(2, l.Kalender.Perioden.Count);
            Assert.Equal(700, l.Kalender.Perioden[0].Rang);      // absteigend nach Rang
        }

        [Theory]
        [InlineData(0, DbWerte.KOND_ART_ZEITRAUM, 1, 2, null, Kalenderbefund.RangUngueltig)]
        [InlineData(200, "URLAUB", 1, 2, null, Kalenderbefund.ArtUnbekannt)]
        [InlineData(200, DbWerte.KOND_ART_ZEITRAUM, 1, null, null, Kalenderbefund.ZeitraumUngueltig)]
        [InlineData(200, DbWerte.KOND_ART_ZEITRAUM, null, null, null, Kalenderbefund.ZeitraumUngueltig)]
        [InlineData(200, DbWerte.KOND_ART_ZEITRAUM, 1, 2, DbWerte.KOND_FEIERTAG_NEUJAHR,
                    Kalenderbefund.ZeitraumUngueltig)]
        [InlineData(200, DbWerte.KOND_ART_ZEITRAUM, null, null, DbWerte.KOND_FEIERTAG_NEUJAHR,
                    Kalenderbefund.ZeitraumUngueltig)]
        [InlineData(200, DbWerte.KOND_ART_FEIERTAG, null, null, "ROSENMONTAG",
                    Kalenderbefund.ZeitraumUngueltig)]
        public void Eine_ungueltige_Periode_ist_ein_benannter_Befund(int rang, string art, int? beginn,
                                                                    int? ende, string regel,
                                                                    Kalenderbefund erwartet)
        {
            var p = new List<Periodenzeile>
            {
                new Periodenzeile { Id = 1, IdKalender = 1, Rang = rang, Art = art, Bezeichner = "p",
                                    Beginn = beginn, Ende = ende, Feiertagsregel = regel, Wert = 16.0 },
            };
            Assert.Equal(erwartet, Kalenderleser.Lesen(Zeile(wert: 20.0), p).Befund);
        }

        [Fact]
        public void Der_Zeilenschreiber_ist_das_Gegenstueck_des_Lesers()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Personen,
                Kalenderangabe.AusWoche(Woche(0.5)), 350.0, new[]
                {
                    Kalenderregel.Zeitraum(200, DbWerte.KOND_ART_FERIEN, "Sommer", 180, 210,
                                           Kalenderangabe.Abgeschaltet),
                    Kalenderregel.Feiertag(700, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                           Kalenderangabe.AlsWochentag(7)),
                });
            var zeile = new Kalenderzeile { Id = 5, IdGebaeude = 7 };
            var perioden = new List<Periodenzeile>();
            Kalenderleser.Schreiben(k, zeile, perioden);

            Assert.Equal(DbWerte.KOND_GROESSE_PERSONEN, zeile.Groesse);
            Assert.Equal(350.0, zeile.Nennwert);
            Assert.NotNull(zeile.Woche);
            Assert.Null(zeile.Wert);
            Assert.False(zeile.Aus);
            Assert.Equal(2, perioden.Count);
            foreach (Periodenzeile p in perioden) Assert.Equal(5, p.IdKalender);

            // Zurueckgelesen ist es derselbe Kalender.
            Kalenderlesung l = Kalenderleser.Lesen(zeile, perioden);
            Assert.Equal(Kalenderbefund.Gelesen, l.Befund);
            double[] a = k.Auswerten(2, JAHR), b = l.Kalender.Auswerten(2, JAHR);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(a[h]), BitConverter.DoubleToInt64Bits(b[h]));
        }

        // =============================================================================
        //  Vererbung Gebäude → Zone je Zelle (F2)
        // =============================================================================

        [Fact]
        public void Eine_leere_Zonenzelle_heisst_wie_das_Gebaeude()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = 16.0;
            g.Nachtabsenkung_Beginn = 21;
            g.Nachtabsenkung_Ende = 5;
            Matrixeingang gb = Konditionierungseingang.Bestand(g, false, false);
            Vorgabematrix gebaeude = Vorgabematrix.Bilden(gb, null);

            // Die Zone traegt NUR den Tagwert; alles andere erbt sie.
            var zone = new Matrixeingang { SollTag = 22.0 };
            Vorgabematrix wirksam = Vorgabematrix.Bilden(zone, null).Erben(gebaeude);

            Assert.Equal(22.0, wirksam.Heizsoll.Tag.Wert);                        // eigene Zelle
            Assert.Equal(g.Raumsolltemperatur_Nachtabsenkung, wirksam.Heizsoll.Nacht.Wert);   // geerbt
            Assert.Equal(16.0, wirksam.Heizsoll.Wochenende.Wert);                 // geerbt
            Assert.Equal(21, wirksam.Heizsoll.Nacht.Von);                         // Nachtfenster geerbt
            Assert.Equal(5, wirksam.Heizsoll.Nacht.Bis);
        }

        [Fact]
        public void Die_Ferienzeitraeume_und_die_Merker_kommen_immer_vom_Gebaeude()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Ferien = 1.0;
            g.Raumsolltemperatur_Ferien = 12.0;
            g.Ferienbeginn_1 = 180.0;
            g.Ferienende_1 = 200.0;
            Matrixeingang gb = Konditionierungseingang.Bestand(g, false, false);

            var zone = new Matrixeingang { SollTag = 22.0 };
            zone.Ferienbeginn[0] = 1.0;        // die Zone traegt etwas - es gilt NICHT
            Matrixeingang wirksam = zone.Erben(gb);

            Assert.Equal(180.0, wirksam.Ferienbeginn[0]);
            Assert.Equal(200.0, wirksam.Ferienende[0]);
            Assert.Equal(1.0, wirksam.Ferienmerker);
        }

        [Fact]
        public void Die_Kaskade_wirkt_je_Zelle_und_je_Feld()
        {
            Matrixzelle geb = Matrixzelle.AusWert(2.0, 22, 6, 2.0);
            Matrixzelle zone = Matrixzelle.NurZeiten(20, null);
            Matrixzelle wirksam = zone.Erben(geb);
            Assert.True(wirksam.Belegt);
            Assert.Equal(2.0, wirksam.Wert);      // Wert geerbt
            Assert.Equal(20, wirksam.Von);        // Von eigen
            Assert.Equal(6, wirksam.Bis);         // Bis geerbt
            Assert.Equal(2.0, wirksam.BedingtK);  // geerbt

            // "aus" der Zone schlaegt den Wert des Gebaeudes.
            Assert.True(Matrixzelle.Abgeschaltet().Erben(geb).Aus);
            // Eine leere Zone erbt alles.
            Assert.Equal(2.0, Matrixzelle.Leer.Erben(geb).Wert);
        }

        // =============================================================================
        //  Die Eigentümerregel als Funktion (5.1)
        // =============================================================================

        [Theory]
        [InlineData(7L, null, null, null, true, Kalendereigentuemer.Gebaeude)]
        [InlineData(7L, 3L, null, null, true, Kalendereigentuemer.Zone)]
        [InlineData(null, null, 5L, null, true, Kalendereigentuemer.Katalogbau)]
        [InlineData(null, null, null, 9L, true, Kalendereigentuemer.Vorlage)]
        [InlineData(7L, null, 5L, null, false, Kalendereigentuemer.Gebaeude)]
        [InlineData(null, 3L, null, null, false, Kalendereigentuemer.Gebaeude)]
        [InlineData(null, null, null, null, false, Kalendereigentuemer.Gebaeude)]
        public void Die_Eigentuemerregel_gilt_Wort_fuer_Wort_wie_der_CHECK(long? geb, long? zone,
                                                                          long? stamm, long? vorlage,
                                                                          bool gueltig,
                                                                          Kalendereigentuemer erwartet)
        {
            bool ok = Kalendereigentuemerregel.Bestimmen(geb, zone, stamm, vorlage,
                                                         out Kalendereigentuemer e);
            Assert.Equal(gueltig, ok);
            if (gueltig) Assert.Equal(erwartet, e);
        }

        // =============================================================================
        //  Die Kennwörter sind eingefroren (Persistenzwerte)
        // =============================================================================

        [Fact]
        public void Die_Kennwoerter_sind_ASCII_und_eingefroren()
        {
            Assert.Equal(new[] { "HEIZSOLL", "KUEHLSOLL", "LUEFTUNG", "GERAETE", "PERSONEN" },
                         DbWerte.KOND_GROESSEN);
            Assert.Equal(new[] { "ZEITRAUM", "FERIEN", "FEIERTAG", "BETRIEBSPAUSE" }, DbWerte.KOND_ARTEN);
            Assert.Equal(new[] { "NENNWERT", "TAG", "NACHT", "WOCHENENDE", "FERIEN", "SAISON" },
                         DbWerte.KOND_ZEILEN);
            Assert.Equal("aus", DbWerte.KOND_WOCHE_AUS);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Assert.True(Konditionierungsgroessen.AusKennwort(Konditionierungsgroessen.Kennwort(g),
                                                                 out Konditionierungsgroesse zurueck));
                Assert.Equal(g, zurueck);
            }
            Assert.False(Konditionierungsgroessen.AusKennwort("LICHT", out _));
        }

        [Fact]
        public void Aus_bedeutet_je_Groesse_das_Richtige()
        {
            Assert.True(double.IsNaN(Konditionierungsgroessen.AusWert(Konditionierungsgroesse.Heizsoll)));
            Assert.True(double.IsPositiveInfinity(
                Konditionierungsgroessen.AusWert(Konditionierungsgroesse.Kuehlsoll)));
            Assert.Equal(0.0, Konditionierungsgroessen.AusWert(Konditionierungsgroesse.Lueftung));
            Assert.Equal(0.0, Konditionierungsgroessen.AusWert(Konditionierungsgroesse.Geraete));
            Assert.Equal(0.0, Konditionierungsgroessen.AusWert(Konditionierungsgroesse.Personen));
        }

        [Fact]
        public void Das_Gemeinjahr_hat_365_Tage_und_die_Umrechnung_laeuft_rund()
        {
            int summe = 0;
            foreach (int t in Feiertage.TageJeMonat) summe += t;
            Assert.Equal(365, summe);
            for (int tag = 1; tag <= 365; tag++)
            {
                Assert.True(Feiertage.Datum(tag, out int m, out int d));
                Assert.Equal(tag, Feiertage.Gemeinjahrestag(m, d));
            }
            Assert.False(Feiertage.Datum(0, out _, out _));
            Assert.False(Feiertage.Datum(366, out _, out _));
            Assert.Equal(-1, Feiertage.Gemeinjahrestag(2, 29));      // im Gemeinjahr gibt es ihn nicht
        }
    }
}
