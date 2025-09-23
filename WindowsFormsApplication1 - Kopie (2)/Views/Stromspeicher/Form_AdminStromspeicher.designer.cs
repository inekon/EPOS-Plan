namespace WindowsFormsApplication1
{
    partial class Form_AdminStromspeicher
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.listBox_Stromspeicher = new System.Windows.Forms.ListBox();
            this.textBox_Bezeichner = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Energie = new System.Windows.Forms.TextBox();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Ladezustand = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Degradation = new System.Windows.Forms.TextBox();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Beenden
            // 
            this.btn_Beenden.Location = new System.Drawing.Point(817, 542);
            this.btn_Beenden.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(87, 30);
            this.btn_Beenden.TabIndex = 0;
            this.btn_Beenden.Text = "OK";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // listBox_Stromspeicher
            // 
            this.listBox_Stromspeicher.FormattingEnabled = true;
            this.listBox_Stromspeicher.ItemHeight = 17;
            this.listBox_Stromspeicher.Location = new System.Drawing.Point(22, 22);
            this.listBox_Stromspeicher.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Stromspeicher.Name = "listBox_Stromspeicher";
            this.listBox_Stromspeicher.Size = new System.Drawing.Size(179, 259);
            this.listBox_Stromspeicher.TabIndex = 2;
            this.listBox_Stromspeicher.SelectedIndexChanged += new System.EventHandler(this.listBox_Stromspeicher_SelectedIndexChanged);
            // 
            // textBox_Bezeichner
            // 
            this.textBox_Bezeichner.Enabled = false;
            this.textBox_Bezeichner.Location = new System.Drawing.Point(336, 56);
            this.textBox_Bezeichner.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Bezeichner.Name = "textBox_Bezeichner";
            this.textBox_Bezeichner.Size = new System.Drawing.Size(116, 25);
            this.textBox_Bezeichner.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(243, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 19);
            this.label1.TabIndex = 4;
            this.label1.Text = "Bezeichner";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(243, 161);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 19);
            this.label2.TabIndex = 6;
            this.label2.Text = "Energie";
            // 
            // textBox_Energie
            // 
            this.textBox_Energie.Location = new System.Drawing.Point(336, 152);
            this.textBox_Energie.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Energie.Name = "textBox_Energie";
            this.textBox_Energie.Size = new System.Drawing.Size(116, 25);
            this.textBox_Energie.TabIndex = 5;
            this.textBox_Energie.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Energie_Validating);
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.Location = new System.Drawing.Point(336, 88);
            this.textBox_Typ.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(116, 25);
            this.textBox_Typ.TabIndex = 5;
            this.textBox_Typ.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Typ_Validating);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(243, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 19);
            this.label3.TabIndex = 6;
            this.label3.Text = "Typ";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(243, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 19);
            this.label4.TabIndex = 8;
            this.label4.Text = "Leistung";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Location = new System.Drawing.Point(336, 119);
            this.textBox_Leistung.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(116, 25);
            this.textBox_Leistung.TabIndex = 7;
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(243, 188);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 19);
            this.label5.TabIndex = 10;
            this.label5.Text = "Ladezustand";
            // 
            // textBox_Ladezustand
            // 
            this.textBox_Ladezustand.Location = new System.Drawing.Point(336, 185);
            this.textBox_Ladezustand.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Ladezustand.Name = "textBox_Ladezustand";
            this.textBox_Ladezustand.Size = new System.Drawing.Size(116, 25);
            this.textBox_Ladezustand.TabIndex = 9;
            this.textBox_Ladezustand.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Ladezustand_Validating);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(243, 220);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 19);
            this.label6.TabIndex = 12;
            this.label6.Text = "Degradation";
            // 
            // textBox_Degradation
            // 
            this.textBox_Degradation.Location = new System.Drawing.Point(336, 216);
            this.textBox_Degradation.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Degradation.Name = "textBox_Degradation";
            this.textBox_Degradation.Size = new System.Drawing.Size(116, 25);
            this.textBox_Degradation.TabIndex = 11;
            this.textBox_Degradation.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Degradation_Validating);
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Speichern.Location = new System.Drawing.Point(265, 324);
            this.btn_Speichern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(119, 30);
            this.btn_Speichern.TabIndex = 13;
            this.btn_Speichern.Text = "Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Neu
            // 
            this.btn_Neu.Location = new System.Drawing.Point(247, 251);
            this.btn_Neu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(205, 30);
            this.btn_Neu.TabIndex = 14;
            this.btn_Neu.Text = "Neu...";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(423, 324);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(87, 30);
            this.btn_OK.TabIndex = 15;
            this.btn_OK.Text = "Beenden";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Location = new System.Drawing.Point(22, 324);
            this.btn_Loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(106, 30);
            this.btn_Loeschen.TabIndex = 16;
            this.btn_Loeschen.Text = "Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // Form_AdminStromspeicher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 367);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Degradation);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Ladezustand);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Leistung);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.textBox_Energie);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Bezeichner);
            this.Controls.Add(this.listBox_Stromspeicher);
            this.Controls.Add(this.btn_Beenden);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_AdminStromspeicher";
            this.Text = "Stromspeicher";
            this.Load += new System.EventHandler(this.Form_Stromspeicher_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.ListBox listBox_Stromspeicher;
        private System.Windows.Forms.TextBox textBox_Bezeichner;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Energie;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Ladezustand;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Degradation;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Loeschen;
    }
}