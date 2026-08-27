namespace WindowsFormsApplication1
{
    partial class ucErtragBonus
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpKwkg = new System.Windows.Forms.GroupBox();
            this.lblKwkgTitel = new System.Windows.Forms.Label();
            this.lblEinspeisung = new System.Windows.Forms.Label();
            this.lblEigen = new System.Windows.Forms.Label();
            this.lblSonderregel = new System.Windows.Forms.Label();
            this.grpDauer = new System.Windows.Forms.GroupBox();
            this.lblDauer = new System.Windows.Forms.Label();
            this.grpSteuern = new System.Windows.Forms.GroupBox();
            this.lblSteuern = new System.Windows.Forms.Label();
            this.grpVerweise = new System.Windows.Forms.GroupBox();
            this.lblFk7 = new System.Windows.Forms.Label();
            this.btnGesetze = new System.Windows.Forms.Button();
            this.grpPv = new System.Windows.Forms.GroupBox();
            this.lblPvErklaerung = new System.Windows.Forms.Label();
            this.lblPvProjekt = new System.Windows.Forms.Label();
            this.cmbPvProjekt = new System.Windows.Forms.ComboBox();
            this.btnPvOeffnen = new System.Windows.Forms.Button();
            this.lblLeer = new System.Windows.Forms.Label();
            this.grpKwkg.SuspendLayout();
            this.grpDauer.SuspendLayout();
            this.grpSteuern.SuspendLayout();
            this.grpVerweise.SuspendLayout();
            this.grpPv.SuspendLayout();
            this.SuspendLayout();
            //
            // grpKwkg
            //
            this.grpKwkg.Controls.Add(this.lblKwkgTitel);
            this.grpKwkg.Controls.Add(this.lblEinspeisung);
            this.grpKwkg.Controls.Add(this.lblEigen);
            this.grpKwkg.Controls.Add(this.lblSonderregel);
            this.grpKwkg.Location = new System.Drawing.Point(12, 12);
            this.grpKwkg.Name = "grpKwkg";
            this.grpKwkg.Size = new System.Drawing.Size(470, 330);
            this.grpKwkg.TabIndex = 0;
            this.grpKwkg.TabStop = false;
            this.grpKwkg.Text = "KWKG-Zuschlag (§ 7 KWKG 2025) — Anzeige aus dem Gesetzeskatalog";
            //
            this.lblKwkgTitel.AutoSize = true;
            this.lblKwkgTitel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblKwkgTitel.Location = new System.Drawing.Point(12, 22);
            this.lblKwkgTitel.Name = "lblKwkgTitel";
            this.lblKwkgTitel.Size = new System.Drawing.Size(200, 15);
            this.lblKwkgTitel.Text = "Eingespeister KWK-Strom (Tranchen):";
            //
            this.lblEinspeisung.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblEinspeisung.Location = new System.Drawing.Point(12, 42);
            this.lblEinspeisung.Name = "lblEinspeisung";
            this.lblEinspeisung.Size = new System.Drawing.Size(446, 108);
            this.lblEinspeisung.Text = "—";
            //
            this.lblSonderregel.Location = new System.Drawing.Point(12, 156);
            this.lblSonderregel.Name = "lblSonderregel";
            this.lblSonderregel.Size = new System.Drawing.Size(446, 34);
            this.lblSonderregel.Text = "—";
            //
            this.lblEigen.Location = new System.Drawing.Point(12, 196);
            this.lblEigen.Name = "lblEigen";
            this.lblEigen.Size = new System.Drawing.Size(446, 122);
            this.lblEigen.Text = "—";
            //
            // grpDauer
            //
            this.grpDauer.Controls.Add(this.lblDauer);
            this.grpDauer.Location = new System.Drawing.Point(12, 352);
            this.grpDauer.Name = "grpDauer";
            this.grpDauer.Size = new System.Drawing.Size(470, 120);
            this.grpDauer.TabIndex = 1;
            this.grpDauer.TabStop = false;
            this.grpDauer.Text = "Förderdauer und Jahresdeckel";
            //
            this.lblDauer.Location = new System.Drawing.Point(12, 22);
            this.lblDauer.Name = "lblDauer";
            this.lblDauer.Size = new System.Drawing.Size(446, 90);
            this.lblDauer.Text = "—";
            //
            // grpSteuern
            //
            this.grpSteuern.Controls.Add(this.lblSteuern);
            this.grpSteuern.Location = new System.Drawing.Point(498, 12);
            this.grpSteuern.Name = "grpSteuern";
            this.grpSteuern.Size = new System.Drawing.Size(476, 214);
            this.grpSteuern.TabIndex = 2;
            this.grpSteuern.TabStop = false;
            this.grpSteuern.Text = "Steuervergünstigungen (HF6, Sätze aus dem Gesetzeskatalog)";
            //
            this.lblSteuern.Location = new System.Drawing.Point(12, 22);
            this.lblSteuern.Name = "lblSteuern";
            this.lblSteuern.Size = new System.Drawing.Size(452, 184);
            this.lblSteuern.Text = "—";
            //
            // grpVerweise
            //
            this.grpVerweise.Controls.Add(this.lblFk7);
            this.grpVerweise.Controls.Add(this.btnGesetze);
            this.grpVerweise.Location = new System.Drawing.Point(498, 236);
            this.grpVerweise.Name = "grpVerweise";
            this.grpVerweise.Size = new System.Drawing.Size(476, 236);
            this.grpVerweise.TabIndex = 3;
            this.grpVerweise.TabStop = false;
            this.grpVerweise.Text = "Pflegeorte (eine Wahrheit je Größe)";
            //
            this.lblFk7.Location = new System.Drawing.Point(12, 22);
            this.lblFk7.Name = "lblFk7";
            this.lblFk7.Size = new System.Drawing.Size(452, 160);
            this.lblFk7.Text = "—";
            //
            this.btnGesetze.Location = new System.Drawing.Point(12, 192);
            this.btnGesetze.Name = "btnGesetze";
            this.btnGesetze.Size = new System.Drawing.Size(200, 30);
            this.btnGesetze.TabIndex = 0;
            this.btnGesetze.Text = "Gesetzesparameter…";
            this.btnGesetze.UseVisualStyleBackColor = true;
            this.btnGesetze.Click += new System.EventHandler(this.btnGesetze_Click);
            //
            // grpPv
            //
            this.grpPv.Controls.Add(this.lblPvErklaerung);
            this.grpPv.Controls.Add(this.lblPvProjekt);
            this.grpPv.Controls.Add(this.cmbPvProjekt);
            this.grpPv.Controls.Add(this.btnPvOeffnen);
            this.grpPv.Location = new System.Drawing.Point(12, 12);
            this.grpPv.Name = "grpPv";
            this.grpPv.Size = new System.Drawing.Size(962, 190);
            this.grpPv.TabIndex = 4;
            this.grpPv.TabStop = false;
            this.grpPv.Text = "PV-Vergütung (EEG) — eine Vergütungswahrheit (V4/F7)";
            this.grpPv.Visible = false;
            //
            this.lblPvErklaerung.Location = new System.Drawing.Point(12, 24);
            this.lblPvErklaerung.Name = "lblPvErklaerung";
            this.lblPvErklaerung.Size = new System.Drawing.Size(936, 66);
            this.lblPvErklaerung.Text = "—";
            //
            this.lblPvProjekt.AutoSize = true;
            this.lblPvProjekt.Location = new System.Drawing.Point(12, 102);
            this.lblPvProjekt.Name = "lblPvProjekt";
            this.lblPvProjekt.Size = new System.Drawing.Size(90, 15);
            this.lblPvProjekt.Text = "Stammprojekt:";
            //
            this.cmbPvProjekt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPvProjekt.Location = new System.Drawing.Point(120, 98);
            this.cmbPvProjekt.Name = "cmbPvProjekt";
            this.cmbPvProjekt.Size = new System.Drawing.Size(360, 23);
            this.cmbPvProjekt.TabIndex = 0;
            //
            this.btnPvOeffnen.Location = new System.Drawing.Point(496, 95);
            this.btnPvOeffnen.Name = "btnPvOeffnen";
            this.btnPvOeffnen.Size = new System.Drawing.Size(220, 30);
            this.btnPvOeffnen.TabIndex = 1;
            this.btnPvOeffnen.Text = "PV-Vergütungsdialog öffnen…";
            this.btnPvOeffnen.UseVisualStyleBackColor = true;
            this.btnPvOeffnen.Click += new System.EventHandler(this.btnPvOeffnen_Click);
            //
            // lblLeer
            //
            this.lblLeer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLeer.ForeColor = System.Drawing.Color.DimGray;
            this.lblLeer.Location = new System.Drawing.Point(0, 0);
            this.lblLeer.Name = "lblLeer";
            this.lblLeer.Size = new System.Drawing.Size(996, 619);
            this.lblLeer.TabIndex = 5;
            this.lblLeer.Text = "—";
            this.lblLeer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblLeer.Visible = false;
            //
            // ucErtragBonus
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpPv);
            this.Controls.Add(this.grpKwkg);
            this.Controls.Add(this.grpDauer);
            this.Controls.Add(this.grpSteuern);
            this.Controls.Add(this.grpVerweise);
            this.Controls.Add(this.lblLeer);
            this.Name = "ucErtragBonus";
            this.Size = new System.Drawing.Size(996, 619);
            this.grpKwkg.ResumeLayout(false);
            this.grpKwkg.PerformLayout();
            this.grpDauer.ResumeLayout(false);
            this.grpSteuern.ResumeLayout(false);
            this.grpVerweise.ResumeLayout(false);
            this.grpPv.ResumeLayout(false);
            this.grpPv.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpKwkg;
        private System.Windows.Forms.Label lblKwkgTitel;
        private System.Windows.Forms.Label lblEinspeisung;
        private System.Windows.Forms.Label lblEigen;
        private System.Windows.Forms.Label lblSonderregel;
        private System.Windows.Forms.GroupBox grpDauer;
        private System.Windows.Forms.Label lblDauer;
        private System.Windows.Forms.GroupBox grpSteuern;
        private System.Windows.Forms.Label lblSteuern;
        private System.Windows.Forms.GroupBox grpVerweise;
        private System.Windows.Forms.Label lblFk7;
        private System.Windows.Forms.Button btnGesetze;
        private System.Windows.Forms.GroupBox grpPv;
        private System.Windows.Forms.Label lblPvErklaerung;
        private System.Windows.Forms.Label lblPvProjekt;
        private System.Windows.Forms.ComboBox cmbPvProjekt;
        private System.Windows.Forms.Button btnPvOeffnen;
        private System.Windows.Forms.Label lblLeer;
    }
}
