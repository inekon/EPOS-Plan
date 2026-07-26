namespace WindowsFormsApplication1
{
    partial class Form_SolarKollektoren
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_SolarKollektoren));
            btn_Kollektor_DB_Edit = new System.Windows.Forms.Button();
            btn_Kollektor_DB_neu = new System.Windows.Forms.Button();
            btn_Kollektor_DB_loeschen = new System.Windows.Forms.Button();
            Label10 = new System.Windows.Forms.Label();
            textBox_Anzahl = new System.Windows.Forms.TextBox();
            Label13 = new System.Windows.Forms.Label();
            textBox_Kollektorneigung = new System.Windows.Forms.TextBox();
            btn_OK = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            textBox_Azimut = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            btn_Hinzzu = new System.Windows.Forms.Button();
            btn_Entfernen = new System.Windows.Forms.Button();
            groupBox_Kollektor = new System.Windows.Forms.GroupBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            textBox_Ruecklauf = new System.Windows.Forms.TextBox();
            btn_Speichern = new System.Windows.Forms.Button();
            Label11 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            label17 = new System.Windows.Forms.Label();
            textBox_Vorlauf = new System.Windows.Forms.TextBox();
            textBox_Aperturflaeche = new System.Windows.Forms.TextBox();
            label14 = new System.Windows.Forms.Label();
            label15 = new System.Windows.Forms.Label();
            btn_Abbrechen = new System.Windows.Forms.Button();
            label_Type = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            label1 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label5 = new System.Windows.Forms.Label();
            Label6 = new System.Windows.Forms.Label();
            Label7 = new System.Windows.Forms.Label();
            Label8 = new System.Windows.Forms.Label();
            textBox_Kollektortype = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            textBox_Firma = new System.Windows.Forms.TextBox();
            textBox_Modul_Apertur = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            listBox_Auswahl = new System.Windows.Forms.ListView();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            groupBox_Kollektor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // btn_Kollektor_DB_Edit
            // 
            resources.ApplyResources(btn_Kollektor_DB_Edit, "btn_Kollektor_DB_Edit");
            btn_Kollektor_DB_Edit.Name = "btn_Kollektor_DB_Edit";
            btn_Kollektor_DB_Edit.UseVisualStyleBackColor = true;
            btn_Kollektor_DB_Edit.Click += btn_Kollektor_DB_Edit_Click;
            // 
            // btn_Kollektor_DB_neu
            // 
            resources.ApplyResources(btn_Kollektor_DB_neu, "btn_Kollektor_DB_neu");
            btn_Kollektor_DB_neu.Name = "btn_Kollektor_DB_neu";
            btn_Kollektor_DB_neu.UseVisualStyleBackColor = true;
            btn_Kollektor_DB_neu.Click += btn_Kollektor_DB_neu_Click;
            // 
            // btn_Kollektor_DB_loeschen
            // 
            resources.ApplyResources(btn_Kollektor_DB_loeschen, "btn_Kollektor_DB_loeschen");
            btn_Kollektor_DB_loeschen.Name = "btn_Kollektor_DB_loeschen";
            btn_Kollektor_DB_loeschen.UseVisualStyleBackColor = true;
            btn_Kollektor_DB_loeschen.Click += btn_Kollektor_DB_loeschen_Click;
            // 
            // Label10
            // 
            resources.ApplyResources(Label10, "Label10");
            Label10.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            Label10.Name = "Label10";
            // 
            // textBox_Anzahl
            // 
            textBox_Anzahl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Anzahl, "textBox_Anzahl");
            textBox_Anzahl.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            textBox_Anzahl.Name = "textBox_Anzahl";
            textBox_Anzahl.TextChanged += textBox_Anzahl_TextChanged;
            // 
            // Label13
            // 
            resources.ApplyResources(Label13, "Label13");
            Label13.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            Label13.Name = "Label13";
            // 
            // textBox_Kollektorneigung
            // 
            textBox_Kollektorneigung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kollektorneigung, "textBox_Kollektorneigung");
            textBox_Kollektorneigung.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            textBox_Kollektorneigung.Name = "textBox_Kollektorneigung";
            textBox_Kollektorneigung.TextChanged += textBox_Kollektorneigung_TextChanged;
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox_Azimut);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(Label13);
            groupBox1.Controls.Add(textBox_Kollektorneigung);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // textBox_Azimut
            // 
            textBox_Azimut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Azimut, "textBox_Azimut");
            textBox_Azimut.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            textBox_Azimut.Name = "textBox_Azimut";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            label4.Name = "label4";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView1, "dataGridView1");
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Click += dataGridView1_Click;
            dataGridView1.Leave += dataGridView1_Leave;
            // 
            // btn_Hinzzu
            // 
            resources.ApplyResources(btn_Hinzzu, "btn_Hinzzu");
            btn_Hinzzu.Name = "btn_Hinzzu";
            btn_Hinzzu.UseVisualStyleBackColor = true;
            btn_Hinzzu.Click += btn_Hinzzu_Click;
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(btn_Entfernen, "btn_Entfernen");
            btn_Entfernen.Name = "btn_Entfernen";
            btn_Entfernen.UseVisualStyleBackColor = true;
            btn_Entfernen.Click += btn_Entfernen_Click;
            // 
            // groupBox_Kollektor
            // 
            groupBox_Kollektor.BackColor = System.Drawing.Color.Khaki;
            groupBox_Kollektor.Controls.Add(pictureBox1);
            groupBox_Kollektor.Controls.Add(textBox_Anzahl);
            groupBox_Kollektor.Controls.Add(textBox_Ruecklauf);
            groupBox_Kollektor.Controls.Add(Label10);
            groupBox_Kollektor.Controls.Add(btn_Speichern);
            groupBox_Kollektor.Controls.Add(Label11);
            groupBox_Kollektor.Controls.Add(label12);
            groupBox_Kollektor.Controls.Add(label17);
            groupBox_Kollektor.Controls.Add(textBox_Vorlauf);
            groupBox_Kollektor.Controls.Add(textBox_Aperturflaeche);
            groupBox_Kollektor.Controls.Add(label14);
            groupBox_Kollektor.Controls.Add(groupBox1);
            groupBox_Kollektor.Controls.Add(label15);
            resources.ApplyResources(groupBox_Kollektor, "groupBox_Kollektor");
            groupBox_Kollektor.Name = "groupBox_Kollektor";
            groupBox_Kollektor.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.setup_trans;
            resources.ApplyResources(pictureBox1, "pictureBox1");
            pictureBox1.Name = "pictureBox1";
            pictureBox1.TabStop = false;
            // 
            // textBox_Ruecklauf
            // 
            textBox_Ruecklauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Ruecklauf, "textBox_Ruecklauf");
            textBox_Ruecklauf.Name = "textBox_Ruecklauf";
            textBox_Ruecklauf.Validating += textBox_Ruecklauf_Validating;
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(btn_Speichern, "btn_Speichern");
            btn_Speichern.Image = Properties.Resources.speichern;
            btn_Speichern.Name = "btn_Speichern";
            btn_Speichern.UseVisualStyleBackColor = true;
            btn_Speichern.Click += btn_Speichern_Click;
            // 
            // Label11
            // 
            resources.ApplyResources(Label11, "Label11");
            Label11.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            Label11.Name = "Label11";
            // 
            // label12
            // 
            label12.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label12, "label12");
            label12.ForeColor = System.Drawing.Color.White;
            label12.Name = "label12";
            // 
            // label17
            // 
            resources.ApplyResources(label17, "label17");
            label17.Name = "label17";
            // 
            // textBox_Vorlauf
            // 
            textBox_Vorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Vorlauf, "textBox_Vorlauf");
            textBox_Vorlauf.Name = "textBox_Vorlauf";
            textBox_Vorlauf.Validating += textBox_Vorlauf_Validating;
            // 
            // textBox_Aperturflaeche
            // 
            textBox_Aperturflaeche.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Aperturflaeche, "textBox_Aperturflaeche");
            textBox_Aperturflaeche.Name = "textBox_Aperturflaeche";
            // 
            // label14
            // 
            label14.BackColor = System.Drawing.Color.Black;
            resources.ApplyResources(label14, "label14");
            label14.ForeColor = System.Drawing.Color.White;
            label14.Name = "label14";
            // 
            // label15
            // 
            resources.ApplyResources(label15, "label15");
            label15.Name = "label15";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click_1;
            // 
            // label_Type
            // 
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.Name = "label_Type";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(textBox_Name);
            groupBox2.Controls.Add(Label5);
            groupBox2.Controls.Add(Label6);
            groupBox2.Controls.Add(Label7);
            groupBox2.Controls.Add(Label8);
            groupBox2.Controls.Add(textBox_Kollektortype);
            groupBox2.Controls.Add(textBox_Beschreibung);
            groupBox2.Controls.Add(textBox_Firma);
            groupBox2.Controls.Add(textBox_Modul_Apertur);
            groupBox2.Controls.Add(Label9);
            resources.ApplyResources(groupBox2, "groupBox2");
            groupBox2.Name = "groupBox2";
            groupBox2.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.Name = "textBox_Name";
            // 
            // Label5
            // 
            resources.ApplyResources(Label5, "Label5");
            Label5.Name = "Label5";
            // 
            // Label6
            // 
            resources.ApplyResources(Label6, "Label6");
            Label6.Name = "Label6";
            // 
            // Label7
            // 
            resources.ApplyResources(Label7, "Label7");
            Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(Label8, "Label8");
            Label8.Name = "Label8";
            // 
            // textBox_Kollektortype
            // 
            textBox_Kollektortype.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Kollektortype, "textBox_Kollektortype");
            textBox_Kollektortype.Name = "textBox_Kollektortype";
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Firma, "textBox_Firma");
            textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Modul_Apertur
            // 
            textBox_Modul_Apertur.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Modul_Apertur, "textBox_Modul_Apertur");
            textBox_Modul_Apertur.Name = "textBox_Modul_Apertur";
            // 
            // Label9
            // 
            resources.ApplyResources(Label9, "Label9");
            Label9.BackColor = System.Drawing.Color.Black;
            Label9.ForeColor = System.Drawing.Color.White;
            Label9.Name = "Label9";
            // 
            // listBox_Auswahl
            // 
            resources.ApplyResources(listBox_Auswahl, "listBox_Auswahl");
            listBox_Auswahl.Name = "listBox_Auswahl";
            listBox_Auswahl.UseCompatibleStateImageBehavior = false;
            listBox_Auswahl.SelectedIndexChanged += listBox_Auswahl_SelectedIndexChanged;
            listBox_Auswahl.MouseClick += listBox_Auswahl_MouseClick;
            // 
            // Form_SolarKollektoren
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(listBox_Auswahl);
            Controls.Add(groupBox2);
            Controls.Add(label_Type);
            Controls.Add(btn_Abbrechen);
            Controls.Add(groupBox_Kollektor);
            Controls.Add(btn_Hinzzu);
            Controls.Add(btn_Entfernen);
            Controls.Add(dataGridView1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(btn_Kollektor_DB_Edit);
            Controls.Add(btn_Kollektor_DB_neu);
            Controls.Add(btn_Kollektor_DB_loeschen);
            Controls.Add(btn_OK);
            Name = "Form_SolarKollektoren";
            Load += Form_SolarKollektoren_Load;
            Paint += Form_SolarKollektoren_Paint;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            groupBox_Kollektor.ResumeLayout(false);
            groupBox_Kollektor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Kollektor_DB_Edit;
private System.Windows.Forms.Button btn_Kollektor_DB_neu;
private System.Windows.Forms.Button btn_Kollektor_DB_loeschen;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Anzahl;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Kollektorneigung;
private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btn_Hinzzu;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.GroupBox groupBox_Kollektor;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label Label11;
        private System.Windows.Forms.TextBox textBox_Aperturflaeche;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label Label5;
        private System.Windows.Forms.Label Label6;
        private System.Windows.Forms.Label Label7;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.TextBox textBox_Kollektortype;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Firma;
        private System.Windows.Forms.TextBox textBox_Modul_Apertur;
        private System.Windows.Forms.Label Label9;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox_Azimut;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Ruecklauf;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textBox_Vorlauf;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ListView listBox_Auswahl;
    }
}