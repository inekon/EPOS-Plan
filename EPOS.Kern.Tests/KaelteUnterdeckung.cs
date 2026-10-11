using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Fallbildung Kälte-Unterdeckung</b> für die Bestandstests der Kälteseite, die eine Rest-Reihe, einen
    /// Deckungsgrad unter 100 % oder eine Maschine an der Leistungsgrenze brauchen. Die gesäten Kälteprojekte
    /// 1017 und 1047 decken ihre Kälte mit der Zonensperre (Entwurf AK3-K, Abschnitt 3; Basis R43) ganz — ihre
    /// frühere Unterdeckung von rund 0,01 MWh/a lag allein an Kühlstunden der Heiztage. Die Fallbildung mindert
    /// deshalb in der Arbeitskopie der Testdatenbank die Kälteleistung <c>Pkuehl</c> aller Stützstellen der
    /// Kühlkennlinie der Projektwärmepumpe(n): Die Unterdeckung kommt dann aus der Leistungsgrenze des Erzeugers
    /// und hängt nicht an der Zonensperre. COP und Stützstellen bleiben, wie sie sind.
    /// </summary>
    internal static class KaelteUnterdeckung
    {
        /// <summary>
        /// Faktor auf <c>Pkuehl</c>: Die Wärmepumpe von 1017/1047 (12 bis 15 kW Kälte je nach Stützstelle) behält
        /// die Hälfte und deckt die Spitzenstunden der Kühltage nicht mehr ganz.
        /// </summary>
        internal const double FAKTOR = 0.5;

        /// <summary>
        /// Mindert die Kühlkennlinie der Wärmepumpen des Projekts auf <paramref name="faktor"/> und liefert die
        /// Zahl der geänderten Stützstellen (die Fallbildung greift nur, wenn es welche gibt).
        /// </summary>
        internal static int WaermepumpeMindern(int projekt, double faktor = FAKTOR)
        {
            int n = DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Kenndaten_Kuehlung SET Pkuehl = Pkuehl * ? WHERE ID_WP IN " +
                "(SELECT ID_WP FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP IS NOT NULL)",
                new DbParam("?", faktor), new DbParam("?", projekt));
            Assert.True(n > 0, string.Format(CultureInfo.InvariantCulture,
                "Projekt {0}: keine Kühlkennlinie einer Projektwärmepumpe gefunden.", projekt));
            return n;
        }
    }
}
