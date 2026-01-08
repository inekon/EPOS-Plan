namespace WindowsFormsApplication1
{
    partial class Form_Heizkessel
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
            this.label11 = new System.Windows.Forms.Label();
            this.btn_Kessel_Entfernen = new System.Windows.Forms.Button();
            this.btn_Kessel_Hinzu = new System.Windows.Forms.Button();
            this.listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            this.label12 = new System.Windows.Forms.Label();
            this.listBox_Kessel = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_Kesselleistung = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_Kesseltyp = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Kesselname = new System.Windows.Forms.TextBox();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(465, 35);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(142, 19);
            this.label11.TabIndex = 89;
            this.label11.Text = "Kessel aus Datenbank";
            // 
            // btn_Kessel_Entfernen
            // 
            this.btn_Kessel_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Entfernen.Location = new System.Drawing.Point(347, 139);
            this.btn_Kessel_Entfernen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kessel_Entfernen.Name = "btn_Kessel_Entfernen";
            this.btn_Kessel_Entfernen.Size = new System.Drawing.Size(88, 37);
            this.btn_Kessel_Entfernen.TabIndex = 88;
            this.btn_Kessel_Entfernen.Text = "-->";
            this.btn_Kessel_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Kessel_Entfernen.Click += new System.EventHandler(this.btn_Kessel_Entfernen_Click);
            // 
            // btn_Kessel_Hinzu
            // 
            this.btn_Kessel_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Hinzu.Location = new System.Drawing.Point(347, 93);
            this.btn_Kessel_Hinzu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Kessel_Hinzu.Name = "btn_Kessel_Hinzu";
            this.btn_Kessel_Hinzu.Size = new System.Drawing.Size(88, 38);
            this.btn_Kessel_Hinzu.TabIndex = 87;
            this.btn_Kessel_Hinzu.Text = "<---";
            this.btn_Kessel_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Kessel_Hinzu.Click += new System.EventHandler(this.btn_Kessel_Hinzu_Click);
            // 
            // listBox_Kessel_DB
            // 
            this.listBox_Kessel_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel_DB.FormattingEnabled = true;
            this.listBox_Kessel_DB.HorizontalScrollbar = true;
            this.listBox_Kessel_DB.ItemHeight = 17;
            this.listBox_Kessel_DB.Location = new System.Drawing.Point(465, 58);
            this.listBox_Kessel_DB.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.Size = new System.Drawing.Size(291, 191);
            this.listBox_Kessel_DB.TabIndex = 86;
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.Location = new System.Drawing.Point(36, 35);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(144, 19);
            this.label12.TabIndex = 85;
            this.label12.Text = "ausgewählt im Projekt";
            // 
            // listBox_Kessel
            // 
            this.listBox_Kessel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel.FormattingEnabled = true;
            this.listBox_Kessel.HorizontalScrollbar = true;
            this.listBox_Kessel.ItemHeight = 17;
            this.listBox_Kessel.Location = new System.Drawing.Point(38, 58);
            this.listBox_Kessel.Margin = new System.Windows.Forms.Padding(4);
            this.listBox_Kessel.Name = "listBox_Kessel";
            this.listBox_Kessel.Size = new System.Drawing.Size(277, 191);
            this.listBox_Kessel.TabIndex = 84;
            this.listBox_Kessel.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_SelectedIndexChanged_1);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(543, 453);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(106, 34);
            this.btn_Abbrechen.TabIndex = 99;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(657, 453);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(106, 34);
            this.btn_OK.TabIndex = 100;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // comboBox_Brennstoffart
            // 
            this.comboBox_Brennstoffart.FormattingEnabled = true;
            this.comboBox_Brennstoffart.Location = new System.Drawing.Point(469, 277);
            this.comboBox_Brennstoffart.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            this.comboBox_Brennstoffart.Size = new System.Drawing.Size(145, 25);
            this.comboBox_Brennstoffart.TabIndex = 101;
            this.comboBox_Brennstoffart.Text = "Gas Brennwert";
            this.comboBox_Brennstoffart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoffart_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(466, 256);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 17);
            this.label1.TabIndex = 102;
            this.label1.Text = "Filtern nach Brennstoffart:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(466, 312);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 17);
            this.label2.TabIndex = 104;
            this.label2.Text = "Filtern nach Leistung:";
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.FormattingEnabled = true;
            this.comboBox_Leistung.Location = new System.Drawing.Point(469, 333);
            this.comboBox_Leistung.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.Size = new System.Drawing.Size(145, 25);
            this.comboBox_Leistung.TabIndex = 103;
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(657, 276);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(99, 32);
            this.btn_Bearbeiten.TabIndex = 108;
            this.btn_Bearbeiten.Text = "Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // btn_Löschen
            // 
            this.btn_Löschen.Location = new System.Drawing.Point(657, 315);
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.Size = new System.Drawing.Size(99, 28);
            this.btn_Löschen.TabIndex = 109;
            this.btn_Löschen.Text = "Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBox_Investitionskosten);
            this.groupBox1.Controls.Add(this.label18);
            this.groupBox1.Controls.Add(this.textBox_Kesselbeschreibung);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.textBox_Kesselleistung);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.textBox_Kesseltyp);
            this.groupBox1.Controls.Add(this.label16);
            this.groupBox1.Controls.Add(this.textBox_Kesselname);
            this.groupBox1.Location = new System.Drawing.Point(12, 256);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(407, 218);
            this.groupBox1.TabIndex = 110;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Modul";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(190, 186);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(143, 19);
            this.label3.TabIndex = 116;
            this.label3.Text = "Investitionskosten [€]:";
            // 
            // textBox_Investitionskosten
            // 
            this.textBox_Investitionskosten.Enabled = false;
            this.textBox_Investitionskosten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Investitionskosten.Location = new System.Drawing.Point(335, 183);
            this.textBox_Investitionskosten.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            this.textBox_Investitionskosten.Size = new System.Drawing.Size(63, 25);
            this.textBox_Investitionskosten.TabIndex = 115;
            this.textBox_Investitionskosten.Text = "10000";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(11, 59);
            this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(94, 19);
            this.label18.TabIndex = 114;
            this.label18.Text = "Beschreibung:";
            // 
            // textBox_Kesselbeschreibung
            // 
            this.textBox_Kesselbeschreibung.Enabled = false;
            this.textBox_Kesselbeschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselbeschreibung.Location = new System.Drawing.Point(115, 55);
            this.textBox_Kesselbeschreibung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Kesselbeschreibung.Multiline = true;
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            this.textBox_Kesselbeschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Kesselbeschreibung.Size = new System.Drawing.Size(282, 89);
            this.textBox_Kesselbeschreibung.TabIndex = 113;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(11, 186);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(100, 19);
            this.label14.TabIndex = 112;
            this.label14.Text = "Leistung []kW]:";
            // 
            // textBox_Kesselleistung
            // 
            this.textBox_Kesselleistung.Enabled = false;
            this.textBox_Kesselleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselleistung.Location = new System.Drawing.Point(115, 183);
            this.textBox_Kesselleistung.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.Size = new System.Drawing.Size(67, 25);
            this.textBox_Kesselleistung.TabIndex = 111;
            this.textBox_Kesselleistung.Text = "10";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(11, 153);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 19);
            this.label15.TabIndex = 110;
            this.label15.Text = "Typ:";
            // 
            // textBox_Kesseltyp
            // 
            this.textBox_Kesseltyp.Enabled = false;
            this.textBox_Kesseltyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesseltyp.Location = new System.Drawing.Point(115, 150);
            this.textBox_Kesseltyp.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            this.textBox_Kesseltyp.Size = new System.Drawing.Size(283, 25);
            this.textBox_Kesseltyp.TabIndex = 109;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(11, 27);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(48, 19);
            this.label16.TabIndex = 108;
            this.label16.Text = "Name:";
            // 
            // textBox_Kesselname
            // 
            this.textBox_Kesselname.Enabled = false;
            this.textBox_Kesselname.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselname.Location = new System.Drawing.Point(115, 23);
            this.textBox_Kesselname.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            this.textBox_Kesselname.Size = new System.Drawing.Size(282, 25);
            this.textBox_Kesselname.TabIndex = 107;
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(774, 32);
            this.label_Type.TabIndex = 111;
            this.label_Type.Text = "Geben Sie Daten des Spitzenlastkessels ein";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Form_Heizkessel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(774, 496);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Löschen);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_Brennstoffart);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.btn_Kessel_Entfernen);
            this.Controls.Add(this.btn_Kessel_Hinzu);
            this.Controls.Add(this.listBox_Kessel_DB);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.listBox_Kessel);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_Heizkessel";
            this.Text = "Verwaltung Heizkessel";
            this.Load += new System.EventHandler(this.Form_Heizkessel_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn_Kessel_Entfernen;
        private System.Windows.Forms.Button btn_Kessel_Hinzu;
        private System.Windows.Forms.ListBox listBox_Kessel_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListBox listBox_Kessel;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ComboBox comboBox_Brennstoffart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Leistung;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Investitionskosten;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Kesselbeschreibung;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_Kesselleistung;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_Kesseltyp;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Kesselname;
        private System.Windows.Forms.Label label_Type;
    }
}