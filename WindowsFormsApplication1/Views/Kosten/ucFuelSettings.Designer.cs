namespace WindowsFormsApplication1
{
    partial class ucFuelSettings
    {
        private void InitializeComponent()
        {
            this.lblCarrierName = new System.Windows.Forms.Label();
            this.lblResult = new System.Windows.Forms.Label();
            this.lblFormula = new System.Windows.Forms.Label();
            this.numArbeitspreis = new System.Windows.Forms.NumericUpDown();
            this.numGrundpreis = new System.Windows.Forms.NumericUpDown();
            this.numHeizwert = new System.Windows.Forms.NumericUpDown();
            this.cmbUnit = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbl_Heizwert = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblBasisnheit = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btn_Save = new System.Windows.Forms.Button();
            this.lbl_Unit_Arbeitspreis = new System.Windows.Forms.Label();
            this.lbl_Unit_Heizwert = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.label9 = new System.Windows.Forms.Label();
            this.lblGruppe = new System.Windows.Forms.Label();
            this.lbl_Unit_Brennwert = new System.Windows.Forms.Label();
            this.lb1_Brennwert = new System.Windows.Forms.Label();
            this.numBrennwert = new System.Windows.Forms.NumericUpDown();
            this.groupBox_Formel = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeizwert)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrennwert)).BeginInit();
            this.groupBox_Formel.SuspendLayout();
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
            this.lblCarrierName.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblCarrierName.Size = new System.Drawing.Size(563, 20);
            this.lblCarrierName.TabIndex = 0;
            this.lblCarrierName.Text = "Energieträger";
            // 
            // lblResult
            // 
            this.lblResult.Location = new System.Drawing.Point(124, 18);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(172, 23);
            this.lblResult.TabIndex = 0;
            this.lblResult.Text = "result";
            // 
            // lblFormula
            // 
            this.lblFormula.Location = new System.Drawing.Point(124, 51);
            this.lblFormula.Name = "lblFormula";
            this.lblFormula.Size = new System.Drawing.Size(299, 23);
            this.lblFormula.TabIndex = 0;
            this.lblFormula.Text = "formula";
            // 
            // numArbeitspreis
            // 
            this.numArbeitspreis.DecimalPlaces = 4;
            this.numArbeitspreis.Location = new System.Drawing.Point(132, 125);
            this.numArbeitspreis.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numArbeitspreis.Name = "numArbeitspreis";
            this.numArbeitspreis.Size = new System.Drawing.Size(120, 20);
            this.numArbeitspreis.TabIndex = 0;
            // 
            // numGrundpreis
            // 
            this.numGrundpreis.DecimalPlaces = 2;
            this.numGrundpreis.Location = new System.Drawing.Point(132, 153);
            this.numGrundpreis.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numGrundpreis.Name = "numGrundpreis";
            this.numGrundpreis.Size = new System.Drawing.Size(120, 20);
            this.numGrundpreis.TabIndex = 0;
            // 
            // numHeizwert
            // 
            this.numHeizwert.DecimalPlaces = 2;
            this.numHeizwert.Location = new System.Drawing.Point(132, 180);
            this.numHeizwert.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numHeizwert.Name = "numHeizwert";
            this.numHeizwert.Size = new System.Drawing.Size(120, 20);
            this.numHeizwert.TabIndex = 0;
            // 
            // cmbUnit
            // 
            this.cmbUnit.Location = new System.Drawing.Point(132, 53);
            this.cmbUnit.Name = "cmbUnit";
            this.cmbUnit.Size = new System.Drawing.Size(120, 21);
            this.cmbUnit.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Preis pro kWh:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Formel:";
            // 
            // lbl_Heizwert
            // 
            this.lbl_Heizwert.AutoSize = true;
            this.lbl_Heizwert.Location = new System.Drawing.Point(28, 183);
            this.lbl_Heizwert.Name = "lbl_Heizwert";
            this.lbl_Heizwert.Size = new System.Drawing.Size(48, 13);
            this.lbl_Heizwert.TabIndex = 3;
            this.lbl_Heizwert.Text = "Heizwert";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(28, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Arbeitspreis";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(28, 156);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Grundpreis";
            // 
            // lblBasisnheit
            // 
            this.lblBasisnheit.AutoSize = true;
            this.lblBasisnheit.Location = new System.Drawing.Point(131, 89);
            this.lblBasisnheit.Name = "lblBasisnheit";
            this.lblBasisnheit.Size = new System.Drawing.Size(66, 13);
            this.lblBasisnheit.TabIndex = 7;
            this.lblBasisnheit.Text = "Basiseinheit:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(28, 56);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Preisbasis";
            // 
            // btn_Save
            // 
            this.btn_Save.AutoSize = true;
            this.btn_Save.Location = new System.Drawing.Point(32, 340);
            this.btn_Save.MinimumSize = new System.Drawing.Size(75, 23);
            this.btn_Save.Name = "btn_Save";
            this.btn_Save.Size = new System.Drawing.Size(80, 27);
            this.btn_Save.TabIndex = 9;
            this.btn_Save.Text = "💾 Speichern";
            this.btn_Save.UseVisualStyleBackColor = true;
            this.btn_Save.Click += new System.EventHandler(this.btn_Save_Click);
            // 
            // lbl_Unit_Arbeitspreis
            // 
            this.lbl_Unit_Arbeitspreis.AutoSize = true;
            this.lbl_Unit_Arbeitspreis.Location = new System.Drawing.Point(260, 128);
            this.lbl_Unit_Arbeitspreis.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Arbeitspreis.Name = "lbl_Unit_Arbeitspreis";
            this.lbl_Unit_Arbeitspreis.Size = new System.Drawing.Size(35, 13);
            this.lbl_Unit_Arbeitspreis.TabIndex = 10;
            this.lbl_Unit_Arbeitspreis.Text = "label7";
            // 
            // lbl_Unit_Heizwert
            // 
            this.lbl_Unit_Heizwert.AutoSize = true;
            this.lbl_Unit_Heizwert.Location = new System.Drawing.Point(260, 184);
            this.lbl_Unit_Heizwert.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Heizwert.Name = "lbl_Unit_Heizwert";
            this.lbl_Unit_Heizwert.Size = new System.Drawing.Size(35, 13);
            this.lbl_Unit_Heizwert.TabIndex = 11;
            this.lbl_Unit_Heizwert.Text = "label8";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(260, 156);
            this.label7.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(13, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "€";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(28, 88);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(66, 13);
            this.label8.TabIndex = 13;
            this.label8.Text = "Basiseinheit:";
            // 
            // dgvHistory
            // 
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Location = new System.Drawing.Point(31, 400);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.Size = new System.Drawing.Size(504, 150);
            this.dgvHistory.TabIndex = 14;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(29, 382);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(66, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "Preishistorie:";
            // 
            // lblGruppe
            // 
            this.lblGruppe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(70)))), ((int)(((byte)(217)))));
            this.lblGruppe.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblGruppe.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGruppe.ForeColor = System.Drawing.Color.White;
            this.lblGruppe.Location = new System.Drawing.Point(0, 20);
            this.lblGruppe.Name = "lblGruppe";
            this.lblGruppe.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblGruppe.Size = new System.Drawing.Size(563, 20);
            this.lblGruppe.TabIndex = 16;
            // 
            // lbl_Unit_Brennwert
            // 
            this.lbl_Unit_Brennwert.AutoSize = true;
            this.lbl_Unit_Brennwert.Location = new System.Drawing.Point(260, 212);
            this.lbl_Unit_Brennwert.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.lbl_Unit_Brennwert.Name = "lbl_Unit_Brennwert";
            this.lbl_Unit_Brennwert.Size = new System.Drawing.Size(35, 13);
            this.lbl_Unit_Brennwert.TabIndex = 19;
            this.lbl_Unit_Brennwert.Text = "label8";
            // 
            // lb1_Brennwert
            // 
            this.lb1_Brennwert.AutoSize = true;
            this.lb1_Brennwert.Location = new System.Drawing.Point(28, 211);
            this.lb1_Brennwert.Name = "lb1_Brennwert";
            this.lb1_Brennwert.Size = new System.Drawing.Size(55, 13);
            this.lb1_Brennwert.TabIndex = 18;
            this.lb1_Brennwert.Text = "Brennwert";
            // 
            // numBrennwert
            // 
            this.numBrennwert.DecimalPlaces = 2;
            this.numBrennwert.Location = new System.Drawing.Point(132, 208);
            this.numBrennwert.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numBrennwert.Name = "numBrennwert";
            this.numBrennwert.Size = new System.Drawing.Size(120, 20);
            this.numBrennwert.TabIndex = 17;
            // 
            // groupBox_Formel
            // 
            this.groupBox_Formel.Controls.Add(this.lblFormula);
            this.groupBox_Formel.Controls.Add(this.lblResult);
            this.groupBox_Formel.Controls.Add(this.label1);
            this.groupBox_Formel.Controls.Add(this.label2);
            this.groupBox_Formel.Location = new System.Drawing.Point(31, 241);
            this.groupBox_Formel.Name = "groupBox_Formel";
            this.groupBox_Formel.Size = new System.Drawing.Size(443, 81);
            this.groupBox_Formel.TabIndex = 20;
            this.groupBox_Formel.TabStop = false;
            // 
            // ucFuelSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.groupBox_Formel);
            this.Controls.Add(this.lbl_Unit_Brennwert);
            this.Controls.Add(this.lb1_Brennwert);
            this.Controls.Add(this.numBrennwert);
            this.Controls.Add(this.lblGruppe);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.dgvHistory);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.lbl_Unit_Heizwert);
            this.Controls.Add(this.lbl_Unit_Arbeitspreis);
            this.Controls.Add(this.btn_Save);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblBasisnheit);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbl_Heizwert);
            this.Controls.Add(this.numArbeitspreis);
            this.Controls.Add(this.numHeizwert);
            this.Controls.Add(this.numGrundpreis);
            this.Controls.Add(this.cmbUnit);
            this.Controls.Add(this.lblCarrierName);
            this.Name = "ucFuelSettings";
            this.Size = new System.Drawing.Size(563, 569);
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeizwert)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrennwert)).EndInit();
            this.groupBox_Formel.ResumeLayout(false);
            this.groupBox_Formel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblCarrierName;
        private System.Windows.Forms.NumericUpDown numArbeitspreis;
        private System.Windows.Forms.NumericUpDown numGrundpreis;
        private System.Windows.Forms.NumericUpDown numHeizwert;
        private System.Windows.Forms.ComboBox cmbUnit;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.Label lblFormula;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbl_Heizwert;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblBasisnheit;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btn_Save;
        private System.Windows.Forms.Label lbl_Unit_Arbeitspreis;
        private System.Windows.Forms.Label lbl_Unit_Heizwert;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblGruppe;
        private System.Windows.Forms.Label lbl_Unit_Brennwert;
        private System.Windows.Forms.Label lb1_Brennwert;
        private System.Windows.Forms.NumericUpDown numBrennwert;
        private System.Windows.Forms.GroupBox groupBox_Formel;
    }
}
