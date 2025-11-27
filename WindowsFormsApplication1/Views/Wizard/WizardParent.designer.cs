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
            this.pnlLeft = new System.Windows.Forms.Panel();
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
            this.button_NeuProjekt = new System.Windows.Forms.Button();
            this.pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_App)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlLeft
            // 
            this.pnlLeft.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.pnlLeft.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pnlLeft.Controls.Add(this.button_NeuProjekt);
            this.pnlLeft.Controls.Add(this.label_Projekt);
            this.pnlLeft.Controls.Add(this.listBox_Projekte);
            this.pnlLeft.Controls.Add(this.pictureBox_App);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlLeft.Location = new System.Drawing.Point(0, 0);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Size = new System.Drawing.Size(174, 654);
            this.pnlLeft.TabIndex = 0;
            // 
            // label_Projekt
            // 
            this.label_Projekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label_Projekt.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Projekt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label_Projekt.Location = new System.Drawing.Point(12, 180);
            this.label_Projekt.Name = "label_Projekt";
            this.label_Projekt.Size = new System.Drawing.Size(145, 44);
            this.label_Projekt.TabIndex = 2;
            this.label_Projekt.Text = "bestehendes Projekt auswählen";
            // 
            // listBox_Projekte
            // 
            this.listBox_Projekte.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.listBox_Projekte.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.listBox_Projekte.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.listBox_Projekte.FormattingEnabled = true;
            this.listBox_Projekte.HorizontalScrollbar = true;
            this.listBox_Projekte.ItemHeight = 20;
            this.listBox_Projekte.Location = new System.Drawing.Point(12, 225);
            this.listBox_Projekte.Name = "listBox_Projekte";
            this.listBox_Projekte.Size = new System.Drawing.Size(150, 284);
            this.listBox_Projekte.TabIndex = 1;
            this.listBox_Projekte.SelectedIndexChanged += new System.EventHandler(this.listBox_Projekte_SelectedIndexChanged);
            // 
            // pictureBox_App
            // 
            this.pictureBox_App.Image = global::WindowsFormsApplication1.Properties.Resources.LogoInekon;
            this.pictureBox_App.Location = new System.Drawing.Point(12, 12);
            this.pictureBox_App.Name = "pictureBox_App";
            this.pictureBox_App.Size = new System.Drawing.Size(145, 121);
            this.pictureBox_App.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_App.TabIndex = 0;
            this.pictureBox_App.TabStop = false;
            this.pictureBox_App.Click += new System.EventHandler(this.pictureBox_App_Click);
            // 
            // pnlBottom
            // 
            this.pnlBottom.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pnlBottom.Controls.Add(this.btnSpeichern);
            this.pnlBottom.Controls.Add(this.tableLayoutPanel1);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 654);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(10);
            this.pnlBottom.Size = new System.Drawing.Size(1049, 57);
            this.pnlBottom.TabIndex = 1;
            // 
            // btnSpeichern
            // 
            this.btnSpeichern.Enabled = false;
            this.btnSpeichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btnSpeichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSpeichern.Location = new System.Drawing.Point(453, 13);
            this.btnSpeichern.Name = "btnSpeichern";
            this.btnSpeichern.Size = new System.Drawing.Size(121, 28);
            this.btnSpeichern.TabIndex = 2;
            this.btnSpeichern.Text = "Speichern";
            this.btnSpeichern.UseVisualStyleBackColor = true;
            this.btnSpeichern.Click += new System.EventHandler(this.btnSpeichern_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.btnCancel, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnNext, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnBack, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(796, 10);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(243, 37);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Location = new System.Drawing.Point(158, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(70, 25);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnNext
            // 
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.Location = new System.Drawing.Point(82, 6);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(70, 25);
            this.btnNext.TabIndex = 1;
            this.btnNext.Text = "&Next >";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnBack
            // 
            this.btnBack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBack.Location = new System.Drawing.Point(6, 6);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(70, 25);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "< &Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // pnlContent
            // 
            this.pnlContent.AutoSize = true;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(174, 0);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(10);
            this.pnlContent.Size = new System.Drawing.Size(875, 654);
            this.pnlContent.TabIndex = 2;
            // 
            // button_NeuProjekt
            // 
            this.button_NeuProjekt.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.button_NeuProjekt.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_NeuProjekt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.button_NeuProjekt.Location = new System.Drawing.Point(12, 521);
            this.button_NeuProjekt.Name = "button_NeuProjekt";
            this.button_NeuProjekt.Size = new System.Drawing.Size(150, 29);
            this.button_NeuProjekt.TabIndex = 0;
            this.button_NeuProjekt.Text = "Neues Projekt...";
            this.button_NeuProjekt.UseVisualStyleBackColor = false;
            this.button_NeuProjekt.Click += new System.EventHandler(this.button_NeuProjekt_Click);
            // 
            // WizardParent
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1049, 711);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlBottom);
            this.Name = "WizardParent";
            this.Text = "Projekt Wizard";
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

