namespace WindowsFormsApplication1
{
    partial class Form_Heizkessel_Admin
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
            this.listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label18.Location = new System.Drawing.Point(350, 59);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(94, 19);
            this.label18.TabIndex = 98;
            this.label18.Text = "Beschreibung:";
            // 
            // textBox_Kesselbeschreibung
            // 
            this.textBox_Kesselbeschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselbeschreibung.Location = new System.Drawing.Point(448, 56);
            this.textBox_Kesselbeschreibung.Multiline = true;
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            this.textBox_Kesselbeschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Kesselbeschreibung.Size = new System.Drawing.Size(258, 69);
            this.textBox_Kesselbeschreibung.TabIndex = 97;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.DimGray;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(512, 172);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(33, 19);
            this.label13.TabIndex = 96;
            this.label13.Text = "kW";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label14.Location = new System.Drawing.Point(379, 172);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(64, 19);
            this.label14.TabIndex = 95;
            this.label14.Text = "Leistung:";
            // 
            // textBox_Kesselleistung
            // 
            this.textBox_Kesselleistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselleistung.Location = new System.Drawing.Point(448, 169);
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Kesselleistung.TabIndex = 94;
            this.textBox_Kesselleistung.Text = "10";
            this.textBox_Kesselleistung.TextChanged += new System.EventHandler(this.textBox_Kesselleistung_TextChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.Location = new System.Drawing.Point(411, 138);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 19);
            this.label15.TabIndex = 93;
            this.label15.Text = "Typ:";
            // 
            // textBox_Kesseltyp
            // 
            this.textBox_Kesseltyp.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesseltyp.Location = new System.Drawing.Point(448, 135);
            this.textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            this.textBox_Kesseltyp.Size = new System.Drawing.Size(258, 25);
            this.textBox_Kesseltyp.TabIndex = 92;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label16.Location = new System.Drawing.Point(396, 28);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(48, 19);
            this.label16.TabIndex = 91;
            this.label16.Text = "Name:";
            // 
            // textBox_Kesselname
            // 
            this.textBox_Kesselname.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Kesselname.Location = new System.Drawing.Point(448, 25);
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            this.textBox_Kesselname.Size = new System.Drawing.Size(258, 25);
            this.textBox_Kesselname.TabIndex = 90;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.Location = new System.Drawing.Point(16, 8);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(142, 19);
            this.label11.TabIndex = 89;
            this.label11.Text = "Kessel aus Datenbank";
            // 
            // listBox_Kessel_DB
            // 
            this.listBox_Kessel_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_Kessel_DB.FormattingEnabled = true;
            this.listBox_Kessel_DB.HorizontalScrollbar = true;
            this.listBox_Kessel_DB.ItemHeight = 17;
            this.listBox_Kessel_DB.Location = new System.Drawing.Point(12, 30);
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.Size = new System.Drawing.Size(271, 191);
            this.listBox_Kessel_DB.TabIndex = 86;
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(632, 348);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(77, 31);
            this.btn_OK.TabIndex = 100;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // comboBox_Brennstoffart
            // 
            this.comboBox_Brennstoffart.FormattingEnabled = true;
            this.comboBox_Brennstoffart.Location = new System.Drawing.Point(20, 248);
            this.comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            this.comboBox_Brennstoffart.Size = new System.Drawing.Size(125, 21);
            this.comboBox_Brennstoffart.TabIndex = 101;
            this.comboBox_Brennstoffart.Text = "Gas Brennwert";
            this.comboBox_Brennstoffart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoffart_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 232);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 13);
            this.label1.TabIndex = 102;
            this.label1.Text = "Filtern nach Brennstoffart:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(155, 232);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 104;
            this.label2.Text = "Filtern nach Leistung:";
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.FormattingEnabled = true;
            this.comboBox_Leistung.Location = new System.Drawing.Point(158, 248);
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.Size = new System.Drawing.Size(125, 21);
            this.comboBox_Leistung.TabIndex = 103;
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(320, 201);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(127, 25);
            this.label3.TabIndex = 106;
            this.label3.Text = "Investitionskosten:";
            // 
            // textBox_Investitionskosten
            // 
            this.textBox_Investitionskosten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Investitionskosten.Location = new System.Drawing.Point(448, 201);
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            this.textBox_Investitionskosten.Size = new System.Drawing.Size(97, 25);
            this.textBox_Investitionskosten.TabIndex = 105;
            this.textBox_Investitionskosten.Text = "10";
            this.textBox_Investitionskosten.TextChanged += new System.EventHandler(this.textBox_Investitionskosten_TextChanged);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.DimGray;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(551, 203);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 19);
            this.label4.TabIndex = 107;
            this.label4.Text = "€";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Bearbeiten.Location = new System.Drawing.Point(158, 290);
            this.btn_Bearbeiten.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(138, 31);
            this.btn_Bearbeiten.TabIndex = 108;
            this.btn_Bearbeiten.Text = "Kessel Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // btn_Neu
            // 
            this.btn_Neu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Neu.Location = new System.Drawing.Point(318, 290);
            this.btn_Neu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(125, 31);
            this.btn_Neu.TabIndex = 109;
            this.btn_Neu.Text = "Neuer Kessel...";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Loeschen.Location = new System.Drawing.Point(12, 290);
            this.btn_Loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(125, 31);
            this.btn_Loeschen.TabIndex = 110;
            this.btn_Loeschen.Text = "Kessel Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // Form_Heizkessel_Admin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 393);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Investitionskosten);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_Brennstoffart);
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
            this.Controls.Add(this.listBox_Kessel_DB);
            this.Name = "Form_Heizkessel_Admin";
            this.Text = "Administration Heizkessel";
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
        private System.Windows.Forms.ListBox listBox_Kessel_DB;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ComboBox comboBox_Brennstoffart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Leistung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Investitionskosten;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.Button btn_Loeschen;
    }
}