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
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
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
            this.listBox_Auswahl.Location = new System.Drawing.Point(27, 95);
            this.listBox_Auswahl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Auswahl.Name = "listBox_Auswahl";
            this.listBox_Auswahl.Size = new System.Drawing.Size(341, 174);
            this.listBox_Auswahl.TabIndex = 1;
            // 
            // btn_Hinzufuegen
            // 
            this.btn_Hinzufuegen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Hinzufuegen.Location = new System.Drawing.Point(374, 140);
            this.btn_Hinzufuegen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
            this.btn_Hinzufuegen.Size = new System.Drawing.Size(90, 31);
            this.btn_Hinzufuegen.TabIndex = 2;
            this.btn_Hinzufuegen.Text = "<--";
            this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
            this.btn_Hinzufuegen.Click += new System.EventHandler(this.btn_Hinzu_Click);
            // 
            // btn_Entfernen
            // 
            this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Entfernen.Location = new System.Drawing.Point(374, 179);
            this.btn_Entfernen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Entfernen.Name = "btn_Entfernen";
            this.btn_Entfernen.Size = new System.Drawing.Size(90, 31);
            this.btn_Entfernen.TabIndex = 3;
            this.btn_Entfernen.Text = "-->";
            this.btn_Entfernen.UseVisualStyleBackColor = true;
            this.btn_Entfernen.Click += new System.EventHandler(this.btn_Entfernen_Click);
            // 
            // listBox_Extern
            // 
            this.listBox_Extern.HorizontalScrollbar = true;
            this.listBox_Extern.ItemHeight = 17;
            this.listBox_Extern.Location = new System.Drawing.Point(470, 95);
            this.listBox_Extern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Extern.Name = "listBox_Extern";
            this.listBox_Extern.Size = new System.Drawing.Size(341, 174);
            this.listBox_Extern.TabIndex = 4;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(469, 72);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(138, 19);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Wärmebedarf aus DB";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(625, 300);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(158, 30);
            this.btn_Bearbeiten.TabIndex = 15;
            this.btn_Bearbeiten.Text = "Gamglinie einlesen...";
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
            this.label_Type.Size = new System.Drawing.Size(828, 37);
            this.label_Type.TabIndex = 16;
            this.label_Type.Text = "Wärmebedarfsdaten (Ganglinien)";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(593, 398);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(98, 31);
            this.btn_Abbrechen.TabIndex = 18;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(713, 399);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(98, 31);
            this.btn_OK.TabIndex = 17;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Location = new System.Drawing.Point(625, 336);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(158, 31);
            this.btn_Loeschen.TabIndex = 20;
            this.btn_Loeschen.Text = "Ganglinie Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // Wizard_Waermebedarf
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(828, 443);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.listBox_Auswahl);
            this.Controls.Add(this.btn_Hinzufuegen);
            this.Controls.Add(this.btn_Entfernen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
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
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Loeschen;
    }
}