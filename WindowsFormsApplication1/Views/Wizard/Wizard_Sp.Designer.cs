namespace WindowsFormsApplication1
{
    partial class Wizard_Sp
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
            this.listBox_SP = new System.Windows.Forms.ListBox();
            this.listBox_SP_DB = new System.Windows.Forms.ListBox();
            this.btn_Hinzu = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
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
            this.label_Type = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBox_SP
            // 
            this.listBox_SP.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP.FormattingEnabled = true;
            this.listBox_SP.ItemHeight = 17;
            this.listBox_SP.Location = new System.Drawing.Point(25, 93);
            this.listBox_SP.Name = "listBox_SP";
            this.listBox_SP.Size = new System.Drawing.Size(225, 157);
            this.listBox_SP.TabIndex = 3;
            this.listBox_SP.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_SelectedIndexChanged);
            // 
            // listBox_SP_DB
            // 
            this.listBox_SP_DB.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listBox_SP_DB.FormattingEnabled = true;
            this.listBox_SP_DB.ItemHeight = 17;
            this.listBox_SP_DB.Location = new System.Drawing.Point(384, 93);
            this.listBox_SP_DB.Name = "listBox_SP_DB";
            this.listBox_SP_DB.Size = new System.Drawing.Size(221, 157);
            this.listBox_SP_DB.TabIndex = 4;
            // 
            // btn_Hinzu
            // 
            this.btn_Hinzu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzu.Location = new System.Drawing.Point(279, 132);
            this.btn_Hinzu.Name = "btn_Hinzu";
            this.btn_Hinzu.Size = new System.Drawing.Size(75, 29);
            this.btn_Hinzu.TabIndex = 5;
            this.btn_Hinzu.Text = "<---";
            this.btn_Hinzu.UseVisualStyleBackColor = true;
            this.btn_Hinzu.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(279, 167);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(75, 28);
            this.btn_Entfernen.TabIndex = 6;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label1.Location = new System.Drawing.Point(21, 71);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(176, 19);
            this.label1.TabIndex = 7;
            this.label1.Text = "ausgewählte Stromspeicher";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label2.Location = new System.Drawing.Point(389, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(192, 19);
            this.label2.TabIndex = 8;
            this.label2.Text = "Stromspeicher aus Datenbank";
            // 
            // textBox_Name
            // 
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(153, 277);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(270, 25);
            this.textBox_Name.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label3.Location = new System.Drawing.Point(92, 280);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 19);
            this.label3.TabIndex = 11;
            this.label3.Text = "Name:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label4.Location = new System.Drawing.Point(107, 310);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 19);
            this.label4.TabIndex = 13;
            this.label4.Text = "Typ:";
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Typ.Location = new System.Drawing.Point(153, 307);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(270, 25);
            this.textBox_Typ.TabIndex = 12;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label5.Location = new System.Drawing.Point(76, 341);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 19);
            this.label5.TabIndex = 15;
            this.label5.Text = "Leistung:";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Leistung.Location = new System.Drawing.Point(153, 338);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(58, 25);
            this.textBox_Leistung.TabIndex = 14;
            this.textBox_Leistung.Text = "10";
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // textBox_Energie
            // 
            this.textBox_Energie.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Energie.Location = new System.Drawing.Point(153, 371);
            this.textBox_Energie.Name = "textBox_Energie";
            this.textBox_Energie.Size = new System.Drawing.Size(117, 25);
            this.textBox_Energie.TabIndex = 16;
            this.textBox_Energie.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Energie_Validating);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.Location = new System.Drawing.Point(83, 374);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 19);
            this.label7.TabIndex = 19;
            this.label7.Text = "Energie:";
            // 
            // textBox_Degradation
            // 
            this.textBox_Degradation.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Degradation.Location = new System.Drawing.Point(153, 401);
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
            this.label8.Location = new System.Drawing.Point(51, 435);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(89, 19);
            this.label8.TabIndex = 21;
            this.label8.Text = "Ladezustand:";
            // 
            // textBox_Ladezustand
            // 
            this.textBox_Ladezustand.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Ladezustand.Location = new System.Drawing.Point(153, 432);
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
            this.label6.Location = new System.Drawing.Point(52, 404);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 19);
            this.label6.TabIndex = 22;
            this.label6.Text = "Degradation:";
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(656, 32);
            this.label_Type.TabIndex = 23;
            this.label_Type.Text = "Geben Sie Daten der Stromspeicher ein";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.Color.Black;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(217, 340);
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
            this.label9.Location = new System.Drawing.Point(217, 434);
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
            this.label10.Location = new System.Drawing.Point(217, 404);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 19);
            this.label10.TabIndex = 61;
            this.label10.Text = "%/a";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(508, 256);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(97, 31);
            this.btn_Bearbeiten.TabIndex = 72;
            this.btn_Bearbeiten.Text = "Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // Wizard_Sp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(656, 477);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label_Type);
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
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.btn_Hinzu);
            this.Controls.Add(this.listBox_SP_DB);
            this.Controls.Add(this.listBox_SP);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Sp";
            this.Text = "Stromspeicher";
            this.Load += new System.EventHandler(this.Wizard_Sp_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_SP;
        private System.Windows.Forms.ListBox listBox_SP_DB;
        private System.Windows.Forms.Button btn_Hinzu;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
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
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btn_Bearbeiten;
    }
}