namespace WindowsFormsApplication1
{
    partial class Wizard_Gebaeude
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
            this.listBox_Gebaeude_DB = new System.Windows.Forms.ListBox();
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
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_Baujahr = new System.Windows.Forms.TextBox();
            this.textBox_Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            this.checkBox_dezWarmwasser = new System.Windows.Forms.CheckBox();
            this.textBox_ID_Gebaeude = new System.Windows.Forms.TextBox();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
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
            this.label_Type.Size = new System.Drawing.Size(687, 32);
            this.label_Type.TabIndex = 24;
            this.label_Type.Text = "Eingabe der Energiedaten";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // listBox_Gebaeude_DB
            // 
            this.listBox_Gebaeude_DB.FormattingEnabled = true;
            this.listBox_Gebaeude_DB.ItemHeight = 17;
            this.listBox_Gebaeude_DB.Location = new System.Drawing.Point(407, 70);
            this.listBox_Gebaeude_DB.Name = "listBox_Gebaeude_DB";
            this.listBox_Gebaeude_DB.Size = new System.Drawing.Size(247, 191);
            this.listBox_Gebaeude_DB.TabIndex = 26;
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(303, 153);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(74, 28);
            this.btn_Entfernen.TabIndex = 28;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(303, 118);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(74, 29);
            this.btn_Hinzu.TabIndex = 27;
            this.btn_Hinzu.Text = "<---";
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
            this.groupBox1.Location = new System.Drawing.Point(371, 273);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(283, 122);
            this.groupBox1.TabIndex = 29;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Filter Gebäude DB";
            // 
            // radioBtn_Sonstige
            // 
            this.radioBtn_Sonstige.AutoSize = true;
            this.radioBtn_Sonstige.Location = new System.Drawing.Point(160, 24);
            this.radioBtn_Sonstige.Name = "radioBtn_Sonstige";
            this.radioBtn_Sonstige.Size = new System.Drawing.Size(79, 23);
            this.radioBtn_Sonstige.TabIndex = 5;
            this.radioBtn_Sonstige.Text = "Sonstige";
            this.radioBtn_Sonstige.UseVisualStyleBackColor = true;
            this.radioBtn_Sonstige.Click += new System.EventHandler(this.radioBtn_Sonstige_CheckedChanged);
            // 
            // radioBtn_Wohngebäude
            // 
            this.radioBtn_Wohngebäude.AutoSize = true;
            this.radioBtn_Wohngebäude.Checked = true;
            this.radioBtn_Wohngebäude.Location = new System.Drawing.Point(18, 24);
            this.radioBtn_Wohngebäude.Name = "radioBtn_Wohngebäude";
            this.radioBtn_Wohngebäude.Size = new System.Drawing.Size(117, 23);
            this.radioBtn_Wohngebäude.TabIndex = 4;
            this.radioBtn_Wohngebäude.TabStop = true;
            this.radioBtn_Wohngebäude.Text = "Wohngebäude";
            this.radioBtn_Wohngebäude.UseVisualStyleBackColor = true;
            this.radioBtn_Wohngebäude.Click += new System.EventHandler(this.radioBtn_Wohngebäude_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Baujahr";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 19);
            this.label1.TabIndex = 2;
            this.label1.Text = "Gebäudeart";
            // 
            // comboBox_Baujahr
            // 
            this.comboBox_Baujahr.FormattingEnabled = true;
            this.comboBox_Baujahr.Location = new System.Drawing.Point(109, 83);
            this.comboBox_Baujahr.Name = "comboBox_Baujahr";
            this.comboBox_Baujahr.Size = new System.Drawing.Size(159, 25);
            this.comboBox_Baujahr.TabIndex = 1;
            this.comboBox_Baujahr.SelectedIndexChanged += new System.EventHandler(this.comboBox_Baujahr_SelectedIndexChanged);
            // 
            // comboBox_Gebäudeart
            // 
            this.comboBox_Gebäudeart.FormattingEnabled = true;
            this.comboBox_Gebäudeart.Location = new System.Drawing.Point(109, 52);
            this.comboBox_Gebäudeart.Name = "comboBox_Gebäudeart";
            this.comboBox_Gebäudeart.Size = new System.Drawing.Size(159, 25);
            this.comboBox_Gebäudeart.TabIndex = 0;
            this.comboBox_Gebäudeart.SelectedIndexChanged += new System.EventHandler(this.comboBox_Gebäudeart_SelectedIndexChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox_TypEinheit);
            this.groupBox2.Controls.Add(this.textBox_Wohnflaeche);
            this.groupBox2.Controls.Add(this.btn_Aendern);
            this.groupBox2.Location = new System.Drawing.Point(24, 271);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(310, 124);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "ausgewähltes Gebäude: Verbrauch";
            // 
            // textBox_TypEinheit
            // 
            this.textBox_TypEinheit.Enabled = false;
            this.textBox_TypEinheit.Location = new System.Drawing.Point(100, 35);
            this.textBox_TypEinheit.Name = "textBox_TypEinheit";
            this.textBox_TypEinheit.Size = new System.Drawing.Size(193, 25);
            this.textBox_TypEinheit.TabIndex = 32;
            // 
            // textBox_Wohnflaeche
            // 
            this.textBox_Wohnflaeche.Enabled = false;
            this.textBox_Wohnflaeche.Location = new System.Drawing.Point(15, 35);
            this.textBox_Wohnflaeche.Name = "textBox_Wohnflaeche";
            this.textBox_Wohnflaeche.Size = new System.Drawing.Size(79, 25);
            this.textBox_Wohnflaeche.TabIndex = 31;
            // 
            // btn_Aendern
            // 
            this.btn_Aendern.Location = new System.Drawing.Point(15, 79);
            this.btn_Aendern.Name = "btn_Aendern";
            this.btn_Aendern.Size = new System.Drawing.Size(278, 28);
            this.btn_Aendern.TabIndex = 31;
            this.btn_Aendern.Text = "Ändern";
            this.btn_Aendern.UseVisualStyleBackColor = true;
            this.btn_Aendern.Click += new System.EventHandler(this.btn_Aendern_Click);
            // 
            // textBox_Gebäudename
            // 
            this.textBox_Gebäudename.Enabled = false;
            this.textBox_Gebäudename.Location = new System.Drawing.Point(124, 411);
            this.textBox_Gebäudename.Name = "textBox_Gebäudename";
            this.textBox_Gebäudename.Size = new System.Drawing.Size(168, 25);
            this.textBox_Gebäudename.TabIndex = 31;
            // 
            // textBox_Gebaeudeart
            // 
            this.textBox_Gebaeudeart.Enabled = false;
            this.textBox_Gebaeudeart.Location = new System.Drawing.Point(450, 411);
            this.textBox_Gebaeudeart.Name = "textBox_Gebaeudeart";
            this.textBox_Gebaeudeart.Size = new System.Drawing.Size(189, 25);
            this.textBox_Gebaeudeart.TabIndex = 32;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Location = new System.Drawing.Point(124, 456);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(530, 56);
            this.textBox_Beschreibung.TabIndex = 33;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(359, 411);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 19);
            this.label3.TabIndex = 6;
            this.label3.Text = "Gebäudeart";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 411);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 19);
            this.label4.TabIndex = 34;
            this.label4.Text = "Gebäudename";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 459);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 19);
            this.label5.TabIndex = 35;
            this.label5.Text = "Beschreibung";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 50);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(144, 19);
            this.label6.TabIndex = 6;
            this.label6.Text = "ausgewählte Gebäude";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(403, 48);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(101, 19);
            this.label7.TabIndex = 36;
            this.label7.Text = "Gebäude in DB";
            // 
            // textBox_Baujahr
            // 
            this.textBox_Baujahr.Location = new System.Drawing.Point(303, 187);
            this.textBox_Baujahr.Name = "textBox_Baujahr";
            this.textBox_Baujahr.Size = new System.Drawing.Size(69, 25);
            this.textBox_Baujahr.TabIndex = 37;
            this.textBox_Baujahr.Text = "Baujahr";
            this.textBox_Baujahr.Visible = false;
            // 
            // textBox_Jahresnutzungsgrad
            // 
            this.textBox_Jahresnutzungsgrad.Location = new System.Drawing.Point(308, 218);
            this.textBox_Jahresnutzungsgrad.Name = "textBox_Jahresnutzungsgrad";
            this.textBox_Jahresnutzungsgrad.Size = new System.Drawing.Size(69, 25);
            this.textBox_Jahresnutzungsgrad.TabIndex = 38;
            this.textBox_Jahresnutzungsgrad.Text = "Jahresnutzungsgrad";
            this.textBox_Jahresnutzungsgrad.Visible = false;
            // 
            // checkBox_dezWarmwasser
            // 
            this.checkBox_dezWarmwasser.AutoSize = true;
            this.checkBox_dezWarmwasser.Location = new System.Drawing.Point(298, 249);
            this.checkBox_dezWarmwasser.Name = "checkBox_dezWarmwasser";
            this.checkBox_dezWarmwasser.Size = new System.Drawing.Size(127, 23);
            this.checkBox_dezWarmwasser.TabIndex = 39;
            this.checkBox_dezWarmwasser.Text = "dezWarmwasser";
            this.checkBox_dezWarmwasser.UseVisualStyleBackColor = true;
            this.checkBox_dezWarmwasser.Visible = false;
            // 
            // textBox_ID_Gebaeude
            // 
            this.textBox_ID_Gebaeude.Location = new System.Drawing.Point(308, 67);
            this.textBox_ID_Gebaeude.Name = "textBox_ID_Gebaeude";
            this.textBox_ID_Gebaeude.Size = new System.Drawing.Size(63, 25);
            this.textBox_ID_Gebaeude.TabIndex = 40;
            this.textBox_ID_Gebaeude.Text = "ID_Gebaeude";
            this.textBox_ID_Gebaeude.Visible = false;
            // 
            // listView_Gebaeude
            // 
            this.listView_Gebaeude.FullRowSelect = true;
            this.listView_Gebaeude.Location = new System.Drawing.Point(24, 72);
            this.listView_Gebaeude.MultiSelect = false;
            this.listView_Gebaeude.Name = "listView_Gebaeude";
            this.listView_Gebaeude.Size = new System.Drawing.Size(252, 189);
            this.listView_Gebaeude.TabIndex = 41;
            this.listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            this.listView_Gebaeude.SelectedIndexChanged += new System.EventHandler(this.listBox_Geb_SelectedIndexChanged);
            // 
            // Wizard_Gebaeude
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(687, 524);
            this.Controls.Add(this.listView_Gebaeude);
            this.Controls.Add(this.textBox_ID_Gebaeude);
            this.Controls.Add(this.checkBox_dezWarmwasser);
            this.Controls.Add(this.textBox_Jahresnutzungsgrad);
            this.Controls.Add(this.textBox_Baujahr);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
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
            this.Controls.Add(this.listBox_Gebaeude_DB);
            this.Controls.Add(this.label_Type);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.ListBox listBox_Gebaeude_DB;
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
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_Baujahr;
        private System.Windows.Forms.TextBox textBox_Jahresnutzungsgrad;
        private System.Windows.Forms.CheckBox checkBox_dezWarmwasser;
        private System.Windows.Forms.TextBox textBox_ID_Gebaeude;
        private System.Windows.Forms.ListView listView_Gebaeude;
    }
}