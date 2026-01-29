namespace WindowsFormsApplication1
{
    partial class Form_SolarKollektorenAdmin 
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_SolarKollektorenAdmin));
            this.btn_Kollektor_DB_Edit = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_neu = new System.Windows.Forms.Button();
            this.btn_Kollektor_DB_loeschen = new System.Windows.Forms.Button();
            this.Label11 = new System.Windows.Forms.Label();
            this.textBox_Kollektor_A = new System.Windows.Forms.TextBox();
            this.Label12 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.Label5 = new System.Windows.Forms.Label();
            this.Label6 = new System.Windows.Forms.Label();
            this.Label7 = new System.Windows.Forms.Label();
            this.Label8 = new System.Windows.Forms.Label();
            this.textBox_Kollektortype = new System.Windows.Forms.TextBox();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.textBox_Firma = new System.Windows.Forms.TextBox();
            this.textBox_Modul_A = new System.Windows.Forms.TextBox();
            this.Label9 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Name = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Kollektor_DB_Edit
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_Edit, "btn_Kollektor_DB_Edit");
            this.btn_Kollektor_DB_Edit.Name = "btn_Kollektor_DB_Edit";
            this.btn_Kollektor_DB_Edit.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_Edit.Click += new System.EventHandler(this.btn_Kollektor_DB_Edit_Click);
            // 
            // btn_Kollektor_DB_neu
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_neu, "btn_Kollektor_DB_neu");
            this.btn_Kollektor_DB_neu.Name = "btn_Kollektor_DB_neu";
            this.btn_Kollektor_DB_neu.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_neu.Click += new System.EventHandler(this.btn_Kollektor_DB_neu_Click);
            // 
            // btn_Kollektor_DB_loeschen
            // 
            resources.ApplyResources(this.btn_Kollektor_DB_loeschen, "btn_Kollektor_DB_loeschen");
            this.btn_Kollektor_DB_loeschen.Name = "btn_Kollektor_DB_loeschen";
            this.btn_Kollektor_DB_loeschen.UseVisualStyleBackColor = true;
            this.btn_Kollektor_DB_loeschen.Click += new System.EventHandler(this.btn_Kollektor_DB_loeschen_Click);
            // 
            // Label11
            // 
            resources.ApplyResources(this.Label11, "Label11");
            this.Label11.Name = "Label11";
            // 
            // textBox_Kollektor_A
            // 
            resources.ApplyResources(this.textBox_Kollektor_A, "textBox_Kollektor_A");
            this.textBox_Kollektor_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektor_A.Name = "textBox_Kollektor_A";
            // 
            // Label12
            // 
            resources.ApplyResources(this.Label12, "Label12");
            this.Label12.BackColor = System.Drawing.Color.Black;
            this.Label12.ForeColor = System.Drawing.Color.White;
            this.Label12.Name = "Label12";
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // Label5
            // 
            resources.ApplyResources(this.Label5, "Label5");
            this.Label5.Name = "Label5";
            // 
            // Label6
            // 
            resources.ApplyResources(this.Label6, "Label6");
            this.Label6.Name = "Label6";
            // 
            // Label7
            // 
            resources.ApplyResources(this.Label7, "Label7");
            this.Label7.Name = "Label7";
            // 
            // Label8
            // 
            resources.ApplyResources(this.Label8, "Label8");
            this.Label8.Name = "Label8";
            // 
            // textBox_Kollektortype
            // 
            resources.ApplyResources(this.textBox_Kollektortype, "textBox_Kollektortype");
            this.textBox_Kollektortype.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kollektortype.Name = "textBox_Kollektortype";
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // textBox_Firma
            // 
            resources.ApplyResources(this.textBox_Firma, "textBox_Firma");
            this.textBox_Firma.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Firma.Name = "textBox_Firma";
            // 
            // textBox_Modul_A
            // 
            resources.ApplyResources(this.textBox_Modul_A, "textBox_Modul_A");
            this.textBox_Modul_A.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Modul_A.Name = "textBox_Modul_A";
            // 
            // Label9
            // 
            resources.ApplyResources(this.Label9, "Label9");
            this.Label9.BackColor = System.Drawing.Color.Black;
            this.Label9.ForeColor = System.Drawing.Color.White;
            this.Label9.Name = "Label9";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // textBox_Name
            // 
            resources.ApplyResources(this.textBox_Name, "textBox_Name");
            this.textBox_Name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Name.Name = "textBox_Name";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // dataGridView1
            // 
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.Silver;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Click += new System.EventHandler(this.dataGridView1_Click);
            this.dataGridView1.Leave += new System.EventHandler(this.dataGridView1_Leave);
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click_1);
            // 
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // Form_SolarKollektorenAdmin
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Name);
            this.Controls.Add(this.btn_Kollektor_DB_Edit);
            this.Controls.Add(this.btn_Kollektor_DB_neu);
            this.Controls.Add(this.btn_Kollektor_DB_loeschen);
            this.Controls.Add(this.Label11);
            this.Controls.Add(this.textBox_Kollektor_A);
            this.Controls.Add(this.Label12);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.Label5);
            this.Controls.Add(this.Label6);
            this.Controls.Add(this.Label7);
            this.Controls.Add(this.Label8);
            this.Controls.Add(this.textBox_Kollektortype);
            this.Controls.Add(this.textBox_Beschreibung);
            this.Controls.Add(this.textBox_Firma);
            this.Controls.Add(this.textBox_Modul_A);
            this.Controls.Add(this.Label9);
            this.Name = "Form_SolarKollektorenAdmin";
            this.Load += new System.EventHandler(this.Form_SolarKollektorenAdmin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
private System.Windows.Forms.Button btn_Kollektor_DB_Edit;
private System.Windows.Forms.Button btn_Kollektor_DB_neu;
private System.Windows.Forms.Button btn_Kollektor_DB_loeschen;
private System.Windows.Forms.Label Label11;
private System.Windows.Forms.TextBox textBox_Kollektor_A;
private System.Windows.Forms.Label Label12;
private System.Windows.Forms.Button btn_OK;
private System.Windows.Forms.Label Label5;
private System.Windows.Forms.Label Label6;
private System.Windows.Forms.Label Label7;
private System.Windows.Forms.Label Label8;
private System.Windows.Forms.TextBox textBox_Kollektortype;
private System.Windows.Forms.TextBox textBox_Beschreibung;
private System.Windows.Forms.TextBox textBox_Firma;
private System.Windows.Forms.TextBox textBox_Modul_A;
private System.Windows.Forms.Label Label9;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Name;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label_Type;
    }
}