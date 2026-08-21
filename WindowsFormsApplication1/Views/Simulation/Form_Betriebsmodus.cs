using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahl des Betriebsmodus (Leistungssteuerung) einer Wärmepumpe — Konzept 4.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Herausgelöst aus <c>Form_Simulation_Config.Uebersicht.BetriebsmodusBearbeiten</c>,
    /// wo der Dialog als Wegwerf-<c>Form</c> im Methodenrumpf entstand und nie
    /// aufgeräumt wurde. Der Dialog ENTSCHEIDET nur; geschrieben wird weiterhin in
    /// der aufrufenden Methode (<c>WaermequelleClass.WertSchreiben</c>), ebenso der
    /// PV-Hinweis und das Auffrischen der Übersicht.
    /// </para>
    /// <para>
    /// Die Vorprüfung „nur für Wärmepumpen" bleibt bewusst beim Aufrufer: Sie ist der
    /// Schutz der Methode, nicht der Maske.
    /// </para>
    /// <para>
    /// Die Oberfläche steht in <c>Form_Betriebsmodus.Designer.cs</c>, ohne eigene
    /// <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt. Im Designer stehen seit der Design-Politur
    /// vom 21.08.2026 die DEUTSCHEN Fassungen derselben Ressourcen (vorher die
    /// Feldnamen als Platzhalter) — allein damit die Entwurfsfläche zeigt, was der
    /// Anwender sieht. Maßgeblich bleibt <see cref="TexteSetzen"/>: Beim Öffnen wird
    /// jeder dieser Texte überschrieben, in der eingestellten Sprache.
    /// </para>
    /// <para>
    /// Die Pixelkoordinaten stammen aus dem früheren Inline-Aufbau; geändert hat die
    /// Design-Politur nur die beiden Fußknöpfe (siehe Geometrieblock unten).
    /// </para>
    /// </remarks>
    public partial class Form_Betriebsmodus : Form
    {
        private readonly string _bezeichner;

        /// <summary>
        /// Der ausgewählte Betriebsmodus als <c>BM_Typ</c>-Wert
        /// (<see cref="WaermequelleClass.MODUS_LAUFZEIT"/> und Geschwister).
        /// Auswertung in derselben Reihenfolge wie vor der Herauslösung.
        /// </summary>
        public string GewaehlterModus
        {
            get
            {
                if (_rbLeistung.Checked) return WaermequelleClass.MODUS_LEISTUNG;
                if (_rbPV.Checked) return WaermequelleClass.MODUS_PV;
                return WaermequelleClass.MODUS_LAUFZEIT;
            }
        }

        /// <param name="bezeichner">Anlagenname für den Fenstertitel.</param>
        /// <param name="aktuellerModus">Bisheriger <c>BM_Typ</c>; steuert die Vorauswahl.</param>
        public Form_Betriebsmodus(string bezeichner, string aktuellerModus)
        {
            _bezeichner = bezeichner;

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Herauslösung wurde
            // AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls keine
            // Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();

            // Bleibt bewusst im Code statt im Designer: Die Kopfzeile leitet ihre
            // Schrift aus der des Fensters ab (wie zuvor "new Font(this.Font,
            // FontStyle.Bold)"). Eine fest eingetragene Schriftart im Designer würde
            // diese Kopplung stillschweigend kappen.
            _lblKopf.Font = new Font(this.Font, FontStyle.Bold);

            TexteSetzen();
            ModusVorwaehlen(aktuellerModus);

            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Oberfläche — gerettete Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_Betriebsmodus.Designer.cs. Designer-Code
        // trägt keine Kommentare; die Pixelentscheidungen stehen deshalb hier.
        //
        // * ClientSize 520 x 300, drei Wahlmöglichkeiten mit je einer Erläuterung
        //   darunter (_lblLaufzeit/_lblLeistung/_lblPV, 460 px breit). Nachgemessen mit
        //   TextRenderer in beiden Sprachen: Der breiteste Absatz braucht 392 px
        //   (deutsch, Laufzeit), die breiteste Wahlzeile 274 px (englisch, PV) - alles
        //   passt ohne Umbruch in die Entwurfsmaße, die Höhe bleibt unverändert.
        //
        // DESIGN-POLITUR 21.08.2026
        //
        // * Die Fußknöpfe tragen jetzt die Standardgröße 110 x 30 (vorher 85 x 23, der
        //   WinForms-Vorgabewert). Die RECHTE KANTE der Knopfgruppe bleibt bei x = 508
        //   und damit 12 px vor dem Fensterrand, genau wie zuvor; verschoben wird nach
        //   links: _btnAbbrechen 423 -> 398, _btnOk 332 -> 278. Zwischen den Knöpfen
        //   liegen 10 px.
        // * y bleibt 258. Mit der neuen Höhe endet die Knopfzeile bei 288 statt 281,
        //   unter ihr bleiben 12 px bis zum Fensterrand - dasselbe Maß wie rechts.
        //   ClientSize musste dafür NICHT wachsen; der Abstand zur letzten
        //   Erläuterung (_lblPV endet bei 246) beträgt weiterhin 12 px.
        // * Die Knopftexte kommen unverändert aus SIM_BTN_OK / SIM_BTN_ABBRECHEN;
        //   AcceptButton und CancelButton waren bereits gesetzt.

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = string.Format(MyResource.Resource.SIM_BETRIEBSMODUS_FENSTERTITEL, _bezeichner);
            _lblKopf.Text = MyResource.Resource.SIM_BETRIEBSMODUS_KOPF;

            _rbLaufzeit.Text = MyResource.Resource.SIM_BM_RB_LAUFZEIT;
            _lblLaufzeit.Text = MyResource.Resource.SIM_BM_TEXT_LAUFZEIT;

            _rbLeistung.Text = MyResource.Resource.SIM_BM_RB_LEISTUNG;
            _lblLeistung.Text = MyResource.Resource.SIM_BM_TEXT_LEISTUNG;

            _rbPV.Text = MyResource.Resource.SIM_BM_RB_PV;
            _lblPV.Text = MyResource.Resource.SIM_BM_TEXT_PV;

            _btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            _btnAbbrechen.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        /// <summary>Vorauswahl aus dem gespeicherten <c>BM_Typ</c>; Vorgabe ist Laufzeit.</summary>
        private void ModusVorwaehlen(string aktuellerModus)
        {
            switch (aktuellerModus)
            {
                case WaermequelleClass.MODUS_LEISTUNG: _rbLeistung.Checked = true; break;
                case WaermequelleClass.MODUS_PV: _rbPV.Checked = true; break;
                default: _rbLaufzeit.Checked = true; break;
            }
        }
    }
}
