namespace WindowsFormsApplication1
{
    partial class Form_BHKWAdmin
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
            this.Label5 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.textBox__M_GrenzL = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.Label23 = new System.Windows.Forms.Label();
            this.comboBox_Brennstoff = new System.Windows.Forms.ComboBox();
            this.comboBox_Leistung = new System.Windows.Forms.ComboBox();
            this.btn_DBBHKW_Edit = new System.Windows.Forms.Button();
            this.btn_DBBHKW_Neu = new System.Windows.Forms.Button();
            this.btn_DBBHKW_Löschen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label5.Location = new System.Drawing.Point(11, 404);
            this.Label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(153, 17);
            this.Label5.TabIndex = 3;
            this.Label5.Text = "Filtern nach Brennstoffart";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label4
            // 
            this.Label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label4.Location = new System.Drawing.Point(3, 135);
            this.Label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(134, 63);
            this.Label4.TabIndex = 4;
            this.Label4.Text = "Untere Grenzleistung des ausgewählten Moduls:";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox__M_GrenzL
            // 
            this.textBox__M_GrenzL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox__M_GrenzL.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox__M_GrenzL.Location = new System.Drawing.Point(138, 140);
            this.textBox__M_GrenzL.Margin = new System.Windows.Forms.Padding(5);
            this.textBox__M_GrenzL.Name = "textBox__M_GrenzL";
            this.textBox__M_GrenzL.Size = new System.Drawing.Size(65, 25);
            this.textBox__M_GrenzL.TabIndex = 11;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Location = new System.Drawing.Point(212, 143);
            this.Label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(19, 17);
            this.Label9.TabIndex = 12;
            this.Label9.Text = "%";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label23
            // 
            this.Label23.AutoSize = true;
            this.Label23.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label23.Location = new System.Drawing.Point(12, 453);
            this.Label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label23.Name = "Label23";
            this.Label23.Size = new System.Drawing.Size(126, 17);
            this.Label23.TabIndex = 13;
            this.Label23.Text = "Filtern nach Leistung";
            this.Label23.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBox_Brennstoff
            // 
            this.comboBox_Brennstoff.Location = new System.Drawing.Point(12, 424);
            this.comboBox_Brennstoff.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Brennstoff.Name = "comboBox_Brennstoff";
            this.comboBox_Brennstoff.Size = new System.Drawing.Size(164, 25);
            this.comboBox_Brennstoff.TabIndex = 14;
            this.comboBox_Brennstoff.SelectedIndexChanged += new System.EventHandler(this.comboBox_Brennstoff_SelectedIndexChanged);
            // 
            // comboBox_Leistung
            // 
            this.comboBox_Leistung.Location = new System.Drawing.Point(13, 474);
            this.comboBox_Leistung.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox_Leistung.Name = "comboBox_Leistung";
            this.comboBox_Leistung.Size = new System.Drawing.Size(164, 25);
            this.comboBox_Leistung.TabIndex = 15;
            this.comboBox_Leistung.SelectedIndexChanged += new System.EventHandler(this.comboBox_Leistung_SelectedIndexChanged);
            // 
            // btn_DBBHKW_Edit
            // 
            this.btn_DBBHKW_Edit.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBBHKW_Edit.Location = new System.Drawing.Point(251, 402);
            this.btn_DBBHKW_Edit.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBBHKW_Edit.Name = "btn_DBBHKW_Edit";
            this.btn_DBBHKW_Edit.Size = new System.Drawing.Size(164, 31);
            this.btn_DBBHKW_Edit.TabIndex = 16;
            this.btn_DBBHKW_Edit.Text = "BHKW in DB ändern...";
            this.btn_DBBHKW_Edit.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Edit.Click += new System.EventHandler(this.btn_DBBHKW_Edit_Click);
            // 
            // btn_DBBHKW_Neu
            // 
            this.btn_DBBHKW_Neu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBBHKW_Neu.Location = new System.Drawing.Point(251, 474);
            this.btn_DBBHKW_Neu.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBBHKW_Neu.Name = "btn_DBBHKW_Neu";
            this.btn_DBBHKW_Neu.Size = new System.Drawing.Size(164, 31);
            this.btn_DBBHKW_Neu.TabIndex = 17;
            this.btn_DBBHKW_Neu.Text = "BHKW in DB neu...";
            this.btn_DBBHKW_Neu.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Neu.Click += new System.EventHandler(this.btn_DBBHKW_Neu_Click);
            // 
            // btn_DBBHKW_Löschen
            // 
            this.btn_DBBHKW_Löschen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_DBBHKW_Löschen.Location = new System.Drawing.Point(251, 435);
            this.btn_DBBHKW_Löschen.Margin = new System.Windows.Forms.Padding(4);
            this.btn_DBBHKW_Löschen.Name = "btn_DBBHKW_Löschen";
            this.btn_DBBHKW_Löschen.Size = new System.Drawing.Size(164, 31);
            this.btn_DBBHKW_Löschen.TabIndex = 18;
            this.btn_DBBHKW_Löschen.Text = "BHKW in DB löschen";
            this.btn_DBBHKW_Löschen.UseVisualStyleBackColor = true;
            this.btn_DBBHKW_Löschen.Click += new System.EventHandler(this.btn_DBBHKW_Löschen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_OK.Location = new System.Drawing.Point(752, 474);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(91, 31);
            this.btn_OK.TabIndex = 25;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.Label12);
            this.groupBox2.Controls.Add(this.Label10);
            this.groupBox2.Controls.Add(this.Label4);
            this.groupBox2.Controls.Add(this.textBox_Name);
            this.groupBox2.Controls.Add(this.textBox__M_GrenzL);
            this.groupBox2.Controls.Add(this.Label9);
            this.groupBox2.Controls.Add(this.textBox_Leistung_th);
            this.groupBox2.Controls.Add(this.textBox_Beschreibung);
            this.groupBox2.Controls.Add(this.textBox_Firma);
            this.groupBox2.Controls.Add(this.textBox_Leistung_el);
            this.groupBox2.Controls.Add(this.Label14);
            this.groupBox2.Controls.Add(this.Label15);
            this.groupBox2.Controls.Add(this.Label16);
            this.groupBox2.Controls.Add(this.Label11);
            this.groupBox2.Location = new System.Drawing.Point(438, 28);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(405, 366);
            this.groupBox2.TabIndex = 52;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Info markiertes BHKW";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 210);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 17);
            this.label2.TabIndex = 60;
            this.label2.Text = "Beschreibung:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label12.Location = new System.Drawing.Point(5, 81);
            this.Label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(126, 17);
            this.Label12.TabIndex = 48;
            this.Label12.Text = "thermische Leistung:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label10.Location = new System.Drawing.Point(42, 24);
            this.Label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(89, 17);
            this.Label10.TabIndex = 49;
            this.Label10.Text = "Modul-Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Name.Location = new System.Drawing.Point(137, 22);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(182, 25);
            this.textBox_Name.TabIndex = 50;
            // 
            // textBox_Leistung_th
            // 
            this.textBox_Leistung_th.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Leistung_th.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Leistung_th.Location = new System.Drawing.Point(137, 81);
            this.textBox_Leistung_th.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Leistung_th.Name = "textBox_Leistung_th";
            this.textBox_Leistung_th.Size = new System.Drawing.Size(63, 25);
            this.textBox_Leistung_th.TabIndex = 52;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Beschreibung.Location = new System.Drawing.Point(8, 231);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            this.textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(380, 119);
            this.textBox_Beschreibung.TabIndex = 53;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Firma.Location = new System.Drawing.Point(137, 51);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(182, 25);
            this.textBox_Firma.TabIndex = 54;
            // 
            // textBox_Leistung_el
            // 
            this.textBox_Leistung_el.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Leistung_el.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Leistung_el.Location = new System.Drawing.Point(137, 110);
            this.textBox_Leistung_el.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Leistung_el.Name = "textBox_Leistung_el";
            this.textBox_Leistung_el.Size = new System.Drawing.Size(63, 25);
            this.textBox_Leistung_el.TabIndex = 55;
            // 
            // Label14
            // 
            this.Label14.AutoSize = true;
            this.Label14.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label14.Location = new System.Drawing.Point(7, 109);
            this.Label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label14.Name = "Label14";
            this.Label14.Size = new System.Drawing.Size(124, 17);
            this.Label14.TabIndex = 56;
            this.Label14.Text = "elektrische Leistung:";
            this.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label15.Location = new System.Drawing.Point(64, 53);
            this.Label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(67, 17);
            this.Label15.TabIndex = 57;
            this.Label15.Text = "Hersteller:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label16
            // 
            this.Label16.AutoSize = true;
            this.Label16.BackColor = System.Drawing.Color.Black;
            this.Label16.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label16.ForeColor = System.Drawing.Color.White;
            this.Label16.Location = new System.Drawing.Point(204, 114);
            this.Label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label16.Name = "Label16";
            this.Label16.Size = new System.Drawing.Size(35, 17);
            this.Label16.TabIndex = 58;
            this.Label16.Text = "kWel";
            this.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.BackColor = System.Drawing.Color.Black;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Location = new System.Drawing.Point(204, 85);
            this.Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(37, 17);
            this.Label11.TabIndex = 59;
            this.Label11.Text = "kWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 25);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(403, 369);
            this.dataGridView1.TabIndex = 75;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            this.dataGridView1.Click += new System.EventHandler(this.dataGridView1_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(13, 5);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(136, 17);
            this.label3.TabIndex = 79;
            this.label3.Text = "Module in Datenbank:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_BHKWAdmin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(856, 517);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.Label23);
            this.Controls.Add(this.comboBox_Brennstoff);
            this.Controls.Add(this.comboBox_Leistung);
            this.Controls.Add(this.btn_DBBHKW_Edit);
            this.Controls.Add(this.btn_DBBHKW_Neu);
            this.Controls.Add(this.btn_DBBHKW_Löschen);
            this.Controls.Add(this.btn_OK);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_BHKWAdmin";
            this.Text = "BHKW Verwaltung";
            this.Load += new System.EventHandler(this.Form_BHKWEing_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.TextBox textBox__M_GrenzL;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.Label Label23;
private System.Windows.Forms.ComboBox comboBox_Brennstoff;
private System.Windows.Forms.ComboBox comboBox_Leistung;
private System.Windows.Forms.Button btn_DBBHKW_Edit;
private System.Windows.Forms.Button btn_DBBHKW_Neu;
private System.Windows.Forms.Button btn_DBBHKW_Löschen;
private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.TextBox textBox_Leistung_th;
        private System.Windows.Forms.TextBox textBox_Firma;
        private System.Windows.Forms.TextBox textBox_Leistung_el;
        private System.Windows.Forms.Label Label14;
        private System.Windows.Forms.Label Label15;
        private System.Windows.Forms.Label Label16;
        private System.Windows.Forms.Label Label11;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label3;
    }
}