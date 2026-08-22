namespace WindowsFormsApplication1
{
    partial class Form_PufferSp 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_PufferSp));
            this.label11 = new System.Windows.Forms.Label();
            this.btn_PufferSp_Entfernen = new System.Windows.Forms.Button();
            this.btn_PufferSp_Hinzu = new System.Windows.Forms.Button();
            this.listBox_Pufferspeicher_DB = new System.Windows.Forms.ListBox();
            this.label12 = new System.Windows.Forms.Label();
            this.listBox_Pufferspeicher = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Versluste = new System.Windows.Forms.TextBox();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.textBox_Volumen = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Hersteller = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label_Type = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_Volumen = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Hersteller = new System.Windows.Forms.ComboBox();
            this.btn_Help = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // btn_PufferSp_Entfernen
            // 
            resources.ApplyResources(this.btn_PufferSp_Entfernen, "btn_PufferSp_Entfernen");
            this.btn_PufferSp_Entfernen.Name = "btn_PufferSp_Entfernen";
            this.btn_PufferSp_Entfernen.UseVisualStyleBackColor = true;
            this.btn_PufferSp_Entfernen.Click += new System.EventHandler(this.btn_PufferSp_Entfernen_Click);
            // 
            // btn_PufferSp_Hinzu
            // 
            resources.ApplyResources(this.btn_PufferSp_Hinzu, "btn_PufferSp_Hinzu");
            this.btn_PufferSp_Hinzu.Name = "btn_PufferSp_Hinzu";
            this.btn_PufferSp_Hinzu.UseVisualStyleBackColor = true;
            this.btn_PufferSp_Hinzu.Click += new System.EventHandler(this.btn_PufferSp_Hinzu_Click);
            // 
            // listBox_Pufferspeicher_DB
            // 
            resources.ApplyResources(this.listBox_Pufferspeicher_DB, "listBox_Pufferspeicher_DB");
            this.listBox_Pufferspeicher_DB.FormattingEnabled = true;
            this.listBox_Pufferspeicher_DB.Name = "listBox_Pufferspeicher_DB";
            this.listBox_Pufferspeicher_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_PufferSp_DB_SelectedIndexChanged);
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // listBox_Pufferspeicher
            // 
            resources.ApplyResources(this.listBox_Pufferspeicher, "listBox_Pufferspeicher");
            this.listBox_Pufferspeicher.FormattingEnabled = true;
            this.listBox_Pufferspeicher.Name = "listBox_Pufferspeicher";
            this.listBox_Pufferspeicher.SelectedIndexChanged += new System.EventHandler(this.listBox_PufferSp_SelectedIndexChanged);
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
            // btn_Bearbeiten
            // 
            resources.ApplyResources(this.btn_Bearbeiten, "btn_Bearbeiten");
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // btn_Löschen
            // 
            resources.ApplyResources(this.btn_Löschen, "btn_Löschen");
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.Label13);
            this.groupBox1.Controls.Add(this.textBox_Investitionskosten);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.textBox_Versluste);
            this.groupBox1.Controls.Add(this.Label10);
            this.groupBox1.Controls.Add(this.textBox_Typ);
            this.groupBox1.Controls.Add(this.Label17);
            this.groupBox1.Controls.Add(this.textBox_Volumen);
            this.groupBox1.Controls.Add(this.label18);
            this.groupBox1.Controls.Add(this.textBox_Hersteller);
            this.groupBox1.Controls.Add(this.label16);
            this.groupBox1.Controls.Add(this.textBox_Name);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.Name = "Label13";
            // 
            // textBox_Investitionskosten
            // 
            resources.ApplyResources(this.textBox_Investitionskosten, "textBox_Investitionskosten");
            this.textBox_Investitionskosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.BackColor = System.Drawing.Color.Black;
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Name = "label6";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Name = "label8";
            // 
            // textBox_Versluste
            // 
            resources.ApplyResources(this.textBox_Versluste, "textBox_Versluste");
            this.textBox_Versluste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Versluste.ForeColor = System.Drawing.Color.Black;
            this.textBox_Versluste.Name = "textBox_Versluste";
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.ForeColor = System.Drawing.Color.Black;
            this.Label10.Name = "Label10";
            // 
            // textBox_Typ
            // 
            resources.ApplyResources(this.textBox_Typ, "textBox_Typ");
            this.textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Typ.ForeColor = System.Drawing.Color.Black;
            this.textBox_Typ.Name = "textBox_Typ";
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.ForeColor = System.Drawing.Color.Black;
            this.Label17.Name = "Label17";
            // 
            // textBox_Volumen
            // 
            resources.ApplyResources(this.textBox_Volumen, "textBox_Volumen");
            this.textBox_Volumen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Volumen.ForeColor = System.Drawing.Color.Black;
            this.textBox_Volumen.Name = "textBox_Volumen";
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // textBox_Hersteller
            // 
            resources.ApplyResources(this.textBox_Hersteller, "textBox_Hersteller");
            this.textBox_Hersteller.Name = "textBox_Hersteller";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.Name = "textBox_Name";
            // 
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // comboBox_Volumen
            // 
            resources.ApplyResources(this.comboBox_Volumen, "comboBox_Volumen");
            this.comboBox_Volumen.FormattingEnabled = true;
            this.comboBox_Volumen.Name = "comboBox_Volumen";
            this.comboBox_Volumen.SelectedIndexChanged += new System.EventHandler(this.comboBox_Volumen_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // comboBox_Hersteller
            // 
            resources.ApplyResources(this.comboBox_Hersteller, "comboBox_Hersteller");
            this.comboBox_Hersteller.FormattingEnabled = true;
            this.comboBox_Hersteller.Name = "comboBox_Hersteller";
            this.comboBox_Hersteller.SelectedIndexChanged += new System.EventHandler(this.comboBox_Hersteller_SelectedIndexChanged);
            // 
            // btn_Help
            // 
            this.btn_Help.BackColor = System.Drawing.Color.Transparent;
            this.btn_Help.BackgroundImage = Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Help.Location = new System.Drawing.Point(730, 34);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new System.Drawing.Size(24, 24);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_PufferSp
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBox_Volumen);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_Hersteller);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Löschen);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.btn_PufferSp_Entfernen);
            this.Controls.Add(this.btn_PufferSp_Hinzu);
            this.Controls.Add(this.listBox_Pufferspeicher_DB);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.listBox_Pufferspeicher);
            this.Name = "Form_PufferSp";
            this.Load += new System.EventHandler(this.Form_PufferSp_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btn_PufferSp_Entfernen;
        private System.Windows.Forms.Button btn_PufferSp_Hinzu;
        private System.Windows.Forms.ListBox listBox_Pufferspeicher_DB;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ListBox listBox_Pufferspeicher;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label Label13;
        private System.Windows.Forms.TextBox textBox_Investitionskosten;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Versluste;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label Label17;
        private System.Windows.Forms.TextBox textBox_Volumen;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Hersteller;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Volumen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Hersteller;
    }
}