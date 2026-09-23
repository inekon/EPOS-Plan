using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenwege, die die drei Zeitreihenverwaltungen teilen</b> (Konzept
    /// Administrationsdialoge, Stufe 4) — Wärmebedarf Lastgang, Solarthermieganglinie und
    /// Stromganglinie, je eine Ausprägung von <see cref="Zeitreihenart"/>.
    ///
    /// <para><b>Die Ansicht im Stammblatt</b> (V9, Gruppe „Ganglinie"): die Stundenreihe
    /// des Katalogsatzes über <see cref="GanglinienAuswertungCtrl.AusKatalog"/> — derselbe
    /// Leseweg wie in den Projektdialogen —, daraus das Zeichenmodell des Jahresverlaufs
    /// (<see cref="ChartRenderer.JahresverlaufModell(string, double[], string, Farbrolle, Achsenfenster)"/>)
    /// und die drei Kennzahlen. Plattformfrei: Die Windows-Hüllen der Wärme- und
    /// Solarverwaltung und die plattformfreie Stromhülle rufen dieselbe Stelle.</para>
    ///
    /// <para><b>Die Verwendung in Projekten</b> (V8, weiche Löschsperre mit dem
    /// Projektnamen) kommt aus EINER Abfrage je Liste
    /// (<see cref="ZeitreihenKatalogCtrl.Projektverwendung"/>).</para>
    /// </summary>
    internal static class ZeitreihenAdminWege
    {
        /// <summary>Bild und Kennzahlen eines Katalogsatzes.</summary>
        internal static Task<Ganglinienansicht> Ansicht(Zeitreihenart art, string bezeichner)
            => Task.FromResult(AnsichtLesen(art, bezeichner));

        /// <summary>
        /// Liest die Reihe und baut Bild und Kennzahlen. <b>Ohne brauchbare Reihe</b>
        /// (weder 8 760 noch 35 040 Werte, oder der Satz ist fort) kommt der Grund zurück
        /// statt eines leeren Bildes.
        /// </summary>
        internal static Ganglinienansicht AnsichtLesen(Zeitreihenart art, string bezeichner)
        {
            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(
                GanglinienQuelle.Zu(art), bezeichner ?? "");
            if (a == null || !a.Erfolgreich)
                return Ganglinienansicht.Ohne(MyResource.Resource.ADM_SB_KEINE_GANGLINIE);

            Zeichenmodell modell = ChartRenderer.JahresverlaufModell(
                "", a.Stundenwerte, MyResource.Resource.CHART_ACHSE_LEISTUNG, Rolle(art));

            return new Ganglinienansicht(modell,
                new GanglinienKennzahlen(a.JahresarbeitMwh, a.SpitzeKw, a.VollbenutzungsstundenH));
        }

        /// <summary>Welche Projekte welchen Satz verwenden — je Bezeichner die Projektnamen.</summary>
        internal static Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> Verwendung(Zeitreihenart art)
            => Task.FromResult(ZeitreihenKatalogCtrl.Projektverwendung(art));

        /// <summary>
        /// Die Farbrolle der Linie: dieselbe Größe wie in den Projektdialogen — Strom als
        /// Bedarf, der Wärmebedarf als Heizwärme, die Solarthermie als Solarwärme.
        /// </summary>
        private static Farbrolle Rolle(Zeitreihenart art)
        {
            switch (art)
            {
                case Zeitreihenart.Stromganglinie: return Farbrolle.BEDARF;
                case Zeitreihenart.Solarganglinie: return Farbrolle.WAERME_SOLAR;
                default:                           return Farbrolle.HEIZWAERME;
            }
        }
    }
}
