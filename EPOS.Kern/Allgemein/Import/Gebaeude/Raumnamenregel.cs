using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Namensregel „unbeheizt"</b> — ein Raum ohne Zustandsangabe gilt als unbeheizt, wenn
    /// sein Name eines der Muster enthält (Umsetzungskonzept 3.5 Nr. 3; für gbXML
    /// Datenaustauschkonzept 3.3: „dieselbe Musterliste wie beim IFC-Weg"). Groß- und
    /// Kleinschreibung zählen nicht. Die Regel ist eine Vorbelegung: Der Dialog zeigt die
    /// Raumliste mit dem Haken, damit sie sichtbar und korrigierbar bleibt.
    /// </summary>
    public static class Raumnamenregel
    {
        private static readonly string[] _muster =
        {
            "Keller", "Garage", "Carport", "Dachboden", "Speicher", "Abstellraum", "Technik", "Schacht", "Aufzug",
            "Basement", "Attic", "Shaft", "Plant",
        };

        /// <summary>Die Muster in fester Reihenfolge (deutsch, dann englisch; „Garage" gilt in beiden).</summary>
        public static IReadOnlyList<string> Muster => _muster;

        /// <summary>Das erste Muster, das im Namen steht; <c>null</c> = keines.</summary>
        public static string Treffer(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (string m in _muster)
                if (name.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) return m;
            return null;
        }

        /// <summary>Deutet der Name auf einen unbeheizten Raum?</summary>
        public static bool IstUnbeheizt(string name) => Treffer(name) != null;
    }
}
