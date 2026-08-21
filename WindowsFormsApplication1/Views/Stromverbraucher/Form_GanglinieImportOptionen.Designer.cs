namespace WindowsFormsApplication1
{
    partial class Form_GanglinieImportOptionen
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
            this.lbl_Datei = new System.Windows.Forms.Label();
            this.grp_Format = new System.Windows.Forms.GroupBox();
            this.lbl_Trennzeichen = new System.Windows.Forms.Label();
            this.cbo_Trennzeichen = new System.Windows.Forms.ComboBox();
            this.lbl_Dezimal = new System.Windows.Forms.Label();
            this.cbo_Dezimal = new System.Windows.Forms.ComboBox();
            this.lbl_Wertspalte = new System.Windows.Forms.Label();
            this.cbo_Wertspalte = new System.Windows.Forms.ComboBox();
            this.lbl_Zeitspalte = new System.Windows.Forms.Label();
            this.cbo_Zeitspalte = new System.Windows.Forms.ComboBox();
            this.lbl_Einheit = new System.Windows.Forms.Label();
            this.cbo_Einheit = new System.Windows.Forms.ComboBox();
            this.lbl_Raster = new System.Windows.Forms.Label();
            this.cbo_Raster = new System.Windows.Forms.ComboBox();
            this.lbl_Konvention = new System.Windows.Forms.Label();
            this.cbo_Konvention = new System.Windows.Forms.ComboBox();
            this.lbl_Blatt = new System.Windows.Forms.Label();
            this.cbo_Blatt = new System.Windows.Forms.ComboBox();
            this.chk_Kopfzeile = new System.Windows.Forms.CheckBox();
            this.btn_Aktualisieren = new System.Windows.Forms.Button();
            this.grp_Vorschau = new System.Windows.Forms.GroupBox();
            this.listView_Vorschau = new System.Windows.Forms.ListView();
            this.lbl_Hinweis = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.grp_Format.SuspendLayout();
            this.grp_Vorschau.SuspendLayout();
            this.SuspendLayout();
            //
            // lbl_Datei
            //
            this.lbl_Datei.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Datei.AutoEllipsis = true;
            this.lbl_Datei.Location = new System.Drawing.Point(12, 10);
            this.lbl_Datei.Name = "lbl_Datei";
            this.lbl_Datei.Size = new System.Drawing.Size(796, 18);
            this.lbl_Datei.Text = "Datei: {0}";
            //
            // lbl_Trennzeichen
            //
            this.lbl_Trennzeichen.Location = new System.Drawing.Point(14, 30);
            this.lbl_Trennzeichen.Name = "lbl_Trennzeichen";
            this.lbl_Trennzeichen.Size = new System.Drawing.Size(170, 18);
            this.lbl_Trennzeichen.Text = "Trennzeichen:";
            //
            // cbo_Trennzeichen
            //
            this.cbo_Trennzeichen.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Trennzeichen.Location = new System.Drawing.Point(192, 26);
            this.cbo_Trennzeichen.Name = "cbo_Trennzeichen";
            this.cbo_Trennzeichen.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Dezimal
            //
            this.lbl_Dezimal.Location = new System.Drawing.Point(412, 30);
            this.lbl_Dezimal.Name = "lbl_Dezimal";
            this.lbl_Dezimal.Size = new System.Drawing.Size(140, 18);
            this.lbl_Dezimal.Text = "Dezimaltrenner:";
            //
            // cbo_Dezimal
            //
            this.cbo_Dezimal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Dezimal.Location = new System.Drawing.Point(560, 26);
            this.cbo_Dezimal.Name = "cbo_Dezimal";
            this.cbo_Dezimal.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Wertspalte
            //
            this.lbl_Wertspalte.Location = new System.Drawing.Point(14, 60);
            this.lbl_Wertspalte.Name = "lbl_Wertspalte";
            this.lbl_Wertspalte.Size = new System.Drawing.Size(170, 18);
            this.lbl_Wertspalte.Text = "Wertspalte:";
            //
            // cbo_Wertspalte
            //
            this.cbo_Wertspalte.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Wertspalte.Location = new System.Drawing.Point(192, 56);
            this.cbo_Wertspalte.Name = "cbo_Wertspalte";
            this.cbo_Wertspalte.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Zeitspalte
            //
            this.lbl_Zeitspalte.Location = new System.Drawing.Point(412, 60);
            this.lbl_Zeitspalte.Name = "lbl_Zeitspalte";
            this.lbl_Zeitspalte.Size = new System.Drawing.Size(140, 18);
            this.lbl_Zeitspalte.Text = "Zeitstempelspalte:";
            //
            // cbo_Zeitspalte
            //
            this.cbo_Zeitspalte.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Zeitspalte.Location = new System.Drawing.Point(560, 56);
            this.cbo_Zeitspalte.Name = "cbo_Zeitspalte";
            this.cbo_Zeitspalte.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Einheit
            //
            this.lbl_Einheit.Location = new System.Drawing.Point(14, 90);
            this.lbl_Einheit.Name = "lbl_Einheit";
            this.lbl_Einheit.Size = new System.Drawing.Size(170, 18);
            this.lbl_Einheit.Text = "Einheit der Werte:";
            //
            // cbo_Einheit
            //
            this.cbo_Einheit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Einheit.Location = new System.Drawing.Point(192, 86);
            this.cbo_Einheit.Name = "cbo_Einheit";
            this.cbo_Einheit.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Raster
            //
            this.lbl_Raster.Location = new System.Drawing.Point(412, 90);
            this.lbl_Raster.Name = "lbl_Raster";
            this.lbl_Raster.Size = new System.Drawing.Size(140, 18);
            this.lbl_Raster.Text = "Zeitraster:";
            //
            // cbo_Raster
            //
            this.cbo_Raster.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Raster.Location = new System.Drawing.Point(560, 86);
            this.cbo_Raster.Name = "cbo_Raster";
            this.cbo_Raster.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Konvention
            //
            this.lbl_Konvention.Location = new System.Drawing.Point(14, 120);
            this.lbl_Konvention.Name = "lbl_Konvention";
            this.lbl_Konvention.Size = new System.Drawing.Size(170, 18);
            this.lbl_Konvention.Text = "Zeitstempel bezeichnet:";
            //
            // cbo_Konvention
            //
            this.cbo_Konvention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Konvention.Location = new System.Drawing.Point(192, 116);
            this.cbo_Konvention.Name = "cbo_Konvention";
            this.cbo_Konvention.Size = new System.Drawing.Size(200, 22);
            //
            // lbl_Blatt
            //
            // Sichtbarkeit haengt vom Konstruktorparameter ab (nur Excel-Quellen) und
            // steht deshalb im Konstruktor-Nachlauf, nicht hier.
            this.lbl_Blatt.Location = new System.Drawing.Point(412, 120);
            this.lbl_Blatt.Name = "lbl_Blatt";
            this.lbl_Blatt.Size = new System.Drawing.Size(140, 18);
            this.lbl_Blatt.Text = "Tabellenblatt:";
            //
            // cbo_Blatt
            //
            this.cbo_Blatt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Blatt.Location = new System.Drawing.Point(560, 116);
            this.cbo_Blatt.Name = "cbo_Blatt";
            this.cbo_Blatt.Size = new System.Drawing.Size(200, 22);
            //
            // chk_Kopfzeile
            //
            this.chk_Kopfzeile.Location = new System.Drawing.Point(14, 148);
            this.chk_Kopfzeile.Name = "chk_Kopfzeile";
            this.chk_Kopfzeile.Size = new System.Drawing.Size(340, 22);
            this.chk_Kopfzeile.Text = "Erste Zeile ist eine Kopfzeile";
            //
            // btn_Aktualisieren
            //
            this.btn_Aktualisieren.Location = new System.Drawing.Point(560, 144);
            this.btn_Aktualisieren.Name = "btn_Aktualisieren";
            this.btn_Aktualisieren.Size = new System.Drawing.Size(200, 30);
            this.btn_Aktualisieren.Text = "Vorschau aktualisieren";
            this.btn_Aktualisieren.Click += new System.EventHandler(this.Aktualisieren_Click);
            //
            // grp_Format
            //
            this.grp_Format.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grp_Format.Location = new System.Drawing.Point(12, 32);
            this.grp_Format.Name = "grp_Format";
            this.grp_Format.Size = new System.Drawing.Size(796, 182);
            this.grp_Format.TabStop = false;
            this.grp_Format.Text = "Format";
            this.grp_Format.Controls.Add(this.lbl_Trennzeichen);
            this.grp_Format.Controls.Add(this.cbo_Trennzeichen);
            this.grp_Format.Controls.Add(this.lbl_Dezimal);
            this.grp_Format.Controls.Add(this.cbo_Dezimal);
            this.grp_Format.Controls.Add(this.lbl_Wertspalte);
            this.grp_Format.Controls.Add(this.cbo_Wertspalte);
            this.grp_Format.Controls.Add(this.lbl_Zeitspalte);
            this.grp_Format.Controls.Add(this.cbo_Zeitspalte);
            this.grp_Format.Controls.Add(this.lbl_Einheit);
            this.grp_Format.Controls.Add(this.cbo_Einheit);
            this.grp_Format.Controls.Add(this.lbl_Raster);
            this.grp_Format.Controls.Add(this.cbo_Raster);
            this.grp_Format.Controls.Add(this.lbl_Konvention);
            this.grp_Format.Controls.Add(this.cbo_Konvention);
            this.grp_Format.Controls.Add(this.lbl_Blatt);
            this.grp_Format.Controls.Add(this.cbo_Blatt);
            this.grp_Format.Controls.Add(this.chk_Kopfzeile);
            this.grp_Format.Controls.Add(this.btn_Aktualisieren);
            //
            // listView_Vorschau
            //
            // Die Spalten entstehen ausschliesslich zur Laufzeit in VorschauFuellen():
            // Zeilennummer plus je eine Spalte pro erkannter Dateispalte. Deshalb steht
            // hier kein einziger ColumnHeader.
            this.listView_Vorschau.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_Vorschau.FullRowSelect = true;
            this.listView_Vorschau.GridLines = true;
            this.listView_Vorschau.Location = new System.Drawing.Point(12, 20);
            this.listView_Vorschau.MultiSelect = false;
            this.listView_Vorschau.Name = "listView_Vorschau";
            this.listView_Vorschau.Size = new System.Drawing.Size(772, 228);
            this.listView_Vorschau.View = System.Windows.Forms.View.Details;
            //
            // grp_Vorschau
            //
            this.grp_Vorschau.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grp_Vorschau.Location = new System.Drawing.Point(12, 220);
            this.grp_Vorschau.Name = "grp_Vorschau";
            this.grp_Vorschau.Size = new System.Drawing.Size(796, 256);
            this.grp_Vorschau.TabStop = false;
            this.grp_Vorschau.Text = "Vorschau (erste {0} Zeilen)";
            this.grp_Vorschau.Controls.Add(this.listView_Vorschau);
            //
            // lbl_Hinweis
            //
            this.lbl_Hinweis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Hinweis.Location = new System.Drawing.Point(12, 482);
            this.lbl_Hinweis.Name = "lbl_Hinweis";
            this.lbl_Hinweis.Size = new System.Drawing.Size(556, 34);
            this.lbl_Hinweis.Text = "Die Vorbelegung stammt aus der Dateianalyse. Nach einer Änderung die Vorschau aktualisieren.";
            //
            // btn_OK
            //
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(576, 518);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(110, 30);
            this.btn_OK.Text = "OK";
            this.btn_OK.Click += new System.EventHandler(this.OK_Click);
            //
            // btn_Abbrechen
            //
            this.btn_Abbrechen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Abbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Abbrechen.Location = new System.Drawing.Point(698, 518);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(110, 30);
            this.btn_Abbrechen.Text = "Abbrechen";
            //
            // Form_GanglinieImportOptionen
            //
            this.AcceptButton = this.btn_OK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btn_Abbrechen;
            this.ClientSize = new System.Drawing.Size(820, 560);
            this.Controls.Add(this.lbl_Datei);
            this.Controls.Add(this.grp_Format);
            this.Controls.Add(this.grp_Vorschau);
            this.Controls.Add(this.lbl_Hinweis);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Abbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(660, 460);
            this.Name = "Form_GanglinieImportOptionen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Lastgang einlesen - Format und Vorschau";
            this.grp_Format.ResumeLayout(false);
            this.grp_Vorschau.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbl_Datei;
        private System.Windows.Forms.GroupBox grp_Format;
        private System.Windows.Forms.Label lbl_Trennzeichen;
        private System.Windows.Forms.ComboBox cbo_Trennzeichen;
        private System.Windows.Forms.Label lbl_Dezimal;
        private System.Windows.Forms.ComboBox cbo_Dezimal;
        private System.Windows.Forms.Label lbl_Wertspalte;
        private System.Windows.Forms.ComboBox cbo_Wertspalte;
        private System.Windows.Forms.Label lbl_Zeitspalte;
        private System.Windows.Forms.ComboBox cbo_Zeitspalte;
        private System.Windows.Forms.Label lbl_Einheit;
        private System.Windows.Forms.ComboBox cbo_Einheit;
        private System.Windows.Forms.Label lbl_Raster;
        private System.Windows.Forms.ComboBox cbo_Raster;
        private System.Windows.Forms.Label lbl_Konvention;
        private System.Windows.Forms.ComboBox cbo_Konvention;
        private System.Windows.Forms.Label lbl_Blatt;
        private System.Windows.Forms.ComboBox cbo_Blatt;
        private System.Windows.Forms.CheckBox chk_Kopfzeile;
        private System.Windows.Forms.Button btn_Aktualisieren;
        private System.Windows.Forms.GroupBox grp_Vorschau;
        private System.Windows.Forms.ListView listView_Vorschau;
        private System.Windows.Forms.Label lbl_Hinweis;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}
