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
    /// Die WINDOWS-HÜLLE einer Razor-Komponente ALS ASSISTENTENSEITE (iU9-W6.0e).
    ///
    /// <para><b>Wozu.</b> <see cref="BlazorDialogForm{TKomponente}"/> ist ein eigenes
    /// modales Fenster. Der Assistent braucht aber das Gegenteil: ein randloses
    /// Formular mit <c>TopLevel = false</c>, das <see cref="WizardParent.LoadNewForm"/>
    /// in sein Inhaltspanel steckt. Diese Klasse ist genau das — dieselbe
    /// <see cref="BlazorWebView"/>, dieselben <c>CreationProperties</c> und damit
    /// derselbe Browserprozess wie die Dialoge, nur ohne Rahmen und ohne
    /// <c>ShowDialog</c>.</para>
    ///
    /// <para><b>Verzögert gebaut, mit Grund.</b> <c>AssistentSeiten.Erzeugen</c> baut
    /// alle dreizehn Seiten AUF EINMAL, bevor die erste sichtbar wird. Vier WebViews
    /// im Voraus wären vier Browserprozesse für Seiten, die der Anwender vielleicht nie
    /// sieht (Risiko R‑W6‑1). Die WebView entsteht deshalb erst in
    /// <see cref="Bestuecken"/> — dem Aufruf, den <c>WizardParent</c> unmittelbar nach
    /// dem Anzeigen der Seite macht.</para>
    ///
    /// <para><b>Beim Wiederbesuch wird die Wurzelkomponente getauscht, nicht die
    /// WebView.</b> Der Assistent kehrt zu einer Seite zurück, und dann muss sie den
    /// inzwischen geänderten Listenstand zeigen — die WinForms-Fassungen bauten dafür
    /// in <c>SetControls</c> ihre Liste neu auf. Ein Austausch der Wurzelkomponente
    /// (<c>RootComponents</c> leeren und neu setzen) kostet einen Neuaufbau der
    /// Komponente, aber KEINEN neuen Browserprozess. Weigert sich die Sammlung —
    /// nicht jede Fassung von <c>BlazorWebView</c> lässt das Ändern nach dem Start zu —,
    /// wird die WebView als Ganzes ersetzt; das ist langsamer, aber richtig.</para>
    ///
    /// <para><b>Das Wunschmaß ist gesetzt, nicht gemessen.</b>
    /// <c>WizardParent.LoadNewForm</c> vergrößert das Assistentenfenster nach
    /// <see cref="Control.PreferredSize"/> der Seite. Eine Form mit einer gedockten
    /// WebView darin meldet dafür nichts Brauchbares — die WebView hat keine
    /// Wunschgröße. Deshalb liefert <see cref="GetPreferredSize"/> das Maß, das die
    /// Hülle im Konstruktor bekommen hat: dieselbe Zahl, die der Dialogweg an
    /// <see cref="BlazorDialogForm{TKomponente}"/> gibt.</para>
    ///
    /// <para><b>Zwei Typparameter seit iU9-W9.0a.</b> Bis Welle 8 trug die Hülle
    /// immer eine <c>List&lt;WErzeugerModel&gt;</c>, weil nur Erzeugerseiten Razor
    /// waren. Die vier Bedarfsseiten der Welle 9 tragen vier andere Listentypen;
    /// der Zeilentyp ist deshalb ein Typparameter geworden. Die einparametrige
    /// Fassung darunter ist der Erzeugerfall und heißt weiterhin so, damit
    /// <c>AssistentSeiten</c> und die sechs Erzeugerhüllen unverändert bleiben.</para>
    /// </summary>
    /// <typeparam name="TKomponente">Die anzuzeigende Razor-Komponente aus EPOS.UI.</typeparam>
    /// <typeparam name="TModell">Der Zeilentyp der geteilten Liste.</typeparam>
    internal class BlazorAssistentSeite<TKomponente, TModell> : Form, IAssistentListenSeite<TModell>
        where TKomponente : Microsoft.AspNetCore.Components.IComponent
    {
        /// <summary>
        /// Die Themafläche — dieselbe wie in <see cref="BlazorDialogForm{TKomponente}"/>
        /// und aus demselben Grund: Der Aufbau der WebView2 dauert 100-300 ms, und ohne
        /// diese Farbe blitzt in dieser Zeit eine weiße Fläche auf.
        /// </summary>
        private static readonly Color Themaflaeche = Color.FromArgb(0xF5, 0xF4, 0xEF);

        private readonly Func<int, string, List<TModell>, IDictionary<string, object>> _gaben;
        private readonly Size _wunschmass;

        private BlazorWebView _web;

        /// <summary>
        /// Baut die Hülle. Die WebView entsteht hier NICHT — siehe Klassenkommentar.
        /// </summary>
        /// <param name="gaben">
        /// Liefert den Parametersatz der Komponente aus Projekt-Id, Projektname und der
        /// geteilten Erzeugerliste. Er wird bei jedem <see cref="Bestuecken"/> neu
        /// erfragt, damit ein Wiederbesuch den aktuellen Stand zeigt.
        /// </param>
        /// <param name="wunschmass">Wunschmaß der Seite im Assistentenfenster.</param>
        internal BlazorAssistentSeite(
            Func<int, string, List<TModell>, IDictionary<string, object>> gaben,
            Size wunschmass)
        {
            _gaben = gaben;
            _wunschmass = wunschmass;

            // Wie SetControls(…, bWizard: true) der WinForms-Fassungen: randlos, weiß,
            // ohne eigene Knopfleiste (die stellt der Assistent).
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Themaflaeche;
            ClientSize = wunschmass;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Die beiden Attribute halten den WinForms-Analysator (WFO1000) still: Eine
        /// oeffentliche Eigenschaft auf einem <see cref="Control"/> gilt ihm sonst als
        /// Designer-Eigenschaft, die ihre Serialisierung erklaeren muss. Diese hier ist
        /// eine reine Laufzeitgabe des Assistenten und gehoert in keinen Designer.
        /// </remarks>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<TModell> Modelle { get; set; }

        /// <inheritdoc />
        public void Bestuecken(int projektId, string projektName)
        {
            IDictionary<string, object> werte =
                _gaben(projektId, projektName ?? "", Modelle ?? new List<TModell>());

            if (_web == null) { WebViewAufbauen(werte); return; }

            try
            {
                // Billiger Weg: dieselbe WebView, neue Wurzelkomponente.
                _web.RootComponents.Clear();
                _web.RootComponents.Add<TKomponente>("#app", werte);
            }
            catch (Exception)
            {
                // Teurer Weg, falls die Sammlung nach dem Start nicht mehr mitspielt.
                WebViewAbraeumen();
                WebViewAufbauen(werte);
            }
        }

        private void WebViewAufbauen(IDictionary<string, object> werte)
        {
            _web = new BlazorWebView
            {
                Dock = DockStyle.Fill,
                HostPage = "wwwroot\\index.html",
                Services = BlazorDienste.Erzeugen()
            };

            // MUSS vor der Handle-Erzeugung stehen. UserDataFolder ist Pflicht: Ohne
            // Angabe legt WebView2 ihr Profil neben die EXE, und unter
            // C:\Program Files darf ein Standardbenutzer nichts anlegen. Derselbe
            // Ordner wie in BlazorDialogForm - damit teilen sich Dialoge und
            // Assistentenseiten EINEN Browserprozess.
            _web.WebView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(Dienste.Pfade.BenutzerLokal, "WebView2"),
                Language = Sprache.Englisch ? "en-US" : "de-DE"
            };

            _web.RootComponents.Add<TKomponente>("#app", werte);
            Controls.Add(_web);
        }

        private void WebViewAbraeumen()
        {
            if (_web == null) return;
            Controls.Remove(_web);
            _web.Dispose();
            _web = null;
        }

        /// <summary>
        /// Das Wunschmaß der Seite. <c>WizardParent.LoadNewForm</c> vergrößert das
        /// Assistentenfenster danach; eine gedockte WebView meldet von sich aus nichts.
        /// </summary>
        public override Size GetPreferredSize(Size vorschlag)
        {
            return _wunschmass;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) WebViewAbraeumen();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Die ERZEUGERFASSUNG der Assistentenhülle (iU9-W6.0e, seit W9.0a ein
    /// Spezialfall von <see cref="BlazorAssistentSeite{TKomponente, TModell}"/>).
    ///
    /// <para>Sie trägt die geteilte <c>List&lt;WErzeugerModel&gt;</c> des
    /// Assistentenlaufs und meldet sich zusätzlich als
    /// <see cref="IAssistentErzeugerSeite"/> — der Zweig, den
    /// <c>WizardParent.LoadNewForm</c> seit Welle 6 kennt.</para>
    /// </summary>
    /// <typeparam name="TKomponente">Die anzuzeigende Razor-Komponente aus EPOS.UI.</typeparam>
    internal sealed class BlazorAssistentSeite<TKomponente>
        : BlazorAssistentSeite<TKomponente, WErzeugerModel>, IAssistentErzeugerSeite
        where TKomponente : Microsoft.AspNetCore.Components.IComponent
    {
        /// <inheritdoc cref="BlazorAssistentSeite{TKomponente, TModell}"/>
        internal BlazorAssistentSeite(
            Func<int, string, List<WErzeugerModel>, IDictionary<string, object>> gaben,
            Size wunschmass)
            : base(gaben, wunschmass)
        {
        }
    }
}
