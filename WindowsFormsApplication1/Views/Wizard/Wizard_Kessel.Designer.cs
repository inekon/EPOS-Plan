namespace WindowsFormsApplication1
{
    partial class Wizard_Kessel
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
            this.label_Type = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_Kesselleistung = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_Kesseltyp = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Kesselname = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.btn_Kessel_Entfernen = new System.Windows.Forms.Button();
            this.btn_Kessel_Hinzu = new System.Windows.Forms.Button();
            this.listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            this.label12 = new System.Windows.Forms.Label();
            this.listBox_Kessel = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(656, 32);
            this.label_Type.TabIndex = 23;
            this.label_Type.Text = "Geben Sie Daten des Spitzenlastkessels ein";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(29, 304);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(91, 19);
            this.label18.TabIndex = 98;
            this.label18.Text = "Beschreibung";
            // 
            // textBox_Kesselbeschreibung
            // 
            this.textBox_Kesselbeschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselbeschreibung.Location = new System.Drawing.Point(121, 301);
            this.textBox_Kesselbeschreibung.Multiline = true;
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            this.textBox_Kesselbeschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Kesselbeschreibung.Size = new System.Drawing.Size(240, 40);
            this.textBox_Kesselbeschreibung.TabIndex = 97;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(185, 378);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(35, 19);
            this.label13.TabIndex = 96;
            this.label13.Text = "kW";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(29, 381);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(61, 19);
            this.label14.TabIndex = 95;
            this.label14.Text = "Leistung";
            // 
            // textBox_Kesselleistung
            // 
            this.textBox_Kesselleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselleistung.Location = new System.Drawing.Point(121, 378);
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Kesselleistung.TabIndex = 94;
            this.textBox_Kesselleistung.Text = "10";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(29, 350);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(30, 19);
            this.label15.TabIndex = 93;
            this.label15.Text = "Typ";
            // 
            // textBox_Kesseltyp
            // 
            this.textBox_Kesseltyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesseltyp.Location = new System.Drawing.Point(121, 347);
            this.textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            this.textBox_Kesseltyp.Size = new System.Drawing.Size(196, 25);
            this.textBox_Kesseltyp.TabIndex = 92;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(29, 273);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(45, 19);
            this.label16.TabIndex = 91;
            this.label16.Text = "Name";
            // 
            // textBox_Kesselname
            // 
            this.textBox_Kesselname.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselname.Location = new System.Drawing.Point(121, 270);
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            this.textBox_Kesselname.Size = new System.Drawing.Size(240, 25);
            this.textBox_Kesselname.TabIndex = 90;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(375, 74);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(142, 19);
            this.label11.TabIndex = 89;
            this.label11.Text = "Kessel aus Datenbank";
            // 
            // btn_Kessel_Entfernen
            // 
            this.btn_Kessel_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Entfernen.Location = new System.Drawing.Point(286, 158);
            this.btn_Kessel_Entfernen.Name = "btn_Kessel_Entfernen";
            this.btn_Kessel_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Kessel_Entfernen.TabIndex = 88;
            this.btn_Kessel_Entfernen.Text = "-->";
            this.btn_Kessel_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Kessel_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Kessel_Hinzu
            // 
            this.btn_Kessel_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Hinzu.Location = new System.Drawing.Point(286, 123);
            this.btn_Kessel_Hinzu.Name = "btn_Kessel_Hinzu";
            this.btn_Kessel_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Kessel_Hinzu.TabIndex = 87;
            this.btn_Kessel_Hinzu.Text = "<---";
            this.btn_Kessel_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Kessel_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // listBox_Kessel_DB
            // 
            this.listBox_Kessel_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel_DB.FormattingEnabled = true;
            this.listBox_Kessel_DB.HorizontalScrollbar = true;
            this.listBox_Kessel_DB.ItemHeight = 17;
            this.listBox_Kessel_DB.Location = new System.Drawing.Point(379, 96);
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.Size = new System.Drawing.Size(250, 157);
            this.listBox_Kessel_DB.TabIndex = 86;
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.Location = new System.Drawing.Point(27, 74);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(126, 19);
            this.label12.TabIndex = 85;
            this.label12.Text = "ausgewählte Kessel";
            // 
            // listBox_Kessel
            // 
            this.listBox_Kessel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel.FormattingEnabled = true;
            this.listBox_Kessel.HorizontalScrollbar = true;
            this.listBox_Kessel.ItemHeight = 17;
            this.listBox_Kessel.Location = new System.Drawing.Point(29, 96);
            this.listBox_Kessel.Name = "listBox_Kessel";
            this.listBox_Kessel.Size = new System.Drawing.Size(238, 157);
            this.listBox_Kessel.TabIndex = 84;
            this.listBox_Kessel.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(485, 322);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 108;
            this.label2.Text = "Filtern nach Leistung:";
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.FormattingEnabled = true;
            this.comboBox_Leistung.Location = new System.Drawing.Point(488, 338);
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.Size = new System.Drawing.Size(125, 21);
            this.comboBox_Leistung.TabIndex = 107;
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(485, 268);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 13);
            this.label1.TabIndex = 106;
            this.label1.Text = "Filtern nach Brennstoffart:";
            // 
            // comboBox_Brennstoffart
            // 
            this.comboBox_Brennstoffart.FormattingEnabled = true;
            this.comboBox_Brennstoffart.Location = new System.Drawing.Point(488, 284);
            this.comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            this.comboBox_Brennstoffart.Size = new System.Drawing.Size(125, 21);
            this.comboBox_Brennstoffart.TabIndex = 105;
            this.comboBox_Brennstoffart.Text = "Gas Brennwert";
            this.comboBox_Brennstoffart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoffart_SelectedIndexChanged);
            // 
            // Wizard_Kessel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(656, 477);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_Brennstoffart);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.textBox_Kesselbeschreibung);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.textBox_Kesselleistung);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.textBox_Kesseltyp);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.textBox_Kesselname);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.btn_Kessel_Entfernen);
            this.Controls.Add(this.btn_Kessel_Hinzu);
            this.Controls.Add(this.listBox_Kessel_DB);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.listBox_Kessel);
            this.Controls.Add(this.label_Type);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Kessel";
            this.Text = "Stromspeicher";
            this.Load += new System.EventHandler(this.Wizard_Kessel_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Kesselbeschreibung;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_Kesselleistung;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_Kesseltyp;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Kesselname;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn_Kessel_Entfernen;
        private System.Windows.Forms.Button btn_Kessel_Hinzu;
        private System.Windows.Forms.ListBox listBox_Kessel_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListBox listBox_Kessel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Leistung;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Brennstoffart;
    }
}