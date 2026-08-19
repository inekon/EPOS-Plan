using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_AdminSettings : Form
    {
        public Form_AdminSettings()
        {
            InitializeComponent();

            // Events verknüpfen
            listBox_Rubriken.SelectedIndexChanged += ListBox_Rubriken_SelectedIndexChanged;
            btn_VDIPathBrowse.Click += (s, e) => BrowseFolder(txt_VDIPath);
            btn_DBExportBrowse.Click += (s, e) => BrowseFolder(txt_DBExportPath);
            btn_DBImportBrowse.Click += (s, e) => BrowseFolder(txt_DBImportPath);
            btn_DBPathBrowse.Click += (s, e) => BrowseFolder(txt_DBPath);
            btn_AllgemeinBrowse.Click += (s, e) => BrowseFolder(txt_AllgemeinPath);

            btn_Speichern.Click += Btn_Speichern_Click;
            btn_Abbrechen.Click += (s, e) => this.Close();

            KiAbschalterAufbauen();

            // Standardmäßig den ersten Eintrag auswählen
            if (listBox_Rubriken.Items.Count > 0)
                listBox_Rubriken.SelectedIndex = 0;
        }

        // ------------------------------------------------------------------
        // Abschalter des KI-Assistenten (Rubrik „Anwendung")
        // ------------------------------------------------------------------

        /// <summary>Der Abschalter; <c>null</c>, wenn er sich nicht aufbauen ließ.</summary>
        private CheckBox chk_KiAus;

        /// <summary>
        /// Baut den Abschalter der Installation in die Rubrik „Anwendung": setzt ihn ein
        /// Betreiber, sind Menüeintrag und Chatfenster des KI-Assistenten nicht
        /// erreichbar und es geht nichts an einen externen Dienst.
        /// </summary>
        /// <remarks>
        /// <b>Programmatisch angelegt.</b> Designer- und .resx-Dateien bleiben unberührt
        /// (Hausregel); die Steuerelemente hängen sich in das vorhandene
        /// <c>panel_Allgemein</c> unterhalb des Pfadfeldes ein. Abgelegt wird nicht in
        /// <c>Properties.Settings</c>, sondern im Registry-Zweig
        /// <c>HKCU\Software\wp-plan</c> - dort liegen auch Sprache, Lizenzzustimmung und
        /// die übrigen KI-Einstellungen (siehe <see cref="KiEinwilligung"/>).
        /// </remarks>
        private void KiAbschalterAufbauen()
        {
            try
            {
                chk_KiAus = new CheckBox
                {
                    Name = "chk_KiAus",
                    Text = MyResource.Resource.KI_ABSCHALTER_ADMIN,
                    Location = new Point(18, 96),
                    Size = new Size(462, 38),
                    TabIndex = 3,
                    Checked = KiEinwilligung.Abgeschaltet
                };

                Label lbl = new Label
                {
                    Name = "lbl_KiAus",
                    AutoSize = false,
                    ForeColor = Color.DimGray,
                    Location = new Point(36, 136),
                    Size = new Size(444, 72),
                    Text = MyResource.Resource.KI_ABSCHALTER_ADMIN_HINWEIS
                };

                // Ein maschinenweiter Eintrag (HKLM) ist die Sperre der Verwaltung - sie
                // darf sich hier nicht loesen lassen.
                if (KiEinwilligung.AbschalterMaschine)
                {
                    chk_KiAus.Enabled = false;
                    lbl.Text = MyResource.Resource.KI_ABSCHALTER_MASCHINE +
                               Environment.NewLine + Environment.NewLine + lbl.Text;
                    lbl.ForeColor = Color.FromArgb(160, 80, 0);
                }

                panel_Allgemein.Controls.Add(chk_KiAus);
                panel_Allgemein.Controls.Add(lbl);
            }
            catch { chk_KiAus = null; }
        }

        /// <summary>Schreibt den Abschalter; bei maschinenweiter Sperre passiert nichts.</summary>
        private void KiAbschalterSpeichern()
        {
            if (chk_KiAus == null || !chk_KiAus.Enabled) return;
            KiEinwilligung.Abgeschaltet = chk_KiAus.Checked;
        }

        private void ListBox_Rubriken_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Alle Panels zuerst ausblenden
            panel_Import.Visible = false;
            panel_Export.Visible = false;
            panel_Internet.Visible = false;
            panel_Allgemein.Visible = false;

            // Das passende Panel aktivieren je nach INDEX (0, 1, 2, 3...)
            switch (listBox_Rubriken.SelectedIndex)
            {
                case 0: // Erster Eintrag in der Liste (z.B. VDI 3805)
                    panel_Import.Visible = true;
                    break;
                case 1: // Zweiter Eintrag (z.B. Datenbank-Export)
                    panel_Export.Visible = true;
                    break;
                case 2: // Dritter Eintrag (z.B. Web-Schnittstellen)
                    panel_Internet.Visible = true;
                    break;
                case 3: // Vierter Eintrag (Allgemein)
                    panel_Allgemein.Visible = true;
                    break;
            }
        }

        // Hilfsfunktion für den Ordner-Auswahldialog
        private void BrowseFolder(TextBox targetTextBox)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = targetTextBox.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    targetTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void Btn_Speichern_Click(object sender, EventArgs e)
        {
            // Werte in die Settings schreiben
            Properties.Settings.Default.VDI3805Path = txt_VDIPath.Text;
            Properties.Settings.Default.DBExportPath = txt_DBExportPath.Text;
            Properties.Settings.Default.DBImportPath = txt_DBImportPath.Text;
            Properties.Settings.Default.DBPath = txt_DBPath.Text;
            Properties.Settings.Default.DBName = txt_DBName.Text;
            Properties.Settings.Default.WordPressUrl = txt_OnlineDokuUrl.Text;
            Properties.Settings.Default.WordPressPrefix = txt_WPPrefix.Text;
            Properties.Settings.Default.PVGISUrl = txt_PVGISUrl.Text;
            Properties.Settings.Default.GeoKodierung = txt_GEOCodUrl.Text;
            Properties.Settings.Default.AllgemeinPath = txt_AllgemeinPath.Text;

            // AUTOMATISCHES ANLEGEN DER ORDNER
            try
            {
                if (!string.IsNullOrWhiteSpace(txt_VDIPath.Text) && !Directory.Exists(txt_VDIPath.Text))
                {
                    Directory.CreateDirectory(txt_VDIPath.Text);
                }

                if (!string.IsNullOrWhiteSpace(txt_DBImportPath.Text) && !Directory.Exists(txt_DBImportPath.Text))
                {
                    Directory.CreateDirectory(txt_DBImportPath.Text);
                }

                if (!string.IsNullOrWhiteSpace(txt_DBPath.Text) && !Directory.Exists(txt_DBPath.Text))
                {
                    Directory.CreateDirectory(txt_DBPath.Text);
                }

                if (!string.IsNullOrWhiteSpace(txt_DBExportPath.Text) && !Directory.Exists(txt_DBExportPath.Text))
                {
                    Directory.CreateDirectory(txt_DBExportPath.Text);
                }

                if (!string.IsNullOrWhiteSpace(txt_AllgemeinPath.Text) && !Directory.Exists(txt_AllgemeinPath.Text))
                {
                    Directory.CreateDirectory(txt_AllgemeinPath.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Die Ordner konnten nicht erstellt werden. Bitte überprüfen Sie die Pfadangaben.\nFehler: {ex.Message}",
                                "Fehler beim Erstellen der Verzeichnisse", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Properties.Settings.Default.Save();

            // Der Abschalter des KI-Assistenten liegt in der Registry, nicht in den
            // Settings - deshalb hier gesondert.
            KiAbschalterSpeichern();

            MessageBox.Show("Die Einstellungen wurden erfolgreich gespeichert.", "Administration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Form_AdminSettings_Load(object sender, EventArgs e)
        {
            // 1. VDI-Pfad bestimmen (Gespeichert oder AppData-Default)
            txt_VDIPath.Text = GetConfiguredOrDefaultVDIPath();

            // 2. DB-Pfade bestimmen: Nutzen jetzt die neuen Hilfsmethoden, 
            // welche die Verknüpfung zum VDI-Pfad nur bei leeren Settings herstellen!
            txt_DBExportPath.Text = GetConfiguredOrDefaultDBExportPath(txt_VDIPath.Text);
            txt_DBImportPath.Text = GetConfiguredOrDefaultDBImportPath(txt_VDIPath.Text);
            txt_DBPath.Text = GetConfiguredOrDefaultDBPath();
            txt_DBName.Text = Properties.Settings.Default.DBName;

            // 3. Allgemein-Pfad bestimmen
            txt_AllgemeinPath.Text = GetConfiguredOrDefaultPath("");

            // Restliche Felder ganz normal laden
            txt_OnlineDokuUrl.Text = Properties.Settings.Default.WordPressUrl;
            txt_WPPrefix.Text = Properties.Settings.Default.WordPressPrefix;
            txt_PVGISUrl.Text = Properties.Settings.Default.PVGISUrl;
            txt_GEOCodUrl.Text = Properties.Settings.Default.GeoKodierung;

            // Abschalter des KI-Assistenten frisch aus der Registry
            if (chk_KiAus != null) chk_KiAus.Checked = KiEinwilligung.Abgeschaltet;
        }

        private void btn_Standardwerte_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                  "Möchten Sie wirklich alle Einstellungen auf die Werksstandards zurücksetzen?",
                  "Standardwerte wiederherstellen",
                  MessageBoxButtons.YesNo,
                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Setzt alle Einstellungen im RAM zurück (Alle Pfade werden wieder leer "")
                Properties.Settings.Default.Reset();

                // Web-Felder neu befüllen
                txt_WPPrefix.Text = Properties.Settings.Default.WordPressPrefix;
                txt_PVGISUrl.Text = Properties.Settings.Default.PVGISUrl;
                txt_GEOCodUrl.Text = Properties.Settings.Default.GeoKodierung;
                txt_OnlineDokuUrl.Text = Properties.Settings.Default.WordPressUrl;

                // Pfade über die Hilfsmethoden frisch als Default generieren lassen
                txt_VDIPath.Text = GetConfiguredOrDefaultVDIPath();
                txt_DBExportPath.Text = GetConfiguredOrDefaultDBExportPath(txt_VDIPath.Text);
                txt_DBImportPath.Text = GetConfiguredOrDefaultDBImportPath(txt_VDIPath.Text);
                txt_DBPath.Text = GetConfiguredOrDefaultDBPath();
                txt_DBPath.Text = Properties.Settings.Default.DBName;
                txt_AllgemeinPath.Text = GetConfiguredOrDefaultPath("");

                MessageBox.Show("Die Standardwerte wurden geladen. Klicken Sie auf 'Speichern', um sie zu übernehmen.", "Zurücksetzen erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // --- HILFSMETHODEN FÜR DIE DYNAMISCHE PFAD-GENERIERUNG ---

        private string GetConfiguredOrDefaultVDIPath()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.VDI3805Path))
            {
                return Properties.Settings.Default.VDI3805Path;
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "WP-Plan");
        }

        private string GetConfiguredOrDefaultDBExportPath(string currentVdiPath)
        {
            // Wenn der User bereits einen eigenen Pfad gespeichert hat, nutzen wir diesen direkt!
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.DBExportPath))
            {
                return Properties.Settings.Default.DBExportPath;
            }

            // FALLBACK: Wenn die Settings leer sind, hängen wir "Backup" an den aktuellen VDI-Pfad an
            return Path.Combine(currentVdiPath, "Backup");
        }

        private string GetConfiguredOrDefaultDBImportPath(string currentVdiPath)
        {
            // Wenn der User bereits einen eigenen Pfad gespeichert hat, nutzen wir diesen direkt!
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.DBImportPath))
            {
                return Properties.Settings.Default.DBImportPath;
            }

            // FALLBACK: Wenn die Settings leer sind, hängen wir "Import" an den aktuellen VDI-Pfad an
            return Path.Combine(currentVdiPath, "Import");
        }

        private string GetConfiguredOrDefaultDBPath()
        {
            // Standard-Datenbankpfad: gespeicherter Wert, sonst %ProgramData%\EPOS_PLAN
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.DBPath))
            {
                return Properties.Settings.Default.DBPath;
            }

            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "EPOS_PLAN");
        }

        private string GetConfiguredOrDefaultPath(string szPath)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.AllgemeinPath))
            {
                return Properties.Settings.Default.AllgemeinPath;
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "WP-Plan");
        }


    }
}