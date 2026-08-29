namespace WindowsFormsApplication1
{
    partial class AktionsKarte
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

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox_Bild = new System.Windows.Forms.PictureBox();
            label_Titel = new System.Windows.Forms.Label();
            label_Beschreibung = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox_Bild).BeginInit();
            SuspendLayout();
            //
            // pictureBox_Bild
            //
            pictureBox_Bild.BackColor = System.Drawing.Color.Transparent;
            pictureBox_Bild.Cursor = System.Windows.Forms.Cursors.Hand;
            pictureBox_Bild.Location = new System.Drawing.Point(170, 16);
            pictureBox_Bild.Name = "pictureBox_Bild";
            pictureBox_Bild.Size = new System.Drawing.Size(64, 64);
            pictureBox_Bild.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox_Bild.TabIndex = 0;
            pictureBox_Bild.TabStop = false;
            pictureBox_Bild.Visible = false;
            //
            // label_Titel
            //
            label_Titel.BackColor = System.Drawing.Color.Transparent;
            label_Titel.Cursor = System.Windows.Forms.Cursors.Hand;
            label_Titel.Font = new System.Drawing.Font("Segoe UI Semibold", 13F, System.Drawing.FontStyle.Bold);
            label_Titel.Location = new System.Drawing.Point(16, 47);
            label_Titel.Name = "label_Titel";
            label_Titel.Size = new System.Drawing.Size(372, 30);
            label_Titel.TabIndex = 1;
            label_Titel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // label_Beschreibung
            //
            label_Beschreibung.BackColor = System.Drawing.Color.Transparent;
            label_Beschreibung.Cursor = System.Windows.Forms.Cursors.Hand;
            label_Beschreibung.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            label_Beschreibung.Location = new System.Drawing.Point(16, 83);
            label_Beschreibung.Name = "label_Beschreibung";
            label_Beschreibung.Size = new System.Drawing.Size(372, 55);
            label_Beschreibung.TabIndex = 2;
            label_Beschreibung.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            //
            // AktionsKarte
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label_Beschreibung);
            Controls.Add(label_Titel);
            Controls.Add(pictureBox_Bild);
            Cursor = System.Windows.Forms.Cursors.Hand;
            Name = "AktionsKarte";
            Size = new System.Drawing.Size(404, 185);
            ((System.ComponentModel.ISupportInitialize)pictureBox_Bild).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox_Bild;
        private System.Windows.Forms.Label label_Titel;
        private System.Windows.Forms.Label label_Beschreibung;
    }
}
