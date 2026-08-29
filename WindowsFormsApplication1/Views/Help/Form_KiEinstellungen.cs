using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einstellungen des KI-Assistenten: API-Schlüssel, Anzeige des fest
    /// vorgegebenen Tageslimits, Neuerkennung des Modells und der Schalter
    /// „Rückfallweg B erzwingen".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Herkunft.</b> Die Maske stand bis zur Designer-Umstellung als
    /// <c>new Form()</c> mitten in <c>Form_KiChat.EinstellungenOeffnen()</c> —
    /// ohne <c>using</c> (das Fenster wurde nie entsorgt) und mit einem
    /// <see cref="ToolTip"/>, den niemand freigab. Beides ist mit dem Umzug
    /// erledigt: Der Aufrufer öffnet den Dialog jetzt in einem <c>using</c>, der
    /// ToolTip hängt als Komponente an <c>components</c> und geht damit über
    /// <c>Dispose</c> mit.
    /// </para>
    /// <para>
    /// <b>Was hier NICHT passiert.</b> Der Dialog speichert nichts. Er liest seine
    /// Anfangswerte aus <see cref="KiChatService"/> und gibt die Eingaben über
    /// <see cref="ApiSchluessel"/> und <see cref="WegBErzwingen"/> zurück; das
    /// Schreiben nach OK und die Rückmeldung im Chatfenster bleiben unverändert
    /// beim Aufrufer. Einzige Ausnahme ist „Modell neu erkennen": Dieser Knopf
    /// hat schon immer den Schlüssel sofort übernommen, damit die Modellabfrage
    /// überhaupt laufen kann — dieses Verhalten ist bewusst mitgezogen worden.
    /// </para>
    /// <para>
    /// Die Oberfläche steht in <c>Form_KiEinstellungen.Designer.cs</c>, ohne
    /// eigene <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und
    /// werden in <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur
    /// Platzhalter.
    /// </para>
    /// </remarks>
    public partial class Form_KiEinstellungen : Form
    {
        public Form_KiEinstellungen()
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
            WerteUebernehmen();

            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Ergebnis für den Aufrufer
        // ==================================================================

        /// <summary>Der eingetragene API-Schlüssel, ohne umgebende Leerzeichen.</summary>
        public string ApiSchluessel { get { return _schluessel.Text.Trim(); } }

        /// <summary>Stand des Schalters „Rückfallweg B erzwingen".</summary>
        public bool WegBErzwingen { get { return _wegB.Checked; } }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// Die Texte, die von Laufzeitwerten abhängen, kommen anschließend in
        /// <see cref="WerteUebernehmen"/> dazu.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.KI_EINST_TITEL;

            _schluesselLabel.Text = MyResource.Resource.KI_EINST_LBL_SCHLUESSEL;
            _limitLabel.Text = MyResource.Resource.KI_EINST_LBL_TAGESLIMIT;
            _modellNeu.Text = MyResource.Resource.KI_EINST_BTN_MODELL;
            _wegB.Text = MyResource.Resource.KI_AKT_WEGB_EINSTELLUNG;
            _ok.Text = MyResource.Resource.KI_EINST_BTN_OK;
            _abbrechen.Text = MyResource.Resource.KI_EINST_BTN_ABBRECHEN;

            _tip.SetToolTip(_limitWert, MyResource.Resource.KI_EINST_TIP_TAGESLIMIT);
        }

        // ==================================================================
        // Werte
        // ==================================================================

        /// <summary>
        /// Holt die Anfangswerte aus <see cref="KiChatService"/> — genau die
        /// Zuweisungen, die vor dem Umzug beim Erzeugen der Steuerelemente
        /// standen. Das Tageslimit ist reine Anzeige: Es wird maschinenweit
        /// vorgegeben und soll vom Anwender nicht angehoben werden können.
        /// </summary>
        private void WerteUebernehmen()
        {
            _schluessel.Text = KiChatService.ApiKey;
            _limitWert.Text = string.Format(MyResource.Resource.KI_EINST_LIMIT_FEST,
                                            KiChatService.Tageslimit);
            _wegB.Checked = KiChatService.WegBErzwingen;

            HinweisSetzen(string.Format(MyResource.Resource.KI_EINST_HINWEIS_MODELL,
                                        KiChatService.MODELL));
        }

        /// <summary>
        /// Baut den dreiteiligen Hinweistext unter den Eingabefeldern auf:
        /// Modellzeile, Datenschutzabsatz, Kontingentabsatz.
        /// </summary>
        /// <remarks>
        /// Vor dem Umzug hing nur die Modellzeile am Anfang eines langen
        /// Literals, und „Modell neu erkennen" tauschte sie über
        /// <c>Substring(IndexOf("\n\n"))</c> aus. Mit Ressourcentexten wäre das
        /// eine Falle: Mehrzeilige Ressourcenwerte liefern zur Laufzeit CRLF, die
        /// Suche nach <c>"\n\n"</c> ginge ins Leere. Deshalb stehen die drei
        /// Absätze einzeln im Katalog und werden hier zusammengesetzt — die
        /// Trennung bleibt wie bisher ein einfaches <c>"\n\n"</c>.
        /// </remarks>
        private void HinweisSetzen(string modellzeile)
        {
            _hinweis.Text = modellzeile + "\n\n" +
                            MyResource.Resource.KI_EINST_HINWEIS_DATEN + "\n\n" +
                            MyResource.Resource.KI_EINST_HINWEIS_KONTINGENT;
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        /// <summary>
        /// „Modell neu erkennen" — vor der Designer-Umstellung ein Lambda an
        /// <c>Click</c>; der Designer verdrahtet ausschließlich Methodenverweise,
        /// deshalb steht der Ablauf jetzt hier.
        /// </summary>
        private void ModellNeu_Click(object sender, EventArgs e)
        {
            // Schlüssel zuerst übernehmen, damit die Abfrage funktioniert
            KiChatService.ApiKey = _schluessel.Text.Trim();
            KiChatService.ModellZuruecksetzen();

            HinweisSetzen(string.Format(MyResource.Resource.KI_EINST_HINWEIS_MODELL_NEU,
                                        KiChatService.MODELL));
        }
    }
}
