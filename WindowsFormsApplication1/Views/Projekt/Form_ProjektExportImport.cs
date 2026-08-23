using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog zum Exportieren eines Projekts in eine .wpx-Datei und zum Importieren
    /// (auch in eine andere DB). Komplett im Code aufgebaut (kein .resx).
    /// Aufruf z. B.:  new Form_ProjektExportImport().ShowDialog();
    /// Nach erfolgreichem Import stehen ImportierteProjektId / ImportierterName bereit.
    /// </summary>
    public class Form_ProjektExportImport : Form
    {
        public int ImportierteProjektId { get; private set; } = -1;
        public string ImportierterName { get; private set; } = "";

        private readonly ProjektExportImportCtrl _io = new ProjektExportImportCtrl();

        // Export
        private ComboBox cbProjekt;
        private Button btnExport;
        // Import
        private TextBox txtDatei;
        private Button btnDatei, btnImport;
        private Label lblInfo;
        private TextBox txtZielname;
        private RadioButton rbNeuerName, rbUeberschreiben, rbAbbrechen;
        // gemeinsam
        private ProgressBar pb;
        private Label lblStatus;
        private TabControl tabs;

        public Form_ProjektExportImport()
        {
            BaueUi();
            LadeProjekte();
        }

        private void BaueUi()
        {
            Text = "Projekt exportieren / importieren";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new System.Drawing.Size(480, 360);
            Font = new System.Drawing.Font("Segoe UI", 9.75f);

            tabs = new TabControl { Location = new System.Drawing.Point(12, 12), Size = new System.Drawing.Size(456, 270) };
            var tpExport = new TabPage("Exportieren");
            var tpImport = new TabPage("Importieren");
            tabs.TabPages.Add(tpExport);
            tabs.TabPages.Add(tpImport);
            Controls.Add(tabs);

            // ---- Export ----
            tpExport.Controls.Add(new Label { Text = "Projekt:", Location = new System.Drawing.Point(16, 24), AutoSize = true });
            cbProjekt = new ComboBox
            {
                Location = new System.Drawing.Point(16, 48), Width = 416,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            tpExport.Controls.Add(cbProjekt);
            btnExport = new Button { Text = "Exportieren…", Location = new System.Drawing.Point(312, 96), Size = new System.Drawing.Size(120, 30) };
            btnExport.Click += btnExport_Click;
            tpExport.Controls.Add(btnExport);

            // ---- Import ----
            btnDatei = new Button { Text = "Datei wählen…", Location = new System.Drawing.Point(16, 20), Size = new System.Drawing.Size(120, 28) };
            btnDatei.Click += btnDatei_Click;
            tpImport.Controls.Add(btnDatei);
            txtDatei = new TextBox { Location = new System.Drawing.Point(144, 22), Width = 288, ReadOnly = true };
            tpImport.Controls.Add(txtDatei);

            lblInfo = new Label { Location = new System.Drawing.Point(16, 56), Size = new System.Drawing.Size(416, 46), ForeColor = System.Drawing.Color.DimGray };
            tpImport.Controls.Add(lblInfo);

            tpImport.Controls.Add(new Label { Text = "Zielname (leer = aus Datei):", Location = new System.Drawing.Point(16, 106), AutoSize = true });
            txtZielname = new TextBox { Location = new System.Drawing.Point(200, 103), Width = 232 };
            tpImport.Controls.Add(txtZielname);

            tpImport.Controls.Add(new Label { Text = "Falls dieser Name bereits existiert:", Location = new System.Drawing.Point(16, 138), AutoSize = true });
            rbNeuerName     = new RadioButton { Text = "Unter neuem Namen importieren", Location = new System.Drawing.Point(24, 160), AutoSize = true, Checked = true };
            rbUeberschreiben= new RadioButton { Text = "Vorhandenes Projekt überschreiben", Location = new System.Drawing.Point(24, 182), AutoSize = true };
            rbAbbrechen     = new RadioButton { Text = "Abbrechen", Location = new System.Drawing.Point(24, 204), AutoSize = true };
            tpImport.Controls.Add(rbNeuerName); tpImport.Controls.Add(rbUeberschreiben); tpImport.Controls.Add(rbAbbrechen);

            btnImport = new Button { Text = "Importieren…", Location = new System.Drawing.Point(312, 200), Size = new System.Drawing.Size(120, 30), Enabled = false };
            btnImport.Click += btnImport_Click;
            tpImport.Controls.Add(btnImport);

            // ---- gemeinsam ----
            pb = new ProgressBar { Location = new System.Drawing.Point(12, 296), Size = new System.Drawing.Size(456, 18) };
            Controls.Add(pb);
            lblStatus = new Label { Location = new System.Drawing.Point(12, 320), Size = new System.Drawing.Size(360, 20), ForeColor = System.Drawing.Color.DimGray };
            Controls.Add(lblStatus);
            var btnSchliessen = new Button { Text = "Schließen", Location = new System.Drawing.Point(388, 318), Size = new System.Drawing.Size(80, 26), DialogResult = DialogResult.Cancel };
            Controls.Add(btnSchliessen);
            CancelButton = btnSchliessen;
        }

        private void LadeProjekte()
        {
            try
            {
                var pc = new ProjektCtrl();
                pc.ReadAll();
                cbProjekt.Items.Clear();
                foreach (var p in pc.items) cbProjekt.Items.Add(p.m_szProjektname);
                if (cbProjekt.Items.Count > 0) cbProjekt.SelectedIndex = 0;
            }
            catch (Exception ex) { lblStatus.Text = "Projekte konnten nicht geladen werden: " + ex.Message; }
        }

        private IProgress<ProjektDuplizierenCtrl.Fortschritt> MacheProgress() =>
            new Progress<ProjektDuplizierenCtrl.Fortschritt>(f =>
            {
                pb.Maximum = Math.Max(1, f.Gesamt);
                pb.Value = Math.Min(Math.Max(0, f.Aktuell), pb.Maximum);
                lblStatus.Text = string.IsNullOrEmpty(f.Tabelle) ? "" : "… " + f.Tabelle;
            });

        // ================= EXPORT =================
        private async void btnExport_Click(object sender, EventArgs e)
        {
            string projekt = cbProjekt.SelectedItem as string;
            if (string.IsNullOrEmpty(projekt)) { MessageBox.Show("Bitte ein Projekt auswählen."); return; }

            using (var sfd = new SaveFileDialog { Filter = "WP-Projekt (*.wpx)|*.wpx", FileName = projekt + ".wpx", DefaultExt = "wpx" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                UiSperren(true);
                var prog = MacheProgress();
                bool ok = await Task.Run(() => _io.Exportieren(projekt, sfd.FileName, prog));
                UiSperren(false);

                lblStatus.Text = ok ? "Export abgeschlossen." : "Export fehlgeschlagen.";
                if (ok) MessageBox.Show("Projekt exportiert:\r\n" + sfd.FileName, "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ================= IMPORT =================
        private void btnDatei_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "WP-Projekt (*.wpx)|*.wpx" })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                txtDatei.Text = ofd.FileName;
                ZeigePaketInfo(ofd.FileName);
                btnImport.Enabled = File.Exists(ofd.FileName);
            }
        }

        // Manifest aus dem ZIP lesen und anzeigen (ohne die DB anzufassen).
        private void ZeigePaketInfo(string pfad)
        {
            try
            {
                using (var zip = ZipFile.OpenRead(pfad))
                {
                    var e = zip.GetEntry("manifest.json");
                    if (e == null) { lblInfo.Text = "Kein gültiges Paket (manifest.json fehlt)."; return; }
                    string json; using (var r = new StreamReader(e.Open())) json = r.ReadToEnd();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        string quelle = Prop(root, "sourceProject");
                        string datum = Prop(root, "exportedUtc");
                        int schema = root.TryGetProperty("schemaVersion", out var sv) ? sv.GetInt32() : 0;
                        if (DateTime.TryParse(datum, out var dt)) datum = dt.ToLocalTime().ToString("g");
                        lblInfo.Text = "Quellprojekt: " + quelle + "\r\nExportiert: " + datum + "   ·   Schema-Version: " + schema;
                        if (string.IsNullOrWhiteSpace(txtZielname.Text)) txtZielname.Text = quelle;
                    }
                }
            }
            catch (Exception ex) { lblInfo.Text = "Paket konnte nicht gelesen werden: " + ex.Message; }
        }

        private static string Prop(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) ? (v.GetString() ?? "") : "";

        private async void btnImport_Click(object sender, EventArgs e)
        {
            string pfad = txtDatei.Text;
            if (!File.Exists(pfad)) { MessageBox.Show("Bitte zuerst eine Datei wählen."); return; }

            var modus = rbUeberschreiben.Checked ? ProjektExportImportCtrl.BeiVorhandenem.Ueberschreiben
                      : rbAbbrechen.Checked      ? ProjektExportImportCtrl.BeiVorhandenem.Abbrechen
                                                 : ProjektExportImportCtrl.BeiVorhandenem.NeuerName;
            string name = string.IsNullOrWhiteSpace(txtZielname.Text) ? null : txtZielname.Text.Trim();

            if (modus == ProjektExportImportCtrl.BeiVorhandenem.Ueberschreiben &&
                MessageBox.Show("Ein evtl. vorhandenes Projekt gleichen Namens wird unwiderruflich überschrieben. Fortfahren?",
                    "Überschreiben", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            UiSperren(true);
            var prog = MacheProgress();
            var res = await Task.Run(() =>
            {
                int id = _io.Importieren(pfad, name, modus, prog, out string f);
                return new { id, f };
            });
            UiSperren(false);

            if (res.id > 0)
            {
                ImportierteProjektId = res.id;
                ImportierterName = name ?? "";   // ggf. aus Manifest; Aufrufer kann per ID nachladen
                lblStatus.Text = "Import abgeschlossen.";
                MessageBox.Show("Projekt importiert.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;   // Dialog schließen, Ergebnis verfügbar
            }
            else
            {
                lblStatus.Text = "Import fehlgeschlagen.";
                MessageBox.Show("Import fehlgeschlagen:\r\n" + (res.f ?? "unbekannter Fehler"),
                    "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiSperren(bool busy)
        {
            tabs.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (!busy) { pb.Value = 0; }
        }
    }
}
