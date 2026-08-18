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
    public class Form_BkUebernahme : Form
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

        private TableLayoutPanel tl;
        private Label lblGegenstand, lblQuelle, lblZiel, lblZielWert, lblQuelleWert;
        private Label lblWertQuelleTitel, lblWertZielTitel, lblKomponenten, lblGrund;
        private ComboBox cbQuelle;
        private TextBox txtKlartext;
        private FlowLayoutPanel pnlKnoepfe;
        private Button btnOk, btnAbbrechen;

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

            InitializeComponent();

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

        // ------------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            this.tl = new TableLayoutPanel();
            this.lblGegenstand = new Label();
            this.lblQuelle = new Label();
            this.cbQuelle = new ComboBox();
            this.lblWertQuelleTitel = new Label();
            this.lblQuelleWert = new Label();
            this.lblZiel = new Label();
            this.lblWertZielTitel = new Label();
            this.lblZielWert = new Label();
            this.lblKomponenten = new Label();
            this.txtKlartext = new TextBox();
            this.lblGrund = new Label();
            this.pnlKnoepfe = new FlowLayoutPanel();
            this.btnOk = new Button();
            this.btnAbbrechen = new Button();
            this.SuspendLayout();

            this.lblGegenstand.Dock = DockStyle.Fill;
            this.lblGegenstand.AutoSize = false;
            this.lblGegenstand.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            this.lblGegenstand.ForeColor = Color.FromArgb(0x1F, 0x4E, 0x79);
            this.lblGegenstand.Margin = new Padding(0, 0, 0, 8);

            this.lblQuelle.Dock = DockStyle.Fill;
            this.lblQuelle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblQuelle.Text = MyResource.Resource.BK_UEB_LBL_QUELLE;

            this.cbQuelle.Dock = DockStyle.Fill;
            this.cbQuelle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbQuelle.Margin = new Padding(0, 2, 0, 6);
            this.cbQuelle.SelectedIndexChanged += new EventHandler(this.cbQuelle_SelectedIndexChanged);

            this.lblWertQuelleTitel.Dock = DockStyle.Fill;
            this.lblWertQuelleTitel.TextAlign = ContentAlignment.MiddleLeft;
            this.lblWertQuelleTitel.Text = MyResource.Resource.BK_UEB_LBL_WERT_QUELLE;

            this.lblQuelleWert.Dock = DockStyle.Fill;
            this.lblQuelleWert.TextAlign = ContentAlignment.MiddleLeft;
            this.lblQuelleWert.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            this.lblZiel.Dock = DockStyle.Fill;
            this.lblZiel.TextAlign = ContentAlignment.MiddleLeft;
            this.lblZiel.AutoEllipsis = true;

            this.lblWertZielTitel.Dock = DockStyle.Fill;
            this.lblWertZielTitel.TextAlign = ContentAlignment.MiddleLeft;
            this.lblWertZielTitel.Text = MyResource.Resource.BK_UEB_LBL_WERT_ZIEL;

            this.lblZielWert.Dock = DockStyle.Fill;
            this.lblZielWert.TextAlign = ContentAlignment.MiddleLeft;

            this.lblKomponenten.Dock = DockStyle.Fill;
            this.lblKomponenten.TextAlign = ContentAlignment.MiddleLeft;
            this.lblKomponenten.ForeColor = Color.DimGray;
            this.lblKomponenten.AutoEllipsis = true;

            this.txtKlartext.Dock = DockStyle.Fill;
            this.txtKlartext.Multiline = true;
            this.txtKlartext.ReadOnly = true;
            this.txtKlartext.ScrollBars = ScrollBars.Vertical;
            this.txtKlartext.BackColor = SystemColors.Window;
            this.txtKlartext.Font = new Font("Segoe UI", 9f);
            this.txtKlartext.Visible = _mitKlartext;

            this.lblGrund.Dock = DockStyle.Fill;
            this.lblGrund.TextAlign = ContentAlignment.MiddleLeft;
            this.lblGrund.ForeColor = Color.Firebrick;
            this.lblGrund.AutoEllipsis = true;

            this.btnOk.Text = MyResource.Resource.BK_UEB_BTN_OK;
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.AutoSize = true;
            this.btnOk.Margin = new Padding(6, 0, 0, 0);
            this.btnOk.MinimumSize = new Size(110, 28);

            this.btnAbbrechen.Text = MyResource.Resource.BK_UEB_BTN_ABBRUCH;
            this.btnAbbrechen.DialogResult = DialogResult.Cancel;
            this.btnAbbrechen.AutoSize = true;
            this.btnAbbrechen.Margin = new Padding(6, 0, 0, 0);
            this.btnAbbrechen.MinimumSize = new Size(110, 28);

            this.pnlKnoepfe.Dock = DockStyle.Fill;
            this.pnlKnoepfe.FlowDirection = FlowDirection.RightToLeft;
            this.pnlKnoepfe.Margin = new Padding(0, 8, 0, 0);
            this.pnlKnoepfe.Controls.Add(this.btnAbbrechen);
            this.pnlKnoepfe.Controls.Add(this.btnOk);

            // Raster: Beschriftung links, Wert rechts.
            this.tl.Dock = DockStyle.Fill;
            this.tl.ColumnCount = 2;
            this.tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132f));
            this.tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.tl.RowCount = 9;
            this.tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 0 Gegenstand
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));            // 1 Quelle
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));            // 2 Quellwert
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));            // 3 Ziel
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));            // 4 Zielwert
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));            // 5 Komponenten
            this.tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));            // 6 Klartext
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));            // 7 Grund
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));            // 8 Knöpfe
            this.tl.Padding = new Padding(12, 10, 12, 10);

            this.tl.Controls.Add(this.lblGegenstand, 0, 0);
            this.tl.SetColumnSpan(this.lblGegenstand, 2);
            this.tl.Controls.Add(this.lblQuelle, 0, 1);
            this.tl.Controls.Add(this.cbQuelle, 1, 1);
            this.tl.Controls.Add(this.lblWertQuelleTitel, 0, 2);
            this.tl.Controls.Add(this.lblQuelleWert, 1, 2);
            this.tl.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = MyResource.Resource.BK_UEB_LBL_ZIEL
            }, 0, 3);
            this.tl.Controls.Add(this.lblZiel, 1, 3);
            this.tl.Controls.Add(this.lblWertZielTitel, 0, 4);
            this.tl.Controls.Add(this.lblZielWert, 1, 4);
            this.tl.Controls.Add(this.lblKomponenten, 0, 5);
            this.tl.SetColumnSpan(this.lblKomponenten, 2);
            this.tl.Controls.Add(this.txtKlartext, 0, 6);
            this.tl.SetColumnSpan(this.txtKlartext, 2);
            this.tl.Controls.Add(this.lblGrund, 0, 7);
            this.tl.SetColumnSpan(this.lblGrund, 2);
            this.tl.Controls.Add(this.pnlKnoepfe, 0, 8);
            this.tl.SetColumnSpan(this.pnlKnoepfe, 2);

            // Im Klartext-Modus tragen die Wertzeilen nichts bei — sie bleiben leer und
            // werden ausgeblendet, damit der Dialog kompakt bleibt.
            if (_mitKlartext)
            {
                this.lblWertQuelleTitel.Visible = false;
                this.lblQuelleWert.Visible = false;
                this.lblWertZielTitel.Visible = false;
                this.lblZielWert.Visible = false;
            }

            this.Controls.Add(this.tl);
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnAbbrechen;
            this.Font = new Font("Segoe UI", 9f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_BkUebernahme";
            this.ClientSize = new Size(520, _mitKlartext ? 380 : 250);
            this.ResumeLayout(false);
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

        /// <summary>Ist der Übernehmen-Knopf freigegeben? (Prüfhilfe)</summary>
        public bool UebernahmeMoeglich { get { return btnOk.Enabled; } }
    }
}
