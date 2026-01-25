namespace WindowsFormsApplication1
{
    partial class Form_Stromverbraucher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Stromverbraucher));
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Strom_DBneu = new System.Windows.Forms.Button();
            this.btn_Strom_loeschen = new System.Windows.Forms.Button();
            this.btn_ErgebnisseVerbrauch = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_StromtypDBedit = new System.Windows.Forms.Button();
            this.btn_Strom_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Stromname = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Stromtyp = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.textBox_StromSumme = new System.Windows.Forms.TextBox();
            this.listBox_Strom_DB = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.listView_Strom_Auswahl = new System.Windows.Forms.ListView();
            this.Label1 = new System.Windows.Forms.Label();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBox_Verbrauch = new System.Windows.Forms.TextBox();
            this.Label8 = new System.Windows.Forms.Label();
            this.btn_neuerWert = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(this.btn_Hinzu, "btn_Hinzu");
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn__Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(this.btn_Entfernen, "btn_Entfernen");
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Strom_DBneu
            // 
            resources.ApplyResources(this.btn_Strom_DBneu, "btn_Strom_DBneu");
            this.btn_Strom_DBneu.Name = "btn_Strom_DBneu";
            this.btn_Strom_DBneu.UseVisualStyleBackColor = true;
            this.btn_Strom_DBneu.Click += new System.EventHandler(this.btn_Strom_DBneu_Click);
            // 
            // btn_Strom_loeschen
            // 
            resources.ApplyResources(this.btn_Strom_loeschen, "btn_Strom_loeschen");
            this.btn_Strom_loeschen.Name = "btn_Strom_loeschen";
            this.btn_Strom_loeschen.UseVisualStyleBackColor = true;
            this.btn_Strom_loeschen.Click += new System.EventHandler(this.btn_Strom_loeschen_Click);
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
            // btn_StromtypDBedit
            // 
            resources.ApplyResources(this.btn_StromtypDBedit, "btn_StromtypDBedit");
            this.btn_StromtypDBedit.Name = "btn_StromtypDBedit";
            this.btn_StromtypDBedit.UseVisualStyleBackColor = true;
            this.btn_StromtypDBedit.Click += new System.EventHandler(this.btn_StromtypDBedit_Click);
            // 
            // btn_Strom_DBedit
            // 
            resources.ApplyResources(this.btn_Strom_DBedit, "btn_Strom_DBedit");
            this.btn_Strom_DBedit.Name = "btn_Strom_DBedit";
            this.btn_Strom_DBedit.UseVisualStyleBackColor = true;
            this.btn_Strom_DBedit.Click += new System.EventHandler(this.btn_Strom_DBedit_Click);
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
            // textBox_Stromname
            // 
            resources.ApplyResources(this.textBox_Stromname, "textBox_Stromname");
            this.textBox_Stromname.BackColor = System.Drawing.Color.White;
            this.textBox_Stromname.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stromname.Name = "textBox_Stromname";
            this.textBox_Stromname.ReadOnly = true;
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.Name = "Label13";
            // 
            // textBox_Jahres_Verbrauch
            // 
            resources.ApplyResources(this.textBox_Jahres_Verbrauch, "textBox_Jahres_Verbrauch");
            this.textBox_Jahres_Verbrauch.BackColor = System.Drawing.Color.White;
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.BackColor = System.Drawing.Color.White;
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            // 
            // textBox_Stromtyp
            // 
            resources.ApplyResources(this.textBox_Stromtyp, "textBox_Stromtyp");
            this.textBox_Stromtyp.BackColor = System.Drawing.Color.White;
            this.textBox_Stromtyp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stromtyp.Name = "textBox_Stromtyp";
            this.textBox_Stromtyp.ReadOnly = true;
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
            // textBox_StromSumme
            // 
            resources.ApplyResources(this.textBox_StromSumme, "textBox_StromSumme");
            this.textBox_StromSumme.BackColor = System.Drawing.Color.White;
            this.textBox_StromSumme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_StromSumme.Name = "textBox_StromSumme";
            this.textBox_StromSumme.ReadOnly = true;
            // 
            // listBox_Strom_DB
            // 
            resources.ApplyResources(this.listBox_Strom_DB, "listBox_Strom_DB");
            this.listBox_Strom_DB.FormattingEnabled = true;
            this.listBox_Strom_DB.Name = "listBox_Strom_DB";
            this.listBox_Strom_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // listView_Strom_Auswahl
            // 
            resources.ApplyResources(this.listView_Strom_Auswahl, "listView_Strom_Auswahl");
            this.listView_Strom_Auswahl.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView_Strom_Auswahl.HideSelection = false;
            this.listView_Strom_Auswahl.Name = "listView_Strom_Auswahl";
            this.listView_Strom_Auswahl.UseCompatibleStateImageBehavior = false;
            this.listView_Strom_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listView_Prozess_Auswahl_SelectedIndexChanged);
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.BackColor = System.Drawing.Color.Khaki;
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.textBox_Verbrauch);
            this.groupBox1.Controls.Add(this.Label8);
            this.groupBox1.Controls.Add(this.btn_neuerWert);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.setup_trans;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // textBox_Verbrauch
            // 
            resources.ApplyResources(this.textBox_Verbrauch, "textBox_Verbrauch");
            this.textBox_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
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
            // Form_Stromverbraucher
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.listView_Strom_Auswahl);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.listBox_Strom_DB);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Strom_DBneu);
            this.Controls.Add(this.btn_Strom_loeschen);
            this.Controls.Add(this.btn_ErgebnisseVerbrauch);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_StromtypDBedit);
            this.Controls.Add(this.btn_Strom_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Stromname);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Stromtyp);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.textBox_StromSumme);
            this.Name = "Form_Stromverbraucher";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Hinzu;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.Button btn_Strom_DBneu;
private System.Windows.Forms.Button btn_Strom_loeschen;
private System.Windows.Forms.Button btn_ErgebnisseVerbrauch;
private System.Windows.Forms.Button btn_Simulation;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_StromtypDBedit;
private System.Windows.Forms.Button btn_Strom_DBedit;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Stromname;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Stromtyp;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox textBox_StromSumme;
private System.Windows.Forms.ListBox listBox_Strom_DB;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.ListView listView_Strom_Auswahl;
private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox_Verbrauch;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.Button btn_neuerWert;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}