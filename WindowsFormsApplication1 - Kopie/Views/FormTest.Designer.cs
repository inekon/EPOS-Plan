namespace WindowsFormsApplication1
{
    partial class FormTest
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

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.textWPName = new System.Windows.Forms.TextBox();
            this.butt_WP = new System.Windows.Forms.Button();
            this.listBoxWP = new System.Windows.Forms.ListBox();
            this.butt_Speichern = new System.Windows.Forms.Button();
            this.textBeschreibung = new System.Windows.Forms.TextBox();
            this.butt_Delete = new System.Windows.Forms.Button();
            this.butt_Neu = new System.Windows.Forms.Button();
            this.butt_ExcelDatei = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textWPName
            // 
            this.textWPName.Location = new System.Drawing.Point(287, 123);
            this.textWPName.Multiline = true;
            this.textWPName.Name = "textWPName";
            this.textWPName.Size = new System.Drawing.Size(268, 34);
            this.textWPName.TabIndex = 0;
            // 
            // butt_WP
            // 
            this.butt_WP.Location = new System.Drawing.Point(30, 88);
            this.butt_WP.Name = "butt_WP";
            this.butt_WP.Size = new System.Drawing.Size(62, 29);
            this.butt_WP.TabIndex = 1;
            this.butt_WP.Text = "WP Liste";
            this.butt_WP.UseVisualStyleBackColor = true;
            this.butt_WP.Click += new System.EventHandler(this.butt_WP_Click);
            // 
            // listBoxWP
            // 
            this.listBoxWP.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxWP.FormattingEnabled = true;
            this.listBoxWP.ItemHeight = 16;
            this.listBoxWP.Location = new System.Drawing.Point(30, 123);
            this.listBoxWP.Name = "listBoxWP";
            this.listBoxWP.Size = new System.Drawing.Size(235, 372);
            this.listBoxWP.TabIndex = 2;
            this.listBoxWP.SelectedIndexChanged += new System.EventHandler(this.listBoxWP_SelectedIndexChanged);
            // 
            // butt_Speichern
            // 
            this.butt_Speichern.Location = new System.Drawing.Point(287, 461);
            this.butt_Speichern.Name = "butt_Speichern";
            this.butt_Speichern.Size = new System.Drawing.Size(84, 31);
            this.butt_Speichern.TabIndex = 3;
            this.butt_Speichern.Text = "Speichern";
            this.butt_Speichern.UseVisualStyleBackColor = true;
            this.butt_Speichern.Click += new System.EventHandler(this.butt_Speichern_Click);
            // 
            // textBeschreibung
            // 
            this.textBeschreibung.Location = new System.Drawing.Point(287, 182);
            this.textBeschreibung.Multiline = true;
            this.textBeschreibung.Name = "textBeschreibung";
            this.textBeschreibung.Size = new System.Drawing.Size(268, 162);
            this.textBeschreibung.TabIndex = 4;
            // 
            // butt_Delete
            // 
            this.butt_Delete.Location = new System.Drawing.Point(488, 461);
            this.butt_Delete.Name = "butt_Delete";
            this.butt_Delete.Size = new System.Drawing.Size(84, 31);
            this.butt_Delete.TabIndex = 5;
            this.butt_Delete.Text = "Löschen";
            this.butt_Delete.UseVisualStyleBackColor = true;
            this.butt_Delete.Click += new System.EventHandler(this.butt_Delete_Click);
            // 
            // butt_Neu
            // 
            this.butt_Neu.Location = new System.Drawing.Point(388, 461);
            this.butt_Neu.Name = "butt_Neu";
            this.butt_Neu.Size = new System.Drawing.Size(84, 31);
            this.butt_Neu.TabIndex = 6;
            this.butt_Neu.Text = "Neu";
            this.butt_Neu.UseVisualStyleBackColor = true;
            this.butt_Neu.Click += new System.EventHandler(this.butt_Neu_Click);
            // 
            // butt_ExcelDatei
            // 
            this.butt_ExcelDatei.Location = new System.Drawing.Point(30, 36);
            this.butt_ExcelDatei.Name = "butt_ExcelDatei";
            this.butt_ExcelDatei.Size = new System.Drawing.Size(131, 33);
            this.butt_ExcelDatei.TabIndex = 7;
            this.butt_ExcelDatei.Text = "VDI 3805 einlesen";
            this.butt_ExcelDatei.UseVisualStyleBackColor = true;
            this.butt_ExcelDatei.Click += new System.EventHandler(this.butt_ExcelDatei_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(568, 125);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(568, 185);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Beschreibung";
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 513);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.butt_ExcelDatei);
            this.Controls.Add(this.butt_Neu);
            this.Controls.Add(this.butt_Delete);
            this.Controls.Add(this.textBeschreibung);
            this.Controls.Add(this.butt_Speichern);
            this.Controls.Add(this.listBoxWP);
            this.Controls.Add(this.butt_WP);
            this.Controls.Add(this.textWPName);
            this.Name = "Form3";
            this.Text = "Form3";
            this.Load += new System.EventHandler(this.Form3_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textWPName;
        private System.Windows.Forms.Button butt_WP;
        private System.Windows.Forms.ListBox listBoxWP;
        private System.Windows.Forms.Button butt_Speichern;
        private System.Windows.Forms.TextBox textBeschreibung;
        private System.Windows.Forms.Button butt_Delete;
        private System.Windows.Forms.Button butt_Neu;
        private System.Windows.Forms.Button butt_ExcelDatei;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}