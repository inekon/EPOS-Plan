namespace WindowsFormsApplication1
{
    partial class Wizard_Prozess 
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
            this.label_Type = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.listBox_SP_DB = new System.Windows.Forms.ListBox();
            this.listBox_SP = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(641, 37);
            this.label_Type.TabIndex = 1;
            this.label_Type.Text = "Prozesswärme";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(59, 291);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 19);
            this.label3.TabIndex = 19;
            this.label3.Text = "Name:";
            // 
            // textBox_Name
            // 
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(110, 289);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(225, 25);
            this.textBox_Name.TabIndex = 18;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(360, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(158, 19);
            this.label2.TabIndex = 17;
            this.label2.Text = "Prozesse aus Datenbank";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label1.Location = new System.Drawing.Point(8, 73);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(142, 19);
            this.label1.TabIndex = 16;
            this.label1.Text = "ausgewählte Prozesse";
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(268, 157);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Entfernen.TabIndex = 15;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(268, 122);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Hinzu.TabIndex = 14;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // listBox_SP_DB
            // 
            this.listBox_SP_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP_DB.FormattingEnabled = true;
            this.listBox_SP_DB.ItemHeight = 17;
            this.listBox_SP_DB.Location = new System.Drawing.Point(364, 95);
            this.listBox_SP_DB.Name = "listBox_SP_DB";
            this.listBox_SP_DB.Size = new System.Drawing.Size(236, 157);
            this.listBox_SP_DB.TabIndex = 13;
            this.listBox_SP_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_DB_SelectedIndexChanged);
            // 
            // listBox_SP
            // 
            this.listBox_SP.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP.FormattingEnabled = true;
            this.listBox_SP.ItemHeight = 17;
            this.listBox_SP.Location = new System.Drawing.Point(12, 95);
            this.listBox_SP.Name = "listBox_SP";
            this.listBox_SP.Size = new System.Drawing.Size(236, 157);
            this.listBox_SP.TabIndex = 12;
            this.listBox_SP.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.Location = new System.Drawing.Point(74, 330);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 19);
            this.label4.TabIndex = 21;
            this.label4.Text = "Typ:";
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Typ.Location = new System.Drawing.Point(110, 329);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(225, 25);
            this.textBox_Typ.TabIndex = 20;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.Location = new System.Drawing.Point(13, 371);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 19);
            this.label5.TabIndex = 23;
            this.label5.Text = "Beschreibung:";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(110, 377);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(490, 85);
            this.textBox_Beschreibung.TabIndex = 22;
            // 
            // Wizard_Prozess
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(641, 474);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.listBox_SP_DB);
            this.Controls.Add(this.listBox_SP);
            this.Controls.Add(this.label_Type);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Prozess";
            this.Text = "frm1";
            this.Load += new System.EventHandler(this.Wizard_Prozess_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.ListBox listBox_SP_DB;
        private System.Windows.Forms.ListBox listBox_SP;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
    }
}