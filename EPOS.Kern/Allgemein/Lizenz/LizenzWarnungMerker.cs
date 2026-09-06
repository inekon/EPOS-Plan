using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Tagesmerker der drei Lizenz-Warnstufen</b> — Anwenderentscheid
    /// <b>iF30‑O‑2</b> vom 06.09.2026: „einmal täglich reicht".
    ///
    /// <para><b>Wogegen er steht.</b> Das Konzept (§ 6) verspricht für 30, 14 und 7 Tage
    /// vor dem Ablauf einen „dezenten Hinweis beim Start (<b>einmal täglich</b>)". Gebaut
    /// war bis iF30 „einmal je Programmstart": Wer sein Programm dreimal am Tag öffnet,
    /// sah dreimal dasselbe Banner. Ein Hinweis, den man dreimal am Tag wegsieht, wird
    /// beim vierten Mal nicht mehr gelesen.</para>
    ///
    /// <para><b>Was er NICHT unterdrückt.</b> Den LESEMODUS. Der ist keine Warnstufe,
    /// sondern ein Zustand, den der Anwender beheben MUSS und sonst nicht sieht — sein
    /// Banner bleibt bei jedem Start stehen (Hausregel W16b‑E‑6). Ebenso wenig berührt er
    /// Kulanzfenster und fällige Nachprüfung: Beide haben Warnstufe <c>0</c> und laufen
    /// deshalb gar nicht durch diesen Merker. Die Entscheidung darüber trifft
    /// <see cref="LizenzLage.MitTagesmerker"/>, nicht die Oberfläche.</para>
    ///
    /// <para><b>Warum der Merker MITSCHREIBT.</b> <see cref="SollZeigen"/> ist keine reine
    /// Frage, sondern Frage und Vermerk in einem — das ist Absicht. Zwei getrennte Aufrufe
    /// („darf ich?" … „ich habe") ließen sich auseinanderreißen: Wer das Vermerken
    /// vergäße, hätte den Zustand vor iF30 zurück, ohne dass es auffiele. Der Vermerk
    /// hängt hier an derselben Zeile wie die Antwort.</para>
    ///
    /// <para><b>Eine zweite Ablage ist es nicht.</b> Der Merker liegt in
    /// <see cref="Dienste.Einstellungen"/> — unter Windows in
    /// <c>HKCU\Software\wp-plan</c> neben <c>LizenzAnker</c>, <c>LizenzZugestimmt</c> und
    /// <c>LizenzDatei</c>, auf iOS in den <c>Preferences</c>. Er trägt keinen
    /// Lizenzzustand, nur „an welchem Tag welche Stufe schon zu sehen war", und ist
    /// deshalb kein Angriffsziel: Wer ihn löscht oder verstellt, bekommt den Hinweis
    /// häufiger zu sehen, nie seltener.</para>
    ///
    /// <para><b>Im Zweifel zeigen.</b> Ein unlesbarer, leerer oder fehlender Wert — und
    /// ebenso eine Ablage, die wirft — führt zum Hinweis. Dieselbe Linie wie
    /// <c>Schreibnaht.Lizenzantwort</c> und <c>ZustimmungCtrl</c>: Der Fehlerfall darf
    /// dem Anwender nichts wegnehmen.</para>
    /// </summary>
    public static class LizenzWarnungMerker
    {
        /// <summary>
        /// Der Schlüssel in <see cref="Dienste.Einstellungen"/>.
        /// </summary>
        /// <remarks>
        /// Geschrieben wie die drei Geschwister des Lizenzwegs (<c>LizenzAnker</c>,
        /// <c>LizenzZugestimmt</c>, <c>LizenzDatei</c>): PascalCase, kein Punkt. Ein
        /// Punkt wäre unter Windows ein zulässiger Wertname, führte den Zweig aber als
        /// einzigen Eintrag mit einer zweiten Schreibweise.
        /// </remarks>
        public const string SCHLUESSEL = "LizenzWarnungGezeigt";

        /// <summary>Die Form des Werts: <c>yyyy-MM-dd|stufe</c>, z. B. <c>2026-09-06|14</c>.</summary>
        private const string TAGESFORM = "yyyy-MM-dd";

        /// <summary>Trennt Tag und Stufe im gemerkten Wert.</summary>
        private const char TRENNER = '|';

        /// <summary>
        /// Soll die Warnstufe <paramref name="warnstufe"/> heute gezeigt werden? Ein
        /// „ja" wird zugleich VERMERKT — siehe Klassenkommentar.
        /// </summary>
        /// <param name="warnstufe">
        /// 0, 30, 14 oder 7 (<c>LizenzManager.Warnstufe</c>). <b>Die kleinere Zahl ist die
        /// dringendere Stufe.</b>
        /// </param>
        /// <param name="heute">
        /// Der heutige Tag. Die Uhrzeit wird abgeschnitten; gerechnet wird auf
        /// <see cref="DateTime.Date"/>. <c>LizenzLage.Ermitteln</c> reicht denselben
        /// UTC-Tag herein, mit dem auch <c>LizenzManager.Warnstufe</c> rechnet — zwei
        /// Vorstellungen von „heute" wären eine zu viel.
        /// </param>
        /// <returns>
        /// <c>true</c>, wenn der Hinweis erscheinen soll: Stufe > 0 und heute noch nicht
        /// mit dieser oder einer dringenderen Stufe gezeigt.
        /// </returns>
        public static bool SollZeigen(int warnstufe, DateTime heute)
        {
            // Keine Stufe, kein Hinweis - und kein Vermerk. Der Regelfall einer
            // gueltigen Lizenz darf in der Ablage keine Spur hinterlassen.
            if (warnstufe <= 0) return false;

            DateTime tag = heute.Date;

            DateTime gemerkterTag;
            int gemerkteStufe;
            if (Gemerkt(out gemerkterTag, out gemerkteStufe)
                && gemerkterTag == tag
                && warnstufe >= gemerkteStufe)
            {
                // Heute schon gezeigt, und zwar mit derselben oder einer dringenderen
                // Stufe. Erst der Sprung 30 -> 14 -> 7 ist eine NEUE Nachricht.
                return false;
            }

            Merken(tag, warnstufe);
            return true;
        }

        /// <summary>
        /// Liest den Vermerk. <c>false</c>, wenn nichts hinterlegt, der Wert unlesbar ist
        /// oder die Ablage wirft — dann wird gezeigt.
        /// </summary>
        private static bool Gemerkt(out DateTime tag, out int stufe)
        {
            tag = DateTime.MinValue;
            stufe = 0;

            string wert;
            try { wert = Dienste.Einstellungen.Lies(SCHLUESSEL, null); }
            catch (Exception) { return false; }

            if (string.IsNullOrWhiteSpace(wert)) return false;

            int trenner = wert.IndexOf(TRENNER);
            if (trenner <= 0 || trenner == wert.Length - 1) return false;

            if (!DateTime.TryParseExact(wert.Substring(0, trenner), TAGESFORM,
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out tag))
                return false;

            if (!int.TryParse(wert.Substring(trenner + 1), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out stufe))
                return false;

            // Eine Stufe <= 0 waere kein Vermerk, sondern Unsinn in der Ablage.
            return stufe > 0;
        }

        /// <summary>
        /// Schreibt den Vermerk fort. Ein Fehlschlag bleibt folgenlos: Dann erscheint der
        /// Hinweis beim nächsten Start erneut, und das ist die harmlose Seite.
        /// </summary>
        private static void Merken(DateTime tag, int stufe)
        {
            try
            {
                Dienste.Einstellungen.Schreib(
                    SCHLUESSEL,
                    tag.ToString(TAGESFORM, CultureInfo.InvariantCulture)
                        + TRENNER
                        + stufe.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                // Absichtlich still - siehe Klassenkommentar „Im Zweifel zeigen".
            }
        }
    }
}
