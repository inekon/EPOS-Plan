namespace WindowsFormsApplication1
{
    partial class Form_ProjektSpeichernUnter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_ProjektSpeichernUnter));
            button_Open = new System.Windows.Forms.Button();
            button_Abbrechen = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            listView_Projekt = new System.Windows.Forms.ListView();
            label2 = new System.Windows.Forms.Label();
            textBox_NeuerProjektName = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // button_Open
            // 
            resources.ApplyResources(button_Open, "button_Open");
            button_Open.Name = "button_Open";
            button_Open.UseVisualStyleBackColor = true;
            button_Open.Click += button_Open_Click;
            // 
            // button_Abbrechen
            // 
            resources.ApplyResources(button_Abbrechen, "button_Abbrechen");
            button_Abbrechen.Name = "button_Abbrechen";
            button_Abbrechen.UseVisualStyleBackColor = true;
            button_Abbrechen.Click += button_Abbrechen_Click;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // listView_Projekt
            // 
            resources.ApplyResources(listView_Projekt, "listView_Projekt");
            listView_Projekt.FullRowSelect = true;
            listView_Projekt.GridLines = true;
            listView_Projekt.Name = "listView_Projekt";
            listView_Projekt.UseCompatibleStateImageBehavior = false;
            listView_Projekt.SelectedIndexChanged += listView_Projekt_SelectedIndexChanged;
            listView_Projekt.DoubleClick += listView_Projekt_DoubleClick;
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // textBox_NeuerProjektName
            // 
            resources.ApplyResources(textBox_NeuerProjektName, "textBox_NeuerProjektName");
            textBox_NeuerProjektName.Name = "textBox_NeuerProjektName";
            // 
            // Form_ProjektSpeichernUnter
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(textBox_NeuerProjektName);
            Controls.Add(label2);
            Controls.Add(listView_Projekt);
            Controls.Add(label1);
            Controls.Add(button_Abbrechen);
            Controls.Add(button_Open);
            Name = "Form_ProjektSpeichernUnter";
            Load += Form_ProjektOpen_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_Open;
        private System.Windows.Forms.Button button_Abbrechen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView_Projekt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_NeuerProjektName;
    }
}