namespace WindowsFormsApplication1
{
    partial class Form_Brauchwasser_Admin 
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
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.listBox_DB = new System.Windows.Forms.ListBox();
            this.btn_DBneu = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label24 = new System.Windows.Forms.Label();
            this.btn_TypeDBedit = new System.Windows.Forms.Button();
            this.btn_DBedit = new System.Windows.Forms.Button();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Prozess_Name = new System.Windows.Forms.TextBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Prozess_Type = new System.Windows.Forms.TextBox();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_Simulation.Location = new System.Drawing.Point(165, 429);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(91, 31);
            this.btn_Simulation.TabIndex = 52;
            this.btn_Simulation.Text = "Grafik";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Abbrechen.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_Abbrechen.Location = new System.Drawing.Point(318, 429);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 51;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // listBox_DB
            // 
            this.listBox_DB.FormattingEnabled = true;
            this.listBox_DB.Location = new System.Drawing.Point(32, 32);
            this.listBox_DB.Name = "listBox_DB";
            this.listBox_DB.Size = new System.Drawing.Size(288, 173);
            this.listBox_DB.TabIndex = 50;
            this.listBox_DB.Click += new System.EventHandler(this.listBox_DB_Click);
            this.listBox_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_DB_SelectedIndexChanged);
            // 
            // btn_DBneu
            // 
            this.btn_DBneu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_DBneu.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_DBneu.Location = new System.Drawing.Point(342, 79);
            this.btn_DBneu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_DBneu.Name = "btn_DBneu";
            this.btn_DBneu.Size = new System.Drawing.Size(154, 31);
            this.btn_DBneu.TabIndex = 35;
            this.btn_DBneu.Text = "Neues Profil...";
            this.btn_DBneu.UseVisualStyleBackColor = true;
            this.btn_DBneu.Click += new System.EventHandler(this.btn_DBneu_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Loeschen.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_Loeschen.Location = new System.Drawing.Point(342, 118);
            this.btn_Loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(154, 31);
            this.btn_Loeschen.TabIndex = 36;
            this.btn_Loeschen.Text = "Profil löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_OK.Location = new System.Drawing.Point(415, 429);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(86, 31);
            this.btn_OK.TabIndex = 37;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label24
            // 
            this.Label24.AutoSize = true;
            this.Label24.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label24.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label24.Location = new System.Drawing.Point(28, 10);
            this.Label24.Name = "Label24";
            this.Label24.Size = new System.Drawing.Size(201, 19);
            this.Label24.TabIndex = 38;
            this.Label24.Text = "Datenbank Brauchwasserprofile";
            this.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_TypeDBedit
            // 
            this.btn_TypeDBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_TypeDBedit.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_TypeDBedit.Location = new System.Drawing.Point(342, 157);
            this.btn_TypeDBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_TypeDBedit.Name = "btn_TypeDBedit";
            this.btn_TypeDBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_TypeDBedit.TabIndex = 39;
            this.btn_TypeDBedit.Text = "Pofiltyp ändern...";
            this.btn_TypeDBedit.UseVisualStyleBackColor = true;
            this.btn_TypeDBedit.Click += new System.EventHandler(this.btn_TypeDBedit_Click);
            // 
            // btn_DBedit
            // 
            this.btn_DBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_DBedit.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_DBedit.Location = new System.Drawing.Point(342, 40);
            this.btn_DBedit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_DBedit.Name = "btn_DBedit";
            this.btn_DBedit.Size = new System.Drawing.Size(154, 31);
            this.btn_DBedit.TabIndex = 40;
            this.btn_DBedit.Text = "Profil ändern...";
            this.btn_DBedit.UseVisualStyleBackColor = true;
            this.btn_DBedit.Click += new System.EventHandler(this.btn_DBedit_Click);
            // 
            // Label12
            // 
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label12.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label12.Location = new System.Drawing.Point(16, 362);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(201, 22);
            this.Label12.TabIndex = 41;
            this.Label12.Text = "jährlicher Wärmebedarf:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label10.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label10.Location = new System.Drawing.Point(61, 223);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(48, 19);
            this.Label10.TabIndex = 42;
            this.Label10.Text = "Name:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Prozess_Name
            // 
            this.textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Prozess_Name.Location = new System.Drawing.Point(117, 225);
            this.textBox_Prozess_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            this.textBox_Prozess_Name.ReadOnly = true;
            this.textBox_Prozess_Name.Size = new System.Drawing.Size(331, 25);
            this.textBox_Prozess_Name.TabIndex = 43;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label13.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label13.Location = new System.Drawing.Point(15, 287);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(94, 19);
            this.Label13.TabIndex = 44;
            this.Label13.Text = "Beschreibung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(225, 362);
            this.textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            this.textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(84, 25);
            this.textBox_Jahres_Verbrauch.TabIndex = 45;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Beschreibung.Location = new System.Drawing.Point(117, 289);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            this.textBox_Beschreibung.Size = new System.Drawing.Size(336, 57);
            this.textBox_Beschreibung.TabIndex = 46;
            // 
            // textBox_Prozess_Type
            // 
            this.textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Prozess_Type.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Prozess_Type.Location = new System.Drawing.Point(117, 257);
            this.textBox_Prozess_Type.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            this.textBox_Prozess_Type.ReadOnly = true;
            this.textBox_Prozess_Type.Size = new System.Drawing.Size(156, 25);
            this.textBox_Prozess_Type.TabIndex = 47;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label15.Location = new System.Drawing.Point(75, 255);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(33, 19);
            this.Label15.TabIndex = 48;
            this.Label15.Text = "Typ:";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label11.Location = new System.Drawing.Point(314, 365);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(48, 19);
            this.Label11.TabIndex = 49;
            this.Label11.Text = "MWth";
            this.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_Brauchwasser_Admin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 470);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.listBox_DB);
            this.Controls.Add(this.btn_DBneu);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label24);
            this.Controls.Add(this.btn_TypeDBedit);
            this.Controls.Add(this.btn_DBedit);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Prozess_Name);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Jahres_Verbrauch);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Prozess_Type);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label11);
            this.Name = "Form_Brauchwasser_Admin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Administration Brauchwasser";
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        #endregion

        private System.Windows.Forms.Button btn_Simulation;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.ListBox listBox_DB;
        private System.Windows.Forms.Button btn_DBneu;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label Label24;
        private System.Windows.Forms.Button btn_TypeDBedit;
        private System.Windows.Forms.Button btn_DBedit;
        private System.Windows.Forms.Label Label12;
        private System.Windows.Forms.Label Label10;
        private System.Windows.Forms.TextBox textBox_Prozess_Name;
        private System.Windows.Forms.Label Label13;
        private System.Windows.Forms.TextBox textBox_Jahres_Verbrauch;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Prozess_Type;
        private System.Windows.Forms.Label Label15;
        private System.Windows.Forms.Label Label11;
    }
}