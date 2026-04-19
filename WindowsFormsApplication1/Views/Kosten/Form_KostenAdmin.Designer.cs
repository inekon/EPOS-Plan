namespace WindowsFormsApplication1
{
    partial class Form_KostenAdmin
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Bezeichnung");
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("ID");
            this.btn_OK = new System.Windows.Forms.Button();
            this.lvwKostenfaktoren = new System.Windows.Forms.ListView();
            this.lblKategorieTitel = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnDeleteKostenfaktor = new System.Windows.Forms.Button();
            this.btnNeuKostenfaktor = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_OK
            // 
            this.btn_OK.BackColor = System.Drawing.SystemColors.Control;
            this.btn_OK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_OK.ForeColor = System.Drawing.Color.Black;
            this.btn_OK.Location = new System.Drawing.Point(469, 371);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(4);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(88, 30);
            this.btn_OK.TabIndex = 15;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = false;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // lvwKostenfaktoren
            // 
            this.lvwKostenfaktoren.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            this.lvwKostenfaktoren.FullRowSelect = true;
            this.lvwKostenfaktoren.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvwKostenfaktoren.HideSelection = false;
            this.lvwKostenfaktoren.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
            this.lvwKostenfaktoren.Location = new System.Drawing.Point(8, 28);
            this.lvwKostenfaktoren.Margin = new System.Windows.Forms.Padding(4);
            this.lvwKostenfaktoren.Name = "lvwKostenfaktoren";
            this.lvwKostenfaktoren.Size = new System.Drawing.Size(537, 203);
            this.lvwKostenfaktoren.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvwKostenfaktoren.TabIndex = 12;
            this.lvwKostenfaktoren.UseCompatibleStateImageBehavior = false;
            this.lvwKostenfaktoren.View = System.Windows.Forms.View.SmallIcon;
            // 
            // lblKategorieTitel
            // 
            this.lblKategorieTitel.AutoSize = true;
            this.lblKategorieTitel.Location = new System.Drawing.Point(5, 7);
            this.lblKategorieTitel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblKategorieTitel.Name = "lblKategorieTitel";
            this.lblKategorieTitel.Size = new System.Drawing.Size(99, 17);
            this.lblKategorieTitel.TabIndex = 16;
            this.lblKategorieTitel.Text = "Kostenfaktoren:";
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.Controls.Add(this.btnDeleteKostenfaktor);
            this.panel2.Controls.Add(this.lblKategorieTitel);
            this.panel2.Controls.Add(this.btnNeuKostenfaktor);
            this.panel2.Controls.Add(this.lvwKostenfaktoren);
            this.panel2.Location = new System.Drawing.Point(8, 51);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(552, 300);
            this.panel2.TabIndex = 17;
            // 
            // btnDeleteKostenfaktor
            // 
            this.btnDeleteKostenfaktor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteKostenfaktor.Location = new System.Drawing.Point(157, 255);
            this.btnDeleteKostenfaktor.Margin = new System.Windows.Forms.Padding(4);
            this.btnDeleteKostenfaktor.Name = "btnDeleteKostenfaktor";
            this.btnDeleteKostenfaktor.Size = new System.Drawing.Size(88, 32);
            this.btnDeleteKostenfaktor.TabIndex = 16;
            this.btnDeleteKostenfaktor.Text = "🗑️ Löschen";
            this.btnDeleteKostenfaktor.UseVisualStyleBackColor = true;
            this.btnDeleteKostenfaktor.Click += new System.EventHandler(this.btnDeleteKostenfaktor_Click);
            // 
            // btnNeuKostenfaktor
            // 
            this.btnNeuKostenfaktor.BackColor = System.Drawing.Color.LightGreen;
            this.btnNeuKostenfaktor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNeuKostenfaktor.Location = new System.Drawing.Point(8, 255);
            this.btnNeuKostenfaktor.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeuKostenfaktor.Name = "btnNeuKostenfaktor";
            this.btnNeuKostenfaktor.Size = new System.Drawing.Size(88, 32);
            this.btnNeuKostenfaktor.TabIndex = 15;
            this.btnNeuKostenfaktor.Text = "➕ Neu";
            this.btnNeuKostenfaktor.UseVisualStyleBackColor = false;
            this.btnNeuKostenfaktor.Click += new System.EventHandler(this.btnNeuKostenfaktor_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.Control;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(15, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(269, 21);
            this.label2.TabIndex = 18;
            this.label2.Text = "Verwalten Sie hier die Kostenfaktoren";
            // 
            // Form_KostenAdmin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(569, 414);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.btn_OK);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_KostenAdmin";
            this.Text = "Administration Kostenfaktoren ";
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ListView lvwKostenfaktoren;
        private System.Windows.Forms.Label lblKategorieTitel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnDeleteKostenfaktor;
        private System.Windows.Forms.Button btnNeuKostenfaktor;
        private System.Windows.Forms.Label label2;
    }
}