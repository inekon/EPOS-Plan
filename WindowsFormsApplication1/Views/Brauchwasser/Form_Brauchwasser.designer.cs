namespace WindowsFormsApplication1
{
    partial class Form_Brauchwasser 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Brauchwasser));
            btn_Hinzu = new System.Windows.Forms.Button();
            btn_Entfernen = new System.Windows.Forms.Button();
            btn_Prozess_DBneu = new System.Windows.Forms.Button();
            btn_Prozess_loeschen = new System.Windows.Forms.Button();
            btn_ErgebnisseVerbrauch = new System.Windows.Forms.Button();
            btn_Simulation = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            Label24 = new System.Windows.Forms.Label();
            btn_ProzTypeDBedit = new System.Windows.Forms.Button();
            btn_Prozess_DBedit = new System.Windows.Forms.Button();
            Label12 = new System.Windows.Forms.Label();
            Label10 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label13 = new System.Windows.Forms.Label();
            textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            textBox_Type = new System.Windows.Forms.TextBox();
            Label15 = new System.Windows.Forms.Label();
            Label11 = new System.Windows.Forms.Label();
            Label19 = new System.Windows.Forms.Label();
            Label18 = new System.Windows.Forms.Label();
            textBox_Summe = new System.Windows.Forms.TextBox();
            btn_Abbrechen = new System.Windows.Forms.Button();
            listView_Auswahl = new System.Windows.Forms.ListView();
            Label1 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            textBox_Verbrauch = new System.Windows.Forms.TextBox();
            Label8 = new System.Windows.Forms.Label();
            btn_neuerWert = new System.Windows.Forms.Button();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            label_Type = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btn_Hinzu
            // 
            resources.ApplyResources(btn_Hinzu, "btn_Hinzu");
            btn_Hinzu.Name = "btn_Hinzu";
            btn_Hinzu.UseVisualStyleBackColor = true;
            btn_Hinzu.Click += btn_Hinzu_Click;
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(btn_Entfernen, "btn_Entfernen");
            btn_Entfernen.Name = "btn_Entfernen";
            btn_Entfernen.UseVisualStyleBackColor = true;
            btn_Entfernen.Click += btn_Entfernen_Click;
            // 
            // btn_Prozess_DBneu
            // 
            resources.ApplyResources(btn_Prozess_DBneu, "btn_Prozess_DBneu");
            btn_Prozess_DBneu.Name = "btn_Prozess_DBneu";
            btn_Prozess_DBneu.UseVisualStyleBackColor = true;
            btn_Prozess_DBneu.Click += btn_Prozess_DBneu_Click;
            // 
            // btn_Prozess_loeschen
            // 
            resources.ApplyResources(btn_Prozess_loeschen, "btn_Prozess_loeschen");
            btn_Prozess_loeschen.Name = "btn_Prozess_loeschen";
            btn_Prozess_loeschen.UseVisualStyleBackColor = true;
            btn_Prozess_loeschen.Click += btn_Prozess_loeschen_Click;
            // 
            // btn_ErgebnisseVerbrauch
            // 
            resources.ApplyResources(btn_ErgebnisseVerbrauch, "btn_ErgebnisseVerbrauch");
            btn_ErgebnisseVerbrauch.Name = "btn_ErgebnisseVerbrauch";
            btn_ErgebnisseVerbrauch.UseVisualStyleBackColor = true;
            btn_ErgebnisseVerbrauch.Click += btn_ErgebnisseVerbrauch_Click;
            // 
            // btn_Simulation
            // 
            resources.ApplyResources(btn_Simulation, "btn_Simulation");
            btn_Simulation.Name = "btn_Simulation";
            btn_Simulation.UseVisualStyleBackColor = true;
            btn_Simulation.Click += btn_Simulation_Click;
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // Label24
            // 
            resources.ApplyResources(Label24, "Label24");
            Label24.Name = "Label24";
            // 
            // btn_ProzTypeDBedit
            // 
            resources.ApplyResources(btn_ProzTypeDBedit, "btn_ProzTypeDBedit");
            btn_ProzTypeDBedit.Name = "btn_ProzTypeDBedit";
            btn_ProzTypeDBedit.UseVisualStyleBackColor = true;
            btn_ProzTypeDBedit.Click += btn_ProzTypeDBedit_Click;
            // 
            // btn_Prozess_DBedit
            // 
            resources.ApplyResources(btn_Prozess_DBedit, "btn_Prozess_DBedit");
            btn_Prozess_DBedit.Name = "btn_Prozess_DBedit";
            btn_Prozess_DBedit.UseVisualStyleBackColor = true;
            btn_Prozess_DBedit.Click += btn_Prozess_DBedit_Click;
            // 
            // Label12
            // 
            resources.ApplyResources(Label12, "Label12");
            Label12.Name = "Label12";
            // 
            // Label10
            // 
            resources.ApplyResources(Label10, "Label10");
            Label10.Name = "Label10";
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.Name = "textBox_Name";
            // 
            // Label13
            // 
            resources.ApplyResources(Label13, "Label13");
            Label13.Name = "Label13";
            // 
            // textBox_Jahres_Verbrauch
            // 
            textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Jahres_Verbrauch, "textBox_Jahres_Verbrauch");
            textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // textBox_Type
            // 
            textBox_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Type, "textBox_Type");
            textBox_Type.Name = "textBox_Type";
            // 
            // Label15
            // 
            resources.ApplyResources(Label15, "Label15");
            Label15.Name = "Label15";
            // 
            // Label11
            // 
            resources.ApplyResources(Label11, "Label11");
            Label11.BackColor = System.Drawing.Color.Black;
            Label11.ForeColor = System.Drawing.Color.White;
            Label11.Name = "Label11";
            // 
            // Label19
            // 
            resources.ApplyResources(Label19, "Label19");
            Label19.Name = "Label19";
            // 
            // Label18
            // 
            resources.ApplyResources(Label18, "Label18");
            Label18.BackColor = System.Drawing.Color.Black;
            Label18.ForeColor = System.Drawing.Color.White;
            Label18.Name = "Label18";
            // 
            // textBox_Summe
            // 
            textBox_Summe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Summe, "textBox_Summe");
            textBox_Summe.Name = "textBox_Summe";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // listView_Auswahl
            // 
            listView_Auswahl.FullRowSelect = true;
            listView_Auswahl.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            resources.ApplyResources(listView_Auswahl, "listView_Auswahl");
            listView_Auswahl.Name = "listView_Auswahl";
            listView_Auswahl.UseCompatibleStateImageBehavior = false;
            listView_Auswahl.SelectedIndexChanged += listView_Auswahl_SelectedIndexChanged;
            // 
            // Label1
            // 
            resources.ApplyResources(Label1, "Label1");
            Label1.Name = "Label1";
            // 
            // groupBox1
            // 
            groupBox1.BackColor = System.Drawing.Color.Khaki;
            groupBox1.Controls.Add(pictureBox1);
            groupBox1.Controls.Add(textBox_Verbrauch);
            groupBox1.Controls.Add(Label8);
            groupBox1.Controls.Add(btn_neuerWert);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.setup_trans;
            resources.ApplyResources(pictureBox1, "pictureBox1");
            pictureBox1.Name = "pictureBox1";
            pictureBox1.TabStop = false;
            // 
            // textBox_Verbrauch
            // 
            textBox_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Verbrauch, "textBox_Verbrauch");
            textBox_Verbrauch.Name = "textBox_Verbrauch";
            textBox_Verbrauch.TextChanged += textBox_Verbrauch_TextChanged;
            // 
            // Label8
            // 
            resources.ApplyResources(Label8, "Label8");
            Label8.BackColor = System.Drawing.Color.Black;
            Label8.ForeColor = System.Drawing.Color.White;
            Label8.Name = "Label8";
            // 
            // btn_neuerWert
            // 
            resources.ApplyResources(btn_neuerWert, "btn_neuerWert");
            btn_neuerWert.Image = Properties.Resources.speichern;
            btn_neuerWert.Name = "btn_neuerWert";
            btn_neuerWert.UseVisualStyleBackColor = true;
            btn_neuerWert.Click += btn_neuerWert_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView1, "dataGridView1");
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Click += listBox_Prozess_DB_SelectedIndexChanged;
            // 
            // label_Type
            // 
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.Name = "label_Type";
            // 
            // Form_Brauchwasser
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label_Type);
            Controls.Add(dataGridView1);
            Controls.Add(groupBox1);
            Controls.Add(listView_Auswahl);
            Controls.Add(Label1);
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_Hinzu);
            Controls.Add(btn_Entfernen);
            Controls.Add(btn_Prozess_DBneu);
            Controls.Add(btn_Prozess_loeschen);
            Controls.Add(btn_ErgebnisseVerbrauch);
            Controls.Add(btn_Simulation);
            Controls.Add(btn_OK);
            Controls.Add(Label24);
            Controls.Add(btn_ProzTypeDBedit);
            Controls.Add(btn_Prozess_DBedit);
            Controls.Add(Label12);
            Controls.Add(Label10);
            Controls.Add(textBox_Name);
            Controls.Add(Label13);
            Controls.Add(textBox_Jahres_Verbrauch);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(textBox_Type);
            Controls.Add(Label15);
            Controls.Add(Label11);
            Controls.Add(Label19);
            Controls.Add(Label18);
            Controls.Add(textBox_Summe);
            Name = "Form_Brauchwasser";
            Load += Form_Brauchwasser_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

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
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Type;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.Label Label19;
private System.Windows.Forms.Label Label18;
private System.Windows.Forms.TextBox textBox_Summe;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.ListView listView_Auswahl;
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