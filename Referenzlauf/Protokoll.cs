using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Sammelt alle Ausgaben des Laufs: sofort auf die Konsole, gleichzeitig gepuffert
    /// fuer lauf_protokoll.md.
    /// </summary>
    internal sealed class Protokoll
    {
        private readonly List<string> _zeilen = new List<string>();

        public int Warnungen { get; private set; }
        public int Fehler { get; private set; }

        public void Zeile(string text)
        {
            string s = Zeitstempel() + " " + text;
            Console.WriteLine(s);
            _zeilen.Add(text);
        }

        public void Warnung(string text)
        {
            Warnungen++;
            Zeile("WARNUNG: " + text);
        }

        public void FehlerZeile(string text)
        {
            Fehler++;
            Zeile("FEHLER: " + text);
        }

        /// <summary>Rohzeile ohne Zeitstempel - fuer Markdown-Bloecke im Protokoll.</summary>
        public void Roh(string text)
        {
            Console.WriteLine(text);
            _zeilen.Add(text);
        }

        /// <summary>
        /// Uebernimmt eine Ausgabezeile eines Kindprozesses und zaehlt darin gemeldete
        /// Warnungen und Fehler mit - sonst wuerde das Protokoll des Elternprozesses
        /// faelschlich "0 Warnungen" ausweisen.
        ///
        /// WORTWAHL (Nacharbeit Paket 8, Befund N13b): Der Protokollkanal der Engine
        /// (SimulationProtokoll) schreibt "Simulation Warnung: ...", "Simulation Hinweis:
        /// ..." und "Simulation FEHLER: ...". Die Fehlerzeile traf der bisherige,
        /// gross geschriebene Vergleich noch, die WARNUNG nicht - ein Lauf mit
        /// Ersatzannahme wies dadurch weiter "0 Warnungen" aus. Deshalb beide
        /// Schreibweisen. Hinweise werden bewusst NICHT gezaehlt: Sie melden einen
        /// vollwertig gerechneten Grenzfall (z. B. die extrapolierte WP-Kennlinie), und
        /// den gab es in jedem bisherigen Referenzlauf.
        ///
        /// Eingefrorene Referenzbasen bleiben davon unberuehrt - gezaehlt wird beim
        /// Lesen der Kindprozessausgabe, nicht beim Vergleich.
        /// </summary>
        public void AusKindprozess(string text)
        {
            if (text != null)
            {
                if (text.Contains("WARNUNG:") || text.Contains("Simulation Warnung:")) Warnungen++;
                else if (text.Contains("FEHLER:") || text.StartsWith("stderr: ")) Fehler++;
            }
            Roh("      | " + text);
        }

        public void Leerzeile()
        {
            Console.WriteLine();
            _zeilen.Add("");
        }

        private static string Zeitstempel()
        {
            return "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "]";
        }

        /// <summary>Schreibt das gesammelte Protokoll als Markdown (UTF-8 mit BOM).</summary>
        public void Speichern(string datei, string titel, IEnumerable<string> kopfzeilen)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + titel);
            sb.AppendLine();
            if (kopfzeilen != null)
            {
                foreach (string k in kopfzeilen) sb.AppendLine(k);
                sb.AppendLine();
            }
            sb.AppendLine("## Ablauf");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (string z in _zeilen) sb.AppendLine(z);
            sb.AppendLine("```");

            Directory.CreateDirectory(Path.GetDirectoryName(datei));
            File.WriteAllText(datei, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
