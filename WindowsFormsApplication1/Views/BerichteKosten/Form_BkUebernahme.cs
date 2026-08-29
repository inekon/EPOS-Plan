using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Bestätigungsdialog der Übernahme auf der Seite „Übersicht" des Reiters
    /// „Berichte &amp; Kosten" — für die Merkmals-Übernahme (Stufe 3) und für die
    /// Komponenten-Übernahme (Stufe 1) derselbe Dialog.
    ///
    /// <para>
    /// EIN DIALOG, ZWEI FÜLLUNGEN. Beide Fälle stellen dieselbe Frage: „woraus, wohin,
    /// und was passiert dann?" Oben deshalb immer die Quellenauswahl (Vorgabe: das
    /// Stammprojekt, alternativ jede andere Variante derselben Gruppe), darunter je nach
    /// Fall die Wertgegenüberstellung Quelle/Ziel oder die Klartext-Zusammenfassung
    /// dessen, was angelegt, ersetzt und entfernt wird. Zwei getrennte Dialoge wären
    /// zweimal dieselbe Quellenauswahl — und die zweite hätte den Fehler.
    /// </para>
    ///
    /// <para>
    /// Der Dialog RECHNET NICHT SELBST. Bei jedem Wechsel der Quelle ruft er den
    /// übergebenen Lader, der die Werte (bzw. die Zusammenfassung) frisch aus der
    /// Datenbank ermittelt — die Prüf- und Schreiblogik bleibt in
    /// <see cref="MerkmalUebernahmeCtrl"/> und <see cref="KomponentenUebernahmeCtrl"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Die Oberfläche steht in <c>Form_BkUebernahme.Designer.cs</c>, weiterhin ohne eigene
    /// <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter. Was von den
    /// Konstruktorparametern abhängt — die Beschriftungen aus <c>titel</c>,
    /// <c>gegenstand</c> und <c>zielName</c> sowie die beiden Erscheinungsformen des
    /// Dialogs (Wertgegenüberstellung bzw. Klartext) — wird im Konstruktor gesetzt.
    /// </remarks>
    public partial class Form_BkUebernahme : Form
    {
        /// <summary>Ein wählbares Quellprojekt (Stamm oder Variante derselben Gruppe).</summary>
        public class Quelle
        {
            public int Id;
            public string Anzeige = "";
            public override string ToString() { return Anzeige; }
        }

        /// <summary>Was der Lader zu einer gewählten Quelle liefert.</summary>
        public class Vorschau
        {
            /// <summary>false = Übernahme aus dieser Quelle nicht möglich (Grund anzeigen).</summary>
            public bool Moeglich;
            public string Grund = "";

            /// <summary>Wertgegenüberstellung (Merkmals-Übernahme).</summary>
            public string WertQuelle = "";
            public string WertZiel = "";

            /// <summary>Betroffene Zeile(n), z. B. „Quellkomponente → Zielkomponente".</summary>
            public string Komponenten = "";

            /// <summary>Mehrzeilige Zusammenfassung (Komponenten-Übernahme).</summary>
            public string Klartext = "";
        }

        private readonly List<Quelle> _quellen;
        private readonly Func<int, Vorschau> _lader;
        private readonly bool _mitKlartext;
        private bool _laedt;

        /// <param name="titel">Fenstertitel (BK_UEB_TITEL_* — Anzeigetext).</param>
        /// <param name="gegenstand">Was übernommen wird: „Gewerk · Merkmal" bzw. „Gewerk".</param>
        /// <param name="zielName">Anzeigename des Zielprojekts.</param>
        /// <param name="quellen">Wählbare Quellen; die erste ist die Vorgabe (Stamm).</param>
        /// <param name="lader">Liefert die Vorschau zur gewählten Quelle.</param>
        /// <param name="mitKlartext">true = Zusammenfassung statt Wertgegenüberstellung.</param>
        public Form_BkUebernahme(string titel, string gegenstand, string zielName,
                                 List<Quelle> quellen, Func<int, Vorschau> lader, bool mitKlartext)
        {
            _quellen = quellen ?? new List<Quelle>();
            _lader = lader;
            _mitKlartext = mitKlartext;

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Anwendung läuft DpiUnaware (app.manifest,
            // Program.SetHighDpiMode), und der bisherige Aufbau setzte AutoScaleMode
            // überhaupt nicht — es fand also ebenfalls keine Skalierung statt. None
            // hält genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();

            // Im Klartext-Modus tragen die Wertzeilen nichts bei — sie bleiben leer und
            // werden ausgeblendet, damit der Dialog kompakt bleibt.
            this.txtKlartext.Visible = _mitKlartext;
            if (_mitKlartext)
            {
                this.lblWertQuelleTitel.Visible = false;
                this.lblQuelleWert.Visible = false;
                this.lblWertZielTitel.Visible = false;
                this.lblZielWert.Visible = false;
            }
            else
            {
                // Der Designer hält die hohe Fassung (Klartextfeld) fest; ohne Klartext
                // fallen die Zusammenfassung und ihr Platz weg.
                this.ClientSize = new Size(520, 250);
            }

            this.Text = titel ?? "";
            this.lblGegenstand.Text = gegenstand ?? "";
            this.lblZiel.Text = zielName ?? "";

            _laedt = true;
            try
            {
                foreach (Quelle q in _quellen) cbQuelle.Items.Add(q);
                if (cbQuelle.Items.Count > 0) cbQuelle.SelectedIndex = 0;
            }
            finally { _laedt = false; }

            Auffrischen();
        }

        /// <summary>ID der gewählten Quelle (-1 = keine).</summary>
        public int GewaehlteQuelleId
        {
            get { Quelle q = cbQuelle.SelectedItem as Quelle; return q != null ? q.Id : -1; }
        }

        // ------------------------------------------------------------------- Texte

        /// <summary>
        /// Setzt die festen sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            lblQuelle.Text = MyResource.Resource.BK_UEB_LBL_QUELLE;
            lblWertQuelleTitel.Text = MyResource.Resource.BK_UEB_LBL_WERT_QUELLE;
            lblZielTitel.Text = MyResource.Resource.BK_UEB_LBL_ZIEL;
            lblWertZielTitel.Text = MyResource.Resource.BK_UEB_LBL_WERT_ZIEL;
            btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            btnAbbrechen.Text = MyResource.Resource.BK_UEB_BTN_ABBRUCH;
        }

        // ---------------------------------------------------------------- Ereignisse

        private void cbQuelle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_laedt) return;
            Auffrischen();
        }

        /// <summary>Werte der gewählten Quelle frisch laden (öffentlich für die Prüfhilfe).</summary>
        public void Auffrischen()
        {
            int id = GewaehlteQuelleId;
            Vorschau v = (_lader != null && id > 0) ? _lader(id) : null;
            if (v == null) v = new Vorschau { Moeglich = false, Grund = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE };

            lblQuelleWert.Text = v.WertQuelle ?? "";
            lblZielWert.Text = v.WertZiel ?? "";
            lblKomponenten.Text = v.Komponenten ?? "";
            txtKlartext.Text = (v.Klartext ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");
            lblGrund.Text = v.Moeglich ? "" : (v.Grund ?? "");
            btnOk.Enabled = v.Moeglich;
        }

        /// <summary>Text der Zusammenfassung bzw. Begründung (Prüfhilfe).</summary>
        public string VorschauText
        {
            get { return _mitKlartext ? txtKlartext.Text : (lblQuelleWert.Text + " → " + lblZielWert.Text); }
        }

        /// <summary>Meldung, warum die Übernahme gesperrt ist (leer = möglich; Prüfhilfe).</summary>
        public string GrundText { get { return lblGrund.Text; } }

        /// <summary>Ist der OK-Knopf freigegeben? (Prüfhilfe)</summary>
        public bool UebernahmeMoeglich { get { return btnOk.Enabled; } }
    }
}
