namespace WindowsFormsApplication1
{
    partial class Form_Stromganglinie_Admin
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Stromganglinie_Admin));
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_Einlesen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_Zeitinterval = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox_Extern
            // 
            resources.ApplyResources(this.listBox_Extern, "listBox_Extern");
            this.listBox_Extern.Name = "listBox_Extern";
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // btn_Hilfe
            // 
            resources.ApplyResources(this.btn_Hilfe, "btn_Hilfe");
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Loeschen
            // 
            resources.ApplyResources(this.btn_Loeschen, "btn_Loeschen");
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_Einlesen
            // 
            resources.ApplyResources(this.btn_Einlesen, "btn_Einlesen");
            this.btn_Einlesen.Name = "btn_Einlesen";
            this.btn_Einlesen.UseVisualStyleBackColor = true;
            this.btn_Einlesen.Click += new System.EventHandler(this.btn_Einlesen_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.label1.Name = "label1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.comboBox_Zeitinterval);
            this.groupBox1.Controls.Add(this.btn_Einlesen);
            this.groupBox1.Controls.Add(this.label1);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // comboBox_Zeitinterval
            // 
            this.comboBox_Zeitinterval.FormattingEnabled = true;
            this.comboBox_Zeitinterval.Items.AddRange(new object[] {
            resources.GetString("comboBox_Zeitinterval.Items"),
            resources.GetString("comboBox_Zeitinterval.Items1")});
            resources.ApplyResources(this.comboBox_Zeitinterval, "comboBox_Zeitinterval");
            this.comboBox_Zeitinterval.Name = "comboBox_Zeitinterval";
            // 
            // Form_Stromganglinie_Admin
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_OK);
            this.Name = "Form_Stromganglinie_Admin";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Button btn_Loeschen;
private System.Windows.Forms.Button btn_Einlesen;
private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_Zeitinterval;
    }
}