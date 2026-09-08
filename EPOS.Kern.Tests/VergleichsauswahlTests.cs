using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die geteilte Vergleichswahl der Seiten Übersicht, Kosten und Wirtschaftlichkeit
    /// (Anwenderwunsch 08.09.2026, W5‑B‑5): Vorgabe alle, der Stamm immer, Abgewähltes
    /// bleibt abgewählt, Neues ist von selbst dabei.
    /// </summary>
    public class VergleichsauswahlTests
    {
        private static readonly int[] GRUPPE = { 1030, 1031, 1032 };
        private const int STAMM = 1030;

        [Fact]
        public void Vorgabe_ist_die_ganze_Gruppe()
        {
            var w = new Vergleichsauswahl();
            Assert.Equal(GRUPPE, w.Gewaehlte(GRUPPE, STAMM));
            Assert.Equal(0, w.AnzahlAbgewaehlt);
        }

        [Fact]
        public void Abgewaehltes_bleibt_abgewaehlt_und_der_Stamm_immer_dabei()
        {
            var w = new Vergleichsauswahl();
            w.Setzen(new[] { 1030, 1032 }, GRUPPE, STAMM);
            Assert.Equal(new[] { 1030, 1032 }, w.Gewaehlte(GRUPPE, STAMM));
            Assert.False(w.IstGewaehlt(1031, STAMM));

            // Niemand kann den Stamm abwaehlen - er ist die Referenz.
            w.Setzen(new int[0], GRUPPE, STAMM);
            Assert.Equal(new[] { 1030 }, w.Gewaehlte(GRUPPE, STAMM));
            Assert.True(w.IstGewaehlt(STAMM, STAMM));
        }

        [Fact]
        public void Eine_neue_Variante_ist_von_selbst_im_Vergleich()
        {
            var w = new Vergleichsauswahl();
            w.Setzen(new[] { 1030, 1032 }, GRUPPE, STAMM);
            Assert.Equal(new[] { 1030, 1032, 1033 }, w.Gewaehlte(new[] { 1030, 1031, 1032, 1033 }, STAMM));
        }

        [Fact]
        public void Geaendert_meldet_nur_echte_Aenderungen()
        {
            var w = new Vergleichsauswahl();
            int gemeldet = 0;
            w.Geaendert += () => gemeldet++;

            w.Setzen(GRUPPE, GRUPPE, STAMM);                 // alles bleibt gewaehlt
            Assert.Equal(0, gemeldet);
            w.Setzen(new[] { 1030, 1032 }, GRUPPE, STAMM);   // 1031 abgewaehlt
            Assert.Equal(1, gemeldet);
            w.Setzen(new[] { 1030, 1032 }, GRUPPE, STAMM);   // dieselbe Wahl noch einmal
            Assert.Equal(1, gemeldet);
            w.Setzen(GRUPPE, GRUPPE, STAMM);                 // 1031 wieder dabei
            Assert.Equal(2, gemeldet);
        }

        [Fact]
        public void Andere_Gruppen_bleiben_unberuehrt()
        {
            var w = new Vergleichsauswahl();
            w.Setzen(new[] { 1030 }, GRUPPE, STAMM);          // 1031 und 1032 abgewaehlt
            w.Setzen(new[] { 2000, 2001 }, new[] { 2000, 2001 }, 2000);
            Assert.Equal(new[] { 1030 }, w.Gewaehlte(GRUPPE, STAMM));
            Assert.Equal(2, w.AnzahlAbgewaehlt);
        }
    }
}
