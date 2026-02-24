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
            this.btn_Kollektor_DB_Edit = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_neu = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_loeschen = new System.Windows.Forms.Button();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Anzahl = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Kollektorneigung = new System.Windows.Forms.TextBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btn_Hinzzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.groupBox_Kollektor = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.Label11 = new System.Windows.Forms.Label();
            this.textBox_Kollektor_A = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Kollektortype = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Modul_A = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.textBox_Azimut = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox_Kollektor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Kollektor_DB_Edit
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_Edit, "btn_Kollektor_DB_Edit");
            this.btn_Kollektor_DB_Edit.Name = "btn_Kollektor_DB_Edit";
            this.btn_Kollektor_DB_Edit.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_Edit.Click += new System.EventHandler(this.btn_Kollektor_DB_Edit_Click);
            // 
            // btn_Kollektor_DB_neu
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_neu, "btn_Kollektor_DB_neu");
            this.btn_Kollektor_DB_neu.Name = "btn_Kollektor_DB_neu";
            this.btn_Kollektor_DB_neu.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_neu.Click += new System.EventHandler(this.btn_Kollektor_DB_neu_Click);
            // 
            // btn_Kollektor_DB_loeschen
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_loeschen, "btn_Kollektor_DB_loeschen");
            this.btn_Kollektor_DB_loeschen.Name = "btn_Kollektor_DB_loeschen";
            this.btn_Kollektor_DB_loeschen.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_loeschen.Click += new System.EventHandler(this.btn_Kollektor_DB_loeschen_Click);
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label10.Name = "Label10";
            // 
            // textBox_Anzahl
            // 
            this.textBox_Anzahl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Anzahl, "textBox_Anzahl");
            this.textBox_Anzahl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.textBox_Anzahl.Name = "textBox_Anzahl";
            this.textBox_Anzahl.TextChanged += new System.EventHandler(this.textBox_Anzahl_TextChanged);
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label13.Name = "Label13";
            // 
            // textBox_Kollektorneigung
            // 
            this.textBox_Kollektorneigung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Kollektorneigung, "textBox_Kollektorneigung");
            this.textBox_Kollektorneigung.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.textBox_Kollektorneigung.Name = "textBox_Kollektorneigung";
            this.textBox_Kollektorneigung.TextChanged += new System.EventHandler(this.textBox_Kollektorneigung_TextChanged);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // listBox_Auswahl
            // 
            resources.ApplyResources(this.listBox_Auswahl, "listBox_Auswahl");
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox_Azimut);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.textBox_Kollektorneigung);
            this.groupBox1.Controls.Add(this.Label13);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Click += new System.EventHandler(this.dataGridView1_Click);
            this.dataGridView1.Leave += new System.EventHandler(this.dataGridView1_Leave);
            // 
            // btn_Hinzzu
            // 
            resources.ApplyResources(this.btn_Hinzzu, "btn_Hinzzu");
            this.btn_Hinzzu.Name = "btn_Hinzzu";
            this.btn_Hinzzu.UseVisualStyleBackColor = true;
            this.btn_Hinzzu.Click += new System.EventHandler(this.btn_Hinzzu_Click);
            // 
            // btn_Entfernen
            // 
            resources.ApplyResources(this.btn_Entfernen, "btn_Entfernen");
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // groupBox_Kollektor
            // 
            this.groupBox_Kollektor.BackColor = System.Drawing.Color.Khaki;
            this.groupBox_Kollektor.Controls.Add(this.pictureBox1);
            this.groupBox_Kollektor.Controls.Add(this.btn_Speichern);
            this.groupBox_Kollektor.Controls.Add(this.textBox_Anzahl);
            this.groupBox_Kollektor.Controls.Add(this.Label10);
            this.groupBox_Kollektor.Controls.Add(this.Label11);
            this.groupBox_Kollektor.Controls.Add(this.textBox_Kollektor_A);
            this.groupBox_Kollektor.Controls.Add(this.groupBox1);
            resources.ApplyResources(this.groupBox_Kollektor, "groupBox_Kollektor");
            this.groupBox_Kollektor.Name = "groupBox_Kollektor";
            this.groupBox_Kollektor.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.setup_trans;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.Label11.Name = "Label11";
            // 
            // textBox_Kollektor_A
            // 
            this.textBox_Kollektor_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Kollektor_A, "textBox_Kollektor_A");
            this.textBox_Kollektor_A.Name = "textBox_Kollektor_A";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click_1);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.Name = "label_Type";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.textBox_Name);
            this.groupBox2.Controls.Add(this.Label5);
            this.groupBox2.Controls.Add(this.Label6);
            this.groupBox2.Controls.Add(this.Label7);
            this.groupBox2.Controls.Add(this.Label8);
            this.groupBox2.Controls.Add(this.textBox_Kollektortype);
            this.groupBox2.Controls.Add(this.textBox_Beschreibung);
            this.groupBox2.Controls.Add(this.textBox_Firma);
            this.groupBox2.Controls.Add(this.textBox_Modul_A);
            this.groupBox2.Controls.Add(this.Label9);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.Name = "textBox_Name";
            // 
            // Label5
            // 
            resources.ApplyResources(this.Label5, "Label5");
            this.Label5.Name = "Label5";
            // 
            // Label6
            // 
            resources.ApplyResources(this.Label6, "Label6");
            this.Label6.Name = "Label6";
            // 
            // Label7
            // 
            resources.ApplyResources(this.Label7, "Label7");
            this.Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.Name = "Label8";
            // 
            // textBox_Kollektortype
            // 
            this.textBox_Kollektortype.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Kollektortype, "textBox_Kollektortype");
            this.textBox_Kollektortype.Name = "textBox_Kollektortype";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Modul_A
            // 
            this.textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Modul_A, "textBox_Modul_A");
            this.textBox_Modul_A.Name = "textBox_Modul_A";
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Name = "Label9";
            // 
            // textBox_Azimut
            // 
            this.textBox_Azimut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Azimut, "textBox_Azimut");
            this.textBox_Azimut.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.textBox_Azimut.Name = "textBox_Azimut";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label4.Name = "label4";
            // 
            // Form_SolarKollektoren
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.groupBox_Kollektor);
            this.Controls.Add(this.btn_Hinzzu);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Kollektor_DB_Edit);
            this.Controls.Add(this.btn_Kollektor_DB_neu);
            this.Controls.Add(this.btn_Kollektor_DB_loeschen);
            this.Controls.Add(this.btn_OK);
            this.Name = "Form_SolarKollektoren";
            this.Load += new System.EventHandler(this.Form_SolarKollektoren_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form_SolarKollektoren_Paint);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox_Kollektor.ResumeLayout(false);
            this.groupBox_Kollektor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.ListBox listBox_Auswahl;
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
        private System.Windows.Forms.TextBox textBox_Kollektor_A;
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
        private System.Windows.Forms.TextBox textBox_Modul_A;
        private System.Windows.Forms.Label Label9;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox_Azimut;
        private System.Windows.Forms.Label label4;
    }
}