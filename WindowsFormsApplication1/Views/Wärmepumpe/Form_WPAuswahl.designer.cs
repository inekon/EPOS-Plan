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
            listView_WP = new System.Windows.Forms.ListView();
            label7 = new System.Windows.Forms.Label();
            textBox_WP = new System.Windows.Forms.TextBox();
            btn_OK = new System.Windows.Forms.Button();
            btn_Abbrechen = new System.Windows.Forms.Button();
            label_Type = new System.Windows.Forms.Label();
            btn_Neu = new System.Windows.Forms.Button();
            btn_Löschen = new System.Windows.Forms.Button();
            btn_Uebernehmen = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // listView_WP
            // 
            resources.ApplyResources(listView_WP, "listView_WP");
            listView_WP.FullRowSelect = true;
            listView_WP.GridLines = true;
            listView_WP.MultiSelect = false;
            listView_WP.Name = "listView_WP";
            listView_WP.UseCompatibleStateImageBehavior = false;
            listView_WP.SelectedIndexChanged += listView_WP_SelectedIndexChanged;
            listView_WP.MouseDoubleClick += listView_WP_MouseDoubleClick;
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.Name = "label7";
            // 
            // textBox_WP
            // 
            resources.ApplyResources(textBox_WP, "textBox_WP");
            textBox_WP.Name = "textBox_WP";
            // 
            // btn_OK
            // 
            resources.ApplyResources(btn_OK, "btn_OK");
            btn_OK.Name = "btn_OK";
            btn_OK.UseVisualStyleBackColor = true;
            btn_OK.Click += btn_OK_Click;
            // 
            // btn_Abbrechen
            // 
            resources.ApplyResources(btn_Abbrechen, "btn_Abbrechen");
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            btn_Abbrechen.Click += btn_Abbrechen_Click;
            // 
            // label_Type
            // 
            resources.ApplyResources(label_Type, "label_Type");
            label_Type.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            label_Type.Name = "label_Type";
            // 
            // btn_Neu
            // 
            resources.ApplyResources(btn_Neu, "btn_Neu");
            btn_Neu.Name = "btn_Neu";
            btn_Neu.UseVisualStyleBackColor = true;
            btn_Neu.Click += btn_Neu_Click;
            // 
            // btn_Löschen
            // 
            resources.ApplyResources(btn_Löschen, "btn_Löschen");
            btn_Löschen.Name = "btn_Löschen";
            btn_Löschen.UseVisualStyleBackColor = true;
            btn_Löschen.Click += btn_Löschen_Click;
            // 
            // btn_Uebernehmen
            // 
            resources.ApplyResources(btn_Uebernehmen, "btn_Uebernehmen");
            btn_Uebernehmen.Name = "btn_Uebernehmen";
            btn_Uebernehmen.UseVisualStyleBackColor = true;
            btn_Uebernehmen.Click += btn_Uebernehmen_Click;
            // 
            // Form_WPAuswahl
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            Controls.Add(btn_Abbrechen);
            Controls.Add(btn_OK);
            Controls.Add(btn_Neu);
            Controls.Add(textBox_WP);
            Controls.Add(label7);
            Controls.Add(btn_Löschen);
            Controls.Add(listView_WP);
            Controls.Add(label_Type);
            Controls.Add(btn_Uebernehmen);
            ForeColor = System.Drawing.Color.Black;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Name = "Form_WPAuswahl";
            ResumeLayout(false);
            PerformLayout();

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