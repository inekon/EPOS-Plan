namespace WindowsFormsApplication1
{
    partial class Wizard_WP
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wizard_WP));
            this.label_Type = new System.Windows.Forms.Label();
            this.listView_WP = new System.Windows.Forms.ListView();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.btn_Ansicht = new System.Windows.Forms.Button();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.textBox_WP = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label_Type
            // 
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Dock = System.Windows.Forms.DockStyle.Top;
            this.label_Type.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Type.Location = new System.Drawing.Point(0, 0);
            this.label_Type.Name = "label_Type";
            this.label_Type.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label_Type.Size = new System.Drawing.Size(583, 32);
            this.label_Type.TabIndex = 1;
            this.label_Type.Text = "Geben Sie die Daten der Wärmepumpe ein";
            this.label_Type.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // listView_WP
            // 
            this.listView_WP.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.listView_WP.FullRowSelect = true;
            this.listView_WP.GridLines = true;
            this.listView_WP.HideSelection = false;
            this.listView_WP.Location = new System.Drawing.Point(27, 238);
            this.listView_WP.MultiSelect = false;
            this.listView_WP.Name = "listView_WP";
            this.listView_WP.Size = new System.Drawing.Size(531, 97);
            this.listView_WP.TabIndex = 4;
            this.listView_WP.UseCompatibleStateImageBehavior = false;
            this.listView_WP.SelectedIndexChanged += new System.EventHandler(this.listView_WP_SelectedIndexChanged);
            this.listView_WP.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseDoubleClick);
            // 
            // btn_Löschen
            // 
            this.btn_Löschen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Löschen.Location = new System.Drawing.Point(174, 397);
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.Size = new System.Drawing.Size(75, 27);
            this.btn_Löschen.TabIndex = 6;
            this.btn_Löschen.Text = "Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label7.Location = new System.Drawing.Point(23, 216);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(228, 19);
            this.label7.TabIndex = 8;
            this.label7.Text = "Liste der Wärmepumpen im Projekt:";
            // 
            // btn_Ansicht
            // 
            this.btn_Ansicht.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Ansicht.Location = new System.Drawing.Point(174, 439);
            this.btn_Ansicht.Name = "btn_Ansicht";
            this.btn_Ansicht.Size = new System.Drawing.Size(75, 27);
            this.btn_Ansicht.TabIndex = 48;
            this.btn_Ansicht.Text = "Ansicht";
            this.btn_Ansicht.UseVisualStyleBackColor = true;
            this.btn_Ansicht.Click += new System.EventHandler(this.btn_Ansicht_Click);
            // 
            // btn_Uebernehmen
            // 
            this.btn_Uebernehmen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Uebernehmen.Location = new System.Drawing.Point(174, 480);
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.Size = new System.Drawing.Size(75, 27);
            this.btn_Uebernehmen.TabIndex = 28;
            this.btn_Uebernehmen.Text = "Ändern";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label1.Location = new System.Drawing.Point(38, 399);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(130, 21);
            this.label1.TabIndex = 50;
            this.label1.Text = "Eintrag löschen";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label2.Location = new System.Drawing.Point(38, 441);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(130, 21);
            this.label2.TabIndex = 51;
            this.label2.Text = "Detailansicht";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.White;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label4.Location = new System.Drawing.Point(38, 482);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(130, 21);
            this.label4.TabIndex = 52;
            this.label4.Text = "Eintrag ändern";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.White;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.label5.Location = new System.Drawing.Point(38, 356);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(130, 21);
            this.label5.TabIndex = 56;
            this.label5.Text = "Neuer Eintrag";
            // 
            // btn_Neu
            // 
            this.btn_Neu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btn_Neu.Location = new System.Drawing.Point(174, 354);
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.Size = new System.Drawing.Size(75, 27);
            this.btn_Neu.TabIndex = 55;
            this.btn_Neu.Text = "Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // textBox_WP
            // 
            this.textBox_WP.Enabled = false;
            this.textBox_WP.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.textBox_WP.Location = new System.Drawing.Point(303, 63);
            this.textBox_WP.Name = "textBox_WP";
            this.textBox_WP.Size = new System.Drawing.Size(215, 29);
            this.textBox_WP.TabIndex = 54;
            this.textBox_WP.Visible = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(4, 45);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(143, 127);
            this.pictureBox1.TabIndex = 57;
            this.pictureBox1.TabStop = false;
            // 
            // Wizard_WP
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(583, 532);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.textBox_WP);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Ansicht);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btn_Löschen);
            this.Controls.Add(this.listView_WP);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Uebernehmen);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Wizard_WP";
            this.Text = "frm1";
            this.Load += new System.EventHandler(this.Wizard_WP_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Type;
        private System.Windows.Forms.ListView listView_WP;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btn_Uebernehmen;
        private System.Windows.Forms.Button btn_Ansicht;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox_WP;
    }
}