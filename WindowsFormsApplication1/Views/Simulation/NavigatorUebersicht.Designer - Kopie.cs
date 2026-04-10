namespace WindowsFormsApplication1
{
    partial class NavigatorUebersicht
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label_3 = new System.Windows.Forms.Label();
            this.label_1 = new System.Windows.Forms.Label();
            this.label_2 = new System.Windows.Forms.Label();
            this.label_4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label_3
            // 
            this.label_3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_3.ForeColor = System.Drawing.Color.Black;
            this.label_3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_3.Location = new System.Drawing.Point(41, 133);
            this.label_3.Name = "label_3";
            this.label_3.Size = new System.Drawing.Size(154, 17);
            this.label_3.TabIndex = 287;
            this.label_3.Text = "Wärmebedarfsdeckung:";
            // 
            // label_1
            // 
            this.label_1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_1.ForeColor = System.Drawing.Color.Black;
            this.label_1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_1.Location = new System.Drawing.Point(352, 32);
            this.label_1.Name = "label_1";
            this.label_1.Size = new System.Drawing.Size(140, 17);
            this.label_1.TabIndex = 288;
            this.label_1.Text = "Strombedarfsdeckung:";
            this.label_1.Visible = false;
            // 
            // label_2
            // 
            this.label_2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_2.ForeColor = System.Drawing.Color.Black;
            this.label_2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_2.Location = new System.Drawing.Point(41, 32);
            this.label_2.Name = "label_2";
            this.label_2.Size = new System.Drawing.Size(154, 17);
            this.label_2.TabIndex = 289;
            this.label_2.Text = "Wärmebedarfsdeckung:";
            this.label_2.Visible = false;
            // 
            // label_4
            // 
            this.label_4.AutoSize = true;
            this.label_4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Underline);
            this.label_4.ForeColor = System.Drawing.Color.Black;
            this.label_4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_4.Location = new System.Drawing.Point(352, 133);
            this.label_4.Name = "label_4";
            this.label_4.Size = new System.Drawing.Size(140, 17);
            this.label_4.TabIndex = 290;
            this.label_4.Text = "Strombedarfsdeckung:";
            // 
            // NavigatorUebersicht
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.label_4);
            this.Controls.Add(this.label_2);
            this.Controls.Add(this.label_1);
            this.Controls.Add(this.label_3);
            this.Name = "NavigatorUebersicht";
            this.Size = new System.Drawing.Size(990, 628);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.NavigatorUebersicht_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label_3;
        private System.Windows.Forms.Label label_1;
        private System.Windows.Forms.Label label_2;
        private System.Windows.Forms.Label label_4;
    }
}
