using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der globalen Anwendungseinstellungen (iU9-W14c.6).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente</b> — genauer:
    /// die EINSTELLUNGSseite. Sie kommt aus <see cref="EinstellungenCtrl"/> (W14c.0i),
    /// dem ersten SCHREIBENDEN Weg zu <c>Properties.Settings</c> außerhalb einer Maske
    /// (Befund W14c-B57). Der KI-Abschalter liegt in der Registry und läuft über
    /// <see cref="KiEinwilligung"/>.</para>
    ///
    /// <para><b>Der Ordnerwähler kommt von der Plattform</b>
    /// (<c>Dienste.Datei.OrdnerWaehlen</c>) — dasselbe Muster wie
    /// <c>Views/Bericht/BerichtSeiteGaben.cs:59</c>. Der Baustein
    /// <c>Dateiwahl</c> braucht dafür KEINE Änderung: Er nimmt den Wähler ohnehin als
    /// Delegaten entgegen. <b>Ohne Delegat kein Knopf</b> — auf iOS liefert
    /// <c>IosDateiDienst.OrdnerWaehlen</c> immer <c>""</c> (Entscheid E-5).</para>
    ///
    /// <para><b>Der Menüpunkt ist der Anker der drei Admin-Einträge</b> (Befund
    /// W14c-B63): <c>InitGesetzeMenue</c>, <c>InitDublettenMenue</c> und
    /// <c>InitLizenzMenue</c> hängen ihre Einträge unterhalb von
    /// <c>MenuItem_Einstellungen</c> ein. Der Menüpunkt selbst bleibt deshalb
    /// unverändert stehen — nur sein Click-Ereignis zeigt jetzt hierher.</para>
    /// </summary>
    internal static class EinstellungenHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 701 × 450).</summary>
        private static readonly Size MASS = new Size(760, 560);

        /// <summary>
        /// Zeigt die Einstellungen als eigenes Fenster — der Weg von
        /// <c>Hauptfensterrahmen.MenuItem_Einstellungen_Click</c>.
        ///
        /// <para><b>Mit Besitzer und in einem <c>using</c></b> (Befund W14c-B34): Der
        /// Vorläufer wurde mit <c>ShowDialog()</c> ohne <c>this</c> und ohne
        /// <c>using</c> geöffnet — er erschien nicht über dem Hauptfenster und wurde
        /// nie entsorgt.</para>
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<EinstellungenDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<EinstellungenDialog>(
                MyResource.Resource.ADM_SET_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ der Komponente.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            bool kiLesbar = true;
            bool kiAus = false;
            bool riegel = false;
            try
            {
                riegel = KiEinwilligung.AbschalterMaschine;
                kiAus = KiEinwilligung.Abgeschaltet;
            }
            catch
            {
                // Befund W14c-B49: Der Vorlaeufer fing das hier ebenfalls ab - liess den
                // Schalter aber KOMMENTARLOS verschwinden. Er bleibt jetzt stehen und
                // sagt, dass er nicht gelesen werden konnte.
                kiLesbar = false;
            }

            return new Dictionary<string, object>
            {
                ["Satz"] = EinstellungenCtrl.Lesen(),
                ["KiAbgeschaltet"] = kiAus,
                ["MaschinenRiegel"] = riegel,
                ["KiLesbar"] = kiLesbar,
                ["OrdnerWaehler"] = new Func<string, Task<string>>(OrdnerWaehlen),
                ["Speichern"] = new Func<Einstellungensatz, bool, Task<SpeicherBefund>>(Speichern),
                ["Zuruecksetzen"] = new Func<Task<Einstellungensatz>>(
                    () => Task.FromResult(EinstellungenCtrl.Zuruecksetzen()))
            };
        }

        private static Task<string> OrdnerWaehlen(string start)
        {
            return Task.FromResult(Dienste.Datei.OrdnerWaehlen(
                MyResource.Resource.ADM_SET_BTN_DURCHSUCHEN, start ?? ""));
        }

        /// <summary>
        /// Schreibt die neun Werte und — gesondert — den KI-Abschalter. Bei
        /// maschinenweiter Sperre bleibt der Schalter unangetastet
        /// (<c>KiAbschalterSpeichern</c> des Vorläufers).
        /// </summary>
        private static Task<SpeicherBefund> Speichern(Einstellungensatz satz, bool kiAus)
        {
            SpeicherBefund befund = EinstellungenCtrl.Speichern(satz);
            if (!befund.Ok) return Task.FromResult(befund);

            try
            {
                if (!KiEinwilligung.AbschalterMaschine) KiEinwilligung.Abgeschaltet = kiAus;
            }
            catch
            {
                // Der Registry-Schalter ist nicht der Grund, die neun Settings zu
                // verwerfen - sie stehen bereits.
            }

            return Task.FromResult(befund);
        }
    }
}
