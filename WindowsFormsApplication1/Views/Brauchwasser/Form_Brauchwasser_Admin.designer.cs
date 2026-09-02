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
            btn_Simulation = new System.Windows.Forms.Button();
            btn_Abbrechen = new System.Windows.Forms.Button();
            listBox_DB = new System.Windows.Forms.ListBox();
            btn_DBneu = new System.Windows.Forms.Button();
            btn_Loeschen = new System.Windows.Forms.Button();
            btn_OK = new System.Windows.Forms.Button();
            Label24 = new System.Windows.Forms.Label();
            btn_TypeDBedit = new System.Windows.Forms.Button();
            btn_DBedit = new System.Windows.Forms.Button();
            Label12 = new System.Windows.Forms.Label();
            Label10 = new System.Windows.Forms.Label();
            textBox_Prozess_Name = new System.Windows.Forms.TextBox();
            Label13 = new System.Windows.Forms.Label();
            textBox_Jahres_Verbrauch = new System.Windows.Forms.TextBox();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            textBox_Prozess_Type = new System.Windows.Forms.TextBox();
            Label15 = new System.Windows.Forms.Label();
            Label11 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // btn_Simulation
            // 
            btn_Simulation.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_Simulation.Location = new System.Drawing.Point(192, 495);
            btn_Simulation.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btn_Simulation.Name = "btn_Simulation";
            btn_Simulation.Size = new System.Drawing.Size(106, 36);
            btn_Simulation.TabIndex = 52;
            btn_Simulation.Text = "Grafik";
            btn_Simulation.UseVisualStyleBackColor = true;
            btn_Simulation.Click += btn_Simulation_Click;
            // 
            // btn_Abbrechen
            // 
            btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_Abbrechen.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_Abbrechen.Location = new System.Drawing.Point(371, 495);
            btn_Abbrechen.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.Size = new System.Drawing.Size(106, 36);
            btn_Abbrechen.TabIndex = 51;
            btn_Abbrechen.Text = "Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // listBox_DB
            // 
            listBox_DB.FormattingEnabled = true;
            listBox_DB.ItemHeight = 15;
            listBox_DB.Location = new System.Drawing.Point(37, 37);
            listBox_DB.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            listBox_DB.Name = "listBox_DB";
            listBox_DB.Size = new System.Drawing.Size(335, 199);
            listBox_DB.TabIndex = 50;
            listBox_DB.Click += listBox_DB_Click;
            listBox_DB.SelectedIndexChanged += listBox_DB_SelectedIndexChanged;
            // 
            // btn_DBneu
            // 
            btn_DBneu.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_DBneu.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_DBneu.Location = new System.Drawing.Point(399, 91);
            btn_DBneu.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_DBneu.Name = "btn_DBneu";
            btn_DBneu.Size = new System.Drawing.Size(180, 36);
            btn_DBneu.TabIndex = 35;
            btn_DBneu.Text = "Neues Profil...";
            btn_DBneu.UseVisualStyleBackColor = true;
            btn_DBneu.Click += btn_DBneu_Click;
            // 
            // btn_Loeschen
            // 
            btn_Loeschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_Loeschen.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_Loeschen.Location = new System.Drawing.Point(399, 136);
            btn_Loeschen.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_Loeschen.Name = "btn_Loeschen";
            btn_Loeschen.Size = new System.Drawing.Size(180, 36);
            btn_Loeschen.TabIndex = 36;
            btn_Loeschen.Text = "Profil löschen";
            btn_Loeschen.UseVisualStyleBackColor = true;
            btn_Loeschen.Click += btn_Loeschen_Click;
            // 
            // btn_OK
            // 
            btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_OK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_OK.Location = new System.Drawing.Point(484, 495);
            btn_OK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_OK.Name = "btn_OK";
            btn_OK.Size = new System.Drawing.Size(100, 36);
            btn_OK.TabIndex = 37;
            btn_OK.Text = "OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // Label24
            // 
            Label24.AutoSize = true;
            Label24.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label24.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label24.Location = new System.Drawing.Point(33, 12);
            Label24.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label24.Name = "Label24";
            Label24.Size = new System.Drawing.Size(201, 19);
            Label24.TabIndex = 38;
            Label24.Text = "Datenbank Brauchwasserprofile";
            Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_TypeDBedit
            // 
            btn_TypeDBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_TypeDBedit.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_TypeDBedit.Location = new System.Drawing.Point(399, 181);
            btn_TypeDBedit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_TypeDBedit.Name = "btn_TypeDBedit";
            btn_TypeDBedit.Size = new System.Drawing.Size(180, 36);
            btn_TypeDBedit.TabIndex = 39;
            btn_TypeDBedit.Text = "Pofiltyp ändern...";
            btn_TypeDBedit.UseVisualStyleBackColor = true;
            btn_TypeDBedit.Click += btn_TypeDBedit_Click;
            // 
            // btn_DBedit
            // 
            btn_DBedit.Font = new System.Drawing.Font("Segoe UI", 10F);
            btn_DBedit.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            btn_DBedit.Location = new System.Drawing.Point(399, 46);
            btn_DBedit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btn_DBedit.Name = "btn_DBedit";
            btn_DBedit.Size = new System.Drawing.Size(180, 36);
            btn_DBedit.TabIndex = 40;
            btn_DBedit.Text = "Profil ändern...";
            btn_DBedit.UseVisualStyleBackColor = true;
            btn_DBedit.Click += btn_DBedit_Click;
            // 
            // Label12
            // 
            Label12.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label12.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label12.Location = new System.Drawing.Point(19, 418);
            Label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label12.Name = "Label12";
            Label12.Size = new System.Drawing.Size(234, 25);
            Label12.TabIndex = 41;
            Label12.Text = "jährlicher Wärmebedarf:";
            Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label10
            // 
            Label10.AutoSize = true;
            Label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label10.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label10.Location = new System.Drawing.Point(71, 257);
            Label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label10.Name = "Label10";
            Label10.Size = new System.Drawing.Size(48, 19);
            Label10.TabIndex = 42;
            Label10.Text = "Name:";
            Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Prozess_Name
            // 
            textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Prozess_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            textBox_Prozess_Name.Location = new System.Drawing.Point(136, 260);
            textBox_Prozess_Name.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            textBox_Prozess_Name.ReadOnly = true;
            textBox_Prozess_Name.Size = new System.Drawing.Size(386, 25);
            textBox_Prozess_Name.TabIndex = 43;
            // 
            // Label13
            // 
            Label13.AutoSize = true;
            Label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label13.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label13.Location = new System.Drawing.Point(18, 331);
            Label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label13.Name = "Label13";
            Label13.Size = new System.Drawing.Size(94, 19);
            Label13.TabIndex = 44;
            Label13.Text = "Beschreibung:";
            Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Jahres_Verbrauch
            // 
            textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Jahres_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            textBox_Jahres_Verbrauch.Location = new System.Drawing.Point(262, 418);
            textBox_Jahres_Verbrauch.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            textBox_Jahres_Verbrauch.ReadOnly = true;
            textBox_Jahres_Verbrauch.Size = new System.Drawing.Size(98, 25);
            textBox_Jahres_Verbrauch.TabIndex = 45;
            // 
            // textBox_Beschreibung
            // 
            textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            textBox_Beschreibung.Location = new System.Drawing.Point(136, 333);
            textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            textBox_Beschreibung.Multiline = true;
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            textBox_Beschreibung.ReadOnly = true;
            textBox_Beschreibung.Size = new System.Drawing.Size(392, 65);
            textBox_Beschreibung.TabIndex = 46;
            // 
            // textBox_Prozess_Type
            // 
            textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox_Prozess_Type.Font = new System.Drawing.Font("Segoe UI", 10F);
            textBox_Prozess_Type.Location = new System.Drawing.Point(136, 297);
            textBox_Prozess_Type.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            textBox_Prozess_Type.ReadOnly = true;
            textBox_Prozess_Type.Size = new System.Drawing.Size(182, 25);
            textBox_Prozess_Type.TabIndex = 47;
            // 
            // Label15
            // 
            Label15.AutoSize = true;
            Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label15.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label15.Location = new System.Drawing.Point(88, 294);
            Label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label15.Name = "Label15";
            Label15.Size = new System.Drawing.Size(33, 19);
            Label15.TabIndex = 48;
            Label15.Text = "Typ:";
            Label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label11
            // 
            Label11.AutoSize = true;
            Label11.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            Label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            Label11.ForeColor = System.Drawing.Color.White;
            Label11.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            Label11.Location = new System.Drawing.Point(366, 421);
            Label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            Label11.Name = "Label11";
            Label11.Size = new System.Drawing.Size(48, 19);
            Label11.TabIndex = 49;
            Label11.Text = "MWth";
            Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_Brauchwasser_Admin
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(602, 542);
            Controls.Add(btn_Simulation);
            Controls.Add(btn_Abbrechen);
            Controls.Add(listBox_DB);
            Controls.Add(btn_DBneu);
            Controls.Add(btn_Loeschen);
            Controls.Add(btn_OK);
            Controls.Add(Label24);
            Controls.Add(btn_TypeDBedit);
            Controls.Add(btn_DBedit);
            Controls.Add(Label12);
            Controls.Add(Label10);
            Controls.Add(textBox_Prozess_Name);
            Controls.Add(Label13);
            Controls.Add(textBox_Jahres_Verbrauch);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(textBox_Prozess_Type);
            Controls.Add(Label15);
            Controls.Add(Label11);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "Form_Brauchwasser_Admin";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Administration Brauchwasser";
            ResumeLayout(false);
            PerformLayout();

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