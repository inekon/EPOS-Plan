using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Notbremse fuer den headless-Lauf.
    ///
    /// Engine und DataRepository zeigen im Fehlerpfad an ueber acht Stellen MessageBoxen
    /// (Konzept Kapitel 13.4). Ohne Bedienung blockiert so eine Box den Lauf bis zum
    /// Timeout. Der Waechter laeuft in einem Hintergrundthread und sucht Dialogfenster
    /// (Fensterklasse "#32770") des EIGENEN Prozesses.
    ///
    /// Er drueckt dabei BEWUSST den bejahenden Knopf (Ja &gt; OK &gt; Ignorieren) und nicht
    /// einfach WM_CLOSE: Die haeufigste Rueckfrage der Engine lautet "Temperatur
    /// unterschreitet Kennlinien-Untergrenze, soll extrapoliert werden? Bei nein wird
    /// Simulation abgebrochen!". Ein WM_CLOSE auf eine Ja/Nein-Box liefert ein undefiniertes
    /// Ergebnis; der Referenzlauf muss aber genau den Weg gehen, den ein Anwender geht.
    ///
    /// Der App-Code bleibt unangetastet. Jeder Dialog wird mit Titel, Text und gedruecktem
    /// Knopf protokolliert, damit im Bericht steht, welcher Pfad angeschlagen hat.
    /// </summary>
    internal sealed class DialogWaechter : IDisposable
    {
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_COMMAND = 0x0111;
        private const string DIALOG_KLASSE = "#32770";

        // Standard-Steuerelement-IDs der Win32-MessageBox, in der Reihenfolge,
        // in der sie gedrueckt werden sollen: erst "weitermachen", dann "abbrechen".
        private static readonly int[] KnopfReihenfolge = { 6 /*Ja*/, 1 /*OK*/, 5 /*Ignorieren*/,
                                                           7 /*Nein*/, 2 /*Abbrechen*/, 3 /*Abbruch*/ };

        private static string KnopfName(int id)
        {
            switch (id)
            {
                case 1: return "OK";
                case 2: return "Abbrechen";
                case 3: return "Abbruch";
                case 4: return "Wiederholen";
                case 5: return "Ignorieren";
                case 6: return "Ja";
                case 7: return "Nein";
                default: return "ID" + id;
            }
        }

        private readonly Thread _thread;
        private readonly List<string> _geschlossen = new List<string>();
        private readonly object _sperre = new object();

        /// <summary>Zuletzt behandelte Dialogfenster - verhindert Doppeleintraege im Protokoll,
        /// solange ein Dialog zwischen zwei Durchlaeufen noch nicht verschwunden ist.</summary>
        private readonly Dictionary<IntPtr, DateTime> _behandelt = new Dictionary<IntPtr, DateTime>();

        private volatile bool _laeuft = true;

        public DialogWaechter()
        {
            _thread = new Thread(Schleife);
            _thread.IsBackground = true;
            _thread.Start();
        }

        /// <summary>Titel/Text aller weggeklickten Dialoge.</summary>
        public string[] GeschlosseneDialoge
        {
            get { lock (_sperre) { return _geschlossen.ToArray(); } }
        }

        private void Schleife()
        {
            uint eigenePid = (uint)Process.GetCurrentProcess().Id;
            while (_laeuft)
            {
                try { DialogeSchliessen(eigenePid); }
                catch { /* Der Waechter darf den Lauf niemals abbrechen. */ }
                Thread.Sleep(400);
            }
        }

        private void DialogeSchliessen(uint eigenePid)
        {
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid != eigenePid) return true;

                var klasse = new StringBuilder(64);
                GetClassName(hWnd, klasse, klasse.Capacity);
                if (klasse.ToString() != DIALOG_KLASSE) return true;

                // Denselben Dialog nicht in jedem Durchlauf erneut melden.
                DateTime zuletzt;
                if (_behandelt.TryGetValue(hWnd, out zuletzt) &&
                    (DateTime.UtcNow - zuletzt) < TimeSpan.FromSeconds(3))
                    return true;
                _behandelt[hWnd] = DateTime.UtcNow;

                var titel = new StringBuilder(512);
                GetWindowText(hWnd, titel, titel.Capacity);
                string text = DialogtextLesen(hWnd);
                string knopf = BejahendDruecken(hWnd);

                lock (_sperre)
                {
                    _geschlossen.Add("Titel='" + titel + "' Antwort='" + knopf + "' Text='" + text + "'");
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Drueckt den ersten vorhandenen Knopf aus <see cref="KnopfReihenfolge"/>.
        /// Rueckgabe: Name des gedrueckten Knopfes bzw. "WM_CLOSE" als letzte Moeglichkeit.
        /// </summary>
        private static string BejahendDruecken(IntPtr dialog)
        {
            var knoepfe = new Dictionary<int, IntPtr>();
            EnumChildWindows(dialog, (kind, lParam) =>
            {
                var klasse = new StringBuilder(64);
                GetClassName(kind, klasse, klasse.Capacity);
                if (klasse.ToString() == "Button")
                {
                    int id = GetDlgCtrlID(kind);
                    if (!knoepfe.ContainsKey(id)) knoepfe[id] = kind;
                }
                return true;
            }, IntPtr.Zero);

            foreach (int id in KnopfReihenfolge)
            {
                IntPtr knopf;
                if (!knoepfe.TryGetValue(id, out knopf)) continue;
                PostMessage(dialog, WM_COMMAND, new IntPtr(id), knopf);
                return KnopfName(id);
            }

            PostMessage(dialog, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return "WM_CLOSE";
        }

        /// <summary>Liest den laengsten statischen Text des Dialogs - das ist die Meldung.</summary>
        private static string DialogtextLesen(IntPtr dialog)
        {
            string laengster = "";
            EnumChildWindows(dialog, (kind, lParam) =>
            {
                var klasse = new StringBuilder(64);
                GetClassName(kind, klasse, klasse.Capacity);
                if (klasse.ToString() == "Static")
                {
                    var text = new StringBuilder(1024);
                    GetWindowText(kind, text, text.Capacity);
                    if (text.Length > laengster.Length) laengster = text.ToString();
                }
                return true;
            }, IntPtr.Zero);
            return laengster.Replace("\r", " ").Replace("\n", " ");
        }

        public void Dispose()
        {
            _laeuft = false;
            try { _thread.Join(1500); } catch { }
        }

        private delegate bool EnumFensterCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumFensterCallback lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumFensterCallback lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetDlgCtrlID(IntPtr hWnd);
    }
}
