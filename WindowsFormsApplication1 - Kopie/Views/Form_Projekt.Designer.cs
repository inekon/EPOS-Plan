namespace WindowsFormsApplication1
{
    partial class Form_Projekt
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
            this.listBox_Projekte = new System.Windows.Forms.ListBox();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Aenderung = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Kunde = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Bearbeiter = new System.Windows.Forms.TextBox();
            this.button_Neu = new System.Windows.Forms.Button();
            this.button_Speichern = new System.Windows.Forms.Button();
            this.button_Loeschen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBox_Projekte
            // 
            this.listBox_Projekte.FormattingEnabled = true;
            this.listBox_Projekte.Location = new System.Drawing.Point(12, 12);
            this.listBox_Projekte.Name = "listBox_Projekte";
            this.listBox_Projekte.Size = new System.Drawing.Size(222, 381);
            this.listBox_Projekte.TabIndex = 0;
            this.listBox_Projekte.SelectedIndexChanged += new System.EventHandler(this.listBox_Projekte_SelectedIndexChanged);
            // 
            // textBox_Name
            // 
            this.textBox_Name.Location = new System.Drawing.Point(366, 23);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(267, 20);
            this.textBox_Name.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(267, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(267, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Letzte Änderung";
            // 
            // textBox_Aenderung
            // 
            this.textBox_Aenderung.Enabled = false;
            this.textBox_Aenderung.Location = new System.Drawing.Point(366, 62);
            this.textBox_Aenderung.Name = "textBox_Aenderung";
            this.textBox_Aenderung.Size = new System.Drawing.Size(113, 20);
            this.textBox_Aenderung.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(267, 107);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Beschreibung";
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Location = new System.Drawing.Point(366, 107);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(267, 97);
            this.textBox_Beschreibung.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(267, 231);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Kunde";
            // 
            // textBox_Kunde
            // 
            this.textBox_Kunde.Location = new System.Drawing.Point(366, 231);
            this.textBox_Kunde.Name = "textBox_Kunde";
            this.textBox_Kunde.Size = new System.Drawing.Size(267, 20);
            this.textBox_Kunde.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(267, 276);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Bearbeiter";
            // 
            // textBox_Bearbeiter
            // 
            this.textBox_Bearbeiter.Location = new System.Drawing.Point(366, 276);
            this.textBox_Bearbeiter.Name = "textBox_Bearbeiter";
            this.textBox_Bearbeiter.Size = new System.Drawing.Size(267, 20);
            this.textBox_Bearbeiter.TabIndex = 9;
            // 
            // button_Neu
            // 
            this.button_Neu.Location = new System.Drawing.Point(270, 317);
            this.button_Neu.Name = "button_Neu";
            this.button_Neu.Size = new System.Drawing.Size(93, 32);
            this.button_Neu.TabIndex = 11;
            this.button_Neu.Text = "Neu";
            this.button_Neu.UseVisualStyleBackColor = true;
            this.button_Neu.Click += new System.EventHandler(this.button_Neu_Click);
            // 
            // button_Speichern
            // 
            this.button_Speichern.Location = new System.Drawing.Point(386, 317);
            this.button_Speichern.Name = "button_Speichern";
            this.button_Speichern.Size = new System.Drawing.Size(93, 32);
            this.button_Speichern.TabIndex = 12;
            this.button_Speichern.Text = "Speichern";
            this.button_Speichern.UseVisualStyleBackColor = true;
            this.button_Speichern.Click += new System.EventHandler(this.button_Speichern_Click);
            // 
            // button_Loeschen
            // 
            this.button_Loeschen.Location = new System.Drawing.Point(12, 399);
            this.button_Loeschen.Name = "button_Loeschen";
            this.button_Loeschen.Size = new System.Drawing.Size(93, 33);
            this.button_Loeschen.TabIndex = 13;
            this.button_Loeschen.Text = "Löschen";
            this.button_Loeschen.UseVisualStyleBackColor = true;
            this.button_Loeschen.Click += new System.EventHandler(this.button_Loeschen_Click);
            // 
            // Form_Projekt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 443);
            this.Controls.Add(this.button_Loeschen);
            this.Controls.Add(this.button_Speichern);
            this.Controls.Add(this.button_Neu);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Bearbeiter);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Kunde);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Aenderung);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.listBox_Projekte);
            this.Name = "Form_Projekt";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Projekte";
            this.Load += new System.EventHandler(this.Form_Projekt_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_Projekte;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Aenderung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Kunde;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Bearbeiter;
        private System.Windows.Forms.Button button_Neu;
        private System.Windows.Forms.Button button_Speichern;
        private System.Windows.Forms.Button button_Loeschen;
    }
}