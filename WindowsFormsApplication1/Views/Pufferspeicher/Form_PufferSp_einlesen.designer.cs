namespace WindowsFormsApplication1
{
    partial class Form_PufferSp_einlesen 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_PufferSp_einlesen));
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.Liste_PufferSp = new System.Windows.Forms.ListBox();
            this.btn_VDI3805 = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.textBox_Volumen = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Versluste = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Label2
            // 
            resources.ApplyResources(this.Label2, "Label2");
            this.Label2.Name = "Label2";
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(this.btn_Uebernehmen, "btn_Uebernehmen");
            this.btn_Uebernehmen.ForeColor = System.Drawing.Color.Black;
            this.btn_Uebernehmen.Image = global::WindowsFormsApplication1.Properties.Resources.save_icon_36513;
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.ForeColor = System.Drawing.Color.Black;
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // Liste_PufferSp
            // 
            resources.ApplyResources(this.Liste_PufferSp, "Liste_PufferSp");
            this.Liste_PufferSp.ForeColor = System.Drawing.Color.Black;
            this.Liste_PufferSp.Name = "Liste_PufferSp";
            this.Liste_PufferSp.SelectedIndexChanged += new System.EventHandler(this.Liste_WP_SelectedIndexChanged);
            // 
            // btn_VDI3805
            // 
            resources.ApplyResources(this.btn_VDI3805, "btn_VDI3805");
            this.btn_VDI3805.ForeColor = System.Drawing.Color.Black;
            this.btn_VDI3805.Name = "btn_VDI3805";
            this.btn_VDI3805.UseVisualStyleBackColor = true;
            this.btn_VDI3805.Click += new System.EventHandler(this.btn_VDI3805_Click);
            // 
            // Label3
            // 
            resources.ApplyResources(this.Label3, "Label3");
            this.Label3.ForeColor = System.Drawing.Color.Black;
            this.Label3.Name = "Label3";
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.ForeColor = System.Drawing.Color.Black;
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.ForeColor = System.Drawing.Color.Black;
            this.Label9.Name = "Label9";
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.ForeColor = System.Drawing.Color.Black;
            this.textBox_Name.Name = "textBox_Name";
            // 
            // Label10
            // 
            resources.ApplyResources(this.Label10, "Label10");
            this.Label10.ForeColor = System.Drawing.Color.Black;
            this.Label10.Name = "Label10";
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Typ, "textBox_Typ");
            this.textBox_Typ.ForeColor = System.Drawing.Color.Black;
            this.textBox_Typ.Name = "textBox_Typ";
            // 
            // Label17
            // 
            resources.ApplyResources(this.Label17, "Label17");
            this.Label17.ForeColor = System.Drawing.Color.Black;
            this.Label17.Name = "Label17";
            // 
            // textBox_Volumen
            // 
            this.textBox_Volumen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Volumen, "textBox_Volumen");
            this.textBox_Volumen.ForeColor = System.Drawing.Color.Black;
            this.textBox_Volumen.Name = "textBox_Volumen";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Name = "label1";
            // 
            // textBox_Versluste
            // 
            this.textBox_Versluste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.textBox_Versluste, "textBox_Versluste");
            this.textBox_Versluste.ForeColor = System.Drawing.Color.Black;
            this.textBox_Versluste.Name = "textBox_Versluste";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.Black;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.Black;
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Name = "label5";
            // 
            // Form_PufferSp_einlesen
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Versluste);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_Uebernehmen);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.Liste_PufferSp);
            this.Controls.Add(this.btn_VDI3805);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.Label17);
            this.Controls.Add(this.textBox_Volumen);
            this.ForeColor = System.Drawing.Color.Black;
            this.Name = "Form_PufferSp_einlesen";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_Uebernehmen;
private System.Windows.Forms.Button btn_Beenden;
private System.Windows.Forms.ListBox Liste_PufferSp;
private System.Windows.Forms.Button btn_VDI3805;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Typ;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_Volumen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Versluste;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
    }
}