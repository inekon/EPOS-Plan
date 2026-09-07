using System;
using WPPlan.Core;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids W8‑O‑5d‑Q2</b> vom 07.09.2026: „keine Treue
    /// zur alten DLL". Die drei Physik-Funktionen des BHKW-Plan-Ports geben seither
    /// <c>double</c> zurück und schneiden nicht mehr ab.
    ///
    /// <para><b>Was abgeschafft wurde.</b> Die native <c>BHKWPLAN.DLL</c> gab
    /// <see cref="BhkwPlan.SolareGewinneC"/>, <see cref="BhkwPlan.SpezWaermeverlusteC"/>
    /// und <see cref="BhkwPlan.TaeglHeizlastWG"/> als <c>int</c> zurück (Borland
    /// <c>_ftol</c>, Abschneiden Richtung Null); der Port hat das nachgebildet. Zwei der
    /// drei liefern das Hundertfache und werden vom Aufrufer wieder durch 100 geteilt —
    /// die Quantisierung landete damit als Hundertstel in den Eingangsgrößen der
    /// Tagesheizlast. Bei <see cref="BhkwPlan.SpezWaermeverlusteC"/> kam ein ZWEITES
    /// Abschneiden dazu: <c>SimulationWaermebedarf</c> teilte das <c>int</c>-Ergebnis mit
    /// <c>/ 100</c>, also ganzzahlig.</para>
    ///
    /// <para><b>Warum das kein Rundungsstreit war.</b> Die Tagesheizlast geht über das
    /// Tagesprofil in 24 Stundenwerte, und die Schwellen des Modells tragen eine
    /// Verschiebung über Stunden weiter. Bei der Umstellung auf <c>double</c> verschob
    /// eine Stelle hinter dem Komma die Tagesheizlast eines Januartags in Projekt 1041
    /// um 0,39 % (<c>protokoll.txt</c> der Basis <c>2026-09-07_R4_Double</c>).</para>
    ///
    /// <para>Ohne Datenbank — die drei Funktionen sind reine Rechnungen über ihre
    /// Argumente. Einzige Ausnahme ist der globale Zustand der Vortemperatur in
    /// <see cref="BhkwPlan.TaeglHeizlastWG"/>; jeder Fall setzt ihn über
    /// <see cref="BhkwPlan.ResetState"/> zurück.</para>
    /// </summary>
    public class BhkwPlanRueckgabeTests
    {
        // =====================================================================
        //  1 — Die Nachkommastellen überleben
        // =====================================================================

        /// <summary>
        /// <b>Solare Gewinne.</b> Der Fall ist so gewählt, dass das Hundertfache
        /// AUSDRÜCKLICH keine ganze Zahl ist: 137,5 · 1 · 0,73 · 100 = 10 037,5. Die alte
        /// Fassung gab 10 037 zurück; nach der Division durch 100 beim Aufrufer waren das
        /// 100,37 statt 100,375 W.
        /// </summary>
        [Fact]
        public void Die_solaren_Gewinne_behalten_ihre_Nachkommastelle()
        {
            double wert = BhkwPlan.SolareGewinneC(
                en: 137.5, an: 1.0, ew: 0.0, eo: 0.0, awo: 0.0, es: 0.0, uAs: 0.0,
                transmissionsgrad: 0.73);

            Assert.Equal(10_037.5, wert, 9);
            Assert.NotEqual(Math.Truncate(wert), wert);

            // GEGENPROBE: Genau diese halbe Einheit hat die alte Fassung verschluckt.
            Assert.Equal(0.5, wert - Math.Truncate(wert), 9);
        }

        /// <summary>
        /// <b>Der spezifische Wärmeverlustkoeffizient</b> — hier wirkte das Abschneiden
        /// doppelt (Funktion und Aufrufer). Ein reiner Transmissionsfall über die
        /// Fensterfläche: 1,25 W/(m²·K) · 10,03 m² · 100 = 1 253,75.
        /// </summary>
        [Fact]
        public void Der_Waermeverlustkoeffizient_behaelt_seine_Nachkommastellen()
        {
            double wert = BhkwPlan.SpezWaermeverlusteC(
                kw: 0, aw: 0, kf: 1.25, af: 10.03, kd: 0, ad: 0, kg: 0, ag: 0,
                ks: 0, uAs: 0, kwb1: 0, lwb1: 0, kwb2: 0, lwb2: 0, kwb3: 0, lwb3: 0,
                aussenTemp: 5.0, wohnflaeche: 0, raumhoehe: 0, lwr: 0);

            Assert.Equal(1_253.75, wert, 9);
            Assert.NotEqual(Math.Truncate(wert), wert);

            // Und die Kette, die der Aufrufer bildet: frueher (int)1253 / (int)100 = 12,
            // heute 12,5375. Das ist der Unterschied, um den es geht - nicht 0,75 W/K,
            // sondern ein halbes Watt je Kelvin auf einen Wert von zwoelfeinhalb.
            Assert.Equal(12.5375, wert / 100.0, 9);
            Assert.Equal(12, 1253 / 100);
        }

        /// <summary>
        /// <b>Die tägliche Heizlast.</b> Der Aufbau ist bewusst schlicht — ein Gebäude
        /// ohne solare und innere Gewinne, konstanter Sollwert, damit die Zahl
        /// nachvollziehbar bleibt. Geprüft wird nicht ihr Betrag, sondern dass sie eine
        /// gebrochene Zahl IST: Die alte Fassung konnte das gar nicht liefern.
        /// </summary>
        [Fact]
        public void Die_Tagesheizlast_ist_keine_ganze_Zahl_mehr()
        {
            BhkwPlan.ResetState();
            double wert = Tagesheizlast(gesamtflaeche: 137.0, wohnflaeche: 120.0);

            Assert.True(wert > 0, "Das Probegebäude heizt gar nicht — der Fall prüft dann nichts.");
            Assert.NotEqual(Math.Truncate(wert), wert);
        }

        /// <summary>
        /// <b>Der Skalierungsschritt war der schlimmste.</b> Die Tagessumme wird zuletzt
        /// von der Katalog- auf die Projektfläche gestreckt
        /// (<c>· Gesamtflaeche / Wohnflaeche</c>) — und GENAU danach schnitt die alte
        /// Fassung ab. Zwei Gebäude, deren Flächen sich unterhalb einer Einheit
        /// Tagesheizlast auswirken, lieferten deshalb dieselbe Zahl. Heute nicht mehr:
        /// Der Unterschied ist genau das Flächenverhältnis.
        /// </summary>
        [Fact]
        public void Die_Flaechenskalierung_wirkt_jetzt_ungerundet()
        {
            BhkwPlan.ResetState();
            double a = Tagesheizlast(gesamtflaeche: 137.0, wohnflaeche: 120.0);

            BhkwPlan.ResetState();
            double b = Tagesheizlast(gesamtflaeche: 137.000001, wohnflaeche: 120.0);

            Assert.NotEqual(a, b);
            Assert.Equal(a * 137.000001 / 137.0, b, 6);

            // GEGENPROBE: Die alte Fassung hätte beide auf dieselbe ganze Zahl
            // abgeschnitten — der Unterschied ist 0,6 Milliwattstunden am Tag.
            Assert.True(Math.Abs(b - a) < 1.0);
            Assert.Equal(Math.Truncate(a), Math.Truncate(b));
        }

        // =====================================================================
        //  2 — Der Rückgabetyp selbst
        // =====================================================================

        /// <summary>
        /// <b>Der Wächter über den Typ.</b> Die drei Funktionen geben <c>double</c>
        /// zurück. Fällt jemand auf <c>int</c> zurück, meldet es dieser Fall statt erst
        /// der nächste Referenzlauf.
        /// </summary>
        [Fact]
        public void Alle_drei_Physikfunktionen_geben_double_zurueck()
        {
            foreach (string name in new[] { "SolareGewinneC", "SpezWaermeverlusteC", "TaeglHeizlastWG" })
            {
                var m = typeof(BhkwPlan).GetMethod(name);
                Assert.NotNull(m);
                Assert.Equal(typeof(double), m.ReturnType);
            }
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        /// <summary>
        /// Ein Tag des Kapazitätsmodells mit festem Sollwert, ohne Absenkung, ohne
        /// solare und innere Gewinne. <c>day = 1</c>, damit die Vortemperatur aus dem
        /// Nachtsollwert startet und der Fall nicht von einem Vorlauf abhängt.
        /// </summary>
        private static double Tagesheizlast(double gesamtflaeche, double wohnflaeche)
        {
            return BhkwPlan.TaeglHeizlastWG(
                day: 1, weAbsenkung: 0, weTemp: 20.0, ferienAbsenkung: 0, ferienTemp: 20.0,
                raumsolltempTag: 20.0, raumsolltempNacht: 20.0,
                innereGewinne: 0.0, solareGewinne: 0.0,
                spezWaermeverluste: 137.37, gebaeudeKapazitaet: 9_000.0,
                aussenTemp: -3.3, maxRaumtemp: 24.0,
                gesamtflaeche: gesamtflaeche, wohnflaeche: wohnflaeche);
        }
    }
}
