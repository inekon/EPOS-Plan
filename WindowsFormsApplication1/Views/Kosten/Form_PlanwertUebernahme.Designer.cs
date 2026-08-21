namespace WindowsFormsApplication1
{
    partial class Form_PlanwertUebernahme
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grid = new System.Windows.Forms.DataGridView();
            this.spalteAnlage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.spalteBasis = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.spalteBetrag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.spalteHerkunft = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblKopf = new System.Windows.Forms.Label();
            this.lblNeben = new System.Windows.Forms.Label();
            this.lblSumme = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbruch = new System.Windows.Forms.Button();
            this.panelMitte = new System.Windows.Forms.Panel();
            this.panelUnten = new System.Windows.Forms.Panel();
            this.panelFuss = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.panelMitte.SuspendLayout();
            this.panelUnten.SuspendLayout();
            this.panelFuss.SuspendLayout();
            this.SuspendLayout();
            //
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.spalteAnlage,
            this.spalteBasis,
            this.spalteBetrag,
            this.spalteHerkunft});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grid.Margin = new System.Windows.Forms.Padding(10);
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.CurrentCellDirtyStateChanged += new System.EventHandler(this.grid_CurrentCellDirtyStateChanged);
            this.grid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellValueChanged);
            //
            // spalteAnlage
            //
            this.spalteAnlage.FillWeight = 130F;
            this.spalteAnlage.HeaderText = "Anlage";
            this.spalteAnlage.Name = "spalteAnlage";
            this.spalteAnlage.ReadOnly = true;
            //
            // spalteBasis
            //
            this.spalteBasis.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.spalteBasis.FillWeight = 110F;
            this.spalteBasis.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.spalteBasis.HeaderText = "Kostenbasis";
            this.spalteBasis.Name = "spalteBasis";
            //
            // spalteBetrag
            //
            this.spalteBetrag.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.spalteBetrag.FillWeight = 80F;
            this.spalteBetrag.HeaderText = "Betrag [€]";
            this.spalteBetrag.Name = "spalteBetrag";
            this.spalteBetrag.ReadOnly = true;
            //
            // spalteHerkunft
            //
            this.spalteHerkunft.FillWeight = 150F;
            this.spalteHerkunft.HeaderText = "Herkunft";
            this.spalteHerkunft.Name = "spalteHerkunft";
            this.spalteHerkunft.ReadOnly = true;
            //
            // lblKopf
            //
            this.lblKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblKopf.Name = "lblKopf";
            this.lblKopf.Padding = new System.Windows.Forms.Padding(10, 8, 10, 0);
            this.lblKopf.Size = new System.Drawing.Size(760, 44);
            this.lblKopf.Text = "Je Anlage festlegen, welcher Wert als Investition gilt. Die Nebenkosten entstehen " +
    "als eigene Zeilen.";
            //
            // lblNeben
            //
            this.lblNeben.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNeben.Name = "lblNeben";
            this.lblNeben.Padding = new System.Windows.Forms.Padding(10, 6, 10, 0);
            this.lblNeben.Size = new System.Drawing.Size(760, 40);
            this.lblNeben.Text = "Nebenkosten — je Posten eine eigene Zeile:";
            //
            // lblSumme
            //
            this.lblSumme.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSumme.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSumme.Name = "lblSumme";
            this.lblSumme.Padding = new System.Windows.Forms.Padding(10, 4, 10, 0);
            this.lblSumme.Size = new System.Drawing.Size(760, 26);
            this.lblSumme.Text = "Hauptposition: {0} €";
            //
            // btnOk
            //
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(498, 6);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(120, 30);
            this.btnOk.Text = "OK";
            //
            // btnAbbruch
            //
            this.btnAbbruch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbruch.Location = new System.Drawing.Point(628, 6);
            this.btnAbbruch.Name = "btnAbbruch";
            this.btnAbbruch.Size = new System.Drawing.Size(120, 30);
            this.btnAbbruch.Text = "Abbrechen";
            //
            // panelMitte
            //
            this.panelMitte.Controls.Add(this.grid);
            this.panelMitte.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMitte.Name = "panelMitte";
            this.panelMitte.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            //
            // panelUnten
            //
            this.panelUnten.Size = new System.Drawing.Size(760, 70);
            this.panelUnten.Controls.Add(this.lblSumme);
            this.panelUnten.Controls.Add(this.lblNeben);
            this.panelUnten.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelUnten.Name = "panelUnten";
            //
            // panelFuss
            //
            this.panelFuss.Size = new System.Drawing.Size(760, 42);
            this.panelFuss.Controls.Add(this.btnOk);
            this.panelFuss.Controls.Add(this.btnAbbruch);
            this.panelFuss.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFuss.Name = "panelFuss";
            //
            // Form_PlanwertUebernahme
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnAbbruch;
            this.ClientSize = new System.Drawing.Size(760, 390);
            this.Controls.Add(this.panelMitte);
            this.Controls.Add(this.panelUnten);
            this.Controls.Add(this.panelFuss);
            this.Controls.Add(this.lblKopf);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 300);
            this.Name = "Form_PlanwertUebernahme";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Technik-Planwert übernehmen — {0}";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.panelMitte.ResumeLayout(false);
            this.panelMitte.PerformLayout();
            this.panelUnten.ResumeLayout(false);
            this.panelUnten.PerformLayout();
            this.panelFuss.ResumeLayout(false);
            this.panelFuss.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn spalteAnlage;
        private System.Windows.Forms.DataGridViewComboBoxColumn spalteBasis;
        private System.Windows.Forms.DataGridViewTextBoxColumn spalteBetrag;
        private System.Windows.Forms.DataGridViewTextBoxColumn spalteHerkunft;
        private System.Windows.Forms.Label lblKopf;
        private System.Windows.Forms.Label lblNeben;
        private System.Windows.Forms.Label lblSumme;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbruch;
        private System.Windows.Forms.Panel panelMitte;
        private System.Windows.Forms.Panel panelUnten;
        private System.Windows.Forms.Panel panelFuss;
    }
}
