using System;
using System.IO;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Der Schreibschutz des Repositorys.
    ///
    /// <para><b>Warum es ihn gibt.</b> Die erzeugte Vorlage ist eine Datenbank mit
    /// Katalogen und Beispielprojekten — je nach Quelle mehrere zehn Megabyte. Landet sie
    /// versehentlich im Arbeitsbaum, ist der naechste <c>git add</c> die Gelegenheit, sie
    /// einzuchecken; <c>.gitignore</c> deckt <c>*.sqlite</c> NICHT pauschal ab (die
    /// Referenzdatenbank steht bewusst im Repository). Deshalb weigert sich das Werkzeug:
    /// Innerhalb des Repositorys ist genau ein Ziel erlaubt, <c>Setup/Vorlage/</c> — der
    /// Ort, den <c>Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md</c> 6.1 nennt und den
    /// <c>.gitignore</c> ausnimmt.</para>
    ///
    /// <para><b>Die Wurzel ist da, wo <c>WP-Plan.sln</c> liegt</b> — dieselbe Erkennung
    /// wie in <c>Werkzeuge/Formularkarte.Tests/Repowurzel.cs</c>. Liegt das Ziel gar nicht
    /// unter einer solchen Wurzel, ist es ausserhalb des Repositorys und damit frei.</para>
    /// </summary>
    internal static class Schreibort
    {
        private const string ERLAUBT = "Setup/Vorlage";

        /// <summary>
        /// Prueft, ob nach <paramref name="ziel"/> geschrieben werden darf.
        /// Rueckgabe <c>null</c> = erlaubt, sonst der Grund der Weigerung.
        /// </summary>
        internal static string Pruefen(string ziel)
        {
            string ordner = Path.GetDirectoryName(Path.GetFullPath(ziel));
            if (string.IsNullOrEmpty(ordner)) return "Das Ziel hat keinen Ordner.";

            string wurzel = Wurzel(ordner);
            if (wurzel == null) return null;   // ausserhalb eines Repositorys — frei

            string relativ = Path.GetRelativePath(wurzel, ordner).Replace(Path.DirectorySeparatorChar, '/');
            if (relativ == ERLAUBT || relativ.StartsWith(ERLAUBT + "/", StringComparison.Ordinal)) return null;

            return "Das Ziel liegt im Repository (" + wurzel + "), aber nicht unter " + ERLAUBT + "/: " + ziel +
                   ". Eine Auslieferungsdatenbank gehoert nie in den Arbeitsbaum — sie enthaelt Kataloge und " +
                   "Beispieldaten und waere der naechste versehentliche Commit.";
        }

        /// <summary>Die Repowurzel oberhalb eines Ordners, erkennbar an <c>WP-Plan.sln</c>; sonst <c>null</c>.</summary>
        internal static string Wurzel(string ordner)
        {
            var d = new DirectoryInfo(ordner);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }
            return null;
        }
    }
}
