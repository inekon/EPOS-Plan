namespace WindowsFormsApplication1
{
    partial class WizardParent
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WizardParent));
            pnlLeft = new System.Windows.Forms.Panel();
            button_ProjektOeffnen = new System.Windows.Forms.Button();
            label_Projekt = new System.Windows.Forms.Label();
            ucProjektAuswahl = new ProjektAuswahl();
            pnlBottom = new System.Windows.Forms.Panel();
            btnNext = new System.Windows.Forms.Button();
            btnBack = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            pnlContent = new System.Windows.Forms.Panel();
            btn_Help = new System.Windows.Forms.Button();
            pnlLeft.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            //
            // pnlLeft
            //
            pnlLeft.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(pnlLeft, "pnlLeft");
            pnlLeft.Controls.Add(btn_Help);
            pnlLeft.Controls.Add(button_ProjektOeffnen);
            pnlLeft.Controls.Add(label_Projekt);
            pnlLeft.Controls.Add(ucProjektAuswahl);
            pnlLeft.Name = "pnlLeft";
            //
            // button_ProjektOeffnen
            //
            button_ProjektOeffnen.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(button_ProjektOeffnen, "button_ProjektOeffnen");
            button_ProjektOeffnen.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            button_ProjektOeffnen.Name = "button_ProjektOeffnen";
            button_ProjektOeffnen.UseVisualStyleBackColor = false;
            button_ProjektOeffnen.Click += button_ProjektOeffnen_Click;
            //
            // label_Projekt
            //
            label_Projekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Projekt, "label_Projekt");
            label_Projekt.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            label_Projekt.Name = "label_Projekt";
            //
            // ucProjektAuswahl
            //
            ucProjektAuswahl.AutomatischeVorauswahl = false;
            ucProjektAuswahl.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(ucProjektAuswahl, "ucProjektAuswahl");
            ucProjektAuswahl.Name = "ucProjektAuswahl";
            ucProjektAuswahl.NurNamensspalte = true;
            ucProjektAuswahl.MarkierungGeaendert += ucProjektAuswahl_MarkierungGeaendert;
            ucProjektAuswahl.ProjektGewaehlt += ucProjektAuswahl_ProjektGewaehlt;
            //
            // pnlBottom
            //
            pnlBottom.BackColor = System.Drawing.Color.White;
            pnlBottom.Controls.Add(btnNext);
            pnlBottom.Controls.Add(btnBack);
            pnlBottom.Controls.Add(btnCancel);
            resources.ApplyResources(pnlBottom, "pnlBottom");
            pnlBottom.Name = "pnlBottom";
            //
            // btnNext
            //
            resources.ApplyResources(btnNext, "btnNext");
            btnNext.Name = "btnNext";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            //
            // btnBack
            //
            resources.ApplyResources(btnBack, "btnBack");
            btnBack.Name = "btnBack";
            btnBack.UseVisualStyleBackColor = true;
            btnBack.Click += btnBack_Click;
            //
            // btnCancel
            //
            resources.ApplyResources(btnCancel, "btnCancel");
            btnCancel.Name = "btnCancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // pnlContent
            //
            resources.ApplyResources(pnlContent, "pnlContent");
            pnlContent.BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            pnlContent.Name = "pnlContent";
            //
            // btn_Help
            //
            btn_Help.BackColor = System.Drawing.Color.Transparent;
            btn_Help.BackgroundImage = Properties.Resources.help_icon;
            btn_Help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btn_Help.Cursor = System.Windows.Forms.Cursors.Hand;
            btn_Help.FlatAppearance.BorderSize = 0;
            btn_Help.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_Help.Location = new System.Drawing.Point(14, 14);
            btn_Help.Name = "btn_Help";
            btn_Help.Size = new System.Drawing.Size(28, 28);
            btn_Help.TabStop = false;
            btn_Help.UseVisualStyleBackColor = false;
            //
            // WizardParent
            //
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            Controls.Add(pnlContent);
            Controls.Add(pnlLeft);
            Controls.Add(pnlBottom);
            Name = "WizardParent";
            Load += WizardParent_Load;
            pnlLeft.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Panel pnlContent;
        private ProjektAuswahl ucProjektAuswahl;
        private System.Windows.Forms.Label label_Projekt;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button button_ProjektOeffnen;
    }
}
