namespace WindowsFormsApplication1
{
    partial class Form_HelpPopup
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
            this.linkLabel_Doku = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // linkLabel_Doku
            // 
            this.linkLabel_Doku.AutoSize = true;
            this.linkLabel_Doku.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.linkLabel_Doku.Location = new System.Drawing.Point(12, 9);
            this.linkLabel_Doku.Name = "linkLabel_Doku";
            this.linkLabel_Doku.Size = new System.Drawing.Size(65, 17);
            this.linkLabel_Doku.TabIndex = 0;
            this.linkLabel_Doku.TabStop = true;
            this.linkLabel_Doku.Text = "linkLabel1";
            // 
            // Form_HelpPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.LightYellow;
            this.ClientSize = new System.Drawing.Size(264, 45);
            this.Controls.Add(this.linkLabel_Doku);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form_HelpPopup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Form_HelpPopup";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel linkLabel_Doku;
    }
}