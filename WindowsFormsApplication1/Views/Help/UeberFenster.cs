using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
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
    /// <para><b>Die Geste.</b> Strg+Umschalt+F12 (ohne Alt, damit AltGr der deutschen Tastatur nicht
    /// stört) fängt das Fenster über
    /// <see cref="BlazorDialogForm{TKomponente}.Befehlstaste"/> ab, unabhängig vom Fokus: Liegt er im
    /// Browserfenster der WebView2, kommt die Taste über deren Tastenereignis (AcceleratorKeyPressed
    /// → <c>KeyDown</c> des Steuerelements, als behandelt zurückgemeldet), sonst über
    /// <see cref="Form.ProcessCmdKey"/>. Eine Nachricht später — nicht mitten in der Tastenverarbeitung und nicht im
    /// Blazor-Verteiler, denn die Rückfrage ist modal — schaltet
    /// <see cref="EntwicklungHuelle.GesteUmschalten"/> den Modus; danach versorgt das Hauptfenster
    /// Menü, Marke und Titel neu, und der Anstoß lässt den Dialog den neuen Diagnoseblock holen.</para>
    ///
    /// <para><b>Zweiter Weg der Geste.</b> Der Razor-Dialog erkennt die Geste zusätzlich selbst an der
    /// physischen Taste, solange der Fokus in seiner Wurzel liegt, und ruft <c>GesteAusgeloest</c>, das
    /// hier denselben Weg nimmt — ein Rückfallweg, falls das Tastenereignis der WebView2 ausbleibt. Eine Sperre lässt je Tastendruck nur
    /// EIN Schalten zu, auch wenn beide Wege ihn sehen.</para>
    ///
    /// <para>Was einen Modaldialog öffnen kann (Protokollordner mit Meldung, Fernwartung), läuft
    /// ebenso eine Nachricht später: Kein Rückruf der WebView2 trägt eine Modalschleife oder
    /// eine Ausnahme heraus.</para>
    /// </summary>
    internal static class UeberFenster
    {
        /// <summary>
        /// Die Tastenfolge der Geste: Strg+Umschalt+F12. F12 allein öffnet bei freigegebenem
        /// Entwicklerbereich <c>WEBVIEW_WERKZEUGE</c> die Entwicklerwerkzeuge der WebView; mit Strg+Umschalt
        /// ist es kein Beschleuniger der WebView. Das Fenster meldet die Geste über das Tastenereignis der
        /// WebView2 (Fokus im Browser) bzw. <see cref="Form.ProcessCmdKey"/> (Fokus auf der WinForms-Seite)
        /// als behandelt.
        /// </summary>
        internal const Keys GESTE = Keys.Control | Keys.Shift | Keys.F12;

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

            // Das Supportpaket: Speicherdialog und Schreiben eine Nachricht spaeter; der Dialog wartet auf das Ergebnis.
            Func<bool, Task<SupportpaketAnzeige>> supportpaket = mitKi =>
            {
                var fertig = new TaskCompletionSource<SupportpaketAnzeige>();
                Spaeter(dlg, () =>
                {
                    try { fertig.TrySetResult(SupportpaketSpeichern(dlg, programmversion, mitKi)); }
                    catch (Exception ex) { fertig.TrySetResult(new SupportpaketAnzeige("", Array.Empty<string>(), Array.Empty<string>(), ex.Message)); }
                });
                return fertig.Task;
            };

            // Die Geste, gleich auf welchem Weg erkannt (ProcessCmdKey oder der Dialog selbst): eine
            // Nachricht spaeter schalten, Hauptfenster neu versorgen, anstossen. Die Sperre laesst
            // waehrend des Schaltens (Rueckfrage ist modal) und kurz danach kein zweites zu.
            var sperre = new Gestensperre();
            Action geste = () => Spaeter(dlg, () =>
            {
                if (!sperre.Betreten(Environment.TickCount64)) return;
                try
                {
                    if (EntwicklungHuelle.GesteUmschalten(neuVersorgen)) anstoss.Ausloesen();
                }
                finally { sperre.Verlassen(Environment.TickCount64); }
            });

            // Die Windows-Schale hat VOR dem Anstoss geschaltet (s. o.); der Rueckruf liefert den Block danach.
            Func<string> schalten = () => UeberHuelle.Diagnosetext(programmversion);

            var werte = new Dictionary<string, object>(UeberHuelle.Gaben(
                Hauptfensterrahmen.PRODUKTNAME, version, Baudatum(), programmversion,
                AdresseOeffnen, kopieren, protokollordner, fernwartung, schalten, anstoss, supportpaket, geste))
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
                geste();
                return true;
            };

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Die SPERRE gegen doppeltes Schalten: Ein Tastendruck kann die Geste auf beiden Wegen
        /// auslösen (<see cref="Form.ProcessCmdKey"/> und der Dialog). Während eines Schaltens und
        /// <see cref="RUHE_MS"/> danach wird kein zweites zugelassen. Nur auf dem Faden der Oberfläche.
        /// </summary>
        internal sealed class Gestensperre
        {
            /// <summary>Die Ruhezeit nach einem Schalten in Millisekunden.</summary>
            internal const long RUHE_MS = 500;

            private bool _laeuft;
            private long _zuletzt = long.MinValue / 2;

            /// <summary><c>true</c> = das Schalten darf laufen; dann ist die Sperre belegt.</summary>
            internal bool Betreten(long jetztMs)
            {
                if (_laeuft || jetztMs - _zuletzt < RUHE_MS) return false;
                _laeuft = true;
                _zuletzt = jetztMs;
                return true;
            }

            /// <summary>Gibt die Sperre frei; die Ruhezeit zählt ab hier.</summary>
            internal void Verlassen(long jetztMs)
            {
                _laeuft = false;
                _zuletzt = jetztMs;
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

        /// <summary>
        /// „Supportpaket speichern…": der Speicherdialog im Dokumentenordner mit dem Vorgabenamen
        /// <see cref="Supportpaket.Vorgabename"/>; Abbruch liefert ein Ergebnis ohne Pfad.
        /// </summary>
        private static SupportpaketAnzeige SupportpaketSpeichern(IWin32Window besitzer, string programmversion, bool mitKiProtokoll)
        {
            string ziel;
            using (var wahl = new SaveFileDialog
            {
                Title = MyResource.Resource.SUP_PAKET_DIALOGTITEL,
                Filter = MyResource.Resource.SUP_PAKET_DATEIFILTER,
                DefaultExt = "zip",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Supportpaket.Vorgabename(DateTime.Now),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            })
            {
                DialogResult antwort = besitzer != null ? wahl.ShowDialog(besitzer) : wahl.ShowDialog();
                if (antwort != DialogResult.OK) return SupportpaketAnzeige.Abgebrochen;
                ziel = wahl.FileName;
            }
            return UeberHuelle.SupportpaketSpeichern(ziel, programmversion, mitKiProtokoll);
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
