namespace WindowsFormsApplication1
{
    partial class Form_GanglinieProtokoll
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
            this.lbl_Kopf = new System.Windows.Forms.Label();
            this.listView_Protokoll = new System.Windows.Forms.ListView();
            this.columnHeader_Stufe = new System.Windows.Forms.ColumnHeader();
            this.columnHeader_Meldung = new System.Windows.Forms.ColumnHeader();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbl_Kopf
            // 
            this.lbl_Kopf.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Kopf.Location = new System.Drawing.Point(12, 10);
            this.lbl_Kopf.Name = "lbl_Kopf";
            this.lbl_Kopf.Size = new System.Drawing.Size(736, 34);
            this.lbl_Kopf.Text = "Die Reihe wurde angepasst oder es liegen Auffälligkeiten vor. Bitte prüfen und bestätigen.";
            // 
            // listView_Protokoll
            // 
            this.listView_Protokoll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_Protokoll.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_Stufe,
            this.columnHeader_Meldung});
            this.listView_Protokoll.FullRowSelect = true;
            this.listView_Protokoll.GridLines = true;
            this.listView_Protokoll.HideSelection = false;
            this.listView_Protokoll.Location = new System.Drawing.Point(12, 50);
            this.listView_Protokoll.MultiSelect = false;
            this.listView_Protokoll.Name = "listView_Protokoll";
            this.listView_Protokoll.Size = new System.Drawing.Size(736, 320);
            this.listView_Protokoll.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader_Stufe
            // 
            this.columnHeader_Stufe.Name = "columnHeader_Stufe";
            this.columnHeader_Stufe.Text = "Stufe";
            this.columnHeader_Stufe.Width = 90;
            // 
            // columnHeader_Meldung
            // 
            this.columnHeader_Meldung.Name = "columnHeader_Meldung";
            this.columnHeader_Meldung.Text = "Meldung";
            this.columnHeader_Meldung.Width = 620;
            // 
            // btn_OK
            // 
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(516, 378);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(110, 30);
            this.btn_OK.Text = "OK";
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Abbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Abbrechen.Location = new System.Drawing.Point(638, 378);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(110, 30);
            this.btn_Abbrechen.Text = "Abbrechen";
            // 
            // Form_GanglinieProtokoll
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btn_Abbrechen;
            this.ClientSize = new System.Drawing.Size(760, 420);
            this.Controls.Add(this.lbl_Kopf);
            this.Controls.Add(this.listView_Protokoll);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Abbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(520, 300);
            this.Name = "Form_GanglinieProtokoll";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Prüfprotokoll des Lastgangimports";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbl_Kopf;
        private System.Windows.Forms.ListView listView_Protokoll;
        private System.Windows.Forms.ColumnHeader columnHeader_Stufe;
        private System.Windows.Forms.ColumnHeader columnHeader_Meldung;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}
