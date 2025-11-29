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
            this.listBox_Extern = new System.Windows.Forms.ListBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.btn_Oeffnen = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Ordner = new System.Windows.Forms.TextBox();
            this.btn_Datei = new System.Windows.Forms.Button();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_Einlesen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listBox_Extern
            // 
            this.listBox_Extern.HorizontalScrollbar = true;
            this.listBox_Extern.ItemHeight = 17;
            this.listBox_Extern.Location = new System.Drawing.Point(24, 35);
            this.listBox_Extern.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBox_Extern.Name = "listBox_Extern";
            this.listBox_Extern.Size = new System.Drawing.Size(639, 174);
            this.listBox_Extern.TabIndex = 4;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Label2.Location = new System.Drawing.Point(20, 12);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(121, 19);
            this.Label2.TabIndex = 5;
            this.Label2.Text = "Ganglinien aus DB";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_OK
            // 
            this.btn_OK.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_OK.Location = new System.Drawing.Point(565, 390);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(98, 31);
            this.btn_OK.TabIndex = 7;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // textBox_Name
            // 
            this.textBox_Name.BackColor = System.Drawing.Color.White;
            this.textBox_Name.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Name.Location = new System.Drawing.Point(157, 307);
            this.textBox_Name.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Name.Multiline = true;
            this.textBox_Name.Name = "textBox_Name";
            this.textBox_Name.ReadOnly = true;
            this.textBox_Name.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Name.Size = new System.Drawing.Size(369, 74);
            this.textBox_Name.TabIndex = 9;
            // 
            // btn_Oeffnen
            // 
            this.btn_Oeffnen.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btn_Oeffnen.Location = new System.Drawing.Point(534, 305);
            this.btn_Oeffnen.Name = "btn_Oeffnen";
            this.btn_Oeffnen.Size = new System.Drawing.Size(119, 23);
            this.btn_Oeffnen.TabIndex = 15;
            this.btn_Oeffnen.Text = "Inhalt anzeigen...";
            this.btn_Oeffnen.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_Oeffnen.UseVisualStyleBackColor = true;
            this.btn_Oeffnen.Click += new System.EventHandler(this.btn_Oeffnen_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label6.Location = new System.Drawing.Point(342, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(126, 19);
            this.label6.TabIndex = 16;
            this.label6.Text = "Datei Basis Ordner:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label6.Visible = false;
            // 
            // textBox_Ordner
            // 
            this.textBox_Ordner.Enabled = false;
            this.textBox_Ordner.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.textBox_Ordner.Location = new System.Drawing.Point(490, 6);
            this.textBox_Ordner.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_Ordner.Name = "textBox_Ordner";
            this.textBox_Ordner.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBox_Ordner.Size = new System.Drawing.Size(176, 25);
            this.textBox_Ordner.TabIndex = 17;
            this.textBox_Ordner.Visible = false;
            // 
            // btn_Datei
            // 
            this.btn_Datei.Location = new System.Drawing.Point(19, 305);
            this.btn_Datei.Name = "btn_Datei";
            this.btn_Datei.Size = new System.Drawing.Size(135, 35);
            this.btn_Datei.TabIndex = 18;
            this.btn_Datei.Text = "Datei Auswählen...";
            this.btn_Datei.UseVisualStyleBackColor = true;
            this.btn_Datei.Click += new System.EventHandler(this.btn_Datei_Click);
            // 
            // btn_Loeschen
            // 
            this.btn_Loeschen.Location = new System.Drawing.Point(24, 214);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(136, 31);
            this.btn_Loeschen.TabIndex = 19;
            this.btn_Loeschen.Text = "Ganglinie Löschen";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            // 
            // btn_Einlesen
            // 
            this.btn_Einlesen.Location = new System.Drawing.Point(19, 346);
            this.btn_Einlesen.Name = "btn_Einlesen";
            this.btn_Einlesen.Size = new System.Drawing.Size(135, 35);
            this.btn_Einlesen.TabIndex = 20;
            this.btn_Einlesen.Text = "Datei Einlesen...";
            this.btn_Einlesen.UseVisualStyleBackColor = true;
            this.btn_Einlesen.Click += new System.EventHandler(this.btn_Einlesen_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(153, 283);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(245, 19);
            this.label1.TabIndex = 21;
            this.label1.Text = "Stundenwerte über 1 Jahr als Textdatei";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label3.Location = new System.Drawing.Point(415, 283);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(238, 19);
            this.label3.TabIndex = 22;
            this.label3.Text = "Dezimaltrennzeichen muß ein \',\' sein !";
            // 
            // Form_Waermeeinlesen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(681, 435);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Einlesen);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Datei);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_Ordner);
            this.Controls.Add(this.btn_Oeffnen);
            this.Controls.Add(this.listBox_Extern);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.textBox_Name);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form_Waermeeinlesen";
            this.Text = "Wärmebedarf Ganglinie";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox_Extern;
private System.Windows.Forms.Label Label2;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.TextBox textBox_Name;
private System.Windows.Forms.Button btn_Oeffnen;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.TextBox textBox_Ordner;
private System.Windows.Forms.Button btn_Datei;
private System.Windows.Forms.Button btn_Loeschen;
private System.Windows.Forms.Button btn_Einlesen;
private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
    }
}