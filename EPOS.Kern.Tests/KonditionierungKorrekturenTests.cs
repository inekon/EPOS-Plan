using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Korrekturen der Stufe KP1b im Rechenweg</b> (Welle R1; Entwurf KP1b Abschnitt 1,
    /// Konzept Konditionierungsprofile 3.6 und 6, Leitkonzept N1.56 Festlegung 7 und N1.61 Nr. 11):
    ///
    /// <list type="bullet">
    /// <item><b>G1</b> — „aus" in der ersten Vorlaufstunde startet wie eine unbeheizte Zone, auf
    /// allen drei Startwegen (Einzone, <see cref="Zonenlauf"/>, <see cref="Zonenschleife"/>).</item>
    /// <item><b>G2</b> — die Schwelle der Sommerlüftung folgt dem Kühlkalender je Stunde und fällt
    /// bei „aus" auf 23 °C; ohne Kalender bleibt jeder Ausdruck wörtlich.</item>
    /// <item><b>G3</b> — die Zuluft gekoppelter Zonen trägt denselben Zusatzleitwert wie der Rand.</item>
    /// <item><b>G6</b> — mit Kühlkalender entfällt die konstante Kühlprüfung (F17).</item>
    /// <item><b>R14</b> — ein erst durch den Kalender wirksamer Kühl-Nachtwert steht im Protokoll.</item>
    /// <item><b>E53</b> — mit AK1 bleibt der Vorlauf außerhalb der Heizperiode leer und zählt getrennt.</item>
    /// </list>
    ///
    /// <para>Ohne Datenbank: Die Kalender entstehen von Hand und kommen als
    /// <see cref="Konditionierungssatz"/> in den Eingangsbauer — genau wie im Lauf, wo der Datenweg
    /// sie liest.</para>
    /// </summary>
    public class KonditionierungKorrekturenTests
    {
        private const int JAHR = 2025;
        private const int START = 8760 - Vdi6007Rechenweg.VORLAUF_H;   // 8 040

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static int W0() => GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende());

        private static Konditionierungssatz Satz() => new Konditionierungssatz(W0(), JAHR);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static GebaeudeKlima KlimaDes()
            => new GebaeudeKlima(Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        /// <summary>Eine Woche, deren Werte die Funktion (Wochentag, Stunde) liefert.</summary>
        private static Konditionierungskalender Wochenkalender(Konditionierungsgroesse g, Func<int, int, double> wert)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = wert(w, st);
            return new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche), null, null);
        }

        /// <summary>Ein Kalender mit einem festen Wert über alle Stunden.</summary>
        private static Konditionierungskalender Konstant(Konditionierungsgroesse g, double wert)
            => new Konditionierungskalender(g, Kalenderangabe.AusWert(wert), null, null);

        /// <summary>
        /// Der Heizkalender, dessen Nachtstunden „aus" sind — Stunde <see cref="START"/> ist eine
        /// Nachtstunde (8 040 = Tag 335, Stunde 0), also steht der Vorlaufstart auf „aus".
        /// </summary>
        private static Konditionierungssatz SatzMitAusInDerStartstunde()
        {
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll,
                     Wochenkalender(Konditionierungsgroesse.Heizsoll,
                                    (w, st) => st >= 6 && st < 22 ? 20.0 : double.NaN));
            return s;
        }

        // =====================================================================
        //  G1 — Vorlaufstart bei „aus"
        // =====================================================================

        /// <summary>
        /// <b>G1 auf allen drei Startwegen:</b> Steht der Heizsollwert der ersten Vorlaufstunde auf
        /// „aus", startet der Lauf nicht mit NaN (das warf), sondern mit dem Mittel von θ_eq über die
        /// Vorlaufstunden (N1.56 Festlegung 7). Der Lauf kommt durch, und der Startwert liegt im
        /// Band der äquivalenten Außentemperatur.
        /// </summary>
        [Fact]
        public void Aus_in_der_ersten_Vorlaufstunde_startet_wie_eine_unbeheizte_Zone()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Konditionierungssatz s = SatzMitAusInDerStartstunde();

            // ---- Weg 1: die Einzone (Vdi6007Rechenweg.Laufen) ----
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, konditionierung: s);
            Assert.True(double.IsNaN(e.ThetaSoll[START]));

            double mittel = e.StartwertUnbeheiztC(START);
            double summe = 0.0;
            for (int h = START; h < 8760; h++) summe += e.ThetaEq[h];
            Assert.Equal(Bits(summe / Vdi6007Rechenweg.VORLAUF_H), Bits(mittel));
            Assert.Equal(Bits(mittel), Bits(Vdi6007Rechenweg.VorlaufStartwertC(e, START)));

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.True(r.JahresheizwaermeMwh > 0.0);

            // ---- Weg 2: der Zonenlauf (adiabater Vorlauf der 4-K-Regel) ----
            ZonenEingang einzeln = ZonenEingang.Einzeln(GebaeudeModellEingang.Bauen(
                g, KlimaDes(), konditionierung: s));
            GebaeudeModellErgebnis rz = Zonenlauf.Laufen(einzeln, 0, 1);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(Bits(r.Raumtemperatur[h]), Bits(rz.Raumtemperatur[h]));

            // ---- Weg 3: die Zonenschleife (zwei beheizte Zonen) ----
            ProjektGebaeudeModel zwei = ZonenEingangTests.ZweiBeheizte(Trennflaechenzuordnung.Innen);
            Mehrzonenergebnis m = Zonenrechnung.Rechnen(zwei, KlimaDes(), false, null, 0, zwei.ID_Gebaeude,
                                                        konditionierung: _ => s);
            Assert.Equal(2, m.Zonen.Count);
            Assert.True(m.Gebaeude.JahresheizwaermeMwh > 0.0);
            // Beide Zonen starten nach der Regel der unbeheizten Zone, nicht am NaN-Sollwert.
            foreach (double start in m.Schleife.Startwerte)
                Assert.True(!double.IsNaN(start) && start > -30.0 && start < 40.0, "Startwert " + start);
        }

        /// <summary>Ohne „aus" steht auf jedem Startweg wörtlich der Bestandsausdruck.</summary>
        [Fact]
        public void Ohne_aus_bleibt_der_Vorlaufstart_der_Sollwert_der_ersten_Vorlaufstunde()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima);
            Assert.Equal(Bits(e.ThetaSoll[START]), Bits(Vdi6007Rechenweg.VorlaufStartwertC(e, START)));
        }

        // =====================================================================
        //  G2 — die Schwelle der Sommerlüftung je Stunde
        // =====================================================================

        /// <summary>
        /// <b>G2:</b> Mit Kühlkalender ist die Schwelle θ_K(h) − 3 K, wo θ_K(h) endlich ist, und
        /// 23 °C, wo die Kühlung „aus" ist (Konzept 3.6). Ohne Kalender bleibt die eine Zahl.
        /// </summary>
        [Fact]
        public void Die_Sommerlueftungsschwelle_folgt_dem_Kuehlkalender_und_faellt_bei_aus_auf_23_Grad()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(27.0);
            g.Sommerlueftung = true;
            Konditionierungssatz s = Satz();
            // Tags 27 °C, nachts „aus" (+∞).
            s.Setzen(Konditionierungsgroesse.Kuehlsoll,
                     Wochenkalender(Konditionierungsgroesse.Kuehlsoll,
                                    (w, st) => st >= 7 && st < 19 ? 27.0 : double.NaN));
            GebaeudeModellEingang mit = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb: true, konditionierung: s);

            Assert.True(mit.KuehlkalenderWirksam);
            Sommerlueftungsregel regel = Vdi6007Rechenweg.LueftungsregelBilden(mit);
            Assert.NotNull(regel);
            Assert.Equal(27.0 - GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT, regel.SchwelleDerStunde(12));
            Assert.Equal(GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE, regel.SchwelleDerStunde(3));

            // Die Regel schaltet an der Schwelle der Stunde: 23,5 °C liegt nachts darüber (23 °C),
            // tags darunter (24 °C).
            regel.Zuruecksetzen();
            Assert.True(regel.Stunde(3, 23.5, 15.0));
            regel.Zuruecksetzen();
            Assert.False(regel.Stunde(12, 23.5, 15.0));
        }

        /// <summary>
        /// <b>Ohne Kühlkalender bleibt die Regel wörtlich:</b> Die Schwelle ist in jeder Stunde
        /// dieselbe Zahl wie bisher (θ_kuehl − 3 K mit wirksamer Kühlung, sonst 23 °C), und der
        /// Aufruf mit Stunde schaltet bitgleich wie der ohne.
        /// </summary>
        [Fact]
        public void Ohne_Kuehlkalender_bleibt_die_Sommerlueftungsregel_bitgleich()
        {
            ProjektGebaeudeModel gekuehlt = Vdi6007Probe.Gekuehlt(26.0);
            gekuehlt.Sommerlueftung = true;
            GebaeudeModellEingang mitKuehlung = Vdi6007Probe.EingangGekuehlt(gekuehlt, Klima);
            Assert.False(mitKuehlung.KuehlkalenderWirksam);

            ProjektGebaeudeModel frei = Vdi6007Probe.Gebaeude();
            frei.Sommerlueftung = true;
            GebaeudeModellEingang ohneKuehlung = Vdi6007Probe.Eingang(frei, Klima);

            foreach ((GebaeudeModellEingang e, double schwelle) in new[]
                     {
                         (mitKuehlung, 26.0 - GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT),
                         (ohneKuehlung, GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE),
                     })
            {
                Sommerlueftungsregel regel = Vdi6007Rechenweg.LueftungsregelBilden(e);
                foreach (int h in new[] { 0, 12, 3000, 8759 })
                    Assert.Equal(Bits(schwelle), Bits(regel.SchwelleDerStunde(h)));
            }

            // Derselbe Schaltvorgang: Aufruf mit und ohne Stunde liefert Zustand für Zustand dasselbe.
            var a = new Sommerlueftungsregel(23.0);
            var b = new Sommerlueftungsregel(23.0);
            var zufall = new Random(4711);
            for (int h = 0; h < 2000; h++)
            {
                double luft = 15.0 + 15.0 * zufall.NextDouble();
                double aussen = 5.0 + 25.0 * zufall.NextDouble();
                Assert.Equal(a.Stunde(luft, aussen), b.Stunde(h, luft, aussen));
            }
        }

        // =====================================================================
        //  G3 — die Zuluft gekoppelter Zonen
        // =====================================================================

        /// <summary>
        /// <b>G3:</b> Der Löser rechnet (g_ext + Z)·θ_Lue; θ_Lue muss deshalb denselben
        /// Zusatzleitwert tragen wie der Rand. Mit Lüftungskalender ist das der größere aus
        /// Kalenderüberschuss und Sommerlüftung — ohne ihn derselbe Ausdruck wie bisher (bitgleich).
        /// </summary>
        [Fact]
        public void Die_Zuluft_gekoppelter_Zonen_traegt_den_Kalenderzusatz()
        {
            ProjektGebaeudeModel g = ZonenEingangTests.ZweiBeheizte(Trennflaechenzuordnung.Innen);
            g.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 2, 200.0) };

            // Ein Lüftungskalender mit Nachtüberschuss: 2,0 1/h nachts, 0,4 1/h tags.
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Lueftung,
                     Wochenkalender(Konditionierungsgroesse.Lueftung,
                                    (w, st) => st >= 22 || st < 6 ? 2.0 : 0.4));

            IReadOnlyList<ZonenEingang> mit = ZonenEingang.Bauen(g, KlimaDes(), konditionierung: _ => s);
            ZonenEingang z = mit[0];
            GebaeudeModellEingang e = z.Eingang;
            Assert.NotNull(e.LueftungZusatzleitwertWK);
            Assert.True(e.LueftungZusatzleitwertWK[2] > 0.0);

            double[] luft = { 21.0, 19.0 };
            // θ_Lue = ((g_aussen + Z)·θ_out + Σ G_zj·θ_j) / (g_gesamt + Z) mit Z aus dem Rand.
            Assert.Equal(Bits(ThetaLueErwartet(z, 2, false, luft, e.ZusatzleitwertWK(2, false))),
                         Bits(z.ThetaLue(2, false, luft)));
            // In der Nachtstunde weicht sie vom alten Ausdruck (nur Sommerlüftung) ab.
            Assert.NotEqual(Bits(ThetaLueErwartet(z, 2, false, luft, 0.0)), Bits(z.ThetaLue(2, false, luft)));

            // ---- Ohne Lüftungskalender bitgleich zum Bestandsausdruck ----
            IReadOnlyList<ZonenEingang> ohne = ZonenEingang.Bauen(g, KlimaDes());
            ZonenEingang zo = ohne[0];
            Assert.Null(zo.Eingang.LueftungZusatzleitwertWK);
            foreach (int h in new[] { 2, 12, 5000 })
                foreach (bool sommer in new[] { false, true })
                {
                    double alt = sommer ? zo.Eingang.SommerlueftungZusatzleitwertWK : 0.0;
                    Assert.Equal(Bits(ThetaLueErwartet(zo, h, sommer, luft, alt)),
                                 Bits(zo.ThetaLue(h, sommer, luft)));
                }
        }

        /// <summary>θ_Lue nach dem Klassenkopf von <see cref="ZonenEingang"/>, mit vorgegebenem Z.</summary>
        private static double ThetaLueErwartet(ZonenEingang z, int h, bool sommer, double[] luft, double zusatz)
        {
            GebaeudeModellEingang e = z.Eingang;
            double gGesamt = 1.0 / e.Parameter.R_ext_KW;
            double gAussen = gGesamt - e.LuftaustauschLeitwert_WK;
            double zaehler = (gAussen + zusatz) * e.ThetaOut[h];
            for (int k = 0; k < e.Luftkopplungen.Count; k++)
                zaehler += e.Luftkopplungen[k].Leitwert_WK * luft[z.LuftIndex[k]];
            return zaehler / (gGesamt + zusatz);
        }

        // =====================================================================
        //  G6 — die doppelte Kühlprüfung
        // =====================================================================

        /// <summary>
        /// <b>G6:</b> Die konstante Prüfung θ_kuehl ≥ θ_soll,max + 1 K gilt nur ohne Kühlkalender.
        /// Mit ihm tritt die stündliche an ihre Stelle (F17) — ein gültiger Kalender wird nicht mehr
        /// wegen der Bestandsspalte abgelehnt.
        /// </summary>
        [Fact]
        public void Mit_Kuehlkalender_entfaellt_die_konstante_Kuehlpruefung()
        {
            // Kuehl_Sollwert 20,5 °C liegt nur 0,5 K über dem Tagsollwert 20 °C.
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(20.5);

            // Ohne Kalender: benannt abgelehnt (Bestand).
            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(
                () => Vdi6007Probe.EingangGekuehlt(g, Klima));
            Assert.Equal(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert, ex.Grund);

            // Mit Kalender, dessen Reihe überall genug Abstand hält: der Lauf kommt durch.
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Kuehlsoll, Konstant(Konditionierungsgroesse.Kuehlsoll, 26.0));
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb: true, konditionierung: s);
            Assert.True(e.KuehlkalenderWirksam);
            for (int h = 0; h < 8760; h++) Assert.Equal(26.0, e.ThetaMax[h]);

            // Die stündliche Prüfung greift weiterhin: eine Stunde unter dem Heizsollwert bricht ab.
            Konditionierungssatz eng = Satz();
            eng.Setzen(Konditionierungsgroesse.Kuehlsoll, Konstant(Konditionierungsgroesse.Kuehlsoll, 20.5));
            GebaeudeModellException ex2 = Assert.Throws<GebaeudeModellException>(
                () => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                                                  Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE,
                                                  kuehlbetrieb: true, konditionierung: eng));
            Assert.Equal(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert, ex2.Grund);
        }

        // =====================================================================
        //  R14 — der Hinweis auf den Kühl-Nachtwert
        // =====================================================================

        /// <summary>
        /// <b>R14:</b> <c>Kuehl_Sollwert_Nacht</c> wirkt erst, sobald ein Kühlkalender gilt — ohne
        /// Konditionierungszeile rechnet der Bestandszweig mit der Konstante. Der Lauf nennt den
        /// Übergang im Protokoll.
        /// </summary>
        [Fact]
        public void Ein_wirksamer_Kuehl_Nachtwert_wird_im_Protokoll_genannt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(26.0);
            g.Kuehl_Sollwert_Nacht = 28.0;

            // Ohne Konditionierung bleibt die Spalte wirkungslos: ThetaMax ist die Konstante.
            GebaeudeModellEingang ohne = Vdi6007Probe.EingangGekuehlt(g, Klima);
            Assert.Equal(0, ohne.KuehlNachtwertStundenH);
            for (int h = 0; h < 8760; h++) Assert.Equal(26.0, ohne.ThetaMax[h]);

            // Der abgeleitete Kalender aus der Matrix trägt den Nachtwert.
            Vorgabematrix matrix = Vorgabematrix.Bilden(Konditionierungseingang.Bestand(g, false, true), null);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, Konditionierungsgroesse.Kuehlsoll, false);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Kuehlsoll, l.Kalender);

            GebaeudeModellEingang mit = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb: true, konditionierung: s);
            Assert.True(mit.KuehlNachtwertStundenH > 0);
            Assert.Equal(28.0, mit.KuehlNachtwertC);

            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisKuehlNachtwert(ohne, "Probegebäude");
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);
            Vdi6007Rechenweg.HinweisKuehlNachtwert(mit, "Probegebäude");
            string hinweis = Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
            Assert.Contains("28", hinweis);
            Assert.Contains(mit.KuehlNachtwertStundenH.ToString(CultureInfo.InvariantCulture), hinweis);
        }

        // =====================================================================
        //  E53 — Heizperiode und AK1 im vollen Lauf
        // =====================================================================

        /// <summary>
        /// <b>E53 im vollen Lauf:</b> Außerhalb der Heizperiode (Heizsollwert „aus") liefert die
        /// Übergabe nichts — der Vorlauf der Kopplung bleibt leer wie jenseits der Heizgrenze —, und
        /// die Stunde zählt getrennt (<see cref="HeizkreisErgebnis.StundenOhneHeizungH"/>), nicht als
        /// Heizgrenzstunde (Konzept 6, Anlagenkopplung 3.4 Nr. 3).
        /// </summary>
        /// <remarks>
        /// Beide Vorlaufquellen (3.4 Punkt 4): die <b>Heizkurve</b>, deren Vorlauf am NaN-Sollwert
        /// von selbst leer ist, und der <b>feste Vorlauf der Anlage</b>, der ohne die Korrektur auch
        /// außerhalb der Heizperiode anstünde — dort trennt sich die Aussage.
        /// </remarks>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Heizperiode_mit_AK1_laesst_den_Vorlauf_leer_und_zaehlt_getrennt(bool heizkurve)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = heizkurve;
            double vorlaufAnlage = heizkurve ? double.NaN : 55.0;

            // Heizperiode 1.10. (Tag 274) bis 30.4. (Tag 120), über den Jahreswechsel.
            Vorgabematrix matrix = Vorgabematrix.Bilden(
                Konditionierungseingang.Bestand(g, true, false),
                new[]
                {
                    new Vorgabezeile
                    {
                        IdGebaeude = g.ID_Gebaeude, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                        Zeile = DbWerte.KOND_ZEILE_SAISON, Von = 274, Bis = 120,
                    },
                });
            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, Konditionierungsgroesse.Heizsoll, false);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            Konditionierungssatz s = Satz();
            s.Setzen(Konditionierungsgroesse.Heizsoll, l.Kalender);

            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, vorlaufAnlage);
            Assert.True(e.KopplungWirksam);
            Assert.Equal(heizkurve ? Vorlaufquelle.Heizkurve : Vorlaufquelle.Anlage, e.Vorlaufquelle);

            GebaeudeModellEingang mit = GebaeudeModellEingang.Bauen(
                g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, vorlaufAnlage,
                konditionierung: s);
            Assert.True(mit.KopplungWirksam);
            int aus = 0;
            for (int h = 0; h < 8760; h++) if (double.IsNaN(mit.ThetaSoll[h])) aus++;
            Assert.Equal(153 * 24, aus);
            Assert.Equal(aus, mit.StundenOhneHeizungH);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(mit, 0, 1);
            HeizkreisErgebnis hk = Assert.IsType<HeizkreisErgebnis>(r.Heizkreis);

            for (int h = 0; h < 8760; h++)
                if (double.IsNaN(mit.ThetaSoll[h]))
                {
                    Assert.True(double.IsNaN(hk.VorlaufC[h]), "Vorlauf in Stunde " + h);
                    Assert.True(double.IsNaN(hk.RuecklaufC[h]), "Rücklauf in Stunde " + h);
                    Assert.Equal(0.0, r.HeizlastW[h]);
                }
                else if (!heizkurve)
                {
                    // Mit festem Vorlauf der Anlage steht er in jeder Stunde der Heizperiode.
                    Assert.Equal(55.0, hk.VorlaufC[h]);
                }

            // Getrennt gezählt: die Aus-Stunden stehen in der eigenen Kennzahl, nicht in der
            // Heizgrenze — die zählt höchstens die Stunden innerhalb der Heizperiode.
            Assert.Equal(aus, hk.StundenOhneHeizungH);
            Assert.True(hk.HeizgrenzeStundenH <= 8760 - aus + 1e-9,
                        "Heizgrenzstunden " + hk.HeizgrenzeStundenH.ToString("0.##", CultureInfo.InvariantCulture));

            // Ohne Heizkalender bleibt die Kennzahl 0, und ein leerer Vorlauf hat allein den Grund
            // der Heizgrenze (Heizkurve unter der Raumluft) — keinen Sollwert „aus".
            GebaeudeModellErgebnis bestand = Vdi6007Rechenweg.Laufen(e, 0, 1);
            HeizkreisErgebnis hb = Assert.IsType<HeizkreisErgebnis>(bestand.Heizkreis);
            Assert.Equal(0, hb.StundenOhneHeizungH);
            for (int h = 0; h < 8760; h++)
                Assert.False(double.IsNaN(e.ThetaSoll[h]), "Ohne Kalender steht in jeder Stunde ein Sollwert.");
        }
    }
}
