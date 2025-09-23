namespace WindowsFormsApplication1
{
    partial class Form_GebWohnflaeche
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
            this.btn_Hilfe = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.txt_Gebaeudeart = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txt_Baujahr = new System.Windows.Forms.TextBox();
            this.Label3 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.txt_Gebaeudename = new System.Windows.Forms.TextBox();
            this.txt_Beschreibung = new System.Windows.Forms.TextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.Label5 = new System.Windows.Forms.Label();
            this.txt_Verbrauch = new System.Windows.Forms.TextBox();
            this.txt_Einheit = new System.Windows.Forms.TextBox();
            this.Label6 = new System.Windows.Forms.Label();
            this.D_BW = new System.Windows.Forms.CheckBox();
            this.J_Text = new System.Windows.Forms.Label();
            this.Jahresnutzungsgrad = new System.Windows.Forms.TextBox();
            this.txt_Bedarfsart_Auswahl = new System.Windows.Forms.TextBox();
            this.cmb_Bedarfsart = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Hilfe
            // 
            this.btn_Hilfe.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Hilfe.Location = new System.Drawing.Point(555, 439);
            this.btn_Hilfe.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Hilfe.Name = "btn_Hilfe";
            this.btn_Hilfe.Size = new System.Drawing.Size(84, 31);
            this.btn_Hilfe.TabIndex = 0;
            this.btn_Hilfe.Text = "Hilfe";
            this.btn_Hilfe.UseVisualStyleBackColor = true;
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_Abbrechen.Location = new System.Drawing.Point(653, 439);
            this.btn_Abbrechen.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(91, 31);
            this.btn_Abbrechen.TabIndex = 1;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btn_OK.Location = new System.Drawing.Point(758, 439);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(77, 31);
            this.btn_OK.TabIndex = 2;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // txt_Gebaeudeart
            // 
            this.txt_Gebaeudeart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Gebaeudeart.Enabled = false;
            this.txt_Gebaeudeart.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Gebaeudeart.Location = new System.Drawing.Point(131, 129);
            this.txt_Gebaeudeart.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Gebaeudeart.Name = "txt_Gebaeudeart";
            this.txt_Gebaeudeart.Size = new System.Drawing.Size(189, 25);
            this.txt_Gebaeudeart.TabIndex = 6;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txt_Baujahr);
            this.groupBox1.Controls.Add(this.Label3);
            this.groupBox1.Controls.Add(this.Label4);
            this.groupBox1.Controls.Add(this.Label1);
            this.groupBox1.Controls.Add(this.txt_Gebaeudename);
            this.groupBox1.Controls.Add(this.txt_Beschreibung);
            this.groupBox1.Controls.Add(this.Label2);
            this.groupBox1.Controls.Add(this.txt_Gebaeudeart);
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.groupBox1.Location = new System.Drawing.Point(14, 13);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(735, 192);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Info ausgewähltes Gebäude";
            // 
            // txt_Baujahr
            // 
            this.txt_Baujahr.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Baujahr.Enabled = false;
            this.txt_Baujahr.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Baujahr.Location = new System.Drawing.Point(131, 161);
            this.txt_Baujahr.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Baujahr.Name = "txt_Baujahr";
            this.txt_Baujahr.Size = new System.Drawing.Size(189, 25);
            this.txt_Baujahr.TabIndex = 13;
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label3.Location = new System.Drawing.Point(31, 132);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(84, 19);
            this.Label3.TabIndex = 14;
            this.Label3.Text = "Gebäudeart:";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label4
            // 
            this.Label4.AutoSize = true;
            this.Label4.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label4.Location = new System.Drawing.Point(52, 161);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(58, 19);
            this.Label4.TabIndex = 15;
            this.Label4.Text = "Baujahr:";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label1.Location = new System.Drawing.Point(15, 35);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(101, 19);
            this.Label1.TabIndex = 9;
            this.Label1.Text = "Gebäudename:";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txt_Gebaeudename
            // 
            this.txt_Gebaeudename.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Gebaeudename.Enabled = false;
            this.txt_Gebaeudename.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Gebaeudename.Location = new System.Drawing.Point(131, 32);
            this.txt_Gebaeudename.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Gebaeudename.Name = "txt_Gebaeudename";
            this.txt_Gebaeudename.Size = new System.Drawing.Size(189, 25);
            this.txt_Gebaeudename.TabIndex = 10;
            // 
            // txt_Beschreibung
            // 
            this.txt_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Beschreibung.Enabled = false;
            this.txt_Beschreibung.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Beschreibung.Location = new System.Drawing.Point(131, 67);
            this.txt_Beschreibung.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Beschreibung.Multiline = true;
            this.txt_Beschreibung.Name = "txt_Beschreibung";
            this.txt_Beschreibung.Size = new System.Drawing.Size(518, 52);
            this.txt_Beschreibung.TabIndex = 11;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(20, 70);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(94, 19);
            this.Label2.TabIndex = 12;
            this.Label2.Text = "Beschreibung:";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.Label5);
            this.groupBox2.Controls.Add(this.txt_Verbrauch);
            this.groupBox2.Controls.Add(this.txt_Einheit);
            this.groupBox2.Controls.Add(this.Label6);
            this.groupBox2.Controls.Add(this.D_BW);
            this.groupBox2.Controls.Add(this.J_Text);
            this.groupBox2.Controls.Add(this.Jahresnutzungsgrad);
            this.groupBox2.Controls.Add(this.txt_Bedarfsart_Auswahl);
            this.groupBox2.Controls.Add(this.cmb_Bedarfsart);
            this.groupBox2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.groupBox2.Location = new System.Drawing.Point(14, 228);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(821, 203);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Eingabe für das ausgewählte Gebäude";
            // 
            // Label5
            // 
            this.Label5.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label5.Location = new System.Drawing.Point(10, 31);
            this.Label5.Name = "Label5";
            this.Label5.Size = new System.Drawing.Size(92, 48);
            this.Label5.TabIndex = 20;
            this.Label5.Text = "Wärmebedarf/Wohnfläche:";
            this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txt_Verbrauch
            // 
            this.txt_Verbrauch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Verbrauch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Verbrauch.Location = new System.Drawing.Point(108, 46);
            this.txt_Verbrauch.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Verbrauch.Name = "txt_Verbrauch";
            this.txt_Verbrauch.Size = new System.Drawing.Size(77, 25);
            this.txt_Verbrauch.TabIndex = 21;
            // 
            // txt_Einheit
            // 
            this.txt_Einheit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Einheit.Enabled = false;
            this.txt_Einheit.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Einheit.Location = new System.Drawing.Point(195, 46);
            this.txt_Einheit.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Einheit.Name = "txt_Einheit";
            this.txt_Einheit.Size = new System.Drawing.Size(125, 25);
            this.txt_Einheit.TabIndex = 22;
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label6.Location = new System.Drawing.Point(374, 21);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(106, 19);
            this.Label6.TabIndex = 23;
            this.Label6.Text = "Art der Angabe:";
            this.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // D_BW
            // 
            this.D_BW.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.D_BW.Location = new System.Drawing.Point(13, 107);
            this.D_BW.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.D_BW.Name = "D_BW";
            this.D_BW.Size = new System.Drawing.Size(263, 26);
            this.D_BW.TabIndex = 24;
            this.D_BW.Text = "Dezentrale Warmwasserbereitung";
            this.D_BW.UseVisualStyleBackColor = true;
            // 
            // J_Text
            // 
            this.J_Text.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.J_Text.Location = new System.Drawing.Point(588, 31);
            this.J_Text.Name = "J_Text";
            this.J_Text.Size = new System.Drawing.Size(148, 142);
            this.J_Text.TabIndex = 25;
            this.J_Text.Text = "Bei Brennstoffangaben bitte Heizkessel Jahresnutzungsgrad eingeben: z.B. 0.85 für" +
    " 85%";
            this.J_Text.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Jahresnutzungsgrad
            // 
            this.Jahresnutzungsgrad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Jahresnutzungsgrad.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Jahresnutzungsgrad.Location = new System.Drawing.Point(744, 57);
            this.Jahresnutzungsgrad.Margin = new System.Windows.Forms.Padding(5);
            this.Jahresnutzungsgrad.Name = "Jahresnutzungsgrad";
            this.Jahresnutzungsgrad.Size = new System.Drawing.Size(56, 25);
            this.Jahresnutzungsgrad.TabIndex = 26;
            // 
            // txt_Bedarfsart_Auswahl
            // 
            this.txt_Bedarfsart_Auswahl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Bedarfsart_Auswahl.Enabled = false;
            this.txt_Bedarfsart_Auswahl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txt_Bedarfsart_Auswahl.Location = new System.Drawing.Point(378, 45);
            this.txt_Bedarfsart_Auswahl.Margin = new System.Windows.Forms.Padding(5);
            this.txt_Bedarfsart_Auswahl.Name = "txt_Bedarfsart_Auswahl";
            this.txt_Bedarfsart_Auswahl.Size = new System.Drawing.Size(198, 25);
            this.txt_Bedarfsart_Auswahl.TabIndex = 27;
            // 
            // cmb_Bedarfsart
            // 
            this.cmb_Bedarfsart.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmb_Bedarfsart.FormattingEnabled = true;
            this.cmb_Bedarfsart.ItemHeight = 17;
            this.cmb_Bedarfsart.Location = new System.Drawing.Point(378, 79);
            this.cmb_Bedarfsart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmb_Bedarfsart.Name = "cmb_Bedarfsart";
            this.cmb_Bedarfsart.Size = new System.Drawing.Size(198, 106);
            this.cmb_Bedarfsart.TabIndex = 28;
            this.cmb_Bedarfsart.SelectedIndexChanged += new System.EventHandler(this.cmb_Bedarfsart_SelectedIndexChanged);
            // 
            // Form_GebWohnflaeche
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(849, 478);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Hilfe);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_GebWohnflaeche";
            this.Text = "Eingabe der gesamten Wohn-/Nutzfläche des ausgewählten Gebäudes ";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_Abbrechen;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TextBox txt_Gebaeudeart;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.TextBox txt_Baujahr;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.Label Label1;
private System.Windows.Forms.TextBox txt_Gebaeudename;
private System.Windows.Forms.TextBox txt_Beschreibung;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.GroupBox groupBox2;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.TextBox txt_Verbrauch;
private System.Windows.Forms.TextBox txt_Einheit;
private System.Windows.Forms.Label Label6;
private System.Windows.Forms.CheckBox D_BW;
private System.Windows.Forms.Label J_Text;
private System.Windows.Forms.TextBox Jahresnutzungsgrad;
private System.Windows.Forms.TextBox txt_Bedarfsart_Auswahl;
private System.Windows.Forms.ListBox cmb_Bedarfsart;


 
    }
}