namespace WindowsFormsApplication1
{
    partial class Form_Main
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_Projekt = new System.Windows.Forms.TextBox();
            this.textBox_Klimaregion = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(56, 123);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Projekt geöffnet";
            // 
            // textBox_Projekt
            // 
            this.textBox_Projekt.Enabled = false;
            this.textBox_Projekt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Projekt.Location = new System.Drawing.Point(184, 123);
            this.textBox_Projekt.Name = "textBox_Projekt";
            this.textBox_Projekt.Size = new System.Drawing.Size(286, 26);
            this.textBox_Projekt.TabIndex = 2;
            // 
            // textBox_Klimaregion
            // 
            this.textBox_Klimaregion.Enabled = false;
            this.textBox_Klimaregion.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Klimaregion.Location = new System.Drawing.Point(184, 167);
            this.textBox_Klimaregion.Name = "textBox_Klimaregion";
            this.textBox_Klimaregion.Size = new System.Drawing.Size(286, 26);
            this.textBox_Klimaregion.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(56, 170);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Klimaregion";
            // 
            // Form_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightGray;
            this.ClientSize = new System.Drawing.Size(709, 496);
            this.ControlBox = false;
            this.Controls.Add(this.textBox_Klimaregion);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_Projekt);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form_Main";
            this.Text = "Hauptformular";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_Projekt;
        private System.Windows.Forms.TextBox textBox_Klimaregion;
        private System.Windows.Forms.Label label2;
    }
}