using EPOS.Referenzlaeufe.Skripte;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Was das Referenzprojekt 1051 „Referenzprojekt Konditionierung" in die Testdatenbank bringt</b>
    /// (Entwurf KP3, Welle RP1; Befund B24) — die Ausnahmen der Zählwachen, die vor 1051 „alles leer"
    /// oder „alles aus" hielten: das Gebäude der Projektkopie (Kalender aller fünf Größen, Vorgaben,
    /// Schalter <c>Kuehlung_Aktiv</c> und <c>Sommerlueftung</c>, Nachtzeit, Ferien) und der eigene
    /// Referenzkatalogbau, aus dem es übernommen ist (<see cref="Konditionierungsprojekt1051.REFERENZBAU"/>).
    /// Muster: <see cref="Zonenbestand"/> (1052, G6d).
    /// </summary>
    internal static class Konditionierungsbestand
    {
        /// <summary>Das Gebäude des Referenzprojekts 1051.</summary>
        internal const string GEBAEUDE_1051 = "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = 1051";

        /// <summary>Der Referenzkatalogbau von 1051.</summary>
        internal const string KATALOGBAU_1051 =
            "SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner = '" + Konditionierungsprojekt1051.REFERENZBAU + "'";

        /// <summary>Die Bedingung „kein Eigentum von 1051" an Kalender- und Vorgabezeilen (<c>ID_Gebaeude</c>, <c>ID_Gebaeude_Stamm</c>).</summary>
        internal const string NICHT_1051 = "(ID_Gebaeude IS NULL OR ID_Gebaeude NOT IN (" + GEBAEUDE_1051 + ")) AND " +
                                           "(ID_Gebaeude_Stamm IS NULL OR ID_Gebaeude_Stamm NOT IN (" + KATALOGBAU_1051 + "))";

        /// <summary>Die Kalender von 1051 (Gebäude und Referenzkatalogbau).</summary>
        internal const string KALENDER_1051 = "SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEBAEUDE_1051 +
                                              ") OR ID_Gebaeude_Stamm IN (" + KATALOGBAU_1051 + ")";

        /// <summary>
        /// Die Zeilen von 1051 in einer Gebäudetabelle als Ausnahme einer Zählung (<c>" AND ID NOT IN (…)"</c>):
        /// in <c>Tab_Gebaeude</c> das Gebäude, in <c>Tab_Gebaeude_STAMM</c> der Referenzkatalogbau, sonst nichts.
        /// </summary>
        internal static string Ausser(string tabelle)
            => tabelle == "Tab_Gebaeude" ? " AND ID NOT IN (" + GEBAEUDE_1051 + ")"
               : tabelle == "Tab_Gebaeude_STAMM" ? " AND ID NOT IN (" + KATALOGBAU_1051 + ")"
               : "";
    }
}
