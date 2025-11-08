namespace WindowsFormsApplication1
{
    partial class Form_Sp
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
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.textBox_Energie = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_Degradation = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Ladezustand = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.listBox_SP_DB = new System.Windows.Forms.ListBox();
            this.listBox_SP = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_Name
            // 
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(136, 218);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(177, 25);
            this.textBox_Name.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(44, 221);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 19);
            this.label3.TabIndex = 11;
            this.label3.Text = "Name";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.Location = new System.Drawing.Point(44, 251);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(30, 19);
            this.label4.TabIndex = 13;
            this.label4.Text = "Typ";
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.Enabled = false;
            this.textBox_Typ.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Typ.Location = new System.Drawing.Point(136, 248);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(177, 25);
            this.textBox_Typ.TabIndex = 12;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.Location = new System.Drawing.Point(44, 282);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 19);
            this.label5.TabIndex = 15;
            this.label5.Text = "Leistung";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Enabled = false;
            this.textBox_Leistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Leistung.Location = new System.Drawing.Point(136, 279);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Leistung.TabIndex = 14;
            this.textBox_Leistung.Text = "10";
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // textBox_Energie
            // 
            this.textBox_Energie.Enabled = false;
            this.textBox_Energie.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Energie.Location = new System.Drawing.Point(136, 312);
            this.textBox_Energie.Name = "textBox_Energie";
            this.textBox_Energie.Size = new System.Drawing.Size(117, 25);
            this.textBox_Energie.TabIndex = 16;
            this.textBox_Energie.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Energie_Validating);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.Location = new System.Drawing.Point(44, 315);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 19);
            this.label7.TabIndex = 19;
            this.label7.Text = "Energie";
            // 
            // textBox_Degradation
            // 
            this.textBox_Degradation.Enabled = false;
            this.textBox_Degradation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Degradation.Location = new System.Drawing.Point(136, 342);
            this.textBox_Degradation.Name = "textBox_Degradation";
            this.textBox_Degradation.Size = new System.Drawing.Size(58, 25);
            this.textBox_Degradation.TabIndex = 18;
            this.textBox_Degradation.Text = "0,1";
            this.textBox_Degradation.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Degradation_Validating);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label8.Location = new System.Drawing.Point(44, 376);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(86, 19);
            this.label8.TabIndex = 21;
            this.label8.Text = "Ladezustand";
            // 
            // textBox_Ladezustand
            // 
            this.textBox_Ladezustand.Enabled = false;
            this.textBox_Ladezustand.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Ladezustand.Location = new System.Drawing.Point(136, 373);
            this.textBox_Ladezustand.Name = "textBox_Ladezustand";
            this.textBox_Ladezustand.Size = new System.Drawing.Size(58, 25);
            this.textBox_Ladezustand.TabIndex = 20;
            this.textBox_Ladezustand.Text = "98";
            this.textBox_Ladezustand.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Ladezustand_Validating);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.Location = new System.Drawing.Point(46, 345);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 19);
            this.label6.TabIndex = 22;
            this.label6.Text = "Degradation";
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.Color.Black;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(200, 281);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(33, 19);
            this.label17.TabIndex = 59;
            this.label17.Text = "kW";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Black;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(200, 376);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(23, 19);
            this.label9.TabIndex = 60;
            this.label9.Text = "%";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Black;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(200, 345);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 19);
            this.label10.TabIndex = 61;
            this.label10.Text = "%/a";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(361, 376);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 62;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(466, 376);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(77, 31);
            this.btn_OK.TabIndex = 63;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(354, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(192, 19);
            this.label2.TabIndex = 69;
            this.label2.Text = "Stromspeicher aus Datenbank";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label1.Location = new System.Drawing.Point(33, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 19);
            this.label1.TabIndex = 68;
            this.label1.Text = "ausgewählte Stromspeicher";
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(252, 107);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Entfernen.TabIndex = 67;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(252, 72);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Hinzu.TabIndex = 66;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // listBox_SP_DB
            // 
            this.listBox_SP_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP_DB.FormattingEnabled = true;
            this.listBox_SP_DB.ItemHeight = 17;
            this.listBox_SP_DB.Location = new System.Drawing.Point(357, 45);
            this.listBox_SP_DB.Name = "listBox_SP_DB";
            this.listBox_SP_DB.Size = new System.Drawing.Size(186, 157);
            this.listBox_SP_DB.TabIndex = 65;
            this.listBox_SP_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_DB_SelectedIndexChanged);
            // 
            // listBox_SP
            // 
            this.listBox_SP.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP.FormattingEnabled = true;
            this.listBox_SP.ItemHeight = 17;
            this.listBox_SP.Location = new System.Drawing.Point(37, 45);
            this.listBox_SP.Name = "listBox_SP";
            this.listBox_SP.Size = new System.Drawing.Size(186, 157);
            this.listBox_SP.TabIndex = 70;
            this.listBox_SP.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.button1.Location = new System.Drawing.Point(375, 221);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(77, 31);
            this.button1.TabIndex = 71;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // Form_Sp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(562, 418);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listBox_SP);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.listBox_SP_DB);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBox_Ladezustand);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.textBox_Degradation);
            this.Controls.Add(this.textBox_Energie);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Leistung);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Name);
            this.Name = "Form_Sp";
            this.Text = "Stromspeicher";
            this.Load += new System.EventHandler(this.Form_Sp_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.TextBox textBox_Energie;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_Degradation;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Ladezustand;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.ListBox listBox_SP_DB;
        private System.Windows.Forms.ListBox listBox_SP;
        private System.Windows.Forms.Button button1;
    }
}