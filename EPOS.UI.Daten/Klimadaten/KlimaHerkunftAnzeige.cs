using System;
using System.Globalization;
using EPOS.UI.Seiten.Start;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Aus der Kern-Auskunft <see cref="KlimaHerkunft"/> die Gaben der
    /// Herkunftszeile</b> — übersetzt und kulturgerecht datiert, EINMAL für beide
    /// Klapplisten der Klimaregion: die Kopfleiste der Startseite (Projektkopie,
    /// <c>StartseiteHuelle</c>) und den Projektassistenten (Katalogsatz,
    /// <see cref="ProjektKopfHuelle"/>).
    ///
    /// <para>Das erste Glied nennt das ganze Wetterjahr — „TRY-Regionaldaten
    /// (Deutschland) · 2045 · sommerwarm". Den Satz baut der Kern
    /// (<see cref="KlimaAnzeige.Quellenzeile"/>), derselbe, der auch die Spalte
    /// „Quelle" der Regionsliste füllt; eine zweite Übersetzung hier wäre ein zweiter
    /// Wortlaut.</para>
    /// </summary>
    internal static class KlimaHerkunftAnzeige
    {
        /// <summary>Die Gaben zur Auskunft; <c>null</c> bleibt <c>null</c> (keine Zeile).</summary>
        internal static KlimaHerkunftGaben Gaben(KlimaHerkunft herkunft)
        {
            if (herkunft == null) return null;

            return new KlimaHerkunftGaben(
                KlimaAnzeige.Quellenzeile(herkunft.Quelle, herkunft.Szenario, herkunft.Bezugsjahr),
                herkunft.Bezeichner, herkunft.Standort, Datumstext(herkunft.Importdatum));
        }

        /// <summary>
        /// Das ISO-Importdatum in der Landesschreibweise. Es steht so in der
        /// Datenbank, weil es dort sortierbar sein muss; gelesen wird es vom
        /// Anwender. Was sich nicht als ISO lesen lässt, bleibt, wie es ist.
        /// </summary>
        internal static string Datumstext(string iso)
        {
            string wert = (iso ?? "").Trim();
            if (wert.Length == 0) return "";

            return DateTime.TryParseExact(wert, "yyyy-MM-dd",
                                          CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out DateTime tag)
                ? tag.ToString("d", CultureInfo.CurrentCulture)
                : wert;
        }
    }
}
