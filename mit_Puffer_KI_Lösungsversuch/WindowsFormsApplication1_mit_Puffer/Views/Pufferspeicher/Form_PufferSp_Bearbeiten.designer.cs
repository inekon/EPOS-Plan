namespace WindowsFormsApplication1
{
    partial class Form_PufferSp_Bearbeiten 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_PufferSp_Bearbeiten));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Investitionskosten = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Volumen = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Verluste = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBox_Speichertyp = new System.Windows.Forms.ComboBox();
            this.Label1 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.textBox_Hersteller = new System.Windows.Forms.TextBox();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Speichern_Unter = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_Ueberschreiben = new System.Windows.Forms.Button();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.Label13);
            this.groupBox3.Controls.Add(this.textBox_Investitionskosten);
            this.groupBox3.Controls.Add(this.Label17);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // Label13
            // 
            resources.ApplyResources(this.Label13, "Label13");
            this.Label13.Name = "Label13";
            // 
            // textBox_Investitionskosten
            // 
            resources.ApplyResources(this.textBox_Investitionskosten, "textBox_Investitionskosten");
            this.textBox_Investitionskosten.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Investitionskosten.Name = "textBox_Investitionskosten";
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.BackColor = System.Drawing.Color.Black;
            this.Label17.ForeColor = System.Drawing.Color.White;
            this.Label17.Name = "Label17";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBox_Volumen);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.Label8);
            this.groupBox2.Controls.Add(this.textBox_Verluste);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Name = "label7";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // textBox_Volumen
            // 
            resources.ApplyResources(this.textBox_Volumen, "textBox_Volumen");
            this.textBox_Volumen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Volumen.Name = "textBox_Volumen";
            this.textBox_Volumen.TextChanged += new System.EventHandler(this.textBox_Volumen_TextChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Name = "label5";
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.Name = "Label8";
            // 
            // textBox_Verluste
            // 
            resources.ApplyResources(this.textBox_Verluste, "textBox_Verluste");
            this.textBox_Verluste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Verluste.Name = "textBox_Verluste";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.comboBox_Speichertyp);
            this.groupBox1.Controls.Add(this.Label1);
            this.groupBox1.Controls.Add(this.Label2);
            this.groupBox1.Controls.Add(this.Label3);
            this.groupBox1.Controls.Add(this.textBox_Name);
            this.groupBox1.Controls.Add(this.textBox_Hersteller);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // comboBox_Speichertyp
            // 
            resources.ApplyResources(this.comboBox_Speichertyp, "comboBox_Speichertyp");
            this.comboBox_Speichertyp.FormattingEnabled = true;
            this.comboBox_Speichertyp.Items.AddRange(new object[] {
            resources.GetString("comboBox_Speichertyp.Items"),
            resources.GetString("comboBox_Speichertyp.Items1"),
            resources.GetString("comboBox_Speichertyp.Items2")});
            this.comboBox_Speichertyp.Name = "comboBox_Speichertyp";
            // 
            // Label1
            // 
            resources.ApplyResources(this.Label1, "Label1");
            this.Label1.Name = "Label1";
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.Name = "Label3";
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Name = "textBox_Name";
            // 
            // textBox_Hersteller
            // 
            resources.ApplyResources(this.textBox_Hersteller, "textBox_Hersteller");
            this.textBox_Hersteller.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Hersteller.Name = "textBox_Hersteller";
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_Speichern_Unter
            // 
            resources.ApplyResources(this.btn_Speichern_Unter, "btn_Speichern_Unter");
            this.btn_Speichern_Unter.Name = "btn_Speichern_Unter";
            this.btn_Speichern_Unter.UseVisualStyleBackColor = true;
            this.btn_Speichern_Unter.Click += new System.EventHandler(this.btn_Speichern_Unter_Click);
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Ueberschreiben
            // 
            resources.ApplyResources(this.btn_Ueberschreiben, "btn_Ueberschreiben");
            this.btn_Ueberschreiben.Name = "btn_Ueberschreiben";
            this.btn_Ueberschreiben.UseVisualStyleBackColor = true;
            this.btn_Ueberschreiben.Click += new System.EventHandler(this.btn_Ueberschreiben_Click);
            // 
            // Form_PufferSp_Bearbeiten
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Ueberschreiben);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_Speichern_Unter);
            this.Controls.Add(this.btn_Speichern);
            this.Name = "Form_PufferSp_Bearbeiten";
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label Label13;
        private System.Windows.Forms.TextBox textBox_Investitionskosten;
        private System.Windows.Forms.Label Label17;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label Label8;
        private System.Windows.Forms.TextBox textBox_Verluste;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox comboBox_Speichertyp;
        private System.Windows.Forms.Label Label1;
        private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.Label Label3;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.TextBox textBox_Hersteller;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_Speichern_Unter;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Volumen;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btn_Ueberschreiben;
        private System.Windows.Forms.Label label7;
    }
}