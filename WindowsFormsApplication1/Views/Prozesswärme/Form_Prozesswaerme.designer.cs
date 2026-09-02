namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Prozesswaerme));
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Prozess_DBneu = new System.Windows.Forms.Button();
            this.btn_Prozess_loeschen = new System.Windows.Forms.Button();
            this.btn_ErgebnisseVerbrauch = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_ProzTypeDBedit = new System.Windows.Forms.Button();
            this.btn_Prozess_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Prozess_Name = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Prozess_Type = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.textBox_SummeProzesswaerme = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.listView_Prozess_Auswahl = new System.Windows.Forms.ListView();
            this.Label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBox_Verbrauch = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.btn_neuerWert = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label_Type = new System.Windows.Forms.Label();
            this.btn_Help = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(this.btn_Hinzu, "btn_Hinzu");
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(this.btn_Entfernen, "btn_Entfernen");
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Prozess_DBneu
            // 
            resources.ApplyResources(this.btn_Prozess_DBneu, "btn_Prozess_DBneu");
            this.btn_Prozess_DBneu.Name = "btn_Prozess_DBneu";
            this.btn_Prozess_DBneu.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBneu.Click += new System.EventHandler(this.btn_Prozess_DBneu_Click);
            // 
            // btn_Prozess_loeschen
            // 
            resources.ApplyResources(this.btn_Prozess_loeschen, "btn_Prozess_loeschen");
            this.btn_Prozess_loeschen.Name = "btn_Prozess_loeschen";
            this.btn_Prozess_loeschen.UseVisualStyleBackColor = true;
            this.btn_Prozess_loeschen.Click += new System.EventHandler(this.btn_Prozess_loeschen_Click);
            // 
            // btn_ErgebnisseVerbrauch
            // 
            resources.ApplyResources(this.btn_ErgebnisseVerbrauch, "btn_ErgebnisseVerbrauch");
            this.btn_ErgebnisseVerbrauch.Name = "btn_ErgebnisseVerbrauch";
            this.btn_ErgebnisseVerbrauch.UseVisualStyleBackColor = true;
            this.btn_ErgebnisseVerbrauch.Click += new System.EventHandler(this.btn_ErgebnisseVerbrauch_Click);
            // 
            // btn_Simulation
            // 
            resources.ApplyResources(this.btn_Simulation, "btn_Simulation");
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label24
            // 
            resources.ApplyResources(this.Label24, "Label24");
            this.Label24.Name = "Label24";
            // 
            // btn_ProzTypeDBedit
            // 
            resources.ApplyResources(this.btn_ProzTypeDBedit, "btn_ProzTypeDBedit");
            this.btn_ProzTypeDBedit.Name = "btn_ProzTypeDBedit";
            this.btn_ProzTypeDBedit.UseVisualStyleBackColor = true;
            this.btn_ProzTypeDBedit.Click += new System.EventHandler(this.btn_ProzTypeDBedit_Click);
            // 
            // btn_Prozess_DBedit
            // 
            resources.ApplyResources(this.btn_Prozess_DBedit, "btn_Prozess_DBedit");
            this.btn_Prozess_DBedit.Name = "btn_Prozess_DBedit";
            this.btn_Prozess_DBedit.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBedit.Click += new System.EventHandler(this.btn_Prozess_DBedit_Click);
            // 
            // Label12
            // 
            resources.ApplyResources(this.Label12, "Label12");
            this.Label12.Name = "Label12";
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.Name = "Label10";
            // 
            // textBox_Prozess_Name
            // 
            this.textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Prozess_Name, "textBox_Prozess_Name");
            this.textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            this.textBox_Prozess_Name.ReadOnly = true;
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.Name = "Label13";
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Jahres_Verbrauch, "textBox_Jahres_Verbrauch");
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            // 
            // textBox_Prozess_Type
            // 
            this.textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Prozess_Type, "textBox_Prozess_Type");
            this.textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            this.textBox_Prozess_Type.ReadOnly = true;
            // 
            // Label15
            // 
            resources.ApplyResources(this.Label15, "Label15");
            this.Label15.Name = "Label15";
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.BackColor = System.Drawing.Color.Black;
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Name = "Label11";
            // 
            // Label19
            // 
            resources.ApplyResources(this.Label19, "Label19");
            this.Label19.Name = "Label19";
            // 
            // Label18
            // 
            resources.ApplyResources(this.Label18, "Label18");
            this.Label18.BackColor = System.Drawing.Color.Black;
            this.Label18.ForeColor = System.Drawing.Color.White;
            this.Label18.Name = "Label18";
            // 
            // textBox_SummeProzesswaerme
            // 
            this.textBox_SummeProzesswaerme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_SummeProzesswaerme, "textBox_SummeProzesswaerme");
            this.textBox_SummeProzesswaerme.Name = "textBox_SummeProzesswaerme";
            this.textBox_SummeProzesswaerme.ReadOnly = true;
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // listView_Prozess_Auswahl
            // 
            this.listView_Prozess_Auswahl.FullRowSelect = true;
            this.listView_Prozess_Auswahl.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView_Prozess_Auswahl.HideSelection = false;
            resources.ApplyResources(this.listView_Prozess_Auswahl, "listView_Prozess_Auswahl");
            this.listView_Prozess_Auswahl.Name = "listView_Prozess_Auswahl";
            this.listView_Prozess_Auswahl.UseCompatibleStateImageBehavior = false;
            this.listView_Prozess_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listView_Prozess_Auswahl_SelectedIndexChanged);
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Khaki;
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.textBox_Verbrauch);
            this.groupBox1.Controls.Add(this.Label8);
            this.groupBox1.Controls.Add(this.btn_neuerWert);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.setup_trans;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // textBox_Verbrauch
            // 
            this.textBox_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Verbrauch, "textBox_Verbrauch");
            this.textBox_Verbrauch.Name = "textBox_Verbrauch";
            this.textBox_Verbrauch.TextChanged += new System.EventHandler(this.textBox_Verbrauch_TextChanged);
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.BackColor = System.Drawing.Color.Black;
            this.Label8.ForeColor = System.Drawing.Color.White;
            this.Label8.Name = "Label8";
            // 
            // btn_neuerWert
            // 
            resources.ApplyResources(this.btn_neuerWert, "btn_neuerWert");
            this.btn_neuerWert.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_neuerWert.Name = "btn_neuerWert";
            this.btn_neuerWert.UseVisualStyleBackColor = true;
            this.btn_neuerWert.Click += new System.EventHandler(this.btn_neuerWert_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Click += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.Name = "label_Type";
            // 
            // btn_Help
            // 
            this.btn_Help.BackColor = System.Drawing.Color.Transparent;
            this.btn_Help.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.help_icon;
            this.btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_Help.FlatAppearance.BorderSize = 0;
            this.btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Help.Location = new System.Drawing.Point(755, 40);
            this.btn_Help.Name = "btn_Help";
            this.btn_Help.Size = new System.Drawing.Size(28, 28);
            this.btn_Help.TabStop = false;
            this.btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_Prozesswaerme
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Help);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.listView_Prozess_Auswahl);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Prozess_DBneu);
            this.Controls.Add(this.btn_Prozess_loeschen);
            this.Controls.Add(this.btn_ErgebnisseVerbrauch);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_ProzTypeDBedit);
            this.Controls.Add(this.btn_Prozess_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Prozess_Name);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Prozess_Type);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.textBox_SummeProzesswaerme);
            this.Name = "Form_Prozesswaerme";
            this.Load += new System.EventHandler(this.Form_Prozesswaerme_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Button btn_Hinzu;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.Button btn_Prozess_DBneu;
private System.Windows.Forms.Button btn_Prozess_loeschen;
private System.Windows.Forms.Button btn_ErgebnisseVerbrauch;
private System.Windows.Forms.Button btn_Simulation;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_ProzTypeDBedit;
private System.Windows.Forms.Button btn_Prozess_DBedit;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Prozess_Name;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Prozess_Type;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox textBox_SummeProzesswaerme;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.ListView listView_Prozess_Auswahl;
private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox_Verbrauch;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.Button btn_neuerWert;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}