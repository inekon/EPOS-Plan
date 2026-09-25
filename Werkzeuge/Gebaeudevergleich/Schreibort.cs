using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gebaeudevergleich
{
    /// <summary>
    /// Der Schreibschutz des Werkzeugs — nach dem Muster der Auslieferungsvorlage, aber
    /// <b>ohne</b> Ausnahme.
    ///
    /// <para><b>Warum ohne Ausnahme.</b> Die Ausgabe dieses Werkzeugs sind Kennzahlen der
    /// Kundenprojekte, und mit <c>--mit-namen</c> stehen die Namen darin; eine Momentaufnahme
    /// ist eine ganze Kundendatenbank. Nichts davon gehört in den Arbeitsbaum, in dem der
    /// nächste <c>GitHub_Sync.bat</c> alles einsammelt. Und nichts davon gehört neben die
    /// Produktivdatenbank unter <c>%ProgramData%\EPOS_PLAN</c>, in der das Programm lebt.</para>
    ///
    /// <para>Verweigert werden: jedes Ziel unter einer Wurzel mit <c>WP-Plan.sln</c>, jedes Ziel
    /// unter <c>%ProgramData%\EPOS_PLAN</c> und bei <c>vergleich</c> und <c>variante</c> eine
    /// <c>--db</c> unter <c>%ProgramData%\EPOS_PLAN</c>: Gerechnet wird nur auf einer
    /// Momentaufnahme, nie auf der Datei, mit der das Programm arbeitet.</para>
    /// </summary>
    internal static class Schreibort
    {
        /// <summary>
        /// Der Ordner der Produktivdatenbank — derselbe Rückfall wie in
        /// <c>DataRepository.GetDBPath</c> (<c>CommonApplicationData\EPOS_PLAN</c>).
        /// </summary>
        internal static string Produktivordner
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EPOS_PLAN");

        /// <summary>Rückgabe <c>null</c> = erlaubt, sonst der Grund der Weigerung.</summary>
        internal static string Pruefen(Argumente a)
        {
            string ordner = a.Zielordner;
            if (string.IsNullOrEmpty(ordner)) return "Das Ziel hat keinen Ordner: " + a.Ziel;

            string wurzel = Wurzel(ordner);
            if (wurzel != null)
                return "Das Ziel liegt im Repository (" + wurzel + "): " + a.Ziel + ". Die Ausgabe enthält " +
                       "Kennzahlen der Kundenprojekte und gehört nie in den Arbeitsbaum - ein Ziel unter %TEMP% nehmen.";

            if (IstUnter(ordner, Produktivordner) || IstUnter(a.Ziel, Produktivordner))
                return "Das Ziel liegt unter " + Produktivordner + ": " + a.Ziel + ". Neben der Datenbank des " +
                       "Programms entsteht keine Datei.";

            if (a.Befehl == Argumente.VERGLEICH || a.Befehl == Argumente.VARIANTE)
            {
                if (IstUnter(a.Db, Produktivordner))
                    return "Die --db liegt unter " + Produktivordner + ": " + a.Db + ". Gerechnet wird nur auf " +
                           "einer Momentaufnahme - zuerst 'aufnahme --quelle " + a.Db + " --ziel <ordner>'.";
            }

            if (a.Befehl == Argumente.VARIANTE)
            {
                if (Gleich(a.Ziel, a.Db)) return "Quelle und Ziel der Variante sind dieselbe Datei.";
                if (File.Exists(a.Ziel) || Directory.Exists(a.Ziel))
                    return "Das Ziel der Variante gibt es schon: " + a.Ziel + ". Es wird nicht überschrieben.";
            }

            if (a.Befehl == Argumente.AUFNAHME && Gleich(a.Quelle, Path.Combine(a.Ziel, Argumente.AUFNAHMEDATEI)))
                return "Quelle und Momentaufnahme sind dieselbe Datei.";

            return null;
        }

        /// <summary>Die Repowurzel oberhalb eines Ordners, erkennbar an <c>WP-Plan.sln</c>; sonst <c>null</c>.</summary>
        internal static string Wurzel(string ordner)
        {
            var d = new DirectoryInfo(Path.GetFullPath(ordner));
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }
            return null;
        }

        /// <summary>Liegt <paramref name="pfad"/> in <paramref name="ordner"/> oder ist er es selbst?</summary>
        internal static bool IstUnter(string pfad, string ordner)
        {
            if (string.IsNullOrEmpty(pfad) || string.IsNullOrEmpty(ordner)) return false;
            string p = Normal(pfad);
            string o = Normal(ordner);
            return p.Equals(o, Vergleich) || p.StartsWith(o + Path.DirectorySeparatorChar, Vergleich);
        }

        internal static bool Gleich(string a, string b)
            => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && Normal(a).Equals(Normal(b), Vergleich);

        private static string Normal(string pfad)
            => Path.GetFullPath(pfad).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        /// <summary>Windows vergleicht Pfade ohne Groß-/Kleinschreibung, Linux mit.</summary>
        private static StringComparison Vergleich
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }
}
