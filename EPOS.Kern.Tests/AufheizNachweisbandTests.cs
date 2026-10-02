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
    /// <b>Das Bürogebäude der Aufheizproben</b> (Entwurf KP3, Welle R4): das Probegebäude
    /// (<see cref="Vdi6007Probe.Gebaeude"/>, Strahlungsanteil 0,3) im synthetischen Jahr mit Sonne
    /// (<see cref="Vdi6007Probe.Klima"/> mit <see cref="Vdi6007Probe.Jahresgang"/>) und dem Büro-Kalender in
    /// allen fünf Größen nach der ausgelieferten Vorlage „Büro" — werktags 7–18 Uhr 20 °C, sonst 16 °C;
    /// Kühlen 26 °C am Tag, sonst „aus" (Kühlbetrieb an); Lüftung am Tag 0,7 1/h, sonst 0,1 1/h; Geräte am
    /// Tag 1, sonst 0,1; Personen 7(8)–17 Uhr anwesend. Die Gebäudezeile trägt <c>Heizleistung_Max</c> nur in
    /// der Variante „Grenze".
    /// </summary>
    internal static class Bueroprobe
    {
        internal const double PERSONEN_NENN_W = 600.0;

        internal static readonly SolardatenModel[] Klimareihe = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        internal static GebaeudeKlima Klima(SolardatenModel[] reihe = null)
            => new GebaeudeKlima(reihe ?? Klimareihe, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        /// <summary>
        /// <b>Das Jahr mit Kälteeinbrüchen</b>: an den ersten <paramref name="tage"/> Werktagen ab dem 8. Januar
        /// liegt die Außenluft in den zwei Stunden nach dem Sprung (8 und 9 Uhr) um <paramref name="kelvin"/> K
        /// tiefer — die Formel sieht nur Fenster und Sprungstunde (Festlegung 9), der Lauf die ganze Spanne bis
        /// h_s + 2: W3 muss anschlagen.
        /// </summary>
        internal static SolardatenModel[] MitKaelteeinbruch(int tage = 6, double kelvin = 12.0)
        {
            SolardatenModel[] reihe = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            bool[] wochenende = Vdi6007Probe.Wochenende();
            int gesetzt = 0;
            for (int d = 7; d < 365 && gesetzt < tage; d++)
            {
                if (wochenende[d]) continue;
                for (int st = 8; st <= 9; st++) reihe[d * 24 + st].Außen_Temp -= kelvin;
                gesetzt++;
            }
            return reihe;
        }

        /// <summary>Werktag = Montag … Freitag (Wochentag 0 … 4).</summary>
        private static bool Werktag(int w) => w < 5;

        private static Konditionierungskalender Woche(Konditionierungsgroesse g, Func<int, int, double> wert, double? nenn = null)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = wert(w, st);
            return new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche), nenn, null);
        }

        /// <summary>Der Büro-Kalender aller fünf Größen (Werte der Vorlage „Büro", Lüftung am Tag mit dem Wert des Gebäudes).</summary>
        internal static Konditionierungssatz Satz()
        {
            var satz = new Konditionierungssatz(GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende()), 2025);
            satz.Setzen(Konditionierungsgroesse.Heizsoll,
                Woche(Konditionierungsgroesse.Heizsoll, (w, st) => Werktag(w) && st >= 7 && st < 18 ? 20.0 : 16.0));
            satz.Setzen(Konditionierungsgroesse.Kuehlsoll,
                Woche(Konditionierungsgroesse.Kuehlsoll, (w, st) => Werktag(w) && st >= 7 && st < 18 ? 26.0 : double.NaN));
            satz.Setzen(Konditionierungsgroesse.Lueftung,
                Woche(Konditionierungsgroesse.Lueftung, (w, st) => Werktag(w) && st >= 7 && st < 18 ? 0.7 : 0.1));
            satz.Setzen(Konditionierungsgroesse.Geraete,
                Woche(Konditionierungsgroesse.Geraete, (w, st) => Werktag(w) && st >= 7 && st < 18 ? 1.0 : 0.1));
            satz.Setzen(Konditionierungsgroesse.Personen,
                Woche(Konditionierungsgroesse.Personen, (w, st) => Werktag(w) && st >= 8 && st < 17 ? 1.0 : 0.0, PERSONEN_NENN_W));
            return satz;
        }

        /// <summary>Das Probegebäude mit Kühlung; <paramref name="heizMaxKw"/> setzt <c>Heizleistung_Max</c>.</summary>
        internal static ProjektGebaeudeModel Gebaeude(double? heizMaxKw = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 26.0;
            g.Heizleistung_Max = heizMaxKw;
            return g;
        }

        /// <summary>Der Eingang mit Büro-Kalender, Kühlbetrieb an.</summary>
        internal static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, SolardatenModel[] reihe = null)
            => GebaeudeModellEingang.Bauen(g, Klima(reihe), kuehlbetrieb: true, konditionierung: Satz());

        internal static Aufheizvorgabe An(bool abzug = false, double? reserve = null)
            => new Aufheizvorgabe(true, abzug ? DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG : null, null, reserve, null);

        /// <summary>
        /// <b>Die Grenze 1,02·Φ_stat an der kältesten Stunde</b> [kW] — Φ_stat der Zielleistung
        /// (θ_T,max, T_a,min, Luftwechsel der Nutzungszeit) aus der Bemessung ohne Grenze.
        /// </summary>
        internal static double GrenzeKw(double faktor = 1.02)
        {
            Aufheizbemessung b = Aufheizoptimierung.Bemessen(ZonenEingang.Einzeln(Eingang(Gebaeude())), An());
            Assert.False(b.QuelleGrenze);
            double phiStat = b.AufheizleistungW / (1.0 + An().ReserveWirksam);
            return faktor * phiStat / 1000.0;
        }

        /// <summary>Ein Lauf mit Plan: der Eingang, die Zone, der Plan, das Ergebnis über <see cref="Vdi6007Rechenweg.Laufen"/>.</summary>
        internal sealed class Lauf
        {
            internal GebaeudeModellEingang Eingang;
            internal ZonenEingang Zone;
            internal Aufheizplan Plan;
            internal GebaeudeModellErgebnis Ergebnis;
        }

        internal static Lauf Rechnen(ProjektGebaeudeModel g, Aufheizvorgabe vorgabe, SolardatenModel[] reihe = null)
        {
            GebaeudeModellEingang e = Eingang(g, reihe);
            ZonenEingang z = ZonenEingang.Einzeln(e);
            Aufheizplan p = vorgabe != null && vorgabe.An ? Aufheizoptimierung.Anwenden(z, vorgabe) : null;
            return new Lauf { Eingang = e, Zone = z, Plan = p, Ergebnis = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude, p) };
        }
    }

    /// <summary>
    /// <b>N-AH3 Orakel</b> (Entwurf KP3 Abschnitt 7, Welle R4; Teilkonzept 4.3, 4.9; Festlegung 19) — das
    /// Nachweisband im Lauf gegen drei unabhängige Wege, am Bürogebäude (<see cref="Bueroprobe"/>: synthetisches
    /// Jahr mit Sonne, Gewinnen, Lüftungskalender), in zwei Varianten: <b>Grenze</b> = <c>Heizleistung_Max</c>
    /// 1,02·Φ_stat an der kältesten Stunde (Augenblicksform, W3 = Kappungsanteil &gt; 0) und <b>Ziel</b> =
    /// Zielleistung (Stundenmittel, W3 = Stundenleistung &gt; 1,01·P_auf).
    /// <list type="number">
    /// <item>(i) In jedem Fenster [h_s − n + 1, h_s + 2] eines Tags ohne W1, W2 und W3 hält der Lauf das Band —
    /// ohne Ausnahme.</item>
    /// <item>(ii) Die W3-Tage des Laufs sind die unabhängig gezählten Überschreitungstage und
    /// <c>Aufheiztage_Nachweisband</c> — in beiden Jahresschleifen.</item>
    /// <item>(iii) Die Vorausrechnung auf einer Kopie des Lösers ab dem Zustand vor dem Fenster trifft den Lauf
    /// Bit für Bit.</item>
    /// <item>(iv) Lauf ohne Rampe + Σ Stufenantworten (über die Matrixfunktionen je Stunde, mit der
    /// Zustandsdifferenz am Fensterbeginn) = Lauf mit Rampe, wo jede Stunde in beiden Läufen geregelt ist —
    /// rel. ≤ 1e-9.</item>
    /// </list>
    /// Fenster, die über den Jahreswechsel reichen, prüfen (iii) und (iv) nicht (der Vorlauf ist ein anderer
    /// Zustand als das Jahresende); (i) und (ii) prüfen sie über den Ring wie der Lauf.
    /// </summary>
    public class AufheizNachweisbandTests
    {
        private const int STUNDEN = 8760;
        private readonly ITestOutputHelper _aus;

        public AufheizNachweisbandTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public static IEnumerable<object[]> Varianten()
            => new[] { "Grenze", "Grenze weit", "Grenze Kälteeinbruch", "Ziel", "Ziel knapp", "Ziel (b)" }.Select(v => new object[] { v });

        private static bool IstGrenze(string variante) => variante.StartsWith("Grenze", StringComparison.Ordinal);

        private static ProjektGebaeudeModel Gebaeude(string variante)
            => Bueroprobe.Gebaeude(variante == "Grenze" ? Bueroprobe.GrenzeKw()
                                   : IstGrenze(variante) ? Bueroprobe.GrenzeKw(1.10) : (double?)null);

        private static SolardatenModel[] Reihe(string variante)
            => variante == "Grenze Kälteeinbruch" ? Bueroprobe.MitKaelteeinbruch() : null;

        private static Aufheizvorgabe Vorgabe(string variante)
            => variante == "Ziel knapp" ? Bueroprobe.An(reserve: 0.02) : Bueroprobe.An(abzug: variante == "Ziel (b)");

        private static int Ring(int h) => ((h % STUNDEN) + STUNDEN) % STUNDEN;

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>Die Tage mit W1 oder W2 — dort sagt die Formel die Überschreitung selbst voraus.</summary>
        private static bool[] TageW12(Aufheizplan p)
        {
            var t = new bool[365];
            foreach (Aufheizsprung sp in p.Spruenge) if (sp.Unerreichbar || sp.Begrenzt) t[sp.Sprungstunde / 24] = true;
            return t;
        }

        private static IEnumerable<int> Fenster(Aufheizsprung sp)
        {
            for (int h = sp.Sprungstunde - sp.N + 1; h <= Math.Min(sp.Sprungstunde + 2, STUNDEN - 1); h++) yield return Ring(h);
        }

        // =====================================================================
        //  (i) und (ii)
        // =====================================================================

        [Theory]
        [MemberData(nameof(Varianten))]
        public void N_AH3_i_ii_Das_Band_haelt_ausser_an_W3_Tagen_und_W3_zaehlt_unabhaengig_gleich(string variante)
        {
            ProjektGebaeudeModel g = Gebaeude(variante);
            Bueroprobe.Lauf lauf = Bueroprobe.Rechnen(g, Vorgabe(variante), Reihe(variante));
            Aufheizplan p = lauf.Plan;
            GebaeudeModellErgebnis r = lauf.Ergebnis;
            Aufheizergebnis a = r.Aufheizung;
            Assert.NotNull(a);
            bool grenze = IstGrenze(variante);
            Assert.Equal(grenze, p.Bemessung.QuelleGrenze);
            Assert.Equal(grenze ? DbWerte.AUFHEIZ_QUELLE_GRENZE : DbWerte.AUFHEIZ_QUELLE_ZIEL, a.AufheizLeistungsquelle);
            Assert.True(p.Aufheiztage > 0, "keine Rampe: das Orakel prüfte nichts");

            // Beide Jahresschleifen (B7): der Zonenlauf derselben Zone ist bitgleich, samt Kappung und W3.
            GebaeudeModellErgebnis rz = Zonenlauf.Laufen(lauf.Zone, 0, g.ID_Gebaeude);
            AufheizGrenzfallTests.Bitgleich(r, rz, variante + ", Zonenlauf");
            AufheizGrenzfallTests.Bitgleich(r.HeizleistungMaxAnteil, rz.HeizleistungMaxAnteil, variante + ", Kappungsanteil");
            Assert.Equal(a with { Rampenmaske = null, Nachweisbandtage = null },
                         rz.Aufheizung with { Rampenmaske = null, Nachweisbandtage = null });
            Assert.Equal(a.Nachweisbandtage, rz.Aufheizung.Nachweisbandtage);

            double pAuf = p.Bemessung.AufheizleistungW;
            double band = 1.01 * pAuf;
            bool[] w12 = TageW12(p);
            var unabhaengig = new bool[365];
            int fensterGeprueft = 0;
            double hoechsteQuote = 0.0, hoechsteKappung = 0.0;
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                int tag = sp.Sprungstunde / 24;
                if (w12[tag]) continue;
                bool ueber = false;
                foreach (int h in Fenster(sp))
                    if (grenze ? r.HeizleistungMaxAnteil[h] > 0.0 : r.HeizlastW[h] > band) ueber = true;
                if (ueber) unabhaengig[tag] = true;
            }
            // (i) ohne Ausnahme: jedes Fenster eines Tags ohne W1, W2, W3 hält das Band.
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                int tag = sp.Sprungstunde / 24;
                if (w12[tag] || a.Nachweisbandtage[tag]) continue;
                fensterGeprueft++;
                foreach (int h in Fenster(sp))
                {
                    if (grenze)
                    {
                        Assert.True(r.HeizleistungMaxAnteil[h] <= Rechenrand.Zu(0.0), "Kappung in Stunde " + h);
                        hoechsteKappung = Math.Max(hoechsteKappung, r.HeizleistungMaxAnteil[h]);
                    }
                    else
                        Assert.True(r.HeizlastW[h] - band <= Rechenrand.Zu(band), "über dem Band in Stunde " + h);
                    hoechsteQuote = Math.Max(hoechsteQuote, r.HeizlastW[h] / pAuf);
                }
            }
            // (ii) die W3-Tage = unabhängig gezählt = Aufheiztage_Nachweisband.
            Assert.Equal(unabhaengig, a.Nachweisbandtage);
            Assert.Equal(unabhaengig.Count(x => x), a.AufheiztageNachweisband);
            Assert.True(fensterGeprueft > 0);
            if (variante == "Grenze Kälteeinbruch") Assert.True(a.AufheiztageNachweisband > 0, "W3 schlägt beim Kälteeinbruch nicht an");

            // Zum Vergleich: der Lauf ohne Rampe in denselben Fenstern.
            GebaeudeModellErgebnis ohne = Vdi6007Rechenweg.Laufen(Bueroprobe.Eingang(g, Reihe(variante)), 0, g.ID_Gebaeude);
            int ueberOhne = 0;
            foreach (Aufheizsprung sp in p.Spruenge.Where(x => x.N > 1))
                if (Fenster(sp).Any(h => grenze ? ohne.HeizleistungMaxAnteil[h] > 0.0 : ohne.HeizlastW[h] > band)) ueberOhne++;

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH3 {0}: P_auf {1:0} W ({2}), t_auf,max {3} h, Sprünge {4}, Rampentage {5}, Σ(n−1) {6} h, längste {7} h, " +
                "W1 {8}, W2 {9}, W3 {10}, W4 {11}; Fenster ohne W1–W3 geprüft {12}, höchste Stundenleistung {13:0.0000}·P_auf, " +
                "höchster Kappungsanteil {14:R}; HeizleistungMax_H {15:0.###} h; gerampte Fenster über dem Band ohne Rampe {16} von {17}",
                variante, pAuf, a.AufheizLeistungsquelle, a.AufheizzeitMaxH, p.Spruenge.Count, a.Aufheiztage, a.AufheizstundenH,
                a.AufheizzeitLaengsteH, a.AufheiztageUnerreichbar, a.AufheiztageBegrenzt, a.AufheiztageNachweisband,
                a.AufheizspruengeAus, fensterGeprueft, hoechsteQuote, hoechsteKappung, a.HeizleistungMaxStundenH, ueberOhne,
                p.Spruenge.Count(x => x.N > 1)));
        }

        // =====================================================================
        //  (iii) und (iv): die Jahresschleife nachgebaut, mit den Zuständen je Stunde
        // =====================================================================

        /// <summary>Der Lauf eines Eingangs Stunde für Stunde nachgebaut: Zustand vor jeder Stunde, Leistung, Fall.</summary>
        private sealed class Nachbau
        {
            internal readonly double[] MAw = new double[STUNDEN], MIw = new double[STUNDEN], Heiz = new double[STUNDEN];
            internal readonly bool[] Geregelt = new bool[STUNDEN];

            internal Nachbau(GebaeudeModellEingang e)
            {
                Assert.Null(Vdi6007Rechenweg.LueftungsregelBilden(e));
                Assert.Null(Vdi6007Rechenweg.NachtauskuehlregelBilden(e));
                var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
                int start = STUNDEN - Vdi6007Rechenweg.VORLAUF_H;
                m.Zuruecksetzen(Vdi6007Rechenweg.VorlaufStartwertC(e, start));
                for (int h = start; h < STUNDEN; h++) m.Schritt(e.Rand(h, false, false));
                for (int h = 0; h < STUNDEN; h++)
                {
                    MAw[h] = m.ThetaMAw;
                    MIw[h] = m.ThetaMIw;
                    Stundenergebnis s = m.Schritt(e.Rand(h, false, false));
                    Heiz[h] = s.HeizleistungW;
                    Betriebsfall[] f = m.LetzteFallfolge;
                    Geregelt[h] = f.Length == 1 && f[0] == Betriebsfall.HeizenGeregelt;
                }
            }
        }

        [Theory]
        [MemberData(nameof(Varianten))]
        public void N_AH3_iii_iv_Vorausrechnung_bitgleich_und_Ueberlagerung_wo_geregelt(string variante)
        {
            ProjektGebaeudeModel g = Gebaeude(variante);
            Bueroprobe.Lauf lauf = Bueroprobe.Rechnen(g, Vorgabe(variante), Reihe(variante));
            GebaeudeModellEingang e = lauf.Eingang, e0 = Bueroprobe.Eingang(g, Reihe(variante));
            GebaeudeModellErgebnis r = lauf.Ergebnis;
            GebaeudeModellErgebnis r0 = Vdi6007Rechenweg.Laufen(e0, 0, g.ID_Gebaeude);
            var mit = new Nachbau(e);
            var ohne = new Nachbau(e0);
            AufheizGrenzfallTests.Bitgleich(mit.Heiz, r.HeizlastW, variante + ", Nachbau mit Rampe");
            AufheizGrenzfallTests.Bitgleich(ohne.Heiz, r0.HeizlastW, variante + ", Nachbau ohne Rampe");

            List<Aufheizsprung> gerampt = lauf.Plan.Spruenge.Where(x => x.N > 1 && x.Sprungstunde - x.N + 1 >= 0).ToList();
            Assert.NotEmpty(gerampt);

            // (iii) Vorausrechnung auf einer Kopie ab dem Zustand vor dem Fenster.
            int vorausFenster = 0, vorausStunden = 0;
            foreach (Aufheizsprung sp in gerampt)
            {
                int h0 = sp.Sprungstunde - sp.N + 1, bis = Math.Min(sp.Sprungstunde + 2, STUNDEN - 1);
                var kopie = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
                kopie.Zuruecksetzen(mit.MAw[h0], mit.MIw[h0]);
                for (int h = h0; h <= bis; h++)
                {
                    Stundenergebnis s = kopie.Schritt(e.Rand(h, false, false));
                    Assert.True(Bits(s.HeizleistungW) == Bits(r.HeizlastW[h]),
                        string.Format(CultureInfo.InvariantCulture, "{0}: Vorausrechnung Stunde {1}: {2:R} gegen {3:R}",
                                      variante, h, s.HeizleistungW, r.HeizlastW[h]));
                    Assert.True(Bits(s.HeizleistungMaxAnteil) == Bits(r.HeizleistungMaxAnteil[h]), "Kappung, Stunde " + h);
                    vorausStunden++;
                }
                vorausFenster++;
            }

            // (iv) Überlagerung: ohne Rampe + Antwort auf (Zustandsdifferenz am Fensterbeginn, Δs je Stunde).
            Zonenmodell2K modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            int ganz = 0, teilweise = 0, stunden = 0;
            double groesste = 0.0;
            foreach (Aufheizsprung sp in gerampt)
            {
                int h0 = sp.Sprungstunde - sp.N + 1, bis = Math.Min(sp.Sprungstunde + 2, STUNDEN - 1);
                var dx = new Vektor2(mit.MAw[h0] - ohne.MAw[h0], mit.MIw[h0] - ohne.MIw[h0]);
                bool vollstaendig = true;
                for (int h = h0; h <= bis; h++)
                {
                    if (!mit.Geregelt[h] || !ohne.Geregelt[h])
                    {
                        vollstaendig = false;
                        break;
                    }
                    Aufheizantwort an = modell.Aufheizantwort(e.HeizungStrahlungsanteil, e.ZusatzleitwertWK(h, false, false));
                    double ds = e.ThetaSoll[h] - e0.ThetaSoll[h];
                    Vektor2 db = ds * an.DeltaB;
                    Uebergang u = an.Stunde;
                    Vektor2 mittel = (1.0 / u.TauS) * (u.Gamma * dx + u.Psi * db);
                    double dP = an.Z.A * mittel.A + an.Z.B * mittel.B + an.G0WK * ds;
                    double vorhersage = r0.HeizlastW[h] + dP;
                    double rel = Math.Abs(vorhersage - r.HeizlastW[h]) / Math.Max(Math.Abs(r.HeizlastW[h]), 1.0);
                    Assert.True(rel <= 1e-9,
                        string.Format(CultureInfo.InvariantCulture, "{0}: Überlagerung Stunde {1}: {2:R} gegen {3:R} (rel. {4:E2})",
                                      variante, h, vorhersage, r.HeizlastW[h], rel));
                    groesste = Math.Max(groesste, rel);
                    stunden++;
                    dx = u.Ende(dx, db);
                }
                if (vollstaendig) ganz++;
                else teilweise++;
            }
            Assert.True(ganz > 0, "kein durchgehend geregeltes Fenster");

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH3 {0}: (iii) {1} Fenster, {2} Stunden bitgleich; (iv) {3} Fenster durchgehend geregelt, {4} bis zur ersten " +
                "ungeregelten Stunde, {5} Stunden, größte Abweichung rel. {6:E2}",
                variante, vorausFenster, vorausStunden, ganz, teilweise, stunden, groesste));
        }
    }
}
