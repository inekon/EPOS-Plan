using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kälteseite der Anlagenkopplung im Eingangsbauer und im Jahreslauf</b> (E37;
    /// Anlagenkopplung 7.2, 8.1, 8.4, 10.5, 11.1) — ohne Datenbank, am Probegebäude aus
    /// <see cref="Vdi6007Probe"/> mit synthetischer Klimareihe: Grenzfall A (Kälteseite nicht
    /// wirksam) und Grenzfall B der Kälte über ein ganzes Jahr, die Vorgaben je Art, die
    /// Prüfregeln, der feste Kaltwasser-Vorlauf samt Vorlaufgrenze, der Auslegungstag (A2), die
    /// feste Nennleistung, der Jahreslauf mit Kältekreis und der Kältekreis des Projekts.
    /// </summary>
    public class AnlagenkopplungKaelteEingangTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungKaelteEingangTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>Das Probegebäude mit wirksamer Kühlung auf 24 °C und eingeschalteter Kühlübergabe (Kühldecke, Vorgaben).</summary>
        private static ProjektGebaeudeModel Kuehlgekoppelt(Action<ProjektGebaeudeModel> aendern = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(24.0);
            g.Kuehluebergabe_Aktiv = true;
            g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            aendern?.Invoke(g);
            return g;
        }

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, string stufe = DbWerte.ANLAGENKOPPLUNG_AK1,
                                                     double kuehlVorlaufAnlage = double.NaN, double skalierung = 1.0,
                                                     bool kuehlbetrieb = true, SolardatenModel[] klima = null)
            => GebaeudeModellEingang.Bauen(g, klima ?? Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb, stufe, double.NaN, skalierung,
                                           kuehlVorlaufAnlage);

        private static GebaeudeModellFehler Grund(Action a)
            => Assert.Throws<GebaeudeModellException>(a).Grund;

        private static void Bitgleich(GebaeudeModellErgebnis a, GebaeudeModellErgebnis b)
        {
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(a.HeizlastW[h].Equals(b.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(a.Raumtemperatur[h].Equals(b.Raumtemperatur[h]), "Raumluft, Stunde " + h);
                Assert.True(a.OperativeTemperatur[h].Equals(b.OperativeTemperatur[h]), "operativ, Stunde " + h);
                if (a.KuehlbedarfKwh != null || b.KuehlbedarfKwh != null)
                    Assert.True(a.KuehlbedarfKwh[h].Equals(b.KuehlbedarfKwh[h]), "Kühlbedarf, Stunde " + h);
            }
            Assert.Equal(a.StundenMitUmschaltung, b.StundenMitUmschaltung);
            Assert.Equal(a.Ueberhitzungsstunden, b.Ueberhitzungsstunden);
        }

        // =====================================================================
        //  Probe 3 und 4 — die beiden Grenzfälle über ein ganzes Jahr
        // =====================================================================

        /// <summary>
        /// <b>Grenzfall A der Kälte</b>: Ohne Projektstufe, ohne wirksame Kühlung, ohne Schalter
        /// (A1) oder mit Art „ideal" bzw. leer rechnet das Gebäude trotz aller gesetzten Spalten der
        /// Kühlübergabe byte-gleich wie ohne sie, und es entsteht kein Kältekreis.
        /// </summary>
        [Fact]
        public void Grenzfall_A_Ohne_wirksame_Kaelteseite_rechnet_das_Gebaeude_byte_gleich()
        {
            Action<ProjektGebaeudeModel> alles = g =>
            {
                g.Kuehl_Uebergabe_Exponent = 1.05;
                g.Kuehl_Uebergabe_Leistung_Nenn = 3.0;
                g.Kuehl_Auslegung_Vorlauf = 15.0;
                g.Kuehl_Auslegung_Ruecklauf = 18.0;
                g.Kuehl_Auslegung_Raumtemperatur = 25.0;
                g.Kuehl_Vorlaufgrenze = 17.0;
                g.Regler_Proportionalband = 2.0;
            };
            GebaeudeModellErgebnis gekuehlt = Vdi6007Rechenweg.Laufen(Vdi6007Probe.EingangGekuehlt(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1);
            GebaeudeModellErgebnis frei = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1);

            foreach ((string stufe, bool kuehlbetrieb, bool schalter, string art, bool mitKuehlung) in new[]
                     {
                         ((string)null, true, true, DbWerte.KUEHLUEBERGABE_KUEHLDECKE, true),
                         (DbWerte.ANLAGENKOPPLUNG_AUS, true, true, DbWerte.KUEHLUEBERGABE_KUEHLDECKE, true),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, true, false, DbWerte.KUEHLUEBERGABE_KUEHLDECKE, true),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, true, true, DbWerte.KUEHLUEBERGABE_IDEAL, true),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, true, true, (string)null, true),
                         (DbWerte.ANLAGENKOPPLUNG_AK1, false, true, DbWerte.KUEHLUEBERGABE_KUEHLDECKE, false),
                     })
            {
                ProjektGebaeudeModel g = Kuehlgekoppelt(alles);
                g.Kuehluebergabe_Aktiv = schalter;
                g.Kuehl_Uebergabe_Art = art;
                GebaeudeModellEingang e = Eingang(g, stufe, 7.0, kuehlbetrieb: kuehlbetrieb);
                Assert.False(e.KuehlKopplungWirksam);
                Assert.Null(e.KuehlUebergabe);
                GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
                Assert.Null(r.Kuehlkreis);
                Bitgleich(mitKuehlung ? gekuehlt : frei, r);
            }
        }

        /// <summary>
        /// <b>Grenzfall B der Kälte über ein Jahr</b>: unbegrenzte Kühlübergabe (Φ_N = +∞),
        /// Proportionalband null, Strahlungsanteil 0 (Gebläsekonvektor, keine Vorlaufgrenze) —
        /// Stunde für Stunde dieselben Bits wie die ideale Kühlung, samt Vorlauf 30 Tage.
        /// </summary>
        [Fact]
        public void Grenzfall_B_Unbegrenzte_Kuehluebergabe_mit_Band_null_ist_ueber_das_Jahr_bitgleich()
        {
            GebaeudeModellErgebnis ideal = Vdi6007Rechenweg.Laufen(Vdi6007Probe.EingangGekuehlt(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1);
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(g =>
            {
                g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;
                g.Kuehl_Uebergabe_Leistung_Nenn = double.PositiveInfinity;
                g.Regler_Proportionalband = 0.0;
            }), kuehlVorlaufAnlage: 7.0);
            Assert.True(e.KuehlKopplungWirksam);
            Assert.Equal(0.0, e.KuehlStrahlungsanteil);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Bitgleich(ideal, r);
            KuehlkreisErgebnis kk = r.Kuehlkreis;
            Assert.NotNull(kk);
            Assert.Equal(0.0, kk.UebergabeBegrenztStundenH);
            Assert.True(kk.Kuehlstunden > 100, "Kühlstunden " + kk.Kuehlstunden);
            Assert.All(kk.VorlaufC, v => Assert.Equal(7.0, v));
            Assert.All(kk.RuecklaufC, v => Assert.Equal(7.0, v));   // W_K unendlich
        }

        // =====================================================================
        //  Probe 11 — Vorgaben, Prüfregeln, Vorlauf, Auslegungstag
        // =====================================================================

        /// <summary>Die EPOS-Vorgaben je Art (A4) kommen im Eingang an; die Raumtemperatur des Auslegungspunkts ist leer der Kühlsollwert.</summary>
        [Fact]
        public void Vorgaben_je_Art_im_Eingang()
        {
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0);
            Assert.True(e.KuehlKopplungWirksam);
            Assert.Equal(1.1, e.KuehlUebergabe.Exponent);
            Assert.Equal(16.0, e.KuehlUebergabe.AuslegungVorlaufC);
            Assert.Equal(19.0, e.KuehlUebergabe.AuslegungRuecklaufC);
            Assert.Equal(24.0, e.KuehlUebergabe.AuslegungRaumC);
            Assert.Equal(16.0, e.KuehlVorlaufgrenzeC);
            Assert.Equal(0.5, e.KuehlStrahlungsanteil);
            Assert.Equal(GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K, e.ReglerbandK);
            Assert.True(e.KuehlNennleistungHergeleitet);
            Assert.Equal(e.AuslegungskuehllastW, e.KuehlUebergabe.PhiNW);
            Assert.Equal(-16.0, e.KuehlUebergabeGespiegelt.AuslegungVorlaufC);
            Assert.Equal(3.0, e.KuehlUebergabeGespiegelt.SpreizungNK);
            Assert.Equal(e.KuehlUebergabe.PhiNW / 3.0, e.KuehlUebergabeGespiegelt.WHWK);
            Assert.False(e.KopplungWirksam);

            GebaeudeModellEingang k = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(1.0, k.KuehlUebergabe.Exponent);
            Assert.Equal(7.0, k.KuehlUebergabe.AuslegungVorlaufC);
            Assert.Equal(12.0, k.KuehlUebergabe.AuslegungRuecklaufC);
            Assert.True(double.IsNaN(k.KuehlVorlaufgrenzeC), "Gebläsekonvektor ohne Grenze");
            Assert.Equal(0.0, k.KuehlStrahlungsanteil);
        }

        /// <summary>Widersprüchliche Eingaben brechen benannt ab; die Grenze über dem Auslegungsvorlauf ist ein Hinweis, kein Fehler.</summary>
        [Fact]
        public void Pruefregeln_der_Kaelteseite()
        {
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Art = "KUEHLTURM"))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Exponent = 2.0))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Auslegung_Vorlauf = 2.0))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Auslegung_Vorlauf = 20.0))));      // V > R
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Auslegung_Ruecklauf = 25.0))));    // R > θ_i,N
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Auslegung_Raumtemperatur = 35.0))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Vorlaufgrenze = 30.0))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = 0.0))));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Grund(() => Eingang(Kuehlgekoppelt(g => g.Regler_Proportionalband = 9.0))));
            string text = Assert.Throws<GebaeudeModellException>(() => Eingang(Kuehlgekoppelt(g => g.Kuehl_Auslegung_Vorlauf = 20.0))).Message;
            Assert.Contains("Kühlübergabe", text);

            GebaeudeModellEingang hinweis = Eingang(Kuehlgekoppelt(g => g.Kuehl_Vorlaufgrenze = 18.0));
            Assert.True(hinweis.KuehlGrenzeUeberAuslegung);
            Assert.False(Eingang(Kuehlgekoppelt()).KuehlGrenzeUeberAuslegung);
        }

        /// <summary>
        /// Der feste Kaltwasser-Vorlauf (7.2): Die Mischgruppe am Gebäude mischt das Kaltwasser der
        /// Anlage auf die Grenze hoch — kälter als die Anlage wird er nie; ohne Anlagenwert gilt der
        /// Auslegungsvorlauf; der Gebläsekonvektor hat keine Grenze.
        /// </summary>
        [Fact]
        public void Der_Kaltwasser_Vorlauf_aus_Anlage_Grenze_oder_Auslegung()
        {
            GebaeudeModellEingang kalt = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 7.0);
            Assert.Equal(Vorlaufquelle.Anlage, kalt.KuehlVorlaufquelle);
            Assert.True(kalt.KuehlVorlaufGekappt);
            Assert.Equal(16.0, kalt.KuehlVorlaufC);
            Assert.Equal(7.0, kalt.KuehlVorlaufQuelleC);

            GebaeudeModellEingang warm = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0);
            Assert.False(warm.KuehlVorlaufGekappt);
            Assert.Equal(18.0, warm.KuehlVorlaufC);

            GebaeudeModellEingang ohne = Eingang(Kuehlgekoppelt());
            Assert.Equal(Vorlaufquelle.Auslegung, ohne.KuehlVorlaufquelle);
            Assert.False(ohne.KuehlVorlaufGekappt);
            Assert.Equal(16.0, ohne.KuehlVorlaufC);

            GebaeudeModellEingang konvektor = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR),
                                                      kuehlVorlaufAnlage: 7.0);
            Assert.False(konvektor.KuehlVorlaufGekappt);
            Assert.Equal(7.0, konvektor.KuehlVorlaufC);
        }

        /// <summary>
        /// <b>Der Auslegungstag</b> (A2): der Tag mit dem höchsten Tagesmittel, deterministisch,
        /// eine positive Kühllast in der Größenordnung der Jahresspitze der idealen Kühlung — und
        /// der Zustand des Laufs bleibt unberührt: Der Jahreslauf mit hergeleiteter und mit genau
        /// dieser eingetragenen Leistung ist derselbe.
        /// </summary>
        [Fact]
        public void Der_Auslegungstag_ist_deterministisch_und_laesst_den_Lauf_unberuehrt()
        {
            GebaeudeModellEingang a = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0);
            GebaeudeModellEingang b = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0);
            Assert.True(a.AuslegungskuehllastW.Equals(b.AuslegungskuehllastW));
            int tag = GebaeudeModellEingang.WaermsterTag(a.ThetaOut, out double mittel);
            Assert.Equal(tag, a.AuslegungstagKuehlung);
            Assert.Equal(mittel, a.AuslegungstagKuehlungMittelC);
            Assert.True(a.AuslegungskuehllastW > 0.0);

            GebaeudeModellErgebnis ideal = Vdi6007Rechenweg.Laufen(Vdi6007Probe.EingangGekuehlt(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1);
            double spitzeIdealW = ideal.KuehlbedarfKwh.Max() * 1000.0;
            _aus.WriteLine($"Auslegungstag {GebaeudeModellEingang.TagText(tag, CultureInfo.GetCultureInfo("de-DE"))} ({mittel:0.00} °C): " +
                           $"{a.AuslegungskuehllastW:0.0} W; Jahresspitze der idealen Kühlung {spitzeIdealW:0.0} W");
            Assert.True(a.AuslegungskuehllastW > 0.5 * spitzeIdealW && a.AuslegungskuehllastW < 2.0 * spitzeIdealW,
                        "Auslegungstag und Jahresspitze in derselben Größenordnung");

            // Eingetragen mit genau dem hergeleiteten Wert (Skalierung 1000 W je kW exakt): derselbe Lauf.
            double kw = a.AuslegungskuehllastW / 1000.0;
            GebaeudeModellEingang fest = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = kw), kuehlVorlaufAnlage: 18.0,
                                                 skalierung: 1.0);
            Assert.False(fest.KuehlNennleistungHergeleitet);
            if (fest.KuehlUebergabe.PhiNW.Equals(a.KuehlUebergabe.PhiNW))
                Bitgleich(Vdi6007Rechenweg.Laufen(a, 0, 1), Vdi6007Rechenweg.Laufen(fest, 0, 1));
        }

        /// <summary>Ohne Kühllast am Auslegungstag (kaltes Klima) bricht die Herleitung benannt ab — mit dem Rat, Φ_N einzutragen.</summary>
        [Fact]
        public void Ohne_Kuehllast_am_Auslegungstag_bricht_die_Herleitung_benannt_ab()
        {
            SolardatenModel[] kalt = Vdi6007Probe.Klima(h => 0.0, mitSonne: false);
            var ex = Assert.Throws<GebaeudeModellException>(() => Eingang(Kuehlgekoppelt(), klima: kalt));
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, ex.Grund);
            Assert.Contains("Nennleistung eintragen", ex.Message);
            // Mit eingetragener Nennleistung rechnet es.
            Assert.True(Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = 2.0), klima: kalt).KuehlKopplungWirksam);
        }

        /// <summary>Die feste Nennleistung wird wie auf der Heizseite auf den Katalogbau umgerechnet (H7); Skalierung NaN heißt hergeleitet.</summary>
        [Fact]
        public void Die_feste_Nennleistung_und_die_Skalierung()
        {
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = 4.0), skalierung: 2.0);
            Assert.Equal(2000.0, e.KuehlUebergabe.PhiNW);
            Assert.True(double.IsNaN(e.AuslegungskuehllastW));
            Assert.Equal(-1, e.AuslegungstagKuehlung);

            GebaeudeModellEingang probe = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = 4.0), skalierung: double.NaN);
            Assert.True(probe.KuehlNennleistungHergeleitet);
            Assert.Equal(probe.AuslegungskuehllastW, probe.KuehlUebergabe.PhiNW);
        }

        // =====================================================================
        //  Der Jahreslauf mit Kältekreis
        // =====================================================================

        /// <summary>
        /// <b>Der kühlgekoppelte Jahreslauf</b>: Der Kältekreis trägt plausible Reihen (Rücklauf =
        /// Vorlauf + Φ_c/W_K, nie unter dem Vorlauf), der Kältebedarf sinkt gegen die ideale Kühlung
        /// (P-Band 1 K hebt die Raumluft), die Überhitzungsstunden steigen nicht unter den Bestand,
        /// die Heizseite bleibt unberührt, zwei Läufe sind byte-gleich.
        /// </summary>
        [Fact]
        public void Der_kuehlgekoppelte_Jahreslauf_traegt_den_Kaeltekreis()
        {
            GebaeudeModellErgebnis ideal = Vdi6007Rechenweg.Laufen(Vdi6007Probe.EingangGekuehlt(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1);
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            GebaeudeModellErgebnis nochmal = Vdi6007Rechenweg.Laufen(Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0), 0, 1);

            KuehlkreisErgebnis kk = r.Kuehlkreis;
            Assert.NotNull(kk);
            Assert.Null(r.Heizkreis);
            _aus.WriteLine($"Kälte {ideal.KuehlenergieMwh:0.0000} → {r.KuehlenergieMwh:0.0000} MWh; Spitze {ideal.KuehlbedarfKwh.Max():0.00} → {r.KuehlbedarfKwh.Max():0.00} kW; " +
                           $"Vorlauf {kk.VorlaufMittelC:0.00} °C, Rücklauf {kk.RuecklaufMittelC:0.00} °C, begrenzt {kk.UebergabeBegrenztStundenH:0.0} h " +
                           $"(Grenze {kk.VorlaufgrenzeStundenH:0.0} h); Überhitzung {ideal.Ueberhitzungsstunden} → {r.Ueberhitzungsstunden} h; Nennleistung {kk.UebergabeNennKw:0.00} kW");
            Assert.True(r.KuehlenergieMwh <= ideal.KuehlenergieMwh, "Band 1 K: weniger Kälte");
            Assert.True(r.Ueberhitzungsstunden >= ideal.Ueberhitzungsstunden, "Band 1 K: mehr Überhitzung");
            Assert.True(kk.Kuehlstunden > 0);
            Assert.Equal(18.0, kk.VorlaufMittelC);
            Assert.True(kk.RuecklaufMittelC > kk.VorlaufMittelC);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(18.0, kk.VorlaufC[h]);
                Assert.True(kk.RuecklaufC[h] >= kk.VorlaufC[h]);
                Assert.Equal(18.0 + r.KuehlbedarfKwh[h] * 1000.0 / e.KuehlUebergabeGespiegelt.WHWK, kk.RuecklaufC[h], 9);
                Assert.True(r.HeizlastW[h].Equals(nochmal.HeizlastW[h]) && r.KuehlbedarfKwh[h].Equals(nochmal.KuehlbedarfKwh[h]));
            }
            Assert.True(r.JahresheizwaermeMwh > 0.0);
        }

        /// <summary>
        /// Eine zu kleine Kühldecke bei Anlagenvorlauf 7 °C (auf 16 °C hochgemischt): Die begrenzten
        /// Stunden werden gezählt, die an der Vorlaufgrenze sind ein Teil davon, und die größte
        /// Überschreitung des Kühlsollwerts steht bereit.
        /// </summary>
        [Fact]
        public void Eine_zu_kleine_Kuehlflaeche_zaehlt_die_begrenzten_Stunden_und_die_Vorlaufgrenze()
        {
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Leistung_Nenn = 0.5), kuehlVorlaufAnlage: 7.0);
            Assert.True(e.KuehlVorlaufGekappt);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            KuehlkreisErgebnis kk = r.Kuehlkreis;
            _aus.WriteLine($"begrenzt {kk.UebergabeBegrenztStundenH:0.0} h, davon Vorlaufgrenze {kk.VorlaufgrenzeStundenH:0.0} h, " +
                           $"größte Überschreitung {kk.GroessteUeberschreitungK:0.00} K");
            Assert.True(kk.UebergabeBegrenztStundenH > 0.0);
            Assert.True(kk.VorlaufgrenzeStundenH > 0.0);
            Assert.True(kk.VorlaufgrenzeStundenH <= kk.UebergabeBegrenztStundenH);
            Assert.True(kk.GroessteUeberschreitungK > 0.0);
            Assert.Equal(16.0, kk.VorlaufFestC);
            Assert.Equal(7.0, kk.VorlaufQuelleC);
            Assert.True(kk.VorlaufGekappt);
        }

        /// <summary>Beide Seiten gekoppelt in einem Jahr: Heiz- und Kältekreis entstehen nebeneinander, die Gründe bleiben je Seite.</summary>
        [Fact]
        public void Beide_Seiten_gekoppelt()
        {
            GebaeudeModellEingang e = Eingang(Kuehlgekoppelt(g =>
            {
                g.Heizkreis_Aktiv = true;
                g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                g.Heizkurve_Aktiv = true;
            }), kuehlVorlaufAnlage: 18.0);
            Assert.True(e.KopplungWirksam && e.KuehlKopplungWirksam);
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.NotNull(r.Heizkreis);
            Assert.NotNull(r.Kuehlkreis);
            Assert.True(r.Heizkreis.Heizstunden > 1000 && r.Kuehlkreis.Kuehlstunden > 0);
        }

        /// <summary>Die Skalierung (E8) multipliziert die Leistungen der Auslegung und lässt Temperaturen und Stunden.</summary>
        [Fact]
        public void Die_Skalierung_multipliziert_die_Nennleistung_der_Kaelteseite()
        {
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0), 0, 1);
            GebaeudeModellErgebnis s = r.Skaliert(2.5);
            Assert.Equal(r.Kuehlkreis.UebergabeNennKw * 2.5, s.Kuehlkreis.UebergabeNennKw);
            Assert.Equal(r.Kuehlkreis.AuslegungskuehllastKw * 2.5, s.Kuehlkreis.AuslegungskuehllastKw);
            Assert.Equal(r.Kuehlkreis.VorlaufMittelC, s.Kuehlkreis.VorlaufMittelC);
            Assert.Equal(r.Kuehlkreis.UebergabeBegrenztStundenH, s.Kuehlkreis.UebergabeBegrenztStundenH);
            Assert.Equal(2.5, s.Kuehlkreis.Skalierungsfaktor);
        }

        /// <summary>
        /// <b>Probe 15 — Symmetrie</b> (E21, F-A16): Jede Spalte und jede Kennzahl der Wärmeseite von
        /// AK1 hat ein Kälte-Gegenstück — oder die Abweichung steht benannt in Anlagenkopplung 7.4.
        /// </summary>
        [Fact]
        public void Symmetrie_jede_Waermegroesse_hat_ein_Gegenstueck_oder_eine_benannte_Abweichung()
        {
            var gegenstueck = new Dictionary<string, string>
            {
                ["Heizkreis_Aktiv"] = "Kuehluebergabe_Aktiv",
                ["Uebergabe_Art"] = "Kuehl_Uebergabe_Art",
                ["Uebergabe_Exponent"] = "Kuehl_Uebergabe_Exponent",
                ["Uebergabe_Leistung_Nenn"] = "Kuehl_Uebergabe_Leistung_Nenn",
                ["Auslegung_Vorlauf"] = "Kuehl_Auslegung_Vorlauf",
                ["Auslegung_Ruecklauf"] = "Kuehl_Auslegung_Ruecklauf",
                ["Auslegung_Raumtemperatur"] = "Kuehl_Auslegung_Raumtemperatur",
                ["Vorlauf_Mittel"] = "Kuehl_Vorlauf_Mittel",
                ["Ruecklauf_Mittel"] = "Kuehl_Ruecklauf_Mittel",
                ["Uebergabe_Begrenzt_Stunden"] = "Kuehl_Uebergabe_Begrenzt_Stunden",
                ["VorlaufMittel_C"] = "KuehlVorlaufMittel_C",
                ["RuecklaufMittel_C"] = "KuehlRuecklaufMittel_C",
                ["UebergabeBegrenzt_H"] = "KuehlUebergabeBegrenzt_H",
            };
            // Die benannten Abweichungen (7.4 Punkte 5 bis 9) mit ihrem Stichwort im Papier.
            var abweichung = new Dictionary<string, string>
            {
                ["Auslegung_Aussentemperatur"] = "Auslegungstag",
                ["Heizkurve_Aktiv"] = "Keine Kühlkurve",
                ["Heizkurve_Niveau"] = "Keine Kühlkurve",
                ["Heizkurve_Steilheit"] = "Keine Kühlkurve",
                ["Regler_Proportionalband"] = "gemeinsames `Regler_Proportionalband`",
                ["Sollwertprofil"] = "Kein Sollwertprofil der Kühlung",
            };

            var kaelte = new HashSet<string>(GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Select(s => s.Key)
                .Concat(KuehluebergabeSchema.Ergebnisspalten.Select(s => s.Name))
                .Concat(KuehluebergabeSchema.SpaltenKuehlkreis.Select(s => s.Key)));
            var waerme = GebaeudeSchema.UEBERGABE_SPALTEN.Select(s => s.Key)
                .Concat(AnlagenkopplungSchema.Ergebnisspalten.Select(s => s.Name))
                .Concat(ErgebnisGebaeudeSchema.SpaltenHeizkreis.Select(s => s.Key));

            string papier = null;
            for (var d = new System.IO.DirectoryInfo(AppContext.BaseDirectory); d != null && papier == null; d = d.Parent)
            {
                string p = System.IO.Path.Combine(d.FullName, "Dokumentation", "aktuell", "Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md");
                if (System.IO.File.Exists(p)) papier = System.IO.File.ReadAllText(p);
            }
            string abschnitt74 = papier == null ? null
                : papier.Substring(papier.IndexOf("### 7.4", StringComparison.Ordinal),
                                   papier.IndexOf("## 8. Datenmodell", StringComparison.Ordinal) - papier.IndexOf("### 7.4", StringComparison.Ordinal));

            foreach (string w in waerme)
            {
                if (gegenstueck.TryGetValue(w, out string k))
                    Assert.True(kaelte.Contains(k), w + " → " + k + " fehlt auf der Kälteseite.");
                else
                {
                    Assert.True(abweichung.TryGetValue(w, out string stichwort), w + " hat weder Gegenstück noch benannte Abweichung.");
                    if (abschnitt74 != null) Assert.Contains(stichwort, abschnitt74);
                }
            }

            // Die Kennzahlen der Kreisergebnisse: jede Eigenschaft der Wärmeseite hat ihr Gegenstück.
            var kennzahl = new Dictionary<string, string>
            {
                ["Heizstunden"] = "Kuehlstunden",
                ["HeizleistungMaxStundenH"] = "KuehlleistungMaxStundenH",
                ["HeizgrenzeStundenH"] = "KeineKaelteStundenH",
                ["GroessteUnterschreitungK"] = "GroessteUeberschreitungK",
                ["AuslegungsheizlastKw"] = "AuslegungskuehllastKw",
                ["AuslegungAussenC"] = "AuslegungstagKuehlung",
                ["AuslegungAussenHergeleitet"] = "AuslegungstagKuehlung",
                ["HeizkurveAktiv"] = null,           // 7.4 Punkt 5
                ["SollwertprofilWirksam"] = null,    // 7.4 Punkt 7
            };
            var props = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            var kuehlNamen = new HashSet<string>(typeof(KuehlkreisErgebnis).GetProperties(props).Select(p => p.Name));
            foreach (System.Reflection.PropertyInfo p in typeof(HeizkreisErgebnis).GetProperties(props))
            {
                Assert.True(kennzahl.ContainsKey(p.Name), "Kennzahl " + p.Name + " ohne Zuordnung in der Symmetrieprobe.");
                if (kennzahl[p.Name] != null)
                    Assert.True(kuehlNamen.Contains(kennzahl[p.Name]), p.Name + " → " + kennzahl[p.Name] + " fehlt im Kältekreis.");
            }
        }

        /// <summary>Der Kältekreis des Projekts mittelt kältebedarfsgewichtet; mit einem Gebäude sind es dessen Zahlen.</summary>
        [Fact]
        public void Der_Kaeltekreis_des_Projekts()
        {
            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(Eingang(Kuehlgekoppelt(), kuehlVorlaufAnlage: 18.0), 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(Eingang(Kuehlgekoppelt(g => g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR),
                                                                        kuehlVorlaufAnlage: 12.0), 1, 2);
            KuehlkreisProjekt eins = KuehlkreisProjekt.Bilden(new[] { a });
            Assert.Equal(a.Kuehlkreis.VorlaufMittelC, eins.VorlaufMittelC);
            Assert.Equal(a.Kuehlkreis.RuecklaufMittelC, eins.RuecklaufMittelC);
            Assert.Equal(a.Kuehlkreis.UebergabeBegrenztStundenH, eins.UebergabeBegrenztStundenH);
            Assert.Null(KuehlkreisProjekt.Bilden(new[] { Vdi6007Rechenweg.Laufen(Vdi6007Probe.EingangGekuehlt(Vdi6007Probe.Gekuehlt(24.0), Klima), 0, 1) }));

            KuehlkreisProjekt zwei = KuehlkreisProjekt.Bilden(new[] { a, b });
            Assert.Equal(2, zwei.GekoppelteGebaeude);
            Assert.True(zwei.VorlaufMittelC > 12.0 && zwei.VorlaufMittelC < 18.0, "gemischt: " + zwei.VorlaufMittelC);
        }
    }
}
