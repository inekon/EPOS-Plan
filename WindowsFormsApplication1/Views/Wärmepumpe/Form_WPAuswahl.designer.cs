namespace WindowsFormsApplication1
{
    partial class Form_WPAuswahl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_WPAuswahl));
            this.listView_WP = new System.Windows.Forms.ListView();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_WP = new System.Windows.Forms.TextBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.label_Type = new System.Windows.Forms.Label();
            this.btn_Neu = new System.Windows.Forms.Button();
            this.btn_Löschen = new System.Windows.Forms.Button();
            this.btn_Uebernehmen = new System.Windows.Forms.Button();
            this.SuspendLayout();
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
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // textBox_WP
            // 
            resources.ApplyResources(this.textBox_WP, "textBox_WP");
            this.textBox_WP.Name = "textBox_WP";
            // 
            // btn_OK
            // 
            resources.ApplyResources(this.btn_OK, "btn_OK");
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(this.btn_Abbrechen, "btn_Abbrechen");
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            // 
            // label_Type
            // 
            resources.ApplyResources(this.label_Type, "label_Type");
            this.label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Type.Name = "label_Type";
            // 
            // btn_Neu
            // 
            resources.ApplyResources(this.btn_Neu, "btn_Neu");
            this.btn_Neu.Image = global::WindowsFormsApplication1.Properties.Resources.catalog_add;
            this.btn_Neu.Name = "btn_Neu";
            this.btn_Neu.UseVisualStyleBackColor = true;
            this.btn_Neu.Click += new System.EventHandler(this.btn_Neu_Click);
            // 
            // btn_Löschen
            // 
            resources.ApplyResources(this.btn_Löschen, "btn_Löschen");
            this.btn_Löschen.Image = global::WindowsFormsApplication1.Properties.Resources.catalog1;
            this.btn_Löschen.Name = "btn_Löschen";
            this.btn_Löschen.UseVisualStyleBackColor = true;
            this.btn_Löschen.Click += new System.EventHandler(this.btn_Löschen_Click);
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(this.btn_Uebernehmen, "btn_Uebernehmen");
            this.btn_Uebernehmen.Image = global::WindowsFormsApplication1.Properties.Resources.catalog_edit;
            this.btn_Uebernehmen.Name = "btn_Uebernehmen";
            this.btn_Uebernehmen.UseVisualStyleBackColor = true;
            this.btn_Uebernehmen.Click += new System.EventHandler(this.btn_Uebernehmen_Click);
            // 
            // Form_WPAuswahl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.btn_Abbrechen);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Neu);
            this.Controls.Add(this.textBox_WP);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.btn_Löschen);
            this.Controls.Add(this.listView_WP);
            this.Controls.Add(this.label_Type);
            this.Controls.Add(this.btn_Uebernehmen);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "Form_WPAuswahl";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListView listView_WP;
        private System.Windows.Forms.Button btn_Löschen;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btn_Uebernehmen;
        private System.Windows.Forms.Button btn_Neu;
        private System.Windows.Forms.TextBox textBox_WP;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Label label_Type;
    }
}