using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Anlagenkopplung im Eingangsbauer und im Jahreslauf</b> (AK1; 3.4, 4.3, 6.1, 8.1,
    /// 8.4, 10.1, 11.1, H7, H10, H12) — ohne Datenbank, am Probegebäude aus
    /// <see cref="Vdi6007Probe"/> mit synthetischer Klimareihe: Grenzfall A (Schalter aus) und
    /// Grenzfall B über ein ganzes Jahr, Sollwert-Zeitprogramm samt Ferien und strengem Leser,
    /// die hergeleiteten Vorgaben, die Vorlaufreihe, die Prüfregeln, die Begrenzung, die
    /// Skalierung und der Heizkreis des Projekts.
    /// </summary>
    public class AnlagenkopplungEingangTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungEingangTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>Das Probegebäude mit eingeschalteter Wärmeübergabe (Radiator, Heizkurve, Vorgaben).</summary>
        private static ProjektGebaeudeModel Gekoppelt(Action<ProjektGebaeudeModel> aendern = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            aendern?.Invoke(g);
            return g;
        }

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, string stufe = DbWerte.ANLAGENKOPPLUNG_AK1,
                                                     double vorlaufAnlage = double.NaN, double skalierung = 1.0)
            => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, stufe, vorlaufAnlage, skalierung);

        private static GebaeudeModellFehler Grund(Action a)
            => Assert.Throws<GebaeudeModellException>(a).Grund;

        private static void ReihenBitgleich(GebaeudeModellErgebnis a, GebaeudeModellErgebnis b)
        {
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(a.HeizlastW[h].Equals(b.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(a.Raumtemperatur[h].Equals(b.Raumtemperatur[h]), "Raumluft, Stunde " + h);
                Assert.True(a.OperativeTemperatur[h].Equals(b.OperativeTemperatur[h]), "operativ, Stunde " + h);
                Assert.True(a.Heizsollwert[h].Equals(b.Heizsollwert[h]), "Sollwert, Stunde " + h);
            }
            Assert.Equal(a.VerbrauchAltKwh, b.VerbrauchAltKwh);
            Assert.Equal(a.StundenMitUmschaltung, b.StundenMitUmschaltung);
        }

        // =====================================================================
        //  Die beiden Grenzfälle (3.7, 11.1) über ein ganzes Jahr
        // =====================================================================

        /// <summary>
        /// <b>Grenzfall A — Schalter aus.</b> Ohne Projektstufe, ohne Schalter oder mit Übergabeart
        /// „ideal" rechnet das Gebäude trotz aller dreizehn gesetzten Spalten byte-gleich wie ein
        /// Gebäude ohne Übergabe, und es entsteht kein Heizkreis (F-A1, F-A17, N-A3).
        /// </summary>
        [Fact]
        public void Grenzfall_A_Ohne_wirksame_Kopplung_rechnet_das_Gebaeude_byte_gleich()
        {
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima), 0, 1);

            Action<ProjektGebaeudeModel> alles = g =>
            {
                g.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE;
                g.Uebergabe_Exponent = 1.2;
                g.Uebergabe_Leistung_Nenn = 3.0;
                g.Auslegung_Vorlauf = 40.0;
                g.Auslegung_Ruecklauf = 30.0;
                g.Auslegung_Raumtemperatur = 21.0;
                g.Auslegung_Aussentemperatur = -14.0;
                g.Heizkurve_Niveau = 2.0;
                g.Heizkurve_Steilheit = 1.2;
                g.Regler_Proportionalband = 2.0;
                g.Sollwertprofil = string.Join(";", Enumerable.Repeat("19", 168));
            };

            foreach ((string stufe, bool schalter, string art) in new[]
                     {
                         ((string)null, true, DbWerte.UEBERGABE_FLAECHE),
                         (DbWerte.ANLAGENKOPPLUNG_AUS, true, DbWerte.UEBERGABE_FLAECHE),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, false, DbWerte.UEBERGABE_FLAECHE),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, true, DbWerte.UEBERGABE_IDEAL),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, true, (string)null),
                     })
            {
                ProjektGebaeudeModel g = Gekoppelt(alles);
                g.Heizkreis_Aktiv = schalter;
                g.Uebergabe_Art = art;
                GebaeudeModellEingang e = Eingang(g, stufe, 55.0);
                Assert.False(e.KopplungWirksam);
                Assert.Equal(GebaeudeFestwerte.VORGABE_HEIZUNG_STRAHLUNGSANTEIL, e.HeizungStrahlungsanteil);
                GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
                Assert.Null(r.Heizkreis);
                ReihenBitgleich(bestand, r);
            }
        }

        /// <summary>
        /// <b>Grenzfall B über ein Jahr</b> (3.7, 11.1): unbegrenzte Übergabe, Proportionalband
        /// null, fester Vorlauf der Anlage — der gekoppelte Weg liefert Stunde für Stunde dieselben
        /// Bits wie die ideale Regelung, samt Vorlauf 30 Tage. Der Radiator bringt den
        /// Strahlungsanteil 0,3 mit, denselben wie die Vorgabe ohne Übergabeart (H12).
        /// </summary>
        [Fact]
        public void Grenzfall_B_Unbegrenzte_Uebergabe_mit_Band_null_ist_ueber_das_Jahr_bitgleich()
        {
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima), 0, 1);

            ProjektGebaeudeModel g = Gekoppelt(x =>
            {
                x.Uebergabe_Leistung_Nenn = double.PositiveInfinity;
                x.Regler_Proportionalband = 0.0;
                x.Heizkurve_Aktiv = false;
            });
            GebaeudeModellEingang e = Eingang(g, DbWerte.ANLAGENKOPPLUNG_AK1, 55.0);
            Assert.True(e.KopplungWirksam);
            Assert.True(e.Uebergabe.Unbegrenzt);
            Assert.Equal(Vorlaufquelle.Anlage, e.Vorlaufquelle);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);

            ReihenBitgleich(bestand, r);
            Assert.NotNull(r.Heizkreis);
            Assert.Equal(0.0, r.Heizkreis.UebergabeBegrenztStundenH);
            Assert.All(r.Heizkreis.VorlaufC, v => Assert.Equal(55.0, v));
        }

        // =====================================================================
        //  Sollwert-Zeitprogramm (4.3, H8, H-F10)
        // =====================================================================

        private static string Profil(Func<int, int, double> wert)
        {
            var werte = new double[168];
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++) werte[t * 24 + s] = wert(t, s);
            return AnlagenkopplungSchema.WochenprofilSchreiben(werte);
        }

        /// <summary>168 Werte gleich der Bestandsbelegung ergeben byte-gleiche Sollwerte (11.1).</summary>
        [Fact]
        public void Ein_Profil_gleich_dem_Bestand_ergibt_byte_gleiche_Sollwerte()
        {
            GebaeudeModellEingang bestand = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima);
            ProjektGebaeudeModel g = Gekoppelt(x => x.Sollwertprofil = Profil((t, s) => s >= 6 && s <= 21 ? 20.0 : 18.0));
            GebaeudeModellEingang e = Eingang(g);
            Assert.True(e.SollwertprofilWirksam);
            for (int h = 0; h < 8760; h++) Assert.True(bestand.ThetaSoll[h].Equals(e.ThetaSoll[h]), "Stunde " + h);
        }

        /// <summary>
        /// Das Profil folgt dem Wochentag des Ortszeit-Kalenders (Montag 00:00 = Stelle 1), und die
        /// Ferienzeiträume wirken darüber mit dem Ferienwert (4.3).
        /// </summary>
        [Fact]
        public void Das_Profil_folgt_dem_Wochentag_und_die_Ferien_wirken_darueber()
        {
            // Referenzjahr 2025: Der 1. Januar ist ein Mittwoch (Montag = 0 → 2).
            Assert.Equal(2, GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende()));
            ProjektGebaeudeModel g = Gekoppelt(x =>
            {
                x.Sollwertprofil = Profil((t, s) => 15.0 + t + s / 100.0);
                x.Ferien = 1.0;
                x.Raumsolltemperatur_Ferien = 12.0;
                x.Ferienbeginn_1 = 10.0;
                x.Ferienende_1 = 12.0;
            });
            GebaeudeModellEingang e = Eingang(g);
            Assert.Equal(17.00, e.ThetaSoll[0], 9);          // Mittwoch 00:00
            Assert.Equal(17.23, e.ThetaSoll[23], 9);         // Mittwoch 23:00
            Assert.Equal(18.07, e.ThetaSoll[24 + 7], 9);     // Donnerstag 07:00
            Assert.Equal(15.00, e.ThetaSoll[5 * 24], 9);     // Montag 6. Januar 00:00
            Assert.Equal(21.05, e.ThetaSoll[4 * 24 + 5], 9); // Sonntag 5. Januar 05:00
            for (int h = 9 * 24; h < 12 * 24; h++) Assert.Equal(12.0, e.ThetaSoll[h]);   // Ferien, Tage 10 … 12
            Assert.Equal(15.0 + ((2 + 12) % 7), e.ThetaSoll[12 * 24], 9);
        }

        /// <summary>Der strenge Leser: 167 Werte, keine Zahl, ein Wert außerhalb — benannter Fehler, kein Auffüllen.</summary>
        [Fact]
        public void Ein_unbrauchbares_Profil_bricht_benannt_ab()
        {
            string zuKurz = string.Join(";", Enumerable.Repeat("20", 167));
            string keineZahl = string.Join(";", Enumerable.Repeat("20", 100)) + ";zwanzig;" + string.Join(";", Enumerable.Repeat("20", 67));
            string zuWarm = string.Join(";", Enumerable.Repeat("20", 167)) + ";45";
            foreach (string profil in new[] { zuKurz, keineZahl, zuWarm })
                Assert.Equal(GebaeudeModellFehler.SollwertprofilUngueltig,
                             Grund(() => Eingang(Gekoppelt(x => x.Sollwertprofil = profil))));

            // Ohne wirksame Kopplung liest niemand das Profil: kein Fehler, der Bestand.
            GebaeudeModellEingang aus = Eingang(Gekoppelt(x => x.Sollwertprofil = zuKurz), DbWerte.ANLAGENKOPPLUNG_AUS);
            Assert.False(aus.SollwertprofilWirksam);
        }

        /// <summary>Eine Wochenendmaske ohne Wochentakt ist kein Kalender.</summary>
        [Fact]
        public void Eine_Maske_ohne_Wochentakt_hat_keinen_Wochentag()
        {
            var maske = new bool[365];
            maske[3] = true;
            Assert.Equal(-1, GebaeudeModellEingang.WochentagDesErstenTags(maske));
            Assert.Equal(-1, GebaeudeModellEingang.WochentagDesErstenTags(new bool[365]));
            Assert.Equal(0, GebaeudeModellEingang.WochentagDesErstenTags(KlimakalenderGemeinsam.WochenendmaskeBilden(2024)));
            Assert.Equal(6, GebaeudeModellEingang.WochentagDesErstenTags(KlimakalenderGemeinsam.WochenendmaskeBilden(2023)));
            Assert.Equal(5, GebaeudeModellEingang.WochentagDesErstenTags(KlimakalenderGemeinsam.WochenendmaskeBilden(2022)));
        }

        // =====================================================================
        //  Hergeleitete Vorgaben (8.4, H7, H10, H12)
        // =====================================================================

        /// <summary>
        /// <b>H10</b>: Die Auslegungs-Außentemperatur ist das kälteste Tagesmittel der Klimareihe,
        /// auf ganze Grad abgerundet; das Feld überschreibt.
        /// </summary>
        [Fact]
        public void H10_Die_Auslegungs_Aussentemperatur_kommt_aus_der_Klimareihe()
        {
            GebaeudeModellEingang e = Eingang(Gekoppelt());
            GebaeudeModellEingang.KaeltesterTag(e.ThetaOut, out double mittel);
            Assert.True(e.AuslegungAussentemperaturHergeleitet);
            Assert.Equal(Math.Floor(mittel), e.AuslegungAussentemperaturC);
            Assert.Equal(-2.0, e.AuslegungAussentemperaturC);   // Jahresgang 10 − 12 K, Tagesmittel −2 °C

            GebaeudeModellEingang f = Eingang(Gekoppelt(x => x.Auslegung_Aussentemperatur = -14.0));
            Assert.False(f.AuslegungAussentemperaturHergeleitet);
            Assert.Equal(-14.0, f.AuslegungAussentemperaturC);
        }

        /// <summary>
        /// <b>8.4</b>: Die hergeleitete Nennleistung ist die stationäre Heizlast des Modells am
        /// Auslegungspunkt — dieselbe Zahl, auf die der Löser einschwingt, wenn er mit festen
        /// Randbedingungen läuft (ohne Lasten, Grundfläche an der Außenluft).
        /// </summary>
        [Fact]
        public void Die_hergeleitete_Nennleistung_ist_die_stationaere_Auslegungslast()
        {
            GebaeudeModellEingang e = Eingang(Gekoppelt(x => x.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT));
            Assert.True(e.UebergabeNennleistungHergeleitet);
            Assert.Equal(e.AuslegungsheizlastW, e.Uebergabe.PhiNW);

            double aussen = e.AuslegungAussentemperaturC;
            var r = new Stundenrand(aussen, aussen, 20.0, double.PositiveInfinity, 0.0, 0.0, 0.0,
                                    heizungStrahlungsanteil: e.HeizungStrahlungsanteil);
            var modell = new Zonenmodell2K(e.Parameter);
            Vorlauf2K.Einschwingen(modell, 20.0, new[] { r }, 1e-9, 20000);
            double stationaer = modell.Schritt(in r).HeizleistungW;
            _aus.WriteLine("Auslegungsheizlast " + e.AuslegungsheizlastW.ToString("0.0") + " W, eingeschwungen " + stationaer.ToString("0.0") + " W");
            Assert.True(Math.Abs(e.AuslegungsheizlastW - stationaer) <= 1e-6 * stationaer);
        }

        /// <summary><b>H12</b>: Mit einer Übergabeart heißt ein leerer Strahlungsanteil „Vorgabe der Art"; ein Wert gewinnt.</summary>
        [Fact]
        public void H12_Der_leere_Strahlungsanteil_ist_die_Vorgabe_der_Uebergabeart()
        {
            Assert.Equal(0.5, Eingang(Gekoppelt(x => x.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE)).HeizungStrahlungsanteil);
            Assert.Equal(0.1, Eingang(Gekoppelt(x => x.Uebergabe_Art = DbWerte.UEBERGABE_KONVEKTOR)).HeizungStrahlungsanteil);
            Assert.Equal(0.3, Eingang(Gekoppelt()).HeizungStrahlungsanteil);
            Assert.Equal(0.7, Eingang(Gekoppelt(x =>
            {
                x.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE;
                x.Heizung_Strahlungsanteil = 0.7;
            })).HeizungStrahlungsanteil);
        }

        /// <summary>
        /// Die Vorgaben der Übergabeart (3.1) und die Umrechnung kW → W einmal im Eingangsbauer
        /// (3.3); eine feste Nennleistung gilt dem wirklichen Gebäude und wird mit dem Verhältnis
        /// auf den Katalogbau umgerechnet (H7).
        /// </summary>
        [Fact]
        public void Vorgaben_der_Art_und_die_feste_Nennleistung()
        {
            GebaeudeModellEingang f = Eingang(Gekoppelt(x => x.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE));
            Assert.Equal(1.1, f.Uebergabe.Exponent);
            Assert.Equal(35.0, f.Uebergabe.AuslegungVorlaufC);
            Assert.Equal(28.0, f.Uebergabe.AuslegungRuecklaufC);
            Assert.Equal(20.0, f.Uebergabe.AuslegungRaumC);          // Raumsolltemperatur_Tag
            Assert.Equal(1.0, f.ReglerbandK);

            GebaeudeModellEingang fest = Eingang(Gekoppelt(x => x.Uebergabe_Leistung_Nenn = 12.0), skalierung: 2.0);
            Assert.False(fest.UebergabeNennleistungHergeleitet);
            Assert.Equal(6000.0, fest.Uebergabe.PhiNW, 9);

            GebaeudeModellEingang probe = Eingang(Gekoppelt(x => x.Uebergabe_Leistung_Nenn = 12.0), skalierung: double.NaN);
            Assert.True(probe.UebergabeNennleistungHergeleitet);
        }

        // =====================================================================
        //  Die Vorlaufreihe (Schritt E, 3.4)
        // =====================================================================

        [Fact]
        public void Die_Vorlaufreihe_kommt_aus_Heizkurve_Anlage_oder_Auslegung()
        {
            GebaeudeModellEingang kurve = Eingang(Gekoppelt());
            Assert.Equal(Vorlaufquelle.Heizkurve, kurve.Vorlaufquelle);
            int aus = 0;
            for (int h = 0; h < 8760; h++)
            {
                double v = kurve.Heizkurve.VorlaufC(kurve.ThetaSoll[h], kurve.ThetaOut[h]);
                if (double.IsNaN(v)) { Assert.True(double.IsNaN(kurve.VorlaufC[h])); aus++; }
                else Assert.Equal(v, kurve.VorlaufC[h]);
            }
            Assert.True(aus > 0 && aus < 8760, "Heizgrenze im Sommer, Heizkurve im Winter");

            GebaeudeModellEingang anlage = Eingang(Gekoppelt(x => x.Heizkurve_Aktiv = false), vorlaufAnlage: 45.0);
            Assert.Equal(Vorlaufquelle.Anlage, anlage.Vorlaufquelle);
            Assert.All(anlage.VorlaufC, v => Assert.Equal(45.0, v));

            GebaeudeModellEingang auslegung = Eingang(Gekoppelt(x => x.Heizkurve_Aktiv = false));
            Assert.Equal(Vorlaufquelle.Auslegung, auslegung.Vorlaufquelle);
            Assert.All(auslegung.VorlaufC, v => Assert.Equal(55.0, v));
        }

        // =====================================================================
        //  Prüfregeln (9.1, 9.5)
        // =====================================================================

        [Fact]
        public void Widerspruechliche_Eingaben_brechen_benannt_ab()
        {
            var faelle = new List<Action<ProjektGebaeudeModel>>
            {
                x => x.Uebergabe_Art = "FUSSBODEN",
                x => { x.Auslegung_Vorlauf = 30.0; x.Auslegung_Ruecklauf = 25.0; x.Auslegung_Raumtemperatur = 26.0; },   // 9.5: Vorlauf nicht über Raum? (30 > 26, Rücklauf 25 < 26)
                x => x.Auslegung_Ruecklauf = 60.0,
                x => x.Uebergabe_Exponent = 2.0,
                x => x.Regler_Proportionalband = 6.0,
                x => x.Regler_Proportionalband = -1.0,
                x => x.Auslegung_Aussentemperatur = 10.0,
                x => x.Uebergabe_Leistung_Nenn = 0.0,
                x => x.Heizkurve_Steilheit = 0.1,
                x => x.Heizkurve_Niveau = 12.0,
                x => x.Auslegung_Vorlauf = 95.0,
            };
            foreach (Action<ProjektGebaeudeModel> fall in faelle)
                Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Gekoppelt(fall))));

            // Die Meldung 9.5 nennt beide Werte: Auslegungsvorlauf unter der Auslegungs-Raumtemperatur.
            var ex = Assert.Throws<GebaeudeModellException>(() => Eingang(Gekoppelt(x =>
            {
                x.Auslegung_Vorlauf = 25.0;
                x.Auslegung_Raumtemperatur = 26.0;
            })));
            Assert.Contains("25", ex.Message);
            Assert.Contains("26", ex.Message);
        }

        // =====================================================================
        //  Der Jahreslauf mit Kopplung
        // =====================================================================

        /// <summary>
        /// Das Jahr mit Radiator, Heizkurve und Vorgaben: Die Aufheizspitze sinkt, die mittlere
        /// Raumtemperatur liegt mit dem Band von 1 K unter der idealen Regelung, der Heizkreis
        /// trägt plausible Reihen, und zwei Läufe sind byte-gleich (N-A2).
        /// </summary>
        [Fact]
        public void Der_gekoppelte_Jahreslauf_kappt_die_Spitze_und_traegt_den_Heizkreis()
        {
            GebaeudeModellEingang eBestand = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima);
            GebaeudeModellEingang e = Eingang(Gekoppelt());
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(eBestand, 0, 1);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            GebaeudeModellErgebnis nochmal = Vdi6007Rechenweg.Laufen(Eingang(Gekoppelt()), 0, 1);

            // Rechenzeit (N-A4): gemessen nach dem Anlauf, je das Beste aus drei Läufen.
            double zeitBestand = double.MaxValue, zeitKopplung = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var uhr = Stopwatch.StartNew();
                Vdi6007Rechenweg.Laufen(eBestand, 0, 1);
                zeitBestand = Math.Min(zeitBestand, uhr.Elapsed.TotalMilliseconds);
                uhr.Restart();
                Vdi6007Rechenweg.Laufen(e, 0, 1);
                zeitKopplung = Math.Min(zeitKopplung, uhr.Elapsed.TotalMilliseconds);
            }

            HeizkreisErgebnis hk = r.Heizkreis;
            _aus.WriteLine($"Nennleistung {hk.UebergabeNennKw:0.00} kW (Auslegung {hk.AuslegungAussenC} °C); Spitze {bestand.SpitzeKw:0.00} → {r.SpitzeKw:0.00} kW; " +
                           $"Jahr {bestand.JahresheizwaermeMwh:0.000} → {r.JahresheizwaermeMwh:0.000} MWh; Raumluft {bestand.MittlereRaumtemperaturHeizzeit:0.00} → {r.MittlereRaumtemperaturHeizzeit:0.00} °C; " +
                           $"Vorlauf {hk.VorlaufMittelC:0.0} °C, Rücklauf {hk.RuecklaufMittelC:0.0} °C, begrenzt {hk.UebergabeBegrenztStundenH:0.0} h; " +
                           $"Laufzeit {zeitBestand:0.0} → {zeitKopplung:0.0} ms");

            Assert.NotNull(hk);
            Assert.True(r.SpitzeKw < bestand.SpitzeKw, "Aufheizspitze gekappt");
            Assert.True(r.MittlereRaumtemperaturHeizzeit < bestand.MittlereRaumtemperaturHeizzeit, "Band 1 K: der Raum liegt etwas unter dem Sollwert");
            Assert.True(hk.Heizstunden > 1000);
            Assert.True(hk.VorlaufMittelC > hk.RuecklaufMittelC && hk.RuecklaufMittelC > 20.0 && hk.VorlaufMittelC <= 55.0);
            for (int h = 0; h < 8760; h++)
            {
                if (double.IsNaN(hk.VorlaufC[h])) { Assert.Equal(0.0, r.HeizlastW[h]); continue; }
                Assert.True(hk.RuecklaufC[h] <= hk.VorlaufC[h]);
                Assert.Equal(hk.VorlaufC[h] - r.HeizlastW[h] / e.Uebergabe.WHWK, hk.RuecklaufC[h], 9);
            }
            ReihenBitgleich(r, nochmal);
            for (int h = 0; h < 8760; h++) Assert.True(r.Heizkreis.RuecklaufC[h].Equals(nochmal.Heizkreis.RuecklaufC[h]) || double.IsNaN(r.Heizkreis.RuecklaufC[h]));
        }

        /// <summary>
        /// <b>Begrenzung</b>: Eine zu kleine Übergabe lässt den Raum in kalten Stunden unter den
        /// Sollwert fallen; die Stunden werden gezählt, und die größte Unterschreitung steht bereit.
        /// </summary>
        [Fact]
        public void Eine_zu_kleine_Uebergabe_zaehlt_die_begrenzten_Stunden()
        {
            GebaeudeModellEingang e = Eingang(Gekoppelt(x => x.Uebergabe_Leistung_Nenn = 5.0));
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            HeizkreisErgebnis hk = r.Heizkreis;
            _aus.WriteLine($"begrenzt {hk.UebergabeBegrenztStundenH:0.0} h, größte Unterschreitung {hk.GroessteUnterschreitungK:0.00} K");
            Assert.True(hk.UebergabeBegrenztStundenH > 100.0);
            Assert.True(hk.GroessteUnterschreitungK > 1.0);
            int unter = 0;
            for (int h = 0; h < 8760; h++)
                if (hk.UebergabeBegrenztAnteil[h] >= 1.0 && r.Raumtemperatur[h] < e.ThetaSoll[h] - 1.0) unter++;
            Assert.True(unter > 0, "in begrenzten Stunden liegt der Raum unter dem Sollwert");
        }

        /// <summary>Die Skalierung (E8) multipliziert die Leistungen der Auslegung und lässt Reihen und Stunden.</summary>
        [Fact]
        public void Die_Skalierung_multipliziert_die_Nennleistung_und_laesst_die_Temperaturen()
        {
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(Eingang(Gekoppelt()), 0, 1);
            GebaeudeModellErgebnis s = r.Skaliert(2.5);
            Assert.Equal(2.5 * r.Heizkreis.UebergabeNennKw, s.Heizkreis.UebergabeNennKw, 9);
            Assert.Equal(2.5 * r.Heizkreis.AuslegungsheizlastKw, s.Heizkreis.AuslegungsheizlastKw, 9);
            Assert.Same(r.Heizkreis.VorlaufC, s.Heizkreis.VorlaufC);
            Assert.Equal(r.Heizkreis.UebergabeBegrenztStundenH, s.Heizkreis.UebergabeBegrenztStundenH);
            Assert.Equal(r.Heizkreis.VorlaufMittelC, s.Heizkreis.VorlaufMittelC);
        }

        /// <summary>
        /// Der Heizkreis des Projekts (6.1): Mit einem gekoppelten Gebäude sind die Zahlen die des
        /// Gebäudes; mit zweien gilt je Stunde das bedarfsgewichtete Mittel; ohne gekoppeltes
        /// Gebäude gibt es keinen (die Erzeugerseite rechnet wie im Bestand).
        /// </summary>
        [Fact]
        public void Der_Heizkreis_des_Projekts_mittelt_bedarfsgewichtet()
        {
            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(Eingang(Gekoppelt()), 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(Eingang(Gekoppelt(x =>
            {
                x.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE;
            })), 1, 2).Skaliert(3.0);
            GebaeudeModellErgebnis ohne = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima), 2, 3);

            Assert.Null(HeizkreisProjekt.Bilden(new[] { ohne }));

            HeizkreisProjekt eins = HeizkreisProjekt.Bilden(new[] { a, ohne });
            Assert.Equal(a.Heizkreis.VorlaufMittelC, eins.VorlaufMittelC);
            Assert.Equal(a.Heizkreis.RuecklaufMittelC, eins.RuecklaufMittelC);
            Assert.Equal(a.Heizkreis.UebergabeBegrenztStundenH, eins.UebergabeBegrenztStundenH);
            Assert.Equal(1, eins.GekoppelteGebaeude);

            HeizkreisProjekt zwei = HeizkreisProjekt.Bilden(new[] { a, b, ohne });
            Assert.Equal(2, zwei.GekoppelteGebaeude);
            for (int h = 0; h < 8760; h++)
            {
                double qa = a.HeizlastW[h], qb = b.HeizlastW[h];
                if (!(qa > 0.0) && !(qb > 0.0)) { Assert.True(double.IsNaN(zwei.VorlaufC[h])); continue; }
                if (qa > 0.0 && qb > 0.0)
                {
                    double erwartet = (qa * a.Heizkreis.VorlaufC[h] + qb * b.Heizkreis.VorlaufC[h]) / (qa + qb);
                    Assert.Equal(erwartet, zwei.VorlaufC[h], 9);
                    Assert.True(zwei.VorlaufC[h] <= Math.Max(a.Heizkreis.VorlaufC[h], b.Heizkreis.VorlaufC[h]) + 1e-12);
                }
            }
        }
    }
}
