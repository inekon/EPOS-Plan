using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Web.WebView2.Core;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WACHE ueber eine <see cref="BlazorWebView"/> — Befund <b>W16b‑B‑1</b>
    /// der Windows-Abnahme vom 05.09.2026.
    ///
    /// <para><b>Der Befund.</b> Ein aus der Startseite geoeffneter Dialog zeigte
    /// eine vollstaendig einheitlich beige Flaeche: kein Absturz, keine Meldung,
    /// kein Inhalt. Beige ist <c>#F5F4EF</c> — die <c>BackColor</c> der Huelle,
    /// die dasteht, SOLANGE die WebView2 nichts gezeichnet hat. Was dazwischen
    /// schiefging, sagte niemand: Der WinForms-<c>BlazorWebView</c> von
    /// <c>Microsoft.AspNetCore.Components.WebView.WindowsForms</c> <b>10.0.100</b>
    /// fuehrt — anders als die WPF- und die MAUI-Fassung — <b>kein</b>
    /// <c>UnhandledException</c>-Ereignis (nachgeprueft an der Metadatenliste des
    /// Pakets: es gibt nur <c>UrlLoading</c>,
    /// <c>BlazorWebViewInitializing</c> und <c>BlazorWebViewInitialized</c>).
    /// Eine gescheiterte WebView2-Initialisierung bleibt damit still.</para>
    ///
    /// <para><b>Was diese Klasse tut.</b> Sie haengt sich an die zwei Ereignisse,
    /// die es GIBT, und legt eine Frist darueber:</para>
    /// <list type="number">
    /// <item><see cref="CoreWebView2.Environment"/> scheitert →
    ///       <c>CoreWebView2InitializationCompleted</c> mit
    ///       <c>IsSuccess == false</c>; die <c>InitializationException</c> wird
    ///       sofort lesbar gezeigt und protokolliert.</item>
    /// <item>Die WebView2 kommt gar nicht erst so weit → nach
    ///       <see cref="FRIST_SEKUNDEN"/> Sekunden steht statt der beigen Flaeche
    ///       ein Text, der sagt, WIE weit sie gekommen ist (CoreWebView2 da?
    ///       Blazor angemeldet?).</item>
    /// </list>
    ///
    /// <para><b>Warum ein Text im Fenster und keine <c>MessageBox</c>.</b> Der
    /// Anwender soll den Wortlaut abschreiben oder ablichten koennen, und eine
    /// Meldung ueber einem modalen Dialog verdeckt genau das Fenster, um das es
    /// geht. Derselbe Wortlaut geht zusaetzlich nach
    /// <see cref="System.Diagnostics.Trace"/> und
    /// <see cref="System.Diagnostics.Debug"/> — dort liest ihn DebugView oder
    /// ein angehaengter Debugger mit.</para>
    ///
    /// <para><b>Sie darf nie selbst stoeren.</b> Jede Zeile steht in
    /// <c>try</c>/<c>catch</c>: Eine Wache, die den Dialog mitreisst, waere
    /// schlimmer als der Befund, den sie meldet.</para>
    /// </summary>
    internal sealed class WebViewWache
    {
        /// <summary>
        /// Frist bis zur Fehlanzeige. Der Aufbau einer WebView2 dauert laut
        /// iU8-Vermessung 100–300 ms; zehn Sekunden sind damit weit jenseits
        /// jeder gewoehnlichen Verzoegerung und lassen auch einem kalten
        /// Browserstart auf einem langsamen Rechner Luft.
        /// </summary>
        internal const int FRIST_SEKUNDEN = 10;

        private readonly BlazorWebView _web;
        private readonly Control _wirt;
        private readonly string _bezeichnung;

        private Timer _frist;
        private bool _blazorBereit;
        private bool _gemeldet;

        private WebViewWache(BlazorWebView web, Control wirt, string bezeichnung)
        {
            _web = web;
            _wirt = wirt;
            _bezeichnung = string.IsNullOrEmpty(bezeichnung) ? "Blazor-Ansicht" : bezeichnung;
        }

        /// <summary>
        /// Haengt die Wache an. Aufzurufen im Aufbau der Huelle, NACH
        /// <c>Controls.Add</c> — die Frist laeuft ab dem ersten Zeichnen des
        /// Wirtes.
        /// </summary>
        /// <param name="web">Die zu ueberwachende Ansicht.</param>
        /// <param name="wirt">Das Fenster bzw. Steuerelement, das sie traegt —
        /// dorthin kommt im Fehlerfall die Textflaeche.</param>
        /// <param name="bezeichnung">Name der Ansicht fuer Meldung und Protokoll
        /// (in der Regel der Typname der Razor-Komponente).</param>
        internal static void Anhaengen(BlazorWebView web, Control wirt, string bezeichnung)
        {
            if (web == null || wirt == null) return;

            try
            {
                WebViewWache wache = new WebViewWache(web, wirt, bezeichnung);

                // Der Ereignistyp wird BEWUSST nicht benannt: Er heisst je nach
                // Fassung des Pakets anders; der Rueckruf braucht ihn nicht.
                web.BlazorWebViewInitialized += (s, e) => wache.BlazorAngemeldet();
                web.WebView.CoreWebView2InitializationCompleted += wache.KernFertig;

                wirt.HandleCreated += wache.WirtSichtbar;
                wirt.Disposed += wache.WirtEntsorgt;

                // Steht das Fenster schon, kommt HandleCreated nicht mehr.
                if (wirt.IsHandleCreated) wache.FristStarten();
            }
            catch (Exception ex)
            {
                Protokoll("Die WebView-Wache konnte nicht angehaengt werden: " + ex.Message);
            }
        }

        // =====================================================================
        //  Die zwei Ereignisse, die es gibt
        // =====================================================================

        private void BlazorAngemeldet()
        {
            _blazorBereit = true;
            Protokoll(_bezeichnung + ": BlazorWebViewInitialized.");
        }

        private void KernFertig(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e != null && e.IsSuccess)
            {
                Protokoll(_bezeichnung + ": CoreWebView2 steht.");
                return;
            }

            Exception fehler = e == null ? null : e.InitializationException;

            Zeigen(_bezeichnung + ": Die WebView2 dieser Ansicht ist nicht gestartet."
                   + Environment.NewLine + Environment.NewLine
                   + "CoreWebView2InitializationCompleted meldet IsSuccess = false."
                   + Environment.NewLine + Ausnahmezeilen(fehler));
        }

        // =====================================================================
        //  Die Frist
        // =====================================================================

        private void WirtSichtbar(object sender, EventArgs e)
        {
            FristStarten();
        }

        private void FristStarten()
        {
            if (_frist != null) return;

            try
            {
                _frist = new Timer { Interval = FRIST_SEKUNDEN * 1000 };
                _frist.Tick += FristAbgelaufen;
                _frist.Start();
            }
            catch (Exception ex)
            {
                Protokoll("Die Frist der WebView-Wache lief nicht an: " + ex.Message);
            }
        }

        private void FristAbgelaufen(object sender, EventArgs e)
        {
            FristBeenden();

            bool kernDa;
            try { kernDa = _web.WebView != null && _web.WebView.CoreWebView2 != null; }
            catch { kernDa = false; }

            // Beides da: Die WebView2 laeuft und Blazor haengt daran. Was danach
            // kommt - eine Ausnahme beim Zeichnen der Komponente - sieht diese
            // Wache nicht; sie schweigt dann auch.
            if (kernDa && _blazorBereit) return;

            Zeigen(_bezeichnung + ": Die Ansicht ist nach " + FRIST_SEKUNDEN
                   + " Sekunden noch leer." + Environment.NewLine + Environment.NewLine
                   + "CoreWebView2: " + (kernDa ? "steht" : "FEHLT") + Environment.NewLine
                   + "Blazor angemeldet: " + (_blazorBereit ? "ja" : "NEIN") + Environment.NewLine
                   + Environment.NewLine
                   + "Ohne CoreWebView2 ist die WebView2-Laufzeit nicht hochgekommen; "
                   + "ohne Blazor-Anmeldung hat der Blazor-Verteiler die Seite nicht "
                   + "uebernommen.");
        }

        private void FristBeenden()
        {
            if (_frist == null) return;

            try
            {
                _frist.Stop();
                _frist.Tick -= FristAbgelaufen;
                _frist.Dispose();
            }
            catch { /* beim Zumachen ist alles erlaubt */ }

            _frist = null;
        }

        private void WirtEntsorgt(object sender, EventArgs e)
        {
            FristBeenden();
        }

        // =====================================================================
        //  Anzeigen und protokollieren
        // =====================================================================

        /// <summary>
        /// Legt den Wortlaut als lesbare, MARKIERBARE Textflaeche ueber den
        /// Wirt und schreibt ihn ins Ablaufprotokoll. Genau einmal je Wache.
        /// </summary>
        private void Zeigen(string satz)
        {
            if (_gemeldet) return;
            _gemeldet = true;

            Protokoll(satz);

            try
            {
                if (_wirt == null || _wirt.IsDisposed) return;

                if (_wirt.InvokeRequired)
                {
                    _wirt.BeginInvoke(new Action<string>(Anbringen), satz);
                    return;
                }

                Anbringen(satz);
            }
            catch (Exception ex)
            {
                Protokoll("Die Fehlanzeige der WebView-Wache scheiterte: " + ex.Message);
            }
        }

        private void Anbringen(string satz)
        {
            if (_wirt == null || _wirt.IsDisposed) return;

            TextBox flaeche = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,

                // OBEN, nicht ueber die ganze Flaeche: Kommt die WebView2 doch
                // noch (ein sehr langsamer Kaltstart), steht sie darunter und
                // bleibt bedienbar. Eine Meldung, die den Inhalt verdeckt, waere
                // ein zweiter Befund.
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(0xFD, 0xF3, 0xF2),   // WARN_FEHLER_FLAECHE
                ForeColor = Color.FromArgb(0x8A, 0x1C, 0x1C),   // WARN_FEHLER_TEXT
                Text = satz.Replace("\n", Environment.NewLine)
            };

            _wirt.Controls.Add(flaeche);
            flaeche.BringToFront();
        }

        /// <summary>Typ, Wortlaut und innerste Ausnahme — mehr braucht die Suche nicht.</summary>
        private static string Ausnahmezeilen(Exception fehler)
        {
            if (fehler == null) return "Eine Ausnahme wurde dabei nicht gemeldet.";

            Exception innerste = fehler;
            while (innerste.InnerException != null) innerste = innerste.InnerException;

            string satz = fehler.GetType().FullName + ": " + fehler.Message;
            if (!ReferenceEquals(innerste, fehler))
                satz += Environment.NewLine + "  → " + innerste.GetType().FullName
                      + ": " + innerste.Message;
            return satz;
        }

        private static void Protokoll(string satz)
        {
            try
            {
                Debug.WriteLine("[WebView] " + satz);
                Trace.WriteLine("[WebView] " + satz);
            }
            catch { /* ein Protokoll darf nie der Grund eines Fehlers sein */ }
        }
    }
}
