namespace WindowsFormsApplication1
{
    partial class ucFuelSettings
    {
        private void InitializeComponent()
        {
            this.lblCarrierName = new System.Windows.Forms.Label();
            this.lblResult = new System.Windows.Forms.Label();
            this.lblFormula = new System.Windows.Forms.Label();
            this.cmbUnit = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblBasisnheit = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btn_Save = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.label9 = new System.Windows.Forms.Label();
            this.lblGruppe = new System.Windows.Forms.Label();
            this.groupBox_Formel = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numCO2 = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.numNOx = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.numSO2 = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbl_Unit_Heizwert = new System.Windows.Forms.Label();
            this.lbl_Leistungspreis = new System.Windows.Forms.Label();
            this.numHeizwert = new System.Windows.Forms.NumericUpDown();
            this.lbl_Unit_Brennwert = new System.Windows.Forms.Label();
            this.lbl_Brennwert = new System.Windows.Forms.Label();
            this.numBrennwert = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.lbl_Unit_Leistungspreis = new System.Windows.Forms.Label();
            this.lbl_Unit_Arbeitspreis = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbl_Heizwert = new System.Windows.Forms.Label();
            this.numArbeitspreis = new System.Windows.Forms.NumericUpDown();
            this.numLeistungspreis = new System.Windows.Forms.NumericUpDown();
            this.numGrundpreis = new System.Windows.Forms.NumericUpDown();
            this.dtpValidFrom = new System.Windows.Forms.DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.groupBox_Formel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCO2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNOx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSO2)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numHeizwert)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrennwert)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungspreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).BeginInit();
            this.SuspendLayout();
            // 
            // lblCarrierName
            // 
            this.lblCarrierName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.lblCarrierName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCarrierName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCarrierName.ForeColor = System.Drawing.Color.White;
            this.lblCarrierName.Location = new System.Drawing.Point(0, 0);
            this.lblCarrierName.Name = "lblCarrierName";
            this.lblCarrierName.Padding = new System.Windows.Forms.Padding(10, 5, 5, 5);
            this.lblCarrierName.Size = new System.Drawing.Size(565, 35);
            this.lblCarrierName.TabIndex = 0;
            this.lblCarrierName.Text = "Energieträger";
            // 
            // lblResult
            // 
            this.lblResult.Location = new System.Drawing.Point(124, 18);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(172, 23);
            this.lblResult.TabIndex = 0;
            this.lblResult.Text = "-";
            // 
            // lblFormula
            // 
            this.lblFormula.Location = new System.Drawing.Point(124, 45);
            this.lblFormula.Name = "lblFormula";
            this.lblFormula.Size = new System.Drawing.Size(299, 23);
            this.lblFormula.TabIndex = 0;
            this.lblFormula.Text = "-";
            // 
            // cmbUnit
            // 
            this.cmbUnit.Location = new System.Drawing.Point(117, 90);
            this.cmbUnit.Name = "cmbUnit";
            this.cmbUnit.Size = new System.Drawing.Size(57, 25);
            this.cmbUnit.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Preis pro kWh:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Formel:";
            // 
            // lblBasisnheit
            // 
            this.lblBasisnheit.AutoSize = true;
            this.lblBasisnheit.Location = new System.Drawing.Point(114, 122);
            this.lblBasisnheit.Name = "lblBasisnheit";
            this.lblBasisnheit.Size = new System.Drawing.Size(22, 17);
            this.lblBasisnheit.TabIndex = 7;
            this.lblBasisnheit.Text = "kg";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 93);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 17);
            this.label6.TabIndex = 8;
            this.label6.Text = "Preisbasis";
            // 
            // btn_Save
            // 
            this.btn_Save.AutoSize = true;
            this.btn_Save.Location = new System.Drawing.Point(133, 420);
            this.btn_Save.MinimumSize = new System.Drawing.Size(75, 23);
            this.btn_Save.Name = "btn_Save";
            this.btn_Save.Size = new System.Drawing.Size(96, 27);
            this.btn_Save.TabIndex = 9;
            this.btn_Save.Text = "💾 Speichern";
            this.btn_Save.UseVisualStyleBackColor = true;
            this.btn_Save.Click += new System.EventHandler(this.btn_Save_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(19, 122);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(78, 17);
            this.label8.TabIndex = 13;
            this.label8.Text = "Basiseinheit:";
            // 
            // dgvHistory
            // 
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Location = new System.Drawing.Point(17, 452);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.Size = new System.Drawing.Size(531, 150);
            this.dgvHistory.TabIndex = 14;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(15, 434);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(82, 17);
            this.label9.TabIndex = 15;
            this.label9.Text = "Preishistorie:";
            // 
            // lblGruppe
            // 
            this.lblGruppe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(70)))), ((int)(((byte)(217)))));
            this.lblGruppe.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGruppe.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGruppe.ForeColor = System.Drawing.Color.White;
            this.lblGruppe.Location = new System.Drawing.Point(0, 35);
            this.lblGruppe.Name = "lblGruppe";
            this.lblGruppe.Padding = new System.Windows.Forms.Padding(10, 5, 5, 5);
            this.lblGruppe.Size = new System.Drawing.Size(565, 35);
            this.lblGruppe.TabIndex = 16;
            // 
            // groupBox_Formel
            // 
            this.groupBox_Formel.Controls.Add(this.lblFormula);
            this.groupBox_Formel.Controls.Add(this.lblResult);
            this.groupBox_Formel.Controls.Add(this.label1);
            this.groupBox_Formel.Controls.Add(this.label2);
            this.groupBox_Formel.Location = new System.Drawing.Point(18, 243);
            this.groupBox_Formel.Name = "groupBox_Formel";
            this.groupBox_Formel.Size = new System.Drawing.Size(425, 76);
            this.groupBox_Formel.TabIndex = 20;
            this.groupBox_Formel.TabStop = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(133, 352);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 17);
            this.label3.TabIndex = 25;
            this.label3.Text = "CO2  [g/kWh]";
            // 
            // numCO2
            // 
            this.numCO2.Location = new System.Drawing.Point(234, 349);
            this.numCO2.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numCO2.Name = "numCO2";
            this.numCO2.Size = new System.Drawing.Size(63, 25);
            this.numCO2.TabIndex = 24;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(133, 376);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(88, 17);
            this.label10.TabIndex = 27;
            this.label10.Text = "NOx  [g/kWh]";
            // 
            // numNOx
            // 
            this.numNOx.Location = new System.Drawing.Point(234, 373);
            this.numNOx.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numNOx.Name = "numNOx";
            this.numNOx.Size = new System.Drawing.Size(63, 25);
            this.numNOx.TabIndex = 26;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(133, 328);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(86, 17);
            this.label11.TabIndex = 29;
            this.label11.Text = "SO2  [g/kWh]";
            // 
            // numSO2
            // 
            this.numSO2.Location = new System.Drawing.Point(234, 325);
            this.numSO2.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numSO2.Name = "numSO2";
            this.numSO2.Size = new System.Drawing.Size(63, 25);
            this.numSO2.TabIndex = 28;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(16, 328);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(116, 17);
            this.label12.TabIndex = 30;
            this.label12.Text = "Emissionsfaktoren:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lbl_Unit_Heizwert);
            this.panel1.Controls.Add(this.lbl_Leistungspreis);
            this.panel1.Controls.Add(this.numHeizwert);
            this.panel1.Controls.Add(this.lbl_Unit_Brennwert);
            this.panel1.Controls.Add(this.lbl_Brennwert);
            this.panel1.Controls.Add(this.numBrennwert);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.lbl_Unit_Leistungspreis);
            this.panel1.Controls.Add(this.lbl_Unit_Arbeitspreis);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.lbl_Heizwert);
            this.panel1.Controls.Add(this.numArbeitspreis);
            this.panel1.Controls.Add(this.numLeistungspreis);
            this.panel1.Controls.Add(this.numGrundpreis);
            this.panel1.Location = new System.Drawing.Point(17, 154);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(531, 88);
            this.panel1.TabIndex = 31;
            // 
            // lbl_Unit_Heizwert
            // 
            this.lbl_Unit_Heizwert.AutoSize = true;
            this.lbl_Unit_Heizwert.Location = new System.Drawing.Point(454, 9);
            this.lbl_Unit_Heizwert.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Heizwert.Name = "lbl_Unit_Heizwert";
            this.lbl_Unit_Heizwert.Size = new System.Drawing.Size(13, 17);
            this.lbl_Unit_Heizwert.TabIndex = 38;
            this.lbl_Unit_Heizwert.Text = "-";
            // 
            // lbl_Leistungspreis
            // 
            this.lbl_Leistungspreis.AutoSize = true;
            this.lbl_Leistungspreis.Location = new System.Drawing.Point(3, 62);
            this.lbl_Leistungspreis.Name = "lbl_Leistungspreis";
            this.lbl_Leistungspreis.Size = new System.Drawing.Size(91, 17);
            this.lbl_Leistungspreis.TabIndex = 37;
            this.lbl_Leistungspreis.Text = "Leistungspreis";
            // 
            // numHeizwert
            // 
            this.numHeizwert.DecimalPlaces = 2;
            this.numHeizwert.Location = new System.Drawing.Point(354, 6);
            this.numHeizwert.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numHeizwert.Name = "numHeizwert";
            this.numHeizwert.Size = new System.Drawing.Size(93, 25);
            this.numHeizwert.TabIndex = 36;
            // 
            // lbl_Unit_Brennwert
            // 
            this.lbl_Unit_Brennwert.AutoSize = true;
            this.lbl_Unit_Brennwert.Location = new System.Drawing.Point(454, 38);
            this.lbl_Unit_Brennwert.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Brennwert.Name = "lbl_Unit_Brennwert";
            this.lbl_Unit_Brennwert.Size = new System.Drawing.Size(13, 17);
            this.lbl_Unit_Brennwert.TabIndex = 35;
            this.lbl_Unit_Brennwert.Text = "-";
            // 
            // lbl_Brennwert
            // 
            this.lbl_Brennwert.AutoSize = true;
            this.lbl_Brennwert.Location = new System.Drawing.Point(282, 37);
            this.lbl_Brennwert.Name = "lbl_Brennwert";
            this.lbl_Brennwert.Size = new System.Drawing.Size(66, 17);
            this.lbl_Brennwert.TabIndex = 34;
            this.lbl_Brennwert.Text = "Brennwert";
            // 
            // numBrennwert
            // 
            this.numBrennwert.DecimalPlaces = 2;
            this.numBrennwert.Location = new System.Drawing.Point(354, 35);
            this.numBrennwert.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numBrennwert.Name = "numBrennwert";
            this.numBrennwert.Size = new System.Drawing.Size(93, 25);
            this.numBrennwert.TabIndex = 33;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(202, 36);
            this.label7.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(15, 17);
            this.label7.TabIndex = 32;
            this.label7.Text = "€/a";
            // 
            // lbl_Unit_Leistungspreis
            // 
            this.lbl_Unit_Leistungspreis.AutoSize = true;
            this.lbl_Unit_Leistungspreis.Location = new System.Drawing.Point(202, 63);
            this.lbl_Unit_Leistungspreis.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Leistungspreis.Name = "lbl_Unit_Leistungspreis";
            this.lbl_Unit_Leistungspreis.Size = new System.Drawing.Size(13, 17);
            this.lbl_Unit_Leistungspreis.TabIndex = 31;
            this.lbl_Unit_Leistungspreis.Text = "-";
            // 
            // lbl_Unit_Arbeitspreis
            // 
            this.lbl_Unit_Arbeitspreis.AutoSize = true;
            this.lbl_Unit_Arbeitspreis.Location = new System.Drawing.Point(202, 8);
            this.lbl_Unit_Arbeitspreis.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Arbeitspreis.Name = "lbl_Unit_Arbeitspreis";
            this.lbl_Unit_Arbeitspreis.Size = new System.Drawing.Size(13, 17);
            this.lbl_Unit_Arbeitspreis.TabIndex = 30;
            this.lbl_Unit_Arbeitspreis.Text = "-";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(2, 35);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(73, 17);
            this.label5.TabIndex = 29;
            this.label5.Text = "Grundpreis";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(2, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 17);
            this.label4.TabIndex = 28;
            this.label4.Text = "Arbeitspreis";
            // 
            // lbl_Heizwert
            // 
            this.lbl_Heizwert.AutoSize = true;
            this.lbl_Heizwert.Location = new System.Drawing.Point(282, 7);
            this.lbl_Heizwert.Name = "lbl_Heizwert";
            this.lbl_Heizwert.Size = new System.Drawing.Size(58, 17);
            this.lbl_Heizwert.TabIndex = 27;
            this.lbl_Heizwert.Text = "Heizwert";
            // 
            // numArbeitspreis
            // 
            this.numArbeitspreis.DecimalPlaces = 2;
            this.numArbeitspreis.Location = new System.Drawing.Point(100, 5);
            this.numArbeitspreis.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numArbeitspreis.Name = "numArbeitspreis";
            this.numArbeitspreis.Size = new System.Drawing.Size(93, 25);
            this.numArbeitspreis.TabIndex = 24;
            // 
            // numLeistungspreis
            // 
            this.numLeistungspreis.DecimalPlaces = 2;
            this.numLeistungspreis.Location = new System.Drawing.Point(100, 59);
            this.numLeistungspreis.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numLeistungspreis.Name = "numLeistungspreis";
            this.numLeistungspreis.Size = new System.Drawing.Size(93, 25);
            this.numLeistungspreis.TabIndex = 25;
            // 
            // numGrundpreis
            // 
            this.numGrundpreis.DecimalPlaces = 2;
            this.numGrundpreis.Location = new System.Drawing.Point(100, 33);
            this.numGrundpreis.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numGrundpreis.Name = "numGrundpreis";
            this.numGrundpreis.Size = new System.Drawing.Size(93, 25);
            this.numGrundpreis.TabIndex = 26;
            // 
            // dtpValidFrom
            // 
            this.dtpValidFrom.Location = new System.Drawing.Point(234, 421);
            this.dtpValidFrom.Name = "dtpValidFrom";
            this.dtpValidFrom.Size = new System.Drawing.Size(230, 25);
            this.dtpValidFrom.TabIndex = 32;
            // 
            // ucFuelSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.dtpValidFrom);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.numSO2);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.numNOx);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.numCO2);
            this.Controls.Add(this.groupBox_Formel);
            this.Controls.Add(this.lblGruppe);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.dgvHistory);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.btn_Save);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblBasisnheit);
            this.Controls.Add(this.cmbUnit);
            this.Controls.Add(this.lblCarrierName);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ucFuelSettings";
            this.Size = new System.Drawing.Size(565, 616);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.groupBox_Formel.ResumeLayout(false);
            this.groupBox_Formel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCO2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNOx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSO2)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numHeizwert)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrennwert)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungspreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblCarrierName;
        private System.Windows.Forms.ComboBox cmbUnit;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.Label lblFormula;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblBasisnheit;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btn_Save;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblGruppe;
        private System.Windows.Forms.GroupBox groupBox_Formel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numCO2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown numNOx;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numSO2;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lbl_Unit_Heizwert;
        private System.Windows.Forms.Label lbl_Leistungspreis;
        private System.Windows.Forms.NumericUpDown numHeizwert;
        private System.Windows.Forms.Label lbl_Unit_Brennwert;
        private System.Windows.Forms.Label lbl_Brennwert;
        private System.Windows.Forms.NumericUpDown numBrennwert;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lbl_Unit_Leistungspreis;
        private System.Windows.Forms.Label lbl_Unit_Arbeitspreis;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lbl_Heizwert;
        private System.Windows.Forms.NumericUpDown numArbeitspreis;
        private System.Windows.Forms.NumericUpDown numLeistungspreis;
        private System.Windows.Forms.NumericUpDown numGrundpreis;
        private System.Windows.Forms.DateTimePicker dtpValidFrom;
    }
}