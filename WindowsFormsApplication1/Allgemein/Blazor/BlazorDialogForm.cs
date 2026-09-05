using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
    /// <para><b>Beispiel</b> (Views\Heizkessel\Form_Heizkessel.cs,
    /// NeuenEnergietraegerAnlegen):</para>
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
        /// <param name="groesse">Gewuenschtes Innenmass beim Oeffnen. Die Huelle klemmt es
        /// auf den Arbeitsbereich des Bildschirms (Befund 03.09.2026: ein Fachdialog mit
        /// 914 px Breite war auf dem Anwenderrechner zusammengequetscht und liess sich
        /// nicht anpassen). Der Anwender kann das Fenster danach ziehen und maximieren;
        /// das Layout innerhalb der Komponente ist fluessig (M2).</param>
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
            // WACHE (Befund W16b-B-1, 05.09.2026): Ein Schluessel ohne passenden
            // [Parameter] laesst die Komponente beim ERSTEN Zeichnen brechen -
            // im Blazor-Verteiler, also ohne Namen und ohne Ort. Eine Zeile
            // Reflexion beim Bauen der Huelle sagt dasselbe Nein frueher und
            // nennt den Schluessel. Muster: ZustandParameterPruefen in
            // BlazorSeite (Befund W16c-B12), nur fuer den ganzen Satz.
            Parametersatzwache.Pruefen(typeof(TKomponente), parameter,
                                       "BlazorDialogForm<" + typeof(TKomponente).Name + ">");

            Text = titel;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(MIN_BREITE, MIN_HOEHE);
            ClientSize = AnBildschirmGeklemmt(groesse);
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

            // Auch die Flaeche der WebView2 selbst traegt die Themafarbe - sonst
            // blitzt beim ersten Zeichnen ihr eigenes Weiss durch. Dieselbe Zeile
            // wie in BlazorSeite; sie fehlte hier bis W16b-B-1.
            try { _web.WebView.DefaultBackgroundColor = Themaflaeche; }
            catch { /* aeltere WebView2-Laufzeit: Schoenheitsfehler, kein Fehlschlag */ }

            // FEHLERSCHRANKE (Befund W13-B-1, 05.09.2026): Gemountet wird nicht
            // die Komponente selbst, sondern EPOS.UI.Bausteine.Wurzel<T> - eine
            // ErrorBoundary mit T darin. Eine ErrorBoundary faengt die Ausnahmen
            // ihrer NACHFAHREN, sie muss also ueber T stehen; eine Wurzel hat
            // aber nichts ueber sich. Ohne dieses Zwischenglied beendet eine
            // Ausnahme aus einem Ereignis oder aus dem Lebenszyklus von T den
            // PROZESS, weil der WinForms-BlazorWebView (10.0.100) kein
            // UnhandledException-Ereignis fuehrt (dieselbe Luecke, die
            // WebViewWache im Klassenkopf als ihre Grenze nennt).
            //
            // Der Parametersatz geht UNVERAENDERT durch (Wurzel.Gaben faengt ihn
            // mit CaptureUnmatchedValues), und die Parametersatzwache oben prueft
            // weiterhin gegen T - nicht gegen die Verpackung.
            _web.RootComponents.Add<EPOS.UI.Bausteine.Wurzel<TKomponente>>("#app", parameter);
            Controls.Add(_web);

            // WACHE (Befund W16b-B-1): Bleibt die Flaeche beige, sagt sie warum.
            // Der WinForms-BlazorWebView fuehrt kein UnhandledException-Ereignis
            // (10.0.100) - ohne diese Wache bleibt eine gescheiterte
            // WebView2-Initialisierung vollstaendig still.
            WebViewWache.Anhaengen(_web, this, typeof(TKomponente).Name);
        }

        /// <summary>Kleinste sinnvolle Aussenmasse: darunter passt kein Dialogkopf mehr.</summary>
        private const int MIN_BREITE = 520;
        private const int MIN_HOEHE = 360;

        // ==================================================================
        //  Die vier Zusaetze fuer den BESITZERLOSEN Lauf (iU9-W15c.6)
        // ==================================================================
        //
        // Bis W15c wurde jeder Blazor-Dialog aus einem offenen Fenster heraus
        // gezeigt. Der Erststart und die Lizenzzustimmung laufen dagegen in
        // Program.Main, VOR Application.Run - es gibt kein Besitzerfenster, weil es
        // noch keines gibt. Form_Erststart (die Maske, die dabei faellt) weicht in
        // genau vier Punkten von dieser Huelle ab; die vier stehen hier als Schalter
        // mit dem HEUTIGEN Vorgabewert. Fuer die vorhandenen Aufrufer aendert sich
        // dadurch nichts (Befund W15c-B8).

        /// <summary>
        /// Eintrag in der Taskleiste. Vorgabe <c>false</c> wie bisher; der
        /// besitzerlose Erststart braucht <c>true</c> — ein minutenlanger Lauf ohne
        /// Elternfenster und ohne Taskleisteneintrag ist nicht wiederzufinden, sobald
        /// er einmal hinter einem anderen Fenster liegt.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ImTaskbar
        {
            get => ShowInTaskbar;
            set => ShowInTaskbar = value;
        }

        /// <summary>
        /// Zentriert auf dem BILDSCHIRM statt auf dem Besitzer. Vorgabe <c>false</c>
        /// (also <see cref="FormStartPosition.CenterParent"/> wie bisher).
        /// </summary>
        /// <remarks>
        /// Ohne Besitzer verhält sich <c>CenterParent</c> bereits wie
        /// <c>CenterScreen</c>; der Schalter macht die Absicht sichtbar und wählt bei
        /// mehreren Schirmen ausdrücklich den, auf dem das Fenster erscheint.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AufBildschirmMittig
        {
            get => StartPosition == FormStartPosition.CenterScreen;
            set => StartPosition = value ? FormStartPosition.CenterScreen
                                         : FormStartPosition.CenterParent;
        }

        /// <summary>
        /// Sperrt Kreuz, Alt+F4 und Esc, solange ein Lauf nicht zu Ende ist —
        /// dieselbe Absicherung wie <c>Form_Erststart:195-200</c> und <c>:212/:263</c>:
        /// <c>ControlBox</c> aus UND ein Riegel in <see cref="Form.OnFormClosing"/>.
        /// </summary>
        /// <remarks>
        /// <b>Warum beides.</b> <c>ControlBox = false</c> nimmt das Kreuz weg, nicht
        /// aber Alt+F4 und nicht den Weg über <c>Schliessen()</c> aus der Komponente.
        /// Der Riegel fängt nur <see cref="CloseReason.UserClosing"/> — ein
        /// Herunterfahren des Rechners oder ein <c>Application.Exit</c> bleibt
        /// möglich, genau wie im Vorläufer. <b>Der Schalter gehört der KOMPONENTE</b>:
        /// Sie weiß, wann der Lauf beginnt und endet, und meldet es über einen
        /// <c>EventCallback&lt;bool&gt;</c> an die Hülle.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SchliessenGesperrt
        {
            get => _schliessenGesperrt;
            set
            {
                if (InvokeRequired)
                {
                    // Der Rueckkanal kommt aus dem Blazor-Verteiler, nicht zwingend
                    // vom Oberflaechenfaden - dieselbe Weiche wie in Schliessen().
                    BeginInvoke(new Action<bool>(v => SchliessenGesperrt = v), value);
                    return;
                }
                _schliessenGesperrt = value;
                if (!IsDisposed) ControlBox = !value;
            }
        }

        private bool _schliessenGesperrt;

        /// <summary>
        /// Das Kleinstmaß des Fensters. Vorgabe 520 × 360 wie bisher; der
        /// Erststart braucht 600 × 400, sonst wird sein Protokollfenster unlesbar.
        /// </summary>
        /// <remarks>
        /// Bewusst KEIN <c>new MinimumSize</c>: Eine geerbte Eigenschaft zu verdecken
        /// ist eine Falle — wer die Hülle je über eine <see cref="Form"/>-Variable
        /// hielte, setzte die andere. Dieser Name sagt, was gemeint ist, und schreibt
        /// dieselbe Eigenschaft.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Size Mindestmass
        {
            get => MinimumSize;
            set => MinimumSize = value;
        }

        /// <inheritdoc />
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Waehrend eines Laufs gibt es kein Zurueck - weder ueber das Kreuz noch
            // ueber Alt+F4. Woertlich aus Form_Erststart:196-200.
            if (_schliessenGesperrt && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Klemmt das gewuenschte Innenmass auf 92 % des Arbeitsbereichs des Bildschirms,
        /// auf dem der Dialog erscheint (Bildschirm des aktiven Fensters, sonst der
        /// Hauptbildschirm). Kleinere Wuensche bleiben unveraendert.
        /// </summary>
        private static Size AnBildschirmGeklemmt(Size gewuenscht)
        {
            Rectangle arbeit;
            try
            {
                Form aktiv = Form.ActiveForm;
                arbeit = (aktiv != null ? Screen.FromControl(aktiv) : Screen.PrimaryScreen).WorkingArea;
            }
            catch
            {
                arbeit = Screen.PrimaryScreen.WorkingArea;
            }
            int maxBreite = Math.Max(MIN_BREITE, (int)(arbeit.Width * 0.92));
            int maxHoehe = Math.Max(MIN_HOEHE, (int)(arbeit.Height * 0.92) - 40);   // Rahmen + Titelleiste
            return new Size(Math.Min(gewuenscht.Width, maxBreite), Math.Min(gewuenscht.Height, maxHoehe));
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

        // iU9-W16c.4 (Anwenderentscheid E-6 / iF21): DIE DPI-INSEL IST WEG.
        //
        // Bis hierher verdeckten zwei ShowDialog-Ueberladungen die von Form
        // (new), um den Faden fuer die Dauer des modalen Laufs auf
        // "Per Monitor V2" zu stellen und danach zurueck. Der Grund war, dass die
        // ANWENDUNG DpiUnaware lief: Ein bitmapskalierter WebView2-Inhalt ist bei
        // 125-200 % sichtbar unscharf, und die Insel war der einzige Weg, einzelne
        // Fenster davon auszunehmen.
        //
        // Seit W16c.4 laeuft die ganze Anwendung Per Monitor V2 (app.manifest und
        // Program.Main); der Sonderweg hat keinen Gegenstand mehr. Aufrufer
        // brauchen nichts zu aendern - sie riefen ShowDialog() und rufen weiter
        // ShowDialog(), jetzt das der Basisklasse.

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && _web != null) _web.Dispose();
            base.Dispose(disposing);
        }
    }

    // iU9-W16c.4: Hier stand die Klasse DpiInsel (Risiko G1 der iU8-Vermessung) -
    // ein P/Invoke auf SetThreadDpiAwarenessContext samt Betreten/Verlassen. Sie
    // war die Antwort auf einen Befund, den es nicht mehr gibt: "EPOS-Plan laeuft
    // insgesamt DPI-unbewusst". Seit W16c.4 laeuft es Per Monitor V2, und ein
    // Fenster, das ohnehin im richtigen Kontext entsteht, braucht keine Insel.
    //
    // Der Weg zurueck steht in der Versionsgeschichte: Wer die Anwendung je
    // wieder DpiUnaware machen muesste, holt die Klasse aus dem Stand vor diesem
    // Commit.
}
