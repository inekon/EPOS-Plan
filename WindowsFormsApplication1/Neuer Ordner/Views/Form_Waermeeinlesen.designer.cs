namespace WindowsFormsApplication1
{
    partial class Form_Waermeeinlesen
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
this.Label1 = new System.Windows.Forms.Label();
this.lb_gewaehlt = new System.Windows.Forms.ListBox();
this.btn_Hinzufuegen = new System.Windows.Forms.Button();
this.btn_Entfernen = new System.Windows.Forms.Button();
this.lb_Auswahlliste = new System.Windows.Forms.ListBox();
this.Label2 = new System.Windows.Forms.Label();
this.btn_Hilfe = new System.Windows.Forms.Button();
this.btn_OK = new System.Windows.Forms.Button();
this.Label3 = new System.Windows.Forms.Label();
this.tb_Infoname = new System.Windows.Forms.TextBox();
this.Label4 = new System.Windows.Forms.Label();
this.tb_Infodatum = new System.Windows.Forms.TextBox();
this.Label5 = new System.Windows.Forms.Label();
this.tb_Infobeschreibung = new System.Windows.Forms.TextBox();
this.SuspendLayout();


//
// btn_Hinzufuegen//
//
this.btn_Hinzufuegen.Font = new System.Drawing.Font("Segoe UI", 8F);
this.btn_Hinzufuegen.Location = new System.Drawing.Point(246, 54);
this.btn_Hinzufuegen.Name = "btn_Hinzufuegen";
this.btn_Hinzufuegen.Size = new System.Drawing.Size(102, 24);
this.btn_Hinzufuegen.TabIndex = 2;
this.btn_Hinzufuegen.Text = "<<<  Hinzufügen";
this.btn_Hinzufuegen.UseVisualStyleBackColor = true;
//
// btn_Entfernen//
//
this.btn_Entfernen.Font = new System.Drawing.Font("Segoe UI", 8F);
this.btn_Entfernen.Location = new System.Drawing.Point(246, 90);
this.btn_Entfernen.Name = "btn_Entfernen";
this.btn_Entfernen.Size = new System.Drawing.Size(102, 24);
this.btn_Entfernen.TabIndex = 3;
this.btn_Entfernen.Text = "Entfernen >>>";
this.btn_Entfernen.UseVisualStyleBackColor = true;


//
// btn_Hilfe//
//
this.btn_Hilfe.Font = new System.Drawing.Font("Segoe UI", 8F);
this.btn_Hilfe.Location = new System.Drawing.Point(384, 318);
this.btn_Hilfe.Name = "btn_Hilfe";
this.btn_Hilfe.Size = new System.Drawing.Size(84, 24);
this.btn_Hilfe.TabIndex = 6;
this.btn_Hilfe.Text = "Hilfe";
this.btn_Hilfe.UseVisualStyleBackColor = true;
//
// btn_OK//
//
this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 8F);
this.btn_OK.Location = new System.Drawing.Point(486,05, 318);
this.btn_OK.Name = "btn_OK";
this.btn_OK.Size = new System.Drawing.Size(84, 24);
this.btn_OK.TabIndex = 7;
this.btn_OK.Text = "Schließen";
this.btn_OK.UseVisualStyleBackColor = true;
//
// Label1//
//
this.Label1.AutoSize = true;
this.Label1.Font = new System.Drawing.Font("Tahoma" , 8F);
this.Label1.Location = new System.Drawing.Point(24, 24);
this.Label1.Name = "Label1";
this.Label1.Size = new System.Drawing.Size(108, 12);
this.Label1.TabIndex = 0;
this.Label1.Text = "Ausgewählt:";
this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
//
// Label2//
//
this.Label2.AutoSize = true;
this.Label2.Font = new System.Drawing.Font("Tahoma" , 8F);
this.Label2.Location = new System.Drawing.Point(366, 24);
this.Label2.Name = "Label2";
this.Label2.Size = new System.Drawing.Size(108, 12);
this.Label2.TabIndex = 5;
this.Label2.Text = "Wärmebedarf:";
this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
//
// Label3//
//
this.Label3.AutoSize = true;
this.Label3.Font = new System.Drawing.Font("Tahoma" , 8F);
this.Label3.Location = new System.Drawing.Point(36, 186);
this.Label3.Name = "Label3";
this.Label3.Size = new System.Drawing.Size(48, 12);
this.Label3.TabIndex = 8;
this.Label3.Text = "Name:";
this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
//
// Label4//
//
this.Label4.AutoSize = true;
this.Label4.Font = new System.Drawing.Font("Tahoma" , 8F);
this.Label4.Location = new System.Drawing.Point(36, 210);
this.Label4.Name = "Label4";
this.Label4.Size = new System.Drawing.Size(48, 12);
this.Label4.TabIndex = 10;
this.Label4.Text = "Datum:";
this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
//
// Label5//
//
this.Label5.AutoSize = true;
this.Label5.Font = new System.Drawing.Font("Tahoma" , 8F);
this.Label5.Location = new System.Drawing.Point(30, 234);
this.Label5.Name = "Label5";
this.Label5.Size = new System.Drawing.Size(54, 12);
this.Label5.TabIndex = 12;
this.Label5.Text = "Beschreibung:";
this.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
//
// tb_Infoname//
//
this.tb_Infoname.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
this.tb_Infoname.Enabled = false;
this.tb_Infoname.Font = new System.Drawing.Font("Segoe UI", 8F);
this.tb_Infoname.Location = new System.Drawing.Point(102, 180);
this.tb_Infoname.Margin = new System.Windows.Forms.Padding(4);
this.tb_Infoname.Name = "tb_Infoname";
this.tb_Infoname.Size = new System.Drawing.Size(294, 294;
this.tb_Infoname.TabIndex = 9;
//
// tb_Infodatum//
//
this.tb_Infodatum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
this.tb_Infodatum.Enabled = false;
this.tb_Infodatum.Font = new System.Drawing.Font("Segoe UI", 8F);
this.tb_Infodatum.Location = new System.Drawing.Point(102, 204);
this.tb_Infodatum.Margin = new System.Windows.Forms.Padding(4);
this.tb_Infodatum.Name = "tb_Infodatum";
this.tb_Infodatum.Size = new System.Drawing.Size(90, 90;
this.tb_Infodatum.TabIndex = 11;
//
// tb_Infobeschreibung//
//
this.tb_Infobeschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
this.tb_Infobeschreibung.Enabled = false;
this.tb_Infobeschreibung.Font = new System.Drawing.Font("Segoe UI", 8F);
this.tb_Infobeschreibung.Location = new System.Drawing.Point(102, 228);
this.tb_Infobeschreibung.Margin = new System.Windows.Forms.Padding(4);
this.tb_Infobeschreibung.Name = "tb_Infobeschreibung";
this.tb_Infobeschreibung.Size = new System.Drawing.Size(444, 444;
this.tb_Infobeschreibung.TabIndex = 13;
//
// lb_Auswahlliste//
//
this.lb_AuswahllisteFormattingEnabled = true;
this.lb_Auswahlliste.Location = new System.Drawing.Point(366, 42);
this.lb_Auswahlliste.Name = "lb_Auswahlliste";
this.lb_Auswahlliste.Size = new System.Drawing.Size(204, 20);
this.lb_Auswahlliste.TabIndex = 4;
//
// Dial_Waermeeinlesen
//
this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
this.ClientSize = new System.Drawing.Size(594, 351);
this.Controls.Add(this.Label1);
this.Controls.Add(this.lb_gewaehlt);
this.Controls.Add(this.btn_Hinzufuegen);
this.Controls.Add(this.btn_Entfernen);
this.Controls.Add(this.lb_Auswahlliste);
this.Controls.Add(this.Label2);
this.Controls.Add(this.btn_Hilfe);
this.Controls.Add(this.btn_OK);
this.Controls.Add(this.Label3);
this.Controls.Add(this.tb_Infoname);
this.Controls.Add(this.Label4);
this.Controls.Add(this.tb_Infodatum);
this.Controls.Add(this.Label5);
this.Controls.Add(this.tb_Infobeschreibung);

            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

private System.Windows.Forms.Label Label1;
private System.Windows.Forms.ListBox lb_gewaehlt;
private System.Windows.Forms.Button btn_Hinzufuegen;
private System.Windows.Forms.Button btn_Entfernen;
private System.Windows.Forms.ListBox lb_Auswahlliste;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_Hilfe;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label3;
private System.Windows.Forms.TextBox tb_Infoname;
private System.Windows.Forms.Label Label4;
private System.Windows.Forms.TextBox tb_Infodatum;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.TextBox tb_Infobeschreibung;


 
    }
}