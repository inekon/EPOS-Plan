using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die feste Tabelle Profilnummer → Nutzung der Zone</b> (Datenaustauschkonzept 16.3): die DIN-V-18599-10-Nummer aus
    /// <c>PdProfileUsage.ProfileUsageType</c> auf eine der Nutzungen des Zonenplans (<see cref="Zonenplan.NUTZUNGEN"/>).
    /// Büroprofile → BUERO, Schulprofile → SCHULE, Wohnprofile → WOHNEN, jede andere Nummer → keine Nutzung (die Nummer
    /// steht im Beleg). Sprachneutral; ausgewiesen mit Nummer und Normname in
    /// <c>Referenzlaeufe/Importproben/LIESMICH_Importproben.md</c>.
    /// <para><b>Zählung:</b> Die Schlüssel sind Nummern der Projektdatei und zählen nach DIN V 18599-10:2018-09 (28, 29
    /// Bibliothek). Die Kategorie DIN des Katalogs führt die DIN/TS 18599-10:2025-10, die ab 22 neu nummeriert (dort 30, 31);
    /// ob HottCAD wie 2018 oder wie 2025 zählt, belegt keine Datei im Repositorium (Konzept Nutzungsprofile 5.4, E94).</para>
    /// <para><b>Vorgabe im Code</b> (Konzept Nutzungsprofile NP-F12): Vorrang hat die Zuordnung <c>DIN_NUMMER</c> in
    /// <c>Tab_Raumnutzungszuordnung</c>; diese Tabelle gilt nur, wenn dort keine Zeile steht oder kein Katalog vorliegt
    /// (<see cref="Raumnutzungsvorbelegung"/>).</para>
    /// </summary>
    internal static class Din18599Nutzung
    {
        private static readonly IReadOnlyDictionary<int, string> TABELLE = new SortedDictionary<int, string>
        {
            // Büro: Einzelbüro, Gruppenbüro, Großraumbüro, Besprechung/Sitzung/Seminar, Schalterhalle
            [1] = DbWerte.KOND_NUTZUNG_BUERO,
            [2] = DbWerte.KOND_NUTZUNG_BUERO,
            [3] = DbWerte.KOND_NUTZUNG_BUERO,
            [4] = DbWerte.KOND_NUTZUNG_BUERO,
            [5] = DbWerte.KOND_NUTZUNG_BUERO,
            // Schule: Klassenzimmer, Hörsaal/Auditorium, Bibliothek Lesesaal, Bibliothek Freihandbereich
            [8] = DbWerte.KOND_NUTZUNG_SCHULE,
            [9] = DbWerte.KOND_NUTZUNG_SCHULE,
            [28] = DbWerte.KOND_NUTZUNG_SCHULE,
            [29] = DbWerte.KOND_NUTZUNG_SCHULE,
            // Wohnen: die Wohnzeilen der Projektdatei (Einfamilienhaus, Mehrfamilienhaus)
            [70] = DbWerte.KOND_NUTZUNG_WOHNEN,
            [71] = DbWerte.KOND_NUTZUNG_WOHNEN,
        };

        /// <summary>Die Nutzung einer Profilnummer; <c>null</c> = keine (auch ohne Nummer).</summary>
        internal static string Nutzung(int? profilnummer)
            => profilnummer is int n && TABELLE.TryGetValue(n, out string nutzung) ? nutzung : null;

        /// <summary>Die Tabelle in Nummernfolge — für Papiere und Wachen.</summary>
        internal static IReadOnlyDictionary<int, string> Tabelle => TABELLE;
    }
}
