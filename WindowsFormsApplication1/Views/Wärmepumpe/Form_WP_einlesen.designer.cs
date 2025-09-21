namespace WindowsFormsApplication1
{
    partial class Form_WP_einlesen
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
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.Liste_WP = new System.Windows.Forms.ListBox();
            this.btn_VDI3805 = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.Label10 = new System.Windows.Forms.Label();
            this.textBox_Aufstellung = new System.Windows.Forms.TextBox();
            this.textBox_ThLeistung = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label13 = new System.Windows.Forms.Label();
            this.textBox_Zusatzheizung = new System.Windows.Forms.TextBox();
            this.Label14 = new System.Windows.Forms.Label();
            this.Label15 = new System.Windows.Forms.Label();
            this.Label16 = new System.Windows.Forms.Label();
            this.textBox__Wirkungsgrad = new System.Windows.Forms.TextBox();
            this.Label17 = new System.Windows.Forms.Label();
            this.textBox_Typ = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Stufen = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_MaxVorlauf = new System.Windows.Forms.TextBox();
            this.textBox_Kuehlleistung = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label2.Location = new System.Drawing.Point(21, 92);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(110, 17);
            this.Label2.TabIndex = 1;
            this.Label2.Text = "Liste WP-Module:";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_Uebernehmen
            // 
            this.btn_Uebernehmen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Uebernehmen.ForeColor = System.Drawing.Color.Black;
            this.btn_Uebernehmen.Location = new System.Drawing.Point(476, 489);
            this.btn_Uebernehmen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.Size = new System.Drawing.Size(122, 31);
            this.btn_Uebernehmen.TabIndex = 5;
            this.btn_Uebernehmen.Text = "Speichern DB";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // btn_Beenden
            // 
            this.btn_Beenden.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Beenden.ForeColor = System.Drawing.Color.Black;
            this.btn_Beenden.Location = new System.Drawing.Point(633, 489);
            this.btn_Beenden.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.Size = new System.Drawing.Size(91, 31);
            this.btn_Beenden.TabIndex = 6;
            this.btn_Beenden.Text = "OK";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // Liste_WP
            // 
            this.Liste_WP.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Liste_WP.ForeColor = System.Drawing.Color.Black;
            this.Liste_WP.HorizontalScrollbar = true;
            this.Liste_WP.ItemHeight = 17;
            this.Liste_WP.Location = new System.Drawing.Point(21, 115);
            this.Liste_WP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Liste_WP.Name = "Liste_WP";
            this.Liste_WP.Size = new System.Drawing.Size(377, 293);
            this.Liste_WP.TabIndex = 8;
            this.Liste_WP.SelectedIndexChanged += new System.EventHandler(this.Liste_WP_SelectedIndexChanged);
            // 
            // btn_VDI3805
            // 
            this.btn_VDI3805.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_VDI3805.ForeColor = System.Drawing.Color.Black;
            this.btn_VDI3805.Location = new System.Drawing.Point(24, 41);
            this.btn_VDI3805.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_VDI3805.Name = "btn_VDI3805";
            this.btn_VDI3805.Size = new System.Drawing.Size(196, 31);
            this.btn_VDI3805.TabIndex = 9;
            this.btn_VDI3805.Text = "WP aus Datei VDI 3805 einlesen...";
            this.btn_VDI3805.UseVisualStyleBackColor = true;
            this.btn_VDI3805.Click += new System.EventHandler(this.btn_VDI3805_Click);
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label3.ForeColor = System.Drawing.Color.Black;
            this.Label3.Location = new System.Drawing.Point(422, 48);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(43, 17);
            this.Label3.TabIndex = 11;
            this.Label3.Text = "Firma:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Firma
            // 
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Enabled = false;
            this.textBox_Firma.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Firma.ForeColor = System.Drawing.Color.Black;
            this.textBox_Firma.Location = new System.Drawing.Point(422, 70);
            this.textBox_Firma.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Firma.Name = "textBox_Firma";
            this.textBox_Firma.Size = new System.Drawing.Size(304, 25);
            this.textBox_Firma.TabIndex = 15;
            // 
            // Label9
            // 
            this.Label9.AutoSize = true;
            this.Label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label9.ForeColor = System.Drawing.Color.Black;
            this.Label9.Location = new System.Drawing.Point(422, 102);
            this.Label9.Name = "Label9";
            this.Label9.Size = new System.Drawing.Size(46, 17);
            this.Label9.TabIndex = 23;
            this.Label9.Text = "Name:";
            this.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Name
            // 
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Name.ForeColor = System.Drawing.Color.Black;
            this.textBox_Name.Location = new System.Drawing.Point(422, 124);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(304, 25);
            this.textBox_Name.TabIndex = 24;
            // 
            // Label10
            // 
            this.Label10.AutoSize = true;
            this.Label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label10.ForeColor = System.Drawing.Color.Black;
            this.Label10.Location = new System.Drawing.Point(422, 163);
            this.Label10.Name = "Label10";
            this.Label10.Size = new System.Drawing.Size(75, 17);
            this.Label10.TabIndex = 25;
            this.Label10.Text = "Aufstellung:";
            this.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_Aufstellung
            // 
            this.textBox_Aufstellung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Aufstellung.Enabled = false;
            this.textBox_Aufstellung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Aufstellung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Aufstellung.Location = new System.Drawing.Point(565, 161);
            this.textBox_Aufstellung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Aufstellung.Name = "textBox_Aufstellung";
            this.textBox_Aufstellung.Size = new System.Drawing.Size(161, 25);
            this.textBox_Aufstellung.TabIndex = 26;
            // 
            // textBox_ThLeistung
            // 
            this.textBox_ThLeistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_ThLeistung.Enabled = false;
            this.textBox_ThLeistung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_ThLeistung.ForeColor = System.Drawing.Color.Black;
            this.textBox_ThLeistung.Location = new System.Drawing.Point(565, 196);
            this.textBox_ThLeistung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_ThLeistung.Name = "textBox_ThLeistung";
            this.textBox_ThLeistung.Size = new System.Drawing.Size(91, 25);
            this.textBox_ThLeistung.TabIndex = 27;
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label12.ForeColor = System.Drawing.Color.Black;
            this.Label12.Location = new System.Drawing.Point(422, 196);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(126, 17);
            this.Label12.TabIndex = 28;
            this.Label12.Text = "thermische Leistung:";
            this.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label13.ForeColor = System.Drawing.Color.Black;
            this.Label13.Location = new System.Drawing.Point(422, 228);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(135, 17);
            this.Label13.TabIndex = 29;
            this.Label13.Text = "elektrische Zuheizung:";
            this.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Zusatzheizung
            // 
            this.textBox_Zusatzheizung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Zusatzheizung.Enabled = false;
            this.textBox_Zusatzheizung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Zusatzheizung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Zusatzheizung.Location = new System.Drawing.Point(565, 228);
            this.textBox_Zusatzheizung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Zusatzheizung.Name = "textBox_Zusatzheizung";
            this.textBox_Zusatzheizung.Size = new System.Drawing.Size(91, 25);
            this.textBox_Zusatzheizung.TabIndex = 30;
            // 
            // Label14
            // 
            this.Label14.AutoSize = true;
            this.Label14.BackColor = System.Drawing.Color.Black;
            this.Label14.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label14.ForeColor = System.Drawing.Color.White;
            this.Label14.Location = new System.Drawing.Point(663, 196);
            this.Label14.Name = "Label14";
            this.Label14.Size = new System.Drawing.Size(42, 19);
            this.Label14.TabIndex = 31;
            this.Label14.Text = "kWth";
            this.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label15
            // 
            this.Label15.AutoSize = true;
            this.Label15.BackColor = System.Drawing.Color.Black;
            this.Label15.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label15.ForeColor = System.Drawing.Color.White;
            this.Label15.Location = new System.Drawing.Point(663, 228);
            this.Label15.Name = "Label15";
            this.Label15.Size = new System.Drawing.Size(29, 19);
            this.Label15.TabIndex = 32;
            this.Label15.Text = "kW";
            this.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label16
            // 
            this.Label16.AutoSize = true;
            this.Label16.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label16.ForeColor = System.Drawing.Color.Black;
            this.Label16.Location = new System.Drawing.Point(422, 294);
            this.Label16.Name = "Label16";
            this.Label16.Size = new System.Drawing.Size(93, 17);
            this.Label16.TabIndex = 33;
            this.Label16.Text = "Wirkungsgrad:";
            this.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox__Wirkungsgrad
            // 
            this.textBox__Wirkungsgrad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox__Wirkungsgrad.Enabled = false;
            this.textBox__Wirkungsgrad.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox__Wirkungsgrad.ForeColor = System.Drawing.Color.Black;
            this.textBox__Wirkungsgrad.Location = new System.Drawing.Point(565, 294);
            this.textBox__Wirkungsgrad.Margin = new System.Windows.Forms.Padding(5);
            this.textBox__Wirkungsgrad.Name = "textBox__Wirkungsgrad";
            this.textBox__Wirkungsgrad.Size = new System.Drawing.Size(91, 25);
            this.textBox__Wirkungsgrad.TabIndex = 34;
            // 
            // Label17
            // 
            this.Label17.AutoSize = true;
            this.Label17.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label17.ForeColor = System.Drawing.Color.Black;
            this.Label17.Location = new System.Drawing.Point(422, 326);
            this.Label17.Name = "Label17";
            this.Label17.Size = new System.Drawing.Size(32, 17);
            this.Label17.TabIndex = 35;
            this.Label17.Text = "Typ:";
            this.Label17.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Typ
            // 
            this.textBox_Typ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Typ.Enabled = false;
            this.textBox_Typ.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Typ.ForeColor = System.Drawing.Color.Black;
            this.textBox_Typ.Location = new System.Drawing.Point(565, 326);
            this.textBox_Typ.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Typ.Name = "textBox_Typ";
            this.textBox_Typ.Size = new System.Drawing.Size(161, 25);
            this.textBox_Typ.TabIndex = 36;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(422, 358);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 17);
            this.label1.TabIndex = 37;
            this.label1.Text = "Stufen:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_Stufen
            // 
            this.textBox_Stufen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Stufen.Enabled = false;
            this.textBox_Stufen.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Stufen.ForeColor = System.Drawing.Color.Black;
            this.textBox_Stufen.Location = new System.Drawing.Point(565, 358);
            this.textBox_Stufen.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Stufen.Name = "textBox_Stufen";
            this.textBox_Stufen.Size = new System.Drawing.Size(161, 25);
            this.textBox_Stufen.TabIndex = 38;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(562, 383);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(107, 17);
            this.label4.TabIndex = 39;
            this.label4.Text = "* 0=modulierend";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Location = new System.Drawing.Point(422, 410);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(84, 17);
            this.label5.TabIndex = 40;
            this.label5.Text = "max. Vorlauf:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_MaxVorlauf
            // 
            this.textBox_MaxVorlauf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_MaxVorlauf.Enabled = false;
            this.textBox_MaxVorlauf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_MaxVorlauf.ForeColor = System.Drawing.Color.Black;
            this.textBox_MaxVorlauf.Location = new System.Drawing.Point(565, 410);
            this.textBox_MaxVorlauf.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_MaxVorlauf.Name = "textBox_MaxVorlauf";
            this.textBox_MaxVorlauf.Size = new System.Drawing.Size(161, 25);
            this.textBox_MaxVorlauf.TabIndex = 41;
            // 
            // textBox_Kuehlleistung
            // 
            this.textBox_Kuehlleistung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kuehlleistung.Enabled = false;
            this.textBox_Kuehlleistung.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Kuehlleistung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Kuehlleistung.Location = new System.Drawing.Point(565, 260);
            this.textBox_Kuehlleistung.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Kuehlleistung.Name = "textBox_Kuehlleistung";
            this.textBox_Kuehlleistung.Size = new System.Drawing.Size(91, 25);
            this.textBox_Kuehlleistung.TabIndex = 42;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(422, 260);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(81, 17);
            this.label6.TabIndex = 43;
            this.label6.Text = "Kühlleistung:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Black;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(663, 260);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 19);
            this.label7.TabIndex = 44;
            this.label7.Text = "kWcool";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form_WP_einlesen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(754, 533);
            this.Controls.Add(this.textBox_Kuehlleistung);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox_MaxVorlauf);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Stufen);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_Uebernehmen);
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.Liste_WP);
            this.Controls.Add(this.btn_VDI3805);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.Label9);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.Label10);
            this.Controls.Add(this.textBox_Aufstellung);
            this.Controls.Add(this.textBox_ThLeistung);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.Label13);
            this.Controls.Add(this.textBox_Zusatzheizung);
            this.Controls.Add(this.Label14);
            this.Controls.Add(this.Label15);
            this.Controls.Add(this.Label16);
            this.Controls.Add(this.textBox__Wirkungsgrad);
            this.Controls.Add(this.Label17);
            this.Controls.Add(this.textBox_Typ);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_WP_einlesen";
            this.Text = "Wärmepumpen Einlesen";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_Uebernehmen;
private System.Windows.Forms.Button btn_Beenden;
private System.Windows.Forms.ListBox Liste_WP;
private System.Windows.Forms.Button btn_VDI3805;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.Label Label9;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Label Label10;
private System.Windows.Forms.TextBox textBox_Aufstellung;
private System.Windows.Forms.TextBox textBox_ThLeistung;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Label Label13;
private System.Windows.Forms.TextBox textBox_Zusatzheizung;
private System.Windows.Forms.Label Label14;
private System.Windows.Forms.Label Label15;
private System.Windows.Forms.Label Label16;
private System.Windows.Forms.TextBox textBox__Wirkungsgrad;
private System.Windows.Forms.Label Label17;
private System.Windows.Forms.TextBox textBox_Typ;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.TextBox textBox_Stufen;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.TextBox textBox_MaxVorlauf;
private System.Windows.Forms.TextBox textBox_Kuehlleistung;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.Label label7;


 
    }
}