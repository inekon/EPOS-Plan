using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kühlkurve als Jahresreihe</b> (Entwurf KK, 2.1; Festlegungen 1–5, 7, 13; Wellenzeile KK1) — ohne Datenbank, am
    /// Probegebäude aus <see cref="Vdi6007Probe"/>: die Form der Zwei-Punkt-Kurve, der Fußpunkt (leer = Auslegungsrücklauf),
    /// die Grenzen (Mindestabstand oben, Erzeuger, Vorlaufgrenze unten), die hergeleitete Auslegungs-Außentemperatur, die
    /// Probe des Mindestabstands, „Reihe konstant = heute bitgleich“ und „Schalter aus = keine Wirkung“.
    /// </summary>
    public class KuehlkurveTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KuehlkurveTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>Das Probegebäude, gekühlt auf 24 °C, Kühldecke mit Vorgaben (16/19 °C, Grenze 16 °C).</summary>
        private static ProjektGebaeudeModel Gebaeude(Action<ProjektGebaeudeModel> aendern = null, double kuehlSollC = 24.0)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(kuehlSollC);
            g.Kuehluebergabe_Aktiv = true;
            g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            aendern?.Invoke(g);
            return g;
        }

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, string stufe = DbWerte.ANLAGENKOPPLUNG_AK3,
                                                     double kuehlVorlaufAnlage = double.NaN)
            => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, true, stufe, double.NaN, 1.0, kuehlVorlaufAnlage);

        private static GebaeudeModellEingang EingangEin(ProjektGebaeudeModel g, string stufe = DbWerte.ANLAGENKOPPLUNG_AK3,
                                                        double kuehlVorlaufAnlage = double.NaN)
        {
            using (KuehlkurveKernschalter.Schalten(true))
                return Eingang(g, stufe, kuehlVorlaufAnlage);
        }

        private static void Bitgleich(GebaeudeModellErgebnis a, GebaeudeModellErgebnis b)
        {
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(a.HeizlastW[h].Equals(b.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(a.Raumtemperatur[h].Equals(b.Raumtemperatur[h]), "Raumluft, Stunde " + h);
                Assert.True(a.KuehlbedarfKwh[h].Equals(b.KuehlbedarfKwh[h]), "Kühlbedarf, Stunde " + h);
                Assert.True(a.Kuehlkreis.VorlaufC[h].Equals(b.Kuehlkreis.VorlaufC[h]), "Kühlvorlauf, Stunde " + h);
                Assert.True(a.Kuehlkreis.RuecklaufC[h].Equals(b.Kuehlkreis.RuecklaufC[h]), "Kühlrücklauf, Stunde " + h);
            }
            Assert.Equal(a.Kuehlkreis.VorlaufgrenzeStundenH, b.Kuehlkreis.VorlaufgrenzeStundenH);
            Assert.Equal(a.Ueberhitzungsstunden, b.Ueberhitzungsstunden);
        }

        // =====================================================================
        //  Die Klasse
        // =====================================================================

        /// <summary>Die Form (Festlegung 2): Fußpunkt bis zum Sollwert, linear, Auslegungspunkt ab θ_out,K,N; nie NaN.</summary>
        [Fact]
        public void Form_der_Zwei_Punkt_Kurve()
        {
            var k = new Kuehlkurve(fusspunktC: 20.0, auslegungVorlaufC: 16.0, auslegungAussenC: 32.0, vorlaufgrenzeC: double.NaN);
            Assert.Equal(20.0, k.KurveC(24.0, 10.0));
            Assert.Equal(20.0, k.KurveC(24.0, 24.0));
            Assert.Equal(18.0, k.KurveC(24.0, 28.0));
            Assert.Equal(17.0, k.KurveC(24.0, 30.0));
            Assert.Equal(16.0, k.KurveC(24.0, 32.0));
            Assert.Equal(16.0, k.KurveC(24.0, 38.0));
            // Ohne endlichen Kühlsollwert der Fußpunkt — waagrecht, nie „aus“.
            Assert.Equal(20.0, k.KurveC(double.NaN, 35.0));
            Assert.Equal(20.0, k.KurveC(double.PositiveInfinity, 35.0));
            // Monoton fallend zwischen den Punkten.
            double vor = double.PositiveInfinity;
            for (double t = 20.0; t <= 36.0; t += 0.25)
            {
                double v = k.KurveC(24.0, t);
                Assert.False(double.IsNaN(v));
                Assert.True(v <= vor, "monoton bei " + t);
                vor = v;
            }
            // Auslegungs-Außentemperatur nicht über dem Sollwert: Sprung am Sollwert.
            var flach = new Kuehlkurve(20.0, 16.0, 23.0, double.NaN);
            Assert.Equal(20.0, flach.KurveC(24.0, 24.0));
            Assert.Equal(16.0, flach.KurveC(24.0, 24.5));
        }

        /// <summary>
        /// Die Grenzen (Festlegungen 4, 5): oben θ_max − Mindestabstand, kälter als der Erzeuger nie, unten immer die
        /// Vorlaufgrenze — sie gewinnt auch gegen die obere Grenze; der Befund „an der Grenze“ nur, wenn sie anhebt.
        /// </summary>
        [Fact]
        public void Grenzen_und_Vorlaufgrenze_als_Untergrenze()
        {
            double abstand = GebaeudeFestwerte.KUEHLKURVE_FUSSPUNKT_ABSTAND_K;
            var k = new Kuehlkurve(23.0, 12.0, 32.0, 16.0);
            Assert.Equal(22.0 - abstand, k.VorlaufC(22.0, 15.0, double.NaN, out bool g1));
            Assert.False(g1);
            Assert.Equal(16.0, k.VorlaufC(24.0, 32.0, double.NaN, out bool g2));
            Assert.True(g2);
            Assert.Equal(18.0, k.VorlaufC(24.0, 32.0, 18.0, out bool g3));
            Assert.False(g3);
            Assert.Equal(16.0, k.VorlaufC(24.0, 32.0, 10.0, out bool g4));
            Assert.True(g4);
            // Die Vorlaufgrenze gewinnt gegen den Mindestabstand (Sollwert 17 °C → oben 14,5 °C).
            Assert.Equal(16.0, k.VorlaufC(17.0, 15.0, double.NaN, out bool g5));
            Assert.True(g5);
            // Ohne Grenze (Gebläsekonvektor) der Auslegungspunkt; ohne Sollwert der Fußpunkt.
            var konvektor = new Kuehlkurve(12.0, 7.0, 32.0, double.NaN);
            Assert.Equal(7.0, konvektor.VorlaufC(24.0, 33.0, double.NaN, out bool g6));
            Assert.False(g6);
            Assert.Equal(12.0, konvektor.VorlaufC(double.NaN, 33.0, double.NaN, out _));
            Assert.Equal(double.PositiveInfinity, Kuehlkurve.ObergrenzeC(double.NaN));
        }

        /// <summary>
        /// <b>Die Probe des Mindestabstands</b> (Festlegung 4): <see cref="GebaeudeFestwerte.KUEHLKURVE_FUSSPUNKT_ABSTAND_K"/>
        /// ist der kleinste Halbkelvinschritt, bei dem die voll geöffnete Kühlübergabe jeder Art mit ihren Vorgaben bei
        /// Raumluft am Kühlsollwert über den ganzen Bereich der Auslegungs-Raumtemperatur noch
        /// <see cref="GebaeudeFestwerte.KUEHLKURVE_PROBE_LEISTUNGSANTEIL_MIN"/> der Nennleistung liefert.
        /// </summary>
        [Fact]
        public void Mindestabstand_Probe()
        {
            string[] arten = { DbWerte.KUEHLUEBERGABE_KUEHLDECKE, DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR };
            double Schwaechster(double abstand)
            {
                double min = double.PositiveInfinity;
                foreach (string art in arten)
                    for (double iN = GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MIN; iN <= GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MAX; iN += 1.0)
                    {
                        Uebergabekennwerte k = Kuehluebergabe.Gespiegelt(1000.0, Kuehluebergabe.VorgabeExponent(art),
                                                                         Kuehluebergabe.VorgabeVorlaufC(art), Kuehluebergabe.VorgabeRuecklaufC(art), iN);
                        min = Math.Min(min, Kuehluebergabe.LeistungOffenW(k, iN - abstand, iN) / 1000.0);
                    }
                return min;
            }
            double festwert = GebaeudeFestwerte.KUEHLKURVE_FUSSPUNKT_ABSTAND_K;
            double an = Schwaechster(festwert), darunter = Schwaechster(festwert - 0.5);
            _aus.WriteLine($"Mindestabstand {festwert} K: schwächster Anteil {an:P1}; {festwert - 0.5} K: {darunter:P1}");
            Assert.True(an >= GebaeudeFestwerte.KUEHLKURVE_PROBE_LEISTUNGSANTEIL_MIN, "der Festwert erfüllt das Kriterium");
            Assert.True(darunter < GebaeudeFestwerte.KUEHLKURVE_PROBE_LEISTUNGSANTEIL_MIN, "ein halbes Kelvin weniger nicht");
        }

        // =====================================================================
        //  Der Eingang
        // =====================================================================

        /// <summary>
        /// Der Eingang mit Kernschalter und AK3: Fußpunkt leer = Auslegungsrücklauf, eingetragen gilt er; die
        /// Auslegungs-Außentemperatur nach Weg 2 (Festlegung 3, E107); Bereiche (Festlegung 7) hart geprüft.
        /// </summary>
        [Fact]
        public void Fusspunkt_Auslegungsaussentemperatur_und_Bereiche_im_Eingang()
        {
            GebaeudeModellEingang leer = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true));
            Assert.True(leer.KuehlkurveWirksam);
            Assert.Equal(GebaeudeFestwerte.KUEHL_AUSLEGUNG_RUECKLAUF_FLAECHE, leer.Kuehlkurve.FusspunktC);
            Assert.Equal(GebaeudeFestwerte.KUEHL_AUSLEGUNG_VORLAUF_FLAECHE, leer.Kuehlkurve.AuslegungVorlaufC);
            GebaeudeModellEingang.WaermsterTag(leer.ThetaOut, out double mittel);
            Assert.Equal(Math.Max(mittel, 24.0 + GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_SPANNE_K), leer.Kuehlkurve.AuslegungAussenC);
            Assert.Equal(mittel, Kuehlkurve.AuslegungAussentemperaturC(leer.ThetaOut));
            Assert.Equal(16.0, leer.Kuehlkurve.VorlaufgrenzeC);
            Assert.True(double.IsNaN(leer.KuehlVorlaufFestC));
            Assert.Equal(0.0, leer.KuehlkurveRaumeinflussKK);

            GebaeudeModellEingang gesetzt = EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 20.0; g.Kuehlkurve_Raumeinfluss = 1.5; }));
            Assert.Equal(20.0, gesetzt.Kuehlkurve.FusspunktC);
            Assert.Equal(1.5, gesetzt.KuehlkurveRaumeinflussKK);

            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Assert.Throws<GebaeudeModellException>(
                () => EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 3.0; }))).Grund);
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Assert.Throws<GebaeudeModellException>(
                () => EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Raumeinfluss = 10.5; }))).Grund);
        }

        /// <summary>
        /// Die Reihe am Eingang und im Jahreslauf: Jede Stunde hält die Grenzen, der Rand liest <c>[h]</c> — der Vorlauf des
        /// Kältekreises ist Stunde für Stunde die Reihe; an warmen Stunden kälter als am Fußpunkt. Kühlsollwert 21 °C, damit
        /// die Probeklima-Reihe viele Stunden auf dem linearen Ast hat (Auslegungspunkt nach Weg 2: 29 °C).
        /// </summary>
        [Fact]
        public void Die_Reihe_gleitet_und_der_Rand_liest_die_Stunde()
        {
            GebaeudeModellEingang e = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true, kuehlSollC: 21.0));
            Assert.True(e.Kuehlkurve.AuslegungAussenC > 21.0, "linearer Ast vorhanden");
            double[] v = e.KuehlVorlaufC;
            Assert.Equal(8760, v.Length);
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(v[h] >= 16.0 && v[h] <= 19.0, "Grenzen, Stunde " + h);
                Assert.True(v[h] <= Kuehlkurve.ObergrenzeC(e.ThetaMax[h]) || e.KuehlVorlaufAnGrenze[h], "Abstand, Stunde " + h);
            }
            Assert.True(v.Distinct().Count() > 2, "die Reihe gleitet");

            // Der Kältekreis führt die Stunden ohne Kühlfreigabe (θ_max = ∞, Heiztag der Zone) als NaN.
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            int verglichen = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (double.IsInfinity(e.ThetaMax[h])) continue;
                verglichen++;
                Assert.True(r.Kuehlkreis.VorlaufC[h].Equals(v[h]), "Rand liest [h], Stunde " + h + ": " + r.Kuehlkreis.VorlaufC[h] + " statt " + v[h]);
            }
            Assert.True(verglichen > 4000, "Stunden mit Kühlfreigabe: " + verglichen);
            GebaeudeModellErgebnis fest = Vdi6007Rechenweg.Laufen(Eingang(Gebaeude(kuehlSollC: 21.0)), 0, 1);
            _aus.WriteLine($"Kühlkurve: Vorlauf {r.Kuehlkreis.VorlaufMittelC:0.00} °C, Kälte {r.KuehlenergieMwh:0.0000} MWh; " +
                           $"fest: {fest.Kuehlkreis.VorlaufMittelC:0.00} °C, {fest.KuehlenergieMwh:0.0000} MWh");
        }

        /// <summary>
        /// <b>Reihe konstant = heute bitgleich</b>: Ohne Kühlkurve ist die Reihe Stunde für Stunde der feste Vorlauf; eine
        /// Kurve, deren Reihe auf denselben Wert fällt (Fußpunkt 18 °C, Anlage 18 °C), rechnet das Jahr bitgleich zum festen Weg.
        /// </summary>
        [Fact]
        public void Reihe_konstant_ist_heute_bitgleich()
        {
            GebaeudeModellEingang fest = Eingang(Gebaeude(), kuehlVorlaufAnlage: 18.0);
            Assert.False(fest.KuehlkurveWirksam);
            Assert.Null(fest.KuehlVorlaufAnGrenze);
            Assert.Equal(18.0, fest.KuehlVorlaufFestC);
            Assert.All(fest.KuehlVorlaufC, x => Assert.True(x.Equals(fest.KuehlVorlaufFestC)));

            GebaeudeModellEingang kurve = EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 18.0; }),
                                                     kuehlVorlaufAnlage: 18.0);
            Assert.True(kurve.KuehlkurveWirksam);
            Assert.All(kurve.KuehlVorlaufC, x => Assert.True(x.Equals(18.0)));
            Bitgleich(Vdi6007Rechenweg.Laufen(fest, 0, 1), Vdi6007Rechenweg.Laufen(kurve, 0, 1));

            // Ohne Kälteseite keine Reihe.
            Assert.Null(Eingang(Vdi6007Probe.Gekuehlt(24.0)).KuehlVorlaufC);
        }

        /// <summary>
        /// <b>Schalter aus = keine Wirkung</b> (Festlegungen 1, 13; E106 Q-KK-2 (a)): ohne Kernschalter, auf AK1/AK2 oder ohne
        /// <c>Kuehlkurve_Aktiv</c> rechnet das Gebäude bitgleich zum festen Vorlauf, auch mit gesetztem Fußpunkt.
        /// </summary>
        [Fact]
        public void Schalter_aus_ist_ohne_Wirkung()
        {
            Assert.False(KuehlkurveKernschalter.Ein);
            Action<ProjektGebaeudeModel> kurve = g =>
            {
                g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 21.0; g.Kuehlkurve_Raumeinfluss = 2.0;
                g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE; g.Kuehlkurve_Auslegung_Aussen = 20.0;
            };
            GebaeudeModellErgebnis bezug = Vdi6007Rechenweg.Laufen(Eingang(Gebaeude()), 0, 1);

            GebaeudeModellEingang aus = Eingang(Gebaeude(kurve));
            Assert.False(aus.KuehlkurveWirksam);
            Assert.Null(aus.Kuehlkurve);
            Bitgleich(bezug, Vdi6007Rechenweg.Laufen(aus, 0, 1));

            foreach (string stufe in new[] { DbWerte.ANLAGENKOPPLUNG_AK1, DbWerte.ANLAGENKOPPLUNG_AK2 })
            {
                GebaeudeModellEingang e = EingangEin(Gebaeude(kurve), stufe);
                Assert.False(e.KuehlkurveWirksam, stufe);
                Bitgleich(Vdi6007Rechenweg.Laufen(Eingang(Gebaeude(), stufe), 0, 1), Vdi6007Rechenweg.Laufen(e, 0, 1));
            }

            GebaeudeModellEingang ohneAktiv = EingangEin(Gebaeude(g =>
            {
                g.Kuehlkurve_Fusspunkt = 14.0; g.Kuehlkurve_Auslegung_Weg = "unbekannt"; g.Kuehlkurve_Auslegung_Aussen = 20.0;
            }));
            Assert.False(ohneAktiv.KuehlkurveWirksam);
            Bitgleich(bezug, Vdi6007Rechenweg.Laufen(ohneAktiv, 0, 1));

            // Der Schalter stellt sich nach dem using-Block zurück.
            using (KuehlkurveKernschalter.Schalten(true)) Assert.True(KuehlkurveKernschalter.Ein);
            Assert.False(KuehlkurveKernschalter.Ein);
        }

        // =====================================================================
        //  Der Auslegungsweg (E107) und die Fußpunktregel
        // =====================================================================

        /// <summary>
        /// <b>Drei Wege, Vorgabe Weg 2</b> (Festlegung 3, E107): leer und „tagesmittel“ rechnen das wärmste Tagesmittel,
        /// mindestens Kühlsollwert + Mindestspanne; „stunde“ die höchste Stundentemperatur; „eingabe“ die Eingabe am Gebäude.
        /// </summary>
        [Fact]
        public void Drei_Auslegungswege_Vorgabe_Weg_2()
        {
            GebaeudeModellEingang leer = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true));
            GebaeudeModellEingang.WaermsterTag(leer.ThetaOut, out double mittel);
            double hoechste = leer.ThetaOut.Max();
            double weg2 = Math.Max(mittel, 24.0 + GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_SPANNE_K);
            _aus.WriteLine($"Probeklima: wärmstes Tagesmittel {mittel:0.00} °C, höchste Stunde {hoechste:0.00} °C, Weg 2 {weg2:0.00} °C");
            Assert.True(mittel < 24.0, "Probeklima: Tagesmittel unter dem Sollwert (Befund KK1)");
            Assert.Equal(weg2, leer.Kuehlkurve.AuslegungAussenC);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL, leer.Kuehlkurve.AuslegungWeg);
            Assert.False(leer.Kuehlkurve.AuslegungRueckfall);

            GebaeudeModellEingang tag = EingangEin(Gebaeude(g =>
            { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL; }));
            Assert.Equal(weg2, tag.Kuehlkurve.AuslegungAussenC);
            Assert.True(leer.KuehlVorlaufC.SequenceEqual(tag.KuehlVorlaufC), "leer = tagesmittel");

            GebaeudeModellEingang stunde = EingangEin(Gebaeude(g =>
            { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE; g.Kuehlkurve_Auslegung_Aussen = 40.0; }));
            Assert.Equal(hoechste, stunde.Kuehlkurve.AuslegungAussenC);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE, stunde.Kuehlkurve.AuslegungWeg);

            GebaeudeModellEingang eingabe = EingangEin(Gebaeude(g =>
            { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE; g.Kuehlkurve_Auslegung_Aussen = 30.0; }));
            Assert.Equal(30.0, eingabe.Kuehlkurve.AuslegungAussenC);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, eingabe.Kuehlkurve.AuslegungWeg);
            Assert.False(eingabe.Kuehlkurve.AuslegungRueckfall);

            // Die Eingabe wirkt nur auf dem Weg „eingabe“.
            GebaeudeModellEingang ohneWeg = EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Aussen = 30.0; }));
            Assert.Equal(weg2, ohneWeg.Kuehlkurve.AuslegungAussenC);

            // Ohne endlichen Kühlsollwert gilt das Tagesmittel allein.
            Assert.Equal(mittel, Kuehlkurve.AuslegungAussentemperaturC(null, null, double.PositiveInfinity, leer.ThetaOut, out _, out _));

            // Ein unbekannter Weg bricht ab.
            Assert.Equal(GebaeudeModellFehler.UebergabeUngueltig, Assert.Throws<GebaeudeModellException>(
                () => EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = "Tagesmittel"; }))).Grund);
        }

        /// <summary>
        /// <b>Die Spanne greift</b>: Am Probeklima (wärmstes Tagesmittel unter 24 °C) springt die Kurve nach Weg 2 am Sollwert
        /// nicht mehr vom Fußpunkt auf den Auslegungsvorlauf — kurz über dem Sollwert liegt der Vorlauf am Fußpunkt, die Reihe
        /// gleitet, die Kurve fällt stetig.
        /// </summary>
        [Fact]
        public void Spanne_greift_kein_Sprung_am_Sollwert()
        {
            GebaeudeModellEingang e = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true));
            Kuehlkurve k = e.Kuehlkurve;
            Assert.True(k.AuslegungAussenC >= 24.0 + GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_SPANNE_K);
            double steigung = (k.FusspunktC - k.AuslegungVorlaufC) / (k.AuslegungAussenC - 24.0);
            double vor = k.KurveC(24.0, 24.0);
            for (double t = 24.0; t <= k.AuslegungAussenC + 2.0; t += 0.1)
            {
                double v = k.KurveC(24.0, t);
                Assert.True(vor - v <= steigung * 0.1 + 1e-9, "kein Sprung bei " + t);
                vor = v;
            }
            Assert.True(k.FusspunktC - k.KurveC(24.0, 24.1) < 0.05, "kurz über dem Sollwert am Fußpunkt");

            // Weg 1 und Weg 2 liegen am Probeklima über dem Sollwert; die Reihe gleitet bei 24 °C.
            double[] v24 = e.KuehlVorlaufC;
            Assert.True(v24.Distinct().Count() > 2, "die Reihe gleitet bei 24 °C");
        }

        /// <summary>
        /// <b>Weg 3 unter dem Sollwert</b>: Eine Eingabe nicht über Kühlsollwert + 1 K oder ohne Wert fällt auf Weg 2 zurück
        /// und meldet sich je Lauf einmal; knapp darüber gilt sie.
        /// </summary>
        [Fact]
        public void Weg_3_unter_dem_Sollwert_faellt_mit_Hinweis_auf_Weg_2()
        {
            GebaeudeModellEingang weg2 = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true));
            foreach (double? eingabe in new double?[] { 24.5, 25.0, 18.0, null })
            {
                GebaeudeModellEingang e = EingangEin(Gebaeude(g =>
                { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE; g.Kuehlkurve_Auslegung_Aussen = eingabe; }));
                Assert.True(e.Kuehlkurve.AuslegungRueckfall, "Rückfall bei " + eingabe);
                Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL, e.Kuehlkurve.AuslegungWeg);
                Assert.Equal(weg2.Kuehlkurve.AuslegungAussenC, e.Kuehlkurve.AuslegungAussenC);
                Assert.True(weg2.KuehlVorlaufC.SequenceEqual(e.KuehlVorlaufC), "Reihe wie Weg 2 bei " + eingabe);

                SimulationProtokoll.NeuStarten();
                Vdi6007Rechenweg.KuehlkurveMelden(e, SimulationProtokoll.Aktuell, "7", "Probegebäude");
                Vdi6007Rechenweg.KuehlkurveMelden(e, SimulationProtokoll.Aktuell, "7", "Probegebäude");
                string hinweis = Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
                Assert.Contains("Probegebäude", hinweis);
                Assert.Contains(eingabe.HasValue ? eingabe.Value.ToString("0.#") : "—", hinweis);
            }

            GebaeudeModellEingang knapp = EingangEin(Gebaeude(g =>
            { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE; g.Kuehlkurve_Auslegung_Aussen = 25.5; }));
            Assert.False(knapp.Kuehlkurve.AuslegungRueckfall);
            Assert.Equal(25.5, knapp.Kuehlkurve.AuslegungAussenC);
            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.KuehlkurveMelden(knapp, SimulationProtokoll.Aktuell, "7", "Probegebäude");
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);
        }

        /// <summary>
        /// <b>Fußpunktregel</b> (Festlegung 18): Ein Fußpunkt kälter als der Auslegungsvorlauf wird auf ihn geklemmt — die Kurve
        /// ist waagrecht und fällt nie mit der Außentemperatur an —, je Lauf einmal gemeldet; gleich oder wärmer bleibt er.
        /// </summary>
        [Fact]
        public void Fusspunktregel_klemmt_auf_den_Auslegungsvorlauf()
        {
            var k = new Kuehlkurve(14.0, 16.0, 32.0, double.NaN);
            Assert.True(k.FusspunktGeklemmt);
            Assert.Equal(16.0, k.FusspunktC);
            Assert.Equal(14.0, k.FusspunktEingabeC);
            for (double t = 10.0; t <= 40.0; t += 0.5) Assert.Equal(16.0, k.KurveC(24.0, t));
            Assert.False(new Kuehlkurve(16.0, 16.0, 32.0, double.NaN).FusspunktGeklemmt);
            Assert.False(new Kuehlkurve(19.0, 16.0, 32.0, double.NaN).FusspunktGeklemmt);

            GebaeudeModellEingang e = EingangEin(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 14.0; }));
            Assert.True(e.Kuehlkurve.FusspunktGeklemmt);
            for (int h = 0; h < 8760; h++)
                Assert.True(e.KuehlVorlaufC[h] >= 16.0, "nie unter dem Auslegungsvorlauf, Stunde " + h);
            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.KuehlkurveMelden(e, SimulationProtokoll.Aktuell, "7", "Probegebäude");
            Vdi6007Rechenweg.KuehlkurveMelden(e, SimulationProtokoll.Aktuell, "7", "Probegebäude");
            string hinweis = Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
            Assert.Contains("14", hinweis);
            Assert.Contains("16", hinweis);

            // Ohne wirksame Kurve schweigt die Meldung.
            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.KuehlkurveMelden(Eingang(Gebaeude(g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 14.0; })),
                                              SimulationProtokoll.Aktuell, "7", "Probegebäude");
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);
        }
    }
}
