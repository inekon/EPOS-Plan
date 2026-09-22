using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Rechenproben des Vorlaufs <see cref="Vorlauf2K"/> (Stufe G0, Rechenschritte 7.2 und
    /// 10.4): Konvergenz unter die Schwelle, monoton; Determinismus; Nichtkonvergenz als
    /// benannter Fehler; Unabhängigkeit vom Startwert; Ausweis von Durchläufen und
    /// Rechenzeit. Nur Phantasiewerte (<see cref="Phantasiegebaeude"/>), keine Zahl einer
    /// Richtlinie.
    /// </summary>
    public class Vorlauf2KTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public Vorlauf2KTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        [Fact]
        public void Die_Regel_steht_als_benannte_Konstanten()
        {
            Assert.Equal(168, Vorlauf2K.WOCHE_H);
            Assert.Equal(0.01, Vorlauf2K.SCHWELLE_K);
            Assert.Equal(20, Vorlauf2K.HOECHSTZAHL_DURCHLAEUFE);
        }

        [Theory]
        [InlineData(false, false, 10.0)]
        [InlineData(true, false, 10.0)]
        [InlineData(true, true, 35.0)]
        [InlineData(true, true, -5.0)]
        public void Der_Vorlauf_konvergiert_monoton_unter_die_Schwelle(bool mitRegelung, bool mitKuehlung, double thetaStart)
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));
            Vorlaufergebnis v = Vorlauf2K.Einschwingen(m, thetaStart, Woche(mitRegelung, mitKuehlung));

            Assert.InRange(v.Durchlaeufe, 2, Vorlauf2K.HOECHSTZAHL_DURCHLAEUFE);
            Assert.True(v.AenderungenK[v.Durchlaeufe - 1] < Vorlauf2K.SCHWELLE_K);
            for (int i = 0; i < v.Durchlaeufe - 1; i++)
            {
                Assert.True(v.AenderungenK[i] >= Vorlauf2K.SCHWELLE_K, "Durchlauf " + (i + 1) + " lag schon unter der Schwelle.");
                Assert.True(v.AenderungenK[i + 1] < v.AenderungenK[i], "Die Änderung wächst in Durchlauf " + (i + 2) + ".");
            }

            // Das Modell trägt den eingeschwungenen Zustand.
            Assert.Equal(v.ThetaMAw, m.ThetaMAw);
            Assert.Equal(v.ThetaMIw, m.ThetaMIw);
            _ausgabe.WriteLine(Zeile("Regelung " + mitRegelung + ", Kühlung " + mitKuehlung + ", Start " + thetaStart.ToString(CultureInfo.InvariantCulture), v));
        }

        [Fact]
        public void Der_Vorlauf_ist_deterministisch()
        {
            Stundenrand[] woche = Woche(mitRegelung: true, mitKuehlung: true);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));

            Vorlaufergebnis va = Vorlauf2K.Einschwingen(a, 12.0, woche);
            Vorlaufergebnis vb = Vorlauf2K.Einschwingen(b, 12.0, woche);
            Assert.Equal(va.AenderungenK.ToArray(), vb.AenderungenK.ToArray());
            Assert.Equal(BitConverter.DoubleToInt64Bits(va.ThetaMAw), BitConverter.DoubleToInt64Bits(vb.ThetaMAw));
            Assert.Equal(BitConverter.DoubleToInt64Bits(va.ThetaMIw), BitConverter.DoubleToInt64Bits(vb.ThetaMIw));

            // Dieselbe Instanz nach einem fremden Lauf: der Vorlauf setzt selbst zurück.
            for (int h = 0; h < 50; h++) a.Schritt(Phantasiegebaeude.Rand(-10.0, 22.0, 24.0, phiConv: 500.0));
            Vorlaufergebnis va2 = Vorlauf2K.Einschwingen(a, 12.0, woche);
            Assert.Equal(va.AenderungenK.ToArray(), va2.AenderungenK.ToArray());
            Assert.Equal(BitConverter.DoubleToInt64Bits(va.ThetaMAw), BitConverter.DoubleToInt64Bits(va2.ThetaMAw));
            Assert.Equal(BitConverter.DoubleToInt64Bits(va.ThetaMIw), BitConverter.DoubleToInt64Bits(va2.ThetaMIw));

            // Und die Woche danach rechnet byte-gleich.
            for (int h = 0; h < woche.Length; h++)
            {
                Stundenergebnis ea = a.Schritt(in woche[h]);
                Stundenergebnis eb = b.Schritt(in woche[h]);
                Assert.Equal(BitConverter.DoubleToInt64Bits(ea.LastW), BitConverter.DoubleToInt64Bits(eb.LastW));
                Assert.Equal(BitConverter.DoubleToInt64Bits(ea.ThetaAirMittel), BitConverter.DoubleToInt64Bits(eb.ThetaAirMittel));
            }
        }

        [Fact]
        public void Nichtkonvergenz_ist_ein_benannter_Fehler()
        {
            // Ein Gebäude mit hundertfacher Masse schwingt in zwanzig Wochen nicht ein.
            var schwer = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true, cAW: 2.0e9, cIW: 5.0e9), "Schwer");
            var f = Assert.Throws<GebaeudeModellException>(
                () => Vorlauf2K.Einschwingen(schwer, 0.0, Woche(mitRegelung: true, mitKuehlung: true)));
            Assert.Equal(GebaeudeModellFehler.VorlaufNichtKonvergiert, f.Grund);
            Assert.Contains(Vorlauf2K.HOECHSTZAHL_DURCHLAEUFE.ToString(System.Globalization.CultureInfo.InvariantCulture), f.Message);
            _ausgabe.WriteLine(f.Message);

            // Dasselbe mit enger Höchstzahl am Standardgebäude.
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));
            f = Assert.Throws<GebaeudeModellException>(
                () => Vorlauf2K.Einschwingen(m, 40.0, Woche(true, true), hoechstzahl: 1));
            Assert.Equal(GebaeudeModellFehler.VorlaufNichtKonvergiert, f.Grund);

            // Leere Randfolge, ungültiger Start, ungültige Schwelle.
            Assert.Equal(GebaeudeModellFehler.RandUngueltig, Assert.Throws<GebaeudeModellException>(
                () => Vorlauf2K.Einschwingen(m, 20.0, ReadOnlySpan<Stundenrand>.Empty)).Grund);
            Assert.Equal(GebaeudeModellFehler.RandUngueltig, Assert.Throws<GebaeudeModellException>(
                () => Vorlauf2K.Einschwingen(m, double.NaN, Woche(true, true))).Grund);
            Assert.Throws<ArgumentOutOfRangeException>(() => Vorlauf2K.Einschwingen(m, 20.0, Woche(true, true), schwelleK: 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Vorlauf2K.Einschwingen(m, 20.0, Woche(true, true), hoechstzahl: 0));
            Assert.Throws<ArgumentNullException>(() => Vorlauf2K.Einschwingen(null, 20.0, Woche(true, true)));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void Nach_dem_Vorlauf_haengt_der_Zustand_nicht_mehr_am_Startwert(bool mitRegelung, bool mitKuehlung)
        {
            Stundenrand[] woche = Woche(mitRegelung, mitKuehlung);
            double[] starts = { -10.0, 20.0, 45.0 };
            var modelle = starts.Select(_ => new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true))).ToArray();
            var ergebnisse = starts.Select((s, i) => Vorlauf2K.Einschwingen(modelle[i], s, woche)).ToArray();

            // Vor dem Vorlauf 55 K auseinander, danach innerhalb der Schwelle.
            double spanneAw = ergebnisse.Max(v => v.ThetaMAw) - ergebnisse.Min(v => v.ThetaMAw);
            double spanneIw = ergebnisse.Max(v => v.ThetaMIw) - ergebnisse.Min(v => v.ThetaMIw);
            Assert.True(spanneAw < Vorlauf2K.SCHWELLE_K && spanneIw < Vorlauf2K.SCHWELLE_K,
                string.Format(CultureInfo.InvariantCulture, "Spanne nach dem Vorlauf {0:G4} / {1:G4} K", spanneAw, spanneIw));

            // Die Woche danach: Lufttemperatur und Last hängen kaum noch am Start.
            double dAir = 0.0, dLast = 0.0;
            for (int h = 0; h < woche.Length; h++)
            {
                Stundenergebnis[] e = modelle.Select(m => m.Schritt(in woche[h])).ToArray();
                dAir = Math.Max(dAir, e.Max(x => x.ThetaAirMittel) - e.Min(x => x.ThetaAirMittel));
                dLast = Math.Max(dLast, e.Max(x => x.LastW) - e.Min(x => x.LastW));
            }
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Durchläufe {0}; Spanne Masse {1:G3}/{2:G3} K; Folgewoche: Luft {3:G3} K, Last {4:G3} W",
                string.Join("/", ergebnisse.Select(v => v.Durchlaeufe)), spanneAw, spanneIw, dAir, dLast));
            Assert.True(dAir < Vorlauf2K.SCHWELLE_K);
        }

        [Fact]
        public void Durchlaeufe_und_Rechenzeit_werden_ausgewiesen()
        {
            // Nur Ausweis, keine Schwelle.
            foreach ((string name, ErsatzparameterRC p) in new[]
            {
                ("Standard", Phantasiegebaeude.Standard(mitFenster: true)),
                ("Leicht", Phantasiegebaeude.Standard(mitFenster: true, cAW: 5.0e6, cIW: 1.0e7)),
                ("Schwer", Phantasiegebaeude.Standard(mitFenster: true, cAW: 6.0e7, cIW: 1.5e8)),
            })
            {
                var m = new Zonenmodell2K(p, name);
                Stundenrand[] woche = Woche(mitRegelung: true, mitKuehlung: true);
                Vorlauf2K.Einschwingen(m, 20.0, woche);   // Warmlauf
                var uhr = Stopwatch.StartNew();
                Vorlaufergebnis v = Vorlauf2K.Einschwingen(m, 20.0, woche);
                uhr.Stop();
                _ausgabe.WriteLine(Zeile(name, v) + string.Format(CultureInfo.InvariantCulture,
                    ", Zeitkonstante {0:F0} h, Rechenzeit {1:F2} ms",
                    -1.0 / m.Eigenwerte.Max() / 3600.0, uhr.Elapsed.TotalMilliseconds));
                Assert.True(v.Durchlaeufe >= 1);
            }
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        internal static string Zeile(string name, Vorlaufergebnis v)
            => string.Format(CultureInfo.InvariantCulture, "{0}: {1} Durchläufe, Änderungen {2} K, Rest geschätzt {3:G3} K",
                             name, v.Durchlaeufe, string.Join(" ", v.AenderungenK.Select(a => a.ToString("G3", CultureInfo.InvariantCulture))),
                             v.RestabschaetzungK);

        /// <summary>
        /// Eine synthetische Woche: Tagesgang außen um 5 °C, Sonne am Tag, Nutzung tagsüber,
        /// Nachtabsenkung; wahlweise mit Heizung und mit Kappung samt Leistungsgrenzen.
        /// </summary>
        private static Stundenrand[] Woche(bool mitRegelung, bool mitKuehlung)
        {
            var w = new Stundenrand[Vorlauf2K.WOCHE_H];
            for (int h = 0; h < w.Length; h++)
            {
                int stunde = h % 24;
                double aussen = 5.0 - 5.0 * Math.Cos(2.0 * Math.PI * (stunde - 3) / 24.0);
                bool tag = stunde >= 6 && stunde < 22;
                double sonne = Math.Max(0.0, Math.Sin(Math.PI * (stunde - 6) / 12.0)) * 2500.0;
                w[h] = new Stundenrand(
                    aussen, aussen + 0.002 * sonne,
                    mitRegelung ? (tag ? 21.0 : 17.0) : double.NaN,
                    mitKuehlung ? 23.0 : double.NaN,
                    0.2 * sonne, 0.7 * sonne, 0.1 * sonne + (tag ? 600.0 : 100.0),
                    heizleistungMaxW: mitKuehlung ? 4000.0 : double.NaN,
                    kuehlleistungMaxW: mitKuehlung ? 1500.0 : double.NaN,
                    heizungStrahlungsanteil: 0.3);
            }
            return w;
        }
    }
}
