namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme_Admin
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
            this.btn_Prozess_DBneu = new System.Windows.Forms.Button();
            this.btn_Prozess_loeschen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_ProzTypeDBedit = new System.Windows.Forms.Button();
            this.btn_Prozess_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Prozess_Name = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Prozess_Type = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.listBox_Prozess_DB = new System.Windows.Forms.ListBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Prozess_DBneu
            // 
            this.btn_Prozess_DBneu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_DBneu.Location = new System.Drawing.Point(338, 95);
            this.btn_Prozess_DBneu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_DBneu.Name = "btn_Prozess_DBneu";
            this.btn_Prozess_DBneu.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_DBneu.TabIndex = 3;
            this.btn_Prozess_DBneu.Text = "Prozess in DB neu";
            this.btn_Prozess_DBneu.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBneu.Click += new System.EventHandler(this.btn_Prozess_DBneu_Click);
            // 
            // btn_Prozess_loeschen
            // 
            this.btn_Prozess_loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_loeschen.Location = new System.Drawing.Point(338, 134);
            this.btn_Prozess_loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_loeschen.Name = "btn_Prozess_loeschen";
            this.btn_Prozess_loeschen.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_loeschen.TabIndex = 4;
            this.btn_Prozess_loeschen.Text = "Prozess in DB löschen";
            this.btn_Prozess_loeschen.UseVisualStyleBackColor = true;
            this.btn_Prozess_loeschen.Click += new System.EventHandler(this.btn_Prozess_loeschen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(411, 445);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(86, 31);
            this.btn_OK.TabIndex = 10;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label24.Location = new System.Drawing.Point(24, 26);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(167, 19);
            this.Label24.TabIndex = 12;
            this.Label24.Text = "Datenbank Prozesswärme";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_ProzTypeDBedit
            // 
            this.btn_ProzTypeDBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_ProzTypeDBedit.Location = new System.Drawing.Point(338, 173);
            this.btn_ProzTypeDBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_ProzTypeDBedit.Name = "btn_ProzTypeDBedit";
            this.btn_ProzTypeDBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_ProzTypeDBedit.TabIndex = 13;
            this.btn_ProzTypeDBedit.Text = "Typ in DB ändern";
            this.btn_ProzTypeDBedit.UseVisualStyleBackColor = true;
            this.btn_ProzTypeDBedit.Click += new System.EventHandler(this.btn_ProzTypeDBedit_Click);
            // 
            // btn_Prozess_DBedit
            // 
            this.btn_Prozess_DBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Prozess_DBedit.Location = new System.Drawing.Point(338, 56);
            this.btn_Prozess_DBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Prozess_DBedit.Name = "btn_Prozess_DBedit";
            this.btn_Prozess_DBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_Prozess_DBedit.TabIndex = 15;
            this.btn_Prozess_DBedit.Text = "Prozess in DB ändern";
            this.btn_Prozess_DBedit.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBedit.Click += new System.EventHandler(this.btn_Prozess_DBedit_Click);
            // 
            // Label12
            // 
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label12.Location = new System.Drawing.Point(33, 392);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(201, 22);
            this.Label12.TabIndex = 19;
            this.Label12.Text = "jährlicher Prozesswärmebedarf:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label10.Location = new System.Drawing.Point(57, 239);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 19);
            this.Label10.TabIndex = 20;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Prozess_Name
            // 
            this.textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Prozess_Name.Location = new System.Drawing.Point(113, 241);
            this.textBox_Prozess_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            this.textBox_Prozess_Name.ReadOnly = true;
            this.textBox_Prozess_Name.Size = new System.Drawing.Size(331, 25);
            this.textBox_Prozess_Name.TabIndex = 21;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label13.Location = new System.Drawing.Point(11, 303);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(94, 19);
            this.Label13.TabIndex = 22;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(242, 392);
            this.textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            this.textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(84, 25);
            this.textBox_Jahres_Verbrauch.TabIndex = 23;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(113, 305);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(336, 57);
            this.textBox_Beschreibung.TabIndex = 24;
            // 
            // textBox_Prozess_Type
            // 
            this.textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Type.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Prozess_Type.Location = new System.Drawing.Point(113, 273);
            this.textBox_Prozess_Type.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            this.textBox_Prozess_Type.ReadOnly = true;
            this.textBox_Prozess_Type.Size = new System.Drawing.Size(156, 25);
            this.textBox_Prozess_Type.TabIndex = 25;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.Location = new System.Drawing.Point(71, 271);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(34, 19);
            this.Label15.TabIndex = 26;
            this.Label15.Text = "Typ:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Location = new System.Drawing.Point(334, 394);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(48, 19);
            this.Label11.TabIndex = 27;
            this.Label11.Text = "MWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox_Prozess_DB
            // 
            this.listBox_Prozess_DB.FormattingEnabled = true;
            this.listBox_Prozess_DB.ItemHeight = 17;
            this.listBox_Prozess_DB.Location = new System.Drawing.Point(28, 48);
            this.listBox_Prozess_DB.Name = "listBox_Prozess_DB";
            this.listBox_Prozess_DB.Size = new System.Drawing.Size(288, 174);
            this.listBox_Prozess_DB.TabIndex = 32;
            this.listBox_Prozess_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(314, 445);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 33;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.Location = new System.Drawing.Point(161, 445);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(91, 31);
            this.btn_Simulation.TabIndex = 34;
            this.btn_Simulation.Text = "Grafik";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // Form_Prozesswaerme_Admin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(521, 489);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.listBox_Prozess_DB);
            this.Controls.Add(this.btn_Prozess_DBneu);
            this.Controls.Add(this.btn_Prozess_loeschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_ProzTypeDBedit);
            this.Controls.Add(this.btn_Prozess_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Prozess_Name);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Prozess_Type);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Prozesswaerme_Admin";
            this.Text = "Prozesswärme Verwaltung";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Prozess_DBneu;
        private System.Windows.Forms.Button btn_Prozess_loeschen;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label24;
private System.Windows.Forms.Button btn_ProzTypeDBedit;
private System.Windows.Forms.Button btn_Prozess_DBedit;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Prozess_Name;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Prozess_Type;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.ListBox listBox_Prozess_DB;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.Button btn_Simulation;


 
    }
}