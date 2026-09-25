using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Baujahr aus Text und Baualtersklasse aus Baujahr</b> (Umsetzungskonzept 3.4, Zeilen
    /// „Baualtersklasse" und „Baujahr") — formatfrei, ohne Datenbank.
    ///
    /// <para><b>Das Baujahr ist Text:</b> <c>Pset_BuildingCommon.YearOfConstruction</c> ist ein
    /// <c>IfcLabel</c>. Gelesen wird die ERSTE vierstellige Zahl im Bereich 1500…2100, die nicht Teil
    /// einer längeren Ziffernfolge ist („ca. 1965" → 1965, „erbaut 1965/66" → 1965, „Altbau" → keins,
    /// „19650" → keins).</para>
    ///
    /// <para><b>Die Klasse folgt dem Jahr</b> nach den acht Jahresklassen des Gebäudekatalogs
    /// (<see cref="GebaeudeStammCtrl.BAUALTERSKLASSEN_DE"/>, Indizes 0…7): A vor 1919, B 1919–1948,
    /// C 1949–1957, D 1958–1968, E 1969–1978, F 1979–1983, G 1984–1994, H 1995–2000. Ab 2001 ist die
    /// Klasse ein Standard (I = Niedrigenergie … U = BEG 40), kein Jahr — dann gibt es keine
    /// abgeleitete Klasse, der Anwender wählt.</para>
    /// </summary>
    internal static class Baujahrregel
    {
        /// <summary>Kleinstes Jahr, das als Baujahr gilt — dieselbe Grenze wie die Spalte (<see cref="GebaeudeSchema.BAUJAHR_MIN"/>).</summary>
        public const int JAHR_MIN = GebaeudeSchema.BAUJAHR_MIN;

        /// <summary>Größtes Jahr, das als Baujahr gilt — dieselbe Grenze wie die Spalte (<see cref="GebaeudeSchema.BAUJAHR_MAX"/>).</summary>
        public const int JAHR_MAX = GebaeudeSchema.BAUJAHR_MAX;

        /// <summary>Letztes Jahr, für das eine Jahresklasse (A…H) gilt.</summary>
        public const int LETZTES_KLASSENJAHR = 2000;

        // Obergrenzen der Jahresklassen A…H (einschließlich); A reicht bis 1918.
        private static readonly int[] _obergrenzen = { 1918, 1948, 1957, 1968, 1978, 1983, 1994, 2000 };

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

        /// <summary>Die Baualtersklasse A…H zu einem Baujahr; <c>null</c> ab 2001 und außerhalb 1500…2100.</summary>
        public static char? Klasse(int jahr)
        {
            if (jahr < JAHR_MIN || jahr > LETZTES_KLASSENJAHR) return null;
            for (int k = 0; k < _obergrenzen.Length; k++)
                if (jahr <= _obergrenzen[k]) return (char)('A' + k);
            return null;
        }

        private static bool IstZiffer(char c) => c >= '0' && c <= '9';
    }
}
