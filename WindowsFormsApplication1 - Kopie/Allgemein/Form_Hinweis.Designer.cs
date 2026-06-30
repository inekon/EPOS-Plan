namespace WindowsFormsApplication1
{
    partial class Form_Hinweis
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Hinweis));
            button1 = new System.Windows.Forms.Button();
            label_Hinweis = new System.Windows.Forms.Label();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.None;
            button1.AutoSize = true;
            button1.Location = new System.Drawing.Point(153, 99);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(58, 31);
            button1.TabIndex = 1;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label_Hinweis
            // 
            label_Hinweis.AutoSize = true;
            label_Hinweis.Font = new System.Drawing.Font("Segoe UI", 10F);
            label_Hinweis.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            label_Hinweis.Location = new System.Drawing.Point(82, 26);
            label_Hinweis.Name = "label_Hinweis";
            label_Hinweis.Size = new System.Drawing.Size(52, 19);
            label_Hinweis.TabIndex = 2;
            label_Hinweis.Text = "Projekt";
            label_Hinweis.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.info_1459077_12801;
            pictureBox1.Location = new System.Drawing.Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(50, 50);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // Form_Hinweis
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(361, 155);
            ControlBox = false;
            Controls.Add(pictureBox1);
            Controls.Add(label_Hinweis);
            Controls.Add(button1);
            Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "Form_Hinweis";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Hinweis";
            Load += Form_Hinweis_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label_Hinweis;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}