namespace WindowsFormsApplication1
{
    partial class UcWirtschaftlichkeit
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
            base.Dispose(disposing);
        }

        // ---------------------------------------------------------------------------
        // Design-Politur 21.08.2026 — Geometrie
        //
        // Die Feldnamen-Platzhalter sind durch die ECHTEN deutschen Texte aus
        // UcWirtschaftlichkeit.TexteSetzen() ersetzt (Beschriftungen, Knöpfe und die
        // vier Spaltenköpfe der Variantenliste), damit die Entwurfsansicht die Seite
        // zeigt. TexteSetzen() bleibt die einzige Quelle und überschreibt sie beim
        // Aufbau erneut. Ohne Text bleiben lblParameter, lblStatus und die Zellen von
        // grid — deren Inhalt entsteht erst aus den Daten.
        //
        // Am echten Text nachgemessen (Segoe UI 9 pt, 96 dpi): die längste
        // Beschriftung „Vergleichsgruppe (Referenz: Stamm, fest gewählt):“ misst
        // 271 px und steht frei (AutoSize), „Szenario:“ 54 px bei x = 12 lässt der
        // Auswahlliste bei x = 78 einen Abstand von 12 px. Der längste Knopftext
        // „Tarifstruktur…“ misst 80 px und passt in die 110 px Regelbreite.
        //
        // Geändert wurde die Knopfzeile: Sie stand mit uneinheitlichen Breiten
        // (124 / 110 / 110 / 110 / 106) und nur 6 px Abstand zu dicht. Jetzt fünf
        // gleiche Knöpfe 110 x 30 mit 10 px Abstand, rechte Kante unverändert 888
        // (= Size.Width 900 − 12). Der linke Knopf beginnt damit bei x = 298 und hält
        // 10 px Abstand zu lblStatus/progress (rechte Kante 288).
        //   btnTarif      304 -> 298, 124 -> 110 px
        //   btnParameter  434 -> 418
        //   btnVerlauf    550 -> 538
        //   btnBerechnen  666 -> 658
        //   btnSchliessen 782 -> 778, 106 -> 110 px
        // colName 330 -> 470 px: Die vier Spalten füllten von 876 px nur 710 und
        // ließen einen leeren Streifen; die Projektnamen sind die längsten Werte.
        // Size (900 x 536) und alle übrigen Koordinaten bleiben unverändert.
        // ---------------------------------------------------------------------------

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblVarianten = new System.Windows.Forms.Label();
            this.lvVarianten = new System.Windows.Forms.ListView();
            this.colArt = new System.Windows.Forms.ColumnHeader();
            this.colBez = new System.Windows.Forms.ColumnHeader();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colSim = new System.Windows.Forms.ColumnHeader();
            this.lblSzenario = new System.Windows.Forms.Label();
            this.cbSzenario = new System.Windows.Forms.ComboBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblParameter = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.btnTarif = new System.Windows.Forms.Button();
            this.btnParameter = new System.Windows.Forms.Button();
            this.btnVerlauf = new System.Windows.Forms.Button();
            this.btnBerechnen = new System.Windows.Forms.Button();
            this.btnSchliessen = new System.Windows.Forms.Button();
            this.btn_Help = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            //
            // lblVarianten
            //
            this.lblVarianten.AutoSize = true;
            this.lblVarianten.Location = new System.Drawing.Point(12, 12);
            this.lblVarianten.Name = "lblVarianten";
            this.lblVarianten.Text = "Vergleichsgruppe (Referenz: Stamm, fest gewählt):";
            //
            // lvVarianten
            //
            this.lvVarianten.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvVarianten.CheckBoxes = true;
            this.lvVarianten.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colArt,
            this.colBez,
            this.colName,
            this.colSim});
            this.lvVarianten.FullRowSelect = true;
            this.lvVarianten.HideSelection = false;
            this.lvVarianten.Location = new System.Drawing.Point(12, 32);
            this.lvVarianten.MultiSelect = false;
            this.lvVarianten.Name = "lvVarianten";
            this.lvVarianten.Size = new System.Drawing.Size(876, 120);
            this.lvVarianten.View = System.Windows.Forms.View.Details;
            this.lvVarianten.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lvVarianten_ItemCheck);
            //
            // colArt
            //
            this.colArt.Name = "colArt";
            this.colArt.Text = "Art";
            this.colArt.Width = 70;
            //
            // colBez
            //
            this.colBez.Name = "colBez";
            this.colBez.Text = "Bezeichner";
            this.colBez.Width = 180;
            //
            // colName
            //
            this.colName.Name = "colName";
            this.colName.Text = "Projektname";
            this.colName.Width = 470;
            //
            // colSim
            //
            this.colSim.Name = "colSim";
            this.colSim.Text = "Simulation";
            this.colSim.Width = 130;
            //
            // lblSzenario
            //
            this.lblSzenario.AutoSize = true;
            this.lblSzenario.Location = new System.Drawing.Point(12, 164);
            this.lblSzenario.Name = "lblSzenario";
            this.lblSzenario.Text = "Szenario:";
            //
            // cbSzenario
            //
            // Die Einträge stehen NICHT hier: Es sind DB-Persistenzwerte
            // (Tab_ErgebnisWirtschaftlichkeit.Szenario) — siehe
            // UcWirtschaftlichkeit.SzenarienFuellen().
            //
            this.cbSzenario.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSzenario.Location = new System.Drawing.Point(78, 160);
            this.cbSzenario.Name = "cbSzenario";
            this.cbSzenario.Width = 140;
            this.cbSzenario.SelectedIndexChanged += new System.EventHandler(this.cbSzenario_SelectedIndexChanged);
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grid.Location = new System.Drawing.Point(12, 258);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.Size = new System.Drawing.Size(876, 202);
            //
            // lblParameter
            //
            this.lblParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblParameter.ForeColor = System.Drawing.Color.DimGray;
            this.lblParameter.Location = new System.Drawing.Point(12, 468);
            this.lblParameter.Name = "lblParameter";
            this.lblParameter.Size = new System.Drawing.Size(876, 18);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(12, 490);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(276, 18);
            //
            // progress
            //
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.progress.Location = new System.Drawing.Point(12, 512);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(276, 14);
            this.progress.Visible = false;
            //
            // btnTarif
            //
            this.btnTarif.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTarif.Location = new System.Drawing.Point(298, 494);
            this.btnTarif.Name = "btnTarif";
            this.btnTarif.Size = new System.Drawing.Size(110, 30);
            this.btnTarif.Text = "Tarifstruktur…";
            this.btnTarif.Click += new System.EventHandler(this.btnTarif_Click);
            //
            // btnParameter
            //
            this.btnParameter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnParameter.Location = new System.Drawing.Point(418, 494);
            this.btnParameter.Name = "btnParameter";
            this.btnParameter.Size = new System.Drawing.Size(110, 30);
            this.btnParameter.Text = "Parameter…";
            this.btnParameter.Click += new System.EventHandler(this.btnParameter_Click);
            //
            // btnVerlauf
            //
            this.btnVerlauf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVerlauf.Location = new System.Drawing.Point(538, 494);
            this.btnVerlauf.Name = "btnVerlauf";
            this.btnVerlauf.Size = new System.Drawing.Size(110, 30);
            this.btnVerlauf.Text = "Verlauf…";
            this.btnVerlauf.Click += new System.EventHandler(this.btnVerlauf_Click);
            //
            // btnBerechnen
            //
            this.btnBerechnen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBerechnen.Location = new System.Drawing.Point(658, 494);
            this.btnBerechnen.Name = "btnBerechnen";
            this.btnBerechnen.Size = new System.Drawing.Size(110, 30);
            this.btnBerechnen.Text = "Berechnen";
            this.btnBerechnen.Click += new System.EventHandler(this.btnBerechnen_Click);
            //
            // btnSchliessen
            //
            this.btnSchliessen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSchliessen.Location = new System.Drawing.Point(778, 494);
            this.btnSchliessen.Name = "btnSchliessen";
            this.btnSchliessen.Size = new System.Drawing.Size(110, 30);
            this.btnSchliessen.Text = "Schließen";
            this.btnSchliessen.Visible = false;
            this.btnSchliessen.Click += new System.EventHandler(this.btnSchliessen_Click);
            //
            // btn_Help
            //
            this.btn_Help.BackColor = System.Drawing.Color.Transparent;
            this.btn_Help.BackgroundImage = Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Help.Location = new System.Drawing.Point(858, 2);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new System.Drawing.Size(28, 28);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;
            //
            // UcWirtschaftlichkeit
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.lblVarianten);
            this.Controls.Add(this.lvVarianten);
            this.Controls.Add(this.lblSzenario);
            this.Controls.Add(this.cbSzenario);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.lblParameter);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.btnTarif);
            this.Controls.Add(this.btnParameter);
            this.Controls.Add(this.btnVerlauf);
            this.Controls.Add(this.btnBerechnen);
            this.Controls.Add(this.btnSchliessen);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(640, 380);
            this.Name = "UcWirtschaftlichkeit";
            this.Size = new System.Drawing.Size(900, 536);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Label lblVarianten;
        private System.Windows.Forms.ListView lvVarianten;
        private System.Windows.Forms.ColumnHeader colArt;
        private System.Windows.Forms.ColumnHeader colBez;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSim;
        private System.Windows.Forms.Label lblSzenario;
        private System.Windows.Forms.ComboBox cbSzenario;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Label lblParameter;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button btnTarif;
        private System.Windows.Forms.Button btnParameter;
        private System.Windows.Forms.Button btnVerlauf;
        private System.Windows.Forms.Button btnBerechnen;
        private System.Windows.Forms.Button btnSchliessen;
    }
}
