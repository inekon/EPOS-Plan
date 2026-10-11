using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER von „Hilfe → Version" (Konzept Administration und Support,
    /// Abschnitt 5). Die Hülle ist plattformfrei (<see cref="UeberHuelle"/>,
    /// <c>EPOS.UI.Daten</c>); hier stehen nur das Fenster, Produktname, Version und Baudatum
    /// und die Rückrufe der Plattform: Kopieren über die Zwischenablage, Protokollordner und
    /// Fernwartung, Adresse öffnen über <c>Dienste.Datei.AdresseOeffnen</c> — und die Geste
    /// des Entwicklermodus (Konzept 3.2, Entwicklerhandbuch Abschnitt 1).
    ///
    /// <para><b>Die Geste.</b> Strg+Umschalt+Alt+E fängt das Fenster über
    /// <see cref="BlazorDialogForm{TKomponente}.Befehlstaste"/> ab, bevor die WebView die Taste
    /// sieht. Eine Nachricht später — nicht mitten in der Tastenverarbeitung und nicht im
    /// Blazor-Verteiler, denn die Rückfrage ist modal — schaltet
    /// <see cref="EntwicklungHuelle.GesteUmschalten"/> den Modus; danach versorgt das Hauptfenster
    /// Menü, Marke und Titel neu, und der Anstoß lässt den Dialog den neuen Diagnoseblock holen.</para>
    ///
    /// <para>Was einen Modaldialog öffnen kann (Protokollordner mit Meldung, Fernwartung), läuft
    /// ebenso eine Nachricht später: Kein Rückruf der WebView2 trägt eine Modalschleife oder
    /// eine Ausnahme heraus.</para>
    /// </summary>
    internal static class UeberFenster
    {
        /// <summary>Die Tastenfolge der Geste.</summary>
        internal const Keys GESTE = Keys.Control | Keys.Shift | Keys.Alt | Keys.E;

        /// <summary>Zeigt „Über EPOS-Plan" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="version">Die Version der Kopfzeile, wie die Kopfleiste sie zeigt.</param>
        /// <param name="neuVersorgen">Versorgt Menü, Marke und Titel des Hauptfensters nach einem Wechsel.</param>
        internal static void Oeffnen(IWin32Window besitzer, string version, Action neuVersorgen)
        {
            BlazorDialogForm<UeberDialog> dlg = null;
            var anstoss = new UeberAnstoss();
            string programmversion = Application.ProductVersion;

            // Die Zwischenablage verlangt den STA-Faden der Oberfläche.
            Action<string> kopieren = text =>
            {
                if (dlg != null && dlg.InvokeRequired) dlg.Invoke(new Action(() => Kopieren(text)));
                else Kopieren(text);
            };
            Action protokollordner = () => Spaeter(dlg, EntwicklungHuelle.ProtokollordnerOeffnen);
            Action fernwartung = () => Spaeter(dlg, () => FernwartungFenster.Oeffnen(dlg));

            // Die Windows-Schale hat VOR dem Anstoss geschaltet (s. o.); der Rueckruf liefert den Block danach.
            Func<string> schalten = () => UeberHuelle.Diagnosetext(programmversion);

            var werte = new Dictionary<string, object>(UeberHuelle.Gaben(
                Hauptfensterrahmen.PRODUKTNAME, version, Baudatum(), programmversion,
                AdresseOeffnen, kopieren, protokollordner, fernwartung, schalten, anstoss))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<UeberDialog>(
                UeberHuelle.Titel(Hauptfensterrahmen.PRODUKTNAME),
                new Size(UeberHuelle.FENSTER_BREITE, UeberHuelle.FENSTER_HOEHE),
                werte);

            dlg.Befehlstaste = taste =>
            {
                if (taste != GESTE) return false;
                Spaeter(dlg, () =>
                {
                    if (EntwicklungHuelle.GesteUmschalten(neuVersorgen)) anstoss.Ausloesen();
                });
                return true;
            };

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Das Baudatum: der Schreibzeitpunkt der Programmdatei, kurz in der Kultur der Oberfläche;
        /// leer, wenn er nicht zu ermitteln ist.
        /// </summary>
        internal static string Baudatum()
        {
            try
            {
                string pfad = typeof(UeberFenster).Assembly.Location;
                if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad)) return "";
                return File.GetLastWriteTime(pfad).ToString("d", CultureInfo.CurrentCulture);
            }
            catch { return ""; }
        }

        /// <summary>Eine Nachricht später auf dem Faden des Fensters; ohne Fenster sofort.</summary>
        private static void Spaeter(Control fenster, Action handlung)
        {
            Action sicher = () =>
            {
                try { handlung(); }
                catch (Exception ex) { Ausnahmeprotokoll.Vermerken("Über EPOS-Plan: " + ex.Message); }
            };
            if (fenster != null && fenster.IsHandleCreated && !fenster.IsDisposed) fenster.BeginInvoke(sicher);
            else sicher();
        }

        /// <summary>Öffnet die Adresse im Standardprogramm; eine Ausnahme heißt „misslungen".</summary>
        private static bool AdresseOeffnen(string adresse)
        {
            try { return Dienste.Datei.AdresseOeffnen(adresse); }
            catch { return false; }
        }

        /// <summary>Text in die Zwischenablage, auf dem STA-Faden der Oberfläche. Eine Ausnahme meldet der Dialog.</summary>
        private static void Kopieren(string text) => Clipboard.SetText(string.IsNullOrEmpty(text) ? " " : text);
    }
}
