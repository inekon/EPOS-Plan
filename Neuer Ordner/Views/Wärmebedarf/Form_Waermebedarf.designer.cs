namespace WindowsFormsApplication1
{
    partial class Form_Waermebedarf
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
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(19, 13);
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
            this.listBox_Auswahl.Location = new System.Drawing.Point(23, 36);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(249, 174);
            this.listBox_Auswahl.TabIndex = 1;
            this.listBox_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Auswahl_SelectedIndexChanged);
            // 
            // btn_Hinzufuegen
            // 
            this.btn_Hinzufuegen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzufuegen.Location = new System.Drawing.Point(300, 70);
            this.btn_Hinzufuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.Size = new System.Drawing.Size(72, 31);
            this.btn_Hinzufuegen.TabIndex = 2;
            this.btn_Hinzufuegen.Text = "<--";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzufuegen_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(300, 117);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(72, 31);
            this.btn_Entfernen.TabIndex = 3;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // listBox_Extern
            // 
            this.listBox_Extern.HorizontalScrollbar = true;
            this.listBox_Extern.ItemHeight = 17;
            this.listBox_Extern.Location = new System.Drawing.Point(401, 36);
            this.listBox_Extern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Extern.Name = "listBox_Extern";
            this.listBox_Extern.Size = new System.Drawing.Size(264, 174);
            this.listBox_Extern.TabIndex = 4;
            this.listBox_Extern.SelectedIndexChanged += new System.EventHandler(this.listBox_Extern_SelectedIndexChanged);
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(397, 13);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(139, 19);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Wärmebedarf aus DB";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Hilfe
            // 
            this.btn_Hilfe.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hilfe.Location = new System.Drawing.Point(23, 304);
            this.btn_Hilfe.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.Size = new System.Drawing.Size(98, 31);
            this.btn_Hilfe.TabIndex = 6;
            this.btn_Hilfe.Text = "Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(567, 304);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(98, 31);
            this.btn_OK.TabIndex = 7;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label3.Location = new System.Drawing.Point(22, 242);
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
            this.textBox_Name.Location = new System.Drawing.Point(78, 242);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(406, 25);
            this.textBox_Name.TabIndex = 9;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(447, 303);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(98, 31);
            this.btn_Abbrechen.TabIndex = 14;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // Form_Waermebedarf
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 345);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Name);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Waermebedarf";
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
private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Button btn_Abbrechen;


 
    }
}