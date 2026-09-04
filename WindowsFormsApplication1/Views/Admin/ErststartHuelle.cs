using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Lizenz;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Erststart-Assistenten (iU9-W15c.7) — Ersatz für
    /// <c>Views/Admin/Form_Erststart.cs</c> (273 Z., ohne Designer).
    ///
    /// <para><b>Sie ist die erste BESITZERLOSE Blazor-Hülle des Bestands.</b>
    /// <c>Program.Main</c> zeigt den Assistenten aus <c>ErststartAnbieten</c> — vor der
    /// Lizenzzustimmung, vor der Schema-Migration, vor <c>Hauptfensterrahmen</c>. Es gibt kein
    /// Elternfenster, weil es noch keines gibt. Drei der vier Zusätze aus W15c.6 sind
    /// deshalb gesetzt: <c>ImTaskbar</c> (ein minutenlanger Lauf ohne
    /// Taskleisteneintrag wäre nicht wiederzufinden), <c>AufBildschirmMittig</c> und
    /// <c>Mindestmass</c> 600 × 400; den vierten — <c>SchliessenGesperrt</c> — schaltet
    /// die KOMPONENTE über ihren Rückkanal <c>LaufAktiv</c>, denn nur sie weiß, wann
    /// der Lauf beginnt und endet.</para>
    ///
    /// <para><b>Der Faden gehört hierher.</b> <c>ErststartMigration.Fuehredurch</c>
    /// läuft minutenlang; der Vorläufer schickte ihn über einen eigenen
    /// <see cref="Thread"/> mit <c>IsBackground = true</c> und meldete über ein
    /// <see cref="Progress{T}"/>, das auf dem Oberflächenfaden erzeugt wurde. Hier ist
    /// es dasselbe, nur als <c>Task.Run</c>: Die Komponente ruft <c>Lauf</c> und
    /// wartet; das Marshalling der Protokollzeilen besorgt der Blazor-Verteiler
    /// (<c>InvokeAsync</c> in der Komponente).</para>
    ///
    /// <para><b>Auf iOS erscheint dieser Assistent nie</b> (Entscheid W15c-E-5,
    /// Befund W15c-B9): Dort ist der Erststart eine Dateikopie aus dem
    /// Anwendungspaket. Die Komponente wandert allein deshalb nach <c>EPOS.UI</c>,
    /// damit im Startweg keine WinForms-Maske zurückbleibt (Regel M1).</para>
    /// </summary>
    internal static class ErststartHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Der Vorläufer stand auf 680 × 460 (min 600 × 400);
        /// die Razor-Fassung braucht etwas mehr Höhe für dasselbe Protokollfenster
        /// mit 44-px-Berührungszielen (Entscheid E-13).
        /// </summary>
        private static readonly Size MASS = new Size(760, 560);

        /// <summary>Kleinstmaß — wie im Vorläufer, sonst wird das Protokoll unlesbar.</summary>
        private static readonly Size MINDEST = new Size(600, 400);

        /// <summary>
        /// Zeigt den Assistenten und führt die Umstellung durch, wenn der Anwender
        /// zustimmt. <b>Besitzerlos und modal</b>, wie <c>Form_Erststart.Zeigen</c>.
        /// </summary>
        /// <param name="dbOrdner">Ordner mit <c>Kenndaten.accdb</c>.</param>
        /// <param name="berichtPfad">Pfad des Migrationsberichts, sofern einer entstand.</param>
        /// <returns>
        /// <c>true</c> = die SQLite-Datei steht; das Programm kann normal weiterstarten.
        /// <c>false</c> = abgelehnt oder fehlgeschlagen; der Grund steht in
        /// <see cref="ErststartCtrl.LetzteMeldung"/>.
        /// </returns>
        internal static bool Zeigen(string dbOrdner, out string berichtPfad)
        {
            bool erfolg = false;
            string bericht = null;
            BlazorDialogForm<ErststartDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Kopftext"] = ErststartCtrl.Kopftext(dbOrdner),
                ["ProtokollText"] = MyResource.Resource.ERST_LBL_PROTOKOLL,
                ["KnopfStarten"] = MyResource.Resource.ERST_BTN_STARTEN,
                ["KnopfBeenden"] = MyResource.Resource.ERST_BTN_BEENDEN,
                ["StatusBereit"] = MyResource.Resource.ERST_STATUS_BEREIT,
                ["StatusLaeuft"] = MyResource.Resource.ERST_STATUS_LAEUFT,
                ["StatusFertig"] = MyResource.Resource.ERST_STATUS_FERTIG,
                ["StatusFehler"] = MyResource.Resource.ERST_STATUS_FEHLER,

                ["Lauf"] = (Func<Action<string>, Task<(bool Ok, string Schlussmeldung)>>)
                           (melden => Laufen(dbOrdner, melden, p => bericht = p)),

                ["LaufAktiv"] = EventCallback.Factory.Create<bool>(
                    new object(), aktiv => { if (dlg != null) dlg.SchliessenGesperrt = aktiv; }),

                ["Fertig"] = EventCallback.Factory.Create<bool>(
                    new object(), ok =>
                    {
                        erfolg = ok;
                        if (dlg != null) dlg.Schliessen(ok);
                    })
            };

            dlg = new BlazorDialogForm<ErststartDialog>(MyResource.Resource.ERST_TITEL, MASS, werte)
            {
                ImTaskbar = true,            // ohne Elternfenster sonst nicht wiederzufinden
                AufBildschirmMittig = true,  // es gibt keinen Besitzer, auf den zentriert würde
                Mindestmass = MINDEST,
            };

            using (dlg)
            {
                dlg.ShowDialog();            // BESITZERLOS - vor jedem anderen Fenster
            }

            berichtPfad = bericht;
            return erfolg;
        }

        /// <summary>
        /// Der Lauf im Hintergrund. <c>Task.Run</c> statt eines eigenen
        /// <see cref="Thread"/>: dieselbe Aufteilung wie im Vorläufer — der
        /// Bedienfaden zeichnet, der Hintergrund rechnet —, nur ohne die
        /// <c>BeginInvoke</c>-Rückreise; die besorgt der Blazor-Verteiler.
        /// </summary>
        private static Task<(bool Ok, string Schlussmeldung)> Laufen(
            string dbOrdner, Action<string> melden, Action<string> berichtMerken)
        {
            // Progress<T> wird HIER erzeugt, also auf dem Oberflaechenfaden - genau wie
            // im Vorlaeufer (:218-220). Seine Meldungen kommen damit von selbst dort
            // wieder an.
            var fortschritt = new Progress<string>(z => melden(z));

            return Task.Run(() =>
            {
                bool ok = false;
                string bericht = null;
                try
                {
                    ok = ErststartCtrl.Starten(dbOrdner, fortschritt, out bericht);
                }
                catch (Exception)
                {
                    // Starten faengt selbst ab und fuellt LetzteMeldung; hier bleibt
                    // nur der Fall "gar nicht erst losgelaufen".
                    ok = false;
                }
                berichtMerken(bericht);
                return (ok, ErststartCtrl.LetzteMeldung);
            });
        }
    }
}
