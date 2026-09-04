using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zustimmung zur Lizenzvereinbarung beim ersten Start (iU9-W15c.9).
    ///
    /// <para>Einmal je Windows-Benutzer, gemerkt als
    /// <c>„&lt;Programmversion&gt; | yyyy-MM-dd HH:mm"</c> unter dem Namen
    /// <see cref="EINSTELLUNG"/>. Bis W15c stand das als vier Registry-Zugriffe in
    /// <c>Form_Lizenz</c> (<c>HKCU\Software\wp-plan\LizenzZugestimmt</c>); über
    /// <c>Dienste.Einstellungen</c> ist es unter Windows <b>derselbe Zweig</b> —
    /// <c>SettingsEinstellungen</c> schreibt ausschließlich in
    /// <c>RegistryEinstellungen</c>, und dessen Standardpfad ist
    /// <c>Software\wp-plan</c> (Befund W15c-B17).</para>
    ///
    /// <para><b>Der Fehlerpfad ist eine Fachentscheidung, kein Zufall</b> (Entscheid
    /// E-15, Befund W15c-B18): Eine nicht lesbare Ablage blockiert den Start NICHT.
    /// Der Kommentar des Bestands lautet wörtlich „im Zweifel den Start nicht
    /// blockieren" — und dabei bleibt es.</para>
    /// </summary>
    internal static class ZustimmungCtrl
    {
        /// <summary>Der Name des Eintrags; unter Windows der Registry-Wert.</summary>
        internal const string EINSTELLUNG = "LizenzZugestimmt";

        /// <summary>
        /// Wurde der Lizenzvereinbarung bereits zugestimmt?
        /// </summary>
        /// <remarks>
        /// <b>Bei einer Ausnahme lautet die Antwort <c>true</c></b> — nicht
        /// <c>false</c>. Das ist absichtlich und wortgleich zum Bestand
        /// (<c>Form_Lizenz.ZustimmungSicherstellen</c>, <c>:999</c>): Eine nicht
        /// lesbare Registry darf den Anwender nicht aus seinem Programm aussperren.
        /// </remarks>
        internal static bool IstZugestimmt()
        {
            try
            {
                string wert = Dienste.Einstellungen.Lies(EINSTELLUNG);
                return !string.IsNullOrEmpty(wert);
            }
            catch
            {
                return true;   // im Zweifel den Start nicht blockieren
            }
        }

        /// <summary>
        /// Merkt die erteilte Zustimmung samt Programmfassung und Zeitpunkt.
        /// Ein Fehlschlag bleibt folgenlos — auch das ist der Bestand
        /// (<c>ZustimmungMerken</c>, <c>:965-978</c>: der ganze Rumpf steht in einem
        /// <c>try</c> mit leerem <c>catch</c>).
        /// </summary>
        /// <param name="version">Die Programmfassung, von der Hülle geliefert.</param>
        /// <param name="zeitpunkt">Der Zeitpunkt der Zustimmung.</param>
        internal static void Merken(string version, DateTime zeitpunkt)
        {
            try
            {
                Dienste.Einstellungen.Schreib(EINSTELLUNG, Vermerk(version, zeitpunkt));
            }
            catch { }
        }

        /// <summary>
        /// Der gemerkte Vermerk als Zeichenkette — <c>„1.1.0.0 | 2026-09-04 10:15"</c>.
        /// Das Format ist eingefroren: Es steht in der Registry von
        /// Bestandsrechnern.
        /// </summary>
        internal static string Vermerk(string version, DateTime zeitpunkt)
        {
            return (version ?? "") + " | " +
                   zeitpunkt.ToString("yyyy-MM-dd HH:mm",
                                      System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
