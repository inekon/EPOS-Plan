using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt K im Löser — die Kälteseite der Anlagenkopplung</b> (Entscheid E37; Konzept
    /// Anlagenkopplung 7, 10.5, 11.1) — die Proben am 2-K-Löser mit dem Phantasiegebäude, ohne
    /// Datenbank: die Spiegelprobe mit Toleranz 0, Grenzfall B der Kälte, die Stetigkeit der
    /// Verteilung, der gekoppelte geregelte Kühlfall bis zur Grenze der Übergabe, die Handrechnung
    /// der Kühldecke, die Begrenzungen, der P-Regler und die Umschaltstunde. Dazu die Vorgaben je
    /// Art und die Wirksamkeit. Alle Zahlen sind Phantasiewerte.
    /// </summary>
    public class AnlagenkopplungKaelteseiteTests
    {
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungKaelteseiteTests(ITestOutputHelper aus) { _aus = aus; }

        /// <summary>Kühldecke der Handrechnung (11.1): 2 kW, 16/19/26 °C, n = 1,1.</summary>
        private static Uebergabekennwerte Kuehldecke(double phiNW = 2000.0, double n = 1.1)
            => Kuehluebergabe.Gespiegelt(phiNW, n, 16.0, 19.0, 26.0);

        /// <summary>Eine Stunde mit Kühlübergabe, ohne Heizung, θ_eq = θ_out.</summary>
        private static Stundenrand KuehlRand(double thetaOut, double kuehlSoll, double phiConv, Uebergabekennwerte gespiegelt,
                                             double vorlauf, double xp, double kuehlMaxW = double.NaN, double strahlung = 0.5,
                                             bool gekappt = false, double heizSoll = double.NaN, Uebergabekennwerte heiz = null,
                                             double heizVorlauf = double.NaN, double phiRadAW = 0.0, double phiRadIW = 0.0)
            => new Stundenrand(thetaOut, thetaOut, heizSoll, kuehlSoll, phiRadAW, phiRadIW, phiConv,
                               double.NaN, kuehlMaxW, 0.3, 0.0, 0.0,
                               uebergabe: heiz, vorlaufC: heizVorlauf, reglerbandK: xp,
                               kuehlUebergabeGespiegelt: gespiegelt, kuehlVorlaufC: vorlauf,
                               kuehlStrahlungsanteil: strahlung, kuehlVorlaufGekappt: gekappt);

        /// <summary>Eine deterministisch gezogene Folge von Stunden (Muster der Heizseite), Kühlung nach Zufall.</summary>
        private static List<Stundenrand> Folge(int stunden, int saat)
        {
            var z = new Random(saat);
            var folge = new List<Stundenrand>(stunden);
            for (int h = 0; h < stunden; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 18.0 - 10.0 * Math.Cos(2.0 * Math.PI * h / 2000.0) + 6.0 * tag + 2.0 * (z.NextDouble() - 0.5);
                double soll = (h % 24) >= 6 && (h % 24) <= 21 ? 20.0 : 16.0;
                double sonne = Math.Max(0.0, tag) * 3000.0 * z.NextDouble();
                double kuehlMax = z.NextDouble() < 0.3 ? 800.0 + 2000.0 * z.NextDouble() : double.NaN;
                double zusatz = z.NextDouble() < 0.1 ? 150.0 : 0.0;
                folge.Add(new Stundenrand(aussen, aussen + 1.0, soll, 25.0, 0.4 * sonne, 0.6 * sonne, 300.0 + 900.0 * z.NextDouble(),
                                          double.NaN, kuehlMax, 0.3, 0.0, zusatz));
            }
            return folge;
        }

        private static Betriebsfall Gespiegelt(Betriebsfall f)
        {
            switch (f)
            {
                case Betriebsfall.HeizenGeregelt: return Betriebsfall.KuehlenGeregelt;
                case Betriebsfall.Heizgrenze: return Betriebsfall.Kuehlgrenze;
                case Betriebsfall.UebergabeGesaettigt: return Betriebsfall.KuehluebergabeGesaettigt;
                case Betriebsfall.UebergabeRegelbereich: return Betriebsfall.KuehluebergabeRegelbereich;
                case Betriebsfall.KuehlenGeregelt: return Betriebsfall.HeizenGeregelt;
                case Betriebsfall.Kuehlgrenze: return Betriebsfall.Heizgrenze;
                case Betriebsfall.KuehluebergabeGesaettigt: return Betriebsfall.UebergabeGesaettigt;
                case Betriebsfall.KuehluebergabeRegelbereich: return Betriebsfall.UebergabeRegelbereich;
                default: return f;
            }
        }

        private static Begrenzungsgrund Gespiegelt(Begrenzungsgrund g)
        {
            switch (g)
            {
                case Begrenzungsgrund.Heizgrenze: return Begrenzungsgrund.KeineKaelte;
                case Begrenzungsgrund.Uebergabe: return Begrenzungsgrund.KuehlUebergabe;
                case Begrenzungsgrund.HeizleistungMax: return Begrenzungsgrund.KuehlleistungMax;
                default: return g;
            }
        }

        private static void Gleich(double a, double b, string was, int h)
            => Assert.True(a.Equals(b), was + ", Stunde " + h + ": " + a.ToString("R") + " / " + b.ToString("R"));

        // =====================================================================
        //  Probe 2 — die Spiegelprobe, Toleranz 0 (10.5, 11.1)
        // =====================================================================

        /// <summary>
        /// <b>Spiegelprobe</b>: Heizgekoppelte Stunden mit dem Rand X gegen kühlgekoppelte mit −X —
        /// Temperaturen, Lasten und Anfangszustand negiert, die Gegenseite jeweils aus, gleicher
        /// Strahlungsanteil a &gt; 0, Heiz- und Kühlleistungsgrenze vertauscht, fester Vorlauf. Die
        /// gespiegelten Kennwerte der Kälteseite sind dann dasselbe Objekt wie die der Wärmeseite.
        /// Erwartet: Leistungen vertauscht, Temperaturen genau negiert, Fallfolge gespiegelt, Gründe
        /// je Seite, Zeitanteile gleich — bitgleich, weil IEEE-Arithmetik vorzeichensymmetrisch ist
        /// und Schritt K ohne Multiplikation spiegelt.
        /// </summary>
        [Theory]
        [InlineData(1.0, 0.3, 11)]
        [InlineData(0.0, 0.5, 12)]
        [InlineData(2.0, 0.1, 13)]
        public void Spiegelprobe_Kaelteseite_ist_bitgleich_zur_gespiegelten_Waermeseite(double xp, double a, int saat)
        {
            var k = new Uebergabekennwerte(2500.0, 1.3, 50.0, 40.0, 20.0);
            var z = new Random(saat);
            var heiz = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var kuehl = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            heiz.Zuruecksetzen(17.0, 18.0);
            kuehl.Zuruecksetzen(-17.0, -18.0);
            int stundenGekoppelt = 0, gesaettigt = 0, regel = 0, grenze = 0;
            for (int h = 0; h < 2500; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 2.0 - 8.0 * Math.Cos(2.0 * Math.PI * h / 1500.0) + 4.0 * tag + 2.0 * (z.NextDouble() - 0.5);
                double soll = (h % 24) >= 6 && (h % 24) <= 21 ? 20.0 : 16.0;
                double sonne = Math.Max(0.0, tag) * 1500.0 * z.NextDouble();
                double grenzeW = z.NextDouble() < 0.3 ? 800.0 + 2000.0 * z.NextDouble() : double.NaN;
                double zusatz = z.NextDouble() < 0.1 ? 150.0 : 0.0;
                double eq = aussen + 1.0, radAW = 0.4 * sonne, radIW = 0.6 * sonne, conv = 200.0 + 300.0 * z.NextDouble();
                double vorlauf = 45.0;

                var rh = new Stundenrand(aussen, eq, soll, double.PositiveInfinity, radAW, radIW, conv,
                                         grenzeW, double.NaN, a, 0.0, zusatz,
                                         uebergabe: k, vorlaufC: vorlauf, reglerbandK: xp);
                var rk = new Stundenrand(-aussen, -eq, double.NaN, -soll, -radAW, -radIW, -conv,
                                         double.NaN, grenzeW, 0.3, 0.0, zusatz,
                                         reglerbandK: xp, kuehlUebergabeGespiegelt: k, kuehlVorlaufC: -vorlauf,
                                         kuehlStrahlungsanteil: a);

                Stundenergebnis eh = heiz.Schritt(in rh);
                Betriebsfall[] fh = heiz.LetzteFallfolge;
                Stundenergebnis ek = kuehl.Schritt(in rk);
                Betriebsfall[] fk = kuehl.LetzteFallfolge;

                Gleich(eh.HeizleistungW, ek.KuehlleistungW, "Leistung Heizen ↔ Kühlen", h);
                Gleich(eh.KuehlleistungW, ek.HeizleistungW, "Gegenseite", h);
                Gleich(eh.ThetaAirMittel, -ek.ThetaAirMittel, "Raumluft", h);
                Gleich(eh.ThetaOpMittel, -ek.ThetaOpMittel, "operativ", h);
                Gleich(eh.ThetaSAwMittel, -ek.ThetaSAwMittel, "Oberfläche AW", h);
                Gleich(eh.ThetaSIwMittel, -ek.ThetaSIwMittel, "Oberfläche IW", h);
                Gleich(eh.ThetaMAwEnde, -ek.ThetaMAwEnde, "Masse AW", h);
                Gleich(eh.ThetaMIwEnde, -ek.ThetaMIwEnde, "Masse IW", h);
                Assert.Equal(eh.Abschnitte, ek.Abschnitte);
                Assert.Equal(fh.Select(Gespiegelt).ToArray(), fk);

                Gleich(eh.VorlaufC, -ek.KuehlVorlaufC, "Vorlauf", h);
                Gleich(eh.RuecklaufC, -ek.KuehlRuecklaufC, "Rücklauf", h);
                Assert.Equal(Gespiegelt(eh.Begrenzungsgrund), ek.KuehlBegrenzungsgrund);
                Gleich(eh.UebergabeBegrenztAnteil, ek.KuehlUebergabeBegrenztAnteil, "Anteil Übergabe", h);
                Gleich(eh.HeizleistungMaxAnteil, ek.KuehlleistungMaxAnteil, "Anteil Leistungsgrenze", h);
                Gleich(eh.HeizgrenzeAnteil, ek.KeineKaelteAnteil, "Anteil ohne Leistung", h);
                Assert.Equal(0.0, ek.VorlaufgrenzeAnteil);
                Assert.True(double.IsNaN(ek.VorlaufC) && double.IsNaN(eh.KuehlVorlaufC), "je Seite nur ihr Vorlauf");

                if (eh.HeizleistungW > 0.0) stundenGekoppelt++;
                if (fh.Contains(Betriebsfall.UebergabeGesaettigt)) gesaettigt++;
                if (fh.Contains(Betriebsfall.UebergabeRegelbereich)) regel++;
                if (eh.HeizleistungMaxAnteil > 0.0) grenze++;
            }
            _aus.WriteLine($"Xp {xp}, a {a}: Stunden mit Leistung {stundenGekoppelt}, gesättigt {gesaettigt}, Regelbereich {regel}, Leistungsgrenze {grenze}");
            Assert.True(stundenGekoppelt > 500 && gesaettigt > 0 && grenze > 0, "die Folge muss alle Fälle treffen");
            if (xp > 0.0) Assert.True(regel > 0, "mit Band auch der Regelbereich");
        }

        // =====================================================================
        //  Probe 4 — Grenzfall B der Kälte (3.7)
        // =====================================================================

        /// <summary>
        /// <b>Grenzfall B Kälte</b>: unbegrenzte Kühlübergabe, <c>Xp = 0</c>, Strahlungsanteil 0 —
        /// Stunde für Stunde BITGLEICH zu <c>KuehlenGeregelt</c> bzw. <c>Kuehlgrenze</c> des
        /// Bestands, auch mit <c>Kuehlleistung_Max</c>.
        /// </summary>
        [Fact]
        public void Grenzfall_B_Unbegrenzte_Kuehluebergabe_mit_Band_null_ist_bitgleich_zur_idealen_Kuehlung()
        {
            Uebergabekennwerte unbegrenzt = Kuehluebergabe.Gespiegelt(double.PositiveInfinity, 1.1, 16.0, 19.0, 26.0);
            List<Stundenrand> ideal = Folge(3000, 42);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(22.0);
            b.Zuruecksetzen(22.0);
            int gekuehlt = 0, kuehlgrenze = 0;
            for (int h = 0; h < ideal.Count; h++)
            {
                Stundenrand ri = ideal[h];
                var rg = new Stundenrand(ri.ThetaOut, ri.ThetaEq, ri.ThetaSoll, ri.ThetaMax, ri.PhiRadAW, ri.PhiRadIW, ri.PhiConv,
                                         ri.HeizleistungMaxW, ri.KuehlleistungMaxW, ri.HeizungStrahlungsanteil,
                                         ri.KuehlungAnteilInnenflaeche, ri.ZusatzleitwertWK,
                                         kuehlUebergabeGespiegelt: unbegrenzt, kuehlVorlaufC: 16.0, kuehlStrahlungsanteil: 0.0);
                Stundenergebnis ei = a.Schritt(in ri);
                Stundenergebnis eg = b.Schritt(in rg);
                Gleich(ei.HeizleistungW, eg.HeizleistungW, "Heizleistung", h);
                Gleich(ei.KuehlleistungW, eg.KuehlleistungW, "Kühlleistung", h);
                Gleich(ei.ThetaAirMittel, eg.ThetaAirMittel, "Raumluft", h);
                Gleich(ei.ThetaOpMittel, eg.ThetaOpMittel, "operativ", h);
                Gleich(ei.ThetaMAwEnde, eg.ThetaMAwEnde, "Masse AW", h);
                Gleich(ei.ThetaMIwEnde, eg.ThetaMIwEnde, "Masse IW", h);
                Assert.Equal(ei.Abschnitte, eg.Abschnitte);
                Assert.Equal(16.0, eg.KuehlVorlaufC);
                Assert.Equal(16.0, eg.KuehlRuecklaufC);        // W_K unendlich
                Assert.Equal(0.0, eg.KuehlUebergabeBegrenztAnteil);
                if (eg.KuehlleistungW > 0.0) gekuehlt++;
                if (eg.KuehlleistungMaxAnteil > 0.0) kuehlgrenze++;
            }
            _aus.WriteLine("Stunden mit Kühlung " + gekuehlt + ", davon an Kuehlleistung_Max " + kuehlgrenze);
            Assert.True(gekuehlt > 200 && kuehlgrenze > 0, "die Folge muss Kühlstunden und die Kühlleistungsgrenze treffen");
        }

        // =====================================================================
        //  Probe 5 — Stetigkeit bei a > 0 (Verteilung wie die Übergabe)
        // =====================================================================

        /// <summary>
        /// <b>Stetigkeit</b>: Mit Strahlungsanteil a &gt; 0 und <c>Xp = 0</c> springt die Kühlleistung
        /// beim Übergang vom Grenzfall (geregelt) zum gesättigten Fall nicht — weil der geregelte
        /// Kühlfall die Kälte wie die Übergabe verteilt (10.5). Gesucht wird die innere Last, an der
        /// der erste Abschnitt kippt; beiderseits davon liegen die Stundenleistungen im Zahlenrand
        /// beieinander.
        /// </summary>
        [Fact]
        public void Der_Uebergang_Grenzfall_gesaettigt_ist_stetig()
        {
            Uebergabekennwerte k = Kuehldecke(1500.0);
            Func<double, (Betriebsfall erster, double leistung)> stunde = conv =>
            {
                var m = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
                m.Zuruecksetzen(25.0, 25.0);
                Stundenrand r = KuehlRand(26.0, 24.0, conv, k, 16.0, 0.0, strahlung: 0.5);
                Stundenergebnis e = m.Schritt(in r);
                return (m.LetzteFallfolge[0], e.KuehlleistungW);
            };

            double lo = 0.0, hi = 4000.0;
            Assert.Equal(Betriebsfall.KuehlenGeregelt, stunde(lo).erster);
            Assert.Equal(Betriebsfall.KuehluebergabeGesaettigt, stunde(hi).erster);
            for (int i = 0; i < 60; i++)
            {
                double mitte = 0.5 * (lo + hi);
                if (stunde(mitte).erster == Betriebsfall.KuehlenGeregelt) lo = mitte; else hi = mitte;
            }
            double unten = stunde(lo).leistung, oben = stunde(hi).leistung;
            _aus.WriteLine($"Kipppunkt bei {lo:0.000000} W innerer Last: {unten:0.000000} → {oben:0.000000} W");
            Assert.True(Math.Abs(oben - unten) < 1e-3, "Sprung " + (oben - unten) + " W");
        }

        // =====================================================================
        //  Probe 6 — endliches Φ_N, Xp = 0, drehende Last (VerletztGekoppelt)
        // =====================================================================

        /// <summary>
        /// <b>Drehende Last</b>: Der gekoppelte geregelte Kühlfall (Grenzfall 3.7) endet an der
        /// Kühlübergabe am Kühlsollwert (<c>GrenzeQW</c>), wenn die verlangte Kälte in der Stunde
        /// über sie wächst — danach rechnet der gesättigte Fall. Die Raumluft unterschreitet den
        /// Kühlsollwert nicht, die Kälte liegt nicht über der idealen Kühlung derselben Stunde, und
        /// die Abschnittsregel fällt nicht. Das ist der Nachweis des Zweigs <c>KuehlenGeregelt</c>
        /// in <c>VerletztGekoppelt</c>.
        /// </summary>
        [Fact]
        public void Der_gekoppelte_geregelte_Kuehlfall_endet_an_der_Grenze_der_Uebergabe()
        {
            Uebergabekennwerte k = Kuehldecke(1500.0);
            ErsatzparameterRC p = Phantasiegebaeude.Standard(true, cAW: 2.0e5, cIW: 5.0e5);
            double grenzeW = Kuehluebergabe.LeistungOffenW(k, 16.0, 24.0);

            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(20.0, 20.0);   // kühle Massen: Die verlangte Kälte wächst in der Stunde.
            Stundenrand r = KuehlRand(30.0, 24.0, 600.0, k, 16.0, 0.0);
            Stundenergebnis e = m.Schritt(in r);
            Betriebsfall[] f = m.LetzteFallfolge;

            // Die ideale Kühlung mit DERSELBEN Verteilung: die unbegrenzte Kühlübergabe (Grenzfall B).
            var ideal = new Zonenmodell2K(p);
            ideal.Zuruecksetzen(20.0, 20.0);
            Stundenrand ri = KuehlRand(30.0, 24.0, 600.0, Kuehldecke(double.PositiveInfinity), 16.0, 0.0);
            Stundenergebnis ei = ideal.Schritt(in ri);

            _aus.WriteLine("Grenze " + grenzeW.ToString("0.0") + " W; Folge " + string.Join(" → ", f) +
                           "; Kälte " + e.KuehlleistungW.ToString("0.0") + " W, ideal " + ei.KuehlleistungW.ToString("0.0") + " W");
            Assert.Equal(Betriebsfall.KuehlenGeregelt, f[0]);
            Assert.Contains(Betriebsfall.KuehluebergabeGesaettigt, f.Skip(1));
            Assert.True(e.KuehlleistungW < ei.KuehlleistungW, "die Übergabe begrenzt");
            Assert.True(e.ThetaAirMittel >= 24.0 - 1e-9, "keine Unterschreitung des Kühlsollwerts");
            Assert.True(e.KuehlUebergabeBegrenztAnteil > 0.0 && e.KuehlUebergabeBegrenztAnteil < 1.0);
            Assert.Equal(0.0, e.HeizleistungW);
        }

        // =====================================================================
        //  Probe 7 — Handrechnung Kühldecke (11.1)
        // =====================================================================

        /// <summary>
        /// <b>Handrechnung Kühldecke</b>: Φ_N = 2 000 W, 16/19/26 °C, n = 1,1 — W_K = 666,7 W/K,
        /// Δθ_m,N = 8,5 K. Bei θ_i = 27 °C und V = 16 °C: Φ ≈ 2 218 W, R ≈ 19,33 °C, G_K ≈ 218,5
        /// W/K. Mit der Grenze 18 °C: Φ ≈ 1 784 W.
        /// </summary>
        [Fact]
        public void Handrechnung_Kuehldecke()
        {
            Uebergabekennwerte k = Kuehldecke();
            Assert.Equal(3.0, k.SpreizungNK, 12);
            Assert.Equal(8.5, k.DeltaThetaMNK, 12);
            Assert.Equal(2000.0 / 3.0, k.WHWK, 9);

            double phi16 = Kuehluebergabe.LeistungOffenW(k, 16.0, 27.0);
            double r16 = Kuehluebergabe.RuecklaufC(k, 16.0, phi16);
            double g16 = Waermeuebergabe.SteigungOffenWK(k, phi16, -16.0, -27.0);
            double phi18 = Kuehluebergabe.LeistungOffenW(k, 18.0, 27.0);
            _aus.WriteLine($"V 16: {phi16:0.00} W, R {r16:0.000} °C, G {g16:0.00} W/K; V 18: {phi18:0.00} W");
            Assert.Equal(2218.0, phi16, 0);
            Assert.Equal(19.33, r16, 2);
            Assert.Equal(218.5, g16, 0);
            Assert.Equal(1784.0, phi18, 0);
            Assert.Equal(0.0, Kuehluebergabe.LeistungOffenW(k, 27.0, 27.0));
            Assert.Equal(0.0, Kuehluebergabe.LeistungOffenW(k, 28.0, 27.0));

            // Die Kennlinie fällt mit der Raumluft (G > 0): numerische Ableitung gegen G.
            double d = 1e-4;
            double ableitung = (Kuehluebergabe.LeistungOffenW(k, 16.0, 27.0 + d) - Kuehluebergabe.LeistungOffenW(k, 16.0, 27.0 - d)) / (2.0 * d);
            Assert.True(Math.Abs(ableitung - g16) < 1e-4 * g16, "dΦ/dθ " + ableitung + " gegen G " + g16);

            // n = 1: die geschlossene Form gegen Newton.
            Uebergabekennwerte k1 = Kuehldecke(2000.0, 1.0);
            double geschlossen = 2000.0 * 11.0 / (8.5 + 2000.0 / (2.0 * k1.WHWK));
            Assert.Equal(geschlossen, Kuehluebergabe.LeistungOffenW(k1, 16.0, 27.0), 9);
        }

        /// <summary>
        /// <b>Handrechnung im Löser</b>: eingeschwungen gesättigt rechnet die Stunde genau, was die
        /// Kühldecke bei der Raumluft hergibt; mit gekapptem Vorlauf an der Grenze 18 °C trägt sie
        /// den Grund <c>VorlaufgrenzeKuehlung</c>, ungekappt (Gebläsekonvektor bei 14 °C, keine
        /// Grenze) den Grund <c>KuehlUebergabe</c>.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Gesaettigt_mit_und_ohne_Vorlaufgrenze(bool gekappt)
        {
            Uebergabekennwerte k = gekappt ? Kuehldecke() : Kuehluebergabe.Gespiegelt(2000.0, 1.0, 7.0, 12.0, 26.0);
            double vorlauf = gekappt ? 18.0 : 14.0;
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            m.Zuruecksetzen(28.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 24 * 30; h++)
            {
                Stundenrand r = KuehlRand(32.0, 24.0, 1200.0, k, vorlauf, 1.0, strahlung: gekappt ? 0.5 : 0.0, gekappt: gekappt);
                e = m.Schritt(in r);
            }
            Assert.True(e.ThetaAirMittel > 25.0, "Raumluft " + e.ThetaAirMittel);
            Assert.Equal(1.0, e.KuehlUebergabeBegrenztAnteil, 12);
            Assert.Equal(gekappt ? 1.0 : 0.0, e.VorlaufgrenzeAnteil, 12);
            Assert.Equal(gekappt ? Begrenzungsgrund.VorlaufgrenzeKuehlung : Begrenzungsgrund.KuehlUebergabe, e.KuehlBegrenzungsgrund);
            double soll = Kuehluebergabe.LeistungOffenW(k, vorlauf, e.ThetaAirMittel);
            Assert.True(Math.Abs(e.KuehlleistungW - soll) <= 1e-6 * soll, "eingeschwungen: " + e.KuehlleistungW + " gegen " + soll);
            Assert.Equal(vorlauf + e.KuehlleistungW / k.WHWK, e.KuehlRuecklaufC, 9);
            Assert.Equal(Begrenzungsgrund.KeineBegrenzung, e.Begrenzungsgrund);
            Assert.True(double.IsNaN(e.VorlaufC));
        }

        // =====================================================================
        //  Probe 8 — Begrenzungen
        // =====================================================================

        /// <summary><c>Kuehlleistung_Max</c> greift unter der Kühlübergabe und trägt seinen eigenen Grund.</summary>
        [Fact]
        public void Kuehlleistung_Max_kappt_unter_der_Kuehluebergabe()
        {
            Uebergabekennwerte k = Kuehldecke(6000.0);
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            m.Zuruecksetzen(26.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 24 * 20; h++)
            {
                Stundenrand r = KuehlRand(34.0, 24.0, 1500.0, k, 16.0, 1.0, kuehlMaxW: 1200.0);
                e = m.Schritt(in r);
            }
            Assert.Equal(1200.0, e.KuehlleistungW, 6);
            Assert.Equal(Begrenzungsgrund.KuehlleistungMax, e.KuehlBegrenzungsgrund);
            Assert.Equal(1.0, e.KuehlleistungMaxAnteil, 12);
            Assert.Equal(0.0, e.KuehlUebergabeBegrenztAnteil);
        }

        /// <summary>Liegt der Kaltwasser-Vorlauf nicht unter der Raumluft, kühlt nichts: freier Lauf, Grund <c>KeineKaelte</c>.</summary>
        [Fact]
        public void Ein_Vorlauf_ueber_der_Raumluft_kuehlt_nicht()
        {
            Uebergabekennwerte k = Kuehldecke();
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(27.0);
            Stundenrand r = KuehlRand(28.0, 24.0, 300.0, k, 40.0, 1.0);
            Stundenergebnis e = m.Schritt(in r);
            Assert.Equal(0.0, e.KuehlleistungW);
            Assert.Equal(Begrenzungsgrund.KeineKaelte, e.KuehlBegrenzungsgrund);
            Assert.Equal(1.0, e.KeineKaelteAnteil, 12);
            Assert.Equal(40.0, e.KuehlVorlaufC);
            Assert.Equal(40.0, e.KuehlRuecklaufC);
            Assert.Equal(new[] { Betriebsfall.Totband }, m.LetzteFallfolge);
        }

        /// <summary>
        /// Gekappter Vorlauf (Anlage 7 °C, Grenze 16 °C): Der Grund <c>VorlaufgrenzeKuehlung</c>
        /// steht nur in gesättigten Abschnitten — im Regelbereich hat die Grenze nichts begrenzt.
        /// </summary>
        [Fact]
        public void Die_Vorlaufgrenze_ist_nur_in_gesaettigten_Stunden_der_Grund()
        {
            Uebergabekennwerte k = Kuehldecke(4000.0);
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            m.Zuruecksetzen(25.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 24 * 20; h++)
            {
                Stundenrand r = KuehlRand(27.0, 24.0, 300.0, k, 16.0, 1.0, gekappt: true);
                e = m.Schritt(in r);
            }
            _aus.WriteLine("Regelbereich: Raumluft " + e.ThetaAirMittel.ToString("0.000") + " °C, " + e.KuehlleistungW.ToString("0.0") + " W");
            Assert.True(e.ThetaAirMittel > 24.0 && e.ThetaAirMittel < 25.0, "bleibende Abweichung im Band");
            Assert.Equal(Begrenzungsgrund.KeineBegrenzung, e.KuehlBegrenzungsgrund);
            Assert.Equal(0.0, e.VorlaufgrenzeAnteil);
            Assert.Equal(0.0, e.KuehlUebergabeBegrenztAnteil);
            Assert.All(m.LetzteFallfolge, f => Assert.Equal(Betriebsfall.KuehluebergabeRegelbereich, f));
        }

        // =====================================================================
        //  Probe 9 — der P-Regler: zwei Knicke, bleibende Abweichung
        // =====================================================================

        /// <summary>
        /// Eine Stunde, in der die Raumluft von über θ_kühl + Xp bis unter θ_kühl fällt (leichtes
        /// Gebäude, heiß am Stundenbeginn, kühle Außenluft): Mit Xp = 1 K findet die Bisektion
        /// BEIDE Knicke — gesättigt → Regelbereich → ohne Kühlung, zwei Fallwechsel. Mit Xp = 0
        /// endet der gesättigte Fall genau einmal, am Kühlsollwert, im geregelten Kühlfall.
        /// </summary>
        [Fact]
        public void Zwei_Knicke_der_Kaelteseite_mit_Band_null_einer()
        {
            Uebergabekennwerte k = Kuehldecke(3000.0);
            ErsatzparameterRC leicht = Phantasiegebaeude.Standard(cAW: 2.0e4, cIW: 5.0e4);

            var mitBand = new Zonenmodell2K(leicht);
            mitBand.Zuruecksetzen(35.0);
            Stundenrand r1 = KuehlRand(20.0, 24.0, 0.0, k, 16.0, 1.0);
            Stundenergebnis e1 = mitBand.Schritt(in r1);
            Betriebsfall[] f1 = mitBand.LetzteFallfolge;

            var ohneBand = new Zonenmodell2K(leicht);
            ohneBand.Zuruecksetzen(35.0);
            Stundenrand r0 = KuehlRand(20.0, 24.0, 0.0, k, 16.0, 0.0);
            Stundenergebnis e0 = ohneBand.Schritt(in r0);
            Betriebsfall[] f0 = ohneBand.LetzteFallfolge;

            _aus.WriteLine("Xp = 1: " + string.Join(" → ", f1) + "; Xp = 0: " + string.Join(" → ", f0));
            Assert.Equal(e1.Abschnitte, f1.Length);
            Assert.Equal(Betriebsfall.KuehluebergabeGesaettigt, f1[0]);
            Assert.Equal(Betriebsfall.Totband, f1[f1.Length - 1]);
            Assert.True(f1.Skip(1).Take(f1.Length - 2).All(f => f == Betriebsfall.KuehluebergabeRegelbereich) && f1.Length >= 3,
                        "zwischen den Knicken nur der Regelbereich");
            Assert.True(e1.KuehlUebergabeBegrenztAnteil > 0.0 && e1.KuehlUebergabeBegrenztAnteil < 1.0);

            Assert.Equal(Betriebsfall.KuehluebergabeGesaettigt, f0[0]);
            Assert.Equal(Betriebsfall.KuehlenGeregelt, f0[1]);
            Assert.DoesNotContain(Betriebsfall.KuehluebergabeRegelbereich, f0);
        }

        /// <summary>
        /// Der Leitwert des Regelbereichs der Kälteseite ist der der gefahrenen Kennlinie,
        /// Φ_offen/Xp + y·G: numerische Ableitung der Kennlinie y(θ)·Φ_offen(θ) gegen ihn —
        /// gespiegelt, also über den Arbeitspunkt der Wärmeseite mit den gespiegelten Größen.
        /// </summary>
        [Fact]
        public void Leitwert_je_Saettigungszustand_der_Kaelteseite()
        {
            Uebergabekennwerte k = Kuehldecke(3000.0);
            double xp = 1.0, kuehlSoll = 24.0;
            // Regelbereich: θ zwischen θ_kühl und θ_kühl + Xp; gespiegelt: Sollwert −24, θ′ = −θ.
            double theta = 24.4;
            Func<double, double> kennlinie = t => Waermeuebergabe.LeistungKennlinieW(k, -16.0, -kuehlSoll, xp, -t);
            double d = 1e-5;
            double numerisch = (kennlinie(theta + d) - kennlinie(theta - d)) / (2.0 * d);
            double offen = Kuehluebergabe.LeistungOffenW(k, 16.0, theta);
            double y = Waermeuebergabe.Stellgrad(-kuehlSoll, xp, -theta);
            double g = offen / xp + y * Waermeuebergabe.SteigungOffenWK(k, offen, -16.0, -theta);
            Assert.True(y > 0.0 && y < 1.0);
            Assert.True(Math.Abs(numerisch - g) < 1e-4 * g, "Regelbereich: " + numerisch + " gegen " + g);

            // Gesättigt (θ über θ_kühl + Xp): der Sekantenleitwert der offenen Kühlübergabe.
            double theta2 = 26.5;
            double num2 = (kennlinie(theta2 + d) - kennlinie(theta2 - d)) / (2.0 * d);
            double offen2 = Kuehluebergabe.LeistungOffenW(k, 16.0, theta2);
            double g2 = Waermeuebergabe.SteigungOffenWK(k, offen2, -16.0, -theta2);
            Assert.True(Math.Abs(num2 - g2) < 1e-4 * g2, "gesättigt: " + num2 + " gegen " + g2);
            Assert.True(g > g2, "im Regelbereich ist die Kennlinie steiler");
        }

        // =====================================================================
        //  Probe 10 — die Umschaltstunde
        // =====================================================================

        /// <summary>
        /// <b>Umschaltstunde</b>: Heizen und Kühlen gekoppelt in EINER Stunde — kalt am
        /// Stundenbeginn, große innere Last. Beide Seiten liefern, jede trägt ihren eigenen Grund,
        /// und die Stunde bleibt unter dem Abschnittsdeckel.
        /// </summary>
        [Fact]
        public void Umschaltstunde_mit_beiden_Seiten_gekoppelt()
        {
            var heiz = new Uebergabekennwerte(3000.0, 1.3, 55.0, 45.0, 20.0);
            Uebergabekennwerte kuehl = Kuehldecke(3000.0);
            ErsatzparameterRC leicht = Phantasiegebaeude.Standard(cAW: 2.0e4, cIW: 5.0e4);
            var m = new Zonenmodell2K(leicht);
            m.Zuruecksetzen(5.0);
            Stundenrand r = KuehlRand(10.0, 24.0, 3000.0, kuehl, 16.0, 1.0, heizSoll: 20.0, heiz: heiz, heizVorlauf: 50.0);
            Stundenergebnis e = m.Schritt(in r);
            Betriebsfall[] f = m.LetzteFallfolge;
            _aus.WriteLine("Folge " + string.Join(" → ", f) + "; Heizen " + e.HeizleistungW.ToString("0.0") +
                           " W, Kühlen " + e.KuehlleistungW.ToString("0.0") + " W; Gründe " + e.Begrenzungsgrund + " / " + e.KuehlBegrenzungsgrund);
            Assert.True(e.HeizleistungW > 0.0 && e.KuehlleistungW > 0.0, "beide Seiten liefern");
            Assert.True(e.Abschnitte <= Zonenmodell2K.ABSCHNITTSDECKEL);
            Assert.Contains(f, x => x == Betriebsfall.UebergabeGesaettigt || x == Betriebsfall.UebergabeRegelbereich);
            Assert.Contains(f, x => x == Betriebsfall.KuehluebergabeGesaettigt || x == Betriebsfall.KuehluebergabeRegelbereich);
            Assert.True(e.Begrenzungsgrund <= Begrenzungsgrund.HeizleistungMax, "Heizgrund " + e.Begrenzungsgrund);
            Assert.True(e.KuehlBegrenzungsgrund == Begrenzungsgrund.KeineBegrenzung || e.KuehlBegrenzungsgrund >= Begrenzungsgrund.KeineKaelte,
                        "Kühlgrund " + e.KuehlBegrenzungsgrund);
            Assert.Equal(50.0, e.VorlaufC);
            Assert.Equal(16.0, e.KuehlVorlaufC);
        }

        // =====================================================================
        //  Rand, Vorgaben, Wirksamkeit
        // =====================================================================

        /// <summary>Eine Kühlübergabe ohne Kühlung (θ_max = +∞) ist ein ungültiger Rand.</summary>
        [Fact]
        public void Kuehluebergabe_ohne_Kuehlung_ist_ungueltig()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            Stundenrand r = KuehlRand(28.0, double.PositiveInfinity, 300.0, Kuehldecke(), 16.0, 1.0);
            var ex = Assert.Throws<GebaeudeModellException>(() => m.Schritt(in r));
            Assert.Equal(GebaeudeModellFehler.RandUngueltig, ex.Grund);
            Stundenrand r2 = KuehlRand(28.0, 24.0, 300.0, Kuehluebergabe.Gespiegelt(2000.0, 1.1, 19.0, 16.0, 26.0), 16.0, 1.0);
            Assert.Equal(GebaeudeModellFehler.RandUngueltig, Assert.Throws<GebaeudeModellException>(() => m.Schritt(in r2)).Grund);
        }

        /// <summary>Die EPOS-Vorgaben je Kühlübergabeart (A4).</summary>
        [Fact]
        public void Vorgaben_je_Kuehluebergabeart()
        {
            Assert.True(Kuehluebergabe.ArtBekannt(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.True(Kuehluebergabe.ArtBekannt(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.True(Kuehluebergabe.ArtBekannt(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.False(Kuehluebergabe.ArtBekannt(DbWerte.KUEHLUEBERGABE_IDEAL));
            Assert.False(Kuehluebergabe.ArtBekannt(DbWerte.UEBERGABE_FLAECHE));
            Assert.NotEqual(DbWerte.UEBERGABE_FLAECHE, DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG);

            Assert.Equal(1.1, Kuehluebergabe.VorgabeExponent(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(1.1, Kuehluebergabe.VorgabeExponent(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.Equal(1.0, Kuehluebergabe.VorgabeExponent(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(16.0, Kuehluebergabe.VorgabeVorlaufC(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(19.0, Kuehluebergabe.VorgabeRuecklaufC(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.Equal(7.0, Kuehluebergabe.VorgabeVorlaufC(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(12.0, Kuehluebergabe.VorgabeRuecklaufC(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(0.5, Kuehluebergabe.VorgabeStrahlungsanteil(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(0.5, Kuehluebergabe.VorgabeStrahlungsanteil(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.Equal(0.0, Kuehluebergabe.VorgabeStrahlungsanteil(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR));
            Assert.Equal(16.0, Kuehluebergabe.VorgabeVorlaufgrenzeC(DbWerte.KUEHLUEBERGABE_KUEHLDECKE));
            Assert.Equal(16.0, Kuehluebergabe.VorgabeVorlaufgrenzeC(DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG));
            Assert.True(double.IsNaN(Kuehluebergabe.VorgabeVorlaufgrenzeC(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR)), "Gebläsekonvektor ohne Grenze");
            Assert.True(double.IsNaN(Kuehluebergabe.VorgabeExponent(null)));
            Assert.True(double.IsNaN(Kuehluebergabe.VorgabeVorlaufC("RADIATOR")));
        }

        /// <summary>
        /// Die Wirksamkeit der Kälteseite (A1, E32): Stufe, Projektschalter, <c>Kuehlung_Aktiv</c>,
        /// Kühlsollwert, <c>Kuehluebergabe_Aktiv</c> und eine Art ungleich ideal — unabhängig vom
        /// Heizkreis.
        /// </summary>
        [Fact]
        public void Wirksamkeit_der_Kaelteseite()
        {
            string kd = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            Assert.True(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, true, kd));
            Assert.True(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK2, true, true, 24.0, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(null, true, true, 24.0, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AUS, true, true, 24.0, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, false, true, 24.0, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, false, 24.0, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, null, true, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, false, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, null, kd));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, true, null));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, true, " "));
            Assert.False(Kuehluebergabe.KopplungWirksam(DbWerte.ANLAGENKOPPLUNG_AK1, true, true, 24.0, true, DbWerte.KUEHLUEBERGABE_IDEAL));
        }

        /// <summary>Der Spiegel der Kennwerte: Spreizung R − V, Übertemperatur θ_i,N − (V + R)/2, W_K = Φ_N/(R − V).</summary>
        [Fact]
        public void Gespiegelte_Kennwerte()
        {
            Uebergabekennwerte k = Kuehluebergabe.Gespiegelt(3000.0, 1.0, 7.0, 12.0, 26.0);
            Assert.Equal(-7.0, k.AuslegungVorlaufC);
            Assert.Equal(-12.0, k.AuslegungRuecklaufC);
            Assert.Equal(-26.0, k.AuslegungRaumC);
            Assert.Equal(5.0, k.SpreizungNK);
            Assert.Equal(16.5, k.DeltaThetaMNK);
            Assert.Equal(600.0, k.WHWK);
            // Im Auslegungspunkt liefert die offene Übergabe Φ_N (bei n = 1 geschlossen).
            Assert.Equal(3000.0, Kuehluebergabe.LeistungOffenW(k, 7.0, 26.0), 9);
            Assert.Equal(12.0, Kuehluebergabe.RuecklaufC(k, 7.0, 3000.0), 12);
        }
    }
}
