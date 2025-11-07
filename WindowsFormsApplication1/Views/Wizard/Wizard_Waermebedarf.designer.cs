namespace WindowsFormsApplication1
{
    partial class Wizard_Waermebedarf 
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
            this.Label1 = new System.Windows.Forms.Label();
            this.listBox_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Hinzufuegen = new System.Windows.Forms.Button();
            this.btn_Entfernen = new System.Windows.Forms.Button();
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(23, 70);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(146, 19);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Ausgewählt im Projekt";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_Auswahl
            // 
            this.listBox_Auswahl.HorizontalScrollbar = true;
            this.listBox_Auswahl.ItemHeight = 17;
            this.listBox_Auswahl.Location = new System.Drawing.Point(23, 93);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(271, 174);
            this.listBox_Auswahl.TabIndex = 1;
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // btn_Hinzufuegen
            // 
            this.btn_Hinzufuegen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzufuegen.Location = new System.Drawing.Point(300, 138);
            this.btn_Hinzufuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.Size = new System.Drawing.Size(63, 31);
            this.btn_Hinzufuegen.TabIndex = 2;
            this.btn_Hinzufuegen.Text = "<--";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(300, 177);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(63, 31);
            this.btn_Entfernen.TabIndex = 3;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // listBox_Extern
            // 
            this.listBox_Extern.HorizontalScrollbar = true;
            this.listBox_Extern.ItemHeight = 17;
            this.listBox_Extern.Location = new System.Drawing.Point(369, 93);
            this.listBox_Extern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Extern.Name = "listBox_Extern";
            this.listBox_Extern.Size = new System.Drawing.Size(287, 174);
            this.listBox_Extern.TabIndex = 4;
            this.listBox_Extern.SelectedIndexChanged += new System.EventHandler(this.listBox_Extern_SelectedIndexChanged);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(368, 70);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(138, 19);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Wärmebedarf aus DB";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label3.Location = new System.Drawing.Point(19, 281);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(48, 19);
            this.Label3.TabIndex = 8;
            this.Label3.Text = "Name:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BackColor = System.Drawing.Color.White;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(73, 282);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(433, 25);
            this.textBox_Name.TabIndex = 9;
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(536, 279);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(120, 30);
            this.btn_Bearbeiten.TabIndex = 15;
            this.btn_Bearbeiten.Text = "DB Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(678, 37);
            this.label_Type.TabIndex = 16;
            this.label_Type.Text = "Wärmebedarfsdaten aus Datei";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Wizard_Waermebedarf
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 374);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Name);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Wizard_Waermebedarf";
            this.Text = "Wärmebedarf Extern";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox listBox_Auswahl;
private System.Windows.Forms.Button btn_Hinzufuegen;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Label label_Type;
    }
}