using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die beiden Ganglinien-Regeln, die mit iU9-W11a aus
    /// <c>Views/Simulation/GanglinienDarstellung.cs</c> in den Kern gezogen sind
    /// (<see cref="Ganglinie"/>).
    ///
    /// <para><b>Was hier geprueft wird.</b> Die Dauerlinie ist eine SORTIERTE KOPIE:
    /// monoton fallend, gleiche Laenge, gleiche Summe, und der Quellvektor bleibt
    /// unberuehrt. Der letzte Punkt ist der eigentliche Grund fuer die Kopie — CSV-Export,
    /// Achsenskalierung und das Zurueckschalten in die chronologische Darstellung
    /// arbeiten mit demselben Vektor weiter.</para>
    ///
    /// <para>Zwei Rasterlaengen, weil der Bestand beide fuehrt: 8 760 Stundenwerte
    /// (Waermeseite) und 35 040 Viertelstundenwerte (Stromseite).</para>
    ///
    /// <para>Ohne Datenbank — reine Rechnung auf <c>float[]</c>.</para>
    /// </summary>
    public class GanglinieTests
    {
        private const int STUNDEN_JAHR = 8760;
        private const int VIERTELSTUNDEN_JAHR = 8760 * 4;

        /// <summary>Ein wiederholbarer Pseudozufallsvektor — fester Startwert, kein Rauschen im Test.</summary>
        private static float[] Reihe(int laenge, int startwert)
        {
            Random r = new Random(startwert);
            float[] werte = new float[laenge];
            for (int i = 0; i < laenge; i++) werte[i] = (float)(r.NextDouble() * 500.0);
            return werte;
        }

        [Fact]
        public void Dauerlinie_faellt_monoton_ueber_8760_Stunden()
        {
            float[] werte = Reihe(STUNDEN_JAHR, 1030);
            float[] dauer = Ganglinie.Dauerlinie(werte);

            Assert.Equal(STUNDEN_JAHR, dauer.Length);
            for (int i = 1; i < dauer.Length; i++)
                Assert.True(dauer[i] <= dauer[i - 1],
                            "Dauerlinie steigt an Stelle " + i + ": " + dauer[i - 1] + " -> " + dauer[i]);
        }

        [Fact]
        public void Dauerlinie_faellt_monoton_ueber_35040_Viertelstunden()
        {
            float[] werte = Reihe(VIERTELSTUNDEN_JAHR, 1007);
            float[] dauer = Ganglinie.Dauerlinie(werte);

            Assert.Equal(VIERTELSTUNDEN_JAHR, dauer.Length);
            for (int i = 1; i < dauer.Length; i++)
                Assert.True(dauer[i] <= dauer[i - 1], "Dauerlinie steigt an Stelle " + i);
        }

        /// <summary>
        /// Umsortieren aendert die Jahressumme nicht — das ist die fachliche Zusage der
        /// Dauerlinie: dieselbe Energie, andere Reihenfolge.
        /// </summary>
        [Fact]
        public void Dauerlinie_haelt_die_Summe()
        {
            float[] werte = Reihe(STUNDEN_JAHR, 1017);
            float[] dauer = Ganglinie.Dauerlinie(werte);

            double vorher = werte.Sum(x => (double)x);
            double nachher = dauer.Sum(x => (double)x);
            Assert.Equal(vorher, nachher, 6);
        }

        /// <summary>
        /// Die Kopie ist der Zweck: Der Quellvektor darf sich nicht bewegen.
        /// </summary>
        [Fact]
        public void Dauerlinie_laesst_den_Quellvektor_unberuehrt()
        {
            float[] werte = Reihe(64, 42);
            float[] abzug = (float[])werte.Clone();

            Ganglinie.Dauerlinie(werte);

            Assert.Equal(abzug, werte);
        }

        [Fact]
        public void Dauerlinie_vertraegt_null()
        {
            Assert.Null(Ganglinie.Dauerlinie(null));
        }

        /// <summary>
        /// Ohne <c>sortiert</c> kommt der ORIGINALVEKTOR zurueck, nicht eine Kopie — das
        /// Zurueckschalten stellt damit bitgleich denselben Kurvenverlauf her.
        /// </summary>
        [Fact]
        public void Anzeigewerte_liefert_unsortiert_denselben_Vektor()
        {
            float[] werte = Reihe(128, 7);
            Assert.Same(werte, Ganglinie.Anzeigewerte(werte, false));
        }

        [Fact]
        public void Anzeigewerte_liefert_sortiert_die_Dauerlinie()
        {
            float[] werte = Reihe(128, 8);
            float[] anzeige = Ganglinie.Anzeigewerte(werte, true);

            Assert.NotSame(werte, anzeige);
            for (int i = 1; i < anzeige.Length; i++)
                Assert.True(anzeige[i] <= anzeige[i - 1]);
        }
    }
}
