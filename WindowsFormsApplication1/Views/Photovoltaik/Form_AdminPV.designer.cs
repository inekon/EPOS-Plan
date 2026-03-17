namespace WindowsFormsApplication1
{
    partial class Form_AdminPV 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_AdminPV));
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.listBox_PV = new System.Windows.Forms.ListBox();
            this.textBox_Bezeichner = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_Wirkungsgrad = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Leistung = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_UMpp = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_ULeerlauf = new System.Windows.Forms.TextBox();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox_IMpp = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox_IKurzschluss = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_TempKoeff = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_Laenge = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.textBox_Breite = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // listBox_PV
            // 
            this.listBox_PV.FormattingEnabled = true;
            resources.ApplyResources(this.listBox_PV, "listBox_PV");
            this.listBox_PV.Name = "listBox_PV";
            this.listBox_PV.TabStop = false;
            this.listBox_PV.SelectedIndexChanged += new System.EventHandler(this.listBox_PV_SelectedIndexChanged);
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
            // textBox_Wirkungsgrad
            // 
            resources.ApplyResources(this.textBox_Wirkungsgrad, "textBox_Wirkungsgrad");
            this.textBox_Wirkungsgrad.Name = "textBox_Wirkungsgrad";
            this.textBox_Wirkungsgrad.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Wirkungsgrad_Validating);
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
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
            // textBox_UMpp
            // 
            resources.ApplyResources(this.textBox_UMpp, "textBox_UMpp");
            this.textBox_UMpp.Name = "textBox_UMpp";
            this.textBox_UMpp.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_UMpp_Validating);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // textBox_ULeerlauf
            // 
            resources.ApplyResources(this.textBox_ULeerlauf, "textBox_ULeerlauf");
            this.textBox_ULeerlauf.Name = "textBox_ULeerlauf";
            this.textBox_ULeerlauf.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_ULeerlauf_Validating);
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.TabStop = false;
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.TabStop = false;
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
            // btn_Speichern
            // 
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.TabStop = false;
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.BackColor = System.Drawing.Color.Black;
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Name = "label8";
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
            // textBox_IMpp
            // 
            resources.ApplyResources(this.textBox_IMpp, "textBox_IMpp");
            this.textBox_IMpp.Name = "textBox_IMpp";
            this.textBox_IMpp.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_IMpp_Validating);
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Name = "label13";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // textBox_IKurzschluss
            // 
            resources.ApplyResources(this.textBox_IKurzschluss, "textBox_IKurzschluss");
            this.textBox_IKurzschluss.Name = "textBox_IKurzschluss";
            this.textBox_IKurzschluss.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_IKurzschluss_Validating);
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Name = "label15";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // textBox_TempKoeff
            // 
            resources.ApplyResources(this.textBox_TempKoeff, "textBox_TempKoeff");
            this.textBox_TempKoeff.Name = "textBox_TempKoeff";
            this.textBox_TempKoeff.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_TempKoeff_Validating);
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.BackColor = System.Drawing.Color.Black;
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Name = "label17";
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // textBox_Laenge
            // 
            resources.ApplyResources(this.textBox_Laenge, "textBox_Laenge");
            this.textBox_Laenge.Name = "textBox_Laenge";
            this.textBox_Laenge.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Laenge_Validating);
            // 
            // label19
            // 
            resources.ApplyResources(this.label19, "label19");
            this.label19.BackColor = System.Drawing.Color.Black;
            this.label19.ForeColor = System.Drawing.Color.White;
            this.label19.Name = "label19";
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            // 
            // textBox_Breite
            // 
            resources.ApplyResources(this.textBox_Breite, "textBox_Breite");
            this.textBox_Breite.Name = "textBox_Breite";
            this.textBox_Breite.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Breite_Validating);
            // 
            // label21
            // 
            resources.ApplyResources(this.label21, "label21");
            this.label21.Name = "label21";
            // 
            // textBox_Firma
            // 
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // Form_AdminPV
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label21);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.textBox_Breite);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.textBox_Laenge);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.textBox_TempKoeff);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.textBox_IKurzschluss);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.textBox_IMpp);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.btn_Speichern);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_ULeerlauf);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_UMpp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox_Leistung);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Wirkungsgrad);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Bezeichner);
            this.Controls.Add(this.listBox_PV);
            this.Controls.Add(this.btn_Beenden);
            this.Name = "Form_AdminPV";
            this.Load += new System.EventHandler(this.Form_AdminPV_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Beenden;
        private System.Windows.Forms.ListBox listBox_PV;
        private System.Windows.Forms.TextBox textBox_Bezeichner;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_Wirkungsgrad;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Leistung;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_UMpp;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_ULeerlauf;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBox_IMpp;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox_IKurzschluss;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_TempKoeff;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_Laenge;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBox_Breite;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox textBox_Firma;
    }
}