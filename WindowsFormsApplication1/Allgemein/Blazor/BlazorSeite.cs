using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Web.WebView2.WinForms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE fuer eine NICHT-MODALE Blazor-Seite (Umsetzungskonzept
    /// iOS, Paket iU9, Welle 5 - Bausteinluecke 9 des Wellenplans).
    ///
    /// <para><b>Der Unterschied zur Dialoghuelle.</b>
    /// <see cref="BlazorDialogForm{T}"/> ist ein eigenes modales Fenster; sie kommt,
    /// zeigt, liefert ein <see cref="DialogResult"/> und geht wieder. Eine SEITE
    /// dagegen sitzt in einer vorhandenen Maske und bleibt dort stehen, solange die
    /// Maske offen ist - hier: die vier Seiten des Reiters „Berichte &amp; Kosten"
    /// in <c>Form_Start.tabPage6</c>. Deshalb ist diese Huelle ein
    /// <see cref="UserControl"/> und kein <see cref="Form"/>.</para>
    ///
    /// <para><b>EINE WebView JE FENSTER</b> (Risiko R5 des Wellenplans). Eine
    /// <see cref="BlazorWebView"/> kostet 60-120 MB und 100-300 ms Aufbau. Die vier
    /// Seiten des Reiters laufen deshalb in EINER Huelle mit EINER WebView; das
    /// Umschalten zwischen ihnen ist Sache der Komponente (Baustein
    /// <c>Reiter</c>), nicht der Windows-Seite. Wer eine zweite Huelle in dasselbe
    /// Fenster haengt, hat den Fehler gemacht, den dieser Absatz verhindern soll.</para>
    ///
    /// <para><b>Projektwechsel ohne Neuaufbau.</b> Eine WebView beim Wechsel des
    /// Projekts wegzuwerfen und neu zu bauen waere jedes Mal ein Aufblitzen und eine
    /// Drittelsekunde Wartezeit. Stattdessen haelt die Huelle einen ZUSTAND mit
    /// Aenderungsereignis: <see cref="Zustand"/> traegt die Projekt-Id samt Namen,
    /// die Komponente haengt sich an <c>Geaendert</c> und zeichnet neu. Die WebView
    /// bleibt dieselbe. Gespeist wird der Zustand aus <c>Dienste.Projekt</c> (iU5)
    /// bzw. unmittelbar aus <see cref="ProjektSetzen"/>, solange die Startmaske noch
    /// WinForms ist.</para>
    ///
    /// <para><b>DPI - der offene Punkt (Risiko R4).</b> Die Anwendung laeuft
    /// DpiUnaware (<c>app.manifest</c>, <c>Program.SetHighDpiMode</c>). Die
    /// Dialoghuelle umgeht das mit der <c>DpiInsel</c>: Sie stellt den Faden fuer
    /// die Dauer des modalen Laufs auf „Per Monitor V2", und weil dabei sowohl das
    /// Fenster als auch das Fenster der WebView2 entsteht, ist der Dialoginhalt
    /// scharf. FUER EINE EINGEBETTETE SEITE GEHT DAS NICHT: Sie hat kein eigenes
    /// Fenster, sie sitzt im Fenster der DpiUnaware-<c>Form_Start</c>, und Windows
    /// skaliert dieses Fenster als Bitmap - bei 125-200 % also unscharf. Ein
    /// Fenster kann seinen DPI-Kontext nachtraeglich nicht wechseln.
    /// <b>Diese Huelle versucht es deshalb gar nicht erst.</b> Der Weg zur
    /// scharfen Seite ist, die Anwendung insgesamt DPI-faehig zu machen (offener
    /// Entscheid iF21) - ein eigenes Paket, das die fest gerechneten
    /// Pixelkoordinaten der gewachsenen WinForms-Masken betrifft. Bis dahin ist die
    /// Schaerfe der Seite ein <b>Abnahmepunkt</b> (Umsetzung_iU9_Nachweise.md,
    /// 125 % und 150 %) und keine Zusage.</para>
    ///
    /// <para><b>Kein weisses Aufblitzen.</b> Wie bei der Dialoghuelle steht die
    /// Themaflaeche, bevor die WebView2 da ist.</para>
    /// </summary>
    /// <typeparam name="TKomponente">Die anzuzeigende Razor-Komponente aus EPOS.UI.</typeparam>
    public class BlazorSeite<TKomponente> : UserControl
        where TKomponente : Microsoft.AspNetCore.Components.IComponent
    {
        /// <summary>
        /// Die Themaflaeche (<c>KartenStil.FLAECHE</c> bzw. <c>--epos-flaeche</c>) -
        /// dieselbe Farbe wie in <see cref="BlazorDialogForm{T}"/>. Sie steht schon,
        /// bevor die WebView2 fertig ist.
        /// </summary>
        private static readonly Color Themaflaeche = Color.FromArgb(0xF5, 0xF4, 0xEF);

        private readonly BlazorWebView _web;

        /// <summary>
        /// Der geteilte Zustand der Seite. Die Huelle schreibt hinein, die Komponente
        /// liest und haengt sich an <see cref="SeitenZustand.Geaendert"/>.
        /// </summary>
        public SeitenZustand Zustand { get; }

        /// <summary>
        /// Baut die Huelle. Die WebView2 wird hier nur zusammengestellt; sie startet
        /// mit der Handle-Erzeugung, also sobald die Seite sichtbar wird.
        /// </summary>
        /// <param name="parameter">Die Parameter der Komponente, Name -&gt; Wert.
        /// Der Zustand wird unter <see cref="SeitenZustand.PARAMETER"/> ergaenzt,
        /// falls der Aufrufer ihn nicht selbst eingetragen hat.</param>
        public BlazorSeite(IDictionary<string, object> parameter)
            : this(parameter, BlazorDienste.Erzeugen())
        {
        }

        /// <summary>
        /// Wie oben, aber mit einem eigenen Dienstverzeichnis - fuer Pruefstaende,
        /// die einen anderen Hilfedienst einlegen wollen.
        /// </summary>
        public BlazorSeite(IDictionary<string, object> parameter, IServiceProvider dienste)
        {
            if (parameter == null) parameter = new Dictionary<string, object>();

            Zustand = parameter.ContainsKey(SeitenZustand.PARAMETER)
                ? (SeitenZustand)parameter[SeitenZustand.PARAMETER]
                : new SeitenZustand();
            parameter[SeitenZustand.PARAMETER] = Zustand;

            Dock = DockStyle.Fill;
            BackColor = Themaflaeche;

            _web = new BlazorWebView
            {
                Dock = DockStyle.Fill,
                HostPage = "wwwroot\\index.html",
                Services = dienste
            };

            // MUSS VOR der Handle-Erzeugung stehen - danach ist die WebView2 bereits
            // gestartet und CreationProperties wirkungslos. Dieselben Angaben wie in
            // der Dialoghuelle, und das ist keine Verdopplung, sondern der Zweck:
            // Ein GEMEINSAMER UserDataFolder heisst ein gemeinsamer Browserprozess
            // fuer Dialoge und Seiten - sonst laufen zwei nebeneinander.
            _web.WebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(Dienste.Pfade.BenutzerLokal, "WebView2"),
                Language = Sprache.Englisch ? "en-US" : "de-DE"
            };

            // Auch die Flaeche der WebView2 selbst traegt die Themafarbe - sonst
            // blitzt beim ersten Zeichnen ihr eigenes Weiss durch.
            try { _web.WebView.DefaultBackgroundColor = Themaflaeche; }
            catch { /* aeltere WebView2-Laufzeit: Schoenheitsfehler, kein Fehlschlag */ }

            _web.RootComponents.Add<TKomponente>("#app", parameter);
            Controls.Add(_web);
        }

        /// <summary>
        /// Stellt die Seite auf ein anderes Projekt ein - OHNE die WebView neu zu
        /// bauen. Die Komponente erfaehrt es ueber <see cref="SeitenZustand.Geaendert"/>.
        /// </summary>
        public void ProjektSetzen(int idProjekt, string name)
        {
            Zustand.ProjektSetzen(idProjekt, name);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && _web != null) _web.Dispose();
            base.Dispose(disposing);
        }
    }
}
