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
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_Kesselleistung = new System.Windows.Forms.TextBox();
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
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Brennstoff = new System.Windows.Forms.TextBox();
            this.checkBox_Brennwert = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // textBox_Kesselbeschreibung
            // 
            resources.ApplyResources(this.textBox_Kesselbeschreibung, "textBox_Kesselbeschreibung");
            this.textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.Color.DimGray;
            resources.ApplyResources(this.label13, "label13");
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Name = "label13";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // textBox_Kesselleistung
            // 
            resources.ApplyResources(this.textBox_Kesselleistung, "textBox_Kesselleistung");
            this.textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            this.textBox_Kesselleistung.TextChanged += new System.EventHandler(this.textBox_Kesselleistung_TextChanged);
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // textBox_Kesselname
            // 
            resources.ApplyResources(this.textBox_Kesselname, "textBox_Kesselname");
            this.textBox_Kesselname.Name = "textBox_Kesselname";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // listBox_Kessel_DB
            // 
            resources.ApplyResources(this.listBox_Kessel_DB, "listBox_Kessel_DB");
            this.listBox_Kessel_DB.FormattingEnabled = true;
            this.listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            this.listBox_Kessel_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Kessel_DB_SelectedIndexChanged);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // comboBox_Brennstoffart
            // 
            this.comboBox_Brennstoffart.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Brennstoffart, "comboBox_Brennstoffart");
            this.comboBox_Brennstoffart.Name = "comboBox_Brennstoffart";
            this.comboBox_Brennstoffart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoffart_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Leistung, "comboBox_Leistung");
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // textBox_Investitionskosten
            // 
            resources.ApplyResources(this.textBox_Investitionskosten, "textBox_Investitionskosten");
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            this.textBox_Investitionskosten.TextChanged += new System.EventHandler(this.textBox_Investitionskosten_TextChanged);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.DimGray;
            resources.ApplyResources(this.label4, "label4");
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // btn_Bearbeiten
            // 
            resources.ApplyResources(this.btn_Bearbeiten, "btn_Bearbeiten");
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(this.btn_Loeschen, "btn_Loeschen");
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // textBox_Brennstoff
            // 
            resources.ApplyResources(this.textBox_Brennstoff, "textBox_Brennstoff");
            this.textBox_Brennstoff.Name = "textBox_Brennstoff";
            // 
            // checkBox_Brennwert
            // 
            resources.ApplyResources(this.checkBox_Brennwert, "checkBox_Brennwert");
            this.checkBox_Brennwert.Name = "checkBox_Brennwert";
            this.checkBox_Brennwert.UseVisualStyleBackColor = true;
            // 
            // Form_Heizkessel_Admin
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox_Brennwert);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Brennstoff);
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
            this.Controls.Add(this.label16);
            this.Controls.Add(this.textBox_Kesselname);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.listBox_Kessel_DB);
            this.Name = "Form_Heizkessel_Admin";
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
    }
}