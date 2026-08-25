namespace WindowsFormsApplication1
{
    partial class Form_LeistungspreisReihe
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
            this.pnlKopf = new System.Windows.Forms.Panel();
            this.lblKopfTitel = new System.Windows.Forms.Label();
            this.lblTraeger = new System.Windows.Forms.Label();
            this.lblEinheit = new System.Windows.Forms.Label();
            this.lblJahr = new System.Windows.Forms.Label();
            this.numJahr = new System.Windows.Forms.NumericUpDown();
            this.lblM1 = new System.Windows.Forms.Label();
            this.lblM2 = new System.Windows.Forms.Label();
            this.lblM3 = new System.Windows.Forms.Label();
            this.lblM4 = new System.Windows.Forms.Label();
            this.lblM5 = new System.Windows.Forms.Label();
            this.lblM6 = new System.Windows.Forms.Label();
            this.lblM7 = new System.Windows.Forms.Label();
            this.lblM8 = new System.Windows.Forms.Label();
            this.lblM9 = new System.Windows.Forms.Label();
            this.lblM10 = new System.Windows.Forms.Label();
            this.lblM11 = new System.Windows.Forms.Label();
            this.lblM12 = new System.Windows.Forms.Label();
            this.numM1 = new System.Windows.Forms.NumericUpDown();
            this.numM2 = new System.Windows.Forms.NumericUpDown();
            this.numM3 = new System.Windows.Forms.NumericUpDown();
            this.numM4 = new System.Windows.Forms.NumericUpDown();
            this.numM5 = new System.Windows.Forms.NumericUpDown();
            this.numM6 = new System.Windows.Forms.NumericUpDown();
            this.numM7 = new System.Windows.Forms.NumericUpDown();
            this.numM8 = new System.Windows.Forms.NumericUpDown();
            this.numM9 = new System.Windows.Forms.NumericUpDown();
            this.numM10 = new System.Windows.Forms.NumericUpDown();
            this.numM11 = new System.Windows.Forms.NumericUpDown();
            this.numM12 = new System.Windows.Forms.NumericUpDown();
            this.lblHinweis = new System.Windows.Forms.Label();
            this.btnLoeschen = new System.Windows.Forms.Button();
            this.btnUebernehmen = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.pnlKopf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numJahr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM9)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM10)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM11)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM12)).BeginInit();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlKopf.Controls.Add(this.lblKopfTitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(474, 40);
            this.pnlKopf.TabIndex = 20;
            //
            // lblKopfTitel
            //
            this.lblKopfTitel.AutoSize = true;
            this.lblKopfTitel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblKopfTitel.ForeColor = System.Drawing.Color.White;
            this.lblKopfTitel.Location = new System.Drawing.Point(12, 9);
            this.lblKopfTitel.Name = "lblKopfTitel";
            this.lblKopfTitel.Size = new System.Drawing.Size(233, 20);
            this.lblKopfTitel.TabIndex = 0;
            this.lblKopfTitel.Text = "Saisonale Leistungspreis-Sätze";
            //
            // lblTraeger
            //
            this.lblTraeger.AutoSize = true;
            this.lblTraeger.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblTraeger.Location = new System.Drawing.Point(16, 50);
            this.lblTraeger.Name = "lblTraeger";
            this.lblTraeger.Size = new System.Drawing.Size(59, 17);
            this.lblTraeger.TabIndex = 0;
            this.lblTraeger.Text = "Träger";
            //
            // lblEinheit
            //
            this.lblEinheit.AutoSize = true;
            this.lblEinheit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblEinheit.Location = new System.Drawing.Point(352, 52);
            this.lblEinheit.Name = "lblEinheit";
            this.lblEinheit.Size = new System.Drawing.Size(84, 15);
            this.lblEinheit.TabIndex = 0;
            this.lblEinheit.Text = "€/(kW·Monat)";
            //
            // lblJahr
            //
            this.lblJahr.AutoSize = true;
            this.lblJahr.Location = new System.Drawing.Point(16, 80);
            this.lblJahr.Name = "lblJahr";
            this.lblJahr.Size = new System.Drawing.Size(30, 15);
            this.lblJahr.TabIndex = 0;
            this.lblJahr.Text = "Jahr:";
            //
            // numJahr
            //
            this.numJahr.Location = new System.Drawing.Point(120, 76);
            this.numJahr.Maximum = new decimal(new int[] { 2100, 0, 0, 0 });
            this.numJahr.Minimum = new decimal(new int[] { 2000, 0, 0, 0 });
            this.numJahr.Name = "numJahr";
            this.numJahr.Size = new System.Drawing.Size(90, 23);
            this.numJahr.TabIndex = 1;
            this.numJahr.Value = new decimal(new int[] { 2026, 0, 0, 0 });
            //
            // Monatszeilen: links Januar-Juni, rechts Juli-Dezember. Die Beschriftungen
            // setzt der Konstruktor aus der aktuellen Kultur (keine 12 Resource-Schlüssel).
            //
            this.lblM1.AutoSize = true;
            this.lblM1.Location = new System.Drawing.Point(16, 118);
            this.lblM1.Name = "lblM1";
            this.lblM1.Size = new System.Drawing.Size(45, 15);
            this.lblM1.Text = "Januar:";
            this.numM1.DecimalPlaces = 2;
            this.numM1.Location = new System.Drawing.Point(120, 114);
            this.numM1.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM1.Name = "numM1";
            this.numM1.Size = new System.Drawing.Size(90, 23);
            this.numM1.TabIndex = 2;
            //
            this.lblM2.AutoSize = true;
            this.lblM2.Location = new System.Drawing.Point(16, 150);
            this.lblM2.Name = "lblM2";
            this.lblM2.Size = new System.Drawing.Size(50, 15);
            this.lblM2.Text = "Februar:";
            this.numM2.DecimalPlaces = 2;
            this.numM2.Location = new System.Drawing.Point(120, 146);
            this.numM2.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM2.Name = "numM2";
            this.numM2.Size = new System.Drawing.Size(90, 23);
            this.numM2.TabIndex = 3;
            //
            this.lblM3.AutoSize = true;
            this.lblM3.Location = new System.Drawing.Point(16, 182);
            this.lblM3.Name = "lblM3";
            this.lblM3.Size = new System.Drawing.Size(38, 15);
            this.lblM3.Text = "März:";
            this.numM3.DecimalPlaces = 2;
            this.numM3.Location = new System.Drawing.Point(120, 178);
            this.numM3.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM3.Name = "numM3";
            this.numM3.Size = new System.Drawing.Size(90, 23);
            this.numM3.TabIndex = 4;
            //
            this.lblM4.AutoSize = true;
            this.lblM4.Location = new System.Drawing.Point(16, 214);
            this.lblM4.Name = "lblM4";
            this.lblM4.Size = new System.Drawing.Size(36, 15);
            this.lblM4.Text = "April:";
            this.numM4.DecimalPlaces = 2;
            this.numM4.Location = new System.Drawing.Point(120, 210);
            this.numM4.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM4.Name = "numM4";
            this.numM4.Size = new System.Drawing.Size(90, 23);
            this.numM4.TabIndex = 5;
            //
            this.lblM5.AutoSize = true;
            this.lblM5.Location = new System.Drawing.Point(16, 246);
            this.lblM5.Name = "lblM5";
            this.lblM5.Size = new System.Drawing.Size(30, 15);
            this.lblM5.Text = "Mai:";
            this.numM5.DecimalPlaces = 2;
            this.numM5.Location = new System.Drawing.Point(120, 242);
            this.numM5.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM5.Name = "numM5";
            this.numM5.Size = new System.Drawing.Size(90, 23);
            this.numM5.TabIndex = 6;
            //
            this.lblM6.AutoSize = true;
            this.lblM6.Location = new System.Drawing.Point(16, 278);
            this.lblM6.Name = "lblM6";
            this.lblM6.Size = new System.Drawing.Size(33, 15);
            this.lblM6.Text = "Juni:";
            this.numM6.DecimalPlaces = 2;
            this.numM6.Location = new System.Drawing.Point(120, 274);
            this.numM6.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM6.Name = "numM6";
            this.numM6.Size = new System.Drawing.Size(90, 23);
            this.numM6.TabIndex = 7;
            //
            this.lblM7.AutoSize = true;
            this.lblM7.Location = new System.Drawing.Point(248, 118);
            this.lblM7.Name = "lblM7";
            this.lblM7.Size = new System.Drawing.Size(29, 15);
            this.lblM7.Text = "Juli:";
            this.numM7.DecimalPlaces = 2;
            this.numM7.Location = new System.Drawing.Point(352, 114);
            this.numM7.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM7.Name = "numM7";
            this.numM7.Size = new System.Drawing.Size(90, 23);
            this.numM7.TabIndex = 8;
            //
            this.lblM8.AutoSize = true;
            this.lblM8.Location = new System.Drawing.Point(248, 150);
            this.lblM8.Name = "lblM8";
            this.lblM8.Size = new System.Drawing.Size(48, 15);
            this.lblM8.Text = "August:";
            this.numM8.DecimalPlaces = 2;
            this.numM8.Location = new System.Drawing.Point(352, 146);
            this.numM8.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM8.Name = "numM8";
            this.numM8.Size = new System.Drawing.Size(90, 23);
            this.numM8.TabIndex = 9;
            //
            this.lblM9.AutoSize = true;
            this.lblM9.Location = new System.Drawing.Point(248, 182);
            this.lblM9.Name = "lblM9";
            this.lblM9.Size = new System.Drawing.Size(65, 15);
            this.lblM9.Text = "September:";
            this.numM9.DecimalPlaces = 2;
            this.numM9.Location = new System.Drawing.Point(352, 178);
            this.numM9.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM9.Name = "numM9";
            this.numM9.Size = new System.Drawing.Size(90, 23);
            this.numM9.TabIndex = 10;
            //
            this.lblM10.AutoSize = true;
            this.lblM10.Location = new System.Drawing.Point(248, 214);
            this.lblM10.Name = "lblM10";
            this.lblM10.Size = new System.Drawing.Size(51, 15);
            this.lblM10.Text = "Oktober:";
            this.numM10.DecimalPlaces = 2;
            this.numM10.Location = new System.Drawing.Point(352, 210);
            this.numM10.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM10.Name = "numM10";
            this.numM10.Size = new System.Drawing.Size(90, 23);
            this.numM10.TabIndex = 11;
            //
            this.lblM11.AutoSize = true;
            this.lblM11.Location = new System.Drawing.Point(248, 246);
            this.lblM11.Name = "lblM11";
            this.lblM11.Size = new System.Drawing.Size(63, 15);
            this.lblM11.Text = "November:";
            this.numM11.DecimalPlaces = 2;
            this.numM11.Location = new System.Drawing.Point(352, 242);
            this.numM11.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM11.Name = "numM11";
            this.numM11.Size = new System.Drawing.Size(90, 23);
            this.numM11.TabIndex = 12;
            //
            this.lblM12.AutoSize = true;
            this.lblM12.Location = new System.Drawing.Point(248, 278);
            this.lblM12.Name = "lblM12";
            this.lblM12.Size = new System.Drawing.Size(59, 15);
            this.lblM12.Text = "Dezember:";
            this.numM12.DecimalPlaces = 2;
            this.numM12.Location = new System.Drawing.Point(352, 274);
            this.numM12.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numM12.Name = "numM12";
            this.numM12.Size = new System.Drawing.Size(90, 23);
            this.numM12.TabIndex = 13;
            //
            // lblHinweis
            //
            this.lblHinweis.AutoSize = true;
            this.lblHinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblHinweis.Location = new System.Drawing.Point(16, 312);
            this.lblHinweis.MaximumSize = new System.Drawing.Size(440, 0);
            this.lblHinweis.Name = "lblHinweis";
            this.lblHinweis.Size = new System.Drawing.Size(283, 15);
            this.lblHinweis.TabIndex = 0;
            this.lblHinweis.Text = "Eine gepflegte Reihe gilt vor dem konstanten Satz.";
            //
            // btnLoeschen
            //
            this.btnLoeschen.Location = new System.Drawing.Point(16, 348);
            this.btnLoeschen.Name = "btnLoeschen";
            this.btnLoeschen.Size = new System.Drawing.Size(110, 30);
            this.btnLoeschen.TabIndex = 15;
            this.btnLoeschen.Text = "Reihe löschen";
            this.btnLoeschen.UseVisualStyleBackColor = true;
            this.btnLoeschen.Click += new System.EventHandler(this.btnLoeschen_Click);
            //
            // btnUebernehmen
            //
            this.btnUebernehmen.Location = new System.Drawing.Point(238, 348);
            this.btnUebernehmen.Name = "btnUebernehmen";
            this.btnUebernehmen.Size = new System.Drawing.Size(110, 30);
            this.btnUebernehmen.TabIndex = 16;
            this.btnUebernehmen.Text = "Übernehmen";
            this.btnUebernehmen.UseVisualStyleBackColor = true;
            this.btnUebernehmen.Click += new System.EventHandler(this.btnUebernehmen_Click);
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Location = new System.Drawing.Point(354, 348);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(104, 30);
            this.btnAbbrechen.TabIndex = 17;
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.UseVisualStyleBackColor = true;
            //
            // Form_LeistungspreisReihe
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(474, 392);
            this.Controls.Add(this.pnlKopf);
            this.Controls.Add(this.lblTraeger);
            this.Controls.Add(this.lblEinheit);
            this.Controls.Add(this.lblJahr);
            this.Controls.Add(this.numJahr);
            this.Controls.Add(this.lblM1);
            this.Controls.Add(this.lblM2);
            this.Controls.Add(this.lblM3);
            this.Controls.Add(this.lblM4);
            this.Controls.Add(this.lblM5);
            this.Controls.Add(this.lblM6);
            this.Controls.Add(this.lblM7);
            this.Controls.Add(this.lblM8);
            this.Controls.Add(this.lblM9);
            this.Controls.Add(this.lblM10);
            this.Controls.Add(this.lblM11);
            this.Controls.Add(this.lblM12);
            this.Controls.Add(this.numM1);
            this.Controls.Add(this.numM2);
            this.Controls.Add(this.numM3);
            this.Controls.Add(this.numM4);
            this.Controls.Add(this.numM5);
            this.Controls.Add(this.numM6);
            this.Controls.Add(this.numM7);
            this.Controls.Add(this.numM8);
            this.Controls.Add(this.numM9);
            this.Controls.Add(this.numM10);
            this.Controls.Add(this.numM11);
            this.Controls.Add(this.numM12);
            this.Controls.Add(this.lblHinweis);
            this.Controls.Add(this.btnLoeschen);
            this.Controls.Add(this.btnUebernehmen);
            this.Controls.Add(this.btnAbbrechen);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_LeistungspreisReihe";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Saisonale Leistungspreis-Sätze";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numJahr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM9)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM10)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM11)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numM12)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblKopfTitel;
        private System.Windows.Forms.Label lblTraeger;
        private System.Windows.Forms.Label lblEinheit;
        private System.Windows.Forms.Label lblJahr;
        private System.Windows.Forms.NumericUpDown numJahr;
        private System.Windows.Forms.Label lblM1;
        private System.Windows.Forms.Label lblM2;
        private System.Windows.Forms.Label lblM3;
        private System.Windows.Forms.Label lblM4;
        private System.Windows.Forms.Label lblM5;
        private System.Windows.Forms.Label lblM6;
        private System.Windows.Forms.Label lblM7;
        private System.Windows.Forms.Label lblM8;
        private System.Windows.Forms.Label lblM9;
        private System.Windows.Forms.Label lblM10;
        private System.Windows.Forms.Label lblM11;
        private System.Windows.Forms.Label lblM12;
        private System.Windows.Forms.NumericUpDown numM1;
        private System.Windows.Forms.NumericUpDown numM2;
        private System.Windows.Forms.NumericUpDown numM3;
        private System.Windows.Forms.NumericUpDown numM4;
        private System.Windows.Forms.NumericUpDown numM5;
        private System.Windows.Forms.NumericUpDown numM6;
        private System.Windows.Forms.NumericUpDown numM7;
        private System.Windows.Forms.NumericUpDown numM8;
        private System.Windows.Forms.NumericUpDown numM9;
        private System.Windows.Forms.NumericUpDown numM10;
        private System.Windows.Forms.NumericUpDown numM11;
        private System.Windows.Forms.NumericUpDown numM12;
        private System.Windows.Forms.Label lblHinweis;
        private System.Windows.Forms.Button btnLoeschen;
        private System.Windows.Forms.Button btnUebernehmen;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
