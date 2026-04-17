
    partial class ucKostenZeile
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
            this.numBetrag = new System.Windows.Forms.NumericUpDown();
            this.numDauer = new System.Windows.Forms.NumericUpDown();
            this.lblEinheit = new System.Windows.Forms.Label();
            this.btn_Delete = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numBetrag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDauer)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblName.Location = new System.Drawing.Point(6, 4);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(43, 17);
            this.lblName.TabIndex = 43;
            this.lblName.Text = "Name";
            // 
            // numBetrag
            // 
            this.numBetrag.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numBetrag.Location = new System.Drawing.Point(109, 2);
            this.numBetrag.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numBetrag.Name = "numBetrag";
            this.numBetrag.Size = new System.Drawing.Size(97, 25);
            this.numBetrag.TabIndex = 44;
            // 
            // numDauer
            // 
            this.numDauer.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.numDauer.Location = new System.Drawing.Point(277, 3);
            this.numDauer.Name = "numDauer";
            this.numDauer.Size = new System.Drawing.Size(64, 25);
            this.numDauer.TabIndex = 46;
            // 
            // lblEinheit
            // 
            this.lblEinheit.AutoSize = true;
            this.lblEinheit.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblEinheit.Location = new System.Drawing.Point(224, 5);
            this.lblEinheit.Name = "lblEinheit";
            this.lblEinheit.Size = new System.Drawing.Size(46, 17);
            this.lblEinheit.TabIndex = 47;
            this.lblEinheit.Text = "Einheit";
            // 
            // btn_Delete
            // 
            this.btn_Delete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Delete.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Delete.ForeColor = System.Drawing.Color.Red;
            this.btn_Delete.Location = new System.Drawing.Point(422, 4);
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.Size = new System.Drawing.Size(22, 21);
            this.btn_Delete.TabIndex = 48;
            this.btn_Delete.Text = "X";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // ucKostenZeile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Delete);
            this.Controls.Add(this.lblEinheit);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.numBetrag);
            this.Controls.Add(this.numDauer);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ucKostenZeile";
            this.Size = new System.Drawing.Size(457, 29);
            ((System.ComponentModel.ISupportInitialize)(this.numBetrag)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDauer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.NumericUpDown numBetrag;
        private System.Windows.Forms.NumericUpDown numDauer;
        private System.Windows.Forms.Label lblEinheit;
    private System.Windows.Forms.Button btn_Delete;
}

