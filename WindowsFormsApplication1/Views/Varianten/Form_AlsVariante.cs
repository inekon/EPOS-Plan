using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// „Als Variante speichern…" (Konzept Kap. 3.3): überführt den aktuellen Stand
    /// des geöffneten Stammprojekts per Bezeichner in eine Variante — Kopie über
    /// VariantenCtrl.AnlegenAusStamm (Duplizieren + Tab_Variante + Energie-Settings).
    ///
    /// Aufruf aus Menü/Hauptformular (idProjekt = aktuell geöffnetes Projekt):
    ///     Form_AlsVariante.Zeige(this, idProjekt, projektname);
    /// Ist das geöffnete Projekt selbst eine Variante, wird ihr Stamm verwendet.
    /// Die Form ist komplett im Code aufgebaut (kein Designer/.resx nötig).
    /// </summary>
    public class Form_AlsVariante : Form
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private TextBox txtBezeichner;
        private Label lblInfo, lblStatus;
        private Button btnOk, btnAbbrechen;

        /// <summary>Komfort-Einstieg: löst Variante→Stamm auf und zeigt den Dialog.</summary>
        public static void Zeige(IWin32Window owner, int idProjekt, string projektname)
        {
            if (idProjekt <= 0)
            {
                MessageBox.Show("Bitte zuerst ein Projekt öffnen.", "Als Variante speichern",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ctrl = new VariantenCtrl();
            int idStamm = idProjekt;
            string stammName = projektname ?? "";

            int refId = ctrl.StammRefDerVariante(idProjekt);
            if (refId > 0)
            {
                // Geöffnetes Projekt ist bereits eine Variante → deren Stamm verwenden.
                idStamm = refId;
                ProjektCtrl pc = new ProjektCtrl();
                pc.ReadSingle(refId);
                if (pc.rows > 0) stammName = pc.m_szProjektname;
            }
            else if (string.IsNullOrWhiteSpace(stammName))
            {
                ProjektCtrl pc = new ProjektCtrl();
                pc.ReadSingle(idProjekt);
                if (pc.rows > 0) stammName = pc.m_szProjektname;
            }

            using (var dlg = new Form_AlsVariante(idStamm, stammName))
                dlg.ShowDialog(owner);
        }

        public Form_AlsVariante(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblInfo = new Label();
            this.txtBezeichner = new TextBox();
            this.lblStatus = new Label();
            this.btnOk = new Button();
            this.btnAbbrechen = new Button();
            this.SuspendLayout();

            this.lblInfo.Location = new Point(12, 12);
            this.lblInfo.Size = new Size(396, 36);
            this.lblInfo.Text = "Der aktuelle Stand von \"" + _stammName + "\" wird als neue " +
                                "Variante gespeichert. Bezeichner der Variante:";

            this.txtBezeichner.Location = new Point(15, 54);
            this.txtBezeichner.Size = new Size(390, 23);

            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(12, 84);
            this.lblStatus.Size = new Size(396, 18);

            this.btnOk.Location = new Point(200, 110);
            this.btnOk.Size = new Size(120, 28);
            this.btnOk.Text = "Anlegen";
            this.btnOk.Click += new EventHandler(this.btnOk_Click);

            this.btnAbbrechen.Location = new Point(326, 110);
            this.btnAbbrechen.Size = new Size(80, 28);
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.DialogResult = DialogResult.Cancel;

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnAbbrechen;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(420, 150);
            this.Font = new Font("Segoe UI", 9f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_AlsVariante";
            this.Text = "Als Variante speichern";
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.txtBezeichner);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnAbbrechen);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                btnOk.Enabled = false;
                lblStatus.Text = "Lege Variante an…";
                Application.DoEvents();   // Statuszeile anzeigen (Duplizieren kann dauern)

                string fehler;
                int neueId = new VariantenCtrl().AnlegenAusStamm(_idStamm, _stammName,
                                                                 txtBezeichner.Text, out fehler);
                if (neueId <= 0)
                {
                    lblStatus.Text = fehler ?? "Variante konnte nicht angelegt werden.";
                    btnOk.Enabled = true;
                    return;
                }

                MessageBox.Show("Variante '" + txtBezeichner.Text.Trim() + "' wurde angelegt.\r\n" +
                                "Verwaltung und Bericht: Berichte & Kosten → Varianten.",
                                "Als Variante speichern", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Fehler: " + ex.Message;
                btnOk.Enabled = true;
            }
            finally { Cursor = Cursors.Default; }
        }
    }
}
