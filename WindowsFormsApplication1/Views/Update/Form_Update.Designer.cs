namespace WindowsFormsApplication1
{
    partial class Form_Update
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
            this.checkBox_Stammdaten = new System.Windows.Forms.CheckBox();
            this.btn_Update = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btn_DB = new System.Windows.Forms.Button();
            this.textBox_DB = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // checkBox_Stammdaten
            // 
            this.checkBox_Stammdaten.AutoSize = true;
            this.checkBox_Stammdaten.Location = new System.Drawing.Point(113, 112);
            this.checkBox_Stammdaten.Name = "checkBox_Stammdaten";
            this.checkBox_Stammdaten.Size = new System.Drawing.Size(101, 17);
            this.checkBox_Stammdaten.TabIndex = 0;
            this.checkBox_Stammdaten.Text = "mit Stammdaten";
            this.checkBox_Stammdaten.UseVisualStyleBackColor = true;
            // 
            // btn_Update
            // 
            this.btn_Update.Location = new System.Drawing.Point(32, 112);
            this.btn_Update.Name = "btn_Update";
            this.btn_Update.Size = new System.Drawing.Size(75, 23);
            this.btn_Update.TabIndex = 1;
            this.btn_Update.Text = "Update";
            this.btn_Update.UseVisualStyleBackColor = true;
            this.btn_Update.Click += new System.EventHandler(this.btn_Update_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(32, 142);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(424, 15);
            this.progressBar1.TabIndex = 2;
            this.progressBar1.Visible = false;
            // 
            // btn_DB
            // 
            this.btn_DB.Location = new System.Drawing.Point(32, 12);
            this.btn_DB.Name = "btn_DB";
            this.btn_DB.Size = new System.Drawing.Size(128, 23);
            this.btn_DB.TabIndex = 3;
            this.btn_DB.Text = "DB auswählen...";
            this.btn_DB.UseVisualStyleBackColor = true;
            this.btn_DB.Click += new System.EventHandler(this.btn_DB_Click);
            // 
            // textBox_DB
            // 
            this.textBox_DB.Location = new System.Drawing.Point(32, 42);
            this.textBox_DB.Multiline = true;
            this.textBox_DB.Name = "textBox_DB";
            this.textBox_DB.Size = new System.Drawing.Size(424, 49);
            this.textBox_DB.TabIndex = 4;
            // 
            // Form_Update
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(470, 174);
            this.Controls.Add(this.textBox_DB);
            this.Controls.Add(this.btn_DB);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btn_Update);
            this.Controls.Add(this.checkBox_Stammdaten);
            this.Name = "Form_Update";
            this.Text = "Datenbank Update";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox_Stammdaten;
        private System.Windows.Forms.Button btn_Update;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btn_DB;
        private System.Windows.Forms.TextBox textBox_DB;
    }
}