namespace WindowsFormsApplication1
{
    partial class Form_ProjektOpen
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
            this.button_Open = new System.Windows.Forms.Button();
            this.button_Abbrechen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listView_Projekt = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // button_Open
            // 
            this.button_Open.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.button_Open.Location = new System.Drawing.Point(375, 272);
            this.button_Open.Name = "button_Open";
            this.button_Open.Size = new System.Drawing.Size(75, 33);
            this.button_Open.TabIndex = 2;
            this.button_Open.Text = "OK";
            this.button_Open.UseVisualStyleBackColor = true;
            this.button_Open.Click += new System.EventHandler(this.button_Open_Click);
            // 
            // button_Abbrechen
            // 
            this.button_Abbrechen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.button_Abbrechen.Location = new System.Drawing.Point(252, 272);
            this.button_Abbrechen.Name = "button_Abbrechen";
            this.button_Abbrechen.Size = new System.Drawing.Size(94, 33);
            this.button_Abbrechen.TabIndex = 3;
            this.button_Abbrechen.Text = "Abbrechen";
            this.button_Abbrechen.UseVisualStyleBackColor = true;
            this.button_Abbrechen.Click += new System.EventHandler(this.button_Abbrechen_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 19);
            this.label1.TabIndex = 4;
            this.label1.Text = "Projektauswahl:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // listView_Projekt
            // 
            this.listView_Projekt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listView_Projekt.FullRowSelect = true;
            this.listView_Projekt.GridLines = true;
            this.listView_Projekt.Location = new System.Drawing.Point(12, 44);
            this.listView_Projekt.Name = "listView_Projekt";
            this.listView_Projekt.Size = new System.Drawing.Size(438, 211);
            this.listView_Projekt.TabIndex = 5;
            this.listView_Projekt.UseCompatibleStateImageBehavior = false;
            this.listView_Projekt.SelectedIndexChanged += new System.EventHandler(this.listView_Projekt_SelectedIndexChanged);
            this.listView_Projekt.DoubleClick += new System.EventHandler(this.listView_Projekt_DoubleClick);
            // 
            // Form_ProjektOpen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(466, 317);
            this.Controls.Add(this.listView_Projekt);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_Abbrechen);
            this.Controls.Add(this.button_Open);
            this.Name = "Form_ProjektOpen";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Projekt Öffnen";
            this.Load += new System.EventHandler(this.Form_ProjektOpen_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_Open;
        private System.Windows.Forms.Button button_Abbrechen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView listView_Projekt;
    }
}