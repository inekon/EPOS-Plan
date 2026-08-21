namespace WindowsFormsApplication1
{
    partial class Form_SpeicherVariantenVergleich
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            // Die Fettschrift der aktiven Zeile gehört der Form selbst und nicht einem
            // Steuerelement - die Basisklasse räumt sie deshalb nicht mit ab.
            if (disposing && (m_SchriftAktiv != null))
            {
                m_SchriftAktiv.Dispose();
                m_SchriftAktiv = null;
            }

            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            lbl_Status = new System.Windows.Forms.Label();
            list_Varianten = new System.Windows.Forms.ListView();
            col_Aktiv = new System.Windows.Forms.ColumnHeader();
            col_Bezeichnung = new System.Windows.Forms.ColumnHeader();
            col_Betriebsart = new System.Windows.Forms.ColumnHeader();
            col_Berechnungsart = new System.Windows.Forms.ColumnHeader();
            col_Kapazitaet = new System.Windows.Forms.ColumnHeader();
            col_Leistung = new System.Windows.Forms.ColumnHeader();
            col_Investition = new System.Windows.Forms.ColumnHeader();
            col_Ertrag = new System.Windows.Forms.ColumnHeader();
            col_DeltaJ = new System.Windows.Forms.ColumnHeader();
            col_Amortisation = new System.Windows.Forms.ColumnHeader();
            col_Npv = new System.Windows.Forms.ColumnHeader();
            col_Vollzyklen = new System.Windows.Forms.ColumnHeader();
            lbl_Legende = new System.Windows.Forms.Label();
            lbl_Hinweis = new System.Windows.Forms.Label();
            lbl_Protokollkopf = new System.Windows.Forms.Label();
            tb_Protokoll = new System.Windows.Forms.TextBox();
            btn_Aktiv = new System.Windows.Forms.Button();
            btn_Csv = new System.Windows.Forms.Button();
            btn_Schliessen = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lbl_Status
            // 
            lbl_Status.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbl_Status.AutoEllipsis = true;
            lbl_Status.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            lbl_Status.Location = new System.Drawing.Point(12, 12);
            lbl_Status.Name = "lbl_Status";
            lbl_Status.Size = new System.Drawing.Size(1216, 20);
            lbl_Status.TabIndex = 0;
            lbl_Status.Text = "{0} Varianten gerechnet ({1} ms). Beste nach ΔJ: {2}.";
            // 
            // list_Varianten
            // 
            list_Varianten.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            list_Varianten.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { col_Aktiv, col_Bezeichnung, col_Betriebsart, col_Berechnungsart, col_Kapazitaet, col_Leistung, col_Investition, col_Ertrag, col_DeltaJ, col_Amortisation, col_Npv, col_Vollzyklen });
            list_Varianten.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            list_Varianten.FullRowSelect = true;
            list_Varianten.GridLines = true;
            list_Varianten.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            list_Varianten.Location = new System.Drawing.Point(12, 38);
            list_Varianten.MultiSelect = false;
            list_Varianten.Name = "list_Varianten";
            list_Varianten.ShowItemToolTips = true;
            list_Varianten.Size = new System.Drawing.Size(1216, 300);
            list_Varianten.TabIndex = 1;
            list_Varianten.UseCompatibleStateImageBehavior = false;
            list_Varianten.View = System.Windows.Forms.View.Details;
            list_Varianten.DoubleClick += Varianten_DoubleClick;
            // 
            // col_Aktiv
            // 
            col_Aktiv.Name = "col_Aktiv";
            col_Aktiv.Text = "Aktiv";
            col_Aktiv.Width = 54;
            // 
            // col_Bezeichnung
            // 
            col_Bezeichnung.Name = "col_Bezeichnung";
            col_Bezeichnung.Text = "Bezeichnung";
            col_Bezeichnung.Width = 210;
            // 
            // col_Betriebsart
            // 
            col_Betriebsart.Name = "col_Betriebsart";
            col_Betriebsart.Text = "Betriebsart";
            col_Betriebsart.Width = 100;
            // 
            // col_Berechnungsart
            // 
            col_Berechnungsart.Name = "col_Berechnungsart";
            col_Berechnungsart.Text = "Berechnungsart";
            col_Berechnungsart.Width = 115;
            // 
            // col_Kapazitaet
            // 
            col_Kapazitaet.Name = "col_Kapazitaet";
            col_Kapazitaet.Text = "Kapazität [kWh]";
            col_Kapazitaet.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Kapazitaet.Width = 115;
            // 
            // col_Leistung
            // 
            col_Leistung.Name = "col_Leistung";
            col_Leistung.Text = "Leistung [kW]";
            col_Leistung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Leistung.Width = 102;
            // 
            // col_Investition
            // 
            col_Investition.Name = "col_Investition";
            col_Investition.Text = "Investition [€]";
            col_Investition.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Investition.Width = 105;
            // 
            // col_Ertrag
            // 
            col_Ertrag.Name = "col_Ertrag";
            col_Ertrag.Text = "Ertrag E_a,äq [€/a]";
            col_Ertrag.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Ertrag.Width = 132;
            // 
            // col_DeltaJ
            // 
            col_DeltaJ.Name = "col_DeltaJ";
            col_DeltaJ.Text = "ΔJ [€/a]";
            col_DeltaJ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_DeltaJ.Width = 100;
            // 
            // col_Amortisation
            // 
            col_Amortisation.Name = "col_Amortisation";
            col_Amortisation.Text = "Amortisation [a]";
            col_Amortisation.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Amortisation.Width = 118;
            // 
            // col_Npv
            // 
            col_Npv.Name = "col_Npv";
            col_Npv.Text = "Kapitalwert [€]";
            col_Npv.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Npv.Width = 110;
            // 
            // col_Vollzyklen
            // 
            col_Vollzyklen.Name = "col_Vollzyklen";
            col_Vollzyklen.Text = "Vollzyklen [1/a]";
            col_Vollzyklen.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            col_Vollzyklen.Width = 112;
            // 
            // lbl_Legende
            // 
            lbl_Legende.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbl_Legende.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            lbl_Legende.ForeColor = System.Drawing.SystemColors.GrayText;
            lbl_Legende.Location = new System.Drawing.Point(12, 344);
            lbl_Legende.Name = "lbl_Legende";
            lbl_Legende.Size = new System.Drawing.Size(1216, 18);
            lbl_Legende.TabIndex = 2;
            lbl_Legende.Text = "Grün hinterlegt: beste Variante nach ΔJ = E_a,äq − I·a(i_z, N). Fett: die aktive Variante — sie speist Übersichtsanzeige und Gesamtsimulation.";
            // 
            // lbl_Hinweis
            // 
            lbl_Hinweis.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbl_Hinweis.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            lbl_Hinweis.ForeColor = System.Drawing.Color.Firebrick;
            lbl_Hinweis.Location = new System.Drawing.Point(12, 364);
            lbl_Hinweis.Name = "lbl_Hinweis";
            lbl_Hinweis.Size = new System.Drawing.Size(1216, 32);
            lbl_Hinweis.TabIndex = 3;
            lbl_Hinweis.Text = "Achtung: Keine dieser Varianten ist als aktiv markiert. Solange das so bleibt, rechnet die Gesamtsimulation ersatzweise über alle Speicheranlagen des Projekts zusammen — wählen Sie die gewünschte Zeile und drücken Sie „Als aktiv setzen\".";
            lbl_Hinweis.Visible = false;
            // 
            // lbl_Protokollkopf
            // 
            lbl_Protokollkopf.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lbl_Protokollkopf.Location = new System.Drawing.Point(12, 400);
            lbl_Protokollkopf.Name = "lbl_Protokollkopf";
            lbl_Protokollkopf.Size = new System.Drawing.Size(200, 22);
            lbl_Protokollkopf.TabIndex = 4;
            lbl_Protokollkopf.Text = "Protokoll";
            // 
            // tb_Protokoll
            // 
            tb_Protokoll.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tb_Protokoll.BackColor = System.Drawing.SystemColors.Window;
            tb_Protokoll.Font = new System.Drawing.Font("Segoe UI", 8.75F);
            tb_Protokoll.Location = new System.Drawing.Point(12, 435);
            tb_Protokoll.Multiline = true;
            tb_Protokoll.Name = "tb_Protokoll";
            tb_Protokoll.ReadOnly = true;
            tb_Protokoll.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            tb_Protokoll.Size = new System.Drawing.Size(1216, 153);
            tb_Protokoll.TabIndex = 5;
            // 
            // btn_Aktiv
            // 
            btn_Aktiv.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btn_Aktiv.Location = new System.Drawing.Point(12, 596);
            btn_Aktiv.Name = "btn_Aktiv";
            btn_Aktiv.Size = new System.Drawing.Size(210, 30);
            btn_Aktiv.TabIndex = 6;
            btn_Aktiv.Text = "Als aktiv setzen";
            btn_Aktiv.Click += Aktiv_Click;
            // 
            // btn_Csv
            // 
            btn_Csv.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btn_Csv.Location = new System.Drawing.Point(232, 596);
            btn_Csv.Name = "btn_Csv";
            btn_Csv.Size = new System.Drawing.Size(190, 30);
            btn_Csv.TabIndex = 7;
            btn_Csv.Text = "Raster als CSV …";
            btn_Csv.Click += Csv_Click;
            // 
            // btn_Schliessen
            // 
            btn_Schliessen.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btn_Schliessen.Location = new System.Drawing.Point(1118, 596);
            btn_Schliessen.Name = "btn_Schliessen";
            btn_Schliessen.Size = new System.Drawing.Size(110, 30);
            btn_Schliessen.TabIndex = 8;
            btn_Schliessen.Text = "Schließen";
            btn_Schliessen.Click += Schliessen_Click;
            // 
            // Form_SpeicherVariantenVergleich
            // 
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            CancelButton = btn_Schliessen;
            ClientSize = new System.Drawing.Size(1240, 640);
            Controls.Add(lbl_Status);
            Controls.Add(list_Varianten);
            Controls.Add(lbl_Legende);
            Controls.Add(lbl_Hinweis);
            Controls.Add(lbl_Protokollkopf);
            Controls.Add(tb_Protokoll);
            Controls.Add(btn_Aktiv);
            Controls.Add(btn_Csv);
            Controls.Add(btn_Schliessen);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(900, 480);
            Name = "Form_SpeicherVariantenVergleich";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Variantenvergleich Stromspeicher";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lbl_Status;
        private System.Windows.Forms.ListView list_Varianten;
        private System.Windows.Forms.ColumnHeader col_Aktiv;
        private System.Windows.Forms.ColumnHeader col_Bezeichnung;
        private System.Windows.Forms.ColumnHeader col_Betriebsart;
        private System.Windows.Forms.ColumnHeader col_Berechnungsart;
        private System.Windows.Forms.ColumnHeader col_Kapazitaet;
        private System.Windows.Forms.ColumnHeader col_Leistung;
        private System.Windows.Forms.ColumnHeader col_Investition;
        private System.Windows.Forms.ColumnHeader col_Ertrag;
        private System.Windows.Forms.ColumnHeader col_DeltaJ;
        private System.Windows.Forms.ColumnHeader col_Amortisation;
        private System.Windows.Forms.ColumnHeader col_Npv;
        private System.Windows.Forms.ColumnHeader col_Vollzyklen;
        private System.Windows.Forms.Label lbl_Legende;
        private System.Windows.Forms.Label lbl_Hinweis;
        private System.Windows.Forms.Label lbl_Protokollkopf;
        private System.Windows.Forms.TextBox tb_Protokoll;
        private System.Windows.Forms.Button btn_Aktiv;
        private System.Windows.Forms.Button btn_Csv;
        private System.Windows.Forms.Button btn_Schliessen;
    }
}
