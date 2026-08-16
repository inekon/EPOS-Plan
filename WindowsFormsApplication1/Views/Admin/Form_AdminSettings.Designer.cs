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
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            listBox_Rubriken = new System.Windows.Forms.ListBox();
            panel_Content = new System.Windows.Forms.Panel();
            panel_Internet = new System.Windows.Forms.Panel();
            label2 = new System.Windows.Forms.Label();
            txt_GEOCodUrl = new System.Windows.Forms.TextBox();
            lbl_PVGIS = new System.Windows.Forms.Label();
            txt_PVGISUrl = new System.Windows.Forms.TextBox();
            lbl_OnlineDoku = new System.Windows.Forms.Label();
            txt_OnlineDokuUrl = new System.Windows.Forms.TextBox();
            lbl_WPPrefix = new System.Windows.Forms.Label();
            txt_WPPrefix = new System.Windows.Forms.TextBox();
            panel_Allgemein = new System.Windows.Forms.Panel();
            lbl_Allgemein = new System.Windows.Forms.Label();
            txt_AllgemeinPath = new System.Windows.Forms.TextBox();
            btn_AllgemeinBrowse = new System.Windows.Forms.Button();
            panel_Import = new System.Windows.Forms.Panel();
            lbl_VDIPath = new System.Windows.Forms.Label();
            txt_VDIPath = new System.Windows.Forms.TextBox();
            btn_VDIPathBrowse = new System.Windows.Forms.Button();
            panel_Export = new System.Windows.Forms.Panel();
            label1 = new System.Windows.Forms.Label();
            txt_DBImportPath = new System.Windows.Forms.TextBox();
            btn_DBImportBrowse = new System.Windows.Forms.Button();
            lbl_DBPath = new System.Windows.Forms.Label();
            txt_DBPath = new System.Windows.Forms.TextBox();
            btn_DBPathBrowse = new System.Windows.Forms.Button();
            lbl_DBExportPath = new System.Windows.Forms.Label();
            txt_DBExportPath = new System.Windows.Forms.TextBox();
            btn_DBExportBrowse = new System.Windows.Forms.Button();
            panel_Buttons = new System.Windows.Forms.Panel();
            btn_Standardwerte = new System.Windows.Forms.Button();
            btn_Abbrechen = new System.Windows.Forms.Button();
            btn_Speichern = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            txt_DBName = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel_Content.SuspendLayout();
            panel_Internet.SuspendLayout();
            panel_Allgemein.SuspendLayout();
            panel_Import.SuspendLayout();
            panel_Export.SuspendLayout();
            panel_Buttons.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(10, 10);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(listBox_Rubriken);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel_Content);
            splitContainer1.Size = new System.Drawing.Size(681, 387);
            splitContainer1.SplitterDistance = 179;
            splitContainer1.TabIndex = 0;
            // 
            // listBox_Rubriken
            // 
            listBox_Rubriken.Dock = System.Windows.Forms.DockStyle.Left;
            listBox_Rubriken.Font = new System.Drawing.Font("Segoe UI", 9F);
            listBox_Rubriken.IntegralHeight = false;
            listBox_Rubriken.ItemHeight = 15;
            listBox_Rubriken.Items.AddRange(new object[] { "VDI Datensätze", "Datenbank", "Web-Schnittstellen (API)", "Anwendung" });
            listBox_Rubriken.Location = new System.Drawing.Point(0, 0);
            listBox_Rubriken.Name = "listBox_Rubriken";
            listBox_Rubriken.Size = new System.Drawing.Size(178, 387);
            listBox_Rubriken.TabIndex = 0;
            // 
            // panel_Content
            // 
            panel_Content.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panel_Content.Controls.Add(panel_Export);
            panel_Content.Controls.Add(panel_Internet);
            panel_Content.Controls.Add(panel_Allgemein);
            panel_Content.Controls.Add(panel_Import);
            panel_Content.Dock = System.Windows.Forms.DockStyle.Fill;
            panel_Content.Location = new System.Drawing.Point(0, 0);
            panel_Content.Name = "panel_Content";
            panel_Content.Size = new System.Drawing.Size(498, 387);
            panel_Content.TabIndex = 0;
            // 
            // panel_Internet
            // 
            panel_Internet.Controls.Add(label2);
            panel_Internet.Controls.Add(txt_GEOCodUrl);
            panel_Internet.Controls.Add(lbl_PVGIS);
            panel_Internet.Controls.Add(txt_PVGISUrl);
            panel_Internet.Controls.Add(lbl_OnlineDoku);
            panel_Internet.Controls.Add(txt_OnlineDokuUrl);
            panel_Internet.Controls.Add(lbl_WPPrefix);
            panel_Internet.Controls.Add(txt_WPPrefix);
            panel_Internet.Dock = System.Windows.Forms.DockStyle.Fill;
            panel_Internet.Location = new System.Drawing.Point(0, 0);
            panel_Internet.Name = "panel_Internet";
            panel_Internet.Padding = new System.Windows.Forms.Padding(15);
            panel_Internet.Size = new System.Drawing.Size(496, 385);
            panel_Internet.TabIndex = 2;
            // 
            // label2
            // 
            label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label2.Location = new System.Drawing.Point(15, 82);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(300, 20);
            label2.TabIndex = 6;
            label2.Text = "Geokodierung-Schnittstelle (URL):";
            // 
            // txt_GEOCodUrl
            // 
            txt_GEOCodUrl.Location = new System.Drawing.Point(18, 107);
            txt_GEOCodUrl.Name = "txt_GEOCodUrl";
            txt_GEOCodUrl.Size = new System.Drawing.Size(460, 23);
            txt_GEOCodUrl.TabIndex = 7;
            txt_GEOCodUrl.Text = "https://nominatim.openstreetmap.org";
            // 
            // lbl_PVGIS
            // 
            lbl_PVGIS.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_PVGIS.Location = new System.Drawing.Point(15, 20);
            lbl_PVGIS.Name = "lbl_PVGIS";
            lbl_PVGIS.Size = new System.Drawing.Size(300, 20);
            lbl_PVGIS.TabIndex = 0;
            lbl_PVGIS.Text = "PVGIS API Server URL:";
            // 
            // txt_PVGISUrl
            // 
            txt_PVGISUrl.Location = new System.Drawing.Point(18, 45);
            txt_PVGISUrl.Name = "txt_PVGISUrl";
            txt_PVGISUrl.Size = new System.Drawing.Size(460, 23);
            txt_PVGISUrl.TabIndex = 1;
            txt_PVGISUrl.Text = "https://re.jrc.ec.europa.eu/api/v5_2/";
            // 
            // lbl_OnlineDoku
            // 
            lbl_OnlineDoku.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_OnlineDoku.Location = new System.Drawing.Point(15, 169);
            lbl_OnlineDoku.Name = "lbl_OnlineDoku";
            lbl_OnlineDoku.Size = new System.Drawing.Size(300, 20);
            lbl_OnlineDoku.TabIndex = 2;
            lbl_OnlineDoku.Text = "Online Dokumentation Basis-URL:";
            // 
            // txt_OnlineDokuUrl
            // 
            txt_OnlineDokuUrl.Location = new System.Drawing.Point(18, 194);
            txt_OnlineDokuUrl.Name = "txt_OnlineDokuUrl";
            txt_OnlineDokuUrl.Size = new System.Drawing.Size(460, 23);
            txt_OnlineDokuUrl.TabIndex = 3;
            txt_OnlineDokuUrl.Text = "http://localhost:8080";
            // 
            // lbl_WPPrefix
            // 
            lbl_WPPrefix.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_WPPrefix.Location = new System.Drawing.Point(15, 239);
            lbl_WPPrefix.Name = "lbl_WPPrefix";
            lbl_WPPrefix.Size = new System.Drawing.Size(400, 20);
            lbl_WPPrefix.TabIndex = 4;
            lbl_WPPrefix.Text = "WordPress API-Präfix / REST-Base (z.B. help, pages, posts):";
            // 
            // txt_WPPrefix
            // 
            txt_WPPrefix.Location = new System.Drawing.Point(18, 264);
            txt_WPPrefix.Name = "txt_WPPrefix";
            txt_WPPrefix.Size = new System.Drawing.Size(150, 23);
            txt_WPPrefix.TabIndex = 5;
            txt_WPPrefix.Text = "help";
            // 
            // panel_Allgemein
            // 
            panel_Allgemein.Controls.Add(lbl_Allgemein);
            panel_Allgemein.Controls.Add(txt_AllgemeinPath);
            panel_Allgemein.Controls.Add(btn_AllgemeinBrowse);
            panel_Allgemein.Dock = System.Windows.Forms.DockStyle.Fill;
            panel_Allgemein.Location = new System.Drawing.Point(0, 0);
            panel_Allgemein.Name = "panel_Allgemein";
            panel_Allgemein.Padding = new System.Windows.Forms.Padding(15);
            panel_Allgemein.Size = new System.Drawing.Size(496, 385);
            panel_Allgemein.TabIndex = 3;
            // 
            // lbl_Allgemein
            // 
            lbl_Allgemein.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_Allgemein.Location = new System.Drawing.Point(15, 20);
            lbl_Allgemein.Name = "lbl_Allgemein";
            lbl_Allgemein.Size = new System.Drawing.Size(300, 20);
            lbl_Allgemein.TabIndex = 0;
            lbl_Allgemein.Text = "Allgemeiner Sicherungspfad / Temp-Ordner:";
            // 
            // txt_AllgemeinPath
            // 
            txt_AllgemeinPath.Location = new System.Drawing.Point(18, 45);
            txt_AllgemeinPath.Name = "txt_AllgemeinPath";
            txt_AllgemeinPath.Size = new System.Drawing.Size(360, 23);
            txt_AllgemeinPath.TabIndex = 1;
            // 
            // btn_AllgemeinBrowse
            // 
            btn_AllgemeinBrowse.Location = new System.Drawing.Point(385, 44);
            btn_AllgemeinBrowse.Name = "btn_AllgemeinBrowse";
            btn_AllgemeinBrowse.Size = new System.Drawing.Size(95, 25);
            btn_AllgemeinBrowse.TabIndex = 2;
            btn_AllgemeinBrowse.Text = "Durchsuchen...";
            // 
            // panel_Import
            // 
            panel_Import.Controls.Add(lbl_VDIPath);
            panel_Import.Controls.Add(txt_VDIPath);
            panel_Import.Controls.Add(btn_VDIPathBrowse);
            panel_Import.Dock = System.Windows.Forms.DockStyle.Fill;
            panel_Import.Location = new System.Drawing.Point(0, 0);
            panel_Import.Name = "panel_Import";
            panel_Import.Padding = new System.Windows.Forms.Padding(15);
            panel_Import.Size = new System.Drawing.Size(496, 385);
            panel_Import.TabIndex = 0;
            // 
            // lbl_VDIPath
            // 
            lbl_VDIPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_VDIPath.Location = new System.Drawing.Point(15, 20);
            lbl_VDIPath.Name = "lbl_VDIPath";
            lbl_VDIPath.Size = new System.Drawing.Size(300, 20);
            lbl_VDIPath.TabIndex = 0;
            lbl_VDIPath.Text = "VDI 3805 Basis-Pfad für Dateiablage:";
            // 
            // txt_VDIPath
            // 
            txt_VDIPath.Location = new System.Drawing.Point(18, 45);
            txt_VDIPath.Name = "txt_VDIPath";
            txt_VDIPath.Size = new System.Drawing.Size(360, 23);
            txt_VDIPath.TabIndex = 1;
            // 
            // btn_VDIPathBrowse
            // 
            btn_VDIPathBrowse.Location = new System.Drawing.Point(385, 44);
            btn_VDIPathBrowse.Name = "btn_VDIPathBrowse";
            btn_VDIPathBrowse.Size = new System.Drawing.Size(95, 25);
            btn_VDIPathBrowse.TabIndex = 2;
            btn_VDIPathBrowse.Text = "Durchsuchen...";
            // 
            // panel_Export
            // 
            panel_Export.Controls.Add(label3);
            panel_Export.Controls.Add(txt_DBName);
            panel_Export.Controls.Add(label1);
            panel_Export.Controls.Add(txt_DBImportPath);
            panel_Export.Controls.Add(btn_DBImportBrowse);
            panel_Export.Controls.Add(lbl_DBPath);
            panel_Export.Controls.Add(txt_DBPath);
            panel_Export.Controls.Add(btn_DBPathBrowse);
            panel_Export.Controls.Add(lbl_DBExportPath);
            panel_Export.Controls.Add(txt_DBExportPath);
            panel_Export.Controls.Add(btn_DBExportBrowse);
            panel_Export.Dock = System.Windows.Forms.DockStyle.Fill;
            panel_Export.Location = new System.Drawing.Point(0, 0);
            panel_Export.Name = "panel_Export";
            panel_Export.Padding = new System.Windows.Forms.Padding(15);
            panel_Export.Size = new System.Drawing.Size(496, 385);
            panel_Export.TabIndex = 1;
            // 
            // label1
            // 
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label1.Location = new System.Drawing.Point(15, 91);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(300, 20);
            label1.TabIndex = 3;
            label1.Text = "Standard Datenbank-Import-Pfad:";
            // 
            // txt_DBImportPath
            // 
            txt_DBImportPath.Location = new System.Drawing.Point(18, 116);
            txt_DBImportPath.Name = "txt_DBImportPath";
            txt_DBImportPath.Size = new System.Drawing.Size(360, 23);
            txt_DBImportPath.TabIndex = 4;
            // 
            // btn_DBImportBrowse
            // 
            btn_DBImportBrowse.Location = new System.Drawing.Point(385, 115);
            btn_DBImportBrowse.Name = "btn_DBImportBrowse";
            btn_DBImportBrowse.Size = new System.Drawing.Size(95, 25);
            btn_DBImportBrowse.TabIndex = 5;
            btn_DBImportBrowse.Text = "Durchsuchen...";
            // 
            // lbl_DBPath
            // 
            lbl_DBPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_DBPath.Location = new System.Drawing.Point(15, 162);
            lbl_DBPath.Name = "lbl_DBPath";
            lbl_DBPath.Size = new System.Drawing.Size(300, 20);
            lbl_DBPath.TabIndex = 6;
            lbl_DBPath.Text = "Standard Datenbank-Pfad:";
            // 
            // txt_DBPath
            // 
            txt_DBPath.Location = new System.Drawing.Point(18, 187);
            txt_DBPath.Name = "txt_DBPath";
            txt_DBPath.Size = new System.Drawing.Size(360, 23);
            txt_DBPath.TabIndex = 7;
            // 
            // btn_DBPathBrowse
            // 
            btn_DBPathBrowse.Location = new System.Drawing.Point(385, 186);
            btn_DBPathBrowse.Name = "btn_DBPathBrowse";
            btn_DBPathBrowse.Size = new System.Drawing.Size(95, 25);
            btn_DBPathBrowse.TabIndex = 8;
            btn_DBPathBrowse.Text = "Durchsuchen...";
            // 
            // lbl_DBExportPath
            // 
            lbl_DBExportPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lbl_DBExportPath.Location = new System.Drawing.Point(15, 20);
            lbl_DBExportPath.Name = "lbl_DBExportPath";
            lbl_DBExportPath.Size = new System.Drawing.Size(300, 20);
            lbl_DBExportPath.TabIndex = 0;
            lbl_DBExportPath.Text = "Standard Datenbank-Export-Pfad:";
            // 
            // txt_DBExportPath
            // 
            txt_DBExportPath.Location = new System.Drawing.Point(18, 45);
            txt_DBExportPath.Name = "txt_DBExportPath";
            txt_DBExportPath.Size = new System.Drawing.Size(360, 23);
            txt_DBExportPath.TabIndex = 1;
            // 
            // btn_DBExportBrowse
            // 
            btn_DBExportBrowse.Location = new System.Drawing.Point(385, 44);
            btn_DBExportBrowse.Name = "btn_DBExportBrowse";
            btn_DBExportBrowse.Size = new System.Drawing.Size(95, 25);
            btn_DBExportBrowse.TabIndex = 2;
            btn_DBExportBrowse.Text = "Durchsuchen...";
            // 
            // panel_Buttons
            // 
            panel_Buttons.Controls.Add(btn_Standardwerte);
            panel_Buttons.Controls.Add(btn_Abbrechen);
            panel_Buttons.Controls.Add(btn_Speichern);
            panel_Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel_Buttons.Location = new System.Drawing.Point(10, 397);
            panel_Buttons.Name = "panel_Buttons";
            panel_Buttons.Size = new System.Drawing.Size(681, 43);
            panel_Buttons.TabIndex = 1;
            // 
            // btn_Standardwerte
            // 
            btn_Standardwerte.Font = new System.Drawing.Font("Segoe MDL2 Assets", 14F);
            btn_Standardwerte.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btn_Standardwerte.Location = new System.Drawing.Point(0, 9);
            btn_Standardwerte.Name = "btn_Standardwerte";
            btn_Standardwerte.Size = new System.Drawing.Size(252, 33);
            btn_Standardwerte.TabIndex = 2;
            btn_Standardwerte.Text = " Standardwerte wiederherstellen";
            btn_Standardwerte.UseVisualStyleBackColor = true;
            btn_Standardwerte.Click += btn_Standardwerte_Click;
            // 
            // btn_Abbrechen
            // 
            btn_Abbrechen.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btn_Abbrechen.Location = new System.Drawing.Point(581, 9);
            btn_Abbrechen.Name = "btn_Abbrechen";
            btn_Abbrechen.Size = new System.Drawing.Size(100, 33);
            btn_Abbrechen.TabIndex = 0;
            btn_Abbrechen.Text = "Abbrechen";
            btn_Abbrechen.UseVisualStyleBackColor = true;
            // 
            // btn_Speichern
            // 
            btn_Speichern.Image = Properties.Resources.save_icon_36513;
            btn_Speichern.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btn_Speichern.Location = new System.Drawing.Point(456, 9);
            btn_Speichern.Name = "btn_Speichern";
            btn_Speichern.Size = new System.Drawing.Size(119, 33);
            btn_Speichern.TabIndex = 1;
            btn_Speichern.Text = "Speichern";
            btn_Speichern.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label3.Location = new System.Drawing.Point(15, 225);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(300, 20);
            label3.TabIndex = 9;
            label3.Text = "Standard Datenbank:";
            // 
            // txt_DBName
            // 
            txt_DBName.Location = new System.Drawing.Point(18, 250);
            txt_DBName.Name = "txt_DBName";
            txt_DBName.Size = new System.Drawing.Size(360, 23);
            txt_DBName.TabIndex = 10;
            // 
            // Form_AdminSettings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(701, 450);
            Controls.Add(splitContainer1);
            Controls.Add(panel_Buttons);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_AdminSettings";
            Padding = new System.Windows.Forms.Padding(10);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Administration - Globale Anwendungseinstellungen";
            Load += Form_AdminSettings_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel_Content.ResumeLayout(false);
            panel_Internet.ResumeLayout(false);
            panel_Internet.PerformLayout();
            panel_Allgemein.ResumeLayout(false);
            panel_Allgemein.PerformLayout();
            panel_Import.ResumeLayout(false);
            panel_Import.PerformLayout();
            panel_Export.ResumeLayout(false);
            panel_Export.PerformLayout();
            panel_Buttons.ResumeLayout(false);
            ResumeLayout(false);

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
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txt_DBName;
    }
}