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
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(16, 326);
            this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(91, 19);
            this.label18.TabIndex = 98;
            this.label18.Text = "Beschreibung";
            // 
            // textBox_Kesselbeschreibung
            // 
            this.textBox_Kesselbeschreibung.Enabled = false;
            this.textBox_Kesselbeschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselbeschreibung.Location = new System.Drawing.Point(124, 322);
            this.textBox_Kesselbeschreibung.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Kesselbeschreibung.Multiline = true;
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            this.textBox_Kesselbeschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Kesselbeschreibung.Size = new System.Drawing.Size(300, 89);
            this.textBox_Kesselbeschreibung.TabIndex = 97;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.DimGray;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(198, 464);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(38, 25);
            this.label13.TabIndex = 96;
            this.label13.Text = "kW";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(16, 463);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(61, 19);
            this.label14.TabIndex = 95;
            this.label14.Text = "Leistung";
            // 
            // textBox_Kesselleistung
            // 
            this.textBox_Kesselleistung.Enabled = false;
            this.textBox_Kesselleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselleistung.Location = new System.Drawing.Point(124, 460);
            this.textBox_Kesselleistung.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.Size = new System.Drawing.Size(67, 25);
            this.textBox_Kesselleistung.TabIndex = 94;
            this.textBox_Kesselleistung.Text = "10";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(16, 422);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(30, 19);
            this.label15.TabIndex = 93;
            this.label15.Text = "Typ";
            // 
            // textBox_Kesseltyp
            // 
            this.textBox_Kesseltyp.Enabled = false;
            this.textBox_Kesseltyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesseltyp.Location = new System.Drawing.Point(124, 420);
            this.textBox_Kesseltyp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            this.textBox_Kesseltyp.Size = new System.Drawing.Size(206, 25);
            this.textBox_Kesseltyp.TabIndex = 92;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(16, 285);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(45, 19);
            this.label16.TabIndex = 91;
            this.label16.Text = "Name";
            // 
            // textBox_Kesselname
            // 
            this.textBox_Kesselname.Enabled = false;
            this.textBox_Kesselname.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselname.Location = new System.Drawing.Point(124, 281);
            this.textBox_Kesselname.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            this.textBox_Kesselname.Size = new System.Drawing.Size(300, 25);
            this.textBox_Kesselname.TabIndex = 90;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(465, 29);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(142, 19);
            this.label11.TabIndex = 89;
            this.label11.Text = "Kessel aus Datenbank";
            // 
            // btn_Kessel_Entfernen
            // 
            this.btn_Kessel_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Kessel_Entfernen.Location = new System.Drawing.Point(349, 139);
            this.btn_Kessel_Entfernen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.btn_Kessel_Hinzu.Location = new System.Drawing.Point(349, 93);
            this.btn_Kessel_Hinzu.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.listBox_Kessel_DB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.Size = new System.Drawing.Size(291, 191);
            this.listBox_Kessel_DB.TabIndex = 86;
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label12.Location = new System.Drawing.Point(36, 29);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
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
            this.listBox_Kessel.Location = new System.Drawing.Point(38, 58);
            this.listBox_Kessel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBox_Kessel.Name = "listBox_Kessel";
            this.listBox_Kessel.Size = new System.Drawing.Size(277, 191);
            this.listBox_Kessel.TabIndex = 84;
            this.listBox_Kessel.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_SelectedIndexChanged_1);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(559, 534);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(106, 41);
            this.btn_Abbrechen.TabIndex = 99;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(681, 534);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(90, 41);
            this.btn_OK.TabIndex = 100;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // comboBox_Brennstoffart
            // 
            this.comboBox_Brennstoffart.FormattingEnabled = true;
            this.comboBox_Brennstoffart.Location = new System.Drawing.Point(611, 299);
            this.comboBox_Brennstoffart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            this.comboBox_Brennstoffart.Size = new System.Drawing.Size(145, 25);
            this.comboBox_Brennstoffart.TabIndex = 101;
            this.comboBox_Brennstoffart.Text = "Gas Brennwert";
            this.comboBox_Brennstoffart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoffart_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(608, 279);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 17);
            this.label1.TabIndex = 102;
            this.label1.Text = "Filtern nach Brennstoffart:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(608, 349);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 17);
            this.label2.TabIndex = 104;
            this.label2.Text = "Filtern nach Leistung:";
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.FormattingEnabled = true;
            this.comboBox_Leistung.Location = new System.Drawing.Point(611, 370);
            this.comboBox_Leistung.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.Size = new System.Drawing.Size(145, 25);
            this.comboBox_Leistung.TabIndex = 103;
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(16, 496);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 51);
            this.label3.TabIndex = 106;
            this.label3.Text = "Investitions-kosten:";
            // 
            // textBox_Investitionskosten
            // 
            this.textBox_Investitionskosten.Enabled = false;
            this.textBox_Investitionskosten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Investitionskosten.Location = new System.Drawing.Point(124, 502);
            this.textBox_Investitionskosten.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            this.textBox_Investitionskosten.Size = new System.Drawing.Size(112, 25);
            this.textBox_Investitionskosten.TabIndex = 105;
            this.textBox_Investitionskosten.Text = "10";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.DimGray;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(244, 507);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 25);
            this.label4.TabIndex = 107;
            this.label4.Text = "€";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form_Heizkessel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(786, 588);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Investitionskosten);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_Brennstoffart);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
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
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form_Heizkessel";
            this.Text = "Verwaltung Heizkessel";
            this.Load += new System.EventHandler(this.Form_Heizkessel_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

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
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ComboBox comboBox_Brennstoffart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Leistung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Investitionskosten;
        private System.Windows.Forms.Label label4;
    }
}