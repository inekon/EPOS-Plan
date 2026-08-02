namespace WindowsFormsApplication1
{
    partial class Form_AdminSettings
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listBox_Rubriken = new System.Windows.Forms.ListBox();
            this.panel_Content = new System.Windows.Forms.Panel();
            this.panel_Export = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_DBImportPath = new System.Windows.Forms.TextBox();
            this.btn_DBImportBrowse = new System.Windows.Forms.Button();
            this.lbl_DBPath = new System.Windows.Forms.Label();
            this.txt_DBPath = new System.Windows.Forms.TextBox();
            this.btn_DBPathBrowse = new System.Windows.Forms.Button();
            this.lbl_DBExportPath = new System.Windows.Forms.Label();
            this.txt_DBExportPath = new System.Windows.Forms.TextBox();
            this.btn_DBExportBrowse = new System.Windows.Forms.Button();
            this.panel_Internet = new System.Windows.Forms.Panel();
            this.lbl_PVGIS = new System.Windows.Forms.Label();
            this.txt_PVGISUrl = new System.Windows.Forms.TextBox();
            this.lbl_OnlineDoku = new System.Windows.Forms.Label();
            this.txt_OnlineDokuUrl = new System.Windows.Forms.TextBox();
            this.lbl_WPPrefix = new System.Windows.Forms.Label();
            this.txt_WPPrefix = new System.Windows.Forms.TextBox();
            this.panel_Allgemein = new System.Windows.Forms.Panel();
            this.lbl_Allgemein = new System.Windows.Forms.Label();
            this.txt_AllgemeinPath = new System.Windows.Forms.TextBox();
            this.btn_AllgemeinBrowse = new System.Windows.Forms.Button();
            this.panel_Import = new System.Windows.Forms.Panel();
            this.lbl_VDIPath = new System.Windows.Forms.Label();
            this.txt_VDIPath = new System.Windows.Forms.TextBox();
            this.btn_VDIPathBrowse = new System.Windows.Forms.Button();
            this.panel_Buttons = new System.Windows.Forms.Panel();
            this.btn_Standardwerte = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_GEOCodUrl = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel_Content.SuspendLayout();
            this.panel_Export.SuspendLayout();
            this.panel_Internet.SuspendLayout();
            this.panel_Allgemein.SuspendLayout();
            this.panel_Import.SuspendLayout();
            this.panel_Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(10, 10);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listBox_Rubriken);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel_Content);
            this.splitContainer1.Size = new System.Drawing.Size(681, 387);
            this.splitContainer1.SplitterDistance = 179;
            this.splitContainer1.TabIndex = 0;
            // 
            // listBox_Rubriken
            // 
            this.listBox_Rubriken.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBox_Rubriken.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.listBox_Rubriken.IntegralHeight = false;
            this.listBox_Rubriken.ItemHeight = 15;
            this.listBox_Rubriken.Items.AddRange(new object[] {
            "VDI Datensätze",
            "Datenbank",
            "Web-Schnittstellen (API)",
            "Anwendung"});
            this.listBox_Rubriken.Location = new System.Drawing.Point(0, 0);
            this.listBox_Rubriken.Name = "listBox_Rubriken";
            this.listBox_Rubriken.Size = new System.Drawing.Size(178, 387);
            this.listBox_Rubriken.TabIndex = 0;
            // 
            // panel_Content
            // 
            this.panel_Content.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel_Content.Controls.Add(this.panel_Internet);
            this.panel_Content.Controls.Add(this.panel_Allgemein);
            this.panel_Content.Controls.Add(this.panel_Import);
            this.panel_Content.Controls.Add(this.panel_Export);
            this.panel_Content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Content.Location = new System.Drawing.Point(0, 0);
            this.panel_Content.Name = "panel_Content";
            this.panel_Content.Size = new System.Drawing.Size(498, 387);
            this.panel_Content.TabIndex = 0;
            // 
            // panel_Export
            // 
            this.panel_Export.Controls.Add(this.label1);
            this.panel_Export.Controls.Add(this.txt_DBImportPath);
            this.panel_Export.Controls.Add(this.btn_DBImportBrowse);
            this.panel_Export.Controls.Add(this.lbl_DBPath);
            this.panel_Export.Controls.Add(this.txt_DBPath);
            this.panel_Export.Controls.Add(this.btn_DBPathBrowse);
            this.panel_Export.Controls.Add(this.lbl_DBExportPath);
            this.panel_Export.Controls.Add(this.txt_DBExportPath);
            this.panel_Export.Controls.Add(this.btn_DBExportBrowse);
            this.panel_Export.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Export.Location = new System.Drawing.Point(0, 0);
            this.panel_Export.Name = "panel_Export";
            this.panel_Export.Padding = new System.Windows.Forms.Padding(15);
            this.panel_Export.Size = new System.Drawing.Size(496, 385);
            this.panel_Export.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(15, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(300, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "Standard Datenbank-Import-Pfad:";
            // 
            // txt_DBImportPath
            // 
            this.txt_DBImportPath.Location = new System.Drawing.Point(18, 116);
            this.txt_DBImportPath.Name = "txt_DBImportPath";
            this.txt_DBImportPath.Size = new System.Drawing.Size(360, 23);
            this.txt_DBImportPath.TabIndex = 4;
            // 
            // btn_DBImportBrowse
            // 
            this.btn_DBImportBrowse.Location = new System.Drawing.Point(385, 115);
            this.btn_DBImportBrowse.Name = "btn_DBImportBrowse";
            this.btn_DBImportBrowse.Size = new System.Drawing.Size(95, 25);
            this.btn_DBImportBrowse.TabIndex = 5;
            this.btn_DBImportBrowse.Text = "Durchsuchen...";
            // 
            // lbl_DBPath
            // 
            this.lbl_DBPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_DBPath.Location = new System.Drawing.Point(15, 162);
            this.lbl_DBPath.Name = "lbl_DBPath";
            this.lbl_DBPath.Size = new System.Drawing.Size(300, 20);
            this.lbl_DBPath.TabIndex = 6;
            this.lbl_DBPath.Text = "Standard Datenbank-Pfad:";
            // 
            // txt_DBPath
            // 
            this.txt_DBPath.Location = new System.Drawing.Point(18, 187);
            this.txt_DBPath.Name = "txt_DBPath";
            this.txt_DBPath.Size = new System.Drawing.Size(360, 23);
            this.txt_DBPath.TabIndex = 7;
            // 
            // btn_DBPathBrowse
            // 
            this.btn_DBPathBrowse.Location = new System.Drawing.Point(385, 186);
            this.btn_DBPathBrowse.Name = "btn_DBPathBrowse";
            this.btn_DBPathBrowse.Size = new System.Drawing.Size(95, 25);
            this.btn_DBPathBrowse.TabIndex = 8;
            this.btn_DBPathBrowse.Text = "Durchsuchen...";
            // 
            // lbl_DBExportPath
            // 
            this.lbl_DBExportPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_DBExportPath.Location = new System.Drawing.Point(15, 20);
            this.lbl_DBExportPath.Name = "lbl_DBExportPath";
            this.lbl_DBExportPath.Size = new System.Drawing.Size(300, 20);
            this.lbl_DBExportPath.TabIndex = 0;
            this.lbl_DBExportPath.Text = "Standard Datenbank-Export-Pfad:";
            // 
            // txt_DBExportPath
            // 
            this.txt_DBExportPath.Location = new System.Drawing.Point(18, 45);
            this.txt_DBExportPath.Name = "txt_DBExportPath";
            this.txt_DBExportPath.Size = new System.Drawing.Size(360, 23);
            this.txt_DBExportPath.TabIndex = 1;
            // 
            // btn_DBExportBrowse
            // 
            this.btn_DBExportBrowse.Location = new System.Drawing.Point(385, 44);
            this.btn_DBExportBrowse.Name = "btn_DBExportBrowse";
            this.btn_DBExportBrowse.Size = new System.Drawing.Size(95, 25);
            this.btn_DBExportBrowse.TabIndex = 2;
            this.btn_DBExportBrowse.Text = "Durchsuchen...";
            // 
            // panel_Internet
            // 
            this.panel_Internet.Controls.Add(this.label2);
            this.panel_Internet.Controls.Add(this.txt_GEOCodUrl);
            this.panel_Internet.Controls.Add(this.lbl_PVGIS);
            this.panel_Internet.Controls.Add(this.txt_PVGISUrl);
            this.panel_Internet.Controls.Add(this.lbl_OnlineDoku);
            this.panel_Internet.Controls.Add(this.txt_OnlineDokuUrl);
            this.panel_Internet.Controls.Add(this.lbl_WPPrefix);
            this.panel_Internet.Controls.Add(this.txt_WPPrefix);
            this.panel_Internet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Internet.Location = new System.Drawing.Point(0, 0);
            this.panel_Internet.Name = "panel_Internet";
            this.panel_Internet.Padding = new System.Windows.Forms.Padding(15);
            this.panel_Internet.Size = new System.Drawing.Size(496, 385);
            this.panel_Internet.TabIndex = 2;
            // 
            // lbl_PVGIS
            // 
            this.lbl_PVGIS.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_PVGIS.Location = new System.Drawing.Point(15, 20);
            this.lbl_PVGIS.Name = "lbl_PVGIS";
            this.lbl_PVGIS.Size = new System.Drawing.Size(300, 20);
            this.lbl_PVGIS.TabIndex = 0;
            this.lbl_PVGIS.Text = "PVGIS API Server URL:";
            // 
            // txt_PVGISUrl
            // 
            this.txt_PVGISUrl.Location = new System.Drawing.Point(18, 45);
            this.txt_PVGISUrl.Name = "txt_PVGISUrl";
            this.txt_PVGISUrl.Size = new System.Drawing.Size(460, 23);
            this.txt_PVGISUrl.TabIndex = 1;
            this.txt_PVGISUrl.Text = "https://re.jrc.ec.europa.eu/api/v5_2/";
            // 
            // lbl_OnlineDoku
            // 
            this.lbl_OnlineDoku.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_OnlineDoku.Location = new System.Drawing.Point(15, 169);
            this.lbl_OnlineDoku.Name = "lbl_OnlineDoku";
            this.lbl_OnlineDoku.Size = new System.Drawing.Size(300, 20);
            this.lbl_OnlineDoku.TabIndex = 2;
            this.lbl_OnlineDoku.Text = "Online Dokumentation Basis-URL:";
            // 
            // txt_OnlineDokuUrl
            // 
            this.txt_OnlineDokuUrl.Location = new System.Drawing.Point(18, 194);
            this.txt_OnlineDokuUrl.Name = "txt_OnlineDokuUrl";
            this.txt_OnlineDokuUrl.Size = new System.Drawing.Size(460, 23);
            this.txt_OnlineDokuUrl.TabIndex = 3;
            this.txt_OnlineDokuUrl.Text = "http://localhost:8080";
            // 
            // lbl_WPPrefix
            // 
            this.lbl_WPPrefix.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_WPPrefix.Location = new System.Drawing.Point(15, 239);
            this.lbl_WPPrefix.Name = "lbl_WPPrefix";
            this.lbl_WPPrefix.Size = new System.Drawing.Size(400, 20);
            this.lbl_WPPrefix.TabIndex = 4;
            this.lbl_WPPrefix.Text = "WordPress API-Präfix / REST-Base (z.B. help, pages, posts):";
            // 
            // txt_WPPrefix
            // 
            this.txt_WPPrefix.Location = new System.Drawing.Point(18, 264);
            this.txt_WPPrefix.Name = "txt_WPPrefix";
            this.txt_WPPrefix.Size = new System.Drawing.Size(150, 23);
            this.txt_WPPrefix.TabIndex = 5;
            this.txt_WPPrefix.Text = "help";
            // 
            // panel_Allgemein
            // 
            this.panel_Allgemein.Controls.Add(this.lbl_Allgemein);
            this.panel_Allgemein.Controls.Add(this.txt_AllgemeinPath);
            this.panel_Allgemein.Controls.Add(this.btn_AllgemeinBrowse);
            this.panel_Allgemein.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Allgemein.Location = new System.Drawing.Point(0, 0);
            this.panel_Allgemein.Name = "panel_Allgemein";
            this.panel_Allgemein.Padding = new System.Windows.Forms.Padding(15);
            this.panel_Allgemein.Size = new System.Drawing.Size(496, 385);
            this.panel_Allgemein.TabIndex = 3;
            // 
            // lbl_Allgemein
            // 
            this.lbl_Allgemein.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_Allgemein.Location = new System.Drawing.Point(15, 20);
            this.lbl_Allgemein.Name = "lbl_Allgemein";
            this.lbl_Allgemein.Size = new System.Drawing.Size(300, 20);
            this.lbl_Allgemein.TabIndex = 0;
            this.lbl_Allgemein.Text = "Allgemeiner Sicherungspfad / Temp-Ordner:";
            // 
            // txt_AllgemeinPath
            // 
            this.txt_AllgemeinPath.Location = new System.Drawing.Point(18, 45);
            this.txt_AllgemeinPath.Name = "txt_AllgemeinPath";
            this.txt_AllgemeinPath.Size = new System.Drawing.Size(360, 23);
            this.txt_AllgemeinPath.TabIndex = 1;
            // 
            // btn_AllgemeinBrowse
            // 
            this.btn_AllgemeinBrowse.Location = new System.Drawing.Point(385, 44);
            this.btn_AllgemeinBrowse.Name = "btn_AllgemeinBrowse";
            this.btn_AllgemeinBrowse.Size = new System.Drawing.Size(95, 25);
            this.btn_AllgemeinBrowse.TabIndex = 2;
            this.btn_AllgemeinBrowse.Text = "Durchsuchen...";
            // 
            // panel_Import
            // 
            this.panel_Import.Controls.Add(this.lbl_VDIPath);
            this.panel_Import.Controls.Add(this.txt_VDIPath);
            this.panel_Import.Controls.Add(this.btn_VDIPathBrowse);
            this.panel_Import.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Import.Location = new System.Drawing.Point(0, 0);
            this.panel_Import.Name = "panel_Import";
            this.panel_Import.Padding = new System.Windows.Forms.Padding(15);
            this.panel_Import.Size = new System.Drawing.Size(496, 385);
            this.panel_Import.TabIndex = 0;
            // 
            // lbl_VDIPath
            // 
            this.lbl_VDIPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lbl_VDIPath.Location = new System.Drawing.Point(15, 20);
            this.lbl_VDIPath.Name = "lbl_VDIPath";
            this.lbl_VDIPath.Size = new System.Drawing.Size(300, 20);
            this.lbl_VDIPath.TabIndex = 0;
            this.lbl_VDIPath.Text = "VDI 3805 Basis-Pfad für Dateiablage:";
            // 
            // txt_VDIPath
            // 
            this.txt_VDIPath.Location = new System.Drawing.Point(18, 45);
            this.txt_VDIPath.Name = "txt_VDIPath";
            this.txt_VDIPath.Size = new System.Drawing.Size(360, 23);
            this.txt_VDIPath.TabIndex = 1;
            // 
            // btn_VDIPathBrowse
            // 
            this.btn_VDIPathBrowse.Location = new System.Drawing.Point(385, 44);
            this.btn_VDIPathBrowse.Name = "btn_VDIPathBrowse";
            this.btn_VDIPathBrowse.Size = new System.Drawing.Size(95, 25);
            this.btn_VDIPathBrowse.TabIndex = 2;
            this.btn_VDIPathBrowse.Text = "Durchsuchen...";
            // 
            // panel_Buttons
            // 
            this.panel_Buttons.Controls.Add(this.btn_Standardwerte);
            this.panel_Buttons.Controls.Add(this.btn_Abbrechen);
            this.panel_Buttons.Controls.Add(this.btn_Speichern);
            this.panel_Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel_Buttons.Location = new System.Drawing.Point(10, 397);
            this.panel_Buttons.Name = "panel_Buttons";
            this.panel_Buttons.Size = new System.Drawing.Size(681, 43);
            this.panel_Buttons.TabIndex = 1;
            // 
            // btn_Standardwerte
            // 
            this.btn_Standardwerte.Font = new System.Drawing.Font("Segoe MDL2 Assets", 14F);
            this.btn_Standardwerte.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Standardwerte.Location = new System.Drawing.Point(0, 9);
            this.btn_Standardwerte.Name = "btn_Standardwerte";
            this.btn_Standardwerte.Size = new System.Drawing.Size(252, 33);
            this.btn_Standardwerte.TabIndex = 2;
            this.btn_Standardwerte.Text = " Standardwerte wiederherstellen";
            this.btn_Standardwerte.UseVisualStyleBackColor = true;
            this.btn_Standardwerte.Click += new System.EventHandler(this.btn_Standardwerte_Click);
            // 
            // btn_Abbrechen
            // 
            this.btn_Abbrechen.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Abbrechen.Location = new System.Drawing.Point(581, 9);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(100, 33);
            this.btn_Abbrechen.TabIndex = 0;
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            // 
            // btn_Speichern
            // 
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.save_icon_36513;
            this.btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btn_Speichern.Location = new System.Drawing.Point(456, 9);
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.Size = new System.Drawing.Size(119, 33);
            this.btn_Speichern.TabIndex = 1;
            this.btn_Speichern.Text = "Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(15, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(300, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Geokodierung-Schnittstelle (URL):";
            // 
            // txt_GEOCodUrl
            // 
            this.txt_GEOCodUrl.Location = new System.Drawing.Point(18, 107);
            this.txt_GEOCodUrl.Name = "txt_GEOCodUrl";
            this.txt_GEOCodUrl.Size = new System.Drawing.Size(460, 23);
            this.txt_GEOCodUrl.TabIndex = 7;
            this.txt_GEOCodUrl.Text = "https://nominatim.openstreetmap.org";
            // 
            // Form_AdminSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(701, 450);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel_Buttons);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_AdminSettings";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Administration - Globale Anwendungseinstellungen";
            this.Load += new System.EventHandler(this.Form_AdminSettings_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel_Content.ResumeLayout(false);
            this.panel_Export.ResumeLayout(false);
            this.panel_Export.PerformLayout();
            this.panel_Internet.ResumeLayout(false);
            this.panel_Internet.PerformLayout();
            this.panel_Allgemein.ResumeLayout(false);
            this.panel_Allgemein.PerformLayout();
            this.panel_Import.ResumeLayout(false);
            this.panel_Import.PerformLayout();
            this.panel_Buttons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox listBox_Rubriken;
        private System.Windows.Forms.Panel panel_Content;

        private System.Windows.Forms.Panel panel_Import;
        private System.Windows.Forms.Panel panel_Export;
        private System.Windows.Forms.Panel panel_Internet;
        private System.Windows.Forms.Panel panel_Allgemein;

        private System.Windows.Forms.Label lbl_VDIPath;
        private System.Windows.Forms.TextBox txt_VDIPath;
        private System.Windows.Forms.Button btn_VDIPathBrowse;

        private System.Windows.Forms.Label lbl_DBExportPath;
        private System.Windows.Forms.TextBox txt_DBExportPath;
        private System.Windows.Forms.Button btn_DBExportBrowse;

        private System.Windows.Forms.Label lbl_PVGIS;
        private System.Windows.Forms.TextBox txt_PVGISUrl;
        private System.Windows.Forms.Label lbl_OnlineDoku;
        private System.Windows.Forms.TextBox txt_OnlineDokuUrl;
        private System.Windows.Forms.Label lbl_WPPrefix;
        private System.Windows.Forms.TextBox txt_WPPrefix;

        private System.Windows.Forms.Label lbl_Allgemein;
        private System.Windows.Forms.TextBox txt_AllgemeinPath;
        private System.Windows.Forms.Button btn_AllgemeinBrowse;

        private System.Windows.Forms.Panel panel_Buttons;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_Abbrechen;
        private System.Windows.Forms.Button btn_Standardwerte;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_DBImportPath;
        private System.Windows.Forms.Button btn_DBImportBrowse;
        private System.Windows.Forms.Label lbl_DBPath;
        private System.Windows.Forms.TextBox txt_DBPath;
        private System.Windows.Forms.Button btn_DBPathBrowse;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_GEOCodUrl;
    }
}