namespace WindowsFormsApplication1
{
    partial class Wizard_Stromlastgang 
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
            this.label_Type = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzufuegen = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
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
            this.label_Type.Size = new System.Drawing.Size(656, 37);
            this.label_Type.TabIndex = 2;
            this.label_Type.Text = "Stromlastgang";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(18, 86);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(146, 19);
            this.Label1.TabIndex = 15;
            this.Label1.Text = "Ausgewählt im Projekt";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_Auswahl
            // 
            this.listBox_Auswahl.HorizontalScrollbar = true;
            this.listBox_Auswahl.Location = new System.Drawing.Point(22, 109);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(249, 173);
            this.listBox_Auswahl.TabIndex = 16;
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_SelectedIndexChanged);
            // 
            // btn_Hinzufuegen
            // 
            this.btn_Hinzufuegen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzufuegen.Location = new System.Drawing.Point(287, 141);
            this.btn_Hinzufuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.Size = new System.Drawing.Size(72, 31);
            this.btn_Hinzufuegen.TabIndex = 17;
            this.btn_Hinzufuegen.Text = "<--";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(287, 188);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(72, 31);
            this.btn_Entfernen.TabIndex = 18;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // listBox_Extern
            // 
            this.listBox_Extern.HorizontalScrollbar = true;
            this.listBox_Extern.Location = new System.Drawing.Point(375, 109);
            this.listBox_Extern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Extern.Name = "listBox_Extern";
            this.listBox_Extern.Size = new System.Drawing.Size(264, 173);
            this.listBox_Extern.TabIndex = 19;
            this.listBox_Extern.SelectedIndexChanged += new System.EventHandler(this.listBox_SP_DB_SelectedIndexChanged);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(371, 86);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(145, 19);
            this.Label2.TabIndex = 20;
            this.Label2.Text = "Stromlastgang aus DB";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label3.Location = new System.Drawing.Point(22, 315);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(48, 19);
            this.Label3.TabIndex = 23;
            this.Label3.Text = "Name:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BackColor = System.Drawing.Color.White;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(78, 315);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(406, 25);
            this.textBox_Name.TabIndex = 24;
            // 
            // Wizard_Stromlastgang
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(656, 368);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.label_Type);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Stromlastgang";
            this.Text = "Stromlastgang";
            this.Load += new System.EventHandler(this.Wizard_Stromlastgang_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.ListBox listBox_Auswahl;
        private System.Windows.Forms.Button btn_Hinzufuegen;
        private System.Windows.Forms.Button btn_Entfernen;
        private System.Windows.Forms.ListBox listBox_Extern;
        private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.Label Label3;
        private System.Windows.Forms.TextBox textBox_Name;
    }
}