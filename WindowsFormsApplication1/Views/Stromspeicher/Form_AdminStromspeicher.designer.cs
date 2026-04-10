namespace WindowsFormsApplication1
{
    partial class Form_AdminStromspeicher
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_AdminStromspeicher));
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.listBox_Stromspeicher = new System.Windows.Forms.ListBox();
            this.textBox_Bezeichner = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Energie = new System.Windows.Forms.TextBox();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Ladezustand = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Degradation = new System.Windows.Forms.TextBox();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox_Modulkosten = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // listBox_Stromspeicher
            // 
            this.listBox_Stromspeicher.FormattingEnabled = true;
            resources.ApplyResources(this.listBox_Stromspeicher, "listBox_Stromspeicher");
            this.listBox_Stromspeicher.Name = "listBox_Stromspeicher";
            this.listBox_Stromspeicher.SelectedIndexChanged += new System.EventHandler(this.listBox_Stromspeicher_SelectedIndexChanged);
            // 
            // textBox_Bezeichner
            // 
            resources.ApplyResources(this.textBox_Bezeichner, "textBox_Bezeichner");
            this.textBox_Bezeichner.Name = "textBox_Bezeichner";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // textBox_Energie
            // 
            resources.ApplyResources(this.textBox_Energie, "textBox_Energie");
            this.textBox_Energie.Name = "textBox_Energie";
            this.textBox_Energie.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Energie_Validating);
            // 
            // textBox_Typ
            // 
            resources.ApplyResources(this.textBox_Typ, "textBox_Typ");
            this.textBox_Typ.Name = "textBox_Typ";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // textBox_Leistung
            // 
            resources.ApplyResources(this.textBox_Leistung, "textBox_Leistung");
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // textBox_Ladezustand
            // 
            resources.ApplyResources(this.textBox_Ladezustand, "textBox_Ladezustand");
            this.textBox_Ladezustand.Name = "textBox_Ladezustand";
            this.textBox_Ladezustand.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Ladezustand_Validating);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textBox_Degradation
            // 
            resources.ApplyResources(this.textBox_Degradation, "textBox_Degradation");
            this.textBox_Degradation.Name = "textBox_Degradation";
            this.textBox_Degradation.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Degradation_Validating);
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
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
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.BackColor = System.Drawing.Color.Black;
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Name = "label8";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.BackColor = System.Drawing.Color.Black;
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Name = "label9";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.BackColor = System.Drawing.Color.Black;
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Name = "label10";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.BackColor = System.Drawing.Color.Black;
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Name = "label11";
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // textBox_Modulkosten
            // 
            resources.ApplyResources(this.textBox_Modulkosten, "textBox_Modulkosten");
            this.textBox_Modulkosten.Name = "textBox_Modulkosten";
            // 
            // Form_AdminStromspeicher
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.textBox_Modulkosten);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Degradation);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_Ladezustand);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Leistung);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Typ);
            this.Controls.Add(this.textBox_Energie);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Bezeichner);
            this.Controls.Add(this.listBox_Stromspeicher);
            this.Controls.Add(this.btn_Beenden);
            this.Name = "Form_AdminStromspeicher";
            this.Load += new System.EventHandler(this.Form_Stromspeicher_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.ListBox listBox_Stromspeicher;
        private System.Windows.Forms.TextBox textBox_Bezeichner;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Energie;
        private System.Windows.Forms.TextBox textBox_Typ;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Ladezustand;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Degradation;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBox_Modulkosten;
    }
}