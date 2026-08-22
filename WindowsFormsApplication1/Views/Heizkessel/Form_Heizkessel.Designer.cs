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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Heizkessel));
            label11 = new System.Windows.Forms.Label();
            btn_Kessel_Entfernen = new System.Windows.Forms.Button();
            btn_Kessel_Hinzu = new System.Windows.Forms.Button();
            listBox_Kessel_DB = new System.Windows.Forms.ListBox();
            label12 = new System.Windows.Forms.Label();
            listBox_Kessel = new System.Windows.Forms.ListView();
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            comboBox_Brennstoffart = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            comboBox_Leistung = new System.Windows.Forms.ComboBox();
            btn_Bearbeiten = new System.Windows.Forms.Button();
            btn_Löschen = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            label_BrennstoffArt = new System.Windows.Forms.Label();
            cmbBrennstoffArt = new System.Windows.Forms.ComboBox();
            checkBox_Brennwert = new System.Windows.Forms.CheckBox();
            label3 = new System.Windows.Forms.Label();
            label18 = new System.Windows.Forms.Label();
            textBox_Kesselbeschreibung = new System.Windows.Forms.TextBox();
            textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            label_Kesseltyp = new System.Windows.Forms.Label();
            textBox_Kesseltyp = new System.Windows.Forms.TextBox();
            label14 = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            textBox_Kesselname = new System.Windows.Forms.TextBox();
            textBox_Kesselleistung = new System.Windows.Forms.TextBox();
            textBox_Ruecklauf = new System.Windows.Forms.TextBox();
            textBox_Vorlauf = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            label17 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label_Type = new System.Windows.Forms.Label();
            btn_Admin = new System.Windows.Forms.Button();
            btn_Help = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label11
            // 
            resources.ApplyResources(label11, "label11");
            label11.Name = "label11";
            // 
            // btn_Kessel_Entfernen
            // 
            resources.ApplyResources(btn_Kessel_Entfernen, "btn_Kessel_Entfernen");
            btn_Kessel_Entfernen.Name = "btn_Kessel_Entfernen";
            btn_Kessel_Entfernen.UseVisualStyleBackColor = true;
            btn_Kessel_Entfernen.Click += btn_Kessel_Entfernen_Click;
            // 
            // btn_Kessel_Hinzu
            // 
            resources.ApplyResources(btn_Kessel_Hinzu, "btn_Kessel_Hinzu");
            btn_Kessel_Hinzu.Name = "btn_Kessel_Hinzu";
            btn_Kessel_Hinzu.UseVisualStyleBackColor = true;
            btn_Kessel_Hinzu.Click += btn_Kessel_Hinzu_Click;
            // 
            // listBox_Kessel_DB
            // 
            resources.ApplyResources(listBox_Kessel_DB, "listBox_Kessel_DB");
            listBox_Kessel_DB.FormattingEnabled = true;
            listBox_Kessel_DB.Name = "listBox_Kessel_DB";
            listBox_Kessel_DB.SelectedIndexChanged += listBox_Kessel_DB_SelectedIndexChanged;
            // 
            // label12
            // 
            resources.ApplyResources(label12, "label12");
            label12.Name = "label12";
            // 
            // listBox_Kessel
            // 
            resources.ApplyResources(listBox_Kessel, "listBox_Kessel");
            listBox_Kessel.Name = "listBox_Kessel";
            listBox_Kessel.UseCompatibleStateImageBehavior = false;
            listBox_Kessel.SelectedIndexChanged += listBox_Kessel_SelectedIndexChanged_1;
            listBox_Kessel.MouseClick += listBox_Kessel_MouseClick;
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
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
            // btn_Bearbeiten
            // 
            resources.ApplyResources(btn_Bearbeiten, "btn_Bearbeiten");
            btn_Bearbeiten.Name = "btn_Bearbeiten";
            btn_Bearbeiten.UseVisualStyleBackColor = true;
            btn_Bearbeiten.Click += btn_Bearbeiten_Click;
            // 
            // btn_Löschen
            // 
            resources.ApplyResources(btn_Löschen, "btn_Löschen");
            btn_Löschen.Name = "btn_Löschen";
            btn_Löschen.UseVisualStyleBackColor = true;
            btn_Löschen.Click += btn_Löschen_Click;
            // 
            // groupBox1
            // 
            groupBox1.BackColor = System.Drawing.Color.White;
            groupBox1.Controls.Add(label_BrennstoffArt);
            groupBox1.Controls.Add(cmbBrennstoffArt);
            groupBox1.Controls.Add(checkBox_Brennwert);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label18);
            groupBox1.Controls.Add(textBox_Kesselbeschreibung);
            groupBox1.Controls.Add(textBox_Investitionskosten);
            groupBox1.Controls.Add(label_Kesseltyp);
            groupBox1.Controls.Add(textBox_Kesseltyp);
            groupBox1.Controls.Add(label14);
            groupBox1.Controls.Add(label16);
            groupBox1.Controls.Add(textBox_Kesselname);
            groupBox1.Controls.Add(textBox_Kesselleistung);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // label_BrennstoffArt
            // 
            resources.ApplyResources(label_BrennstoffArt, "label_BrennstoffArt");
            label_BrennstoffArt.Name = "label_BrennstoffArt";
            // 
            // cmbBrennstoffArt
            // 
            cmbBrennstoffArt.FormattingEnabled = true;
            resources.ApplyResources(cmbBrennstoffArt, "cmbBrennstoffArt");
            cmbBrennstoffArt.Name = "cmbBrennstoffArt";
            // 
            // checkBox_Brennwert
            // 
            resources.ApplyResources(checkBox_Brennwert, "checkBox_Brennwert");
            checkBox_Brennwert.Name = "checkBox_Brennwert";
            checkBox_Brennwert.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // label18
            // 
            resources.ApplyResources(label18, "label18");
            label18.Name = "label18";
            // 
            // textBox_Kesselbeschreibung
            // 
            textBox_Kesselbeschreibung.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(textBox_Kesselbeschreibung, "textBox_Kesselbeschreibung");
            textBox_Kesselbeschreibung.Name = "textBox_Kesselbeschreibung";
            textBox_Kesselbeschreibung.ReadOnly = true;
            // 
            // textBox_Investitionskosten
            // 
            textBox_Investitionskosten.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(textBox_Investitionskosten, "textBox_Investitionskosten");
            textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            textBox_Investitionskosten.ReadOnly = true;
            // 
            // label_Kesseltyp
            // 
            resources.ApplyResources(label_Kesseltyp, "label_Kesseltyp");
            label_Kesseltyp.Name = "label_Kesseltyp";
            // 
            // textBox_Kesseltyp
            // 
            textBox_Kesseltyp.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(textBox_Kesseltyp, "textBox_Kesseltyp");
            textBox_Kesseltyp.Name = "textBox_Kesseltyp";
            textBox_Kesseltyp.ReadOnly = true;
            // 
            // label14
            // 
            resources.ApplyResources(label14, "label14");
            label14.Name = "label14";
            // 
            // label16
            // 
            resources.ApplyResources(label16, "label16");
            label16.Name = "label16";
            // 
            // textBox_Kesselname
            // 
            textBox_Kesselname.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(textBox_Kesselname, "textBox_Kesselname");
            textBox_Kesselname.Name = "textBox_Kesselname";
            textBox_Kesselname.ReadOnly = true;
            // 
            // textBox_Kesselleistung
            // 
            textBox_Kesselleistung.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(textBox_Kesselleistung, "textBox_Kesselleistung");
            textBox_Kesselleistung.Name = "textBox_Kesselleistung";
            textBox_Kesselleistung.ReadOnly = true;
            // 
            // textBox_Ruecklauf
            // 
            textBox_Ruecklauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Ruecklauf, "textBox_Ruecklauf");
            textBox_Ruecklauf.Name = "textBox_Ruecklauf";
            textBox_Ruecklauf.Validating += textBox_Ruecklauf_Validating;
            // 
            // textBox_Vorlauf
            // 
            textBox_Vorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Vorlauf, "textBox_Vorlauf");
            textBox_Vorlauf.Name = "textBox_Vorlauf";
            textBox_Vorlauf.Validating += textBox_Vorlauf_Validating;
            // 
            // label6
            // 
            label6.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label6, "label6");
            label6.ForeColor = System.Drawing.Color.White;
            label6.Name = "label6";
            // 
            // label13
            // 
            label13.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label13, "label13");
            label13.ForeColor = System.Drawing.Color.White;
            label13.Name = "label13";
            // 
            // label17
            // 
            resources.ApplyResources(label17, "label17");
            label17.Name = "label17";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // label_Type
            // 
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.Name = "label_Type";
            // 
            // btn_Admin
            // 
            resources.ApplyResources(btn_Admin, "btn_Admin");
            btn_Admin.Name = "btn_Admin";
            btn_Admin.UseVisualStyleBackColor = true;
            btn_Admin.Click += btn_Admin_Click;
            // 
            // btn_Help
            // 
            btn_Help.BackColor = System.Drawing.Color.Transparent;
            btn_Help.BackgroundImage = Properties.Resources.help_icon;
            btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            btn_Help.FlatAppearance.BorderSize = 0;
            btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_Help.Location = new System.Drawing.Point(730, 33);
            btn_Help.Name = "btn_Help";
            btn_Help.Size = new System.Drawing.Size(24, 24);
            btn_Help.TabStop = false;
            btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_Heizkessel
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(btn_Help);
            Controls.Add(textBox_Ruecklauf);
            Controls.Add(btn_Admin);
            Controls.Add(textBox_Vorlauf);
            Controls.Add(label_Type);
            Controls.Add(label6);
            Controls.Add(groupBox1);
            Controls.Add(label13);
            Controls.Add(btn_Löschen);
            Controls.Add(label17);
            Controls.Add(label4);
            Controls.Add(btn_Bearbeiten);
            Controls.Add(label2);
            Controls.Add(comboBox_Leistung);
            Controls.Add(label1);
            Controls.Add(comboBox_Brennstoffart);
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_OK);
            Controls.Add(label11);
            Controls.Add(btn_Kessel_Entfernen);
            Controls.Add(btn_Kessel_Hinzu);
            Controls.Add(listBox_Kessel_DB);
            Controls.Add(label12);
            Controls.Add(listBox_Kessel);
            Name = "Form_Heizkessel";
            Load += Form_Heizkessel_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn_Kessel_Entfernen;
        private System.Windows.Forms.Button btn_Kessel_Hinzu;
        private System.Windows.Forms.ListBox listBox_Kessel_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListView listBox_Kessel;
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
        private System.Windows.Forms.Label label_Kesseltyp;
        private System.Windows.Forms.TextBox textBox_Kesseltyp;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Kesselname;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Button btn_Admin;
        private System.Windows.Forms.CheckBox checkBox_Brennwert;
        private System.Windows.Forms.TextBox textBox_Ruecklauf;
        private System.Windows.Forms.TextBox textBox_Vorlauf;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label_BrennstoffArt;
        private System.Windows.Forms.ComboBox cmbBrennstoffArt;
    }
}