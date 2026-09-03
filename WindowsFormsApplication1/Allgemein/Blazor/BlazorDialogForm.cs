using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Web.WebView2.WinForms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE fuer einen Blazor-Dialog (Umsetzungskonzept iOS,
    /// Paket iU8, Strang B).
    ///
    /// <para><b>Wozu.</b> Ab iU8 entsteht jeder neue Dialog als Razor-Komponente in
    /// <c>EPOS.UI</c> - plattformfrei, ohne WinForms, ohne System.Drawing, ohne
    /// Datenbank. Diese Klasse ist alles, was Windows davon noch braucht: ein
    /// gewoehnliches modales <see cref="Form"/> mit einer
    /// <see cref="BlazorWebView"/> darin. Sie ist bewusst die EINZIGE Stelle, an der
    /// WinForms und Blazor aufeinandertreffen; jeder weitere Dialog benutzt sie
    /// unveraendert und stellt nur andere Parameter hinein.</para>
    ///
    /// <para><b>Der Aufrufer merkt nichts.</b> <c>ShowDialog()</c> liefert weiterhin
    /// ein <see cref="DialogResult"/>. Die Komponente meldet ihr Ergebnis ueber
    /// einen <c>EventCallback</c>, der <see cref="Schliessen"/> ruft.</para>
    ///
    /// <para><b>Beispiel</b> (Views\Kosten\Form_Kosten.cs, CreateNewEnergyCarrier):</para>
    /// <code>
    /// var werte = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Energietraeger"] = EnergietraegerVarianteCtrl.Energietraeger(),
    ///     ["Geschlossen"]    = EventCallback.Factory.Create&lt;Ergebnis&gt;(this, e =&gt; { ... })
    /// };
    /// using (var dlg = new BlazorDialogForm&lt;EnergietraegerVarianteDialog&gt;(titel, groesse, werte))
    /// {
    ///     if (dlg.ShowDialog() != DialogResult.OK) return "";
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="TKomponente">Die anzuzeigende Razor-Komponente aus EPOS.UI.</typeparam>
    public sealed class BlazorDialogForm<TKomponente> : Form
        where TKomponente : Microsoft.AspNetCore.Components.IComponent
    {
        /// <summary>
        /// Die Themaflaeche (<c>KartenStil.FLAECHE</c> bzw. <c>--epos-flaeche</c>).
        /// Sie steht schon, bevor die WebView2 fertig ist: Deren Aufbau laeuft
        /// asynchron und dauert 100-300 ms; ohne diese Farbe blitzt in dieser Zeit
        /// eine weisse Flaeche auf (Risiko G3 der iU8-Vermessung).
        /// </summary>
        private static readonly Color Themaflaeche = Color.FromArgb(0xF5, 0xF4, 0xEF);

        private readonly BlazorWebView _web;

        /// <summary>
        /// Baut die Huelle. Die WebView2 wird hier nur zusammengestellt; sie startet
        /// erst mit der Handle-Erzeugung, also beim <c>ShowDialog</c>.
        /// </summary>
        /// <param name="titel">Fenstertitel - derselbe Text wie in der Komponente.</param>
        /// <param name="groesse">Innenmass des Dialogs. Die Groesse ist je Dialog fest;
        /// das Anpassen an den Inhalt macht das Layout innerhalb der Komponente.</param>
        /// <param name="parameter">Die Parameter der Komponente, Name -&gt; Wert.</param>
        public BlazorDialogForm(string titel, Size groesse, IDictionary<string, object> parameter)
            : this(titel, groesse, parameter, BlazorDienste.Erzeugen())
        {
        }

        /// <summary>
        /// Wie oben, aber mit einem eigenen Dienstverzeichnis - fuer Pruefstaende,
        /// die einen anderen Hilfedienst einlegen wollen.
        /// </summary>
        public BlazorDialogForm(string titel, Size groesse, IDictionary<string, object> parameter,
                                IServiceProvider dienste)
        {
            Text = titel;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = groesse;
            BackColor = Themaflaeche;

            _web = new BlazorWebView
            {
                Dock = DockStyle.Fill,
                HostPage = "wwwroot\\index.html",
                Services = dienste
            };

            // MUSS VOR der Handle-Erzeugung stehen: Danach ist die WebView2 bereits
            // gestartet und CreationProperties wirkungslos.
            //
            // UserDataFolder ist PFLICHT, kein Feinschliff. Ohne Angabe legt WebView2
            // ihr Profil neben die EXE ("EPOS_Plan.exe.WebView2"). Bei der
            // maschinenweiten Installation liegt die EXE unter C:\Program Files -
            // dort darf ein Standardbenutzer nichts anlegen, die WebView2 kaeme gar
            // nicht hoch. %LOCALAPPDATA%\WP-Plan ist der Ordner, den die Anwendung
            // ohnehin fuer benutzereigene Ablagen benutzt (Dienste.Pfade.BenutzerLokal).
            //
            // Language steuert Kontextmenue und Rechtschreibpruefung der WebView2 -
            // die Texte der Komponente kommen davon unabhaengig aus Resource.*.
            _web.WebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(Dienste.Pfade.BenutzerLokal, "WebView2"),
                Language = Sprache.Englisch ? "en-US" : "de-DE"
            };

            _web.RootComponents.Add<TKomponente>("#app", parameter);
            Controls.Add(_web);
        }

        /// <summary>
        /// Schliesst den Dialog mit einem Ergebnis. Gerufen wird die Methode aus dem
        /// <c>EventCallback</c> der Komponente und damit vom Blazor-Verteiler, nicht
        /// zwingend vom Oberflaechenfaden - deshalb die Weiche.
        /// </summary>
        /// <param name="ok"><c>true</c> = OK, <c>false</c> = Abbrechen.</param>
        public void Schliessen(bool ok)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(Schliessen), ok);
                return;
            }

            DialogResult = ok ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Zeigt den Dialog modal - mit der DPI-Insel aus <see cref="DpiInsel"/>.
        /// </summary>
        /// <remarks>
        /// Die Methode VERDECKT <see cref="Form.ShowDialog()"/> bewusst (<c>new</c>).
        /// <see cref="Form.ShowDialog()"/> ist nicht ueberschreibbar, die Insel muss
        /// aber den gesamten modalen Lauf umschliessen: Nur dann entstehen sowohl das
        /// Fenster als auch das Fenster der WebView2 im gewuenschten DPI-Kontext.
        /// Aufrufer halten die Huelle immer unter ihrem eigenen Typ; der Weg ueber
        /// eine <see cref="Form"/>-Variable kommt nicht vor.
        /// </remarks>
        public new DialogResult ShowDialog()
        {
            IntPtr vorher = DpiInsel.Betreten();
            try
            {
                return base.ShowDialog();
            }
            finally
            {
                DpiInsel.Verlassen(vorher);
            }
        }

        /// <summary>Wie <see cref="ShowDialog()"/>, mit ausdruecklichem Besitzerfenster.</summary>
        public new DialogResult ShowDialog(IWin32Window eltern)
        {
            IntPtr vorher = DpiInsel.Betreten();
            try
            {
                return base.ShowDialog(eltern);
            }
            finally
            {
                DpiInsel.Verlassen(vorher);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && _web != null) _web.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// DPI-INSEL fuer die Blazor-Huelle (Risiko G1 der iU8-Vermessung).
    ///
    /// <para><b>Der Befund.</b> EPOS-Plan laeuft insgesamt DPI-unbewusst:
    /// <c>app.manifest</c> setzt <c>dpiAware=false</c>, <c>Program.Main</c>
    /// <c>HighDpiMode.DpiUnaware</c>. Windows skaliert die Fenster deshalb als
    /// Bitmap. Fuer die gewachsenen WinForms-Masken mit ihren festen
    /// Pixelkoordinaten ist das die einzige Fassung, die ueberall gleich aussieht -
    /// eine Umstellung der ganzen Anwendung ist ein eigenes Paket.</para>
    ///
    /// <para><b>Warum die Insel.</b> Ein bitmapskalierter WebView2-Inhalt ist bei
    /// 125-200 % sichtbar unscharf, und ausgerechnet der erste Blazor-Dialog waere
    /// davon betroffen. Windows 10 ab 1803 erlaubt es, EINZELNE Fenster in einem
    /// anderen DPI-Kontext zu erzeugen als den Rest des Prozesses. Genau das
    /// geschieht hier: Der Faden wird fuer die Dauer des modalen Laufs auf
    /// "Per Monitor V2" gestellt und danach exakt zurueckgesetzt. Die WinForms-Masken
    /// dahinter bleiben unberuehrt.</para>
    ///
    /// <para><b>Wenn es nicht geht, geht es ohne.</b> Auf einem aelteren Windows
    /// liefert der Aufruf <c>IntPtr.Zero</c>; dann laeuft der Dialog wie bisher
    /// bitmapskaliert. Das ist ein Schoenheitsfehler, kein Fehlschlag - deshalb wird
    /// hier nichts gemeldet und nichts geworfen. Ob die Insel wirklich greift, ist
    /// ein Windows-Pruefpunkt (Umsetzung_iU8_Nachweise.md, 125 % und 150 %).</para>
    /// </summary>
    internal static class DpiInsel
    {
        /// <summary>
        /// <c>DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2</c> - als Pseudo-Handle
        /// definiert (winuser.h: <c>((DPI_AWARENESS_CONTEXT)-4)</c>).
        /// </summary>
        private static readonly IntPtr PerMonitorV2 = new IntPtr(-4);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        /// <summary>
        /// Stellt den Faden auf "Per Monitor V2" und liefert den vorherigen Kontext
        /// zurueck. <c>IntPtr.Zero</c> heisst: nicht moeglich - dann ist auch nichts
        /// zurueckzusetzen.
        /// </summary>
        internal static IntPtr Betreten()
        {
            try
            {
                return SetThreadDpiAwarenessContext(PerMonitorV2);
            }
            catch (EntryPointNotFoundException)
            {
                return IntPtr.Zero;   // Windows aelter als 10 (1803)
            }
            catch (DllNotFoundException)
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>Setzt den Kontext aus <see cref="Betreten"/> wieder ein.</summary>
        internal static void Verlassen(IntPtr vorher)
        {
            if (vorher == IntPtr.Zero) return;

            try
            {
                SetThreadDpiAwarenessContext(vorher);
            }
            catch (EntryPointNotFoundException) { /* nach einem erfolgreichen Betreten() unmoeglich */ }
            catch (DllNotFoundException) { }
        }
    }
}
