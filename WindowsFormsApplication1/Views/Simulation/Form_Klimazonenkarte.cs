using System;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Karte der 15 Klimazonen nach DIN 4710 zum Ansehen und Auswählen
    /// (Konzept_Klimazonenkarte_EPOS-Plan.md, Stufen 1+2 in einem: Anwenderwunsch
    /// und Kartengrafik vom 29.08.2026). Aufgerufen aus dem Erdreich-Quellendialog;
    /// „OK" übernimmt die angeklickte Zone in dessen Auswahlliste, ein Doppelklick
    /// auf eine Zonenfläche übernimmt direkt. Die Auswahlliste selbst bleibt
    /// bestehen — für die Tastatur und für „nicht zugeordnet", das auf der Karte
    /// bewusst nicht wählbar ist.
    /// </summary>
    /// <remarks>
    /// Die Oberfläche steht in <c>Form_Klimazonenkarte.Designer.cs</c>, ohne eigene
    /// <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt; im Designer stehen die deutschen Echttexte.
    /// Die Kartenmechanik (Bild, Polygone, Hit-Test, Hervorhebung) liegt vollständig
    /// im Control <see cref="KlimazonenKarte"/>.
    /// </remarks>
    public partial class Form_Klimazonenkarte : Form
    {
        /// <summary>
        /// Baut den Dialog auf.
        /// </summary>
        /// <param name="aktuelleZone">Vorgewählte Zone 1…15; 0 = keine.</param>
        public Form_Klimazonenkarte(int aktuelleZone)
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und laesst
            // AutoScaleDimensions weg: Die Anwendung laeuft DpiUnaware (app.manifest,
            // Program.SetHighDpiMode) - None haelt den Faktor 1:1 fest, mit dem alle
            // Masse dieses Dialogs gemessen sind.
            InitializeComponent();
            TexteSetzen();

            _karte.GewaehlteZone = aktuelleZone;
            AuswahlAnzeigen();

            FensterEinpassung.Einhaengen(this);
        }

        /// <summary>Die auf der Karte gewählte Zone 1…15; 0 = keine Auswahl.</summary>
        public int GewaehlteZone
        {
            get { return _karte.GewaehlteZone; }
        }

        // ------------------------------------------------------------------- Texte

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Echttexte.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.SIMQ_KARTE_TITEL;
            _btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            _btnAbbruch.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        /// <summary>Anzeigetext einer Zone — gleiche Bauweise wie die Auswahlliste
        /// des Erdreich-Dialogs („8 — 2.000 h/a").</summary>
        private static string ZonenText(int zone)
        {
            return zone.ToString(CultureInfo.CurrentCulture) + " — " +
                VDI4640Pruefung.VolllaststundenZone(zone).ToString("N0", CultureInfo.CurrentCulture) + " h/a";
        }

        /// <summary>Statuszeile unter der Karte nachführen.</summary>
        private void AuswahlAnzeigen()
        {
            int zone = _karte.GewaehlteZone;
            _lblGewaehlt.Text = zone >= 1
                ? string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_KARTE_GEWAEHLT, ZonenText(zone))
                : MyResource.Resource.SIMQ_KARTE_KEINE;
        }

        // --------------------------------------------------------------- Ereignisse

        /// <summary>Zone angeklickt — Statuszeile nachführen.</summary>
        private void karte_ZoneGewaehlt(object sender, EventArgs e)
        {
            AuswahlAnzeigen();
        }

        /// <summary>Doppelklick auf eine Zone — direkt übernehmen.</summary>
        private void karte_ZoneUebernommen(object sender, EventArgs e)
        {
            AuswahlAnzeigen();
            this.DialogResult = DialogResult.OK;
        }
    }
}
