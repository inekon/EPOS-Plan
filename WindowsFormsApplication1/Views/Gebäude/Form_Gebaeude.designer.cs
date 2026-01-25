namespace WindowsFormsApplication1
{
    partial class Form_Gebaeude
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Gebaeude));
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioBtn_Sonstige = new System.Windows.Forms.RadioButton();
            this.radioBtn_Wohngebäude = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Baujahr = new System.Windows.Forms.ComboBox();
            this.comboBox_Gebäudeart = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox_TypEinheit = new System.Windows.Forms.TextBox();
            this.textBox_Wohnflaeche = new System.Windows.Forms.TextBox();
            this.btn_Aendern = new System.Windows.Forms.Button();
            this.textBox_Gebäudename = new System.Windows.Forms.TextBox();
            this.textBox_Gebaeudeart = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label_ListProjektGebaeude = new System.Windows.Forms.Label();
            this.label_ListGebaeudeDB = new System.Windows.Forms.Label();
            this.textBox_Baujahr = new System.Windows.Forms.TextBox();
            this.textBox_Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            this.checkBox_dezWarmwasser = new System.Windows.Forms.CheckBox();
            this.textBox_ID_Gebaeude = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.btn_GebAendern_DB = new System.Windows.Forms.Button();
            this.btn_Geb_Neu_DB = new System.Windows.Forms.Button();
            this.btn_GebLoeschen_DB = new System.Windows.Forms.Button();
            this.btn_GebTypAendern_DB = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(this.btn_Entfernen, "btn_Entfernen");
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(this.btn_Hinzu, "btn_Hinzu");
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioBtn_Sonstige);
            this.groupBox1.Controls.Add(this.radioBtn_Wohngebäude);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.comboBox_Baujahr);
            this.groupBox1.Controls.Add(this.comboBox_Gebäudeart);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // radioBtn_Sonstige
            // 
            resources.ApplyResources(this.radioBtn_Sonstige, "radioBtn_Sonstige");
            this.radioBtn_Sonstige.Name = "radioBtn_Sonstige";
            this.radioBtn_Sonstige.UseVisualStyleBackColor = true;
            this.radioBtn_Sonstige.Click += new System.EventHandler(this.radioBtn_Sonstige_CheckedChanged);
            // 
            // radioBtn_Wohngebäude
            // 
            resources.ApplyResources(this.radioBtn_Wohngebäude, "radioBtn_Wohngebäude");
            this.radioBtn_Wohngebäude.Checked = true;
            this.radioBtn_Wohngebäude.Name = "radioBtn_Wohngebäude";
            this.radioBtn_Wohngebäude.TabStop = true;
            this.radioBtn_Wohngebäude.UseVisualStyleBackColor = true;
            this.radioBtn_Wohngebäude.Click += new System.EventHandler(this.radioBtn_Wohngebäude_CheckedChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // comboBox_Baujahr
            // 
            this.comboBox_Baujahr.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Baujahr, "comboBox_Baujahr");
            this.comboBox_Baujahr.Name = "comboBox_Baujahr";
            this.comboBox_Baujahr.SelectedIndexChanged += new System.EventHandler(this.comboBox_Baujahr_SelectedIndexChanged);
            // 
            // comboBox_Gebäudeart
            // 
            this.comboBox_Gebäudeart.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Gebäudeart, "comboBox_Gebäudeart");
            this.comboBox_Gebäudeart.Name = "comboBox_Gebäudeart";
            this.comboBox_Gebäudeart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Gebäudeart_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox_TypEinheit);
            this.groupBox2.Controls.Add(this.textBox_Wohnflaeche);
            this.groupBox2.Controls.Add(this.btn_Aendern);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // textBox_TypEinheit
            // 
            resources.ApplyResources(this.textBox_TypEinheit, "textBox_TypEinheit");
            this.textBox_TypEinheit.Name = "textBox_TypEinheit";
            // 
            // textBox_Wohnflaeche
            // 
            resources.ApplyResources(this.textBox_Wohnflaeche, "textBox_Wohnflaeche");
            this.textBox_Wohnflaeche.Name = "textBox_Wohnflaeche";
            // 
            // btn_Aendern
            // 
            resources.ApplyResources(this.btn_Aendern, "btn_Aendern");
            this.btn_Aendern.Name = "btn_Aendern";
            this.btn_Aendern.UseVisualStyleBackColor = true;
            this.btn_Aendern.Click += new System.EventHandler(this.btn_Aendern_Click);
            // 
            // textBox_Gebäudename
            // 
            resources.ApplyResources(this.textBox_Gebäudename, "textBox_Gebäudename");
            this.textBox_Gebäudename.Name = "textBox_Gebäudename";
            // 
            // textBox_Gebaeudeart
            // 
            resources.ApplyResources(this.textBox_Gebaeudeart, "textBox_Gebaeudeart");
            this.textBox_Gebaeudeart.Name = "textBox_Gebaeudeart";
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label_ListProjektGebaeude
            // 
            resources.ApplyResources(this.label_ListProjektGebaeude, "label_ListProjektGebaeude");
            this.label_ListProjektGebaeude.Name = "label_ListProjektGebaeude";
            // 
            // label_ListGebaeudeDB
            // 
            resources.ApplyResources(this.label_ListGebaeudeDB, "label_ListGebaeudeDB");
            this.label_ListGebaeudeDB.Name = "label_ListGebaeudeDB";
            // 
            // textBox_Baujahr
            // 
            resources.ApplyResources(this.textBox_Baujahr, "textBox_Baujahr");
            this.textBox_Baujahr.Name = "textBox_Baujahr";
            // 
            // textBox_Jahresnutzungsgrad
            // 
            resources.ApplyResources(this.textBox_Jahresnutzungsgrad, "textBox_Jahresnutzungsgrad");
            this.textBox_Jahresnutzungsgrad.Name = "textBox_Jahresnutzungsgrad";
            // 
            // checkBox_dezWarmwasser
            // 
            resources.ApplyResources(this.checkBox_dezWarmwasser, "checkBox_dezWarmwasser");
            this.checkBox_dezWarmwasser.Name = "checkBox_dezWarmwasser";
            this.checkBox_dezWarmwasser.UseVisualStyleBackColor = true;
            // 
            // textBox_ID_Gebaeude
            // 
            resources.ApplyResources(this.textBox_ID_Gebaeude, "textBox_ID_Gebaeude");
            this.textBox_ID_Gebaeude.Name = "textBox_ID_Gebaeude";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // listView_Gebaeude
            // 
            this.listView_Gebaeude.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView_Gebaeude.HideSelection = false;
            resources.ApplyResources(this.listView_Gebaeude, "listView_Gebaeude");
            this.listView_Gebaeude.Name = "listView_Gebaeude";
            this.listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            this.listView_Gebaeude.SelectedIndexChanged += new System.EventHandler(this.listView_Gebaeude_SelectedIndexChanged);
            // 
            // btn_GebAendern_DB
            // 
            resources.ApplyResources(this.btn_GebAendern_DB, "btn_GebAendern_DB");
            this.btn_GebAendern_DB.Name = "btn_GebAendern_DB";
            this.btn_GebAendern_DB.UseVisualStyleBackColor = true;
            this.btn_GebAendern_DB.Click += new System.EventHandler(this.btn_GebAendern_DB_Click);
            // 
            // btn_Geb_Neu_DB
            // 
            resources.ApplyResources(this.btn_Geb_Neu_DB, "btn_Geb_Neu_DB");
            this.btn_Geb_Neu_DB.Name = "btn_Geb_Neu_DB";
            this.btn_Geb_Neu_DB.UseVisualStyleBackColor = true;
            this.btn_Geb_Neu_DB.Click += new System.EventHandler(this.btn_Geb_Neu_DB_Click);
            // 
            // btn_GebLoeschen_DB
            // 
            resources.ApplyResources(this.btn_GebLoeschen_DB, "btn_GebLoeschen_DB");
            this.btn_GebLoeschen_DB.Name = "btn_GebLoeschen_DB";
            this.btn_GebLoeschen_DB.UseVisualStyleBackColor = true;
            this.btn_GebLoeschen_DB.Click += new System.EventHandler(this.btn_GebLoeschen_DB_Click);
            // 
            // btn_GebTypAendern_DB
            // 
            resources.ApplyResources(this.btn_GebTypAendern_DB, "btn_GebTypAendern_DB");
            this.btn_GebTypAendern_DB.Name = "btn_GebTypAendern_DB";
            this.btn_GebTypAendern_DB.UseVisualStyleBackColor = true;
            this.btn_GebTypAendern_DB.Click += new System.EventHandler(this.btn_GebTypAendern_DB_Click);
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Click += new System.EventHandler(this.listBox_Gebaeude_DB_SelectedIndexChanged);
            this.dataGridView1.Leave += new System.EventHandler(this.dataGridView1_Leave);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.Name = "label_Type";
            // 
            // Form_Gebaeude
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.listView_Gebaeude);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btn_GebTypAendern_DB);
            this.Controls.Add(this.btn_GebLoeschen_DB);
            this.Controls.Add(this.btn_Geb_Neu_DB);
            this.Controls.Add(this.btn_GebAendern_DB);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_ID_Gebaeude);
            this.Controls.Add(this.checkBox_dezWarmwasser);
            this.Controls.Add(this.textBox_Jahresnutzungsgrad);
            this.Controls.Add(this.textBox_Baujahr);
            this.Controls.Add(this.label_ListGebaeudeDB);
            this.Controls.Add(this.label_ListProjektGebaeude);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Gebaeudeart);
            this.Controls.Add(this.textBox_Gebäudename);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Hinzu);
            this.Name = "Form_Gebaeude";
            this.Load += new System.EventHandler(this.Form_Gebaeude_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioBtn_Sonstige;
        private System.Windows.Forms.RadioButton radioBtn_Wohngebäude;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Baujahr;
        private System.Windows.Forms.ComboBox comboBox_Gebäudeart;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox_TypEinheit;
        private System.Windows.Forms.TextBox textBox_Wohnflaeche;
        private System.Windows.Forms.Button btn_Aendern;
        private System.Windows.Forms.TextBox textBox_Gebäudename;
        private System.Windows.Forms.TextBox textBox_Gebaeudeart;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label_ListProjektGebaeude;
        private System.Windows.Forms.Label label_ListGebaeudeDB;
        private System.Windows.Forms.TextBox textBox_Baujahr;
        private System.Windows.Forms.TextBox textBox_Jahresnutzungsgrad;
        private System.Windows.Forms.CheckBox checkBox_dezWarmwasser;
        private System.Windows.Forms.TextBox textBox_ID_Gebaeude;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ListView listView_Gebaeude;
        private System.Windows.Forms.Button btn_GebAendern_DB;
        private System.Windows.Forms.Button btn_Geb_Neu_DB;
        private System.Windows.Forms.Button btn_GebLoeschen_DB;
        private System.Windows.Forms.Button btn_GebTypAendern_DB;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label_Type;
    }
}