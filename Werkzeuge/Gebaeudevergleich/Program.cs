using System;
using System.Text;

namespace Gebaeudevergleich
{
    /// <summary>
    /// Der Einstieg des Werkzeugs: Kommandozeile lesen, Schreibort prüfen, Unterbefehl
    /// ausführen.
    ///
    /// <para><b>Rückgabe</b> (für alle Unterbefehle gleich): <see cref="OHNE_ROT"/> = keine rote
    /// Zeile, <see cref="ROT"/> = mindestens eine rote Zeile (nur <c>vergleich</c>),
    /// <see cref="ABBRUCH"/> = Abbruch mit Grund. Ein Abbruch vor dem Anlegen des Zielordners
    /// (falscher Aufruf, verweigerter Schreibort) legt nichts an.</para>
    /// </summary>
    internal static class Program
    {
        internal const int OHNE_ROT = 0;
        internal const int ROT = 1;
        internal const int ABBRUCH = 2;

        private static int Main(string[] args)
        {
            // UTF-8 ohne Vorspann auf der Konsole - wie in Werkzeuge/Auslieferungsvorlage.
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            if (args.Length == 0 || args[0] == "--hilfe" || args[0] == "-h" || args[0] == "--help")
            {
                Argumente.HilfeAusgeben(Console.Out);
                return ABBRUCH;
            }

            Argumente arg = Argumente.Lesen(args);
            if (arg.Fehler != null)
            {
                Console.Error.WriteLine("Abbruch: " + arg.Fehler);
                Console.Error.WriteLine("Aufrufhilfe: --hilfe");
                Console.WriteLine("Exitcode " + ABBRUCH);
                return ABBRUCH;
            }

            // Der Schreibort wird VOR dem Anlegen des Zielordners geprüft - ein verweigertes
            // Ziel darf nicht einmal ein leeres protokoll.txt bekommen (T7).
            // Erst danach wird die Eingabedatei überhaupt angesehen: Eine --db unter
            // %ProgramData%\EPOS_PLAN wird verweigert, ohne sie zu öffnen.
            string verweigert = Schreibort.Pruefen(arg) ?? arg.DateienPruefen();
            if (verweigert != null)
            {
                Console.Error.WriteLine("Abbruch: " + verweigert);
                Console.WriteLine("Exitcode " + ABBRUCH);
                return ABBRUCH;
            }

            return Einstieg.Ausfuehren(arg);
        }
    }
}
