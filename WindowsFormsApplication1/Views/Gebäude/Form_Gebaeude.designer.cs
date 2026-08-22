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
            btn_Entfernen = new System.Windows.Forms.Button();
            btn_Hinzu = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            radioBtn_Sonstige = new System.Windows.Forms.RadioButton();
            radioBtn_Wohngebäude = new System.Windows.Forms.RadioButton();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            comboBox_Baujahr = new System.Windows.Forms.ComboBox();
            comboBox_Gebäudeart = new System.Windows.Forms.ComboBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            textBox_TypEinheit = new System.Windows.Forms.TextBox();
            textBox_Wohnflaeche = new System.Windows.Forms.TextBox();
            btn_Aendern = new System.Windows.Forms.Button();
            textBox_Gebäudename = new System.Windows.Forms.TextBox();
            textBox_Gebaeudeart = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label_ListProjektGebaeude = new System.Windows.Forms.Label();
            label_ListGebaeudeDB = new System.Windows.Forms.Label();
            textBox_Baujahr = new System.Windows.Forms.TextBox();
            textBox_Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            checkBox_dezWarmwasser = new System.Windows.Forms.CheckBox();
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            listView_Gebaeude = new System.Windows.Forms.ListView();
            btn_GebAendern_DB = new System.Windows.Forms.Button();
            btn_Geb_Neu_DB = new System.Windows.Forms.Button();
            btn_GebLoeschen_DB = new System.Windows.Forms.Button();
            btn_GebTypAendern_DB = new System.Windows.Forms.Button();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            label_Type = new System.Windows.Forms.Label();
            textBox_Suche = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            btn_Help = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(btn_Entfernen, "btn_Entfernen");
            btn_Entfernen.Name = "btn_Entfernen";
            btn_Entfernen.UseVisualStyleBackColor = true;
            btn_Entfernen.Click += btn_Entfernen_Click;
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(btn_Hinzu, "btn_Hinzu");
            btn_Hinzu.Name = "btn_Hinzu";
            btn_Hinzu.UseVisualStyleBackColor = true;
            btn_Hinzu.Click += btn_Hinzu_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioBtn_Sonstige);
            groupBox1.Controls.Add(radioBtn_Wohngebäude);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(comboBox_Baujahr);
            groupBox1.Controls.Add(comboBox_Gebäudeart);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // radioBtn_Sonstige
            // 
            resources.ApplyResources(radioBtn_Sonstige, "radioBtn_Sonstige");
            radioBtn_Sonstige.Name = "radioBtn_Sonstige";
            radioBtn_Sonstige.UseVisualStyleBackColor = true;
            radioBtn_Sonstige.Click += radioBtn_Sonstige_CheckedChanged;
            // 
            // radioBtn_Wohngebäude
            // 
            resources.ApplyResources(radioBtn_Wohngebäude, "radioBtn_Wohngebäude");
            radioBtn_Wohngebäude.Checked = true;
            radioBtn_Wohngebäude.Name = "radioBtn_Wohngebäude";
            radioBtn_Wohngebäude.TabStop = true;
            radioBtn_Wohngebäude.UseVisualStyleBackColor = true;
            radioBtn_Wohngebäude.Click += radioBtn_Wohngebäude_CheckedChanged;
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // comboBox_Baujahr
            // 
            comboBox_Baujahr.FormattingEnabled = true;
            resources.ApplyResources(comboBox_Baujahr, "comboBox_Baujahr");
            comboBox_Baujahr.Name = "comboBox_Baujahr";
            comboBox_Baujahr.SelectedIndexChanged += comboBox_Baujahr_SelectedIndexChanged;
            // 
            // comboBox_Gebäudeart
            // 
            comboBox_Gebäudeart.FormattingEnabled = true;
            resources.ApplyResources(comboBox_Gebäudeart, "comboBox_Gebäudeart");
            comboBox_Gebäudeart.Name = "comboBox_Gebäudeart";
            comboBox_Gebäudeart.SelectedIndexChanged += comboBox_Gebäudeart_SelectedIndexChanged;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBox_TypEinheit);
            groupBox2.Controls.Add(textBox_Wohnflaeche);
            groupBox2.Controls.Add(btn_Aendern);
            resources.ApplyResources(groupBox2, "groupBox2");
            groupBox2.Name = "groupBox2";
            groupBox2.TabStop = false;
            // 
            // textBox_TypEinheit
            // 
            resources.ApplyResources(textBox_TypEinheit, "textBox_TypEinheit");
            textBox_TypEinheit.Name = "textBox_TypEinheit";
            // 
            // textBox_Wohnflaeche
            // 
            resources.ApplyResources(textBox_Wohnflaeche, "textBox_Wohnflaeche");
            textBox_Wohnflaeche.Name = "textBox_Wohnflaeche";
            // 
            // btn_Aendern
            // 
            resources.ApplyResources(btn_Aendern, "btn_Aendern");
            btn_Aendern.Name = "btn_Aendern";
            btn_Aendern.UseVisualStyleBackColor = true;
            btn_Aendern.Click += btn_Aendern_Click;
            // 
            // textBox_Gebäudename
            // 
            resources.ApplyResources(textBox_Gebäudename, "textBox_Gebäudename");
            textBox_Gebäudename.Name = "textBox_Gebäudename";
            // 
            // textBox_Gebaeudeart
            // 
            resources.ApplyResources(textBox_Gebaeudeart, "textBox_Gebaeudeart");
            textBox_Gebaeudeart.Name = "textBox_Gebaeudeart";
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.Name = "label5";
            // 
            // label_ListProjektGebaeude
            // 
            resources.ApplyResources(label_ListProjektGebaeude, "label_ListProjektGebaeude");
            label_ListProjektGebaeude.Name = "label_ListProjektGebaeude";
            // 
            // label_ListGebaeudeDB
            // 
            resources.ApplyResources(label_ListGebaeudeDB, "label_ListGebaeudeDB");
            label_ListGebaeudeDB.Name = "label_ListGebaeudeDB";
            // 
            // textBox_Baujahr
            // 
            resources.ApplyResources(textBox_Baujahr, "textBox_Baujahr");
            textBox_Baujahr.Name = "textBox_Baujahr";
            // 
            // textBox_Jahresnutzungsgrad
            // 
            resources.ApplyResources(textBox_Jahresnutzungsgrad, "textBox_Jahresnutzungsgrad");
            textBox_Jahresnutzungsgrad.Name = "textBox_Jahresnutzungsgrad";
            // 
            // checkBox_dezWarmwasser
            // 
            resources.ApplyResources(checkBox_dezWarmwasser, "checkBox_dezWarmwasser");
            checkBox_dezWarmwasser.Name = "checkBox_dezWarmwasser";
            checkBox_dezWarmwasser.UseVisualStyleBackColor = true;
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
            // listView_Gebaeude
            // 
            listView_Gebaeude.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            resources.ApplyResources(listView_Gebaeude, "listView_Gebaeude");
            listView_Gebaeude.Name = "listView_Gebaeude";
            listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            listView_Gebaeude.SelectedIndexChanged += listView_Gebaeude_SelectedIndexChanged;
            // 
            // btn_GebAendern_DB
            // 
            resources.ApplyResources(btn_GebAendern_DB, "btn_GebAendern_DB");
            btn_GebAendern_DB.Name = "btn_GebAendern_DB";
            btn_GebAendern_DB.UseVisualStyleBackColor = true;
            btn_GebAendern_DB.Click += btn_GebAendern_DB_Click;
            // 
            // btn_Geb_Neu_DB
            // 
            resources.ApplyResources(btn_Geb_Neu_DB, "btn_Geb_Neu_DB");
            btn_Geb_Neu_DB.Name = "btn_Geb_Neu_DB";
            btn_Geb_Neu_DB.UseVisualStyleBackColor = true;
            btn_Geb_Neu_DB.Click += btn_Geb_Neu_DB_Click;
            // 
            // btn_GebLoeschen_DB
            // 
            resources.ApplyResources(btn_GebLoeschen_DB, "btn_GebLoeschen_DB");
            btn_GebLoeschen_DB.Name = "btn_GebLoeschen_DB";
            btn_GebLoeschen_DB.UseVisualStyleBackColor = true;
            btn_GebLoeschen_DB.Click += btn_GebLoeschen_DB_Click;
            // 
            // btn_GebTypAendern_DB
            // 
            resources.ApplyResources(btn_GebTypAendern_DB, "btn_GebTypAendern_DB");
            btn_GebTypAendern_DB.Name = "btn_GebTypAendern_DB";
            btn_GebTypAendern_DB.UseVisualStyleBackColor = true;
            btn_GebTypAendern_DB.Click += btn_GebTypAendern_DB_Click;
            // 
            // pictureBox1
            // 
            resources.ApplyResources(pictureBox1, "pictureBox1");
            pictureBox1.Name = "pictureBox1";
            pictureBox1.TabStop = false;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView1, "dataGridView1");
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Click += listBox_Gebaeude_DB_SelectedIndexChanged;
            dataGridView1.Leave += dataGridView1_Leave;
            // 
            // label_Type
            // 
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.Name = "label_Type";
            // 
            // textBox_Suche
            // 
            resources.ApplyResources(textBox_Suche, "textBox_Suche");
            textBox_Suche.Name = "textBox_Suche";
            // 
            // label6
            // 
            resources.ApplyResources(label6, "label6");
            label6.Name = "label6";
            // 
            // btn_Help
            // 
            btn_Help.BackColor = System.Drawing.Color.Transparent;
            btn_Help.BackgroundImage = Properties.Resources.help_icon;
            btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            btn_Help.FlatAppearance.BorderSize = 0;
            btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_Help.Location = new System.Drawing.Point(508, 38);
            btn_Help.Name = "btn_Help";
            btn_Help.Size = new System.Drawing.Size(28, 28);
            btn_Help.TabStop = false;
            btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_Gebaeude
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            Controls.Add(btn_Help);
            Controls.Add(label6);
            Controls.Add(textBox_Suche);
            Controls.Add(label_Type);
            Controls.Add(dataGridView1);
            Controls.Add(listView_Gebaeude);
            Controls.Add(pictureBox1);
            Controls.Add(btn_GebTypAendern_DB);
            Controls.Add(btn_GebLoeschen_DB);
            Controls.Add(btn_Geb_Neu_DB);
            Controls.Add(btn_GebAendern_DB);
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_OK);
            Controls.Add(textBox_Baujahr);
            Controls.Add(label_ListGebaeudeDB);
            Controls.Add(label_ListProjektGebaeude);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(textBox_Gebaeudeart);
            Controls.Add(textBox_Gebäudename);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(btn_Entfernen);
            Controls.Add(btn_Hinzu);
            Controls.Add(checkBox_dezWarmwasser);
            Controls.Add(textBox_Jahresnutzungsgrad);
            Name = "Form_Gebaeude";
            Load += Form_Gebaeude_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Help;
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
        private System.Windows.Forms.TextBox textBox_Suche;
        private System.Windows.Forms.Label label6;
    }
}