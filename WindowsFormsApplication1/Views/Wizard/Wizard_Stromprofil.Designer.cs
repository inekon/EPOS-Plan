namespace WindowsFormsApplication1
{
    partial class Wizard_Stromprofil 
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
            this.btn_Bearbeiten = new System.Windows.Forms.Button();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Stromtyp = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.Label18 = new System.Windows.Forms.Label();
            this.textBox_StromSumme = new System.Windows.Forms.TextBox();
            this.Label19 = new System.Windows.Forms.Label();
            this.listBox_Strom_Auswahl = new System.Windows.Forms.ListBox();
            this.btn_Delete = new System.Windows.Forms.Button();
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
            this.label_Type.Size = new System.Drawing.Size(700, 37);
            this.label_Type.TabIndex = 2;
            this.label_Type.Text = "Standard Stromprofil";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Bearbeiten
            // 
            this.btn_Bearbeiten.Location = new System.Drawing.Point(323, 86);
            this.btn_Bearbeiten.Name = "btn_Bearbeiten";
            this.btn_Bearbeiten.Size = new System.Drawing.Size(144, 29);
            this.btn_Bearbeiten.TabIndex = 3;
            this.btn_Bearbeiten.Text = "Hinzufügen/Bearbeiten...";
            this.btn_Bearbeiten.UseVisualStyleBackColor = true;
            this.btn_Bearbeiten.Click += new System.EventHandler(this.btn_Bearbeiten_Click);
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(41, 52);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(267, 19);
            this.Label1.TabIndex = 53;
            this.Label1.Text = "Ausgewählte Stromverbraucher im Projekt";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label12
            // 
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label12.Location = new System.Drawing.Point(34, 321);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(90, 52);
            this.Label12.TabIndex = 42;
            this.Label12.Text = "jährlicher Strombedarf:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label10.Location = new System.Drawing.Point(76, 264);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 19);
            this.Label10.TabIndex = 43;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Enabled = false;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Name.Location = new System.Drawing.Point(132, 266);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.Size = new System.Drawing.Size(345, 22);
            this.textBox_Name.TabIndex = 44;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label13.Location = new System.Drawing.Point(30, 379);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(94, 19);
            this.Label13.TabIndex = 45;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Enabled = false;
            this.textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(132, 337);
            this.textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(63, 22);
            this.textBox_Jahres_Verbrauch.TabIndex = 46;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Enabled = false;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(132, 381);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(345, 57);
            this.textBox_Beschreibung.TabIndex = 47;
            // 
            // textBox_Stromtyp
            // 
            this.textBox_Stromtyp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stromtyp.Enabled = false;
            this.textBox_Stromtyp.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_Stromtyp.Location = new System.Drawing.Point(132, 298);
            this.textBox_Stromtyp.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Stromtyp.Name = "textBox_Stromtyp";
            this.textBox_Stromtyp.Size = new System.Drawing.Size(176, 22);
            this.textBox_Stromtyp.TabIndex = 48;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.Location = new System.Drawing.Point(90, 296);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(33, 19);
            this.Label15.TabIndex = 49;
            this.Label15.Text = "Typ:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.BackColor = System.Drawing.Color.Black;
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Location = new System.Drawing.Point(203, 338);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(48, 19);
            this.Label11.TabIndex = 50;
            this.Label11.Text = "MWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label18
            // 
            this.Label18.AutoSize = true;
            this.Label18.BackColor = System.Drawing.Color.Black;
            this.Label18.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label18.ForeColor = System.Drawing.Color.White;
            this.Label18.Location = new System.Drawing.Point(429, 459);
            this.Label18.Name = "Label18";
            this.Label18.Size = new System.Drawing.Size(48, 19);
            this.Label18.TabIndex = 51;
            this.Label18.Text = "MW/h";
            this.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_StromSumme
            // 
            this.textBox_StromSumme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_StromSumme.Enabled = false;
            this.textBox_StromSumme.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_StromSumme.Location = new System.Drawing.Point(342, 458);
            this.textBox_StromSumme.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_StromSumme.Name = "textBox_StromSumme";
            this.textBox_StromSumme.Size = new System.Drawing.Size(80, 22);
            this.textBox_StromSumme.TabIndex = 52;
            // 
            // Label19
            // 
            this.Label19.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label19.Location = new System.Drawing.Point(41, 450);
            this.Label19.Name = "Label19";
            this.Label19.Size = new System.Drawing.Size(301, 36);
            this.Label19.TabIndex = 55;
            this.Label19.Text = "Summe aller ausgewählten Strombverbraucher:";
            this.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_Strom_Auswahl
            // 
            this.listBox_Strom_Auswahl.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox_Strom_Auswahl.FormattingEnabled = true;
            this.listBox_Strom_Auswahl.ItemHeight = 17;
            this.listBox_Strom_Auswahl.Location = new System.Drawing.Point(41, 74);
            this.listBox_Strom_Auswahl.Name = "listBox_Strom_Auswahl";
            this.listBox_Strom_Auswahl.Size = new System.Drawing.Size(264, 157);
            this.listBox_Strom_Auswahl.TabIndex = 56;
            this.listBox_Strom_Auswahl.SelectedIndexChanged += new System.EventHandler(this.listBox_Strom_Auswahl_SelectedIndexChanged);
            // 
            // btn_Delete
            // 
            this.btn_Delete.Location = new System.Drawing.Point(323, 121);
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.Size = new System.Drawing.Size(144, 29);
            this.btn_Delete.TabIndex = 57;
            this.btn_Delete.Text = "Löschen";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btn_Delete_Click);
            // 
            // Wizard_Stromprofil
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(700, 495);
            this.Controls.Add(this.btn_Delete);
            this.Controls.Add(this.listBox_Strom_Auswahl);
            this.Controls.Add(this.Label19);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Stromtyp);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.Label18);
            this.Controls.Add(this.textBox_StromSumme);
            this.Controls.Add(this.btn_Bearbeiten);
            this.Controls.Add(this.label_Type);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_Stromprofil";
            this.Text = "Photovoltaik";
            this.Load += new System.EventHandler(this.Wizard_Stromprofil_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.Button btn_Bearbeiten;
        private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label Label13;
        private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Stromtyp;
        private System.Windows.Forms.Label Label15;
        private System.Windows.Forms.Label Label11;
        private System.Windows.Forms.Label Label18;
        private System.Windows.Forms.TextBox textBox_StromSumme;
        private System.Windows.Forms.Label Label19;
        private System.Windows.Forms.ListBox listBox_Strom_Auswahl;
        private System.Windows.Forms.Button btn_Delete;
    }
}