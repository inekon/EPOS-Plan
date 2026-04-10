namespace WindowsFormsApplication1
{
    partial class Form_ProjektOpen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ProjektOpen));
            this.button_Open = new System.Windows.Forms.Button();
            this.button_Abbrechen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listView_Projekt = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // button_Open
            // 
            resources.ApplyResources(this.button_Open, "button_Open");
            this.button_Open.Name = "button_Open";
            this.button_Open.UseVisualStyleBackColor = true;
            this.button_Open.Click += new System.EventHandler(this.button_Open_Click);
            // 
            // button_Abbrechen
            // 
            resources.ApplyResources(this.button_Abbrechen, "button_Abbrechen");
            this.button_Abbrechen.Name = "button_Abbrechen";
            this.button_Abbrechen.UseVisualStyleBackColor = true;
            this.button_Abbrechen.Click += new System.EventHandler(this.button_Abbrechen_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // listView_Projekt
            // 
            resources.ApplyResources(this.listView_Projekt, "listView_Projekt");
            this.listView_Projekt.FullRowSelect = true;
            this.listView_Projekt.GridLines = true;
            this.listView_Projekt.HideSelection = false;
            this.listView_Projekt.Name = "listView_Projekt";
            this.listView_Projekt.UseCompatibleStateImageBehavior = false;
            this.listView_Projekt.SelectedIndexChanged += new System.EventHandler(this.listView_Projekt_SelectedIndexChanged);
            this.listView_Projekt.DoubleClick += new System.EventHandler(this.listView_Projekt_DoubleClick);
            // 
            // Form_ProjektOpen
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.listView_Projekt);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_Abbrechen);
            this.Controls.Add(this.button_Open);
            this.Name = "Form_ProjektOpen";
            this.Load += new System.EventHandler(this.Form_ProjektOpen_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_Open;
        private System.Windows.Forms.Button button_Abbrechen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView_Projekt;
    }
}