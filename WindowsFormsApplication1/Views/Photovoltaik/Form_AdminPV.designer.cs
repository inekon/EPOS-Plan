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
            this.btn_Beenden.Location = new System.Drawing.Point(817, 542);
            this.btn_Beenden.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(87, 30);
            this.btn_Beenden.TabIndex = 0;
            this.btn_Beenden.Text = "OK";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // listBox_PV
            // 
            this.listBox_PV.FormattingEnabled = true;
            this.listBox_PV.ItemHeight = 17;
            this.listBox_PV.Location = new System.Drawing.Point(12, 22);
            this.listBox_PV.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_PV.Name = "listBox_PV";
            this.listBox_PV.Size = new System.Drawing.Size(211, 259);
            this.listBox_PV.TabIndex = 2;
            this.listBox_PV.TabStop = false;
            this.listBox_PV.SelectedIndexChanged += new System.EventHandler(this.listBox_PV_SelectedIndexChanged);
            // 
            // textBox_Bezeichner
            // 
            this.textBox_Bezeichner.Enabled = false;
            this.textBox_Bezeichner.Location = new System.Drawing.Point(336, 22);
            this.textBox_Bezeichner.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Bezeichner.Name = "textBox_Bezeichner";
            this.textBox_Bezeichner.Size = new System.Drawing.Size(250, 25);
            this.textBox_Bezeichner.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(237, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 19);
            this.label1.TabIndex = 4;
            this.label1.Text = "Bezeichner:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(253, 192);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 19);
            this.label2.TabIndex = 6;
            this.label2.Text = "Wirkungsgrad:";
            // 
            // textBox_Wirkungsgrad
            // 
            this.textBox_Wirkungsgrad.Location = new System.Drawing.Point(431, 190);
            this.textBox_Wirkungsgrad.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Wirkungsgrad.Name = "textBox_Wirkungsgrad";
            this.textBox_Wirkungsgrad.Size = new System.Drawing.Size(62, 25);
            this.textBox_Wirkungsgrad.TabIndex = 5;
            this.textBox_Wirkungsgrad.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Wirkungsgrad_Validating);
            // 
            // textBox_Beschreibung
            // 
            this.textBox_Beschreibung.Location = new System.Drawing.Point(336, 88);
            this.textBox_Beschreibung.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Beschreibung.Multiline = true;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            this.textBox_Beschreibung.Size = new System.Drawing.Size(250, 59);
            this.textBox_Beschreibung.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(237, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 19);
            this.label3.TabIndex = 6;
            this.label3.Text = "Beschreibung:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(253, 163);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(138, 19);
            this.label4.TabIndex = 8;
            this.label4.Text = "Nennleistung (Pmax):";
            // 
            // textBox_Leistung
            // 
            this.textBox_Leistung.Location = new System.Drawing.Point(431, 160);
            this.textBox_Leistung.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Leistung.Name = "textBox_Leistung";
            this.textBox_Leistung.Size = new System.Drawing.Size(62, 25);
            this.textBox_Leistung.TabIndex = 4;
            this.textBox_Leistung.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Leistung_Validating);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(253, 224);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(176, 19);
            this.label5.TabIndex = 10;
            this.label5.Text = "Spannung im MPP (Umpp):";
            // 
            // textBox_UMpp
            // 
            this.textBox_UMpp.Location = new System.Drawing.Point(431, 220);
            this.textBox_UMpp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_UMpp.Name = "textBox_UMpp";
            this.textBox_UMpp.Size = new System.Drawing.Size(62, 25);
            this.textBox_UMpp.TabIndex = 6;
            this.textBox_UMpp.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_UMpp_Validating);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(253, 254);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(157, 19);
            this.label6.TabIndex = 12;
            this.label6.Text = "Leerlaufspannung (Uoc):";
            // 
            // textBox_ULeerlauf
            // 
            this.textBox_ULeerlauf.Location = new System.Drawing.Point(431, 250);
            this.textBox_ULeerlauf.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_ULeerlauf.Name = "textBox_ULeerlauf";
            this.textBox_ULeerlauf.Size = new System.Drawing.Size(62, 25);
            this.textBox_ULeerlauf.TabIndex = 7;
            this.textBox_ULeerlauf.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_ULeerlauf_Validating);
            // 
            // btn_Neu
            // 
            this.btn_Neu.Location = new System.Drawing.Point(134, 285);
            this.btn_Neu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(89, 30);
            this.btn_Neu.TabIndex = 14;
            this.btn_Neu.TabStop = false;
            this.btn_Neu.Text = "Neu...";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(501, 449);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(87, 30);
            this.btn_OK.TabIndex = 15;
            this.btn_OK.TabStop = false;
            this.btn_OK.Text = "Beenden";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Location = new System.Drawing.Point(11, 285);
            this.btn_Loeschen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(89, 30);
            this.btn_Loeschen.TabIndex = 16;
            this.btn_Loeschen.Text = "Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(497, 163);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 19);
            this.label7.TabIndex = 17;
            this.label7.Text = "kW";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.Black;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(498, 223);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(18, 19);
            this.label9.TabIndex = 19;
            this.label9.Text = "V";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.Black;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label10.ForeColor = System.Drawing.Color.White;
            this.label10.Location = new System.Drawing.Point(498, 253);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(18, 19);
            this.label10.TabIndex = 20;
            this.label10.Text = "V";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Speichern.Location = new System.Drawing.Point(376, 449);
            this.btn_Speichern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(119, 30);
            this.btn_Speichern.TabIndex = 13;
            this.btn_Speichern.TabStop = false;
            this.btn_Speichern.Text = "Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Black;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(498, 192);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(20, 19);
            this.label8.TabIndex = 21;
            this.label8.Text = "%";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.Color.Black;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label11.ForeColor = System.Drawing.Color.White;
            this.label11.Location = new System.Drawing.Point(498, 284);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(18, 19);
            this.label11.TabIndex = 24;
            this.label11.Text = "A";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(253, 285);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(145, 19);
            this.label12.TabIndex = 23;
            this.label12.Text = "Strom im MPP (Impp):";
            // 
            // textBox_IMpp
            // 
            this.textBox_IMpp.Location = new System.Drawing.Point(431, 281);
            this.textBox_IMpp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_IMpp.Name = "textBox_IMpp";
            this.textBox_IMpp.Size = new System.Drawing.Size(62, 25);
            this.textBox_IMpp.TabIndex = 8;
            this.textBox_IMpp.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_IMpp_Validating);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.Color.Black;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(498, 315);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(18, 19);
            this.label13.TabIndex = 27;
            this.label13.Text = "A";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(253, 316);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(146, 19);
            this.label14.TabIndex = 26;
            this.label14.Text = "Kurzschlussstrom (Isc):";
            // 
            // textBox_IKurzschluss
            // 
            this.textBox_IKurzschluss.Location = new System.Drawing.Point(431, 312);
            this.textBox_IKurzschluss.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_IKurzschluss.Name = "textBox_IKurzschluss";
            this.textBox_IKurzschluss.Size = new System.Drawing.Size(62, 25);
            this.textBox_IKurzschluss.TabIndex = 9;
            this.textBox_IKurzschluss.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_IKurzschluss_Validating);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Black;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label15.ForeColor = System.Drawing.Color.White;
            this.label15.Location = new System.Drawing.Point(498, 346);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 19);
            this.label15.TabIndex = 30;
            this.label15.Text = "%/K";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(253, 347);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(154, 19);
            this.label16.TabIndex = 29;
            this.label16.Text = "Temp.-Koeffizient Pmax:";
            // 
            // textBox_TempKoeff
            // 
            this.textBox_TempKoeff.Location = new System.Drawing.Point(431, 343);
            this.textBox_TempKoeff.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_TempKoeff.Name = "textBox_TempKoeff";
            this.textBox_TempKoeff.Size = new System.Drawing.Size(62, 25);
            this.textBox_TempKoeff.TabIndex = 10;
            this.textBox_TempKoeff.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_TempKoeff_Validating);
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.BackColor = System.Drawing.Color.Black;
            this.label17.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label17.ForeColor = System.Drawing.Color.White;
            this.label17.Location = new System.Drawing.Point(498, 377);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(33, 19);
            this.label17.TabIndex = 33;
            this.label17.Text = "mm";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(253, 378);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(49, 19);
            this.label18.TabIndex = 32;
            this.label18.Text = "Länge:";
            // 
            // textBox_Laenge
            // 
            this.textBox_Laenge.Location = new System.Drawing.Point(431, 374);
            this.textBox_Laenge.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Laenge.Name = "textBox_Laenge";
            this.textBox_Laenge.Size = new System.Drawing.Size(62, 25);
            this.textBox_Laenge.TabIndex = 11;
            this.textBox_Laenge.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Laenge_Validating);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.BackColor = System.Drawing.Color.Black;
            this.label19.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label19.ForeColor = System.Drawing.Color.White;
            this.label19.Location = new System.Drawing.Point(498, 408);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(33, 19);
            this.label19.TabIndex = 36;
            this.label19.Text = "mm";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(253, 409);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(47, 19);
            this.label20.TabIndex = 35;
            this.label20.Text = "Breite:";
            // 
            // textBox_Breite
            // 
            this.textBox_Breite.Location = new System.Drawing.Point(431, 405);
            this.textBox_Breite.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Breite.Name = "textBox_Breite";
            this.textBox_Breite.Size = new System.Drawing.Size(62, 25);
            this.textBox_Breite.TabIndex = 12;
            this.textBox_Breite.Validating += new System.ComponentModel.CancelEventHandler(this.textBox_Breite_Validating);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(237, 58);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(70, 19);
            this.label21.TabIndex = 38;
            this.label21.Text = "Hersteller:";
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.Location = new System.Drawing.Point(336, 55);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(250, 25);
            this.textBox_Firma.TabIndex = 2;
            // 
            // Form_AdminPV
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 489);
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
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_AdminPV";
            this.Text = "Administration Photovoltaik Module";
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