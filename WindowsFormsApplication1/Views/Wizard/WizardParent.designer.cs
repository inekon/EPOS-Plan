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
            button_NeuProjekt = new System.Windows.Forms.Button();
            label_Projekt = new System.Windows.Forms.Label();
            listBox_Projekte = new System.Windows.Forms.ListBox();
            pictureBox_App = new System.Windows.Forms.PictureBox();
            pnlBottom = new System.Windows.Forms.Panel();
            btnNext = new System.Windows.Forms.Button();
            btnBack = new System.Windows.Forms.Button();
            btnOeffnen = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            btnSpeichern = new System.Windows.Forms.Button();
            pnlContent = new System.Windows.Forms.Panel();
            btn_Help = new System.Windows.Forms.Button();
            pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox_App).BeginInit();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlLeft
            // 
            pnlLeft.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(pnlLeft, "pnlLeft");
            pnlLeft.Controls.Add(btn_Help);
            pnlLeft.Controls.Add(button_NeuProjekt);
            pnlLeft.Controls.Add(label_Projekt);
            pnlLeft.Controls.Add(listBox_Projekte);
            pnlLeft.Controls.Add(pictureBox_App);
            pnlLeft.Name = "pnlLeft";
            // 
            // button_NeuProjekt
            // 
            button_NeuProjekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(button_NeuProjekt, "button_NeuProjekt");
            button_NeuProjekt.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            button_NeuProjekt.Name = "button_NeuProjekt";
            button_NeuProjekt.UseVisualStyleBackColor = false;
            button_NeuProjekt.Click += button_NeuProjekt_Click;
            // 
            // label_Projekt
            // 
            label_Projekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(label_Projekt, "label_Projekt");
            label_Projekt.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            label_Projekt.Name = "label_Projekt";
            // 
            // listBox_Projekte
            // 
            listBox_Projekte.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            resources.ApplyResources(listBox_Projekte, "listBox_Projekte");
            listBox_Projekte.ForeColor = System.Drawing.Color.FromArgb(0, 0, 192);
            listBox_Projekte.FormattingEnabled = true;
            listBox_Projekte.Name = "listBox_Projekte";
            listBox_Projekte.SelectedIndexChanged += listBox_Projekte_SelectedIndexChanged;
            // 
            // pictureBox_App
            // 
            pictureBox_App.Image = Properties.Resources.LogoInekon;
            resources.ApplyResources(pictureBox_App, "pictureBox_App");
            pictureBox_App.Name = "pictureBox_App";
            pictureBox_App.TabStop = false;
            pictureBox_App.Click += pictureBox_App_Click;
            // 
            // pnlBottom
            // 
            pnlBottom.BackColor = System.Drawing.Color.White;
            pnlBottom.Controls.Add(btnNext);
            pnlBottom.Controls.Add(btnBack);
            pnlBottom.Controls.Add(btnOeffnen);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnSpeichern);
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
            // btnOeffnen
            // 
            resources.ApplyResources(btnOeffnen, "btnOeffnen");
            btnOeffnen.Name = "btnOeffnen";
            btnOeffnen.UseVisualStyleBackColor = true;
            btnOeffnen.Click += btnOeffnen_Click;
            // 
            // btnCancel
            // 
            resources.ApplyResources(btnCancel, "btnCancel");
            btnCancel.Name = "btnCancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSpeichern
            // 
            resources.ApplyResources(btnSpeichern, "btnSpeichern");
            btnSpeichern.Image = Properties.Resources.save_icon_36513;
            btnSpeichern.Name = "btnSpeichern";
            btnSpeichern.UseVisualStyleBackColor = true;
            btnSpeichern.Click += btnSpeichern_Click;
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
            btn_Help.Location = new System.Drawing.Point(14, 162);
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
            ((System.ComponentModel.ISupportInitialize)pictureBox_App).EndInit();
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Help;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.PictureBox pictureBox_App;
        private System.Windows.Forms.ListBox listBox_Projekte;
        private System.Windows.Forms.Label label_Projekt;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnSpeichern;
        private System.Windows.Forms.Button button_NeuProjekt;
        private System.Windows.Forms.Button btnOeffnen;
    }
}

