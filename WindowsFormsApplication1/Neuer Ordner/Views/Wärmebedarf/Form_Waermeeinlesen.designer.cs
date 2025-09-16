namespace WindowsFormsApplication1
{
    partial class Form_Waermeeinlesen
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
            this.Label4 = new System.Windows.Forms.Label();
            this.textBox_Datum = new System.Windows.Forms.TextBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Oeffnen = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Ordner = new System.Windows.Forms.TextBox();
            this.btn_Kopieren = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
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
            this.btn_Hinzufuegen.Location = new System.Drawing.Point(294, 70);
            this.btn_Hinzufuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.Size = new System.Drawing.Size(85, 31);
            this.btn_Hinzufuegen.TabIndex = 2;
            this.btn_Hinzufuegen.Text = "<--";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzufuegen_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(294, 117);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(85, 31);
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
            this.btn_Hilfe.Location = new System.Drawing.Point(39, 457);
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
            this.btn_OK.Location = new System.Drawing.Point(583, 457);
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
            this.Label3.Location = new System.Drawing.Point(66, 301);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(78, 19);
            this.Label3.TabIndex = 8;
            this.Label3.Text = "Dateiname:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(152, 301);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(472, 25);
            this.textBox_Name.TabIndex = 9;
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label4.Location = new System.Drawing.Point(90, 337);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(54, 19);
            this.Label4.TabIndex = 10;
            this.Label4.Text = "Datum:";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Datum
            // 
            this.textBox_Datum.Enabled = false;
            this.textBox_Datum.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Datum.Location = new System.Drawing.Point(152, 336);
            this.textBox_Datum.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Datum.Name = "textBox_Datum";
            this.textBox_Datum.Size = new System.Drawing.Size(105, 25);
            this.textBox_Datum.TabIndex = 11;
            this.textBox_Datum.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Label5
            // 
            this.Label5.AutoSize = true;
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label5.Location = new System.Drawing.Point(50, 374);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(94, 19);
            this.Label5.TabIndex = 12;
            this.Label5.Text = "Beschreibung:";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(152, 374);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(472, 69);
            this.textBox_Beschreibung.TabIndex = 13;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(463, 456);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(98, 31);
            this.btn_Abbrechen.TabIndex = 14;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_Oeffnen
            // 
            this.btn_Oeffnen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Oeffnen.Location = new System.Drawing.Point(638, 303);
            this.btn_Oeffnen.Name = "btn_Oeffnen";
            this.btn_Oeffnen.Size = new System.Drawing.Size(28, 23);
            this.btn_Oeffnen.TabIndex = 15;
            this.btn_Oeffnen.Text = "...";
            this.btn_Oeffnen.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_Oeffnen.UseVisualStyleBackColor = true;
            this.btn_Oeffnen.Click += new System.EventHandler(this.btn_Oeffnen_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.Location = new System.Drawing.Point(18, 266);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(126, 19);
            this.label6.TabIndex = 16;
            this.label6.Text = "Datei Basis Ordner:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Ordner
            // 
            this.textBox_Ordner.Enabled = false;
            this.textBox_Ordner.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Ordner.Location = new System.Drawing.Point(152, 266);
            this.textBox_Ordner.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Ordner.Name = "textBox_Ordner";
            this.textBox_Ordner.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Ordner.Size = new System.Drawing.Size(472, 25);
            this.textBox_Ordner.TabIndex = 17;
            // 
            // btn_Kopieren
            // 
            this.btn_Kopieren.Location = new System.Drawing.Point(401, 217);
            this.btn_Kopieren.Name = "btn_Kopieren";
            this.btn_Kopieren.Size = new System.Drawing.Size(135, 31);
            this.btn_Kopieren.TabIndex = 18;
            this.btn_Kopieren.Text = "Datei hinzufügen...";
            this.btn_Kopieren.UseVisualStyleBackColor = true;
            this.btn_Kopieren.Click += new System.EventHandler(this.button1_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Location = new System.Drawing.Point(542, 217);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(124, 31);
            this.btn_Loeschen.TabIndex = 19;
            this.btn_Loeschen.Text = "Datei Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // Form_Waermeeinlesen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 504);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Kopieren);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Ordner);
            this.Controls.Add(this.btn_Oeffnen);
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
            this.Controls.Add(this.Label4);
            this.Controls.Add(this.textBox_Datum);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Waermeeinlesen";
            this.Text = "Wärmebedarf Extern";
            this.Load += new System.EventHandler(this.Form_Waermeeinlesen_Load);
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
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.TextBox textBox_Datum;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.Button btn_Oeffnen;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.TextBox textBox_Ordner;
private System.Windows.Forms.Button btn_Kopieren;
private System.Windows.Forms.Button btn_Loeschen;


 
    }
}