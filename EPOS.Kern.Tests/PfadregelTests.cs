using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Pfadregel</b> (Konzept Diagramme, Entscheid DG-Q5: „eine Konstante im
    /// Modell"): Wie viele Stuetzstellen einer Reihe in den SVG-Pfad gehen.
    ///
    /// <para>Geprueft wird dreierlei: die ROHGRENZE (bis 8 760 Stuetzstellen und drei
    /// Reihen geht der Pfad roh, darueber gebuendelt), dass die BUENDELUNG keine
    /// Spitze verliert — sie ist der ganze Grund, warum nicht einfach jeder n-te Wert
    /// genommen wird — und dass sie DETERMINISTISCH ist, ohne die kein byte-gleicher
    /// SVG-Text moeglich waere.</para>
    /// </summary>
    public class PfadregelTests
    {
        // =====================================================================
        // 1 — Die Rohgrenze
        // =====================================================================

        /// <summary>
        /// Die zwei Zahlen stehen als Konstanten da und nicht verstreut in einer
        /// Zeichenmethode: Ein Stundenjahr geht roh, eine Viertelstundenreihe nicht,
        /// und ab der vierten Reihe wird gebuendelt (Pruefstand vom 20.09.2026 —
        /// sechs rohe Reihen liessen einen Bildaufbau von 60 aus).
        /// </summary>
        [Fact]
        public void DieRohgrenzeIstEinStundenjahrUndDreiReihen()
        {
            Assert.Equal(8760, Pfadregel.ROH_BIS_STUETZSTELLEN);
            Assert.Equal(3, Pfadregel.ROH_BIS_REIHEN);

            Assert.True(Pfadregel.Roh(8760, 3));       // genau auf der Grenze
            Assert.True(Pfadregel.Roh(2, 1));
            Assert.False(Pfadregel.Roh(8761, 1));      // eine Stuetzstelle darueber
            Assert.False(Pfadregel.Roh(35040, 1));     // Viertelstundenreihe
            Assert.False(Pfadregel.Roh(8760, 4));      // die vierte Reihe
        }

        // =====================================================================
        // 2 — Die Buendelung haelt die Spitzen
        // =====================================================================

        /// <summary>
        /// Je Bildpunktspalte Minimum UND Maximum: Eine Spitze und ein Tal MITTEN in
        /// einer Spalte bleiben im Pfad, obwohl die Spalte zehn Werte zusammenfasst.
        /// Genau das verliert „jeder n-te Wert", mit dem das PNG heute zeichnet.
        /// </summary>
        [Fact]
        public void DieBuendelungVerliertKeineSpitze()
        {
            var werte = new double[100];
            for (int i = 0; i < werte.Length; i++) werte[i] = 1.0;
            werte[45] = 99.0;    // die Spitze
            werte[47] = -7.0;    // das Tal, in derselben Spalte

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 10);

            Assert.Contains(punkte, p => p.X == 45f && p.Y == 99f);
            Assert.Contains(punkte, p => p.X == 47f && p.Y == -7f);
            Assert.True(punkte.Count <= 2 * 10,
                        "hoechstens zwei Punkte je Spalte, gezaehlt: " + punkte.Count);
        }

        /// <summary>
        /// „In Indexreihenfolge" heisst: Der Pfad laeuft von links nach rechts durch,
        /// auch innerhalb einer Spalte. Liefe er zurueck, entstuende im Bild ein
        /// Zickzack ueber die Spaltenbreite statt einer senkrechten Spanne.
        /// </summary>
        [Fact]
        public void DiePunkteStehenInIndexreihenfolge()
        {
            var werte = new double[500];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = Math.Sin(i * 0.37) + 0.2 * Math.Sin(i * 3.1);

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 60);

            Assert.NotEmpty(punkte);
            for (int i = 1; i < punkte.Count; i++)
                Assert.True(punkte[i].X > punkte[i - 1].X,
                            "Punkt " + i + " springt zurueck: " + punkte[i].X +
                            " nach " + punkte[i - 1].X);
        }

        /// <summary>
        /// Mehr Spalten als Werte: Jede Spalte traegt hoechstens einen Wert, und dann
        /// steht jeder Wert genau einmal im Pfad — die Buendelung ist bei 1:1 vom
        /// rohen Pfad nicht zu unterscheiden.
        /// </summary>
        [Fact]
        public void BeiMehrSpaltenAlsWertenStehtJederWertEinmal()
        {
            var werte = new double[] { 3, 1, 4, 1, 5 };

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 100);

            Assert.Equal(werte.Length, punkte.Count);
            for (int i = 0; i < werte.Length; i++)
            {
                Assert.Equal(i, punkte[i].X);
                Assert.Equal((float)werte[i], punkte[i].Y);
            }
        }

        // =====================================================================
        // 3 — Determinismus
        // =====================================================================

        /// <summary>
        /// Zweimal gebuendelt ergibt dieselbe Punktfolge. Ohne diese Zusage waere der
        /// byte-gleiche SVG-Text nicht zu halten.
        /// </summary>
        [Fact]
        public void ZweimalGebuendeltErgibtDieselbenPunkte()
        {
            var werte = new double[8760];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = 20 + 15 * Math.Sin(2 * Math.PI * i / 8760.0) + Math.Sin(i * 0.9);

            IReadOnlyList<Punkt> a = Pfadregel.Gebuendelt(werte, 1154);
            IReadOnlyList<Punkt> b = Pfadregel.Gebuendelt(werte, 1154);

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i]);
        }

        /// <summary>Eine leere oder fehlende Reihe gibt keine Punkte statt einer Ausnahme.</summary>
        [Fact]
        public void OhneWerteGibtEsKeinePunkte()
        {
            Assert.Empty(Pfadregel.Gebuendelt(null, 10));
            Assert.Empty(Pfadregel.Gebuendelt(new double[0], 10));
            Assert.Single(Pfadregel.Gebuendelt(new double[] { 7.0 }, 0));   // Spalten < 1
        }
    }
}
