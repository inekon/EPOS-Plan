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
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.button_NeuProjekt = new System.Windows.Forms.Button();
            this.label_Projekt = new System.Windows.Forms.Label();
            this.listBox_Projekte = new System.Windows.Forms.ListBox();
            this.pictureBox_App = new System.Windows.Forms.PictureBox();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnSpeichern = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_App)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlLeft
            // 
            resources.ApplyResources(this.pnlLeft, "pnlLeft");
            this.pnlLeft.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.pnlLeft.Controls.Add(this.button_NeuProjekt);
            this.pnlLeft.Controls.Add(this.label_Projekt);
            this.pnlLeft.Controls.Add(this.listBox_Projekte);
            this.pnlLeft.Controls.Add(this.pictureBox_App);
            this.pnlLeft.Name = "pnlLeft";
            // 
            // button_NeuProjekt
            // 
            resources.ApplyResources(this.button_NeuProjekt, "button_NeuProjekt");
            this.button_NeuProjekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.button_NeuProjekt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.button_NeuProjekt.Name = "button_NeuProjekt";
            this.button_NeuProjekt.UseVisualStyleBackColor = false;
            this.button_NeuProjekt.Click += new System.EventHandler(this.button_NeuProjekt_Click);
            // 
            // label_Projekt
            // 
            resources.ApplyResources(this.label_Projekt, "label_Projekt");
            this.label_Projekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Projekt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label_Projekt.Name = "label_Projekt";
            // 
            // listBox_Projekte
            // 
            resources.ApplyResources(this.listBox_Projekte, "listBox_Projekte");
            this.listBox_Projekte.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.listBox_Projekte.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.listBox_Projekte.FormattingEnabled = true;
            this.listBox_Projekte.Name = "listBox_Projekte";
            this.listBox_Projekte.SelectedIndexChanged += new System.EventHandler(this.listBox_Projekte_SelectedIndexChanged);
            // 
            // pictureBox_App
            // 
            resources.ApplyResources(this.pictureBox_App, "pictureBox_App");
            this.pictureBox_App.Image = global::WindowsFormsApplication1.Properties.Resources.LogoInekon;
            this.pictureBox_App.Name = "pictureBox_App";
            this.pictureBox_App.TabStop = false;
            this.pictureBox_App.Click += new System.EventHandler(this.pictureBox_App_Click);
            // 
            // pnlBottom
            // 
            resources.ApplyResources(this.pnlBottom, "pnlBottom");
            this.pnlBottom.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pnlBottom.Controls.Add(this.btnSpeichern);
            this.pnlBottom.Controls.Add(this.tableLayoutPanel1);
            this.pnlBottom.Name = "pnlBottom";
            // 
            // btnSpeichern
            // 
            resources.ApplyResources(this.btnSpeichern, "btnSpeichern");
            this.btnSpeichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btnSpeichern.Name = "btnSpeichern";
            this.btnSpeichern.UseVisualStyleBackColor = true;
            this.btnSpeichern.Click += new System.EventHandler(this.btnSpeichern_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.btnCancel, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnNext, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnBack, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnNext
            // 
            resources.ApplyResources(this.btnNext, "btnNext");
            this.btnNext.Name = "btnNext";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnBack
            // 
            resources.ApplyResources(this.btnBack, "btnBack");
            this.btnBack.Name = "btnBack";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // pnlContent
            // 
            resources.ApplyResources(this.pnlContent, "pnlContent");
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pnlContent.Name = "pnlContent";
            // 
            // WizardParent
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlBottom);
            this.Name = "WizardParent";
            this.Load += new System.EventHandler(this.WizardParent_Load);
            this.pnlLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_App)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.PictureBox pictureBox_App;
        private System.Windows.Forms.ListBox listBox_Projekte;
        private System.Windows.Forms.Label label_Projekt;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnSpeichern;
        private System.Windows.Forms.Button button_NeuProjekt;
    }
}

