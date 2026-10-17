using System.Globalization;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Datum im Gemeinjahr als Text</b> (Teilkonzept Konditionierungsprofile 3.2): Lesen und
    /// Anzeigen von Tag und Monat ohne Jahr für jedes Datumsfeld der Konditionierung — Saison,
    /// Perioden, Ferien. Gespeichert wird der Jahrestag 1 … 365 im Gemeinjahr; die Umrechnung ist
    /// <see cref="Feiertage.Gemeinjahrestag"/> hin und <see cref="Feiertage.Datum"/> zurück.
    ///
    /// <para><b>Angenommen</b> werden „TT.MM.“, „TT.MM“, „T.M.“, „T.M“ — führende Nullen nach Wahl,
    /// Komma statt Punkt (Zifferntastatur eines Tablets) — und „TT.MM.JJJJ“: Das Jahr wird verworfen
    /// (<c>jahrVerworfen</c>), denn der Kalender kennt kein Jahr. Der 29.02. und jeder unmögliche Tag
    /// (31.04., 00.05., 12.13.) sind kein Datum. <b>Angezeigt</b> wird einheitlich „TT.MM.“.</para>
    ///
    /// <para>Ohne Datenbank, ohne Kultur: Ziffern sind nur 0–9.</para>
    /// </summary>
    public static class Gemeinjahrdatumtext
    {
        /// <summary>Tag und Monat, getrennt durch Punkt oder Komma; danach wahlweise ein Trenner und ein vierstelliges Jahr.</summary>
        private static readonly Regex MUSTER = new(
            @"^\s*(?<tag>[0-9]{1,2})\s*[.,]\s*(?<monat>[0-9]{1,2})\s*(?:[.,]\s*(?<jahr>[0-9]{4})?)?\s*$",
            RegexOptions.CultureInvariant);

        /// <summary>Liest einen Text als Jahrestag 1 … 365 im Gemeinjahr; <c>false</c>, wenn er keiner ist.</summary>
        public static bool Lesen(string text, out int jahrestag) => Lesen(text, out jahrestag, out _);

        /// <summary>
        /// Liest einen Text als Jahrestag 1 … 365 im Gemeinjahr; <paramref name="jahrVerworfen"/> meldet,
        /// dass der Text ein Jahr trug, das nicht zählt. <c>false</c> bei einem Text, der kein Tag im
        /// Gemeinjahr ist — auch beim 29.02.
        /// </summary>
        public static bool Lesen(string text, out int jahrestag, out bool jahrVerworfen)
        {
            jahrestag = 0;
            jahrVerworfen = false;
            Match m = MUSTER.Match(text ?? "");
            if (!m.Success) return false;
            int t = Feiertage.Gemeinjahrestag(int.Parse(m.Groups["monat"].Value, CultureInfo.InvariantCulture),
                                              int.Parse(m.Groups["tag"].Value, CultureInfo.InvariantCulture));
            if (t < 1) return false;
            jahrestag = t;
            jahrVerworfen = m.Groups["jahr"].Success;
            return true;
        }

        /// <summary>Der Anzeigetext eines Jahrestags: „TT.MM.“; leer ohne Wert oder außerhalb 1 … 365.</summary>
        public static string Anzeigetext(int? jahrestag)
        {
            if (jahrestag is not int t || !Feiertage.Datum(t, out int monat, out int tag)) return "";
            return tag.ToString("00", CultureInfo.InvariantCulture) + "."
                 + monat.ToString("00", CultureInfo.InvariantCulture) + ".";
        }
    }
}
