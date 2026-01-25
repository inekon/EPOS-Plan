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
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.Name = "label_Type";
            // 
            // listView_WP
            // 
            resources.ApplyResources(this.listView_WP, "listView_WP");
            this.listView_WP.FullRowSelect = true;
            this.listView_WP.GridLines = true;
            this.listView_WP.HideSelection = false;
            this.listView_WP.MultiSelect = false;
            this.listView_WP.Name = "listView_WP";
            this.listView_WP.UseCompatibleStateImageBehavior = false;
            this.listView_WP.SelectedIndexChanged += new System.EventHandler(this.listView_WP_SelectedIndexChanged);
            this.listView_WP.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseDoubleClick);
            // 
            // btn_Löschen
            // 
            resources.ApplyResources(this.btn_Löschen, "btn_Löschen");
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // btn_Ansicht
            // 
            resources.ApplyResources(this.btn_Ansicht, "btn_Ansicht");
            this.btn_Ansicht.Name = "btn_Ansicht";
            this.btn_Ansicht.UseVisualStyleBackColor = true;
            this.btn_Ansicht.Click += new System.EventHandler(this.btn_Ansicht_Click);
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(this.btn_Uebernehmen, "btn_Uebernehmen");
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // textBox_WP
            // 
            resources.ApplyResources(this.textBox_WP, "textBox_WP");
            this.textBox_WP.Name = "textBox_WP";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // Wizard_WP
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
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