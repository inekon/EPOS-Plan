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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Heizkessel_Admin));
            label18 = new System.Windows.Forms.Label();
            textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            label13 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            textBox_Kesselleistung = new System.Windows.Forms.TextBox();
            label16 = new System.Windows.Forms.Label();
            textBox_Kesselname = new System.Windows.Forms.TextBox();
            label11 = new System.Windows.Forms.Label();
            listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            btn_OK = new System.Windows.Forms.Button();
            comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            comboBox_Leistung = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            btn_Bearbeiten = new System.Windows.Forms.Button();
            btn_Neu = new System.Windows.Forms.Button();
            btn_Loeschen = new System.Windows.Forms.Button();
            label5 = new System.Windows.Forms.Label();
            textBox_Brennstoff = new System.Windows.Forms.TextBox();
            checkBox_Brennwert = new System.Windows.Forms.CheckBox();
            textBox_Ruecklauf = new System.Windows.Forms.TextBox();
            textBox_Vorlauf = new System.Windows.Forms.TextBox();
            label46 = new System.Windows.Forms.Label();
            label47 = new System.Windows.Forms.Label();
            label48 = new System.Windows.Forms.Label();
            label49 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // label18
            // 
            resources.ApplyResources(label18, "label18");
            label18.Name = "label18";
            // 
            // textBox_Kesselbeschreibung
            // 
            resources.ApplyResources(textBox_Kesselbeschreibung, "textBox_Kesselbeschreibung");
            textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            // 
            // label13
            // 
            label13.BackColor = System.Drawing.Color.DimGray;
            resources.ApplyResources(label13, "label13");
            label13.ForeColor = System.Drawing.Color.White;
            label13.Name = "label13";
            // 
            // label14
            // 
            resources.ApplyResources(label14, "label14");
            label14.Name = "label14";
            // 
            // textBox_Kesselleistung
            // 
            resources.ApplyResources(textBox_Kesselleistung, "textBox_Kesselleistung");
            textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            textBox_Kesselleistung.TextChanged += textBox_Kesselleistung_TextChanged;
            // 
            // label16
            // 
            resources.ApplyResources(label16, "label16");
            label16.Name = "label16";
            // 
            // textBox_Kesselname
            // 
            resources.ApplyResources(textBox_Kesselname, "textBox_Kesselname");
            textBox_Kesselname.Name = "textBox_Kesselname";
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.Name = "label11";
            // 
            // listBox_Kessel_DB
            // 
            resources.ApplyResources(listBox_Kessel_DB, "listBox_Kessel_DB");
            listBox_Kessel_DB.FormattingEnabled = true;
            listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            listBox_Kessel_DB.SelectedIndexChanged += listBox_Kessel_DB_SelectedIndexChanged;
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // comboBox_Brennstoffart
            // 
            comboBox_Brennstoffart.FormattingEnabled = true;
            resources.ApplyResources(comboBox_Brennstoffart, "comboBox_Brennstoffart");
            comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            comboBox_Brennstoffart.SelectedIndexChanged += comboBox_Brennstoffart_SelectedIndexChanged;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // comboBox_Leistung
            // 
            comboBox_Leistung.FormattingEnabled = true;
            resources.ApplyResources(comboBox_Leistung, "comboBox_Leistung");
            comboBox_Leistung.Name = "comboBox_Leistung";
            comboBox_Leistung.SelectedIndexChanged += comboBox_Leistung_SelectedIndexChanged;
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // textBox_Investitionskosten
            // 
            resources.ApplyResources(textBox_Investitionskosten, "textBox_Investitionskosten");
            textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            textBox_Investitionskosten.TextChanged += textBox_Investitionskosten_TextChanged;
            // 
            // label4
            // 
            label4.BackColor = System.Drawing.Color.DimGray;
            resources.ApplyResources(label4, "label4");
            label4.ForeColor = System.Drawing.Color.White;
            label4.Name = "label4";
            // 
            // btn_Bearbeiten
            // 
            resources.ApplyResources(btn_Bearbeiten, "btn_Bearbeiten");
            btn_Bearbeiten.Name = "btn_Bearbeiten";
            btn_Bearbeiten.UseVisualStyleBackColor = true;
            btn_Bearbeiten.Click += btn_Bearbeiten_Click;
            // 
            // btn_Neu
            // 
            resources.ApplyResources(btn_Neu, "btn_Neu");
            btn_Neu.Name = "btn_Neu";
            btn_Neu.UseVisualStyleBackColor = true;
            btn_Neu.Click += btn_Neu_Click;
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(btn_Loeschen, "btn_Loeschen");
            btn_Loeschen.Name = "btn_Loeschen";
            btn_Loeschen.UseVisualStyleBackColor = true;
            btn_Loeschen.Click += btn_Loeschen_Click;
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.Name = "label5";
            // 
            // textBox_Brennstoff
            // 
            resources.ApplyResources(textBox_Brennstoff, "textBox_Brennstoff");
            textBox_Brennstoff.Name = "textBox_Brennstoff";
            // 
            // checkBox_Brennwert
            // 
            resources.ApplyResources(checkBox_Brennwert, "checkBox_Brennwert");
            checkBox_Brennwert.Name = "checkBox_Brennwert";
            checkBox_Brennwert.UseVisualStyleBackColor = true;
            // 
            // textBox_Ruecklauf
            // 
            textBox_Ruecklauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Ruecklauf, "textBox_Ruecklauf");
            textBox_Ruecklauf.Name = "textBox_Ruecklauf";
            // 
            // textBox_Vorlauf
            // 
            textBox_Vorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Vorlauf, "textBox_Vorlauf");
            textBox_Vorlauf.Name = "textBox_Vorlauf";
            // 
            // label46
            // 
            label46.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(label46, "label46");
            label46.ForeColor = System.Drawing.Color.Black;
            label46.Name = "label46";
            // 
            // label47
            // 
            label47.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(label47, "label47");
            label47.ForeColor = System.Drawing.Color.Black;
            label47.Name = "label47";
            // 
            // label48
            // 
            resources.ApplyResources(label48, "label48");
            label48.Name = "label48";
            // 
            // label49
            // 
            resources.ApplyResources(label49, "label49");
            label49.Name = "label49";
            // 
            // Form_Heizkessel_Admin
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(textBox_Ruecklauf);
            Controls.Add(textBox_Vorlauf);
            Controls.Add(label46);
            Controls.Add(label47);
            Controls.Add(label48);
            Controls.Add(label49);
            Controls.Add(checkBox_Brennwert);
            Controls.Add(label5);
            Controls.Add(textBox_Brennstoff);
            Controls.Add(btn_Loeschen);
            Controls.Add(btn_Neu);
            Controls.Add(btn_Bearbeiten);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(textBox_Investitionskosten);
            Controls.Add(label2);
            Controls.Add(comboBox_Leistung);
            Controls.Add(label1);
            Controls.Add(comboBox_Brennstoffart);
            Controls.Add(btn_OK);
            Controls.Add(label18);
            Controls.Add(textBox_Kesselbeschreibung);
            Controls.Add(label13);
            Controls.Add(label14);
            Controls.Add(textBox_Kesselleistung);
            Controls.Add(label16);
            Controls.Add(textBox_Kesselname);
            Controls.Add(label11);
            Controls.Add(listBox_Kessel_DB);
            Name = "Form_Heizkessel_Admin";
            Load += Form_Heizkessel_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Kesselbeschreibung;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_Kesselleistung;
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Brennstoff;
        private System.Windows.Forms.CheckBox checkBox_Brennwert;
        private System.Windows.Forms.TextBox textBox_Ruecklauf;
        private System.Windows.Forms.TextBox textBox_Vorlauf;
        private System.Windows.Forms.Label label46;
        private System.Windows.Forms.Label label47;
        private System.Windows.Forms.Label label48;
        private System.Windows.Forms.Label label49;
    }
}