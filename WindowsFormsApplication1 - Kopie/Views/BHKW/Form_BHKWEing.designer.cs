namespace WindowsFormsApplication1
{
    partial class Form_BHKWEing
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_BHKWEing));
            this.Label1 = new System.Windows.Forms.Label();
            this.Label5 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzzu = new System.Windows.Forms.Button();
            this.btn_BHKW_Löschen = new System.Windows.Forms.Button();
            this.textBox__M_GrenzL = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.Label23 = new System.Windows.Forms.Label();
            this.comboBox_Brennstoff = new System.Windows.Forms.ComboBox();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.btn_DBBHKW_Edit = new System.Windows.Forms.Button();
            this.btn_DBBHKW_Neu = new System.Windows.Forms.Button();
            this.btn_DBBHKW_Löschen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox__Summe_Leistung = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Label19 = new System.Windows.Forms.Label();
            this.Label17 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.Label20 = new System.Windows.Forms.Label();
            this.textBox_Volumen_Pendelsp = new System.Windows.Forms.TextBox();
            this.textBox_Größe_Pendelsp = new System.Windows.Forms.TextBox();
            this.checkBox_Rendemix = new System.Windows.Forms.CheckBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.textBox_Leistung_th = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Leistung_el = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label16 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // Label5
            // 
            resources.ApplyResources(this.Label5, "Label5");
            this.Label5.Name = "Label5";
            // 
            // Label4
            // 
            resources.ApplyResources(this.Label4, "Label4");
            this.Label4.Name = "Label4";
            // 
            // listBox_Auswahl
            // 
            resources.ApplyResources(this.listBox_Auswahl, "listBox_Auswahl");
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // btn_Hinzzu
            // 
            resources.ApplyResources(this.btn_Hinzzu, "btn_Hinzzu");
            this.btn_Hinzzu.Name = "btn_Hinzzu";
            this.btn_Hinzzu.UseVisualStyleBackColor = true;
            this.btn_Hinzzu.Click += new System.EventHandler(this.btn_Hinzzu_Click);
            // 
            // btn_BHKW_Löschen
            // 
            resources.ApplyResources(this.btn_BHKW_Löschen, "btn_BHKW_Löschen");
            this.btn_BHKW_Löschen.Name = "btn_BHKW_Löschen";
            this.btn_BHKW_Löschen.UseVisualStyleBackColor = true;
            this.btn_BHKW_Löschen.Click += new System.EventHandler(this.btn_BHKW_Löschen_Click);
            // 
            // textBox__M_GrenzL
            // 
            this.textBox__M_GrenzL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox__M_GrenzL, "textBox__M_GrenzL");
            this.textBox__M_GrenzL.Name = "textBox__M_GrenzL";
            this.textBox__M_GrenzL.Validating += new System.ComponentModel.CancelEventHandler(this.textBox__M_GrenzL_Validating);
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Name = "Label9";
            // 
            // Label23
            // 
            resources.ApplyResources(this.Label23, "Label23");
            this.Label23.Name = "Label23";
            // 
            // comboBox_Brennstoff
            // 
            resources.ApplyResources(this.comboBox_Brennstoff, "comboBox_Brennstoff");
            this.comboBox_Brennstoff.Name = "comboBox_Brennstoff";
            this.comboBox_Brennstoff.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoff_SelectedIndexChanged);
            // 
            // comboBox_Leistung
            // 
            resources.ApplyResources(this.comboBox_Leistung, "comboBox_Leistung");
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // btn_DBBHKW_Edit
            // 
            resources.ApplyResources(this.btn_DBBHKW_Edit, "btn_DBBHKW_Edit");
            this.btn_DBBHKW_Edit.Name = "btn_DBBHKW_Edit";
            this.btn_DBBHKW_Edit.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Edit.Click += new System.EventHandler(this.btn_DBBHKW_Edit_Click);
            // 
            // btn_DBBHKW_Neu
            // 
            resources.ApplyResources(this.btn_DBBHKW_Neu, "btn_DBBHKW_Neu");
            this.btn_DBBHKW_Neu.Name = "btn_DBBHKW_Neu";
            this.btn_DBBHKW_Neu.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Neu.Click += new System.EventHandler(this.btn_DBBHKW_Neu_Click);
            // 
            // btn_DBBHKW_Löschen
            // 
            resources.ApplyResources(this.btn_DBBHKW_Löschen, "btn_DBBHKW_Löschen");
            this.btn_DBBHKW_Löschen.Name = "btn_DBBHKW_Löschen";
            this.btn_DBBHKW_Löschen.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Löschen.Click += new System.EventHandler(this.btn_DBBHKW_Löschen_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label7
            // 
            resources.ApplyResources(this.Label7, "Label7");
            this.Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.BackColor = System.Drawing.Color.Black;
            this.Label8.ForeColor = System.Drawing.Color.White;
            this.Label8.Name = "Label8";
            // 
            // textBox__Summe_Leistung
            // 
            this.textBox__Summe_Leistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox__Summe_Leistung, "textBox__Summe_Leistung");
            this.textBox__Summe_Leistung.Name = "textBox__Summe_Leistung";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Label19);
            this.groupBox1.Controls.Add(this.Label17);
            this.groupBox1.Controls.Add(this.Label18);
            this.groupBox1.Controls.Add(this.Label20);
            this.groupBox1.Controls.Add(this.textBox_Volumen_Pendelsp);
            this.groupBox1.Controls.Add(this.textBox_Größe_Pendelsp);
            this.groupBox1.Controls.Add(this.checkBox_Rendemix);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // Label19
            // 
            resources.ApplyResources(this.Label19, "Label19");
            this.Label19.Name = "Label19";
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.BackColor = System.Drawing.Color.Black;
            this.Label17.ForeColor = System.Drawing.Color.White;
            this.Label17.Name = "Label17";
            // 
            // Label18
            // 
            resources.ApplyResources(this.Label18, "Label18");
            this.Label18.BackColor = System.Drawing.Color.Black;
            this.Label18.ForeColor = System.Drawing.Color.White;
            this.Label18.Name = "Label18";
            // 
            // Label20
            // 
            resources.ApplyResources(this.Label20, "Label20");
            this.Label20.Name = "Label20";
            // 
            // textBox_Volumen_Pendelsp
            // 
            this.textBox_Volumen_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Volumen_Pendelsp, "textBox_Volumen_Pendelsp");
            this.textBox_Volumen_Pendelsp.Name = "textBox_Volumen_Pendelsp";
            this.textBox_Volumen_Pendelsp.TextChanged += new System.EventHandler(this.textBox_Volumen_Pendelsp_TextChanged);
            // 
            // textBox_Größe_Pendelsp
            // 
            this.textBox_Größe_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Größe_Pendelsp, "textBox_Größe_Pendelsp");
            this.textBox_Größe_Pendelsp.Name = "textBox_Größe_Pendelsp";
            // 
            // checkBox_Rendemix
            // 
            resources.ApplyResources(this.checkBox_Rendemix, "checkBox_Rendemix");
            this.checkBox_Rendemix.Name = "checkBox_Rendemix";
            this.checkBox_Rendemix.UseVisualStyleBackColor = true;
            this.checkBox_Rendemix.CheckedChanged += new System.EventHandler(this.checkBox_Rendemix_CheckedChanged);
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
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.Name = "label_Type";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.Label12);
            this.panel1.Controls.Add(this.Label10);
            this.panel1.Controls.Add(this.textBox_Name);
            this.panel1.Controls.Add(this.textBox_Leistung_th);
            this.panel1.Controls.Add(this.textBox_Beschreibung);
            this.panel1.Controls.Add(this.textBox_Firma);
            this.panel1.Controls.Add(this.textBox_Leistung_el);
            this.panel1.Controls.Add(this.Label14);
            this.panel1.Controls.Add(this.Label15);
            this.panel1.Controls.Add(this.Label16);
            this.panel1.Controls.Add(this.Label11);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
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
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.Name = "textBox_Name";
            // 
            // textBox_Leistung_th
            // 
            this.textBox_Leistung_th.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Leistung_th, "textBox_Leistung_th");
            this.textBox_Leistung_th.Name = "textBox_Leistung_th";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Leistung_el
            // 
            this.textBox_Leistung_el.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Leistung_el, "textBox_Leistung_el");
            this.textBox_Leistung_el.Name = "textBox_Leistung_el";
            // 
            // Label14
            // 
            resources.ApplyResources(this.Label14, "Label14");
            this.Label14.Name = "Label14";
            // 
            // Label15
            // 
            resources.ApplyResources(this.Label15, "Label15");
            this.Label15.Name = "Label15";
            // 
            // Label16
            // 
            resources.ApplyResources(this.Label16, "Label16");
            this.Label16.BackColor = System.Drawing.Color.Black;
            this.Label16.ForeColor = System.Drawing.Color.White;
            this.Label16.Name = "Label16";
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.BackColor = System.Drawing.Color.Black;
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Name = "Label11";
            // 
            // Form_BHKWEing
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzzu);
            this.Controls.Add(this.btn_BHKW_Löschen);
            this.Controls.Add(this.textBox__M_GrenzL);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.Label23);
            this.Controls.Add(this.comboBox_Brennstoff);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.btn_DBBHKW_Edit);
            this.Controls.Add(this.btn_DBBHKW_Neu);
            this.Controls.Add(this.btn_DBBHKW_Löschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.textBox__Summe_Leistung);
            this.Name = "Form_BHKWEing";
            this.Load += new System.EventHandler(this.Form_BHKWEing_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.ListBox listBox_Auswahl;
private System.Windows.Forms.Button btn_Hinzzu;
private System.Windows.Forms.Button btn_BHKW_Löschen;
private System.Windows.Forms.TextBox textBox__M_GrenzL;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.Label Label23;
private System.Windows.Forms.ComboBox comboBox_Brennstoff;
private System.Windows.Forms.ComboBox comboBox_Leistung;
private System.Windows.Forms.Button btn_DBBHKW_Edit;
private System.Windows.Forms.Button btn_DBBHKW_Neu;
private System.Windows.Forms.Button btn_DBBHKW_Löschen;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox textBox__Summe_Leistung;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label Label19;
        private System.Windows.Forms.Label Label17;
        private System.Windows.Forms.Label Label18;
        private System.Windows.Forms.Label Label20;
        private System.Windows.Forms.TextBox textBox_Volumen_Pendelsp;
        private System.Windows.Forms.TextBox textBox_Größe_Pendelsp;
        private System.Windows.Forms.CheckBox checkBox_Rendemix;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.TextBox textBox_Leistung_th;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Firma;
        private System.Windows.Forms.TextBox textBox_Leistung_el;
        private System.Windows.Forms.Label Label14;
        private System.Windows.Forms.Label Label15;
        private System.Windows.Forms.Label Label16;
        private System.Windows.Forms.Label Label11;
    }
}