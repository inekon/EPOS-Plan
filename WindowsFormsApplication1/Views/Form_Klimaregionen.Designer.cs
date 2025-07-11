namespace WindowsFormsApplication1
{
    partial class Form_Klimaregionen
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
            this.label3 = new System.Windows.Forms.Label();
            this.button_Klima = new System.Windows.Forms.Button();
            this.listBox_Klimaregionen = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 189);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Klima Datei";
            // 
            // button_Klima
            // 
            this.button_Klima.Location = new System.Drawing.Point(108, 184);
            this.button_Klima.Name = "button_Klima";
            this.button_Klima.Size = new System.Drawing.Size(110, 23);
            this.button_Klima.TabIndex = 9;
            this.button_Klima.Text = "Klimadaten einlesen";
            this.button_Klima.UseVisualStyleBackColor = true;
            this.button_Klima.Click += new System.EventHandler(this.button_Klima_Click);
            // 
            // listBox_Klimaregionen
            // 
            this.listBox_Klimaregionen.FormattingEnabled = true;
            this.listBox_Klimaregionen.Location = new System.Drawing.Point(26, 31);
            this.listBox_Klimaregionen.Name = "listBox_Klimaregionen";
            this.listBox_Klimaregionen.Size = new System.Drawing.Size(374, 147);
            this.listBox_Klimaregionen.TabIndex = 11;
            // 
            // Form_Klimaregionen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 223);
            this.Controls.Add(this.listBox_Klimaregionen);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button_Klima);
            this.Name = "Form_Klimaregionen";
            this.Text = "Klimaregionen";
            this.Load += new System.EventHandler(this.Form_Klimaregionen_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_Klima;
        private System.Windows.Forms.ListBox listBox_Klimaregionen;
    }
}