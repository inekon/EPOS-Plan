#nullable enable

namespace EPOS.UI.Dialoge.Bedarf
{
    /// <summary>
    /// Wie das Löschen eines Bedarfs-Katalogsatzes ausgegangen ist (iU9-W14b.1) —
    /// die Sicht der KOMPONENTE auf <c>BedarfStammCtrl.BedarfLoeschErgebnis</c>.
    ///
    /// <para>Warum eine zweite Aufzählung: Der Kerntyp ist <c>internal</c> und liegt
    /// hinter der Assemblygrenze; eine Razor-Komponente sieht ihn nicht. Die Hülle
    /// bildet den einen auf den anderen ab — dieselbe Trennung wie bei
    /// <c>KatalogSpeicherErgebnis</c> aus Welle 6.</para>
    /// </summary>
    public enum BedarfLoeschAusgang
    {
        /// <summary>Der Satz ist weg.</summary>
        Geloescht = 0,

        /// <summary>Auslieferungsbestand (<c>ReadOnly</c>) — er bleibt stehen.</summary>
        Schreibgeschuetzt = 1,

        /// <summary>Das Löschen hat nicht gegriffen.</summary>
        Fehlgeschlagen = 2
    }
}
