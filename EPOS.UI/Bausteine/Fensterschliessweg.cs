#nullable enable
using System;
using System.Threading.Tasks;

namespace EPOS.UI.Bausteine
{
    /// <summary>
    /// <b>Das Schließkreuz eines EIGENEN Fensters führt erst offene Blätter zurück</b> (Anwenderwunsch
    /// „Gleiches Verhalten mit anderen Dialogen“): Wie Kreuz, Esc und Hintergrund einer
    /// <see cref="Ueberlagerung"/> schließen Fensterkreuz und Alt+F4 eines Dialogs im eigenen Fenster
    /// zuerst das oberste <see cref="Blattwechsel"/>-Blatt; erst vom Wurzelblatt aus schließt das Fenster.
    ///
    /// <para><b>Die Naht.</b> Die Fensterhülle (unter Windows <c>BlazorDialogForm</c>) legt EINEN Schließweg
    /// je Fenster an und reicht ihn der <see cref="Fensterwurzel{TInhalt}"/> als Parameter herein; die
    /// Wurzel gibt dessen <see cref="Stapel"/> als <c>CascadingValue</c> an den Dialog, und jeder
    /// Blattwechsel unmittelbar im Dialog meldet sich dort an (in einer Überlagerung gilt deren eigener
    /// Stapel). Beim Schließen fragt die Hülle nur <see cref="SchliessenAbfangen"/> — die Entscheidung
    /// „Blatt offen → zurück“ steht hier, plattformfrei und ohne Fenster prüfbar.</para>
    ///
    /// <para><b>Faden.</b> Der Rückweg eines Blattes ist ein <c>EventCallback</c> und gehört dem Verteiler
    /// des Renderers; die Wurzel legt dafür ihren <c>InvokeAsync</c> als <see cref="Verteiler"/> herein.
    /// Die Abfrage selbst liest nur die Zahl der angemeldeten Blätter und antwortet sofort — das
    /// <c>FormClosing</c> der Hülle ist synchron.</para>
    /// </summary>
    public sealed class Fensterschliessweg
    {
        /// <summary>Die Blätter, die unmittelbar im Fensterdialog stehen.</summary>
        public Blattstapel Stapel { get; } = new();

        /// <summary>
        /// Führt eine Arbeit auf dem Verteiler des Renderers aus; setzt die
        /// <see cref="Fensterwurzel{TInhalt}"/>. Ohne Verteiler (noch nicht gezeichnet) läuft die Arbeit
        /// unmittelbar.
        /// </summary>
        public Func<Func<Task>, Task>? Verteiler { get; set; }

        /// <summary>Der zuletzt angestoßene Rückweg (Prüfhilfe; die Hülle wartet nicht darauf).</summary>
        public Task LetzterRueckweg { get; private set; } = Task.CompletedTask;

        /// <summary>
        /// Will das Fenster schließen? <c>true</c> = es stand ein Blatt: dessen Rückweg ist angestoßen, die
        /// Hülle bricht das Schließen ab. <c>false</c> = Wurzelblatt, das Fenster schließt wie bisher.
        /// </summary>
        public bool SchliessenAbfangen()
        {
            if (!Stapel.BlattOffen) return false;
            Func<Task> arbeit = () => Stapel.ZurueckZumVorigen();
            LetzterRueckweg = Verteiler is null ? arbeit() : Verteiler(arbeit);
            return true;
        }
    }
}
