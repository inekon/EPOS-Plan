using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die fünf Reihen der Konditionierung im Stundenmodell</b> (Stufe KP1, Konzept
    /// Konditionierungsprofile 6): Heizsollwert, Kühlsollwert, Lüftung, Geräte und Personen als
    /// Stundenreihen statt als Konstante — und die <b>Bauvorschrift der Byte-Gleichheit</b>: Ohne
    /// Kalender nimmt der Eingang wörtlich den Bestandszweig.
    ///
    /// <para>Ohne Datenbank: Die Kalender entstehen von Hand, und der Eingangsbauer bekommt sie als
    /// <see cref="Konditionierungssatz"/> — genau wie im Lauf, wo der Datenweg sie liest.</para>
    /// </summary>
    public class KonditionierungReihenTests
    {
        private const int JAHR = 2025;

        private static int W0() => GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende());

        private static Konditionierungssatz Satz() => new Konditionierungssatz(W0(), JAHR);

        /// <summary>Ein Kalender mit einem festen Wert über alle Stunden.</summary>
        private static Konditionierungskalender Konstant(Konditionierungsgroesse g, double wert,
                                                         double? nennwert = null)
            => new Konditionierungskalender(g, Kalenderangabe.AusWert(wert), nennwert, null);

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, Konditionierungssatz satz,
                                                    bool kuehlbetrieb = false)
            => GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                           Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb,
                                           konditionierung: satz);

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[h]) == BitConverter.DoubleToInt64Bits(b[h]),
                            was + ": Stunde " + h + " — " + a[h].ToString("G17", CultureInfo.InvariantCulture) +
                            " statt " + b[h].ToString("G17", CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Die Bauvorschrift der Byte-Gleichheit
        // =============================================================================

        [Fact]
        public void Ohne_Satz_und_mit_leerem_Satz_rechnet_der_Eingang_woertlich_wie_bisher()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang ohne = Eingang(g, null);
            GebaeudeModellEingang leer = Eingang(g, Satz());

            Assert.Null(ohne.Konditionierung);
            Assert.False(leer.Konditionierung.Wirksam);
            Assert.Null(ohne.LueftungZusatzleitwertWK);
            Assert.Null(leer.LueftungZusatzleitwertWK);
            Assert.Null(ohne.InnereGewinneReihe_W);
            Assert.Equal(0, ohne.StundenOhneHeizungH);

            Bitgleich(ohne.ThetaSoll, leer.ThetaSoll, "ThetaSoll");
            Bitgleich(ohne.ThetaMax, leer.ThetaMax, "ThetaMax");
            Bitgleich(ohne.PhiConv, leer.PhiConv, "PhiConv");
            Bitgleich(ohne.PhiRadAW, leer.PhiRadAW, "PhiRadAW");
            Bitgleich(ohne.PhiRadIW, leer.PhiRadIW, "PhiRadIW");
            Assert.Equal(ohne.Luftwechselrate_h, leer.Luftwechselrate_h);
            Assert.Equal(ohne.SommerlueftungZusatzleitwertWK, leer.SommerlueftungZusatzleitwertWK);
        }

        [Fact]
        public void Ein_konstanter_Kalender_liefert_dieselbe_Reihe_wie_der_Bestandswert()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang ohne = Eingang(g, null);

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll,
                     Konstant(Konditionierungsgroesse.Heizsoll, g.Raumsolltemperatur_Tag));
            GebaeudeModellEingang mit = Eingang(g, s);

            // Der Bestandsfahrplan senkt nachts ab, der konstante Kalender nicht - aber der Tagwert
            // muss Stunde fuer Stunde gleich sein.
            for (int h = 0; h < 8760; h++)
                if (ohne.Nutzungszeit(h))
                    Assert.Equal(BitConverter.DoubleToInt64Bits(ohne.ThetaSoll[h]),
                                 BitConverter.DoubleToInt64Bits(mit.ThetaSoll[h]));
            Assert.Equal(0, mit.StundenOhneHeizungH);
        }

        // =============================================================================
        //  Heizsollwert und Heizperiode (E53)
        // =============================================================================

        [Fact]
        public void Aus_im_Heizkalender_heisst_NaN_und_die_Stunde_rechnet_ohne_Heizung()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll,
                     new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                  Kalenderangabe.Abgeschaltet, null, null));
            GebaeudeModellEingang e = Eingang(g, s);

            Assert.Equal(8760, e.StundenOhneHeizungH);
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(double.IsNaN(e.ThetaSoll[h]));
                Assert.False(e.Rand(h).MitHeizung);
            }
        }

        [Fact]
        public void Die_Heizperiode_schaltet_ausserhalb_aus_und_innerhalb_gilt_die_Woche()
        {
            // Heizperiode 1.10. (Tag 274) bis 30.4. (Tag 120) - ueber den Jahreswechsel.
            var matrix = MatrixMitSaison(274, 120);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            Assert.True(l.Kalender.TraegtAus());

            double[] r = l.Kalender.Auswerten(W0(), JAHR);
            // Innerhalb (Januar, Tag 0) wird geheizt, ausserhalb (Juni, Tag 160) nicht.
            Assert.False(double.IsNaN(r[0 * 24 + 12]));
            Assert.True(double.IsNaN(r[160 * 24 + 12]));
            // Die Grenzen gelten GANZE TAGE: Tag 120 (Index 119) noch heizen, Tag 121 (Index 120) aus.
            Assert.False(double.IsNaN(r[119 * 24 + 23]));
            Assert.True(double.IsNaN(r[120 * 24 + 0]));
            Assert.True(double.IsNaN(r[272 * 24 + 23]));
            Assert.False(double.IsNaN(r[273 * 24 + 0]));

            // 274 … 365 und 1 … 120 sind 92 + 120 = 212 Heiztage, 153 Tage aus.
            int aus = 0;
            for (int h = 0; h < 8760; h++) if (double.IsNaN(r[h])) aus++;
            Assert.Equal(153 * 24, aus);
        }

        [Fact]
        public void Eine_leere_Saison_heisst_ganzjaehrig_und_ein_ganzes_Jahr_ebenso()
        {
            foreach (var (von, bis) in new (int?, int?)[] { (null, null), (1, 365) })
            {
                Vorgabematrix m = von.HasValue ? MatrixMitSaison(von.Value, bis.Value) : MatrixOhneSaison();
                Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, true);
                Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
                double[] r = l.Kalender.Auswerten(W0(), JAHR);
                for (int h = 0; h < 8760; h++) Assert.False(double.IsNaN(r[h]));
            }
        }

        [Theory]
        [InlineData(274, null)]
        [InlineData(null, 120)]
        public void Start_und_Ende_der_Saison_gibt_es_nur_zusammen(int? von, int? bis)
        {
            Matrixeingang b = Bestand();
            var vorgaben = new List<Vorgabezeile>
            {
                new Vorgabezeile
                {
                    IdGebaeude = 1, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                    Zeile = DbWerte.KOND_ZEILE_SAISON, Von = von, Bis = bis,
                },
            };
            Vorgabematrix m = Vorgabematrix.Bilden(b, vorgaben);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(m, Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.SaisonUngueltig, l.Befund);
        }

        private static Matrixeingang Bestand()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = 16.0;
            return Konditionierungseingang.Bestand(g, false, false);
        }

        private static Vorgabematrix MatrixOhneSaison() => Vorgabematrix.Bilden(Bestand(), null);

        private static Vorgabematrix MatrixMitSaison(int von, int bis)
            => Vorgabematrix.Bilden(Bestand(), new[]
            {
                new Vorgabezeile
                {
                    IdGebaeude = 1, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                    Zeile = DbWerte.KOND_ZEILE_SAISON, Von = von, Bis = bis,
                },
            });

        // =============================================================================
        //  Kühlsollwert (P7 a, P13 a) und die stündliche Prüfung (F17)
        // =============================================================================

        [Fact]
        public void Der_Kuehlkalender_tritt_an_die_Stelle_der_Konstante_und_aus_heisst_unendlich()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(26.0);
            Konditionierungssatz s = Satz();
            // Tag 26 Grad, Nacht "aus" - die Zone schwingt nachts nach oben frei (E32).
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = st >= 7 && st < 19 ? 26.0 : double.NaN;
            s.Setzen(Konditionierungsgroesse.Kuehlsoll,
                     new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang), Vdi6007Probe.Wochenende(),
                Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE,
                kuehlbetrieb: true, konditionierung: s);

            Assert.Equal(26.0, e.ThetaMax[12]);
            Assert.True(double.IsPositiveInfinity(e.ThetaMax[3]));
            Assert.True(e.Rand(12).MitKuehlung);
            Assert.False(e.Rand(3).MitKuehlung);
        }

        [Fact]
        public void Die_stuendliche_Kuehlpruefung_lehnt_eine_Stunde_unter_dem_Heizsollwert_benannt_ab()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(26.0);
            Konditionierungssatz s = Satz();
            // Der Kuehlsollwert 20,5 Grad liegt nur 0,5 K ueber dem Tagsollwert 20 Grad.
            s.Setzen(Konditionierungsgroesse.Kuehlsoll, Konstant(Konditionierungsgroesse.Kuehlsoll, 20.5));
            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(() =>
                GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                            Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                            GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb: true,
                                            konditionierung: s));
            Assert.Equal(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert, ex.Grund);
        }

        // =============================================================================
        //  Lüftung (F15): Jahresminimum in R_ext, der Überschuss je Stunde
        // =============================================================================

        [Fact]
        public void Ein_konstanter_Lueftungskalender_hat_keinen_Zusatzleitwert()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Luftwechsel_Infiltration = 0.3;
            g.Luftwechsel_Nutzer = 0.4;
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     Konstant(Konditionierungsgroesse.Lueftung, 0.4, nennwert: null));
            // Der Nennwert (Infiltration) kommt nicht aus dem Kalender - Lueftung traegt keinen;
            // deshalb rechnet der Eingang mit 0 + 0,4 = 0,4 1/h.
            GebaeudeModellEingang e = Eingang(g, s);
            Assert.Equal(0.4, e.Luftwechselrate_h, 12);
            Assert.NotNull(e.LueftungZusatzleitwertWK);
            for (int h = 0; h < 8760; h++) Assert.Equal(0.0, e.LueftungZusatzleitwertWK[h]);
        }

        /// <summary>
        /// <b>Die Mechanik des Zusatzleitwerts</b> — Überschuss über dem Jahresminimum, nie negativ,
        /// und im Stundenrand.
        ///
        /// <para><b>Semantikwechsel nach P9 (Stufe KP1b):</b> Dieser Test hielt bis KP1a die
        /// <em>unbedingte</em> Wirkung eines Nachtüberschusses als zulässig fest. P9 (b) verbietet
        /// sie — eine Nachtauskühlung wirkt nur unter der Bedingung der Sommerlüftung. Das Muster
        /// ist deshalb „Werktag über Wochenende": ein Überschuss, der mit dem Nachtfenster nichts
        /// zu tun hat und damit nach Konzept 3.7 unbedingt bleibt. Die Nachtlüftung prüft
        /// <c>NachtauskuehlungTests</c> (N-NK2).</para>
        /// </summary>
        [Fact]
        public void Der_Ueberschuss_ueber_dem_Jahresminimum_laeuft_als_Zusatzleitwert_und_ist_nie_negativ()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = w >= 5 ? 0.4 : 2.0;   // Werktag ueber Wochenende
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            GebaeudeModellEingang e = Eingang(g, s);

            // Das Jahresminimum (der Wochenendwert) geht in R_ext.
            Assert.Equal(0.4, e.Luftwechselrate_h, 12);

            // Die Stellen der ersten Woche: Werktag ueber dem Minimum, Wochenende genau darauf.
            int w0 = W0();
            int werktag = -1, wochenende = -1;
            for (int h = 0; h < 168 && (werktag < 0 || wochenende < 0); h++)
            {
                int tag = (w0 + h / 24) % 7;
                if (tag < 5 && werktag < 0) werktag = h;
                if (tag >= 5 && wochenende < 0) wochenende = h;
            }
            Assert.True(werktag >= 0 && wochenende >= 0);

            // Der Ueberschuss (2,0 - 0,4) laeuft an Werktagen, nie negativ.
            Assert.True(e.LueftungZusatzleitwertWK[werktag] > 0.0);
            Assert.Equal(0.0, e.LueftungZusatzleitwertWK[wochenende]);
            for (int h = 0; h < 8760; h++) Assert.True(e.LueftungZusatzleitwertWK[h] >= 0.0);

            // Der Zusatzleitwert steht im Stundenrand.
            Assert.Equal(e.LueftungZusatzleitwertWK[werktag], e.Rand(werktag).ZusatzleitwertWK, 9);
            Assert.Equal(0.0, e.Rand(wochenende).ZusatzleitwertWK);

            // P9: Ohne Nachtauskuehlvorgabe gibt es keinen bedingten Anteil - der ganze
            // Ueberschuss wirkt unbedingt (hier: ausserhalb jedes Nachtfensters).
            Assert.Null(e.NachtauskuehlungWK);
        }

        // =============================================================================
        //  Geräte und Personen (P1 b)
        // =============================================================================

        [Fact]
        public void Geraete_und_Personen_gehen_je_Stunde_in_PhiConv_und_PhiRad()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang ohne = Eingang(g, null);

            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = st >= 8 && st < 17 ? 1.0 : 0.0;
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Geraete,
                     new Konditionierungskalender(Konditionierungsgroesse.Geraete,
                                                  Kalenderangabe.AusWoche(woche), null, null));
            GebaeudeModellEingang mit = Eingang(g, s);

            Assert.NotNull(mit.InnereGewinneReihe_W);
            // Zur Arbeitszeit voller Nennwert (Interne_Waermegewinne), sonst 0.
            Assert.Equal(g.Interne_Waermegewinne, mit.InnereGewinneReihe_W[12], 9);
            Assert.Equal(0.0, mit.InnereGewinneReihe_W[3]);
            // Zur Arbeitszeit ist PhiConv bitgleich mit dem Bestandsweg (Anteil 1 x Nennwert).
            Assert.Equal(BitConverter.DoubleToInt64Bits(ohne.PhiConv[12]),
                         BitConverter.DoubleToInt64Bits(mit.PhiConv[12]));
            // Nachts fehlen die inneren Lasten ganz.
            Assert.True(mit.PhiConv[3] < ohne.PhiConv[3]);
        }

        [Fact]
        public void Ein_Personenkalender_bringt_seinen_Nennwert_zusaetzlich_ein()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang ohne = Eingang(g, null);

            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Personen,
                     Konstant(Konditionierungsgroesse.Personen, 1.0, nennwert: 350.0));
            GebaeudeModellEingang mit = Eingang(g, s);

            for (int h = 0; h < 8760; h++)
                Assert.Equal(g.Interne_Waermegewinne + 350.0, mit.InnereGewinneReihe_W[h], 9);
            Assert.True(mit.PhiConv[12] > ohne.PhiConv[12]);
        }

        // =============================================================================
        //  Determinismus
        // =============================================================================

        [Fact]
        public void Zwei_Laeufe_derselben_Eingaben_liefern_bitgleiche_Reihen()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz Bauen()
            {
                Konditionierungssatz s = Satz();
                s.Setzen(Konditionierungsgroesse.Heizsoll, Konstant(Konditionierungsgroesse.Heizsoll, 20.0));
                s.Setzen(Konditionierungsgroesse.Lueftung, Konstant(Konditionierungsgroesse.Lueftung, 0.7));
                s.Setzen(Konditionierungsgroesse.Personen,
                         Konstant(Konditionierungsgroesse.Personen, 0.5, nennwert: 350.0));
                return s;
            }
            GebaeudeModellEingang a = Eingang(g, Bauen());
            GebaeudeModellEingang b = Eingang(g, Bauen());
            Bitgleich(a.ThetaSoll, b.ThetaSoll, "ThetaSoll");
            Bitgleich(a.PhiConv, b.PhiConv, "PhiConv");
            Bitgleich(a.LueftungZusatzleitwertWK, b.LueftungZusatzleitwertWK, "Zusatzleitwert");
            Bitgleich(a.InnereGewinneReihe_W, b.InnereGewinneReihe_W, "InnereGewinne");
        }
    }
}
