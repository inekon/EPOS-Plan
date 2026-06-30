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
            Label5 = new System.Windows.Forms.Label();
            Label4 = new System.Windows.Forms.Label();
            textBox__M_GrenzL = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            Label23 = new System.Windows.Forms.Label();
            comboBox_Brennstoff = new System.Windows.Forms.ComboBox();
            comboBox_Leistung = new System.Windows.Forms.ComboBox();
            btn_DBBHKW_Edit = new System.Windows.Forms.Button();
            btn_DBBHKW_Neu = new System.Windows.Forms.Button();
            btn_DBBHKW_Löschen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            groupBox2 = new System.Windows.Forms.GroupBox();
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
            dataGridView1 = new System.Windows.Forms.DataGridView();
            label3 = new System.Windows.Forms.Label();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // Label5
            // 
            Label5.AutoSize = true;
            Label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label5.Location = new System.Drawing.Point(11, 404);
            Label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label5.Name = "Label5";
            Label5.Size = new System.Drawing.Size(153, 17);
            Label5.TabIndex = 3;
            Label5.Text = "Filtern nach Brennstoffart";
            Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label4
            // 
            Label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label4.Location = new System.Drawing.Point(3, 135);
            Label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label4.Name = "Label4";
            Label4.Size = new System.Drawing.Size(134, 63);
            Label4.TabIndex = 4;
            Label4.Text = "Untere Grenzleistung des ausgewählten Moduls:";
            Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox__M_GrenzL
            // 
            textBox__M_GrenzL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox__M_GrenzL.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox__M_GrenzL.Location = new System.Drawing.Point(137, 140);
            textBox__M_GrenzL.Margin = new System.Windows.Forms.Padding(5);
            textBox__M_GrenzL.Name = "textBox__M_GrenzL";
            textBox__M_GrenzL.Size = new System.Drawing.Size(65, 25);
            textBox__M_GrenzL.TabIndex = 11;
            // 
            // Label9
            // 
            Label9.AutoSize = true;
            Label9.BackColor = System.Drawing.Color.Black;
            Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label9.ForeColor = System.Drawing.Color.White;
            Label9.Location = new System.Drawing.Point(204, 142);
            Label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label9.Name = "Label9";
            Label9.Size = new System.Drawing.Size(19, 17);
            Label9.TabIndex = 12;
            Label9.Text = "%";
            Label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label23
            // 
            Label23.AutoSize = true;
            Label23.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label23.Location = new System.Drawing.Point(12, 453);
            Label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label23.Name = "Label23";
            Label23.Size = new System.Drawing.Size(126, 17);
            Label23.TabIndex = 13;
            Label23.Text = "Filtern nach Leistung";
            Label23.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBox_Brennstoff
            // 
            comboBox_Brennstoff.Location = new System.Drawing.Point(12, 424);
            comboBox_Brennstoff.Margin = new System.Windows.Forms.Padding(4);
            comboBox_Brennstoff.Name = "comboBox_Brennstoff";
            comboBox_Brennstoff.Size = new System.Drawing.Size(164, 25);
            comboBox_Brennstoff.TabIndex = 14;
            comboBox_Brennstoff.SelectedIndexChanged += comboBox_Brennstoff_SelectedIndexChanged;
            // 
            // comboBox_Leistung
            // 
            comboBox_Leistung.Location = new System.Drawing.Point(13, 474);
            comboBox_Leistung.Margin = new System.Windows.Forms.Padding(4);
            comboBox_Leistung.Name = "comboBox_Leistung";
            comboBox_Leistung.Size = new System.Drawing.Size(164, 25);
            comboBox_Leistung.TabIndex = 15;
            comboBox_Leistung.SelectedIndexChanged += comboBox_Leistung_SelectedIndexChanged;
            // 
            // btn_DBBHKW_Edit
            // 
            btn_DBBHKW_Edit.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btn_DBBHKW_Edit.Location = new System.Drawing.Point(251, 402);
            btn_DBBHKW_Edit.Margin = new System.Windows.Forms.Padding(4);
            btn_DBBHKW_Edit.Name = "btn_DBBHKW_Edit";
            btn_DBBHKW_Edit.Size = new System.Drawing.Size(164, 31);
            btn_DBBHKW_Edit.TabIndex = 16;
            btn_DBBHKW_Edit.Text = "Bearbeiten...";
            btn_DBBHKW_Edit.UseVisualStyleBackColor = true;
            btn_DBBHKW_Edit.Click += btn_DBBHKW_Edit_Click;
            // 
            // btn_DBBHKW_Neu
            // 
            btn_DBBHKW_Neu.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btn_DBBHKW_Neu.Location = new System.Drawing.Point(251, 474);
            btn_DBBHKW_Neu.Margin = new System.Windows.Forms.Padding(4);
            btn_DBBHKW_Neu.Name = "btn_DBBHKW_Neu";
            btn_DBBHKW_Neu.Size = new System.Drawing.Size(164, 31);
            btn_DBBHKW_Neu.TabIndex = 17;
            btn_DBBHKW_Neu.Text = "Neu...";
            btn_DBBHKW_Neu.UseVisualStyleBackColor = true;
            btn_DBBHKW_Neu.Click += btn_DBBHKW_Neu_Click;
            // 
            // btn_DBBHKW_Löschen
            // 
            btn_DBBHKW_Löschen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btn_DBBHKW_Löschen.Location = new System.Drawing.Point(251, 435);
            btn_DBBHKW_Löschen.Margin = new System.Windows.Forms.Padding(4);
            btn_DBBHKW_Löschen.Name = "btn_DBBHKW_Löschen";
            btn_DBBHKW_Löschen.Size = new System.Drawing.Size(164, 31);
            btn_DBBHKW_Löschen.TabIndex = 18;
            btn_DBBHKW_Löschen.Text = "Löschen";
            btn_DBBHKW_Löschen.UseVisualStyleBackColor = true;
            btn_DBBHKW_Löschen.Click += btn_DBBHKW_Löschen_Click;
            // 
            // btn_OK
            // 
            btn_OK.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btn_OK.Location = new System.Drawing.Point(752, 474);
            btn_OK.Margin = new System.Windows.Forms.Padding(4);
            btn_OK.Name = "btn_OK";
            btn_OK.Size = new System.Drawing.Size(91, 31);
            btn_OK.TabIndex = 25;
            btn_OK.Text = "OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(Label12);
            groupBox2.Controls.Add(Label10);
            groupBox2.Controls.Add(Label4);
            groupBox2.Controls.Add(textBox_Name);
            groupBox2.Controls.Add(textBox__M_GrenzL);
            groupBox2.Controls.Add(Label9);
            groupBox2.Controls.Add(textBox_Leistung_th);
            groupBox2.Controls.Add(textBox_Beschreibung);
            groupBox2.Controls.Add(textBox_Firma);
            groupBox2.Controls.Add(textBox_Leistung_el);
            groupBox2.Controls.Add(Label14);
            groupBox2.Controls.Add(Label15);
            groupBox2.Controls.Add(Label16);
            groupBox2.Controls.Add(Label11);
            groupBox2.Location = new System.Drawing.Point(438, 28);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(405, 366);
            groupBox2.TabIndex = 52;
            groupBox2.TabStop = false;
            groupBox2.Text = "Info markiertes BHKW";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label2.Location = new System.Drawing.Point(6, 210);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(89, 17);
            label2.TabIndex = 60;
            label2.Text = "Beschreibung:";
            label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label12
            // 
            Label12.AutoSize = true;
            Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label12.Location = new System.Drawing.Point(5, 81);
            Label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label12.Name = "Label12";
            Label12.Size = new System.Drawing.Size(126, 17);
            Label12.TabIndex = 48;
            Label12.Text = "thermische Leistung:";
            Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            Label10.AutoSize = true;
            Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label10.Location = new System.Drawing.Point(42, 24);
            Label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label10.Name = "Label10";
            Label10.Size = new System.Drawing.Size(89, 17);
            Label10.TabIndex = 49;
            Label10.Text = "Modul-Name:";
            Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Name.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Name.Location = new System.Drawing.Point(137, 22);
            textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            textBox_Name.Name = "textBox_Name";
            textBox_Name.Size = new System.Drawing.Size(182, 25);
            textBox_Name.TabIndex = 50;
            // 
            // textBox_Leistung_th
            // 
            textBox_Leistung_th.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Leistung_th.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Leistung_th.Location = new System.Drawing.Point(137, 81);
            textBox_Leistung_th.Margin = new System.Windows.Forms.Padding(5);
            textBox_Leistung_th.Name = "textBox_Leistung_th";
            textBox_Leistung_th.Size = new System.Drawing.Size(65, 25);
            textBox_Leistung_th.TabIndex = 52;
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Beschreibung.Location = new System.Drawing.Point(8, 231);
            textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            textBox_Beschreibung.Multiline = true;
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            textBox_Beschreibung.ReadOnly = true;
            textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            textBox_Beschreibung.Size = new System.Drawing.Size(380, 119);
            textBox_Beschreibung.TabIndex = 53;
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Firma.Location = new System.Drawing.Point(137, 51);
            textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            textBox_Firma.Name = "textBox_Firma";
            textBox_Firma.Size = new System.Drawing.Size(182, 25);
            textBox_Firma.TabIndex = 54;
            // 
            // textBox_Leistung_el
            // 
            textBox_Leistung_el.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Leistung_el.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textBox_Leistung_el.Location = new System.Drawing.Point(137, 110);
            textBox_Leistung_el.Margin = new System.Windows.Forms.Padding(5);
            textBox_Leistung_el.Name = "textBox_Leistung_el";
            textBox_Leistung_el.Size = new System.Drawing.Size(65, 25);
            textBox_Leistung_el.TabIndex = 55;
            // 
            // Label14
            // 
            Label14.AutoSize = true;
            Label14.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label14.Location = new System.Drawing.Point(7, 109);
            Label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label14.Name = "Label14";
            Label14.Size = new System.Drawing.Size(124, 17);
            Label14.TabIndex = 56;
            Label14.Text = "elektrische Leistung:";
            Label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label15
            // 
            Label15.AutoSize = true;
            Label15.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label15.Location = new System.Drawing.Point(64, 53);
            Label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label15.Name = "Label15";
            Label15.Size = new System.Drawing.Size(67, 17);
            Label15.TabIndex = 57;
            Label15.Text = "Hersteller:";
            Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label16
            // 
            Label16.AutoSize = true;
            Label16.BackColor = System.Drawing.Color.Black;
            Label16.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label16.ForeColor = System.Drawing.Color.White;
            Label16.Location = new System.Drawing.Point(204, 114);
            Label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label16.Name = "Label16";
            Label16.Size = new System.Drawing.Size(35, 17);
            Label16.TabIndex = 58;
            Label16.Text = "kWel";
            Label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label11
            // 
            Label11.AutoSize = true;
            Label11.BackColor = System.Drawing.Color.Black;
            Label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Label11.ForeColor = System.Drawing.Color.White;
            Label11.Location = new System.Drawing.Point(204, 85);
            Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label11.Name = "Label11";
            Label11.Size = new System.Drawing.Size(37, 17);
            Label11.TabIndex = 59;
            Label11.Text = "kWth";
            Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new System.Drawing.Point(12, 25);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new System.Drawing.Size(403, 369);
            dataGridView1.TabIndex = 75;
            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label3.Location = new System.Drawing.Point(13, 5);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(136, 17);
            label3.TabIndex = 79;
            label3.Text = "Module in Datenbank:";
            label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_BHKWAdmin
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new System.Drawing.Size(856, 517);
            Controls.Add(label3);
            Controls.Add(dataGridView1);
            Controls.Add(groupBox2);
            Controls.Add(Label5);
            Controls.Add(Label23);
            Controls.Add(comboBox_Brennstoff);
            Controls.Add(comboBox_Leistung);
            Controls.Add(btn_DBBHKW_Edit);
            Controls.Add(btn_DBBHKW_Neu);
            Controls.Add(btn_DBBHKW_Löschen);
            Controls.Add(btn_OK);
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "Form_BHKWAdmin";
            Text = "BHKW Verwaltung";
            Load += Form_BHKWEing_Load;
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

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