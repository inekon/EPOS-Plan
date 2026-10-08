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
        /// Auslegungs-Außentemperatur ist das wärmste Tagesmittel (Festlegung 3); Bereiche (Festlegung 7) hart geprüft.
        /// </summary>
        [Fact]
        public void Fusspunkt_Auslegungsaussentemperatur_und_Bereiche_im_Eingang()
        {
            GebaeudeModellEingang leer = EingangEin(Gebaeude(g => g.Kuehlkurve_Aktiv = true));
            Assert.True(leer.KuehlkurveWirksam);
            Assert.Equal(GebaeudeFestwerte.KUEHL_AUSLEGUNG_RUECKLAUF_FLAECHE, leer.Kuehlkurve.FusspunktC);
            Assert.Equal(GebaeudeFestwerte.KUEHL_AUSLEGUNG_VORLAUF_FLAECHE, leer.Kuehlkurve.AuslegungVorlaufC);
            GebaeudeModellEingang.WaermsterTag(leer.ThetaOut, out double mittel);
            Assert.Equal(mittel, leer.Kuehlkurve.AuslegungAussenC);
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
        /// die Probeklima-Reihe (wärmstes Tagesmittel 22 °C) einen linearen Ast hat; bei 24 °C läge die
        /// Auslegungs-Außentemperatur unter dem Sollwert und die Kurve spränge (siehe <see cref="Form_der_Zwei_Punkt_Kurve"/>).
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
            Action<ProjektGebaeudeModel> kurve = g => { g.Kuehlkurve_Aktiv = true; g.Kuehlkurve_Fusspunkt = 21.0; g.Kuehlkurve_Raumeinfluss = 2.0; };
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

            GebaeudeModellEingang ohneAktiv = EingangEin(Gebaeude(g => { g.Kuehlkurve_Fusspunkt = 21.0; }));
            Assert.False(ohneAktiv.KuehlkurveWirksam);
            Bitgleich(bezug, Vdi6007Rechenweg.Laufen(ohneAktiv, 0, 1));

            // Der Schalter stellt sich nach dem using-Block zurück.
            using (KuehlkurveKernschalter.Schalten(true)) Assert.True(KuehlkurveKernschalter.Ein);
            Assert.False(KuehlkurveKernschalter.Ein);
        }
    }
}
