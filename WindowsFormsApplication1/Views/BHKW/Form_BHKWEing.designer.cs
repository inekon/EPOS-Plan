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
            Label1 = new System.Windows.Forms.Label();
            Label5 = new System.Windows.Forms.Label();
            Label4 = new System.Windows.Forms.Label();
            listBox_Auswahl = new System.Windows.Forms.ListBox();
            btn_Hinzzu = new System.Windows.Forms.Button();
            btn_BHKW_Löschen = new System.Windows.Forms.Button();
            textBox__M_GrenzL = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            Label23 = new System.Windows.Forms.Label();
            comboBox_Brennstoff = new System.Windows.Forms.ComboBox();
            comboBox_Leistung = new System.Windows.Forms.ComboBox();
            btn_DBBHKW_Edit = new System.Windows.Forms.Button();
            btn_DBBHKW_Neu = new System.Windows.Forms.Button();
            btn_DBBHKW_Löschen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            Label7 = new System.Windows.Forms.Label();
            Label8 = new System.Windows.Forms.Label();
            textBox__Summe_Leistung = new System.Windows.Forms.TextBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            Label19 = new System.Windows.Forms.Label();
            Label17 = new System.Windows.Forms.Label();
            Label18 = new System.Windows.Forms.Label();
            Label20 = new System.Windows.Forms.Label();
            textBox_Volumen_Pendelsp = new System.Windows.Forms.TextBox();
            textBox_Größe_Pendelsp = new System.Windows.Forms.TextBox();
            checkBox_Rendemix = new System.Windows.Forms.CheckBox();
            dataGridView1 = new System.Windows.Forms.DataGridView();
            btn_Abbrechen = new System.Windows.Forms.Button();
            label_Type = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            label2 = new System.Windows.Forms.Label();
            Label12 = new System.Windows.Forms.Label();
            Label10 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            textBox_Leistung_th = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            textBox_Firma = new System.Windows.Forms.TextBox();
            textBox_Leistung_el = new System.Windows.Forms.TextBox();
            Label14 = new System.Windows.Forms.Label();
            Label15 = new System.Windows.Forms.Label();
            Label16 = new System.Windows.Forms.Label();
            Label11 = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Label1
            // 
            resources.ApplyResources(Label1, "Label1");
            Label1.Name = "Label1";
            // 
            // Label5
            // 
            resources.ApplyResources(Label5, "Label5");
            Label5.Name = "Label5";
            // 
            // Label4
            // 
            resources.ApplyResources(Label4, "Label4");
            Label4.Name = "Label4";
            // 
            // listBox_Auswahl
            // 
            resources.ApplyResources(listBox_Auswahl, "listBox_Auswahl");
            listBox_Auswahl.Name = "listBox_Auswahl";
            listBox_Auswahl.SelectedIndexChanged += listBox_Auswahl_SelectedIndexChanged;
            // 
            // btn_Hinzzu
            // 
            resources.ApplyResources(btn_Hinzzu, "btn_Hinzzu");
            btn_Hinzzu.Name = "btn_Hinzzu";
            btn_Hinzzu.UseVisualStyleBackColor = true;
            btn_Hinzzu.Click += btn_Hinzzu_Click;
            // 
            // btn_BHKW_Löschen
            // 
            resources.ApplyResources(btn_BHKW_Löschen, "btn_BHKW_Löschen");
            btn_BHKW_Löschen.Name = "btn_BHKW_Löschen";
            btn_BHKW_Löschen.UseVisualStyleBackColor = true;
            btn_BHKW_Löschen.Click += btn_BHKW_Löschen_Click;
            // 
            // textBox__M_GrenzL
            // 
            textBox__M_GrenzL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox__M_GrenzL, "textBox__M_GrenzL");
            textBox__M_GrenzL.Name = "textBox__M_GrenzL";
            textBox__M_GrenzL.Validating += textBox__M_GrenzL_Validating;
            // 
            // Label9
            // 
            resources.ApplyResources(Label9, "Label9");
            Label9.BackColor = System.Drawing.Color.Black;
            Label9.ForeColor = System.Drawing.Color.White;
            Label9.Name = "Label9";
            // 
            // Label23
            // 
            resources.ApplyResources(Label23, "Label23");
            Label23.Name = "Label23";
            // 
            // comboBox_Brennstoff
            // 
            resources.ApplyResources(comboBox_Brennstoff, "comboBox_Brennstoff");
            comboBox_Brennstoff.Name = "comboBox_Brennstoff";
            comboBox_Brennstoff.SelectedIndexChanged += comboBox_Brennstoff_SelectedIndexChanged;
            // 
            // comboBox_Leistung
            // 
            resources.ApplyResources(comboBox_Leistung, "comboBox_Leistung");
            comboBox_Leistung.Name = "comboBox_Leistung";
            comboBox_Leistung.SelectedIndexChanged += comboBox_Leistung_SelectedIndexChanged;
            // 
            // btn_DBBHKW_Edit
            // 
            resources.ApplyResources(btn_DBBHKW_Edit, "btn_DBBHKW_Edit");
            btn_DBBHKW_Edit.Name = "btn_DBBHKW_Edit";
            btn_DBBHKW_Edit.UseVisualStyleBackColor = true;
            btn_DBBHKW_Edit.Click += btn_DBBHKW_Edit_Click;
            // 
            // btn_DBBHKW_Neu
            // 
            resources.ApplyResources(btn_DBBHKW_Neu, "btn_DBBHKW_Neu");
            btn_DBBHKW_Neu.Name = "btn_DBBHKW_Neu";
            btn_DBBHKW_Neu.UseVisualStyleBackColor = true;
            btn_DBBHKW_Neu.Click += btn_DBBHKW_Neu_Click;
            // 
            // btn_DBBHKW_Löschen
            // 
            resources.ApplyResources(btn_DBBHKW_Löschen, "btn_DBBHKW_Löschen");
            btn_DBBHKW_Löschen.Name = "btn_DBBHKW_Löschen";
            btn_DBBHKW_Löschen.UseVisualStyleBackColor = true;
            btn_DBBHKW_Löschen.Click += btn_DBBHKW_Löschen_Click;
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // Label7
            // 
            resources.ApplyResources(Label7, "Label7");
            Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(Label8, "Label8");
            Label8.BackColor = System.Drawing.Color.Black;
            Label8.ForeColor = System.Drawing.Color.White;
            Label8.Name = "Label8";
            // 
            // textBox__Summe_Leistung
            // 
            textBox__Summe_Leistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox__Summe_Leistung, "textBox__Summe_Leistung");
            textBox__Summe_Leistung.Name = "textBox__Summe_Leistung";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(Label19);
            groupBox1.Controls.Add(Label17);
            groupBox1.Controls.Add(Label18);
            groupBox1.Controls.Add(Label20);
            groupBox1.Controls.Add(textBox_Volumen_Pendelsp);
            groupBox1.Controls.Add(textBox_Größe_Pendelsp);
            groupBox1.Controls.Add(checkBox_Rendemix);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // Label19
            // 
            resources.ApplyResources(Label19, "Label19");
            Label19.Name = "Label19";
            // 
            // Label17
            // 
            resources.ApplyResources(Label17, "Label17");
            Label17.BackColor = System.Drawing.Color.Black;
            Label17.ForeColor = System.Drawing.Color.White;
            Label17.Name = "Label17";
            // 
            // Label18
            // 
            resources.ApplyResources(Label18, "Label18");
            Label18.BackColor = System.Drawing.Color.Black;
            Label18.ForeColor = System.Drawing.Color.White;
            Label18.Name = "Label18";
            // 
            // Label20
            // 
            resources.ApplyResources(Label20, "Label20");
            Label20.Name = "Label20";
            // 
            // textBox_Volumen_Pendelsp
            // 
            textBox_Volumen_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Volumen_Pendelsp, "textBox_Volumen_Pendelsp");
            textBox_Volumen_Pendelsp.Name = "textBox_Volumen_Pendelsp";
            textBox_Volumen_Pendelsp.TextChanged += textBox_Volumen_Pendelsp_TextChanged;
            // 
            // textBox_Größe_Pendelsp
            // 
            textBox_Größe_Pendelsp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Größe_Pendelsp, "textBox_Größe_Pendelsp");
            textBox_Größe_Pendelsp.Name = "textBox_Größe_Pendelsp";
            // 
            // checkBox_Rendemix
            // 
            resources.ApplyResources(checkBox_Rendemix, "checkBox_Rendemix");
            checkBox_Rendemix.Name = "checkBox_Rendemix";
            checkBox_Rendemix.UseVisualStyleBackColor = true;
            checkBox_Rendemix.CheckedChanged += checkBox_Rendemix_CheckedChanged;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView1, "dataGridView1");
            dataGridView1.Name = "dataGridView1";
            dataGridView1.SelectionChanged += dataGridView1_Click;
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // label_Type
            // 
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.Name = "label_Type";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.White;
            panel1.Controls.Add(label2);
            panel1.Controls.Add(Label12);
            panel1.Controls.Add(Label10);
            panel1.Controls.Add(textBox_Name);
            panel1.Controls.Add(textBox_Leistung_th);
            panel1.Controls.Add(textBox_Beschreibung);
            panel1.Controls.Add(textBox_Firma);
            panel1.Controls.Add(textBox_Leistung_el);
            panel1.Controls.Add(Label14);
            panel1.Controls.Add(Label15);
            panel1.Controls.Add(Label16);
            panel1.Controls.Add(Label11);
            resources.ApplyResources(panel1, "panel1");
            panel1.Name = "panel1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
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
            // textBox_Leistung_th
            // 
            textBox_Leistung_th.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Leistung_th, "textBox_Leistung_th");
            textBox_Leistung_th.Name = "textBox_Leistung_th";
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            textBox_Beschreibung.ReadOnly = true;
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Firma, "textBox_Firma");
            textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Leistung_el
            // 
            textBox_Leistung_el.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Leistung_el, "textBox_Leistung_el");
            textBox_Leistung_el.Name = "textBox_Leistung_el";
            // 
            // Label14
            // 
            resources.ApplyResources(Label14, "Label14");
            Label14.Name = "Label14";
            // 
            // Label15
            // 
            resources.ApplyResources(Label15, "Label15");
            Label15.Name = "Label15";
            // 
            // Label16
            // 
            resources.ApplyResources(Label16, "Label16");
            Label16.BackColor = System.Drawing.Color.Black;
            Label16.ForeColor = System.Drawing.Color.White;
            Label16.Name = "Label16";
            // 
            // Label11
            // 
            resources.ApplyResources(Label11, "Label11");
            Label11.BackColor = System.Drawing.Color.Black;
            Label11.ForeColor = System.Drawing.Color.White;
            Label11.Name = "Label11";
            // 
            // Form_BHKWEing
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(panel1);
            Controls.Add(label3);
            Controls.Add(label_Type);
            Controls.Add(btn_Abbrechen);
            Controls.Add(dataGridView1);
            Controls.Add(groupBox1);
            Controls.Add(Label1);
            Controls.Add(Label5);
            Controls.Add(Label4);
            Controls.Add(listBox_Auswahl);
            Controls.Add(btn_Hinzzu);
            Controls.Add(btn_BHKW_Löschen);
            Controls.Add(textBox__M_GrenzL);
            Controls.Add(Label9);
            Controls.Add(Label23);
            Controls.Add(comboBox_Brennstoff);
            Controls.Add(comboBox_Leistung);
            Controls.Add(btn_DBBHKW_Edit);
            Controls.Add(btn_DBBHKW_Neu);
            Controls.Add(btn_DBBHKW_Löschen);
            Controls.Add(btn_OK);
            Controls.Add(Label7);
            Controls.Add(Label8);
            Controls.Add(textBox__Summe_Leistung);
            Name = "Form_BHKWEing";
            Load += Form_BHKWEing_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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