namespace WindowsFormsApplication1
{
    partial class Form_Import
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Import));
            this.btn_Update = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btn_DB = new System.Windows.Forms.Button();
            this.textBox_DB = new System.Windows.Forms.TextBox();
            this.btn_Beenden = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Update
            // 
            resources.ApplyResources(this.btn_Update, "btn_Update");
            this.btn_Update.Name = "btn_Update";
            this.btn_Update.UseVisualStyleBackColor = true;
            this.btn_Update.Click += new System.EventHandler(this.btn_Update_Click);
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // btn_DB
            // 
            resources.ApplyResources(this.btn_DB, "btn_DB");
            this.btn_DB.Name = "btn_DB";
            this.btn_DB.UseVisualStyleBackColor = true;
            this.btn_DB.Click += new System.EventHandler(this.btn_DB_Click);
            // 
            // textBox_DB
            // 
            resources.ApplyResources(this.textBox_DB, "textBox_DB");
            this.textBox_DB.Name = "textBox_DB";
            // 
            // btn_Beenden
            // 
            resources.ApplyResources(this.btn_Beenden, "btn_Beenden");
            this.btn_Beenden.Name = "btn_Beenden";
            this.btn_Beenden.UseVisualStyleBackColor = true;
            this.btn_Beenden.Click += new System.EventHandler(this.btn_Beenden_Click);
            // 
            // Form_Import
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_Beenden);
            this.Controls.Add(this.textBox_DB);
            this.Controls.Add(this.btn_DB);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btn_Update);
            this.Name = "Form_Import";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_Update;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btn_DB;
        private System.Windows.Forms.TextBox textBox_DB;
        private System.Windows.Forms.Button btn_Beenden;
    }
}