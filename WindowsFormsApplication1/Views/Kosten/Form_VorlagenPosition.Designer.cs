namespace WindowsFormsApplication1
{
    partial class Form_VorlagenPosition
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
            this.lblBezeichnung = new System.Windows.Forms.Label();
            this.txtBezeichnung = new System.Windows.Forms.TextBox();
            this.lblKostenart = new System.Windows.Forms.Label();
            this.cmbKostenart = new System.Windows.Forms.ComboBox();
            this.chkErloes = new System.Windows.Forms.CheckBox();
            this.lblEmpfehlung = new System.Windows.Forms.Label();
            this.txtEmpfVon = new System.Windows.Forms.TextBox();
            this.lblBis = new System.Windows.Forms.Label();
            this.txtEmpfBis = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblBezeichnung
            //
            this.lblBezeichnung.AutoSize = true;
            this.lblBezeichnung.Location = new System.Drawing.Point(14, 17);
            this.lblBezeichnung.Name = "lblBezeichnung";
            this.lblBezeichnung.Size = new System.Drawing.Size(78, 15);
            this.lblBezeichnung.TabIndex = 0;
            this.lblBezeichnung.Text = "Bezeichnung:";
            //
            // txtBezeichnung
            //
            this.txtBezeichnung.Location = new System.Drawing.Point(140, 14);
            this.txtBezeichnung.Name = "txtBezeichnung";
            this.txtBezeichnung.Size = new System.Drawing.Size(280, 23);
            this.txtBezeichnung.TabIndex = 1;
            //
            // lblKostenart
            //
            this.lblKostenart.AutoSize = true;
            this.lblKostenart.Location = new System.Drawing.Point(14, 50);
            this.lblKostenart.Name = "lblKostenart";
            this.lblKostenart.Size = new System.Drawing.Size(60, 15);
            this.lblKostenart.TabIndex = 2;
            this.lblKostenart.Text = "Kostenart:";
            //
            // cmbKostenart
            //
            this.cmbKostenart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKostenart.Location = new System.Drawing.Point(140, 47);
            this.cmbKostenart.Name = "cmbKostenart";
            this.cmbKostenart.Size = new System.Drawing.Size(280, 23);
            this.cmbKostenart.TabIndex = 3;
            //
            // chkErloes
            //
            this.chkErloes.AutoSize = true;
            this.chkErloes.Location = new System.Drawing.Point(140, 80);
            this.chkErloes.Name = "chkErloes";
            this.chkErloes.Size = new System.Drawing.Size(200, 19);
            this.chkErloes.TabIndex = 4;
            this.chkErloes.Text = "Erlös/Zuschuss (negativer Ausweis)";
            this.chkErloes.UseVisualStyleBackColor = true;
            //
            // lblEmpfehlung
            //
            this.lblEmpfehlung.AutoSize = true;
            this.lblEmpfehlung.Location = new System.Drawing.Point(14, 113);
            this.lblEmpfehlung.Name = "lblEmpfehlung";
            this.lblEmpfehlung.Size = new System.Drawing.Size(110, 15);
            this.lblEmpfehlung.TabIndex = 5;
            this.lblEmpfehlung.Text = "Empfehlung von/bis:";
            //
            // txtEmpfVon
            //
            this.txtEmpfVon.Location = new System.Drawing.Point(140, 110);
            this.txtEmpfVon.Name = "txtEmpfVon";
            this.txtEmpfVon.Size = new System.Drawing.Size(80, 23);
            this.txtEmpfVon.TabIndex = 6;
            this.txtEmpfVon.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtEmpfVon.TextChanged += new System.EventHandler(this.Zahl_TextChanged);
            //
            // lblBis
            //
            this.lblBis.AutoSize = true;
            this.lblBis.Location = new System.Drawing.Point(228, 113);
            this.lblBis.Name = "lblBis";
            this.lblBis.Size = new System.Drawing.Size(22, 15);
            this.lblBis.TabIndex = 7;
            this.lblBis.Text = "bis";
            //
            // txtEmpfBis
            //
            this.txtEmpfBis.Location = new System.Drawing.Point(256, 110);
            this.txtEmpfBis.Name = "txtEmpfBis";
            this.txtEmpfBis.Size = new System.Drawing.Size(80, 23);
            this.txtEmpfBis.TabIndex = 8;
            this.txtEmpfBis.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtEmpfBis.TextChanged += new System.EventHandler(this.Zahl_TextChanged);
            //
            // btnOk
            //
            this.btnOk.Location = new System.Drawing.Point(236, 156);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(88, 27);
            this.btnOk.TabIndex = 9;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Location = new System.Drawing.Point(332, 156);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(88, 27);
            this.btnAbbrechen.TabIndex = 10;
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.UseVisualStyleBackColor = true;
            //
            // Form_VorlagenPosition
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(436, 197);
            this.Controls.Add(this.lblBezeichnung);
            this.Controls.Add(this.txtBezeichnung);
            this.Controls.Add(this.lblKostenart);
            this.Controls.Add(this.cmbKostenart);
            this.Controls.Add(this.chkErloes);
            this.Controls.Add(this.lblEmpfehlung);
            this.Controls.Add(this.txtEmpfVon);
            this.Controls.Add(this.lblBis);
            this.Controls.Add(this.txtEmpfBis);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnAbbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_VorlagenPosition";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Position bearbeiten";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblBezeichnung;
        private System.Windows.Forms.TextBox txtBezeichnung;
        private System.Windows.Forms.Label lblKostenart;
        private System.Windows.Forms.ComboBox cmbKostenart;
        private System.Windows.Forms.CheckBox chkErloes;
        private System.Windows.Forms.Label lblEmpfehlung;
        private System.Windows.Forms.TextBox txtEmpfVon;
        private System.Windows.Forms.Label lblBis;
        private System.Windows.Forms.TextBox txtEmpfBis;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
