using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Baujahr aus Text und Baualtersklasse aus Baujahr</b> (Umsetzungskonzept 3.4, Zeilen
    /// „Baualtersklasse" und „Baujahr"; Entscheid E47) — formatfrei, ohne Datenbank.
    ///
    /// <para><b>Das Baujahr ist Text:</b> <c>Pset_BuildingCommon.YearOfConstruction</c> ist ein
    /// <c>IfcLabel</c>. Gelesen wird die ERSTE vierstellige Zahl im Bereich 1500…2100, die nicht Teil
    /// einer längeren Ziffernfolge ist („ca. 1965" → 1965, „erbaut 1965/66" → 1965, „Altbau" → keins,
    /// „19650" → keins).</para>
    ///
    /// <para><b>Die Klasse folgt dem Jahr</b> — für JEDES Jahr 1500…2100 (Konzept Baualtersklassen 3.1):
    /// A bis 1859, B 1860–1918, C 1919–1948, D 1949–1957, E 1958–1968, F 1969–1978, G 1979–1983,
    /// H 1984–1994, I 1995–2001, J 2002–2009, K 2010–2015, L 2016–2020, M ab 2021
    /// (<see cref="GebaeudeStammCtrl.BAUALTERSKLASSEN_DE"/>, Indizes 0…12). Das Baujahr FÜHRT: Ist es
    /// gesetzt, gilt diese Klasse im Katalogeditor, in der Gebäudeverwaltung und im Import.</para>
    /// </summary>
    public static class Baujahrregel
    {
        /// <summary>Kleinstes Jahr, das als Baujahr gilt — dieselbe Grenze wie die Spalte (<see cref="GebaeudeSchema.BAUJAHR_MIN"/>).</summary>
        public const int JAHR_MIN = GebaeudeSchema.BAUJAHR_MIN;

        /// <summary>Größtes Jahr, das als Baujahr gilt — dieselbe Grenze wie die Spalte (<see cref="GebaeudeSchema.BAUJAHR_MAX"/>).</summary>
        public const int JAHR_MAX = GebaeudeSchema.BAUJAHR_MAX;

        /// <summary>
        /// Obergrenzen der Klassen A…L (einschließlich); M reicht bis <see cref="JAHR_MAX"/>. Die eine
        /// Stelle der Jahresgrenzen.
        /// </summary>
        public static readonly IReadOnlyList<int> OBERGRENZEN =
            new[] { 1859, 1918, 1948, 1957, 1968, 1978, 1983, 1994, 2001, 2009, 2015, 2020 };

        /// <summary>Die erste vierstellige Jahreszahl 1500…2100 im Text; <c>null</c> = keine.</summary>
        public static int? Jahr(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int i = 0;
            while (i < text.Length)
            {
                if (!IstZiffer(text[i])) { i++; continue; }
                int anfang = i;
                while (i < text.Length && IstZiffer(text[i])) i++;
                if (i - anfang != 4) continue;   // nur eine Folge aus genau vier Ziffern
                int jahr = int.Parse(text.Substring(anfang, 4), NumberStyles.None, CultureInfo.InvariantCulture);
                if (jahr >= JAHR_MIN && jahr <= JAHR_MAX) return jahr;
            }
            return null;
        }

        /// <summary>Die Baualtersklasse A…M zu einem Baujahr; <c>null</c> außerhalb 1500…2100.</summary>
        public static char? Klasse(int jahr)
        {
            if (jahr < JAHR_MIN || jahr > JAHR_MAX) return null;
            for (int k = 0; k < OBERGRENZEN.Count; k++)
                if (jahr <= OBERGRENZEN[k]) return (char)('A' + k);
            return (char)('A' + OBERGRENZEN.Count);
        }

        /// <summary>Der Listenindex der Klasse zu einem Baujahr (0 = A … 12 = M); <c>null</c> ohne Jahr oder außerhalb 1500…2100.</summary>
        public static int? KlassenIndex(int? jahr)
        {
            char? k = jahr is int j ? Klasse(j) : null;
            return k.HasValue ? k.Value - 'A' : (int?)null;
        }

        private static bool IstZiffer(char c) => c >= '0' && c <= '9';
    }
}
