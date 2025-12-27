namespace WindowsFormsApplication1
{
    partial class Form_SolarKollektorenAdmin 
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
            this.btn_Kollektor_DB_Edit = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_neu = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_loeschen = new System.Windows.Forms.Button();
            this.Label11 = new System.Windows.Forms.Label();
            this.textBox_Kollektor_A = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label5 = new System.Windows.Forms.Label();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Kollektortype = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Modul_A = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Kollektor_DB_Edit
            // 
            this.btn_Kollektor_DB_Edit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_Edit.Location = new System.Drawing.Point(28, 368);
            this.btn_Kollektor_DB_Edit.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_Edit.Name = "btn_Kollektor_DB_Edit";
            this.btn_Kollektor_DB_Edit.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_Edit.TabIndex = 10;
            this.btn_Kollektor_DB_Edit.Text = "Kollektor in DB ändern...";
            this.btn_Kollektor_DB_Edit.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_Edit.Click += new System.EventHandler(this.btn_Kollektor_DB_Edit_Click);
            // 
            // btn_Kollektor_DB_neu
            // 
            this.btn_Kollektor_DB_neu.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_neu.Location = new System.Drawing.Point(28, 409);
            this.btn_Kollektor_DB_neu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_neu.Name = "btn_Kollektor_DB_neu";
            this.btn_Kollektor_DB_neu.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_neu.TabIndex = 11;
            this.btn_Kollektor_DB_neu.Text = "Kollektor in DB neu...";
            this.btn_Kollektor_DB_neu.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_neu.Click += new System.EventHandler(this.btn_Kollektor_DB_neu_Click);
            // 
            // btn_Kollektor_DB_loeschen
            // 
            this.btn_Kollektor_DB_loeschen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Kollektor_DB_loeschen.Location = new System.Drawing.Point(28, 450);
            this.btn_Kollektor_DB_loeschen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kollektor_DB_loeschen.Name = "btn_Kollektor_DB_loeschen";
            this.btn_Kollektor_DB_loeschen.Size = new System.Drawing.Size(162, 33);
            this.btn_Kollektor_DB_loeschen.TabIndex = 12;
            this.btn_Kollektor_DB_loeschen.Text = "Kollektor in DB löschen";
            this.btn_Kollektor_DB_loeschen.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_loeschen.Click += new System.EventHandler(this.btn_Kollektor_DB_loeschen_Click);
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label11.Location = new System.Drawing.Point(402, 260);
            this.Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(97, 17);
            this.Label11.TabIndex = 15;
            this.Label11.Text = "Kollektorfläche:";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Kollektor_A
            // 
            this.textBox_Kollektor_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektor_A.Enabled = false;
            this.textBox_Kollektor_A.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kollektor_A.Location = new System.Drawing.Point(506, 258);
            this.textBox_Kollektor_A.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kollektor_A.Name = "textBox_Kollektor_A";
            this.textBox_Kollektor_A.Size = new System.Drawing.Size(112, 25);
            this.textBox_Kollektor_A.TabIndex = 16;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.BackColor = System.Drawing.Color.Black;
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label12.ForeColor = System.Drawing.Color.White;
            this.Label12.Location = new System.Drawing.Point(621, 262);
            this.Label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(24, 17);
            this.Label12.TabIndex = 17;
            this.Label12.Text = "m²";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(707, 452);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(105, 31);
            this.btn_OK.TabIndex = 22;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label5.Location = new System.Drawing.Point(402, 100);
            this.Label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(63, 17);
            this.Label5.TabIndex = 23;
            this.Label5.Text = "Kollektor:";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label6.Location = new System.Drawing.Point(402, 131);
            this.Label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(89, 17);
            this.Label6.TabIndex = 24;
            this.Label6.Text = "Beschreibung:";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label7
            // 
            this.Label7.AutoSize = true;
            this.Label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label7.Location = new System.Drawing.Point(402, 195);
            this.Label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label7.Name = "Label7";
            this.Label7.Size = new System.Drawing.Size(67, 17);
            this.Label7.TabIndex = 25;
            this.Label7.Text = "Hersteller:";
            this.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label8
            // 
            this.Label8.AutoSize = true;
            this.Label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label8.Location = new System.Drawing.Point(402, 225);
            this.Label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label8.Name = "Label8";
            this.Label8.Size = new System.Drawing.Size(83, 17);
            this.Label8.TabIndex = 26;
            this.Label8.Text = "Modulfläche:";
            this.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Kollektortype
            // 
            this.textBox_Kollektortype.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektortype.Enabled = false;
            this.textBox_Kollektortype.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kollektortype.Location = new System.Drawing.Point(492, 97);
            this.textBox_Kollektortype.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kollektortype.Name = "textBox_Kollektortype";
            this.textBox_Kollektortype.Size = new System.Drawing.Size(308, 25);
            this.textBox_Kollektortype.TabIndex = 27;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(492, 129);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(308, 56);
            this.textBox_Beschreibung.TabIndex = 28;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Enabled = false;
            this.textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Firma.Location = new System.Drawing.Point(492, 192);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(308, 25);
            this.textBox_Firma.TabIndex = 29;
            // 
            // textBox_Modul_A
            // 
            this.textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Modul_A.Enabled = false;
            this.textBox_Modul_A.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Modul_A.Location = new System.Drawing.Point(506, 224);
            this.textBox_Modul_A.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Modul_A.Name = "textBox_Modul_A";
            this.textBox_Modul_A.Size = new System.Drawing.Size(113, 25);
            this.textBox_Modul_A.TabIndex = 30;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Location = new System.Drawing.Point(622, 228);
            this.Label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(24, 17);
            this.Label9.TabIndex = 31;
            this.Label9.Text = "m²";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(402, 67);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 34;
            this.label1.Text = "Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(492, 64);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(308, 25);
            this.textBox_Name.TabIndex = 35;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(25, 38);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 17);
            this.label3.TabIndex = 37;
            this.label3.Text = "Auswahl in DB:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(26, 59);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(359, 302);
            this.dataGridView1.TabIndex = 76;
            this.dataGridView1.Click += new System.EventHandler(this.dataGridView1_Click);
            this.dataGridView1.Leave += new System.EventHandler(this.dataGridView1_Leave);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Abbrechen.Location = new System.Drawing.Point(608, 452);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 80;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click_1);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(825, 31);
            this.label_Type.TabIndex = 81;
            this.label_Type.Text = "Eingabe der Solarkollektoren";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form_SolarKollektorenAdmin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(825, 494);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.btn_Kollektor_DB_Edit);
            this.Controls.Add(this.btn_Kollektor_DB_neu);
            this.Controls.Add(this.btn_Kollektor_DB_loeschen);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.textBox_Kollektor_A);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.Label6);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.textBox_Kollektortype);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.textBox_Modul_A);
            this.Controls.Add(this.Label9);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_SolarKollektorenAdmin";
            this.Text = "Eingabe der Solarkollektoren";
            this.Load += new System.EventHandler(this.Form_SolarKollektorenAdmin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
private System.Windows.Forms.Button btn_Kollektor_DB_Edit;
private System.Windows.Forms.Button btn_Kollektor_DB_neu;
private System.Windows.Forms.Button btn_Kollektor_DB_loeschen;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.TextBox textBox_Kollektor_A;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.Label Label6;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox textBox_Kollektortype;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.TextBox textBox_Modul_A;
private System.Windows.Forms.Label Label9;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label_Type;
    }
}