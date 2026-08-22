using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Variantentest
    {
        private System.ComponentModel.IContainer components = null;

        // Steuerelemente (im Designer editierbar)
        private Label lblStamm;
        private ComboBox cbStamm;
        private CheckBox chkNurStaemme;
        private Label lblListe;
        private ListView lvAuswahl;
        private ColumnHeader colArt;
        private ColumnHeader colBezeichner;
        private ColumnHeader colProjektname;
        private Label lblBez;
        private TextBox txtBezeichner;
        private Button btnAnlegen;
        private Button btnLoeschen;
        private Button btnSimulieren;
        private Button btnVergleich;
        private Button btnBericht;
        private Button btnWirtschaft;
        private Button btn_Help;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblStamm = new Label();
            this.cbStamm = new ComboBox();
            this.chkNurStaemme = new CheckBox();
            this.lblListe = new Label();
            this.lvAuswahl = new ListView();
            this.colArt = new ColumnHeader();
            this.colBezeichner = new ColumnHeader();
            this.colProjektname = new ColumnHeader();
            this.lblBez = new Label();
            this.txtBezeichner = new TextBox();
            this.btnAnlegen = new Button();
            this.btnLoeschen = new Button();
            this.btnSimulieren = new Button();
            this.btnVergleich = new Button();
            this.btnBericht = new Button();
            this.btnWirtschaft = new Button();
            this.lblStatus = new Label();
            this.btn_Help = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lblStamm
            this.lblStamm.AutoSize = true;
            this.lblStamm.Location = new Point(12, 15);
            this.lblStamm.Name = "lblStamm";
            this.lblStamm.Text = "Stammprojekt:";

            // cbStamm
            this.cbStamm.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.cbStamm.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbStamm.Location = new Point(110, 12);
            this.cbStamm.Name = "cbStamm";
            this.cbStamm.Width = 320;
            this.cbStamm.SelectedIndexChanged += new EventHandler(this.cbStamm_SelectedIndexChanged);

            // chkNurStaemme
            this.chkNurStaemme.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.chkNurStaemme.AutoSize = true;
            this.chkNurStaemme.Location = new Point(440, 14);
            this.chkNurStaemme.Name = "chkNurStaemme";
            this.chkNurStaemme.Text = "nur Stammprojekte";
            this.chkNurStaemme.CheckedChanged += new EventHandler(this.chkNurStaemme_CheckedChanged);

            // lblListe
            this.lblListe.AutoSize = true;
            this.lblListe.Location = new Point(12, 48);
            this.lblListe.Name = "lblListe";
            this.lblListe.Text = "Stamm + Varianten:";

            // lvAuswahl
            this.lvAuswahl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lvAuswahl.Columns.AddRange(new ColumnHeader[] { this.colArt, this.colBezeichner, this.colProjektname });
            this.lvAuswahl.FullRowSelect = true;
            this.lvAuswahl.HideSelection = false;
            this.lvAuswahl.Location = new Point(12, 68);
            this.lvAuswahl.MultiSelect = false;
            this.lvAuswahl.Name = "lvAuswahl";
            this.lvAuswahl.Size = new Size(536, 250);
            this.lvAuswahl.View = View.Details;
            this.lvAuswahl.SelectedIndexChanged += new EventHandler(this.lvAuswahl_SelectedIndexChanged);

            // colArt / colBezeichner / colProjektname
            this.colArt.Text = "Art";
            this.colArt.Width = 90;
            this.colBezeichner.Text = "Bezeichner";
            this.colBezeichner.Width = 200;
            this.colProjektname.Text = "Projektname";
            this.colProjektname.Width = 210;

            // lblBez
            this.lblBez.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.lblBez.AutoSize = true;
            this.lblBez.Location = new Point(12, 330);
            this.lblBez.Name = "lblBez";
            this.lblBez.Text = "Bezeichner:";

            // txtBezeichner
            this.txtBezeichner.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.txtBezeichner.Location = new Point(90, 327);
            this.txtBezeichner.Name = "txtBezeichner";
            this.txtBezeichner.Width = 250;

            // btnAnlegen
            this.btnAnlegen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnAnlegen.Location = new Point(350, 325);
            this.btnAnlegen.Name = "btnAnlegen";
            this.btnAnlegen.Size = new Size(198, 23);
            this.btnAnlegen.Text = "Variante anlegen";
            this.btnAnlegen.Click += new EventHandler(this.btnAnlegen_Click);

            // btnLoeschen
            this.btnLoeschen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnLoeschen.Location = new Point(350, 358);
            this.btnLoeschen.Name = "btnLoeschen";
            this.btnLoeschen.Size = new Size(198, 23);
            this.btnLoeschen.Text = "Variante löschen";
            this.btnLoeschen.Click += new EventHandler(this.btnLoeschen_Click);

            // btnSimulieren
            this.btnSimulieren.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnSimulieren.Location = new Point(12, 400);
            this.btnSimulieren.Name = "btnSimulieren";
            this.btnSimulieren.Size = new Size(260, 34);
            this.btnSimulieren.Text = "Simulation starten";
            this.btnSimulieren.Click += new EventHandler(this.btnSimulieren_Click);

            // btnVergleich
            this.btnVergleich.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnVergleich.Location = new Point(288, 400);
            this.btnVergleich.Name = "btnVergleich";
            this.btnVergleich.Size = new Size(260, 34);
            this.btnVergleich.Text = "Projektvergleich + Bericht (alt)";
            this.btnVergleich.Click += new EventHandler(this.btnVergleich_Click);

            // btnBericht (neu, Phase 1 Berichtsmodul — öffnet Form_Bericht)
            this.btnBericht.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.btnBericht.Location = new Point(12, 440);
            this.btnBericht.Name = "btnBericht";
            this.btnBericht.Size = new Size(536, 34);
            this.btnBericht.Text = "Bericht erstellen…";
            this.btnBericht.Click += new EventHandler(this.btnBericht_Click);

            // btnWirtschaft (neu, Phase 6 — öffnet Form_Wirtschaftlichkeit)
            this.btnWirtschaft.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.btnWirtschaft.Location = new Point(12, 480);
            this.btnWirtschaft.Name = "btnWirtschaft";
            this.btnWirtschaft.Size = new Size(536, 34);
            this.btnWirtschaft.Text = "Wirtschaftlichkeit (Kapitalwertmethode)…";
            this.btnWirtschaft.Click += new EventHandler(this.btnWirtschaft_Click);

            // lblStatus
            this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(12, 358);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(320, 20);

            // btn_Help
            this.btn_Help.BackColor = Color.Transparent;
            this.btn_Help.BackgroundImage = Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = ImageLayout.Zoom;
            this.btn_Help.Cursor = Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = FlatStyle.Flat;
            this.btn_Help.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btn_Help.Location = new Point(520, 38);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new Size(28, 28);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;

            // Form_Variantentest
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(560, 540);
            this.MinimumSize = new Size(500, 480);
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Name = "Form_Variantentest";
            this.Text = "Projektvarianten";
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.lblStamm);
            this.Controls.Add(this.cbStamm);
            this.Controls.Add(this.chkNurStaemme);
            this.Controls.Add(this.lblListe);
            this.Controls.Add(this.lvAuswahl);
            this.Controls.Add(this.lblBez);
            this.Controls.Add(this.txtBezeichner);
            this.Controls.Add(this.btnAnlegen);
            this.Controls.Add(this.btnLoeschen);
            this.Controls.Add(this.btnSimulieren);
            this.Controls.Add(this.btnVergleich);
            this.Controls.Add(this.btnBericht);
            this.Controls.Add(this.btnWirtschaft);
            this.Controls.Add(this.lblStatus);
            this.Load += new EventHandler(this.Form_Variantentest_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
