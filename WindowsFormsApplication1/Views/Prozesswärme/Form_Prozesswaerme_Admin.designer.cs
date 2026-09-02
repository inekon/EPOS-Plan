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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Prozesswaerme_Admin));
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
            resources.ApplyResources(this.btn_Prozess_DBneu, "btn_Prozess_DBneu");
            this.btn_Prozess_DBneu.Name = "btn_Prozess_DBneu";
            this.btn_Prozess_DBneu.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBneu.Click += new System.EventHandler(this.btn_Prozess_DBneu_Click);
            // 
            // btn_Prozess_loeschen
            // 
            resources.ApplyResources(this.btn_Prozess_loeschen, "btn_Prozess_loeschen");
            this.btn_Prozess_loeschen.Name = "btn_Prozess_loeschen";
            this.btn_Prozess_loeschen.UseVisualStyleBackColor = true;
            this.btn_Prozess_loeschen.Click += new System.EventHandler(this.btn_Prozess_loeschen_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label24
            // 
            resources.ApplyResources(this.Label24, "Label24");
            this.Label24.Name = "Label24";
            // 
            // btn_ProzTypeDBedit
            // 
            resources.ApplyResources(this.btn_ProzTypeDBedit, "btn_ProzTypeDBedit");
            this.btn_ProzTypeDBedit.Name = "btn_ProzTypeDBedit";
            this.btn_ProzTypeDBedit.UseVisualStyleBackColor = true;
            this.btn_ProzTypeDBedit.Click += new System.EventHandler(this.btn_ProzTypeDBedit_Click);
            // 
            // btn_Prozess_DBedit
            // 
            resources.ApplyResources(this.btn_Prozess_DBedit, "btn_Prozess_DBedit");
            this.btn_Prozess_DBedit.Name = "btn_Prozess_DBedit";
            this.btn_Prozess_DBedit.UseVisualStyleBackColor = true;
            this.btn_Prozess_DBedit.Click += new System.EventHandler(this.btn_Prozess_DBedit_Click);
            // 
            // Label12
            // 
            resources.ApplyResources(this.Label12, "Label12");
            this.Label12.Name = "Label12";
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.Name = "Label10";
            // 
            // textBox_Prozess_Name
            // 
            this.textBox_Prozess_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Prozess_Name, "textBox_Prozess_Name");
            this.textBox_Prozess_Name.Name = "textBox_Prozess_Name";
            this.textBox_Prozess_Name.ReadOnly = true;
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.Name = "Label13";
            // 
            // textBox_Jahres_Verbrauch
            // 
            this.textBox_Jahres_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Jahres_Verbrauch, "textBox_Jahres_Verbrauch");
            this.textBox_Jahres_Verbrauch.Name = "textBox_Jahres_Verbrauch";
            this.textBox_Jahres_Verbrauch.ReadOnly = true;
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.ReadOnly = true;
            // 
            // textBox_Prozess_Type
            // 
            this.textBox_Prozess_Type.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Prozess_Type, "textBox_Prozess_Type");
            this.textBox_Prozess_Type.Name = "textBox_Prozess_Type";
            this.textBox_Prozess_Type.ReadOnly = true;
            // 
            // Label15
            // 
            resources.ApplyResources(this.Label15, "Label15");
            this.Label15.Name = "Label15";
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Label11.ForeColor = System.Drawing.Color.White;
            this.Label11.Name = "Label11";
            // 
            // listBox_Prozess_DB
            // 
            this.listBox_Prozess_DB.FormattingEnabled = true;
            resources.ApplyResources(this.listBox_Prozess_DB, "listBox_Prozess_DB");
            this.listBox_Prozess_DB.Name = "listBox_Prozess_DB";
            this.listBox_Prozess_DB.Click += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            this.listBox_Prozess_DB.SelectedIndexChanged += new System.EventHandler(this.listBox_Prozess_DB_SelectedIndexChanged);
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_Simulation
            // 
            resources.ApplyResources(this.btn_Simulation, "btn_Simulation");
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.UseVisualStyleBackColor = true;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // Form_Prozesswaerme_Admin
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
            this.Name = "Form_Prozesswaerme_Admin";
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