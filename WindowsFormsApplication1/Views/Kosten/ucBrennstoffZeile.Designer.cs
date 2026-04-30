namespace WindowsFormsApplication1
{
    partial class ucBrennstoffZeile
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
            this.lblName = new System.Windows.Forms.Label();
            this.numLeistungpreis = new System.Windows.Forms.NumericUpDown();
            this.lblEinheit = new System.Windows.Forms.Label();
            this.numGrundpreis = new System.Windows.Forms.NumericUpDown();
            this.numArbeitspreis = new System.Windows.Forms.NumericUpDown();
            this.lblHi = new System.Windows.Forms.Label();
            this.lblPreisEinheit = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungpreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblName.Location = new System.Drawing.Point(8, 6);
            this.lblName.Margin = new System.Windows.Forms.Padding(0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(97, 17);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "label1";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numLeistungpreis
            // 
            this.numLeistungpreis.AutoSize = true;
            this.numLeistungpreis.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numLeistungpreis.Location = new System.Drawing.Point(395, 4);
            this.numLeistungpreis.Margin = new System.Windows.Forms.Padding(0);
            this.numLeistungpreis.Name = "numLeistungpreis";
            this.numLeistungpreis.Size = new System.Drawing.Size(66, 25);
            this.numLeistungpreis.TabIndex = 1;
            // 
            // lblEinheit
            // 
            this.lblEinheit.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblEinheit.Location = new System.Drawing.Point(116, 6);
            this.lblEinheit.Margin = new System.Windows.Forms.Padding(0);
            this.lblEinheit.Name = "lblEinheit";
            this.lblEinheit.Size = new System.Drawing.Size(43, 17);
            this.lblEinheit.TabIndex = 2;
            this.lblEinheit.Text = "label1";
            this.lblEinheit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numGrundpreis
            // 
            this.numGrundpreis.AutoSize = true;
            this.numGrundpreis.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numGrundpreis.Location = new System.Drawing.Point(246, 4);
            this.numGrundpreis.Margin = new System.Windows.Forms.Padding(0);
            this.numGrundpreis.Name = "numGrundpreis";
            this.numGrundpreis.Size = new System.Drawing.Size(66, 25);
            this.numGrundpreis.TabIndex = 3;
            // 
            // numArbeitspreis
            // 
            this.numArbeitspreis.AutoSize = true;
            this.numArbeitspreis.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numArbeitspreis.Location = new System.Drawing.Point(318, 4);
            this.numArbeitspreis.Margin = new System.Windows.Forms.Padding(0);
            this.numArbeitspreis.Name = "numArbeitspreis";
            this.numArbeitspreis.Size = new System.Drawing.Size(66, 25);
            this.numArbeitspreis.TabIndex = 4;
            // 
            // lblHi
            // 
            this.lblHi.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblHi.Location = new System.Drawing.Point(171, 6);
            this.lblHi.Margin = new System.Windows.Forms.Padding(0);
            this.lblHi.Name = "lblHi";
            this.lblHi.Size = new System.Drawing.Size(43, 17);
            this.lblHi.TabIndex = 5;
            this.lblHi.Text = "label1";
            this.lblHi.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPreisEinheit
            // 
            this.lblPreisEinheit.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblPreisEinheit.Location = new System.Drawing.Point(471, 6);
            this.lblPreisEinheit.Margin = new System.Windows.Forms.Padding(0);
            this.lblPreisEinheit.Name = "lblPreisEinheit";
            this.lblPreisEinheit.Size = new System.Drawing.Size(43, 17);
            this.lblPreisEinheit.TabIndex = 6;
            this.lblPreisEinheit.Text = "label1";
            this.lblPreisEinheit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ucBrennstoffZeile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblPreisEinheit);
            this.Controls.Add(this.lblHi);
            this.Controls.Add(this.numArbeitspreis);
            this.Controls.Add(this.numGrundpreis);
            this.Controls.Add(this.lblEinheit);
            this.Controls.Add(this.numLeistungpreis);
            this.Controls.Add(this.lblName);
            this.Name = "ucBrennstoffZeile";
            this.Size = new System.Drawing.Size(571, 31);
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungpreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrundpreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArbeitspreis)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.NumericUpDown numLeistungpreis;
        private System.Windows.Forms.Label lblEinheit;
        private System.Windows.Forms.NumericUpDown numGrundpreis;
        private System.Windows.Forms.NumericUpDown numArbeitspreis;
        private System.Windows.Forms.Label lblHi;
        private System.Windows.Forms.Label lblPreisEinheit;
    }
}
