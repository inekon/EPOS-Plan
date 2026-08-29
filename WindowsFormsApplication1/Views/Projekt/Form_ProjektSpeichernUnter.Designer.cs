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
            label_Beschreibung = new System.Windows.Forms.Label();
            textBox_Beschreibung = new System.Windows.Forms.TextBox();
            label_Kunde = new System.Windows.Forms.Label();
            textBox_Kunde = new System.Windows.Forms.TextBox();
            label_Bearbeiter = new System.Windows.Forms.Label();
            textBox_Bearbeiter = new System.Windows.Forms.TextBox();
            panel_Fortschritt = new System.Windows.Forms.Panel();
            lbl_Fortschritt = new System.Windows.Forms.Label();
            progressBar_Duplizieren = new System.Windows.Forms.ProgressBar();
            btn_Help = new System.Windows.Forms.Button();
            panel_Fortschritt.SuspendLayout();
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
            // label_Beschreibung
            // 
            resources.ApplyResources(label_Beschreibung, "label_Beschreibung");
            label_Beschreibung.Name = "label_Beschreibung";
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(textBox_Beschreibung, "textBox_Beschreibung");
            textBox_Beschreibung.Multiline = true;
            textBox_Beschreibung.Name = "textBox_Beschreibung";
            textBox_Beschreibung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            // 
            // label_Kunde
            // 
            resources.ApplyResources(label_Kunde, "label_Kunde");
            label_Kunde.Name = "label_Kunde";
            // 
            // textBox_Kunde
            // 
            resources.ApplyResources(textBox_Kunde, "textBox_Kunde");
            textBox_Kunde.Name = "textBox_Kunde";
            // 
            // label_Bearbeiter
            // 
            resources.ApplyResources(label_Bearbeiter, "label_Bearbeiter");
            label_Bearbeiter.Name = "label_Bearbeiter";
            // 
            // textBox_Bearbeiter
            // 
            resources.ApplyResources(textBox_Bearbeiter, "textBox_Bearbeiter");
            textBox_Bearbeiter.Name = "textBox_Bearbeiter";
            // 
            // panel_Fortschritt
            // 
            panel_Fortschritt.Controls.Add(progressBar_Duplizieren);
            panel_Fortschritt.Controls.Add(lbl_Fortschritt);
            panel_Fortschritt.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel_Fortschritt.Name = "panel_Fortschritt";
            panel_Fortschritt.Padding = new System.Windows.Forms.Padding(10, 4, 10, 6);
            panel_Fortschritt.Size = new System.Drawing.Size(400, 46);
            panel_Fortschritt.TabIndex = 6;
            panel_Fortschritt.Visible = false;
            // 
            // lbl_Fortschritt
            // 
            lbl_Fortschritt.Dock = System.Windows.Forms.DockStyle.Top;
            lbl_Fortschritt.Name = "lbl_Fortschritt";
            lbl_Fortschritt.Size = new System.Drawing.Size(380, 18);
            lbl_Fortschritt.TabIndex = 0;
            // 
            // progressBar_Duplizieren
            // 
            progressBar_Duplizieren.Dock = System.Windows.Forms.DockStyle.Bottom;
            progressBar_Duplizieren.Name = "progressBar_Duplizieren";
            progressBar_Duplizieren.Size = new System.Drawing.Size(380, 20);
            progressBar_Duplizieren.TabIndex = 1;
            // 
            // btn_Help
            // 
            btn_Help.BackColor = System.Drawing.Color.Transparent;
            btn_Help.BackgroundImage = Properties.Resources.help_icon;
            btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            btn_Help.FlatAppearance.BorderSize = 0;
            btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_Help.Location = new System.Drawing.Point(496, 17);
            btn_Help.Name = "btn_Help";
            btn_Help.Size = new System.Drawing.Size(28, 28);
            btn_Help.TabStop = false;
            btn_Help.UseVisualStyleBackColor = false;
            // 
            // Form_ProjektSpeichernUnter
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(btn_Help);
            Controls.Add(textBox_Bearbeiter);
            Controls.Add(label_Bearbeiter);
            Controls.Add(textBox_Kunde);
            Controls.Add(label_Kunde);
            Controls.Add(textBox_Beschreibung);
            Controls.Add(label_Beschreibung);
            Controls.Add(textBox_NeuerProjektName);
            Controls.Add(label2);
            Controls.Add(listView_Projekt);
            Controls.Add(label1);
            Controls.Add(button_Abbrechen);
            Controls.Add(button_Open);
            Controls.Add(panel_Fortschritt);
            Name = "Form_ProjektSpeichernUnter";
            Load += Form_ProjektSpeichernUnter_Load;
            panel_Fortschritt.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Button button_Open;
        private System.Windows.Forms.Button button_Abbrechen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView_Projekt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_NeuerProjektName;
        private System.Windows.Forms.Label label_Beschreibung;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label_Kunde;
        private System.Windows.Forms.TextBox textBox_Kunde;
        private System.Windows.Forms.Label label_Bearbeiter;
        private System.Windows.Forms.TextBox textBox_Bearbeiter;
        private System.Windows.Forms.Panel panel_Fortschritt;
        private System.Windows.Forms.Label lbl_Fortschritt;
        private System.Windows.Forms.ProgressBar progressBar_Duplizieren;
    }
}