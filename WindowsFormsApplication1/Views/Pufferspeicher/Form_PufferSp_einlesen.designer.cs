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
            btn_Uebernehmen = new System.Windows.Forms.Button();
            btn_Beenden = new System.Windows.Forms.Button();
            Liste_PufferSp = new System.Windows.Forms.ListBox();
            btn_VDI3805 = new System.Windows.Forms.Button();
            Label3 = new System.Windows.Forms.Label();
            textBox_Firma = new System.Windows.Forms.TextBox();
            Label9 = new System.Windows.Forms.Label();
            textBox_Name = new System.Windows.Forms.TextBox();
            Label10 = new System.Windows.Forms.Label();
            textBox_Typ = new System.Windows.Forms.TextBox();
            Label17 = new System.Windows.Forms.Label();
            textBox_Volumen = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            textBox_Versluste = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            num_VolumenVon = new System.Windows.Forms.NumericUpDown();
            num_VolumenBis = new System.Windows.Forms.NumericUpDown();
            lbl_VolumenFilter = new System.Windows.Forms.Label();
            lbl_VolumenBis = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)num_VolumenVon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)num_VolumenBis).BeginInit();
            SuspendLayout();
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(btn_Uebernehmen, "btn_Uebernehmen");
            btn_Uebernehmen.ForeColor = System.Drawing.Color.Black;
            btn_Uebernehmen.Image = Properties.Resources.save_icon_36513;
            btn_Uebernehmen.Name = "btn_Uebernehmen";
            btn_Uebernehmen.UseVisualStyleBackColor = true;
            btn_Uebernehmen.Click += btn_Uebernehmen_Click;
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(btn_Beenden, "btn_Beenden");
            btn_Beenden.ForeColor = System.Drawing.Color.Black;
            btn_Beenden.Name = "btn_Beenden";
            btn_Beenden.UseVisualStyleBackColor = true;
            btn_Beenden.Click += btn_Beenden_Click;
            // 
            // Liste_PufferSp
            // 
            resources.ApplyResources(Liste_PufferSp, "Liste_PufferSp");
            Liste_PufferSp.ForeColor = System.Drawing.Color.Black;
            Liste_PufferSp.Name = "Liste_PufferSp";
            Liste_PufferSp.SelectedIndexChanged += Liste_WP_SelectedIndexChanged;
            // 
            // btn_VDI3805
            // 
            resources.ApplyResources(btn_VDI3805, "btn_VDI3805");
            btn_VDI3805.ForeColor = System.Drawing.Color.Black;
            btn_VDI3805.Name = "btn_VDI3805";
            btn_VDI3805.UseVisualStyleBackColor = true;
            btn_VDI3805.Click += btn_VDI3805_Click;
            // 
            // Label3
            // 
            resources.ApplyResources(Label3, "Label3");
            Label3.ForeColor = System.Drawing.Color.Black;
            Label3.Name = "Label3";
            // 
            // textBox_Firma
            // 
            textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Firma, "textBox_Firma");
            textBox_Firma.ForeColor = System.Drawing.Color.Black;
            textBox_Firma.Name = "textBox_Firma";
            // 
            // Label9
            // 
            resources.ApplyResources(Label9, "Label9");
            Label9.ForeColor = System.Drawing.Color.Black;
            Label9.Name = "Label9";
            // 
            // textBox_Name
            // 
            textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Name, "textBox_Name");
            textBox_Name.ForeColor = System.Drawing.Color.Black;
            textBox_Name.Name = "textBox_Name";
            // 
            // Label10
            // 
            resources.ApplyResources(Label10, "Label10");
            Label10.ForeColor = System.Drawing.Color.Black;
            Label10.Name = "Label10";
            // 
            // textBox_Typ
            // 
            textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Typ, "textBox_Typ");
            textBox_Typ.ForeColor = System.Drawing.Color.Black;
            textBox_Typ.Name = "textBox_Typ";
            // 
            // Label17
            // 
            resources.ApplyResources(Label17, "Label17");
            Label17.ForeColor = System.Drawing.Color.Black;
            Label17.Name = "Label17";
            // 
            // textBox_Volumen
            // 
            textBox_Volumen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Volumen, "textBox_Volumen");
            textBox_Volumen.ForeColor = System.Drawing.Color.Black;
            textBox_Volumen.Name = "textBox_Volumen";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.ForeColor = System.Drawing.Color.Black;
            label1.Name = "label1";
            // 
            // textBox_Versluste
            // 
            textBox_Versluste.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(textBox_Versluste, "textBox_Versluste");
            textBox_Versluste.ForeColor = System.Drawing.Color.Black;
            textBox_Versluste.Name = "textBox_Versluste";
            // 
            // label4
            // 
            resources.ApplyResources(label4, "label4");
            label4.BackColor = System.Drawing.Color.Black;
            label4.ForeColor = System.Drawing.Color.White;
            label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(label5, "label5");
            label5.BackColor = System.Drawing.Color.Black;
            label5.ForeColor = System.Drawing.Color.White;
            label5.Name = "label5";
            // 
            // num_VolumenVon
            // 
            resources.ApplyResources(num_VolumenVon, "num_VolumenVon");
            num_VolumenVon.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_VolumenVon.Name = "num_VolumenVon";
            num_VolumenVon.ValueChanged += Volumenfilter_ValueChanged;
            // 
            // num_VolumenBis
            // 
            resources.ApplyResources(num_VolumenBis, "num_VolumenBis");
            num_VolumenBis.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            num_VolumenBis.Name = "num_VolumenBis";
            num_VolumenBis.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            num_VolumenBis.ValueChanged += Volumenfilter_ValueChanged;
            // 
            // lbl_VolumenFilter
            // 
            resources.ApplyResources(lbl_VolumenFilter, "lbl_VolumenFilter");
            lbl_VolumenFilter.ForeColor = System.Drawing.Color.Black;
            lbl_VolumenFilter.Name = "lbl_VolumenFilter";
            // 
            // lbl_VolumenBis
            // 
            resources.ApplyResources(lbl_VolumenBis, "lbl_VolumenBis");
            lbl_VolumenBis.ForeColor = System.Drawing.Color.Black;
            lbl_VolumenBis.Name = "lbl_VolumenBis";
            // 
            // Form_PufferSp_einlesen
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lbl_VolumenFilter);
            Controls.Add(num_VolumenVon);
            Controls.Add(lbl_VolumenBis);
            Controls.Add(num_VolumenBis);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label1);
            Controls.Add(textBox_Versluste);
            Controls.Add(btn_Uebernehmen);
            Controls.Add(btn_Beenden);
            Controls.Add(Liste_PufferSp);
            Controls.Add(btn_VDI3805);
            Controls.Add(Label3);
            Controls.Add(textBox_Firma);
            Controls.Add(Label9);
            Controls.Add(textBox_Name);
            Controls.Add(Label10);
            Controls.Add(textBox_Typ);
            Controls.Add(Label17);
            Controls.Add(textBox_Volumen);
            ForeColor = System.Drawing.Color.Black;
            Name = "Form_PufferSp_einlesen";
            ((System.ComponentModel.ISupportInitialize)num_VolumenVon).EndInit();
            ((System.ComponentModel.ISupportInitialize)num_VolumenBis).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
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
        private System.Windows.Forms.NumericUpDown num_VolumenVon;
        private System.Windows.Forms.NumericUpDown num_VolumenBis;
        private System.Windows.Forms.Label lbl_VolumenFilter;
        private System.Windows.Forms.Label lbl_VolumenBis;
    }
}